using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Domain.Devices;

public sealed class ClientInfo : ValueObject
{
    public DeviceFingerprint DeviceFingerprint { get; private set; } = null!;

    public IpAddress IpAddress { get; private set; } = null!;

    public string? Country { get; private set; }

    public string Browser { get; private set; } = string.Empty;

    public string OperatingSystem { get; private set; } = string.Empty;

    private ClientInfo() { }

    private ClientInfo(
        DeviceFingerprint deviceFingerprint,
        IpAddress ipAddress,
        string? country,
        string browser,
        string operatingSystem)
    {
        DeviceFingerprint = deviceFingerprint;
        IpAddress = ipAddress;
        Country = country?.Trim();
        Browser = browser.Trim();
        OperatingSystem = operatingSystem.Trim();
    }

    public static Result<ClientInfo> Create(
        DeviceFingerprint deviceFingerprint,
        IpAddress ipAddress,
        string? country,
        string browser,
        string operatingSystem)
    {
        if (string.IsNullOrWhiteSpace(browser))
        {
            return Result<ClientInfo>.Failure(
                Error.Validation(
                    "Devices.ClientInfo.BrowserRequired",
                    "Browser is required."));
        }

        if (string.IsNullOrWhiteSpace(operatingSystem))
        {
            return Result<ClientInfo>.Failure(
                Error.Validation(
                    "Devices.ClientInfo.OperatingSystemRequired",
                    "Operating system is required."));
        }

        return Result<ClientInfo>.Success(
            new ClientInfo(
                deviceFingerprint,
                ipAddress,
                country,
                browser,
                operatingSystem));
    }

    public static ClientInfo From(
        DeviceFingerprint deviceFingerprint,
        IpAddress ipAddress,
        string? country,
        string browser,
        string operatingSystem)
    {
        return new(
            deviceFingerprint,
            ipAddress,
            country,
            browser,
            operatingSystem);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DeviceFingerprint;
        yield return IpAddress;
        yield return Country;
        yield return Browser;
        yield return OperatingSystem;
    }

    public override string ToString()
    {
        return $"{Browser} on {OperatingSystem} ({Country})";
    }
}