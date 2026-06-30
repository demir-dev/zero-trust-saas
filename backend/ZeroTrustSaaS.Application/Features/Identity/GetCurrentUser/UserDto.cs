namespace ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    bool IsEmailConfirmed,
    bool IsMfaEnabled,
    string MfaMethod,
    DateTime RegisteredAtUtc,
    DateTime? LastLoginUtc,
    DateTime? LockedUntilUtc);
