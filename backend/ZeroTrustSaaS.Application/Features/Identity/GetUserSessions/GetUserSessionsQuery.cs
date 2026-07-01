namespace ZeroTrustSaaS.Application.Features.Identity.GetUserSessions;

public sealed record GetUserSessionsQuery(Guid UserId, Guid? TenantId);
