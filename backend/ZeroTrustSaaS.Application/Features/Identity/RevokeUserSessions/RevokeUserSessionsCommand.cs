namespace ZeroTrustSaaS.Application.Features.Identity.RevokeUserSessions;

public sealed record RevokeUserSessionsCommand(Guid UserId, Guid ActorId, Guid? TenantId = null);
