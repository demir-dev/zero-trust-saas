using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Application.Features.Identity.Mfa;

public sealed class EnableMfaCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        EnableMfaCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var secretResult = MfaSecret.Create(command.Secret);

        if (secretResult.IsFailure)
            return Result.Failure(secretResult.Error);

        var now = dateTimeProvider.UtcNow;

        var result = user.EnableMfa(command.Method, secretResult.Value, now);

        if (result.IsFailure)
            return result;

        userRepository.Update(user);

        var logResult = AuditLog.Create(
            SecurityEventType.MfaEnabled,
            AuditSeverity.High,
            now,
            userId: user.Id,
            tenantId: null);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
