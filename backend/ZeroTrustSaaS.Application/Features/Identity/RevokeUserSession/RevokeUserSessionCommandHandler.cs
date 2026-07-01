using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.RevokeUserSession;

public sealed class RevokeUserSessionCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUserContext currentUser,
    ISecurityStampCache securityStampCache,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RevokeUserSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserManage);
        if (permCheck.IsFailure) return permCheck;

        var user = await userRepository.GetByIdWithTokensAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var now = dateTimeProvider.UtcNow;

        // Revoking any session rotates the stamp so the user's existing JWTs are
        // immediately rejected by OnTokenValidated on the next authenticated request.
        var result = user.RevokeAllUserRefreshTokens(now);
        if (result.IsFailure) return result;

        userRepository.Update(user);

        var logResult = AuditLog.Create(
            SecurityEventType.SessionsRevoked,
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
}
