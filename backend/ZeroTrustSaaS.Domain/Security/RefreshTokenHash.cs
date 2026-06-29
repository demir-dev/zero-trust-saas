using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security;

public sealed class RefreshTokenHash : ValueObject
{
    public string Value { get; }

    private RefreshTokenHash(string value)
    {
        Value = value;
    }

    public static Result<RefreshTokenHash> Create(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return Result<RefreshTokenHash>.Failure(
                Error.Validation(
                    "Security.RefreshTokenHash.Empty",
                    "Refresh token hash is required."));
        }

        return Result<RefreshTokenHash>.Success(
            new RefreshTokenHash(hash.Trim()));
    }

    public static RefreshTokenHash From(string value)
    {
        return new(value.Trim());
    }

    public static RefreshTokenHash Empty()
    {
        return new(string.Empty);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(RefreshTokenHash hash)
    {
        return hash.Value;
    }
}