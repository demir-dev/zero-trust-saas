using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Platform;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class PlatformConfigurationRepository(AppDbContext dbContext)
    : IPlatformConfigurationRepository
{
    public Task<PlatformConfiguration?> GetAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.PlatformConfigurations
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(PlatformConfiguration config, CancellationToken cancellationToken = default)
    {
        return dbContext.PlatformConfigurations.AddAsync(config, cancellationToken).AsTask();
    }

    public void Update(PlatformConfiguration config)
    {
        dbContext.Entry(config).State = EntityState.Modified;
    }
}
