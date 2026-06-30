using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Devices.Errors;

public static class ClientInfoErrors
{
    public static readonly Error IpAddressRequired =
        Error.Validation("Devices.ClientInfo.IpAddressRequired", "IP address is required.");

    public static readonly Error DeviceFingerprintRequired =
        Error.Validation("Devices.ClientInfo.DeviceFingerprintRequired", "Device fingerprint is required.");

    public static readonly Error BrowserRequired =
        Error.Validation("Devices.ClientInfo.BrowserRequired", "Browser information is required.");

    public static readonly Error OperatingSystemRequired =
        Error.Validation("Devices.ClientInfo.OperatingSystemRequired", "Operating system information is required.");

    public static readonly Error CountryRequired =
        Error.Validation("Devices.ClientInfo.CountryRequired", "Country information is required.");
}
