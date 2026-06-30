using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Tenants;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class TenantMembershipRepository(AppDbContext dbContext)
    : ITenantMembershipRepository
{
    public Task<TenantMembership?> GetAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.TenantMemberships
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMembership>> GetByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TenantMemberships
            .Where(m => m.TenantId == tenantId)
            .OrderBy(m => m.JoinedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMembership>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TenantMemberships
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return dbContext.TenantMemberships
            .CountAsync(m => m.TenantId == tenantId, cancellationToken);
    }

    public Task AddAsync(TenantMembership membership, CancellationToken cancellationToken = default)
    {
        return dbContext.TenantMemberships.AddAsync(membership, cancellationToken).AsTask();
    }

    public void Update(TenantMembership membership)
    {
        dbContext.Entry(membership).State = EntityState.Modified;
    }
}
