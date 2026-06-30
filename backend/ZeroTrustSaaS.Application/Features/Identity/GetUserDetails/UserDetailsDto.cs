namespace ZeroTrustSaaS.Application.Features.Identity.GetUserDetails;

public sealed record UserDetailsDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    bool IsEmailConfirmed,
    bool IsMfaEnabled,
    string MfaMethod,
    DateTime RegisteredAtUtc,
    DateTime? LastLoginUtc,
    DateTime? LockedUntilUtc,
    IReadOnlyList<UserRoleSummaryDto> Roles);

public sealed record UserRoleSummaryDto(
    Guid RoleId,
    string RoleName,
    bool IsSystem,
    DateTime AssignedAtUtc);
