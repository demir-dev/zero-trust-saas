using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Identity.GetUsers;

public sealed class GetUsersQueryHandler(
    ITenantMembershipRepository membershipRepository,
    IUserRepository userRepository)
{
    public async Task<Result<PagedResult<UserDto>>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var memberships = await membershipRepository.GetByTenantIdAsync(
            query.TenantId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var total = await membershipRepository.CountByTenantAsync(query.TenantId, cancellationToken);

        var items = new List<UserDto>(memberships.Count);

        foreach (var membership in memberships)
        {
            var user = await userRepository.GetByIdAsync(membership.UserId, cancellationToken);
            if (user is null)
                continue;

            items.Add(new UserDto(
                user.Id,
                user.Email.Value,
                user.DisplayName,
                user.Status.ToString(),
                user.IsEmailConfirmed,
                user.IsMfaEnabled,
                user.MfaMethod.ToString(),
                user.RegisteredAtUtc,
                user.LastLoginUtc,
                user.LockedUntilUtc));
        }

        return Result<PagedResult<UserDto>>.Success(
            new PagedResult<UserDto>(items, total, query.Page, query.PageSize));
    }
}
