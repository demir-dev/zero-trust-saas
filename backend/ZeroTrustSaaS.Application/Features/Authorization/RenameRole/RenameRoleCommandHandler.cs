using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Authorization.RenameRole;

public sealed class RenameRoleCommandHandler(
    IRoleRepository roleRepository,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RenameRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.RoleManage);
        if (permCheck.IsFailure) return permCheck;
        var role = await roleRepository.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var nameResult = RoleName.Create(command.NewName);

        if (nameResult.IsFailure)
            return nameResult;

        var result = role.Rename(nameResult.Value);

        if (result.IsFailure)
            return result;

        roleRepository.Update(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
