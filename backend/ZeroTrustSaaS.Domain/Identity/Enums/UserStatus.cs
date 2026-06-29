namespace ZeroTrustSaaS.Domain.Identity;

public enum UserStatus
{
    PendingVerification = 1,
    Active = 2,
    Locked = 3,
    Suspended = 4,
    Disabled = 5
}