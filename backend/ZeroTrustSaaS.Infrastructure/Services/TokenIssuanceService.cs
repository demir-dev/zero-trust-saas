using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class TokenIssuanceService(
    ISessionRepository sessionRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IRoleRepository roleRepository,
    ITokenGenerator tokenGenerator)
    : ITokenIssuanceService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<Result<TokenIssuanceResult>> IssueAsync(
        User user,
        Guid? tenantId,
        Guid? deviceId,
        ClientInfo clientInfo,
        string? userAgent,
        DateTime now,
        CancellationToken ct = default)
    {
        var sessionResult = Session.Create(
            user.Id,
            tenantId,
            deviceId,
            clientInfo.IpAddress.Value == string.Empty ? null : clientInfo.IpAddress.Value,
            userAgent,
            clientInfo.Browser,
            clientInfo.OperatingSystem,
            clientInfo.Country,
            now);

        if (sessionResult.IsFailure)
            return Result<TokenIssuanceResult>.Failure(sessionResult.Error);

        var session = sessionResult.Value;
        await sessionRepository.AddAsync(session, ct);

        string rawRefreshToken = tokenGenerator.GenerateRefreshTokenValue();
        string hashedToken = tokenGenerator.HashRefreshToken(rawRefreshToken);

        var tokenHashResult = RefreshTokenHash.Create(hashedToken);
        if (tokenHashResult.IsFailure)
            return Result<TokenIssuanceResult>.Failure(tokenHashResult.Error);

        var refreshTokenResult = RefreshToken.Create(
            user.Id,
            session.Id,
            tokenHashResult.Value,
            clientInfo,
            now,
            now.Add(RefreshTokenLifetime),
            tenantId,
            trustedDeviceId: deviceId);

        if (refreshTokenResult.IsFailure)
            return Result<TokenIssuanceResult>.Failure(refreshTokenResult.Error);

        await refreshTokenRepository.AddAsync(refreshTokenResult.Value, ct);

        string accessToken;

        if (tenantId is null)
        {
            var userRoles = await roleRepository.GetUserRolesAsync(user.Id, null, ct);
            var platformRoleNames = new List<string>();

            foreach (var ur in userRoles.Where(r => r.IsActive))
            {
                var role = await roleRepository.GetByIdAsync(ur.RoleId, ct);
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
                sessionId: session.Id,
                deviceId: deviceId);
        }
        else
        {
            var userRoles = await roleRepository.GetUserRolesAsync(user.Id, tenantId, ct);
            var activeUserRole = userRoles.FirstOrDefault(ur => ur.IsActive);

            string tenantRoleName = string.Empty;
            var permissionCodes = new List<string>();

            if (activeUserRole is not null)
            {
                var role = await roleRepository.GetByIdAsync(activeUserRole.RoleId, ct);
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
                sessionId: session.Id,
                deviceId: deviceId);
        }

        return Result<TokenIssuanceResult>.Success(
            new TokenIssuanceResult(accessToken, rawRefreshToken, session.Id));
    }
}
