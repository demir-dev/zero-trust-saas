using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Authorization.RemovePermissionFromRole;

public sealed class RemovePermissionFromRoleCommandHandler(
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RemovePermissionFromRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
            return Result.Failure(RoleErrors.NotFound);

        var codeResult = PermissionCode.Create(command.PermissionCode);

        if (codeResult.IsFailure)
            return codeResult;

        var result = role.RemovePermission(codeResult.Value);

        if (result.IsFailure)
            return result;

        roleRepository.Update(role);

        var now = dateTimeProvider.UtcNow;
        var logResult = AuditLog.Create(
            SecurityEventType.PermissionRemoved,
            AuditSeverity.Warning,
            now,
            userId: command.ActorId,
            tenantId: command.TenantId,
            metadata: command.PermissionCode);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
