using ZeroTrustSaaS.Domain.Platform;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface IPlatformConfigurationRepository
{
    Task<PlatformConfiguration?> GetAsync(CancellationToken cancellationToken = default);

    Task AddAsync(PlatformConfiguration config, CancellationToken cancellationToken = default);

    void Update(PlatformConfiguration config);
}
