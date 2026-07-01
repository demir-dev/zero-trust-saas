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
    IAuditLogRepository auditLogRepository,
    ICurrentUserContext currentUser,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    private const string PasswordChars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<Result<ForcePasswordResetResponse>> Handle(
        ForcePasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserManage);
        if (permCheck.IsFailure) return Result<ForcePasswordResetResponse>.Failure(permCheck.Error);
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

        return Result<ForcePasswordResetResponse>.Success(new ForcePasswordResetResponse(tempPassword));
    }

    private static string GenerateTemporaryPassword(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => PasswordChars[b % PasswordChars.Length]).ToArray());
    }
}
