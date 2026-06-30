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
using ZeroTrustSaaS.Domain.Security.Enums;
using DomainRefreshToken = ZeroTrustSaaS.Domain.Identity.RefreshToken;

namespace ZeroTrustSaaS.Application.Features.Identity.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
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
        // Slug lookup — generic error on failure to prevent tenant enumeration
        var normalizedSlug = command.TenantSlug.Trim().ToLowerInvariant();
        var tenant = await tenantRepository.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (tenant is null || !tenant.IsActive)
            return Result<LoginResponse>.Failure(UserErrors.InvalidCredentials);

        var user = await userRepository.GetByEmailAsync(
            command.Email,
            tenant.Id,
            cancellationToken);

        var now = dateTimeProvider.UtcNow;

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
                    user.TenantId,
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
            var loginResult = user.IsLocked
                ? LoginResult.Locked
                : LoginResult.Suspended;

            return Result<LoginResponse>.Failure(
                user.IsLocked ? UserErrors.UserIsLocked : UserErrors.UserIsSuspended);
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

            return Result<LoginResponse>.Success(new LoginResponse(
                AccessToken: null,
                RefreshToken: null,
                Result: LoginResult.MfaRequired,
                RequiresMfa: true,
                UserId: user.Id));
        }

        return await IssueTokensAsync(user, command, now, auditLogRepository, cancellationToken);
    }

    private async Task<Result<LoginResponse>> IssueTokensAsync(
        User user,
        LoginCommand command,
        DateTime now,
        IAuditLogRepository auditLogRepository,
        CancellationToken cancellationToken)
    {
        string rawRefreshToken = tokenGenerator.GenerateRefreshTokenValue();
        string hashedToken = tokenGenerator.HashRefreshToken(rawRefreshToken);

        var tokenHashResult = RefreshTokenHash.Create(hashedToken);
        if (tokenHashResult.IsFailure)
            return Result<LoginResponse>.Failure(tokenHashResult.Error);

        // Build a dedicated ClientInfo for the RefreshToken — must not share the
        // same CLR instance with LoginAttempt; EF tracks owned entities by owner path.
        var tokenClientInfo = BuildClientInfo(command);

        var refreshTokenResult = DomainRefreshToken.Create(
            user.Id,
            tokenHashResult.Value,
            tokenClientInfo,
            now,
            now.Add(RefreshTokenLifetime));

        if (refreshTokenResult.IsFailure)
            return Result<LoginResponse>.Failure(refreshTokenResult.Error);

        var issueResult = user.IssueRefreshToken(refreshTokenResult.Value, now);
        if (issueResult.IsFailure)
            return Result<LoginResponse>.Failure(issueResult.Error);

        // Build a separate ClientInfo instance for the LoginAttempt aggregate.
        var attemptClientInfo = BuildClientInfo(command);

        var successAttemptResult = LoginAttempt.Create(
            user.Id,
            attemptClientInfo,
            LoginResult.Success,
            RiskLevel.Low,
            now);

        if (successAttemptResult.IsSuccess)
            user.RecordSuccessfulLogin(successAttemptResult.Value, now);

        string accessToken = tokenGenerator.GenerateJwtToken(
            user.Id,
            user.TenantId,
            []);

        userRepository.Update(user);

        // Build a fresh IpAddress for AuditLog — must not share with ClientInfo instances.
        var auditIpResult = IpAddress.Create(command.IpAddress);
        await LogSecurityEvent(
            SecurityEventType.LoginSucceeded,
            AuditSeverity.Info,
            now,
            user.Id,
            user.TenantId,
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
        Guid tenantId,
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
