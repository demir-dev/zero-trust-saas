using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Application.Features.Identity.Logout;

public sealed class LogoutCommandHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    ISessionStatusCache sessionStatusCache,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (command.SessionId.HasValue)
        {
            // Precise single-session logout: revoke only this session and its tokens.
            var session = await sessionRepository.GetByIdAsync(command.SessionId.Value, cancellationToken);

            if (session is not null && session.UserId == command.UserId && !session.IsRevoked)
            {
                session.Revoke(command.LoggedOutAtUtc, SessionRevocationReason.UserLogout);
                sessionRepository.Update(session);

                await refreshTokenRepository.RevokeAllBySessionIdAsync(
                    session.Id, command.LoggedOutAtUtc, cancellationToken);
            }

            var logResult = AuditLog.Create(
                SecurityEventType.Logout,
                AuditSeverity.Info,
                command.LoggedOutAtUtc,
                userId: command.UserId);

            if (logResult.IsSuccess)
                await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (command.SessionId.HasValue)
                sessionStatusCache.Invalidate(command.SessionId.Value);
        }
        else
        {
            // Fallback for JWTs issued before session_id claim existed: rotate stamp to invalidate all.
            user.RevokeAllUserRefreshTokens(command.LoggedOutAtUtc);
            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
