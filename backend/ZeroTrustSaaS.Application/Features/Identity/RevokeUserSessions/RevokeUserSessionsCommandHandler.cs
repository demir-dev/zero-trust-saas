using Microsoft.Extensions.Logging;
using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.RevokeUserSessions;

public sealed class RevokeUserSessionsCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IDateTimeProvider dateTimeProvider,
    ISecurityStampCache securityStampCache,
    ISessionStatusCache sessionStatusCache,
    ILogger<RevokeUserSessionsCommandHandler> logger,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RevokeUserSessionsCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdWithTokensAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var now = dateTimeProvider.UtcNow;
        var result = user.RevokeAllUserRefreshTokens(now);

        if (result.IsFailure)
            return result;

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

        // Invalidate caches — stamp first (covers legacy JWTs without session_id),
        // then each individual session (covers JWTs with session_id claim).
        securityStampCache.Invalidate(command.UserId);

        var revokedSessions = user.RefreshTokens.Where(t => t.IsRevoked).ToList();
        foreach (var t in revokedSessions)
            sessionStatusCache.Invalidate(t.Id);

        // Debug: log every token so we can verify the DB write took effect.
        logger.LogInformation(
            "[RevokeUserSessions] user={UserId}  total_tokens={Total}  revoked_now={Revoked}",
            command.UserId, user.RefreshTokens.Count, revokedSessions.Count);

        foreach (var t in user.RefreshTokens)
        {
            logger.LogDebug(
                "[RevokeUserSessions] token  id={Id}  is_active={IsActive}  " +
                "is_used={IsUsed}  is_revoked={IsRevoked}  " +
                "used_at={UsedAtUtc}  revoked_at={RevokedAtUtc}  expires_at={ExpiresAtUtc}",
                t.Id,
                t.IsActive,
                t.IsUsed,
                t.IsRevoked,
                t.UsedAtUtc?.ToString("O"),
                t.RevokedAtUtc?.ToString("O"),
                t.ExpiresAtUtc.ToString("O"));
        }

        return Result.Success();
    }
}
