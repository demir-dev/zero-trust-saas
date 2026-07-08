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

namespace ZeroTrustSaaS.Application.Features.Identity.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ITenantMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    ITokenIssuanceService tokenIssuanceService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = dateTimeProvider.UtcNow;

        bool isTenantLogin = !string.IsNullOrWhiteSpace(command.TenantSlug);
        Guid? tenantId = null;

        if (isTenantLogin)
        {
            var normalizedSlug = command.TenantSlug!.Trim().ToLowerInvariant();
            var tenant = await tenantRepository.GetBySlugAsync(normalizedSlug, cancellationToken);

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
                await LogAuditEvent(
                    SecurityEventType.LoginFailed,
                    AuditSeverity.Warning,
                    now, user.Id, tenantId,
                    failedAuditIp.IsSuccess ? failedAuditIp.Value : null,
                    command.UserAgent,
                    auditLogRepository, cancellationToken);

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

        if (isTenantLogin)
        {
            var membership = await membershipRepository.GetAsync(tenantId!.Value, user.Id, cancellationToken);
            if (membership is null || !membership.IsActive)
                return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);
        }

        // Device resolution: update existing or register new.
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

        if (!isTenantLogin)
        {
            var platformRolesCheck = await roleRepository.GetUserRolesAsync(user.Id, null, cancellationToken);
            bool hasPlatformRole = platformRolesCheck.Any(ur => ur.IsActive);

            if (!hasPlatformRole)
            {
                return Result<LoginResponse>.Success(new LoginResponse(
                    AccessToken: null,
                    RefreshToken: null,
                    Result: LoginResult.TenantSelectionRequired,
                    RequiresMfa: false,
                    UserId: user.Id,
                    IsPlatformUser: false));
            }
        }

        var clientInfoForIssuance = BuildClientInfo(command);
        var issuanceResult = await tokenIssuanceService.IssueAsync(
            user, tenantId, deviceId, clientInfoForIssuance, command.UserAgent, now, cancellationToken);

        if (issuanceResult.IsFailure)
            return Result<LoginResponse>.Failure(issuanceResult.Error);

        var successAttemptResult = LoginAttempt.Create(
            user.Id,
            clientInfoForIssuance,
            LoginResult.Success,
            RiskLevel.Low,
            now);

        if (successAttemptResult.IsSuccess)
            user.RecordSuccessfulLogin(successAttemptResult.Value, now);

        userRepository.Update(user);

        var auditIpResult = IpAddress.Create(command.IpAddress);
        await LogAuditEvent(
            SecurityEventType.LoginSucceeded,
            AuditSeverity.Info,
            now, user.Id, tenantId,
            auditIpResult.IsSuccess ? auditIpResult.Value : null,
            command.UserAgent,
            auditLogRepository, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            AccessToken: issuanceResult.Value.AccessToken,
            RefreshToken: issuanceResult.Value.RawRefreshToken,
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

    private static async Task LogAuditEvent(
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
        var logResult = AuditLog.Create(eventType, severity, occurredAt,
            userId: userId, tenantId: tenantId, ipAddress: ip, userAgent: userAgent);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);
    }
}
