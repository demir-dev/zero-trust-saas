namespace ZeroTrustSaaS.Application.Features.Identity.Login;

public sealed record LoginCommand(
    string TenantSlug,
    string Email,
    string Password,
    string IpAddress,
    string UserAgent,
    string DeviceFingerprint,
    string Country,
    string Browser,
    string OperatingSystem);
