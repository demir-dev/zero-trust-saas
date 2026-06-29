namespace ZeroTrustSaaS.Domain.Common;

public abstract class AuditableEntity : AggregateRoot
{
    public DateTime CreatedAtUtc { get; protected set; }

    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedAtUtc { get; protected set; }

    public string? UpdatedBy { get; protected set; }

    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id)
        : base(id)
    {
        CreatedAtUtc = DateTime.UtcNow;
    }

    internal void SetAudit(
        string? createdBy,
        string? updatedBy)
    {
        if (CreatedBy is null)
            CreatedBy = createdBy;

        UpdatedBy = updatedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}