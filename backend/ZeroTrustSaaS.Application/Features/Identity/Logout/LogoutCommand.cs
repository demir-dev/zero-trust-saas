namespace ZeroTrustSaaS.Application.Features.Identity.Logout;

public sealed record LogoutCommand(Guid UserId, DateTime LoggedOutAtUtc);
