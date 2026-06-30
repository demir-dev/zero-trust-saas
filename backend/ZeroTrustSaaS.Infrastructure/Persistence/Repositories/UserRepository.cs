using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Identity;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        string email,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .FirstOrDefaultAsync(
                u => u.Email.Value == email && u.TenantId == tenantId,
                cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AnyAsync(
                u => u.Email.Value == email && u.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.RegisteredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .CountAsync(u => u.TenantId == tenantId, cancellationToken);
    }

    public Task<int> CountMfaEnabledAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .CountAsync(u => u.TenantId == tenantId && u.IsMfaEnabled, cancellationToken);
    }

    public Task<int> CountTotalAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users.CountAsync(cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public void Update(User user)
    {
        // context.Update(user) recursively marks every entity in the graph as Modified,
        // including new LoginAttempts and RefreshTokens that have Guid keys but were never
        // inserted — causing SaveChanges to emit UPDATEs that affect 0 rows.
        // Instead: mark only the User scalar properties as Modified, then explicitly
        // promote Detached collection members (new, never-tracked) to Added.
        dbContext.Entry(user).State = EntityState.Modified;

        foreach (var attempt in user.LoginAttempts)
        {
            var entry = dbContext.Entry(attempt);
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Added;
        }

        foreach (var token in user.RefreshTokens)
        {
            var entry = dbContext.Entry(token);
            if (entry.State == EntityState.Detached)
                entry.State = EntityState.Added;
        }
    }
}
