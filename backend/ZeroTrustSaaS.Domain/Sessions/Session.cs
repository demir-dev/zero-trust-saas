using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Sessions.Errors;

namespace ZeroTrustSaaS.Domain.Sessions;

public sealed class Session : AggregateRoot
{
    private static readonly TimeSpan DefaultSessionLifetime = TimeSpan.FromDays(30);

    private Session()
    {
    }

    private Session(
        Guid id,
        Guid userId,
        Guid? tenantId,
        Guid? trustedDeviceId,
        string? ipAddress,
        string? userAgent,
        string? browser,
        string? operatingSystem,
        string? country,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TenantId = tenantId;
        TrustedDeviceId = trustedDeviceId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Browser = browser;
        OperatingSystem = operatingSystem;
        Country = country;
        CreatedAtUtc = createdAtUtc;
        LastSeenAtUtc = createdAtUtc;
        LastActivityUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = SessionStatus.Active;
        RevokedReason = SessionRevocationReason.None;
    }

    public Guid UserId { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid? TrustedDeviceId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime LastSeenAtUtc { get; private set; }

    public DateTime LastActivityUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public SessionRevocationReason RevokedReason { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? Browser { get; private set; }

    public string? OperatingSystem { get; private set; }

    public string? Country { get; private set; }

    public string? City { get; private set; }

    public SessionStatus Status { get; private set; }

    public bool IsActive => Status == SessionStatus.Active && ExpiresAtUtc > DateTime.UtcNow;

    public bool IsRevoked => Status == SessionStatus.Revoked;

    public bool IsExpired => ExpiresAtUtc <= DateTime.UtcNow;

    public static Result<Session> Create(
        Guid userId,
        Guid? tenantId,
        Guid? trustedDeviceId,
        string? ipAddress,
        string? userAgent,
        string? browser,
        string? operatingSystem,
        string? country,
        DateTime createdAtUtc,
        DateTime? expiresAtUtc = null)
    {
        var session = new Session(
            Guid.NewGuid(),
            userId,
            tenantId,
            trustedDeviceId,
            ipAddress,
            userAgent,
            browser,
            operatingSystem,
            country,
            createdAtUtc,
            expiresAtUtc ?? createdAtUtc.Add(DefaultSessionLifetime));

        return Result<Session>.Success(session);
    }

    public Result Revoke(DateTime revokedAtUtc, SessionRevocationReason reason)
    {
        if (IsRevoked)
            return Result.Failure(SessionErrors.AlreadyRevoked);

        Status = SessionStatus.Revoked;
        RevokedAtUtc = revokedAtUtc;
        RevokedReason = reason;

        return Result.Success();
    }

    public Result UpdateActivity(
        DateTime now,
        string? ipAddress,
        string? browser,
        string? operatingSystem,
        string? country)
    {
        LastSeenAtUtc = now;
        LastActivityUtc = now;

        if (!string.IsNullOrEmpty(ipAddress))
            IpAddress = ipAddress;

        if (!string.IsNullOrEmpty(browser))
            Browser = browser;

        if (!string.IsNullOrEmpty(operatingSystem))
            OperatingSystem = operatingSystem;

        if (!string.IsNullOrEmpty(country))
            Country = country;

        return Result.Success();
    }
}
