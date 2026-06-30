namespace ZeroTrustSaaS.Application.Features.Identity.CreateTenantUser;

public sealed record CreateTenantUserCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid? RoleId = null);
