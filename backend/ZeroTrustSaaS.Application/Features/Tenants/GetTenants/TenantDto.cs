namespace ZeroTrustSaaS.Application.Features.Tenants.GetTenants;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string Status,
    int MemberCount,
    DateTime CreatedAtUtc);
