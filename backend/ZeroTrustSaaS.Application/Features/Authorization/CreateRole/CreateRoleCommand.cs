namespace ZeroTrustSaaS.Application.Features.Authorization.CreateRole;

public sealed record CreateRoleCommand(
    string Name,
    Guid? TenantId,
    string Scope);
