using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization;

public sealed class Role : AggregateRoot
{
    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
    }

    private Role(
        Guid id,
        RoleName name,
        Guid? tenantId,
        PermissionScope scope,
        bool isSystem)
        : base(id)
    {
        Name = name;
        TenantId = tenantId;
        Scope = scope;
        IsSystem = isSystem;
    }

    public RoleName Name { get; private set; } = null!;

    public Guid? TenantId { get; private set; }

    public PermissionScope Scope { get; private set; }

    public bool IsSystem { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public static Result<Role> Create(
        RoleName name,
        Guid? tenantId,
        PermissionScope scope,
        bool isSystem = false)
    {
        var role = new Role(Guid.NewGuid(), name, tenantId, scope, isSystem);

        return Result<Role>.Success(role);
    }

    public Result Rename(RoleName name)
    {
        if (IsSystem)
            return Result.Failure(RoleErrors.SystemRoleCannotBeModified);

        Name = name;

        return Result.Success();
    }

    public Result AssignPermission(PermissionCode code, DateTime assignedAtUtc)
    {
        if (IsSystem)
            return Result.Failure(RoleErrors.SystemRoleCannotBeModified);

        if (_permissions.Any(p => p.Code == code))
            return Result.Failure(RoleErrors.PermissionAlreadyAssigned);

        _permissions.Add(RolePermission.Create(Id, code, assignedAtUtc));

        return Result.Success();
    }

    public Result RemovePermission(PermissionCode code)
    {
        if (IsSystem)
            return Result.Failure(RoleErrors.SystemRoleCannotBeModified);

        var permission = _permissions.FirstOrDefault(p => p.Code == code);

        if (permission is null)
            return Result.Failure(RoleErrors.PermissionNotAssigned);

        _permissions.Remove(permission);

        return Result.Success();
    }

    public bool HasPermission(PermissionCode code)
    {
        return _permissions.Any(p => p.Code == code);
    }
}
