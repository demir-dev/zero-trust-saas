namespace ZeroTrustSaaS.Application.Features.Authorization.RenameRole;

public sealed record RenameRoleCommand(Guid RoleId, string NewName, Guid ActorId, Guid? TenantId = null);
