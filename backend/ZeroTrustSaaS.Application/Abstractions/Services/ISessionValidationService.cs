using System.Security.Claims;

namespace ZeroTrustSaaS.Application.Abstractions.Services;

/// <summary>
/// Validates every authenticated request against the Session aggregate.
/// Called from OnTokenValidated; all auth-pipeline business logic lives here,
/// not in middleware.
/// </summary>
public interface ISessionValidationService
{
    /// <summary>
    /// Validates the JWT principal against security stamp, tenant, session, and device state.
    /// Also updates Session.LastSeenAtUtc (debounced — at most once per minute per session).
    /// </summary>
    Task<SessionValidationResult> ValidateAsync(
        ClaimsPrincipal principal,
        CancellationToken ct = default);
}
