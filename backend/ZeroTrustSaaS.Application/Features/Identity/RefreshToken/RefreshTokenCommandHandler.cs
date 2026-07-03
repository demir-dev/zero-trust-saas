using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Enums;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Sessions;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ISessionRepository sessionRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    ITokenGenerator tokenGenerator,
    ITenantStatusCache tenantStatusCache,
    ISessionStatusCache sessionStatusCache,
    IDeviceStatusCache deviceStatusCache,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        string hashedInput = tokenGenerator.HashRefreshToken(command.RefreshToken);
        var existingToken = await refreshTokenRepository.GetByHashAsync(hashedInput, cancellationToken);

        if (existingToken is null)
            return Result<RefreshTokenResponse>.Failure(UserErrors.RefreshTokenNotFound);

        var now = dateTimeProvider.UtcNow;

        if (!existingToken.IsActive)
        {
            if (existingToken.IsRevoked || existingToken.IsUsed)
            {
                // Presented a consumed/revoked token — replay attack.
                // Revoke the entire session and all its tokens.
                var session2 = await sessionRepository.GetByIdAsync(existingToken.SessionId, cancellationToken);
                if (session2 is not null && !session2.IsRevoked)
                {
                    session2.Revoke(now, SessionRevocationReason.ReplayAttack);
                    sessionRepository.Update(session2);
                }

                await refreshTokenRepository.RevokeAllBySessionIdAsync(existingToken.SessionId, now, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                sessionStatusCache.Invalidate(existingToken.SessionId);

                var error = existingToken.IsRevoked
                    ? UserErrors.RefreshTokenAlreadyRevoked
                    : RefreshTokenErrors.AlreadyUsed;
                return Result<RefreshTokenResponse>.Failure(error);
            }

            if (existingToken.IsExpired)
                return Result<RefreshTokenResponse>.Failure(UserErrors.RefreshTokenExpired);
        }

        // Load the session — this is now the stable identity that stays through rotations.
        var session = await sessionRepository.GetByIdAsync(existingToken.SessionId, cancellationToken);
        if (session is null || !session.IsActive)
            return Result<RefreshTokenResponse>.Failure(UserErrors.RefreshTokenNotFound);

        var activeUser = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (activeUser is null || !activeUser.CanAuthenticate)
            return Result<RefreshTokenResponse>.Failure(UserErrors.LoginNotAllowed);

        if (existingToken.TrustedDeviceId.HasValue)
        {
            var deviceStatus = await deviceStatusCache.GetStatusAsync(
                existingToken.TrustedDeviceId.Value, cancellationToken);
            if (deviceStatus is Domain.Devices.DeviceStatus.Blocked or Domain.Devices.DeviceStatus.Revoked)
                return Result<RefreshTokenResponse>.Failure(Domain.Devices.Errors.TrustedDeviceErrors.DeviceBlocked);
        }

        var tenantId = existingToken.TenantId;

        if (tenantId is not null &&
            !await tenantStatusCache.IsActiveAsync(tenantId.Value, cancellationToken))
            return Result<RefreshTokenResponse>.Failure(TenantErrors.TenantSuspended);

        var ipResult = IpAddress.Create(command.IpAddress);
        var fingerprintResult = DeviceFingerprint.Create(command.DeviceFingerprint);
        var clientInfoResult = ClientInfo.Create(
            fingerprintResult.IsSuccess ? fingerprintResult.Value : DeviceFingerprint.From("unknown"),
            ipResult.IsSuccess ? ipResult.Value : IpAddress.Empty(),
            command.Country,
            command.Browser,
            command.OperatingSystem);

        var clientInfo = clientInfoResult.IsSuccess
            ? clientInfoResult.Value
            : ClientInfo.From(
                DeviceFingerprint.From("unknown"),
                IpAddress.Empty(),
                command.Country,
                command.Browser,
                command.OperatingSystem);

        // Rotate the refresh token — session.Id remains the stable session identity.
        string newRawToken = tokenGenerator.GenerateRefreshTokenValue();
        string newHashedToken = tokenGenerator.HashRefreshToken(newRawToken);

        var newTokenHashResult = RefreshTokenHash.Create(newHashedToken);
        if (newTokenHashResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(newTokenHashResult.Error);

        var newRefreshTokenResult = Domain.Identity.RefreshToken.Create(
            activeUser.Id,
            session.Id,
            newTokenHashResult.Value,
            clientInfo,
            now,
            now.Add(RefreshTokenLifetime),
            tenantId,
            trustedDeviceId: existingToken.TrustedDeviceId);

        if (newRefreshTokenResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(newRefreshTokenResult.Error);

        var rotateResult = existingToken.Rotate(newRefreshTokenResult.Value.Id, now);
        if (rotateResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(rotateResult.Error);

        // Update session activity — same session persists through the rotation.
        session.UpdateActivity(
            now,
            ipResult.IsSuccess ? ipResult.Value.Value : null,
            command.Browser,
            command.OperatingSystem,
            command.Country);

        var deviceId = existingToken.TrustedDeviceId;

        // Re-build JWT claims — session_id = session.Id (stable, not the new RT id).
        string accessToken;

        if (tenantId is null)
        {
            var userRoles = await roleRepository.GetUserRolesAsync(activeUser.Id, null, cancellationToken);
            var platformRoleNames = new List<string>();

            foreach (var ur in userRoles.Where(r => r.IsActive))
            {
                var role = await roleRepository.GetByIdAsync(ur.RoleId, cancellationToken);
                if (role is not null)
                    platformRoleNames.Add(role.Name.Value);
            }

            accessToken = tokenGenerator.GenerateJwtToken(
                activeUser.Id,
                activeUser.Email.Value,
                activeUser.SecurityStamp.Value.ToString(),
                platformRoleNames,
                tenantId: null,
                tenantRole: null,
                permissions: [],
                sessionId: session.Id,
                deviceId: deviceId);
        }
        else
        {
            var userRoles = await roleRepository.GetUserRolesAsync(activeUser.Id, tenantId, cancellationToken);
            var activeUserRole = userRoles.FirstOrDefault(ur => ur.IsActive);

            string tenantRoleName = string.Empty;
            var permissionCodes = new List<string>();

            if (activeUserRole is not null)
            {
                var role = await roleRepository.GetByIdAsync(activeUserRole.RoleId, cancellationToken);
                if (role is not null)
                {
                    tenantRoleName = role.Name.Value;
                    permissionCodes.AddRange(role.Permissions.Select(p => p.Code.Value));
                }
            }

            accessToken = tokenGenerator.GenerateJwtToken(
                activeUser.Id,
                activeUser.Email.Value,
                activeUser.SecurityStamp.Value.ToString(),
                platformRoles: [],
                tenantId: tenantId,
                tenantRole: tenantRoleName,
                permissions: permissionCodes,
                sessionId: session.Id,
                deviceId: deviceId);
        }

        refreshTokenRepository.Update(existingToken);
        await refreshTokenRepository.AddAsync(newRefreshTokenResult.Value, cancellationToken);
        sessionRepository.Update(session);

        var logResult = AuditLog.Create(
            SecurityEventType.SessionRefreshed,
            AuditSeverity.Info,
            now,
            userId: activeUser.Id,
            tenantId: tenantId,
            ipAddress: ipResult.IsSuccess ? ipResult.Value : null,
            userAgent: command.UserAgent);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
            AccessToken: accessToken,
            RefreshToken: newRawToken));
    }
}
