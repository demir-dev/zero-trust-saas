using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Tenants.GetTenants;

public sealed class GetTenantsQueryHandler(ITenantRepository tenantRepository)
{
    public async Task<Result<PagedResult<TenantDto>>> Handle(
        GetTenantsQuery query,
        CancellationToken cancellationToken = default)
    {
        var tenants = await tenantRepository.GetAllAsync(query.Page, query.PageSize, cancellationToken);
        var total = await tenantRepository.CountAsync(cancellationToken);

        var items = tenants
            .Select(t => new TenantDto(
                t.Id,
                t.Name.Value,
                t.Slug.Value,
                t.Status.ToString(),
                t.Memberships.Count,
                t.CreatedAtUtc))
            .ToList();

        return Result<PagedResult<TenantDto>>.Success(
            new PagedResult<TenantDto>(items, total, query.Page, query.PageSize));
    }
}
