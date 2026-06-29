using System.Text.RegularExpressions;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Identity;

public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex =
        new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Email>.Failure(
                Error.Validation(
                    "Identity.Email.Empty",
                    "Email address is required."));
        }

        value = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(value))
        {
            return Result<Email>.Failure(
                Error.Validation(
                    "Identity.Email.Invalid",
                    "The email address format is invalid."));
        }

        return Result<Email>.Success(
            new Email(value));
    }

    public static Email From(string value)
    {
        return new(value.Trim().ToLowerInvariant());
    }

    public static Email Empty()
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

    public static implicit operator string(Email email)
    {
        return email.Value;
    }
}