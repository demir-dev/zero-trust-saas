using ZeroTrustSaaS.Domain.Tenants;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface ITenantMembershipRepository
{
    Task<TenantMembership?> GetAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantMembership>> GetByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantMembership>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> CountByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task AddAsync(TenantMembership membership, CancellationToken cancellationToken = default);

    void Update(TenantMembership membership);
}
