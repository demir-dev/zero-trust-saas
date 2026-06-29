namespace ZeroTrustSaaS.Domain.Security.Enums;

public enum MfaMethod
{
    None = 0,

    Totp = 1,

    Email = 2,

    Sms = 3
}