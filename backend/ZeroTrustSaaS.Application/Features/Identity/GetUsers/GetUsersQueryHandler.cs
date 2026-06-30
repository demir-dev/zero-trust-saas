using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Identity.GetUsers;

public sealed class GetUsersQueryHandler(IUserRepository userRepository)
{
    public async Task<Result<PagedResult<UserDto>>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetByTenantIdAsync(
            query.TenantId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var total = await userRepository.CountByTenantAsync(query.TenantId, cancellationToken);

        var items = users
            .Select(u => new UserDto(
                u.Id,
                u.TenantId,
                u.Email.Value,
                u.Status.ToString(),
                u.IsEmailConfirmed,
                u.IsMfaEnabled,
                u.MfaMethod.ToString(),
                u.RegisteredAtUtc,
                u.LastLoginUtc,
                u.LockedUntilUtc))
            .ToList();

        return Result<PagedResult<UserDto>>.Success(
            new PagedResult<UserDto>(items, total, query.Page, query.PageSize));
    }
}
