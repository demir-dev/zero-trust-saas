using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Authorization.AssignPermissionToRole;

public sealed class AssignPermissionToRoleCommandHandler(
    IRoleRepository roleRepository,
    ICurrentUserContext currentUser,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        AssignPermissionToRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.RoleManage);
        if (permCheck.IsFailure) return permCheck;
        var role = await roleRepository.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var codeResult = PermissionCode.Create(command.PermissionCode);

        if (codeResult.IsFailure)
            return codeResult;

        var result = role.AssignPermission(codeResult.Value, dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return result;

        roleRepository.Update(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
