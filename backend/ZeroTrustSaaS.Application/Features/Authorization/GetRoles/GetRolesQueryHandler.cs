using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Authorization.GetRoles;

public sealed class GetRolesQueryHandler(IRoleRepository roleRepository)
{
    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(
        GetRolesQuery query,
        CancellationToken cancellationToken = default)
    {
        var roles = query.TenantId.HasValue
            ? await roleRepository.GetByTenantIdAsync(query.TenantId.Value, cancellationToken)
            : await roleRepository.GetAllGlobalAsync(cancellationToken);

        var items = roles
            .Select(r => new RoleDto(
                r.Id,
                r.Name.Value,
                r.TenantId,
                r.Scope.ToString(),
                r.IsSystem,
                r.Permissions.Select(p => p.Code.Value).ToList()))
            .ToList();

        return Result<IReadOnlyList<RoleDto>>.Success(items);
    }
}
