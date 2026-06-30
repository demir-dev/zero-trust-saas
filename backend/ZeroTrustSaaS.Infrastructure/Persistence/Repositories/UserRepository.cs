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

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public void Update(User user)
    {
        // Capture new (Detached) collection members BEFORE attaching the User.
        // Setting Entry(user).State = Modified triggers EF relationship fixup, which
        // auto-tracks any navigation items it finds — changing their state from Detached
        // to Modified. A newly created LoginAttempt/RefreshToken with a Guid PK would then
        // be marked Modified (not Detached), so the post-attach check would miss them and
        // EF would emit UPDATEs against rows that don't exist yet → DbUpdateConcurrencyException.
        var newAttempts = user.LoginAttempts
            .Where(a => dbContext.Entry(a).State == EntityState.Detached)
            .ToList();
        var newTokens = user.RefreshTokens
            .Where(t => dbContext.Entry(t).State == EntityState.Detached)
            .ToList();

        dbContext.Entry(user).State = EntityState.Modified;

        foreach (var attempt in newAttempts)
            dbContext.Entry(attempt).State = EntityState.Added;

        foreach (var token in newTokens)
            dbContext.Entry(token).State = EntityState.Added;
    }
}
