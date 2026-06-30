using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Application.Features.Tenants.ActivateTenant;

public sealed class ActivateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(
        ActivateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);

        if (tenant is null)
            return Result.Failure(TenantErrors.NotFound);

        var result = tenant.Reactivate(dateTimeProvider.UtcNow);
        if (result.IsFailure)
            return result;

        tenantRepository.Update(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
