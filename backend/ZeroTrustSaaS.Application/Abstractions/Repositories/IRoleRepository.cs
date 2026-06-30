using ZeroTrustSaaS.Domain.Authorization;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(string name, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetAllGlobalAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserRole>> GetUserRolesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);

    void Update(Role role);

    void Remove(Role role);

    void UpdateUserRole(UserRole userRole);

    void RemoveUserRole(UserRole userRole);
}
