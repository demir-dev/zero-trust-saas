namespace ZeroTrustSaaS.Application.Features.Authorization.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId, Guid ActorId, Guid? TenantId = null);
