namespace ZeroTrustSaaS.Application.Features.Tenants.GetTenantUsers;

public sealed record TenantUserDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string MembershipStatus,
    bool IsOwner,
    DateTime JoinedAtUtc);
