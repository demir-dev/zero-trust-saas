using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class SessionStatusCache(
    IMemoryCache cache,
    ISessionRepository sessionRepository,
    ILogger<SessionStatusCache> logger)
    : ISessionStatusCache
{
    private static string Key(Guid sessionId) => $"sess:{sessionId}";

    public async Task<bool> IsActiveAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key(sessionId), out bool cached))
        {
            logger.LogDebug(
                "[SessionCache] HIT  session={SessionId}  cached_active={IsActive}",
                sessionId, cached);
            return cached;
        }

        var session = await sessionRepository.GetByIdAsync(sessionId, ct);

        if (session is null)
        {
            logger.LogWarning(
                "[SessionCache] MISS  session={SessionId}  session=NOT_FOUND → active=false",
                sessionId);
            cache.Set(Key(sessionId), false, TimeSpan.FromMinutes(5));
            return false;
        }

        bool isActive = session.IsActive;

        logger.LogDebug(
            "[SessionCache] MISS  session={SessionId}  status={Status}  " +
            "is_active={IsActive}  expires_at={ExpiresAtUtc}  revoked_at={RevokedAtUtc}",
            sessionId,
            session.Status,
            isActive,
            session.ExpiresAtUtc.ToString("O"),
            session.RevokedAtUtc?.ToString("O"));

        cache.Set(Key(sessionId), isActive, TimeSpan.FromMinutes(5));
        return isActive;
    }

    public void Invalidate(Guid sessionId)
    {
        cache.Remove(Key(sessionId));
        logger.LogDebug("[SessionCache] INVALIDATED  session={SessionId}", sessionId);
    }
}
