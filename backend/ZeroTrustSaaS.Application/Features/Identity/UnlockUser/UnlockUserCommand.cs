namespace ZeroTrustSaaS.Application.Features.Identity.UnlockUser;

public sealed record UnlockUserCommand(Guid UserId, DateTime UnlockedAtUtc);
