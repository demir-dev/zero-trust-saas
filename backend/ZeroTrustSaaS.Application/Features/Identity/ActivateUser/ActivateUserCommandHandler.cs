using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.ActivateUser;

public sealed class ActivateUserCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUserContext currentUser,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        ActivateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserManage);
        if (permCheck.IsFailure) return permCheck;
        var user = await userRepository.GetByIdWithTokensAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var now = dateTimeProvider.UtcNow;
        var result = user.Resume(now);

        if (result.IsFailure)
            return result;

        userRepository.Update(user);

        var logResult = AuditLog.Create(
            SecurityEventType.UserActivated,
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
