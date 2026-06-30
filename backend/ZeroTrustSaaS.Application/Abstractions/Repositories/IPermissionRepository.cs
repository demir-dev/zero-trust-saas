using ZeroTrustSaaS.Domain.Authorization;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);
}
