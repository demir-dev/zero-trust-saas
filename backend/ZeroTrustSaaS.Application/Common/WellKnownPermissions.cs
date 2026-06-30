namespace ZeroTrustSaaS.Application.Common;

public static class WellKnownPermissions
{
    public const string TenantView     = "tenant.view";
    public const string TenantManage   = "tenant.manage";
    public const string UserView       = "user.view";
    public const string UserCreate     = "user.create";
    public const string UserManage     = "user.manage";
    public const string RoleView       = "role.view";
    public const string RoleManage     = "role.manage";
    public const string DeviceView     = "device.view";
    public const string DeviceManage   = "device.manage";
    public const string AuditView      = "audit.view";
    public const string MfaManage      = "mfa.manage";
    public const string SecurityManage = "security.manage";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions =
        new Dictionary<string, string>
        {
            [TenantView]     = "View tenant information",
            [TenantManage]   = "Create and manage tenants",
            [UserView]       = "View users",
            [UserCreate]     = "Invite and create users",
            [UserManage]     = "Lock, unlock, and manage users",
            [RoleView]       = "View roles and permissions",
            [RoleManage]     = "Create and assign roles",
            [DeviceView]     = "View trusted devices",
            [DeviceManage]   = "Trust, revoke, and block devices",
            [AuditView]      = "View audit logs and security events",
            [MfaManage]      = "Configure MFA settings",
            [SecurityManage] = "Manage security policies",
        };

    public static readonly IReadOnlyList<string> OwnerPermissions = new[]
    {
        TenantView, TenantManage, UserView, UserCreate, UserManage,
        RoleView, RoleManage, DeviceView, DeviceManage,
        AuditView, MfaManage, SecurityManage
    };

    public static readonly IReadOnlyList<string> AdministratorPermissions = new[]
    {
        TenantView, UserView, UserCreate, UserManage,
        RoleView, RoleManage, DeviceView, DeviceManage,
        AuditView, MfaManage
    };

    public static readonly IReadOnlyList<string> MemberPermissions = new[]
    {
        UserView, DeviceView, AuditView
    };
}
