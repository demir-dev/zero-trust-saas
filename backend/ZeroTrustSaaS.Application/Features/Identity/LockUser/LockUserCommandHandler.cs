using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.LockUser;

public sealed class LockUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan DefaultLockDuration = TimeSpan.FromHours(24);

    public async Task<Result> Handle(
        LockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var duration = command.Duration ?? DefaultLockDuration;
        var result = user.LockUntil(command.LockedAtUtc.Add(duration), command.LockedAtUtc);

        if (result.IsFailure)
            return result;

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
