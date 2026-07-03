using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Application.Features.Identity.GetUserSessions;

public sealed record SessionDto(
    Guid Id,
    SessionStatus Status,
    DateTime CreatedAtUtc,
    DateTime LastSeenAtUtc,
    DateTime LastActivityUtc,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    string? IpAddress,
    string? Browser,
    string? OperatingSystem,
    string? Country,
    Guid? TenantId,
    Guid? TrustedDeviceId,
    bool IsCurrentSession);
