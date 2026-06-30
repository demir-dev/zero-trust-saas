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
        var totalTenantsTask = tenantRepository.CountAsync(cancellationToken);
        var totalUsersTask = userRepository.CountTotalAsync(cancellationToken);
        var trustedDevicesTask = deviceRepository.CountByStatusAsync(DeviceStatus.Trusted, cancellationToken);
        var revokedDevicesTask = deviceRepository.CountByStatusAsync(DeviceStatus.Revoked, cancellationToken);
        var blockedDevicesTask = deviceRepository.CountByStatusAsync(DeviceStatus.Blocked, cancellationToken);
        var auditCountTask = auditLogRepository.CountAsync(query.TenantId, cancellationToken);
        var suspiciousTask = auditLogRepository.CountByEventTypeAsync(
            SecurityEventType.SuspiciousLoginDetected,
            query.TenantId,
            cancellationToken);
        var failedLoginTask = auditLogRepository.CountByEventTypeAsync(
            SecurityEventType.LoginFailed,
            query.TenantId,
            cancellationToken);

        await Task.WhenAll(
            totalTenantsTask, totalUsersTask, trustedDevicesTask,
            revokedDevicesTask, blockedDevicesTask, auditCountTask,
            suspiciousTask, failedLoginTask);

        int mfaEnabled = query.TenantId.HasValue
            ? await userRepository.CountMfaEnabledAsync(query.TenantId.Value, cancellationToken)
            : 0;

        var dto = new SecurityOverviewDto(
            totalTenantsTask.Result,
            totalUsersTask.Result,
            mfaEnabled,
            trustedDevicesTask.Result,
            revokedDevicesTask.Result,
            blockedDevicesTask.Result,
            auditCountTask.Result,
            suspiciousTask.Result,
            failedLoginTask.Result);

        return Result<SecurityOverviewDto>.Success(dto);
    }
}
