using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Authorization.GetPermissions;

public sealed class GetPermissionsQueryHandler(IPermissionRepository permissionRepository)
{
    public async Task<Result<IReadOnlyList<PermissionDto>>> Handle(
        GetPermissionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var permissions = await permissionRepository.GetAllAsync(cancellationToken);

        var items = permissions
            .Select(p => new PermissionDto(p.Id, p.Code.Value, p.Description))
            .ToList();

        return Result<IReadOnlyList<PermissionDto>>.Success(items);
    }
}
