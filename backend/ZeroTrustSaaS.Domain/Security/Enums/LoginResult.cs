namespace ZeroTrustSaaS.Domain.Security.Enums;

public enum LoginResult
{
    Success = 1,

    InvalidCredentials = 2,

    Locked = 3,

    Suspended = 4,

    MfaRequired = 5,

    DeviceRejected = 6
}