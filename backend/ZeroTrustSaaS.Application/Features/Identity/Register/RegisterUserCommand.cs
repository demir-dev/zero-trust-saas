namespace ZeroTrustSaaS.Application.Features.Identity.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null);
