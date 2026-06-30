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
        var user = await userRepository.GetByEmailAsync(
            command.Email,
            command.TenantId,
            cancellationToken);

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

        var now = dateTimeProvider.UtcNow;

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash.Value))
        {
            if (user is not null)
            {
                var failedAttemptResult = LoginAttempt.Create(
                    user.Id,
                    clientInfo,
                    LoginResult.InvalidCredentials,
                    RiskLevel.Medium,
                    now);

                if (failedAttemptResult.IsSuccess)
                    user.RecordFailedLogin(failedAttemptResult.Value, now);

                await LogSecurityEvent(
                    SecurityEventType.LoginFailed,
                    AuditSeverity.Warning,
                    now,
                    user.Id,
                    user.TenantId,
                    ipResult.IsSuccess ? ipResult.Value : null,
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
                clientInfo,
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

        return await IssueTokensAsync(user, clientInfo, now, command.UserAgent, ipResult, auditLogRepository, cancellationToken);
    }

    private async Task<Result<LoginResponse>> IssueTokensAsync(
        User user,
        ClientInfo clientInfo,
        DateTime now,
        string userAgent,
        Result<IpAddress> ipResult,
        IAuditLogRepository auditLogRepository,
        CancellationToken cancellationToken)
    {
        string rawRefreshToken = tokenGenerator.GenerateRefreshTokenValue();
        string hashedToken = tokenGenerator.HashRefreshToken(rawRefreshToken);

        var tokenHashResult = RefreshTokenHash.Create(hashedToken);

        if (tokenHashResult.IsFailure)
            return Result<LoginResponse>.Failure(tokenHashResult.Error);

        var refreshTokenResult = DomainRefreshToken.Create(
            user.Id,
            tokenHashResult.Value,
            clientInfo,
            now,
            now.Add(RefreshTokenLifetime));

        if (refreshTokenResult.IsFailure)
            return Result<LoginResponse>.Failure(refreshTokenResult.Error);

        var issueResult = user.IssueRefreshToken(refreshTokenResult.Value, now);

        if (issueResult.IsFailure)
            return Result<LoginResponse>.Failure(issueResult.Error);

        var successAttemptResult = LoginAttempt.Create(
            user.Id,
            clientInfo,
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

        await LogSecurityEvent(
            SecurityEventType.LoginSucceeded,
            AuditSeverity.Info,
            now,
            user.Id,
            user.TenantId,
            ipResult.IsSuccess ? ipResult.Value : null,
            userAgent,
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
