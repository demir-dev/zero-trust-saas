namespace ZeroTrustSaaS.Application.Features.Devices.RevokeDevice;

public sealed record RevokeDeviceCommand(Guid DeviceId, DateTime RevokedAtUtc);
