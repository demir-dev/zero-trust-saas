using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Devices.Errors;

public static class DeviceFingerprintErrors
{
    public static readonly Error Required =
        Error.Validation("Devices.DeviceFingerprint.Required",
            "Device fingerprint is required.");
}
