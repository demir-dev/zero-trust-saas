namespace ZeroTrustSaaS.Application.Features.Audit.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    Guid? TenantId = null,
    Guid? UserId = null,
    int Page = 1,
    int PageSize = 50);
