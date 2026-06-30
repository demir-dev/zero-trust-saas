using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Tenants;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;

public sealed class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> Handle(
        CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var nameResult = TenantName.Create(command.Name);

        if (nameResult.IsFailure)
            return Result<Guid>.Failure(nameResult.Error);

        var slugResult = TenantSlug.Create(command.Slug);

        if (slugResult.IsFailure)
            return Result<Guid>.Failure(slugResult.Error);

        bool slugExists = await tenantRepository.ExistsBySlugAsync(
            slugResult.Value.Value,
            cancellationToken);

        if (slugExists)
            return Result<Guid>.Failure(TenantErrors.SlugAlreadyExists);

        var now = dateTimeProvider.UtcNow;

        var tenantResult = Tenant.Create(nameResult.Value, slugResult.Value, now);

        if (tenantResult.IsFailure)
            return Result<Guid>.Failure(tenantResult.Error);

        var tenant = tenantResult.Value;

        var membershipResult = TenantMembership.Create(
            tenant.Id,
            command.OwnerUserId,
            now,
            isOwner: true);

        if (membershipResult.IsFailure)
            return Result<Guid>.Failure(membershipResult.Error);

        tenant.AddMembership(membershipResult.Value);

        await tenantRepository.AddAsync(tenant, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(tenant.Id);
    }
}
