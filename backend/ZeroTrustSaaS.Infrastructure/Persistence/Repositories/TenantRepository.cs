using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Tenants;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository(AppDbContext dbContext) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Slug.Value == slug, cancellationToken);
    }

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return dbContext.Tenants
            .AnyAsync(t => t.Slug.Value == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Tenants
            .OrderBy(t => t.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Tenants.CountAsync(cancellationToken);
    }

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        return dbContext.Tenants.AddAsync(tenant, cancellationToken).AsTask();
    }

    public void Update(Tenant tenant)
    {
        dbContext.Tenants.Update(tenant);
    }
}
