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
    ISecurityStampCache securityStampCache,
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
        var user = await userRepository.GetByIdWithTokensAsync(device.UserId, cancellationToken);
        if (user is not null)
        {
            user.RevokeAllUserRefreshTokens(now);
            userRepository.Update(user);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (user is not null)
            securityStampCache.Invalidate(device.UserId);

        return Result.Success();
    }
}
