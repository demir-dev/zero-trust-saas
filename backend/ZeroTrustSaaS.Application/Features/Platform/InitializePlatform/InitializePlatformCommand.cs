namespace ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;

public sealed record InitializePlatformCommand(
    string OrganizationName,
    string OrganizationSlug,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword);
