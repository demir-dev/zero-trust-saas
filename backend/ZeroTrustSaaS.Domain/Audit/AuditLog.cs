using ZeroTrustSaaS.Domain.Audit.Errors;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Domain.Audit;

public sealed class AuditLog : Entity
{
    public const int MaxMetadataLength = 2000;

    private AuditLog()
    {
    }

    private AuditLog(
        Guid id,
        SecurityEventType eventType,
        AuditSeverity severity,
        DateTime occurredAtUtc,
        Guid? userId,
        Guid? tenantId,
        IpAddress? ipAddress,
        string? userAgent,
        Guid? trustedDeviceId,
        string? metadata)
        : base(id)
    {
        EventType = eventType;
        Severity = severity;
        OccurredAtUtc = occurredAtUtc;
        UserId = userId;
        TenantId = tenantId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        TrustedDeviceId = trustedDeviceId;
        Metadata = metadata;
    }

    public SecurityEventType EventType { get; private set; }

    public AuditSeverity Severity { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? TenantId { get; private set; }

    public IpAddress? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public Guid? TrustedDeviceId { get; private set; }

    public string? Metadata { get; private set; }

    public bool IsSecurityCritical =>
        Severity is AuditSeverity.High or AuditSeverity.Critical;

    public static Result<AuditLog> Create(
        SecurityEventType eventType,
        AuditSeverity severity,
        DateTime occurredAtUtc,
        Guid? userId = null,
        Guid? tenantId = null,
        IpAddress? ipAddress = null,
        string? userAgent = null,
        Guid? trustedDeviceId = null,
        string? metadata = null)
    {
        if (occurredAtUtc == default)
            return Result<AuditLog>.Failure(AuditLogErrors.InvalidOccurredAt);

        if (metadata is not null && metadata.Length > MaxMetadataLength)
            return Result<AuditLog>.Failure(AuditLogErrors.MetadataTooLong);

        return Result<AuditLog>.Success(new AuditLog(
            Guid.NewGuid(),
            eventType,
            severity,
            occurredAtUtc,
            userId,
            tenantId,
            ipAddress,
            userAgent,
            trustedDeviceId,
            metadata));
    }

    public Result AttachMetadata(string metadata)
    {
        if (metadata.Length > MaxMetadataLength)
            return Result.Failure(AuditLogErrors.MetadataTooLong);

        Metadata = metadata;

        return Result.Success();
    }
}
