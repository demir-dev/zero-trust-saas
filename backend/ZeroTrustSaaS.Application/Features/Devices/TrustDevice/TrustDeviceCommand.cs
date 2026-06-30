namespace ZeroTrustSaaS.Application.Features.Devices.TrustDevice;

public sealed record TrustDeviceCommand(
    Guid UserId,
    string DeviceName,
    string DeviceFingerprint,
    string IpAddress,
    string Country,
    string Browser,
    string OperatingSystem);
