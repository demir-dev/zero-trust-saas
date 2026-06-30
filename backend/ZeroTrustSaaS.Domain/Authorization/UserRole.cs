using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization;

public sealed class UserRole : Entity
{
    private UserRole()
    {
    }

    private UserRole(
        Guid id,
        Guid userId,
        Guid roleId,
        Guid? tenantId,
        DateTime assignedAtUtc)
        : base(id)
    {
        UserId = userId;
        RoleId = roleId;
        TenantId = tenantId;
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid? TenantId { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive => RevokedAtUtc is null;

    public static Result<UserRole> Create(
        Guid userId,
        Guid roleId,
        Guid? tenantId,
        DateTime assignedAtUtc)
    {
        if (userId == Guid.Empty)
            return Result<UserRole>.Failure(UserRoleErrors.InvalidUserId);

        if (roleId == Guid.Empty)
            return Result<UserRole>.Failure(UserRoleErrors.InvalidRoleId);

        return Result<UserRole>.Success(
            new UserRole(Guid.NewGuid(), userId, roleId, tenantId, assignedAtUtc));
    }

    public Result Revoke(DateTime revokedAtUtc)
    {
        if (!IsActive)
            return Result.Failure(UserRoleErrors.NotFound);

        RevokedAtUtc = revokedAtUtc;

        return Result.Success();
    }
}
