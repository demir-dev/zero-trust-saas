using ZeroTrustSaaS.Domain.Identity;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<int> CountTotalAsync(CancellationToken cancellationToken = default);

    Task<int> CountMfaEnabledAsync(CancellationToken cancellationToken = default);

    Task<int> CountLockedAsync(CancellationToken cancellationToken = default);

    Task<int> CountMfaEnabledByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<int> CountLockedByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    void Update(User user);
}
