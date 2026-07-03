namespace ZeroTrustSaaS.Domain.Identity.Enums;

public enum RefreshTokenRevocationReason
{
    UserLogout,
    PasswordChanged,
    SecurityStampRotated,
    AdminRevoked,
    ReplayAttack,
    SuspiciousActivity,
    SessionRevoked,
}