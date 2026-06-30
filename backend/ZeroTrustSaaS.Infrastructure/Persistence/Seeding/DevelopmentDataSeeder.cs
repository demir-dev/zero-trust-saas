using ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;
using ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds development data on first startup. Never runs in Production.
/// Creates a PlatformOwner + a demo tenant idempotently.
/// </summary>
public sealed class DevelopmentDataSeeder(
    InitializePlatformCommandHandler initHandler,
    CreateTenantCommandHandler tenantHandler)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Returns AlreadyInitialized if already seeded — idempotent.
        await initHandler.Handle(
            new InitializePlatformCommand(
                FirstName: "Platform",
                LastName: "Owner",
                Email: "owner@zerotrust.local",
                Password: "Admin123!"),
            cancellationToken);

        // Returns SlugAlreadyExists on second run — idempotent.
        await tenantHandler.Handle(
            new CreateTenantCommand(
                Name: "ZeroTrust Labs",
                Slug: "zerotrust",
                OwnerFirstName: "Tenant",
                OwnerLastName: "Admin",
                OwnerEmail: "admin@zerotrust.local",
                OwnerPassword: "Admin123!"),
            cancellationToken);
    }
}
