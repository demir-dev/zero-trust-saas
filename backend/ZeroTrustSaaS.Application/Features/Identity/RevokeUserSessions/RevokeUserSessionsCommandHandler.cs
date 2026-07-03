using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Application.Features.Identity.RevokeUserSessions;

public sealed class RevokeUserSessionsCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    IDateTimeProvider dateTimeProvider,
    ISecurityStampCache securityStampCache,
    ISessionStatusCache sessionStatusCache,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RevokeUserSessionsCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var now = dateTimeProvider.UtcNow;

        // Rotate stamp first — immediately invalidates all in-flight JWTs for this user.
        user.RevokeAllUserRefreshTokens(now);
        userRepository.Update(user);

        // Bulk-revoke all sessions and their tokens; capture IDs for cache invalidation.
        var revokedSessionIds = await sessionRepository.RevokeAllByUserIdAsync(
            command.UserId, now, SessionRevocationReason.SecurityStampRotated, cancellationToken);

        await refreshTokenRepository.RevokeAllByUserIdAsync(command.UserId, now, cancellationToken);

        var logResult = AuditLog.Create(
            SecurityEventType.LogoutAll,
            AuditSeverity.Warning,
            now,
            userId: command.UserId,
            tenantId: command.TenantId);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Stamp invalidation covers all JWTs via the stamp-check path.
        securityStampCache.Invalidate(command.UserId);

        // Also invalidate individual session cache entries so the session-check path
        // rejects revoked sessions immediately, even if the stamp cache hasn't expired yet.
        foreach (var sessionId in revokedSessionIds)
            sessionStatusCache.Invalidate(sessionId);

        return Result.Success();
    }
}
