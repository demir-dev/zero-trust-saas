using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Authorization.RevokeUserRole;

public sealed class RevokeUserRoleCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUserContext currentUser,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RevokeUserRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.RoleManage);
        if (permCheck.IsFailure) return permCheck;

        if (!currentUser.IsPlatformUser)
        {
            var targetRolesForHierarchy = await roleRepository.GetUserRolesAsync(
                command.UserId, command.TenantId, cancellationToken);
            var targetLevel = 0;
            foreach (var ur in targetRolesForHierarchy.Where(r => r.IsActive))
            {
                var role = await roleRepository.GetByIdAsync(ur.RoleId, cancellationToken);
                if (role is not null)
                {
                    var lvl = WellKnownPermissions.GetRoleLevel(role.Name.Value);
                    if (lvl > targetLevel) targetLevel = lvl;
                }
            }
            if (currentUser.GetTenantRoleLevel() <= targetLevel)
                return Result.Failure(AuthorizationErrors.InsufficientHierarchyLevel);
        }

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var userRoles = await roleRepository.GetUserRolesAsync(command.UserId, command.TenantId, cancellationToken);
        var userRole = userRoles.FirstOrDefault(ur => ur.RoleId == command.RoleId && ur.IsActive);

        if (userRole is null)
            return Result.Failure(UserRoleErrors.NotFound);

        var now = dateTimeProvider.UtcNow;
        var result = userRole.Revoke(now);

        if (result.IsFailure)
            return result;

        roleRepository.UpdateUserRole(userRole);

        var logResult = AuditLog.Create(
            SecurityEventType.RoleRevoked,
            AuditSeverity.Info,
            now,
            userId: command.UserId,
            tenantId: command.TenantId);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
