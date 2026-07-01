using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;

namespace ZeroTrustSaaS.Application.Features.Devices.BlockDevice;

public sealed class BlockDeviceCommandHandler(
    ITrustedDeviceRepository deviceRepository,
    IUserRepository userRepository,
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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate each revoked session and the device status so in-flight JWTs are rejected.
        if (user is not null)
        {
            var revokedIds = user.RefreshTokens
                .Where(rt => rt.TrustedDeviceId == device.Id)
                .Select(rt => rt.Id);

            foreach (var sessionId in revokedIds)
                sessionStatusCache.Invalidate(sessionId);
        }

        deviceStatusCache.Invalidate(device.Id);

        return Result.Success();
    }
}
