using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.GetUserDetails;

public sealed class GetUserDetailsQueryHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository)
{
    public async Task<Result<UserDetailsDto>> Handle(
        GetUserDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
            return Result<UserDetailsDto>.Failure(UserErrors.NotFound);

        var userRoles = await roleRepository.GetUserRolesAsync(query.UserId, query.TenantId, cancellationToken);

        var activeRoleSummaries = new List<UserRoleSummaryDto>();

        foreach (var ur in userRoles.Where(r => r.IsActive))
        {
            var role = await roleRepository.GetByIdAsync(ur.RoleId, cancellationToken);
            if (role is not null)
            {
                activeRoleSummaries.Add(new UserRoleSummaryDto(
                    role.Id,
                    role.Name.Value,
                    role.IsSystem,
                    ur.AssignedAtUtc));
            }
        }

        var dto = new UserDetailsDto(
            user.Id,
            user.Email.Value,
            user.DisplayName,
            user.Status.ToString(),
            user.IsEmailConfirmed,
            user.IsMfaEnabled,
            user.MfaMethod.ToString(),
            user.RegisteredAtUtc,
            user.LastLoginUtc,
            user.LockedUntilUtc,
            activeRoleSummaries);

        return Result<UserDetailsDto>.Success(dto);
    }
}
