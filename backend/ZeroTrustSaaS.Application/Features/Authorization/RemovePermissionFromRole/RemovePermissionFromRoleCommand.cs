namespace ZeroTrustSaaS.Application.Features.Authorization.RemovePermissionFromRole;

public sealed record RemovePermissionFromRoleCommand(Guid RoleId, string PermissionCode, Guid ActorId, Guid? TenantId = null);
