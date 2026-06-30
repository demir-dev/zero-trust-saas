using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Domain.Common;

public abstract class SecureAggregateRoot : AuditableEntity
{
    public SecurityStamp SecurityStamp { get; protected set; } = null!;

    public long Version { get; protected set; }

    protected SecureAggregateRoot()
    {
    }

    protected SecureAggregateRoot(Guid id)
        : base(id)
    {
        SecurityStamp = SecurityStamp.Create();
        Version = 1;
    }

    protected void RotateSecurityStamp()
    {
        SecurityStamp = SecurityStamp.Create();
    }

    protected void IncrementVersion()
    {
        Version++;
    }
}