using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization;

public sealed class Permission : Entity
{
    private Permission()
    {
    }

    private Permission(Guid id, PermissionCode code, string description)
        : base(id)
    {
        Code = code;
        Description = description;
    }

    public PermissionCode Code { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public static Result<Permission> Create(PermissionCode code, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result<Permission>.Failure(PermissionErrors.DescriptionRequired);

        return Result<Permission>.Success(
            new Permission(Guid.NewGuid(), code, description.Trim()));
    }

    public Result ChangeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure(PermissionErrors.DescriptionRequired);

        Description = description.Trim();

        return Result.Success();
    }
}
