using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Authorization;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository(AppDbContext dbContext) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Roles
            .Include("Permissions")
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<Role?> GetByNameAsync(
        string name,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Roles
            .Include("Permissions")
            .FirstOrDefaultAsync(
                r => r.Name.Value == name && r.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Include("Permissions")
            .Where(r => r.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllGlobalAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .Include("Permissions")
            .Where(r => r.TenantId == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserRole>> GetUserRolesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        return dbContext.Roles.AddAsync(role, cancellationToken).AsTask();
    }

    public Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        return dbContext.UserRoles.AddAsync(userRole, cancellationToken).AsTask();
    }

    public void Update(Role role)
    {
        dbContext.Roles.Update(role);
    }

    public void Remove(Role role)
    {
        dbContext.Roles.Remove(role);
    }

    public void UpdateUserRole(UserRole userRole)
    {
        dbContext.UserRoles.Update(userRole);
    }

    public void RemoveUserRole(UserRole userRole)
    {
        dbContext.UserRoles.Remove(userRole);
    }
}
