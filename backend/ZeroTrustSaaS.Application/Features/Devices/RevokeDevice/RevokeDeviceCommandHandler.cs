using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Application.Features.Devices.RevokeDevice;

public sealed class RevokeDeviceCommandHandler(
    ITrustedDeviceRepository deviceRepository,
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    ISessionStatusCache sessionStatusCache,
    IDeviceStatusCache deviceStatusCache,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RevokeDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var device = await deviceRepository.GetByIdAsync(command.DeviceId, cancellationToken);

        if (device is null)
            return Result.Failure(TrustedDeviceErrors.NotFound);

        var now = dateTimeProvider.UtcNow;
        var result = device.Revoke(now);
        if (result.IsFailure)
            return result;

        deviceRepository.Update(device);

        // Revoke only sessions belonging to this device; leave other devices' sessions alive.
        var deviceSessions = await sessionRepository.GetActiveByDeviceIdAsync(device.Id, cancellationToken);

        foreach (var session in deviceSessions)
        {
            session.Revoke(now, SessionRevocationReason.DeviceRevoked);
            sessionRepository.Update(session);
            await refreshTokenRepository.RevokeAllBySessionIdAsync(session.Id, now, cancellationToken);
        }

        var logResult = AuditLog.Create(
            SecurityEventType.TrustedDeviceRevoked,
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
