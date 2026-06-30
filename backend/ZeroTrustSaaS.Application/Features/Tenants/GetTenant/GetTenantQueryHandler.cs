using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenants;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Application.Features.Tenants.GetTenant;

public sealed class GetTenantQueryHandler(ITenantRepository tenantRepository)
{
    public async Task<Result<TenantDto>> Handle(
        GetTenantQuery query,
        CancellationToken cancellationToken = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(query.TenantId, cancellationToken);

        if (tenant is null)
            return Result<TenantDto>.Failure(TenantErrors.NotFound);

        var dto = new TenantDto(
            tenant.Id,
            tenant.Name.Value,
            tenant.Slug.Value,
            tenant.Status.ToString(),
            tenant.Memberships.Count,
            tenant.CreatedAtUtc);

        return Result<TenantDto>.Success(dto);
    }
}
