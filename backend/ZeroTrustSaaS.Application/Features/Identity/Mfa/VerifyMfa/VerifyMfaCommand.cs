namespace ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyMfa;

public sealed record VerifyMfaCommand(
    Guid UserId,
    string? TenantSlug,
    string Code,
    bool IsRecoveryCode,
    string IpAddress,
    string UserAgent,
    string DeviceFingerprint,
    string Country,
    string Browser,
    string OperatingSystem,
    bool TrustDevice = false);
