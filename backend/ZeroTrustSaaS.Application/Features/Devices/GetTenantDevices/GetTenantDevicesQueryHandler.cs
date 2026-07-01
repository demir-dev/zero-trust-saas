using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Application.Features.Devices.GetDevices;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Devices.GetTenantDevices;

public sealed class GetTenantDevicesQueryHandler(ITrustedDeviceRepository deviceRepository)
{
    public async Task<Result<IReadOnlyList<DeviceDto>>> Handle(
        GetTenantDevicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var devices = await deviceRepository.GetByTenantIdAsync(
            query.TenantId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var items = devices
            .Select(d => new DeviceDto(
                d.Id,
                d.UserId,
                d.Name.Value,
                d.Status.ToString(),
                d.ClientInfo.DeviceFingerprint.Value,
                d.ClientInfo.IpAddress.Value,
                d.ClientInfo.Country,
                d.ClientInfo.Browser,
                d.ClientInfo.OperatingSystem,
                d.CreatedAtUtc,
                d.TrustedAtUtc,
                d.LastSeenAtUtc,
                d.LastLoginAtUtc,
                d.RevokedAtUtc))
            .ToList();

        return Result<IReadOnlyList<DeviceDto>>.Success(items);
    }
}
