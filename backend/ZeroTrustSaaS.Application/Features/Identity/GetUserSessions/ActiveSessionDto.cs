namespace ZeroTrustSaaS.Application.Features.Identity.GetUserSessions;

public sealed record ActiveSessionDto(
    Guid Id,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    string? IpAddress,
    string Browser,
    string OperatingSystem,
    string Country,
    Guid? TenantId);
