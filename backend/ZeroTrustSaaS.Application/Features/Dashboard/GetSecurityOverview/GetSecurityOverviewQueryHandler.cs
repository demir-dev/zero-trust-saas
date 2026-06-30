using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;

namespace ZeroTrustSaaS.Application.Features.Dashboard.GetSecurityOverview;

public sealed class GetSecurityOverviewQueryHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ITenantMembershipRepository membershipRepository,
    ITrustedDeviceRepository deviceRepository,
    IAuditLogRepository auditLogRepository)
{
    public async Task<Result<SecurityOverviewDto>> Handle(
        GetSecurityOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        // EF Core DbContext is not thread-safe — queries must run sequentially.
        int totalTenants;
        int totalUsers;
        int mfaEnabledCount;
        int lockedUsersCount;
        int trustedDevices;
        int revokedDevices;
        int blockedDevices;

        if (query.TenantId.HasValue)
        {
            var tenantId = query.TenantId.Value;
            totalTenants = 1;
            totalUsers = await membershipRepository.CountByTenantAsync(tenantId, cancellationToken);
            mfaEnabledCount = await userRepository.CountMfaEnabledByTenantAsync(tenantId, cancellationToken);
            lockedUsersCount = await userRepository.CountLockedByTenantAsync(tenantId, cancellationToken);
            trustedDevices = await deviceRepository.CountByStatusAndTenantAsync(DeviceStatus.Trusted, tenantId, cancellationToken);
            revokedDevices = await deviceRepository.CountByStatusAndTenantAsync(DeviceStatus.Revoked, tenantId, cancellationToken);
            blockedDevices = await deviceRepository.CountByStatusAndTenantAsync(DeviceStatus.Blocked, tenantId, cancellationToken);
        }
        else
        {
            totalTenants = await tenantRepository.CountAsync(cancellationToken);
            totalUsers = await userRepository.CountTotalAsync(cancellationToken);
            mfaEnabledCount = await userRepository.CountMfaEnabledAsync(cancellationToken);
            lockedUsersCount = await userRepository.CountLockedAsync(cancellationToken);
            trustedDevices = await deviceRepository.CountByStatusAsync(DeviceStatus.Trusted, cancellationToken);
            revokedDevices = await deviceRepository.CountByStatusAsync(DeviceStatus.Revoked, cancellationToken);
            blockedDevices = await deviceRepository.CountByStatusAsync(DeviceStatus.Blocked, cancellationToken);
        }

        var auditCount = await auditLogRepository.CountAsync(query.TenantId, cancellationToken);
        var suspicious = await auditLogRepository.CountByEventTypeAsync(
            SecurityEventType.SuspiciousLoginDetected,
            query.TenantId,
            cancellationToken);
        var failedLogins = await auditLogRepository.CountByEventTypeAsync(
            SecurityEventType.LoginFailed,
            query.TenantId,
            cancellationToken);

        var dto = new SecurityOverviewDto(
            totalTenants,
            totalUsers,
            mfaEnabledCount,
            lockedUsersCount,
            trustedDevices,
            revokedDevices,
            blockedDevices,
            auditCount,
            suspicious,
            failedLogins);

        return Result<SecurityOverviewDto>.Success(dto);
    }
}
