using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Platform;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Runs in ALL environments on every startup.
/// Ensures the platform_configuration sentinel row exists and that the 3 platform
/// roles are seeded. Safe to re-run — skips anything that already exists.
/// </summary>
public sealed class PlatformConfigurationSeeder(
    IPlatformConfigurationRepository platformConfigRepository,
    IRoleRepository roleRepository,
    IUnitOfWork unitOfWork)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var added = false;

        // Seed the sentinel PlatformConfiguration row.
        var config = await platformConfigRepository.GetAsync(cancellationToken);
        if (config is null)
        {
            config = PlatformConfiguration.CreateNew();
            await platformConfigRepository.AddAsync(config, cancellationToken);
            added = true;
        }

        // Seed the 3 platform roles (TenantId = null).
        foreach (var roleName in WellKnownPlatformRoles.All)
        {
            var existing = await roleRepository.GetByNameAsync(roleName, tenantId: null, cancellationToken);
            if (existing is not null)
                continue;

            var nameResult = RoleName.Create(roleName);
            if (nameResult.IsFailure) continue;

            var roleResult = Role.Create(nameResult.Value, tenantId: null, PermissionScope.Global, isSystem: true);
            if (roleResult.IsFailure) continue;

            await roleRepository.AddAsync(roleResult.Value, cancellationToken);
            added = true;
        }

        if (added)
            await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
