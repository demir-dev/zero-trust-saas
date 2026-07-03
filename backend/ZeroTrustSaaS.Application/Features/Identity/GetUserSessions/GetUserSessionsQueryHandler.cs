using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.GetUserSessions;

public sealed class GetUserSessionsQueryHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    ICurrentUserContext currentUser)
{
    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(
        GetUserSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var permCheck = currentUser.RequirePermission(WellKnownPermissions.UserView);
        if (permCheck.IsFailure) return Result<IReadOnlyList<SessionDto>>.Failure(permCheck.Error);

        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
            return Result<IReadOnlyList<SessionDto>>.Failure(UserErrors.NotFound);

        var sessions = await sessionRepository.GetRecentByUserIdAsync(query.UserId, cancellationToken);

        var dtos = sessions
            .Select(s => new SessionDto(
                Id: s.Id,
                Status: s.Status,
                CreatedAtUtc: s.CreatedAtUtc,
                LastSeenAtUtc: s.LastSeenAtUtc,
                LastActivityUtc: s.LastActivityUtc,
                ExpiresAtUtc: s.ExpiresAtUtc,
                RevokedAtUtc: s.RevokedAtUtc,
                IpAddress: s.IpAddress,
                Browser: s.Browser,
                OperatingSystem: s.OperatingSystem,
                Country: s.Country,
                TenantId: s.TenantId,
                TrustedDeviceId: s.TrustedDeviceId,
                IsCurrentSession: s.Id == currentUser.SessionId))
            .ToList();

        return Result<IReadOnlyList<SessionDto>>.Success(dtos);
    }
}
