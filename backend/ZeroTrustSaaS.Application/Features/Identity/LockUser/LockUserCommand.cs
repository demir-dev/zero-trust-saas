namespace ZeroTrustSaaS.Application.Features.Identity.LockUser;

public sealed record LockUserCommand(Guid UserId, DateTime LockedAtUtc, TimeSpan? Duration = null);
