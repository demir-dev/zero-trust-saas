namespace ZeroTrustSaaS.Application.Features.Identity.Register;

public sealed record RegisterUserCommand(
    Guid TenantId,
    string Email,
    string Password);
