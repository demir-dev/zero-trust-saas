namespace ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;

public sealed record UserDto(
    Guid Id,
    Guid TenantId,
    string Email,
    string Status,
    bool IsEmailConfirmed,
    bool IsMfaEnabled,
    string MfaMethod,
    DateTime RegisteredAtUtc,
    DateTime? LastLoginUtc,
    DateTime? LockedUntilUtc);
