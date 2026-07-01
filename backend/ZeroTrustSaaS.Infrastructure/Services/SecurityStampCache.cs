using Microsoft.Extensions.Caching.Memory;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class SecurityStampCache(IMemoryCache cache, IUserRepository userRepository)
    : ISecurityStampCache
{
    private static string Key(Guid userId) => $"ss:{userId}";

    public async Task<string?> GetOrFetchStampAsync(Guid userId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key(userId), out string? cached))
            return cached;

        var stamp = await userRepository.GetSecurityStampAsync(userId, ct);
        if (stamp is not null)
            cache.Set(Key(userId), stamp, TimeSpan.FromMinutes(5));

        return stamp;
    }

    public void Invalidate(Guid userId) => cache.Remove(Key(userId));
}
