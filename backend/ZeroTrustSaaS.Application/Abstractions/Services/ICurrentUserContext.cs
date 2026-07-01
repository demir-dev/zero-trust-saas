namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface ICurrentUserContext
{
    Guid UserId { get; }

    Guid? TenantId { get; }

    IEnumerable<string> PlatformRoles { get; }

    bool IsPlatformUser { get; }

    IEnumerable<string> Permissions { get; }

    string? TenantRole { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}
