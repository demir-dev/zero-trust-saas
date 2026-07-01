using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Devices.Errors;

public static class TrustedDeviceErrors
{
    public static readonly Error AlreadyTrusted =
        Error.Conflict(
            "Devices.TrustedDevice.AlreadyTrusted",
            "The device is already trusted.");

    public static readonly Error AlreadyBlocked =
        Error.Conflict(
            "Devices.TrustedDevice.AlreadyBlocked",
            "The device is already blocked.");

    public static readonly Error AlreadyRevoked =
        Error.Conflict(
            "Devices.TrustedDevice.AlreadyRevoked",
            "The device has already been revoked.");

    public static readonly Error NotFound =
        Error.NotFound(
            "Devices.TrustedDevice.NotFound",
            "Trusted device was not found.");

    public static readonly Error DeviceBlocked =
        Error.Forbidden(
            "Devices.TrustedDevice.DeviceBlocked",
            "This device has been blocked and cannot be used to sign in.");

    public static readonly Error NotBlocked =
        Error.Conflict(
            "Devices.TrustedDevice.NotBlocked",
            "The device is not currently blocked.");
}