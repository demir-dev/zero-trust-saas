namespace ZeroTrustSaaS.Application.Features.Authorization.AssignRole;

public sealed record AssignRoleCommand(
    Guid UserId,
    Guid RoleId,
    Guid? TenantId);
