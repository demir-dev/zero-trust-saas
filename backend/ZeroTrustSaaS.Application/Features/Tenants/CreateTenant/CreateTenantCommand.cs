namespace ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    Guid OwnerUserId);
