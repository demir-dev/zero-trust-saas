namespace ZeroTrustSaaS.Application.Features.Dashboard.GetSecurityOverview;

public sealed record SecurityOverviewDto(
    int TotalTenants,
    int TotalUsers,
    int MfaEnabledCount,
    int LockedUsersCount,
    int TrustedDevicesCount,
    int RevokedDevicesCount,
    int BlockedDevicesCount,
    int AuditLogCount,
    int SuspiciousEventCount,
    int FailedLoginCount);
