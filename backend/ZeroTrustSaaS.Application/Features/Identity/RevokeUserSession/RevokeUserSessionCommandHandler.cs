using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Sessions;
using ZeroTrustSaaS.Domain.Sessions.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.RevokeUserSession;

public sealed class RevokeUserSessionCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUserContext currentUser,
    ISessionStatusCache sessionStatusCache,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RevokeUserSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserManage);
        if (permCheck.IsFailure) return permCheck;

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var session = await sessionRepository.GetByIdAsync(command.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure(SessionErrors.NotFound);

        if (session.UserId != command.UserId)
            return Result.Failure(SessionErrors.NotOwnedByUser);

        if (session.IsRevoked)
            return Result.Failure(SessionErrors.AlreadyRevoked);

        var now = dateTimeProvider.UtcNow;
        var revokeResult = session.Revoke(now, SessionRevocationReason.AdminRevoked);
        if (revokeResult.IsFailure) return revokeResult;

        sessionRepository.Update(session);

        await refreshTokenRepository.RevokeAllBySessionIdAsync(session.Id, now, cancellationToken);

        var logResult = AuditLog.Create(
            SecurityEventType.SessionRevoked,
            AuditSeverity.Warning,
            now,
            userId: command.UserId,
            tenantId: command.TenantId);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        sessionStatusCache.Invalidate(command.SessionId);

        return Result.Success();
    }
}
