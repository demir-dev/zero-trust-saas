using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Devices.GetDevices;

public sealed class GetDevicesQueryHandler(ITrustedDeviceRepository deviceRepository)
{
    public async Task<Result<IReadOnlyList<DeviceDto>>> Handle(
        GetDevicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var devices = await deviceRepository.GetByUserIdAsync(query.UserId, cancellationToken);

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
