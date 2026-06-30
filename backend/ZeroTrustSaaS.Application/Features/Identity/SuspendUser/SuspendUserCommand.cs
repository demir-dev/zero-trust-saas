namespace ZeroTrustSaaS.Application.Features.Identity.SuspendUser;

public sealed record SuspendUserCommand(Guid UserId, Guid ActorId, Guid? TenantId = null);
