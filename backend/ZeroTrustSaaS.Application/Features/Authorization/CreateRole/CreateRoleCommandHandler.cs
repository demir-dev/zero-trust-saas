using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Authorization.CreateRole;

public sealed class CreateRoleCommandHandler(
    IRoleRepository roleRepository,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> Handle(
        CreateRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.RoleManage);
        if (permCheck.IsFailure) return Result<Guid>.Failure(permCheck.Error);
        var nameResult = RoleName.Create(command.Name);

        if (nameResult.IsFailure)
            return Result<Guid>.Failure(nameResult.Error);

        if (!Enum.TryParse<PermissionScope>(command.Scope, ignoreCase: true, out var scope))
            scope = PermissionScope.Tenant;

        var roleResult = Role.Create(nameResult.Value, command.TenantId, scope);

        if (roleResult.IsFailure)
            return Result<Guid>.Failure(roleResult.Error);

        await roleRepository.AddAsync(roleResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(roleResult.Value.Id);
    }
}
