namespace ZeroTrustSaaS.Application.Features.Identity.GetUserDetails;

public sealed record GetUserDetailsQuery(Guid UserId, Guid? TenantId = null);
