using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization;

public sealed class RoleName : ValueObject
{
    public const int MaxLength = 50;

    public string Value { get; }

    private RoleName(string value)
    {
        Value = value;
    }

    public static Result<RoleName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<RoleName>.Failure(RoleErrors.NameRequired);

        if (value.Length > MaxLength)
            return Result<RoleName>.Failure(RoleErrors.NameTooLong);

        return Result<RoleName>.Success(new RoleName(value.Trim()));
    }

    public static RoleName From(string value)
    {
        return new(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(RoleName name) => name.Value;
}
