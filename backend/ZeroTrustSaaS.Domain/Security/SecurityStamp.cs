using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security;

public sealed class SecurityStamp : ValueObject
{
    public Guid Value { get; }

    private SecurityStamp(Guid value)
    {
        Value = value;
    }

    public static SecurityStamp Create()
    {
        return new(Guid.NewGuid());
    }

    public static SecurityStamp From(Guid value)
    {
        return new(value);
    }

    public static SecurityStamp Empty()
    {
        return new(Guid.Empty);
    }

    public override string ToString()
    {
        return Value.ToString();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator Guid(SecurityStamp stamp)
    {
        return stamp.Value;
    }
}
