using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;

namespace ZeroTrustSaaS.Application.Features.Dashboard.GetSecurityOverview;

public sealed class GetSecurityOverviewQueryHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ITrustedDeviceRepository deviceRepository,
    IAuditLogRepository auditLogRepository)
{
    public async Task<Result<SecurityOverviewDto>> Handle(
        GetSecurityOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        // EF Core DbContext is not thread-safe — queries must run sequentially.
        var totalTenants = await tenantRepository.CountAsync(cancellationToken);
        var totalUsers = await userRepository.CountTotalAsync(cancellationToken);
        var trustedDevices = await deviceRepository.CountByStatusAsync(DeviceStatus.Trusted, cancellationToken);
        var revokedDevices = await deviceRepository.CountByStatusAsync(DeviceStatus.Revoked, cancellationToken);
        var blockedDevices = await deviceRepository.CountByStatusAsync(DeviceStatus.Blocked, cancellationToken);
        var auditCount = await auditLogRepository.CountAsync(query.TenantId, cancellationToken);
        var suspicious = await auditLogRepository.CountByEventTypeAsync(
            SecurityEventType.SuspiciousLoginDetected,
            query.TenantId,
            cancellationToken);
        var failedLogins = await auditLogRepository.CountByEventTypeAsync(
            SecurityEventType.LoginFailed,
            query.TenantId,
            cancellationToken);
        int mfaEnabled = query.TenantId.HasValue
            ? await userRepository.CountMfaEnabledAsync(query.TenantId.Value, cancellationToken)
            : 0;

        var dto = new SecurityOverviewDto(
            totalTenants,
            totalUsers,
            mfaEnabled,
            trustedDevices,
            revokedDevices,
            blockedDevices,
            auditCount,
            suspicious,
            failedLogins);

        return Result<SecurityOverviewDto>.Success(dto);
    }
}
