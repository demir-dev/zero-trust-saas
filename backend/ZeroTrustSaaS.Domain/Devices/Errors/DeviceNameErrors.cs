using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Devices.Errors;

public static class DeviceNameErrors
{
    public static readonly Error Required =
        Error.Validation(
            "Devices.DeviceName.Required",
            "The device name is required.");

    public static readonly Error TooLong =
        Error.Validation(
            "Devices.DeviceName.TooLong",
            $"The device name cannot exceed {DeviceName.MaxLength} characters.");
}