using ZeroTrustSaaS.Domain.Devices;

namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface IDeviceStatusCache
{
    Task<DeviceStatus?> GetStatusAsync(Guid deviceId, CancellationToken ct = default);
    void Invalidate(Guid deviceId);
}
