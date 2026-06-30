namespace ZeroTrustSaaS.Application.Features.Identity.GetUsers;

public sealed record GetUsersQuery(
    Guid TenantId,
    int Page = 1,
    int PageSize = 20);
