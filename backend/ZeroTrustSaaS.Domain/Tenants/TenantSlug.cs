using System.Text.RegularExpressions;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Domain.Tenants;

public sealed class TenantSlug : ValueObject
{
    private static readonly Regex ValidSlugPattern =
        new(@"^[a-z0-9][a-z0-9-]{1,48}[a-z0-9]$", RegexOptions.Compiled);

    public string Value { get; }

    private TenantSlug(string value)
    {
        Value = value;
    }

    public static Result<TenantSlug> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<TenantSlug>.Failure(TenantErrors.SlugRequired);

        var normalized = value.Trim().ToLowerInvariant();

        if (!ValidSlugPattern.IsMatch(normalized))
            return Result<TenantSlug>.Failure(TenantErrors.InvalidSlug);

        return Result<TenantSlug>.Success(new TenantSlug(normalized));
    }

    public static TenantSlug From(string value)
    {
        return new(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(TenantSlug slug) => slug.Value;
}
