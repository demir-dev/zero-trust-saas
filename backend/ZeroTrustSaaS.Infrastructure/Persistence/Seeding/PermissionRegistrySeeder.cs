using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Authorization;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the global permission catalog on every startup. Runs in ALL environments.
/// Idempotent — skips permissions that already exist.
/// </summary>
public sealed class PermissionRegistrySeeder(
    IPermissionRepository permissionRepository,
    IUnitOfWork unitOfWork)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await permissionRepository.GetAllAsync(cancellationToken);
        var existingCodes = existing.Select(p => p.Code.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = false;

        foreach (var (code, description) in WellKnownPermissions.AllWithDescriptions)
        {
            if (existingCodes.Contains(code)) continue;

            var permResult = Permission.Create(PermissionCode.From(code), description);
            if (permResult.IsSuccess)
            {
                await permissionRepository.AddAsync(permResult.Value, cancellationToken);
                added = true;
            }
        }

        if (added)
            await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
