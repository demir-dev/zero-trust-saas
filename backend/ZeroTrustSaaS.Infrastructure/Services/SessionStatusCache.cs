using Microsoft.Extensions.Caching.Memory;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class SessionStatusCache(IMemoryCache cache, IRefreshTokenRepository refreshTokenRepository)
    : ISessionStatusCache
{
    private static string Key(Guid sessionId) => $"sess:{sessionId}";

    public async Task<bool> IsActiveAsync(Guid sessionId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key(sessionId), out bool cached))
            return cached;

        var token = await refreshTokenRepository.GetByIdAsync(sessionId, ct);
        bool isActive = token?.IsActive ?? false;
        cache.Set(Key(sessionId), isActive, TimeSpan.FromMinutes(5));
        return isActive;
    }

    public void Invalidate(Guid sessionId) => cache.Remove(Key(sessionId));
}
