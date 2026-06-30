namespace ZeroTrustSaaS.Application.Features.Tenants.GetTenants;

public sealed record GetTenantsQuery(int Page = 1, int PageSize = 20);
