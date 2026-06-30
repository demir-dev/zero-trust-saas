using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Authorization.DeleteRole;

public sealed class DeleteRoleCommandHandler(
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        DeleteRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        if (role.IsSystem)
            return Result.Failure(RoleErrors.CannotDeleteSystemRole);

        var roleName = role.Name.Value;
        roleRepository.Remove(role);

        var now = dateTimeProvider.UtcNow;
        var logResult = AuditLog.Create(
            SecurityEventType.RoleDeleted,
            AuditSeverity.Warning,
            now,
            userId: command.ActorId,
            tenantId: command.TenantId,
            metadata: roleName);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
