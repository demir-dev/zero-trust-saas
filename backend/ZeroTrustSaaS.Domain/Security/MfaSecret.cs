using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security;

public sealed class MfaSecret : ValueObject
{
    public string Value { get; }

    private MfaSecret(string value)
    {
        Value = value;
    }

    public static Result<MfaSecret> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<MfaSecret>.Failure(
                Error.Validation(
                    "Security.MfaSecret.Empty",
                    "The MFA secret is required."));
        }

        return Result<MfaSecret>.Success(
            new MfaSecret(value.Trim()));
    }

    public static MfaSecret From(string value)
    {
        return new(value.Trim());
    }

    public static MfaSecret Empty()
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

    public static implicit operator string(MfaSecret secret)
    {
        return secret.Value;
    }
}