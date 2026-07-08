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

    public Task<User?> GetByIdWithTokensAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // RefreshTokens are no longer a navigation on User. Delegates to standard lookup.
        return GetByIdAsync(id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AnyAsync(u => u.Email.Value == email, cancellationToken);
    }

    public Task<int> CountTotalAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users.CountAsync(cancellationToken);
    }

    public Task<int> CountMfaEnabledAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users.CountAsync(u => u.IsMfaEnabled, cancellationToken);
    }

    public Task<int> CountLockedAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users.CountAsync(
            u => u.Status == UserStatus.Locked && u.LockedUntilUtc > DateTime.UtcNow,
            cancellationToken);
    }

    public Task<int> CountMfaEnabledByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return dbContext.TenantMemberships
            .Where(m => m.TenantId == tenantId)
            .Join(dbContext.Users, m => m.UserId, u => u.Id, (m, u) => u)
            .CountAsync(u => u.IsMfaEnabled, cancellationToken);
    }

    public Task<int> CountLockedByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return dbContext.TenantMemberships
            .Where(m => m.TenantId == tenantId)
            .Join(dbContext.Users, m => m.UserId, u => u.Id, (m, u) => u)
            .CountAsync(
                u => u.Status == UserStatus.Locked && u.LockedUntilUtc > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<string?> GetSecurityStampAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stamp = await dbContext.Users
            .Where(u => u.Id == id)
            .Select(u => (Guid?)u.SecurityStamp.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return stamp?.ToString();
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public void Update(User user)
    {
        // No-op: every caller fetches the User via GetByIdAsync on this same scoped
        // DbContext before mutating it, so the aggregate is already tracked. EF's
        // automatic change detection on SaveChanges picks up scalar changes and newly
        // added LoginAttempts without help — see AppDbContext.OnModelCreating, which
        // configures all Guid keys as ValueGeneratedNever so EF stops guessing
        // Added vs. Modified from whether the key is already set.
    }
}
