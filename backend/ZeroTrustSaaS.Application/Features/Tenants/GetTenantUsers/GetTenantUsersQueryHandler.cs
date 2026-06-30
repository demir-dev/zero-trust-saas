using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Tenants.GetTenantUsers;

public sealed class GetTenantUsersQueryHandler(
    ITenantMembershipRepository membershipRepository,
    IUserRepository userRepository)
{
    public async Task<Result<PagedResult<TenantUserDto>>> Handle(
        GetTenantUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var memberships = await membershipRepository.GetByTenantIdAsync(
            query.TenantId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var total = await membershipRepository.CountByTenantAsync(query.TenantId, cancellationToken);

        var items = new List<TenantUserDto>(memberships.Count);

        foreach (var membership in memberships)
        {
            var user = await userRepository.GetByIdAsync(membership.UserId, cancellationToken);
            if (user is null)
                continue;

            items.Add(new TenantUserDto(
                user.Id,
                user.Email.Value,
                user.DisplayName,
                membership.Status.ToString(),
                membership.IsOwner,
                membership.JoinedAtUtc));
        }

        return Result<PagedResult<TenantUserDto>>.Success(
            new PagedResult<TenantUserDto>(items, total, query.Page, query.PageSize));
    }
}
