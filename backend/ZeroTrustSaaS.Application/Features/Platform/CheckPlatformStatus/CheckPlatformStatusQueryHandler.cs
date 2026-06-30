using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Platform.CheckPlatformStatus;

public sealed class CheckPlatformStatusQueryHandler(ITenantRepository tenantRepository)
{
    public async Task<Result<bool>> Handle(
        CheckPlatformStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var count = await tenantRepository.CountAsync(cancellationToken);
        return Result<bool>.Success(count > 0);
    }
}
