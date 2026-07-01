using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class SessionStatusCache(
    IMemoryCache cache,
    IRefreshTokenRepository refreshTokenRepository,
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

        var token = await refreshTokenRepository.GetByIdAsync(sessionId, ct);

        if (token is null)
        {
            logger.LogWarning(
                "[SessionCache] MISS  session={SessionId}  token=NOT_FOUND → active=false",
                sessionId);
            cache.Set(Key(sessionId), false, TimeSpan.FromMinutes(5));
            return false;
        }

        bool isActive = token.IsActive;

        logger.LogDebug(
            "[SessionCache] MISS  session={SessionId}  token_id={TokenId}  " +
            "is_active={IsActive}  is_used={IsUsed}  is_revoked={IsRevoked}  " +
            "used_at={UsedAtUtc}  revoked_at={RevokedAtUtc}  expires_at={ExpiresAtUtc}",
            sessionId,
            token.Id,
            isActive,
            token.IsUsed,
            token.IsRevoked,
            token.UsedAtUtc?.ToString("O"),
            token.RevokedAtUtc?.ToString("O"),
            token.ExpiresAtUtc.ToString("O"));

        cache.Set(Key(sessionId), isActive, TimeSpan.FromMinutes(5));
        return isActive;
    }

    public void Invalidate(Guid sessionId)
    {
        cache.Remove(Key(sessionId));
        logger.LogDebug("[SessionCache] INVALIDATED  session={SessionId}", sessionId);
    }
}
