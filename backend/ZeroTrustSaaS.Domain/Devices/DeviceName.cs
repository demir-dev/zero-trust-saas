using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices.Errors;

namespace ZeroTrustSaaS.Domain.Devices;

public sealed class DeviceName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; }

    private DeviceName(string value)
    {
        Value = value;
    }

    public static Result<DeviceName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<DeviceName>.Failure(
                DeviceNameErrors.Required);
        }

        value = value.Trim();

        if (value.Length > MaxLength)
        {
            return Result<DeviceName>.Failure(
                DeviceNameErrors.TooLong);
        }

        return Result<DeviceName>.Success(
            new DeviceName(value));
    }

    public static DeviceName From(string value)
    {
        return new(value.Trim());
    }

    public static DeviceName Empty()
    {
        return new(string.Empty);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(DeviceName deviceName)
    {
        return deviceName.Value;
    }
}