using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.SuspendUser;

public sealed class SuspendUserCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        SuspendUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var now = dateTimeProvider.UtcNow;
        var result = user.Suspend(now);

        if (result.IsFailure)
            return result;

        userRepository.Update(user);

        var logResult = AuditLog.Create(
            SecurityEventType.UserSuspended,
            AuditSeverity.Warning,
            now,
            userId: command.UserId,
            tenantId: command.TenantId);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
