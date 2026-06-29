using System.Net;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security;

public sealed class IpAddress : ValueObject
{
    public string Value { get; }

    public bool IsLoopback =>
        IPAddress.IsLoopback(IPAddress.Parse(Value));

    private IpAddress(string value)
    {
        Value = value;
    }

    public static Result<IpAddress> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<IpAddress>.Failure(
                Error.Validation(
                    "Security.IpAddress.Empty",
                    "IP address is required."));
        }

        value = value.Trim();

        if (!IPAddress.TryParse(value, out _))
        {
            return Result<IpAddress>.Failure(
                Error.Validation(
                    "Security.IpAddress.Invalid",
                    "The IP address is invalid."));
        }

        return Result<IpAddress>.Success(
            new IpAddress(value));
    }

    public static IpAddress From(string value)
    {
        return new(value.Trim());
    }

    public static IpAddress Empty()
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

    public static implicit operator string(IpAddress address)
    {
        return address.Value;
    }
}