using Microsoft.Extensions.Caching.Memory;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class TenantStatusCache(IMemoryCache cache, ITenantRepository tenantRepository)
    : ITenantStatusCache
{
    private static string Key(Guid tenantId) => $"ts:{tenantId}";

    public async Task<bool> IsActiveAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key(tenantId), out bool cached))
            return cached;

        var tenant = await tenantRepository.GetByIdAsync(tenantId, ct);
        bool isActive = tenant?.IsActive ?? false;
        cache.Set(Key(tenantId), isActive, TimeSpan.FromMinutes(5));
        return isActive;
    }

    public void Invalidate(Guid tenantId) => cache.Remove(Key(tenantId));
}
