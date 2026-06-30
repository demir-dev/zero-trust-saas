namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface ICurrentUserContext
{
    Guid UserId { get; }

    Guid TenantId { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }
}
