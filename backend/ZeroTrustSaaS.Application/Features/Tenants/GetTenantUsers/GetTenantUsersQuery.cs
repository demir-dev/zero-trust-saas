namespace ZeroTrustSaaS.Application.Features.Tenants.GetTenantUsers;

public sealed record GetTenantUsersQuery(Guid TenantId, int Page = 1, int PageSize = 20);
