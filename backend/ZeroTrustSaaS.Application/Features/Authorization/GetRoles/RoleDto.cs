namespace ZeroTrustSaaS.Application.Features.Authorization.GetRoles;

public sealed record RoleDto(
    Guid Id,
    string Name,
    Guid? TenantId,
    string Scope,
    bool IsSystem,
    IReadOnlyList<string> Permissions);
