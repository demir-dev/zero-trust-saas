namespace ZeroTrustSaaS.Application.Abstractions.Services;

public enum SessionValidationStatus
{
    Valid = 0,
    MissingClaims = 1,
    StampMismatch = 2,
    UserNotActive = 3,
    TenantSuspended = 4,
    SessionRevoked = 5,
    SessionExpired = 6,
    DeviceBlocked = 7,
}

public sealed record SessionValidationResult(
    SessionValidationStatus Status,
    string? FailureReason = null)
{
    public bool IsValid => Status == SessionValidationStatus.Valid;

    public static SessionValidationResult Ok() =>
        new(SessionValidationStatus.Valid);

    public static SessionValidationResult Fail(SessionValidationStatus status, string reason) =>
        new(status, reason);
}
