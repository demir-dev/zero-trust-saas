using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.GetUserSessions;

public sealed class GetUserSessionsQueryHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    ICurrentUserContext currentUser)
{
    public async Task<Result<IReadOnlyList<ActiveSessionDto>>> Handle(
        GetUserSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserView);
        if (permCheck.IsFailure) return Result<IReadOnlyList<ActiveSessionDto>>.Failure(permCheck.Error);

        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
            return Result<IReadOnlyList<ActiveSessionDto>>.Failure(UserErrors.NotFound);

        var tokens = await refreshTokenRepository.GetActiveByUserIdAsync(query.UserId, cancellationToken);

        var sessions = tokens.Select(t => new ActiveSessionDto(
            Id: t.Id,
            IssuedAtUtc: t.IssuedAtUtc,
            ExpiresAtUtc: t.ExpiresAtUtc,
            IpAddress: t.IssuedClient.IpAddress.Value == string.Empty ? null : t.IssuedClient.IpAddress.Value,
            Browser: t.IssuedClient.Browser,
            OperatingSystem: t.IssuedClient.OperatingSystem,
            Country: t.IssuedClient.Country,
            TenantId: t.TenantId))
            .ToList();

        return Result<IReadOnlyList<ActiveSessionDto>>.Success(sessions);
    }
}
