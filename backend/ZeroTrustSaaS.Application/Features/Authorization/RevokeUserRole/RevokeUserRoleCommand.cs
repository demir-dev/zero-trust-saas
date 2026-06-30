namespace ZeroTrustSaaS.Application.Features.Authorization.RevokeUserRole;

public sealed record RevokeUserRoleCommand(Guid UserId, Guid RoleId, Guid? TenantId, Guid ActorId);
