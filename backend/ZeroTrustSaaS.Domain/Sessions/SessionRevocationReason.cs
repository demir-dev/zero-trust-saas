namespace ZeroTrustSaaS.Domain.Sessions;

public enum SessionRevocationReason
{
    None = 0,
    UserLogout = 1,
    AdminRevoked = 2,
    SecurityStampRotated = 3,
    DeviceBlocked = 4,
    DeviceRevoked = 5,
    PasswordChanged = 6,
    AccountSuspended = 7,
    ReplayAttack = 8,
}
