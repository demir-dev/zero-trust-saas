namespace ZeroTrustSaaS.Application.Features.Identity.ActivateUser;

public sealed record ActivateUserCommand(Guid UserId, Guid ActorId, Guid? TenantId = null);
