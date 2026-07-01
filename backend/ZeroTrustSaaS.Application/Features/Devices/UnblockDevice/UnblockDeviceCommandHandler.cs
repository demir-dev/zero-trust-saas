using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;
using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Application.Features.Devices.UnblockDevice;

public sealed class UnblockDeviceCommandHandler(
    ITrustedDeviceRepository deviceRepository,
    IAuditLogRepository auditLogRepository,
    IDeviceStatusCache deviceStatusCache,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        UnblockDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var device = await deviceRepository.GetByIdAsync(command.DeviceId, cancellationToken);

        if (device is null)
            return Result.Failure(TrustedDeviceErrors.NotFound);

        var result = device.Unblock();

        if (result.IsFailure)
            return result;

        deviceRepository.Update(device);

        var now = dateTimeProvider.UtcNow;
        var logResult = AuditLog.Create(
            SecurityEventType.TrustedDeviceUnblocked,
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
