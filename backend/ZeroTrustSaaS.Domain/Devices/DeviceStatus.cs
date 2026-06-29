namespace ZeroTrustSaaS.Domain.Devices;

public enum DeviceStatus
{
    Pending = 1,

    Trusted = 2,

    Revoked = 3,

    Blocked = 4
}