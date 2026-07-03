using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Application.Features.Devices.BlockDevice;

public sealed class BlockDeviceCommandHandler(
    ITrustedDeviceRepository deviceRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    IDateTimeProvider dateTimeProvider,
    ISessionStatusCache sessionStatusCache,
    IDeviceStatusCache deviceStatusCache,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        BlockDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var device = await deviceRepository.GetByIdAsync(command.DeviceId, cancellationToken);

        if (device is null)
            return Result.Failure(TrustedDeviceErrors.NotFound);

        var result = device.Block();
        if (result.IsFailure)
            return result;

        deviceRepository.Update(device);

        var now = dateTimeProvider.UtcNow;

        // Revoke only sessions that belong to this device; leave other devices' sessions alive.
        var deviceSessions = await sessionRepository.GetActiveByDeviceIdAsync(device.Id, cancellationToken);

        foreach (var session in deviceSessions)
        {
            session.Revoke(now, SessionRevocationReason.DeviceBlocked);
            sessionRepository.Update(session);
            await refreshTokenRepository.RevokeAllBySessionIdAsync(session.Id, now, cancellationToken);
        }

        var logResult = AuditLog.Create(
            SecurityEventType.TrustedDeviceBlocked,
            AuditSeverity.Warning,
            now,
            userId: device.UserId,
            tenantId: null);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var session in deviceSessions)
            sessionStatusCache.Invalidate(session.Id);

        deviceStatusCache.Invalidate(device.Id);

        return Result.Success();
    }
}
