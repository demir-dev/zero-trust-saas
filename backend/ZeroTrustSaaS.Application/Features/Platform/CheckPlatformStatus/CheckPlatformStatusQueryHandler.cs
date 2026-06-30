using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Platform.CheckPlatformStatus;

public sealed class CheckPlatformStatusQueryHandler(IPlatformConfigurationRepository platformConfigRepository)
{
    public async Task<Result<bool>> Handle(
        CheckPlatformStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var config = await platformConfigRepository.GetAsync(cancellationToken);
        return Result<bool>.Success(config?.IsInitialized ?? false);
    }
}
