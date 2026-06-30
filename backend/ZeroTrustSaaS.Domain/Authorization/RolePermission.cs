using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization;

public sealed class RolePermission : Entity
{
    private RolePermission()
    {
    }

    private RolePermission(Guid id, Guid roleId, PermissionCode code, DateTime assignedAtUtc)
        : base(id)
    {
        RoleId = roleId;
        Code = code;
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid RoleId { get; private set; }

    public PermissionCode Code { get; private set; } = null!;

    public DateTime AssignedAtUtc { get; private set; }

    internal static RolePermission Create(Guid roleId, PermissionCode code, DateTime assignedAtUtc)
    {
        return new(Guid.NewGuid(), roleId, code, assignedAtUtc);
    }
}
