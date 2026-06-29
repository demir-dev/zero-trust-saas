using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Identity;

public sealed class PasswordHash : ValueObject
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static Result<PasswordHash> Create(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return Result<PasswordHash>.Failure(
                Error.Validation(
                    "Identity.PasswordHash.Empty",
                    "Password hash is required."));
        }

        return Result<PasswordHash>.Success(
            new PasswordHash(hash.Trim()));
    }

    public static PasswordHash From(string value)
    {
        return new(value.Trim());
    }

    public static PasswordHash Empty()
    {
        return new(string.Empty);
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PasswordHash hash)
    {
        return hash.Value;
    }
}