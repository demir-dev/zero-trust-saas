using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Audit.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
{
    public async Task<Result<PagedResult<AuditLogDto>>> Handle(
        GetAuditLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Audit.AuditLog> logs;

        if (query.UserId.HasValue)
        {
            logs = await auditLogRepository.GetByUserIdAsync(
                query.UserId.Value,
                query.Page,
                query.PageSize,
                cancellationToken);
        }
        else if (query.TenantId.HasValue)
        {
            logs = await auditLogRepository.GetByTenantIdAsync(
                query.TenantId.Value,
                query.Page,
                query.PageSize,
                cancellationToken);
        }
        else
        {
            logs = await auditLogRepository.GetRecentAsync(
                query.PageSize,
                null,
                cancellationToken);
        }

        var total = await auditLogRepository.CountAsync(query.TenantId, cancellationToken);

        var items = logs
            .Select(l => new AuditLogDto(
                l.Id,
                l.EventType.ToString(),
                l.Severity.ToString(),
                l.OccurredAtUtc,
                l.UserId,
                l.TenantId,
                l.IpAddress?.Value,
                l.UserAgent,
                l.Metadata,
                l.IsSecurityCritical))
            .ToList();

        return Result<PagedResult<AuditLogDto>>.Success(
            new PagedResult<AuditLogDto>(items, total, query.Page, query.PageSize));
    }
}
