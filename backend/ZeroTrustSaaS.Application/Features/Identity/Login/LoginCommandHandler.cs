using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Devices.Errors;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Enums;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Security.Enums;
using DomainRefreshToken = ZeroTrustSaaS.Domain.Identity.RefreshToken;

namespace ZeroTrustSaaS.Application.Features.Identity.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ITenantMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow;

        // Platform login when no slug is provided; tenant login otherwise.
        bool isTenantLogin = !string.IsNullOrWhiteSpace(command.TenantSlug);

        Guid? tenantId = null;

        if (isTenantLogin)
        {
            var normalizedSlug = command.TenantSlug!.Trim().ToLowerInvariant();
            var tenant = await tenantRepository.GetBySlugAsync(normalizedSlug, cancellationToken);

            // Generic error — do not reveal whether slug exists (prevents tenant enumeration).
            if (tenant is null || !tenant.IsActive)
                return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);

            tenantId = tenant.Id;
        }

        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash.Value))
        {
            if (user is not null)
            {
                var failedAttemptResult = LoginAttempt.Create(
                    user.Id,
                    BuildClientInfo(command),
                    LoginResult.InvalidCredentials,
                    RiskLevel.Medium,
                    now);

                if (failedAttemptResult.IsSuccess)
                    user.RecordFailedLogin(failedAttemptResult.Value, now);

                var failedAuditIp = IpAddress.Create(command.IpAddress);
                await LogSecurityEvent(
                    SecurityEventType.LoginFailed,
                    AuditSeverity.Warning,
                    now,
                    user.Id,
                    tenantId,
                    failedAuditIp.IsSuccess ? failedAuditIp.Value : null,
                    command.UserAgent,
                    auditLogRepository,
                    cancellationToken);

                userRepository.Update(user);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);
        }

        if (!user.CanAuthenticate)
        {
            return Result<LoginResponse>.Failure(
                user.IsLocked ? UserErrors.UserIsLocked : UserErrors.UserIsSuspended);
        }

        // For tenant login, validate an active membership exists.
        if (isTenantLogin)
        {
            var membership = await membershipRepository.GetAsync(tenantId!.Value, user.Id, cancellationToken);
            if (membership is null || !membership.IsActive)
                return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);
        }

        // Device tracking: update existing device or auto-register a new one.
        // Returns the device Id to carry into the JWT and refresh token.
        Guid? deviceId = null;
        var fp = DeviceFingerprint.Create(command.DeviceFingerprint);
        if (fp.IsSuccess)
        {
            var existing = await trustedDeviceRepository
                .GetByFingerprintAsync(user.Id, fp.Value.Value, cancellationToken);

            if (existing is not null && !existing.IsRevoked)
            {
                if (existing.IsBlocked)
                    return Result<LoginResponse>.Failure(TrustedDeviceErrors.DeviceBlocked);

                existing.RecordLogin(now);
                if (command.TrustDevice && existing.IsPending)
                    existing.Trust(now);
                trustedDeviceRepository.Update(existing);
                deviceId = existing.Id;
            }
            else
            {
                var clientInfo = BuildClientInfo(command);
                var nameResult = DeviceName.Create($"{command.Browser} on {command.OperatingSystem}");
                if (nameResult.IsSuccess)
                {
                    var newDeviceResult = TrustedDevice.Register(user.Id, nameResult.Value, clientInfo);
                    if (newDeviceResult.IsSuccess)
                    {
                        var newDevice = newDeviceResult.Value;
                        if (command.TrustDevice)
                            newDevice.Trust(now);
                        await trustedDeviceRepository.AddAsync(newDevice, cancellationToken);
                        deviceId = newDevice.Id;
                    }
                }
            }
        }

        if (user.IsMfaEnabled)
        {
            var mfaAttemptResult = LoginAttempt.Create(
                user.Id,
                BuildClientInfo(command),
                LoginResult.MfaRequired,
                RiskLevel.Low,
                now);

            if (mfaAttemptResult.IsSuccess)
                user.RecordSuccessfulLogin(mfaAttemptResult.Value, now);

            userRepository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var platformRoles = await roleRepository.GetUserRolesAsync(user.Id, null, cancellationToken);
            bool isPlatformUser = platformRoles.Any(ur => ur.IsActive);

            return Result<LoginResponse>.Success(new LoginResponse(
                AccessToken: null,
                RefreshToken: null,
                Result: LoginResult.MfaRequired,
                RequiresMfa: true,
                UserId: user.Id,
                IsPlatformUser: isPlatformUser));
        }

        return await IssueTokensAsync(user, command, tenantId, deviceId, now, cancellationToken);
    }

    private async Task<Result<LoginResponse>> IssueTokensAsync(
        User user,
        LoginCommand command,
        Guid? tenantId,
        Guid? deviceId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        string rawRefreshToken = tokenGenerator.GenerateRefreshTokenValue();
        string hashedToken = tokenGenerator.HashRefreshToken(rawRefreshToken);

        var tokenHashResult = RefreshTokenHash.Create(hashedToken);
        if (tokenHashResult.IsFailure)
            return Result<LoginResponse>.Failure(tokenHashResult.Error);

        var tokenClientInfo = BuildClientInfo(command);
        var refreshTokenResult = DomainRefreshToken.Create(
            user.Id,
            tokenHashResult.Value,
            tokenClientInfo,
            now,
            now.Add(RefreshTokenLifetime),
            tenantId,
            trustedDeviceId: deviceId);

        if (refreshTokenResult.IsFailure)
            return Result<LoginResponse>.Failure(refreshTokenResult.Error);

        var issueResult = user.IssueRefreshToken(refreshTokenResult.Value, now);
        if (issueResult.IsFailure)
            return Result<LoginResponse>.Failure(issueResult.Error);

        var attemptClientInfo = BuildClientInfo(command);
        var successAttemptResult = LoginAttempt.Create(
            user.Id,
            attemptClientInfo,
            LoginResult.Success,
            RiskLevel.Low,
            now);

        if (successAttemptResult.IsSuccess)
            user.RecordSuccessfulLogin(successAttemptResult.Value, now);

        var sessionId = refreshTokenResult.Value.Id;

        // Build JWT claims based on login context.
        string accessToken;

        if (tenantId is null)
        {
            // Platform login: emit platform_role claims, no tenant context.
            var userRoles = await roleRepository.GetUserRolesAsync(user.Id, null, cancellationToken);
            var platformRoleNames = new List<string>();

            foreach (var ur in userRoles.Where(r => r.IsActive))
            {
                var role = await roleRepository.GetByIdAsync(ur.RoleId, cancellationToken);
                if (role is not null)
                    platformRoleNames.Add(role.Name.Value);
            }

            accessToken = tokenGenerator.GenerateJwtToken(
                user.Id,
                user.Email.Value,
                user.SecurityStamp.Value.ToString(),
                platformRoleNames,
                tenantId: null,
                tenantRole: null,
                permissions: [],
                sessionId: sessionId,
                deviceId: deviceId);
        }
        else
        {
            // Tenant login: emit tenant_id, tenant_role, permission claims.
            var userRoles = await roleRepository.GetUserRolesAsync(user.Id, tenantId, cancellationToken);
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
                user.Id,
                user.Email.Value,
                user.SecurityStamp.Value.ToString(),
                platformRoles: [],
                tenantId: tenantId,
                tenantRole: tenantRoleName,
                permissions: permissionCodes,
                sessionId: sessionId,
                deviceId: deviceId);
        }

        userRepository.Update(user);

        var auditIpResult = IpAddress.Create(command.IpAddress);
        await LogSecurityEvent(
            SecurityEventType.LoginSucceeded,
            AuditSeverity.Info,
            now,
            user.Id,
            tenantId,
            auditIpResult.IsSuccess ? auditIpResult.Value : null,
            command.UserAgent,
            auditLogRepository,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            Result: LoginResult.Success,
            RequiresMfa: false,
            UserId: user.Id));
    }

    private static ClientInfo BuildClientInfo(LoginCommand command)
    {
        var fp = DeviceFingerprint.Create(command.DeviceFingerprint);
        var ip = IpAddress.Create(command.IpAddress);
        var result = ClientInfo.Create(
            fp.IsSuccess ? fp.Value : DeviceFingerprint.From("unknown"),
            ip.IsSuccess ? ip.Value : IpAddress.Empty(),
            command.Country,
            command.Browser,
            command.OperatingSystem);
        return result.IsSuccess
            ? result.Value
            : ClientInfo.From(DeviceFingerprint.From("unknown"), IpAddress.Empty(),
                command.Country, command.Browser, command.OperatingSystem);
    }

    private static async Task LogSecurityEvent(
        SecurityEventType eventType,
        AuditSeverity severity,
        DateTime occurredAt,
        Guid userId,
        Guid? tenantId,
        IpAddress? ip,
        string? userAgent,
        IAuditLogRepository auditLogRepository,
        CancellationToken cancellationToken)
    {
        var logResult = AuditLog.Create(
            eventType,
            severity,
            occurredAt,
            userId: userId,
            tenantId: tenantId,
            ipAddress: ip,
            userAgent: userAgent);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);
    }
}
