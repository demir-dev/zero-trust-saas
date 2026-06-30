using ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds development data on first startup. Never runs in Production.
/// Delegates to InitializePlatformCommandHandler — no logic duplication.
/// </summary>
public sealed class DevelopmentDataSeeder(InitializePlatformCommandHandler handler)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var command = new InitializePlatformCommand(
            OrganizationName: "ZeroTrust Labs",
            OrganizationSlug: "zerotrust",
            AdminFirstName: "Admin",
            AdminLastName: "User",
            AdminEmail: "admin@zerotrust.local",
            AdminPassword: "Admin123!");

        // Handler returns AlreadyInitialized if already seeded — idempotent
        await handler.Handle(command, cancellationToken);
    }
}
