using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Authorization.AssignRole;

public sealed class AssignRoleCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUserContext currentUser,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        AssignRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.RoleManage);
        if (permCheck.IsFailure) return permCheck;
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var role = await roleRepository.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var now = dateTimeProvider.UtcNow;

        var existingRoles = await roleRepository.GetUserRolesAsync(
            command.UserId,
            command.TenantId,
            cancellationToken);

        bool alreadyAssigned = existingRoles.Any(
            r => r.RoleId == command.RoleId && r.IsActive);

        if (alreadyAssigned)
            return Result.Failure(UserRoleErrors.AlreadyAssigned);

        var userRoleResult = UserRole.Create(
            command.UserId,
            command.RoleId,
            command.TenantId,
            now);

        if (userRoleResult.IsFailure)
            return userRoleResult;

        await roleRepository.AddUserRoleAsync(userRoleResult.Value, cancellationToken);

        var logResult = AuditLog.Create(
            SecurityEventType.RoleAssigned,
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
