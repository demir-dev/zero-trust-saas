using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;

namespace ZeroTrustSaaS.Application.Features.Devices.BlockDevice;

public sealed class BlockDeviceCommandHandler(
    ITrustedDeviceRepository deviceRepository,
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
