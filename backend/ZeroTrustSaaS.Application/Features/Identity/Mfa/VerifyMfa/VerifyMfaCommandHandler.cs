using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Identity.Login;
using ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyAndEnableMfa;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Enums;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Security.Enums;
using DomainRefreshToken = ZeroTrustSaaS.Domain.Identity.RefreshToken;

namespace ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyMfa;

public sealed class VerifyMfaCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ITenantMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IMfaCodeValidator mfaCodeValidator,
    ITokenGenerator tokenGenerator,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

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
            var auditIpResult = IpAddress.Create(command.IpAddress);
            var logResult = AuditLog.Create(
                SecurityEventType.MfaFailed,
                AuditSeverity.Warning,
                dateTimeProvider.UtcNow,
                userId: user.Id,
                tenantId: tenantId,
                ipAddress: auditIpResult.IsSuccess ? auditIpResult.Value : null,
                userAgent: command.UserAgent);

            if (logResult.IsSuccess)
                await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<LoginResponse>.Failure(UserErrors.InvalidMfaCode);
        }

        return await IssueTokensAsync(user, command, tenantId, cancellationToken);
    }

    private async Task<Result<LoginResponse>> IssueTokensAsync(
        User user,
        VerifyMfaCommand command,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        string rawRefreshToken = tokenGenerator.GenerateRefreshTokenValue();
        string hashedToken = tokenGenerator.HashRefreshToken(rawRefreshToken);

        var tokenHashResult = RefreshTokenHash.Create(hashedToken);
        if (tokenHashResult.IsFailure)
            return Result<LoginResponse>.Failure(tokenHashResult.Error);

        var fp = DeviceFingerprint.Create(command.DeviceFingerprint);
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

        var refreshTokenResult = DomainRefreshToken.Create(
            user.Id,
            tokenHashResult.Value,
            clientInfo,
            now,
            now.Add(RefreshTokenLifetime),
            tenantId);

        if (refreshTokenResult.IsFailure)
            return Result<LoginResponse>.Failure(refreshTokenResult.Error);

        var issueResult = user.IssueRefreshToken(refreshTokenResult.Value, now);
        if (issueResult.IsFailure)
            return Result<LoginResponse>.Failure(issueResult.Error);

        var attemptResult = LoginAttempt.Create(
            user.Id,
            clientInfo,
            LoginResult.Success,
            RiskLevel.Low,
            now);

        if (attemptResult.IsSuccess)
            user.RecordSuccessfulLogin(attemptResult.Value, now);

        string accessToken;

        if (tenantId is null)
        {
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
                permissions: []);
        }
        else
        {
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
                permissions: permissionCodes);
        }

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
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            Result: LoginResult.Success,
            RequiresMfa: false,
            UserId: user.Id));
    }
}
