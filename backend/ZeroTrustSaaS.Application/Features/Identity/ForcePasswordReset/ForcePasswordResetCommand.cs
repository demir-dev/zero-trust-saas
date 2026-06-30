namespace ZeroTrustSaaS.Application.Features.Identity.ForcePasswordReset;

public sealed record ForcePasswordResetCommand(Guid UserId, Guid ActorId, Guid? TenantId = null);
