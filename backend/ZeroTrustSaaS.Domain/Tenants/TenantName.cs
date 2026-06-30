using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Domain.Tenants;

public sealed class TenantName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; }

    private TenantName(string value)
    {
        Value = value;
    }

    public static Result<TenantName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<TenantName>.Failure(TenantErrors.NameRequired);

        if (value.Length > MaxLength)
            return Result<TenantName>.Failure(TenantErrors.NameTooLong);

        return Result<TenantName>.Success(new TenantName(value.Trim()));
    }

    public static TenantName From(string value)
    {
        return new(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(TenantName name) => name.Value;
}
