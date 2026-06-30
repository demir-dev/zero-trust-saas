using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Authorization.CloneRole;

public sealed class CloneRoleCommandHandler(
    IRoleRepository roleRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> Handle(
        CloneRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        var source = await roleRepository.GetByIdAsync(command.SourceRoleId, cancellationToken);

        if (source is null)
            return Result<Guid>.Failure(RoleErrors.NotFound);

        var nameResult = RoleName.Create(command.NewName);

        if (nameResult.IsFailure)
            return Result<Guid>.Failure(nameResult.Error);

        var cloneResult = Role.Create(nameResult.Value, command.TenantId ?? source.TenantId, source.Scope);

        if (cloneResult.IsFailure)
            return Result<Guid>.Failure(cloneResult.Error);

        var clone = cloneResult.Value;
        var now = dateTimeProvider.UtcNow;

        foreach (var permission in source.Permissions)
        {
            clone.AssignPermission(permission.Code, now);
        }

        await roleRepository.AddAsync(clone, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(clone.Id);
    }
}
