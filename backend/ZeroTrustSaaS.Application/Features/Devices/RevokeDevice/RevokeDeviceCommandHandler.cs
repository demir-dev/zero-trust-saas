using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;

namespace ZeroTrustSaaS.Application.Features.Devices.RevokeDevice;

public sealed class RevokeDeviceCommandHandler(
    ITrustedDeviceRepository deviceRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        RevokeDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var device = await deviceRepository.GetByIdAsync(command.DeviceId, cancellationToken);

        if (device is null)
            return Result.Failure(TrustedDeviceErrors.NotFound);

        var result = device.Revoke(command.RevokedAtUtc);

        if (result.IsFailure)
            return result;

        deviceRepository.Update(device);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
