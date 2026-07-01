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
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    ITokenGenerator tokenGenerator,
    ITenantStatusCache tenantStatusCache,
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

        if (!existingToken.IsActive)
        {
            if (existingToken.IsRevoked)
            {
                var user = await userRepository.GetByIdWithTokensAsync(existingToken.UserId, cancellationToken);

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

        // Re-issue with same tenant context (null = platform, set = tenant).
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
            now.Add(RefreshTokenLifetime),
            tenantId);

        if (newRefreshTokenResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(newRefreshTokenResult.Error);

        var rotateResult = existingToken.Rotate(newRefreshTokenResult.Value.Id, now);
        if (rotateResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(rotateResult.Error);

        var issueResult = activeUser.IssueRefreshToken(newRefreshTokenResult.Value, now);
        if (issueResult.IsFailure)
            return Result<RefreshTokenResponse>.Failure(issueResult.Error);

        // Re-build JWT claims matching the same context as the original token.
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
                permissions: []);
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
                permissions: permissionCodes);
        }

        refreshTokenRepository.Update(existingToken);
        userRepository.Update(activeUser);

        var logResult = AuditLog.Create(
            SecurityEventType.RefreshTokenRotated,
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
