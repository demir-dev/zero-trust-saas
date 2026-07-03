using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Devices;

namespace ZeroTrustSaaS.Infrastructure.Services;

/// <summary>
/// Validates every authenticated request against the full session security model.
/// Order of checks mirrors the JWT claims trust hierarchy:
///   1. Required claims present
///   2. Security stamp (covers: password change, suspend, lock, disable, email change)
///   3. Tenant status (tenant-context requests only)
///   4. Session status — authoritative check against the Session aggregate
///   5. Device status
///   6. Session activity heartbeat (debounced write, max once per minute per session)
/// </summary>
internal sealed class SessionValidationService(
    ISecurityStampCache stampCache,
    ITenantStatusCache tenantStatusCache,
    ISessionStatusCache sessionStatusCache,
    IDeviceStatusCache deviceStatusCache,
    ISessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache cache,
    IDateTimeProvider dateTimeProvider,
    ILogger<SessionValidationService> logger)
    : ISessionValidationService
{
    // Heartbeat writes at most once per minute per session so LastSeenAtUtc reflects
    // real activity without a DB write on every single request.
    private static readonly TimeSpan ActivityDebounce = TimeSpan.FromSeconds(60);
    private static string ActivityKey(Guid sid) => $"sess-hb:{sid}";

    public async Task<SessionValidationResult> ValidateAsync(
        ClaimsPrincipal principal,
        CancellationToken ct = default)
    {
        // ── 1. Required claims ──────────────────────────────────────────────────
        var userIdStr  = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var stampClaim = principal.FindFirstValue("security_stamp");

        if (userIdStr is null || stampClaim is null
            || !Guid.TryParse(userIdStr, out var userId))
        {
            logger.LogWarning("[SessionValidation] MissingClaims");
            return SessionValidationResult.Fail(
                SessionValidationStatus.MissingClaims,
                "Required JWT claims (sub, security_stamp) are missing.");
        }

        // ── 2. Security stamp ───────────────────────────────────────────────────
        var currentStamp = await stampCache.GetOrFetchStampAsync(userId, ct);
        if (currentStamp is null || currentStamp != stampClaim)
        {
            logger.LogWarning(
                "[SessionValidation] StampMismatch user={UserId} jwt={JwtStamp} db={DbStamp}",
                userId, stampClaim, currentStamp ?? "(null)");
            return SessionValidationResult.Fail(
                SessionValidationStatus.StampMismatch,
                "Security stamp mismatch — token is no longer valid.");
        }

        // ── 3. Tenant status ────────────────────────────────────────────────────
        var tenantIdStr = principal.FindFirstValue("tenant_id");
        if (tenantIdStr is not null && Guid.TryParse(tenantIdStr, out var tenantId))
        {
            if (!await tenantStatusCache.IsActiveAsync(tenantId, ct))
            {
                logger.LogWarning("[SessionValidation] TenantSuspended tenant={TenantId}", tenantId);
                return SessionValidationResult.Fail(
                    SessionValidationStatus.TenantSuspended,
                    "The tenant account is suspended.");
            }
        }

        // ── 4. Session status ───────────────────────────────────────────────────
        // ISessionStatusCache now checks Session.IsActive (Status == Active && not expired),
        // not RefreshToken.IsActive. Revocation is effective immediately.
        Guid? sessionId = null;
        var sessionIdStr = principal.FindFirstValue("session_id");
        if (sessionIdStr is not null && Guid.TryParse(sessionIdStr, out var sid))
        {
            sessionId = sid;
            bool sessionActive = await sessionStatusCache.IsActiveAsync(sid, ct);
            if (!sessionActive)
            {
                logger.LogWarning("[SessionValidation] SessionRevoked session={SessionId}", sid);
                return SessionValidationResult.Fail(
                    SessionValidationStatus.SessionRevoked,
                    "The session has been revoked or has expired.");
            }
        }

        // ── 5. Device status ────────────────────────────────────────────────────
        var deviceIdStr = principal.FindFirstValue("device_id");
        if (deviceIdStr is not null && Guid.TryParse(deviceIdStr, out var deviceId))
        {
            var deviceStatus = await deviceStatusCache.GetStatusAsync(deviceId, ct);
            if (deviceStatus is DeviceStatus.Blocked or DeviceStatus.Revoked)
            {
                logger.LogWarning(
                    "[SessionValidation] DeviceBlocked device={DeviceId} status={Status}",
                    deviceId, deviceStatus);
                return SessionValidationResult.Fail(
                    SessionValidationStatus.DeviceBlocked,
                    "The device associated with this session is blocked or revoked.");
            }
        }

        // ── 6. Session activity heartbeat ───────────────────────────────────────
        if (sessionId.HasValue)
            await TryRecordActivityAsync(sessionId.Value, ct);

        logger.LogDebug(
            "[SessionValidation] Valid user={UserId} session={SessionId}",
            userId, sessionId?.ToString() ?? "(none)");

        return SessionValidationResult.Ok();
    }

    private async Task TryRecordActivityAsync(Guid sid, CancellationToken ct)
    {
        var key = ActivityKey(sid);
        if (cache.TryGetValue(key, out _))
            return; // debounced — updated less than a minute ago

        try
        {
            var session = await sessionRepository.GetByIdAsync(sid, ct);
            if (session is not null && session.IsActive)
            {
                var now = dateTimeProvider.UtcNow;
                // Pass null for all client-info fields — we only touch the timestamps here.
                // Full client-info refresh happens during token rotation in RefreshTokenCommandHandler.
                session.UpdateActivity(now, ipAddress: null, browser: null, operatingSystem: null, country: null);
                sessionRepository.Update(session);
                await unitOfWork.SaveChangesAsync(ct);
            }

            cache.Set(key, true, ActivityDebounce);
        }
        catch (Exception ex)
        {
            // Activity recording is best-effort; never fail auth because of it.
            logger.LogError(ex,
                "[SessionValidation] Failed to record activity for session={SessionId}", sid);
        }
    }
}
