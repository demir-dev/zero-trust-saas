using ZeroTrustSaaS.Domain.Tenants;

namespace ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    TenantPlan Plan,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    string OwnerPassword,
    Guid? ExistingOwnerUserId = null);
