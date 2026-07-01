using Microsoft.Extensions.Caching.Memory;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Devices;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class DeviceStatusCache(IMemoryCache cache, ITrustedDeviceRepository trustedDeviceRepository)
    : IDeviceStatusCache
{
    private static string Key(Guid deviceId) => $"dev:{deviceId}";

    public async Task<DeviceStatus?> GetStatusAsync(Guid deviceId, CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key(deviceId), out DeviceStatus? cached))
            return cached;

        var device = await trustedDeviceRepository.GetByIdAsync(deviceId, ct);
        var status = device?.Status;
        if (status.HasValue)
            cache.Set(Key(deviceId), status, TimeSpan.FromMinutes(5));
        return status;
    }

    public void Invalidate(Guid deviceId) => cache.Remove(Key(deviceId));
}
