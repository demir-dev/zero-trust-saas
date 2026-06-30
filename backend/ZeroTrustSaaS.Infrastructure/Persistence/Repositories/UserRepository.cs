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

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public void Update(User user)
    {
        dbContext.Users.Update(user);
    }
}
