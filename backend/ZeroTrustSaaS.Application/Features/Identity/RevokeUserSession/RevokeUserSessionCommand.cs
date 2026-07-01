namespace ZeroTrustSaaS.Application.Features.Identity.RevokeUserSession;

public sealed record RevokeUserSessionCommand(Guid UserId, Guid SessionId, Guid? TenantId);
