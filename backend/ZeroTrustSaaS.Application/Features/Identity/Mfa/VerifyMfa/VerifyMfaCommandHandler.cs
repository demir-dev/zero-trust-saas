using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Identity.Login;
using ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyAndEnableMfa;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Devices.Errors;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Enums;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyMfa;

public sealed class VerifyMfaCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ITenantMembershipRepository membershipRepository,
    ITrustedDeviceRepository trustedDeviceRepository,
    IAuditLogRepository auditLogRepository,
    IMfaCodeValidator mfaCodeValidator,
    ITokenIssuanceService tokenIssuanceService,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<LoginResponse>> Handle(
        VerifyMfaCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result<LoginResponse>.Failure(UserErrors.NotFound);

        if (!user.IsMfaEnabled)
            return Result<LoginResponse>.Failure(UserErrors.MfaNotEnabled);

        if (!user.CanAuthenticate)
        {
            return Result<LoginResponse>.Failure(
                user.IsLocked ? UserErrors.UserIsLocked : UserErrors.UserIsSuspended);
        }

        bool isTenantLogin = !string.IsNullOrWhiteSpace(command.TenantSlug);
        Guid? tenantId = null;

        if (isTenantLogin)
        {
            var normalizedSlug = command.TenantSlug!.Trim().ToLowerInvariant();
            var tenant = await tenantRepository.GetBySlugAsync(normalizedSlug, cancellationToken);

            if (tenant is null || !tenant.IsActive)
                return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);

            tenantId = tenant.Id;

            var membership = await membershipRepository.GetAsync(tenantId.Value, user.Id, cancellationToken);
            if (membership is null || !membership.IsActive)
                return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);
        }

        bool isCodeValid;

        if (command.IsRecoveryCode)
        {
            var hash = VerifyAndEnableMfaCommandHandler.HashRecoveryCode(command.Code.Trim().ToUpperInvariant());
            isCodeValid = user.ConsumeRecoveryCode(hash);
        }
        else
        {
            isCodeValid = mfaCodeValidator.Validate(
                user.MfaSecret!.Value,
                command.Code,
                MfaMethod.Totp);
        }

        if (!isCodeValid)
        {
            var now2 = dateTimeProvider.UtcNow;
            var auditIpResult = IpAddress.Create(command.IpAddress);
            var logResult = AuditLog.Create(
                SecurityEventType.MfaFailed,
                AuditSeverity.Warning,
                now2,
                userId: user.Id,
                tenantId: tenantId,
                ipAddress: auditIpResult.IsSuccess ? auditIpResult.Value : null,
                userAgent: command.UserAgent);

            if (logResult.IsSuccess)
                await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LoginResponse>.Failure(UserErrors.InvalidMfaCode);
        }

        var now = dateTimeProvider.UtcNow;

        // Resolve device: trust it if requested.
        Guid? deviceId = null;
        var fp = DeviceFingerprint.Create(command.DeviceFingerprint);
        if (fp.IsSuccess)
        {
            var device = await trustedDeviceRepository
                .GetByFingerprintAsync(user.Id, fp.Value.Value, cancellationToken);

            if (device is not null)
            {
                if (device.IsBlocked)
                    return Result<LoginResponse>.Failure(TrustedDeviceErrors.DeviceBlocked);

                if (!device.IsRevoked)
                {
                    if (command.TrustDevice && device.IsPending)
                    {
                        device.Trust(now);
                        trustedDeviceRepository.Update(device);
                    }
                    deviceId = device.Id;
                }
            }
        }

        var ip = IpAddress.Create(command.IpAddress);
        var clientInfoResult = ClientInfo.Create(
            fp.IsSuccess ? fp.Value : DeviceFingerprint.From("unknown"),
            ip.IsSuccess ? ip.Value : IpAddress.Empty(),
            command.Country,
            command.Browser,
            command.OperatingSystem);

        var clientInfo = clientInfoResult.IsSuccess
            ? clientInfoResult.Value
            : ClientInfo.From(DeviceFingerprint.From("unknown"), IpAddress.Empty(),
                command.Country, command.Browser, command.OperatingSystem);

        var issuanceResult = await tokenIssuanceService.IssueAsync(
            user, tenantId, deviceId, clientInfo, command.UserAgent, now, cancellationToken);

        if (issuanceResult.IsFailure)
            return Result<LoginResponse>.Failure(issuanceResult.Error);

        var attemptResult = LoginAttempt.Create(
            user.Id, clientInfo, LoginResult.Success, RiskLevel.Low, now);

        if (attemptResult.IsSuccess)
            user.RecordSuccessfulLogin(attemptResult.Value, now);

        userRepository.Update(user);

        var auditIp = IpAddress.Create(command.IpAddress);
        var successLog = AuditLog.Create(
            SecurityEventType.MfaSucceeded,
            AuditSeverity.Info,
            now,
            userId: user.Id,
            tenantId: tenantId,
            ipAddress: auditIp.IsSuccess ? auditIp.Value : null,
            userAgent: command.UserAgent);

        if (successLog.IsSuccess)
            await auditLogRepository.AddAsync(successLog.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            AccessToken: issuanceResult.Value.AccessToken,
            RefreshToken: issuanceResult.Value.RawRefreshToken,
            Result: LoginResult.Success,
            RequiresMfa: false,
            UserId: user.Id));
    }
}
