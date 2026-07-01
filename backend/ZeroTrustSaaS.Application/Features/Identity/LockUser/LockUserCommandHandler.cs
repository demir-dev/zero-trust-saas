using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.LockUser;

public sealed class LockUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ICurrentUserContext currentUser,
    ISecurityStampCache securityStampCache,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan DefaultLockDuration = TimeSpan.FromHours(24);

    public async Task<Result> Handle(
        LockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserManage);
        if (permCheck.IsFailure) return permCheck;

        if (!currentUser.IsPlatformUser)
        {
            var targetLevel = await GetTargetRoleLevelAsync(command.UserId, currentUser.TenantId, cancellationToken);
            if (currentUser.GetTenantRoleLevel() <= targetLevel)
                return Result.Failure(AuthorizationErrors.InsufficientHierarchyLevel);
        }

        var user = await userRepository.GetByIdWithTokensAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var duration = command.Duration ?? DefaultLockDuration;
        var result = user.LockUntil(command.LockedAtUtc.Add(duration), command.LockedAtUtc);

        if (result.IsFailure)
            return result;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        securityStampCache.Invalidate(command.UserId);

        return Result.Success();
    }

    private async Task<int> GetTargetRoleLevelAsync(Guid userId, Guid? tenantId, CancellationToken ct)
    {
        var userRoles = await roleRepository.GetUserRolesAsync(userId, tenantId, ct);
        var maxLevel = 0;
        foreach (var ur in userRoles.Where(r => r.IsActive))
        {
            var role = await roleRepository.GetByIdAsync(ur.RoleId, ct);
            if (role is not null)
            {
                var lvl = WellKnownPermissions.GetRoleLevel(role.Name.Value);
                if (lvl > maxLevel) maxLevel = lvl;
            }
        }
        return maxLevel;
    }
}
