using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.SuspendUser;

public sealed class SuspendUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUserContext currentUser,
    IDateTimeProvider dateTimeProvider,
    ISecurityStampCache securityStampCache,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        SuspendUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserManage);
        if (permCheck.IsFailure) return permCheck;

        if (!currentUser.IsPlatformUser)
        {
            var targetLevel = await GetTargetRoleLevelAsync(command.UserId, currentUser.TenantId, cancellationToken);
            if (currentUser.GetTenantRoleLevel() <= targetLevel)
                return Result.Failure(AuthorizationErrors.InsufficientHierarchyLevel);
        }

        var user = await userRepository.GetByIdWithTokensAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var now = dateTimeProvider.UtcNow;
        var result = user.Suspend(now);

        if (result.IsFailure)
            return result;

        userRepository.Update(user);

        var logResult = AuditLog.Create(
            SecurityEventType.UserSuspended,
            AuditSeverity.Warning,
            now,
            userId: command.UserId,
            tenantId: command.TenantId);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        securityStampCache.Invalidate(command.UserId);
        return Result.Success();
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
}
