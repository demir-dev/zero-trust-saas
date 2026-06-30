using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security;

public sealed class UserAgent : ValueObject
{
    public const int MaxLength = 512;

    public string Value { get; }

    private UserAgent(string value)
    {
        Value = value;
    }

    public static Result<UserAgent> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<UserAgent>.Failure(
                Error.Validation(
                    "Security.UserAgent.Required",
                    "User agent is required."));
        }

        if (value.Length > MaxLength)
        {
            return Result<UserAgent>.Failure(
                Error.Validation(
                    "Security.UserAgent.TooLong",
                    $"User agent must not exceed {MaxLength} characters."));
        }

        return Result<UserAgent>.Success(new UserAgent(value.Trim()));
    }

    public static UserAgent From(string value)
    {
        return new(value);
    }

    public static UserAgent Unknown()
    {
        return new("Unknown");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(UserAgent userAgent) => userAgent.Value;
}
