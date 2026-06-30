using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Devices;

public sealed class DeviceFingerprint : ValueObject
{
    public string Value { get; }

    private DeviceFingerprint(string value)
    {
        Value = value;
    }

    public static Result<DeviceFingerprint> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<DeviceFingerprint>.Failure(
                Error.Validation(
                    "DeviceFingerprint.Empty",
                    "Device fingerprint cannot be empty."));
        }

        return Result<DeviceFingerprint>.Success(
            new DeviceFingerprint(value.Trim()));
    }

    public static DeviceFingerprint From(string value)
    {
        return new(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(DeviceFingerprint fingerprint)
    {
        return fingerprint.Value;
    }
}