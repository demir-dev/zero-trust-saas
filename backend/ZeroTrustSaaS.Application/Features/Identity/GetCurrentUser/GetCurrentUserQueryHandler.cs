using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IUserRepository userRepository)
{
    public async Task<Result<UserDto>> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
            return Result<UserDto>.Failure(UserErrors.NotFound);

        var dto = new UserDto(
            user.Id,
            user.Email.Value,
            user.DisplayName,
            user.Status.ToString(),
            user.IsEmailConfirmed,
            user.IsMfaEnabled,
            user.MfaMethod.ToString(),
            user.RegisteredAtUtc,
            user.LastLoginUtc,
            user.LockedUntilUtc);

        return Result<UserDto>.Success(dto);
    }
}
