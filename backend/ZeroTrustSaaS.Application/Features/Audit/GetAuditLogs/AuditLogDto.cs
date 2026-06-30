namespace ZeroTrustSaaS.Application.Features.Audit.GetAuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    string EventType,
    string Severity,
    DateTime OccurredAtUtc,
    Guid? UserId,
    Guid? TenantId,
    string? IpAddress,
    string? UserAgent,
    string? Metadata,
    bool IsSecurityCritical);
