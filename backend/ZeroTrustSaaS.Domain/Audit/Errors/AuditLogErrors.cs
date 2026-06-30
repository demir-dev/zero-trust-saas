using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Audit.Errors;

public static class AuditLogErrors
{
    public static readonly Error InvalidOccurredAt =
        Error.Validation("Audit.AuditLog.InvalidOccurredAt",
            "Audit log occurrence time must be a valid UTC date.");

    public static readonly Error MetadataTooLong =
        Error.Validation("Audit.AuditLog.MetadataTooLong",
            $"Metadata must not exceed {AuditLog.MaxMetadataLength} characters.");
}
