namespace ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;

public sealed record InitializePlatformCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password);
