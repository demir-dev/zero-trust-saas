namespace ZeroTrustSaaS.Application.Features.Authorization.CloneRole;

public sealed record CloneRoleCommand(Guid SourceRoleId, string NewName, Guid? TenantId = null);
