using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;

namespace ZeroTrustSaaS.Domain.Devices;

public sealed class TrustedDevice : AuditableEntity
{
    private TrustedDevice()
    {
    }

    private TrustedDevice(
        Guid id,
        Guid userId,
        DeviceName name,
        ClientInfo clientInfo)
        : base(id)
    {
        UserId = userId;
        Name = name;
        ClientInfo = clientInfo;

        Status = DeviceStatus.Pending;
        TrustedAtUtc = null;
        RevokedAtUtc = null;
    }

    public Guid UserId { get; private set; }

    public DeviceName Name { get; private set; } = null!;

    public ClientInfo ClientInfo { get; private set; } = null!;

    public DeviceStatus Status { get; private set; }

    public DateTime? TrustedAtUtc { get; private set; }

    public DateTime? LastSeenAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsPending =>
        Status == DeviceStatus.Pending;

    public bool IsTrusted =>
        Status == DeviceStatus.Trusted;

    public bool IsBlocked =>
        Status == DeviceStatus.Blocked;

    public bool IsRevoked =>
        Status == DeviceStatus.Revoked;

    public static Result<TrustedDevice> Register(
        Guid userId,
        DeviceName name,
        ClientInfo clientInfo)
    {
        var device = new TrustedDevice(
            Guid.NewGuid(),
            userId,
            name,
            clientInfo);

        return Result<TrustedDevice>.Success(device);
    }

    public Result Rename(DeviceName name)
    {
        if (Name == name)
            return Result.Success();

        Name = name;

        return Result.Success();
    }

    public Result Trust(DateTime trustedAtUtc)
    {
        if (IsTrusted)
            return Result.Failure(
                TrustedDeviceErrors.AlreadyTrusted);

        if (IsRevoked)
            return Result.Failure(
                TrustedDeviceErrors.AlreadyRevoked);

        Status = DeviceStatus.Trusted;
        TrustedAtUtc = trustedAtUtc;

        return Result.Success();
    }

    public Result Block()
    {
        if (IsBlocked)
            return Result.Failure(
                TrustedDeviceErrors.AlreadyBlocked);

        if (IsRevoked)
            return Result.Failure(
                TrustedDeviceErrors.AlreadyRevoked);

        Status = DeviceStatus.Blocked;

        return Result.Success();
    }

    public Result Unblock()
    {
        if (!IsBlocked)
            return Result.Failure(
                TrustedDeviceErrors.NotBlocked);

        Status = DeviceStatus.Pending;

        return Result.Success();
    }

    public Result Revoke(DateTime revokedAtUtc)
    {
        if (IsRevoked)
            return Result.Failure(
                TrustedDeviceErrors.AlreadyRevoked);

        Status = DeviceStatus.Revoked;
        RevokedAtUtc = revokedAtUtc;

        return Result.Success();
    }

    public Result RecordLogin(DateTime occurredAtUtc)
    {
        LastLoginAtUtc = occurredAtUtc;
        LastSeenAtUtc = occurredAtUtc;

        return Result.Success();
    }

    public Result RecordSeen(DateTime occurredAtUtc)
    {
        LastSeenAtUtc = occurredAtUtc;

        return Result.Success();
    }
}