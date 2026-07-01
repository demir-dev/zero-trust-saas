using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;
using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Application.Features.Devices.BlockDevice;

public sealed class BlockDeviceCommandHandler(
    ITrustedDeviceRepository deviceRepository,
    IUserRepository userRepository,
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
        var user = await userRepository.GetByIdWithTokensAsync(device.UserId, cancellationToken);
        if (user is not null)
        {
            var deviceSessions = user.RefreshTokens
                .Where(rt => rt.TrustedDeviceId == device.Id && rt.IsActive)
                .ToList();

            foreach (var session in deviceSessions)
                user.RevokeRefreshToken(session.Id, now);

            if (deviceSessions.Count > 0)
                userRepository.Update(user);
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

        // Invalidate each revoked session and the device status so in-flight JWTs are rejected immediately.
        if (user is not null)
        {
            foreach (var session in user.RefreshTokens.Where(rt => rt.TrustedDeviceId == device.Id))
                sessionStatusCache.Invalidate(session.Id);
        }

        deviceStatusCache.Invalidate(device.Id);

        return Result.Success();
    }
}
