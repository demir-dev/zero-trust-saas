using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;
using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Application.Features.Devices.TrustDeviceById;

public sealed class TrustDeviceByIdCommandHandler(
    ITrustedDeviceRepository deviceRepository,
    IAuditLogRepository auditLogRepository,
    IDeviceStatusCache deviceStatusCache,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        TrustDeviceByIdCommand command,
        CancellationToken cancellationToken = default)
    {
        var device = await deviceRepository.GetByIdAsync(command.DeviceId, cancellationToken);

        if (device is null)
            return Result.Failure(TrustedDeviceErrors.NotFound);

        if (device.IsRevoked)
            return Result.Failure(TrustedDeviceErrors.AlreadyRevoked);

        if (device.IsTrusted)
            return Result.Success();

        var now = dateTimeProvider.UtcNow;
        var result = device.Trust(now);

        if (result.IsFailure)
            return result;

        deviceRepository.Update(device);

        var logResult = AuditLog.Create(
            SecurityEventType.TrustedDeviceAdded,
            AuditSeverity.Info,
            now,
            userId: device.UserId,
            tenantId: null);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        deviceStatusCache.Invalidate(device.Id);

        return Result.Success();
    }
}
