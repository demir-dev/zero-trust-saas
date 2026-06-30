namespace ZeroTrustSaaS.Application.Features.Devices.GetDevices;

public sealed record DeviceDto(
    Guid Id,
    Guid UserId,
    string Name,
    string Status,
    string DeviceFingerprint,
    string IpAddress,
    string? Country,
    string? Browser,
    string? OperatingSystem,
    DateTime? TrustedAtUtc,
    DateTime? LastSeenAtUtc,
    DateTime? RevokedAtUtc);
