using System.Text.RegularExpressions;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization;

public sealed class PermissionCode : ValueObject
{
    private static readonly Regex ValidCodePattern =
        new(@"^[a-z]+(\.[a-z]+)+$", RegexOptions.Compiled);

    public string Value { get; }

    private PermissionCode(string value)
    {
        Value = value;
    }

    public static Result<PermissionCode> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PermissionCode>.Failure(PermissionErrors.CodeRequired);

        var normalized = value.Trim().ToLowerInvariant();

        if (!ValidCodePattern.IsMatch(normalized))
            return Result<PermissionCode>.Failure(PermissionErrors.InvalidCodeFormat);

        return Result<PermissionCode>.Success(new PermissionCode(normalized));
    }

    public static PermissionCode From(string value)
    {
        return new(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PermissionCode code) => code.Value;
}
