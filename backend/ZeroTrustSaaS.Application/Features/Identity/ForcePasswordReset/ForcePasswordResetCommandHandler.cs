using System.Security.Cryptography;
using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.ForcePasswordReset;

public sealed class ForcePasswordResetCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUserContext currentUser,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    ISecurityStampCache securityStampCache,
    IUnitOfWork unitOfWork)
{
    private const string PasswordChars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<Result<ForcePasswordResetResponse>> Handle(
        ForcePasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserManage);
        if (permCheck.IsFailure) return Result<ForcePasswordResetResponse>.Failure(permCheck.Error);

        if (!currentUser.IsPlatformUser)
        {
            var targetLevel = await GetTargetRoleLevelAsync(command.UserId, currentUser.TenantId, cancellationToken);
            if (currentUser.GetTenantRoleLevel() <= targetLevel)
                return Result<ForcePasswordResetResponse>.Failure(AuthorizationErrors.InsufficientHierarchyLevel);
        }

        var user = await userRepository.GetByIdWithTokensAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result<ForcePasswordResetResponse>.Failure(UserErrors.NotFound);

        var tempPassword = GenerateTemporaryPassword(12);
        var hash = passwordHasher.Hash(tempPassword);

        var passwordHashResult = PasswordHash.Create(hash);
        if (passwordHashResult.IsFailure)
            return Result<ForcePasswordResetResponse>.Failure(passwordHashResult.Error);

        var now = dateTimeProvider.UtcNow;
        var result = user.ChangePassword(passwordHashResult.Value, now);

        if (result.IsFailure)
            return Result<ForcePasswordResetResponse>.Failure(result.Error);

        userRepository.Update(user);

        var logResult = AuditLog.Create(
            SecurityEventType.PasswordResetForced,
            AuditSeverity.High,
            now,
            userId: command.UserId,
            tenantId: command.TenantId);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        securityStampCache.Invalidate(command.UserId);

        return Result<ForcePasswordResetResponse>.Success(new ForcePasswordResetResponse(tempPassword));
    }

    private async Task<int> GetTargetRoleLevelAsync(Guid userId, Guid? tenantId, CancellationToken ct)
    {
        var userRoles = await roleRepository.GetUserRolesAsync(userId, tenantId, ct);
        var maxLevel = 0;
        foreach (var ur in userRoles.Where(r => r.IsActive))
        {
            var role = await roleRepository.GetByIdAsync(ur.RoleId, ct);
            if (role is not null)
            {
                var lvl = WellKnownPermissions.GetRoleLevel(role.Name.Value);
                if (lvl > maxLevel) maxLevel = lvl;
            }
        }
        return maxLevel;
    }

    private static string GenerateTemporaryPassword(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => PasswordChars[b % PasswordChars.Length]).ToArray());
    }
}
