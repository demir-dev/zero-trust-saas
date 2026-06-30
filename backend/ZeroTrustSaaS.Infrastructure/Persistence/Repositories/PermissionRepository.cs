using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Authorization;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class PermissionRepository(AppDbContext dbContext) : IPermissionRepository
{
    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions
            .OrderBy(p => p.Code.Value)
            .ToListAsync(cancellationToken);
    }

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Code.Value == code, cancellationToken);
    }

    public Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        return dbContext.Permissions.AddAsync(permission, cancellationToken).AsTask();
    }
}
