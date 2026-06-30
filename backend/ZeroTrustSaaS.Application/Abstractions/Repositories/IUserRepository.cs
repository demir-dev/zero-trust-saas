using ZeroTrustSaaS.Domain.Identity;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<int> CountMfaEnabledAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<int> CountTotalAsync(CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    void Update(User user);
}
