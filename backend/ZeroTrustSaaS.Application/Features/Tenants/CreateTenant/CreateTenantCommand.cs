namespace ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    string OwnerPassword,
    Guid? ExistingOwnerUserId = null);
