namespace ZeroTrustSaaS.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public bool HasIdentity()
    {
        return Id != Guid.Empty;
    }

    protected void SetId(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Id == ((Entity)obj).Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(
        Entity? left,
        Entity? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(
        Entity? left,
        Entity? right)
    {
        return !Equals(left, right);
    }
}