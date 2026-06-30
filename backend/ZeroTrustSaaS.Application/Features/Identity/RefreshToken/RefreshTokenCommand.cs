namespace ZeroTrustSaaS.Application.Features.Identity.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress,
    string UserAgent,
    string DeviceFingerprint,
    string Country,
    string Browser,
    string OperatingSystem);
