using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Application.Features.Devices.TrustDevice;

public sealed class TrustDeviceCommandHandler(
    IUserRepository userRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    IAuditLogRepository auditLogRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> Handle(
        TrustDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result<Guid>.Failure(UserErrors.NotFound);

        var nameResult = DeviceName.Create(command.DeviceName);

        if (nameResult.IsFailure)
            return Result<Guid>.Failure(nameResult.Error);

        var fingerprintResult = DeviceFingerprint.Create(command.DeviceFingerprint);

        if (fingerprintResult.IsFailure)
            return Result<Guid>.Failure(fingerprintResult.Error);

        var ipResult = IpAddress.Create(command.IpAddress);

        var clientInfoResult = ClientInfo.Create(
            fingerprintResult.Value,
            ipResult.IsSuccess ? ipResult.Value : IpAddress.Empty(),
            command.Country,
            command.Browser,
            command.OperatingSystem);

        if (clientInfoResult.IsFailure)
            return Result<Guid>.Failure(clientInfoResult.Error);

        var now = dateTimeProvider.UtcNow;

        // Dedup: if a non-revoked device with the same fingerprint already exists, trust it.
        var existingDevice = await trustedDeviceRepository
            .GetByFingerprintAsync(command.UserId, fingerprintResult.Value.Value, cancellationToken);

        if (existingDevice is not null && !existingDevice.IsRevoked)
        {
            if (!existingDevice.IsTrusted)
            {
                var reTrustResult = existingDevice.Trust(now);
                if (reTrustResult.IsFailure)
                    return Result<Guid>.Failure(reTrustResult.Error);
                trustedDeviceRepository.Update(existingDevice);
            }

            var existingLogResult = AuditLog.Create(
                SecurityEventType.TrustedDeviceAdded,
                AuditSeverity.Info,
                now,
                userId: command.UserId,
                tenantId: null,
                ipAddress: ipResult.IsSuccess ? ipResult.Value : null);

            if (existingLogResult.IsSuccess)
                await auditLogRepository.AddAsync(existingLogResult.Value, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(existingDevice.Id);
        }

        var deviceResult = TrustedDevice.Register(
            command.UserId,
            nameResult.Value,
            clientInfoResult.Value);

        if (deviceResult.IsFailure)
            return Result<Guid>.Failure(deviceResult.Error);

        var device = deviceResult.Value;
        device.Trust(now);

        await trustedDeviceRepository.AddAsync(device, cancellationToken);

        var logResult = AuditLog.Create(
            SecurityEventType.TrustedDeviceAdded,
            AuditSeverity.Info,
            now,
            userId: command.UserId,
            tenantId: null,
            ipAddress: ipResult.IsSuccess ? ipResult.Value : null);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(device.Id);
    }
}
