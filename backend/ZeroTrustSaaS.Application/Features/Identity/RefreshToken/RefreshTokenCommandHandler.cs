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

namespace ZeroTrustSaaS.Application.Features.Identity.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    ITokenGenerator tokenGenerator,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        string hashedInput = tokenGenerator.HashRefreshToken(command.RefreshToken);

        var existingToken = await refreshTokenRepository.GetByHashAsync(
            hashedInput,
            cancellationToken);

        if (existingToken is null)
            return Result<RefreshTokenResponse>.Failure(UserErrors.RefreshTokenNotFound);

        if (!existingToken.IsActive)
        {
            if (existingToken.IsRevoked)
            {
                var user = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);

                if (user is not null)
                {
                    var now2 = dateTimeProvider.UtcNow;
                    user.RevokeAllUserRefreshTokens(now2);
                    userRepository.Update(user);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                return Result<RefreshTokenResponse>.Failure(UserErrors.RefreshTokenAlreadyRevoked);
            }

            if (existingToken.IsExpired)
                return Result<RefreshTokenResponse>.Failure(UserErrors.RefreshTokenExpired);
        }

        var activeUser = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);

        if (activeUser is null || !activeUser.CanAuthenticate)
            return Result<RefreshTokenResponse>.Failure(UserErrors.LoginNotAllowed);

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

        string newRawToken = tokenGenerator.GenerateRefreshTokenValue();
        string newHashedToken = tokenGenerator.HashRefreshToken(newRawToken);

        var newTokenHashResult = RefreshTokenHash.Create(newHashedToken);

        if (newTokenHashResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(newTokenHashResult.Error);

        var newRefreshTokenResult = Domain.Identity.RefreshToken.Create(
            activeUser.Id,
            newTokenHashResult.Value,
            clientInfo,
            now,
            now.Add(RefreshTokenLifetime));

        if (newRefreshTokenResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(newRefreshTokenResult.Error);

        var rotateResult = existingToken.Rotate(newRefreshTokenResult.Value.Id, now);

        if (rotateResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(rotateResult.Error);

        var issueResult = activeUser.IssueRefreshToken(newRefreshTokenResult.Value, now);

        if (issueResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(issueResult.Error);

        string accessToken = tokenGenerator.GenerateJwtToken(
            activeUser.Id,
            activeUser.TenantId,
            []);

        refreshTokenRepository.Update(existingToken);
        userRepository.Update(activeUser);

        var logResult = AuditLog.Create(
            SecurityEventType.RefreshTokenRotated,
            AuditSeverity.Info,
            now,
            userId: activeUser.Id,
            tenantId: activeUser.TenantId,
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
