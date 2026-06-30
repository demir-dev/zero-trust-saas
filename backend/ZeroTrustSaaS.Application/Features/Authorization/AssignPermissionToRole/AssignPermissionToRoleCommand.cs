namespace ZeroTrustSaaS.Application.Features.Authorization.AssignPermissionToRole;

public sealed record AssignPermissionToRoleCommand(Guid RoleId, string PermissionCode);
