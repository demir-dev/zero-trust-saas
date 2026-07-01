using Microsoft.Extensions.DependencyInjection;
using ZeroTrustSaaS.Application.Features.Audit.GetAuditLogs;
using ZeroTrustSaaS.Application.Features.Authorization.AssignPermissionToRole;
using ZeroTrustSaaS.Application.Features.Authorization.AssignRole;
using ZeroTrustSaaS.Application.Features.Authorization.CloneRole;
using ZeroTrustSaaS.Application.Features.Authorization.CreateRole;
using ZeroTrustSaaS.Application.Features.Authorization.DeleteRole;
using ZeroTrustSaaS.Application.Features.Authorization.GetPermissions;
using ZeroTrustSaaS.Application.Features.Authorization.GetRoles;
using ZeroTrustSaaS.Application.Features.Authorization.RemovePermissionFromRole;
using ZeroTrustSaaS.Application.Features.Authorization.RenameRole;
using ZeroTrustSaaS.Application.Features.Authorization.RevokeUserRole;
using ZeroTrustSaaS.Application.Features.Identity.CreateTenantUser;
using ZeroTrustSaaS.Application.Features.Identity.ForcePasswordReset;
using ZeroTrustSaaS.Application.Features.Identity.GetUserDetails;
using ZeroTrustSaaS.Application.Features.Dashboard.GetSecurityOverview;
using ZeroTrustSaaS.Application.Features.Devices.BlockDevice;
using ZeroTrustSaaS.Application.Features.Devices.GetDevices;
using ZeroTrustSaaS.Application.Features.Devices.RevokeDevice;
using ZeroTrustSaaS.Application.Features.Devices.TrustDevice;
using ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;
using ZeroTrustSaaS.Application.Features.Devices.GetTenantDevices;
using ZeroTrustSaaS.Application.Features.Identity.ActivateUser;
using ZeroTrustSaaS.Application.Features.Identity.GetUsers;
using ZeroTrustSaaS.Application.Features.Identity.LockUser;
using ZeroTrustSaaS.Application.Features.Identity.GetUserSessions;
using ZeroTrustSaaS.Application.Features.Identity.RevokeUserSession;
using ZeroTrustSaaS.Application.Features.Identity.RevokeUserSessions;
using ZeroTrustSaaS.Application.Features.Identity.SuspendUser;
using ZeroTrustSaaS.Application.Features.Identity.Login;
using ZeroTrustSaaS.Application.Features.Identity.Logout;
using ZeroTrustSaaS.Application.Features.Identity.Mfa;
using ZeroTrustSaaS.Application.Features.Identity.Mfa.SetupTotp;
using ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyAndEnableMfa;
using ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyMfa;
using ZeroTrustSaaS.Application.Features.Identity.RefreshToken;
using ZeroTrustSaaS.Application.Features.Identity.Register;
using ZeroTrustSaaS.Application.Features.Identity.UnlockUser;
using ZeroTrustSaaS.Application.Features.Platform.CheckPlatformStatus;
using ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;
using ZeroTrustSaaS.Application.Features.Tenants.ActivateTenant;
using ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenant;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenantUsers;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenants;
using ZeroTrustSaaS.Application.Features.Tenants.SuspendTenant;

namespace ZeroTrustSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Commands
        services.AddScoped<RegisterUserCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<DisableMfaCommandHandler>();
        services.AddScoped<SetupTotpQueryHandler>();
        services.AddScoped<VerifyAndEnableMfaCommandHandler>();
        services.AddScoped<VerifyMfaCommandHandler>();
        services.AddScoped<LockUserCommandHandler>();
        services.AddScoped<UnlockUserCommandHandler>();
        services.AddScoped<SuspendUserCommandHandler>();
        services.AddScoped<ActivateUserCommandHandler>();
        services.AddScoped<RevokeUserSessionsCommandHandler>();
        services.AddScoped<TrustDeviceCommandHandler>();
        services.AddScoped<RevokeDeviceCommandHandler>();
        services.AddScoped<BlockDeviceCommandHandler>();
        services.AddScoped<CreateTenantCommandHandler>();
        services.AddScoped<SuspendTenantCommandHandler>();
        services.AddScoped<ActivateTenantCommandHandler>();
        services.AddScoped<AssignRoleCommandHandler>();
        services.AddScoped<RevokeUserRoleCommandHandler>();
        services.AddScoped<CreateRoleCommandHandler>();
        services.AddScoped<DeleteRoleCommandHandler>();
        services.AddScoped<RenameRoleCommandHandler>();
        services.AddScoped<CloneRoleCommandHandler>();
        services.AddScoped<AssignPermissionToRoleCommandHandler>();
        services.AddScoped<RemovePermissionFromRoleCommandHandler>();
        services.AddScoped<CreateTenantUserCommandHandler>();
        services.AddScoped<ForcePasswordResetCommandHandler>();
        services.AddScoped<RevokeUserSessionCommandHandler>();
        services.AddScoped<CheckPlatformStatusQueryHandler>();
        services.AddScoped<InitializePlatformCommandHandler>();

        // Queries
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<GetUserDetailsQueryHandler>();
        services.AddScoped<GetUsersQueryHandler>();
        services.AddScoped<GetTenantsQueryHandler>();
        services.AddScoped<GetTenantQueryHandler>();
        services.AddScoped<GetTenantUsersQueryHandler>();
        services.AddScoped<GetDevicesQueryHandler>();
        services.AddScoped<GetTenantDevicesQueryHandler>();
        services.AddScoped<GetRolesQueryHandler>();
        services.AddScoped<GetPermissionsQueryHandler>();
        services.AddScoped<GetAuditLogsQueryHandler>();
        services.AddScoped<GetUserSessionsQueryHandler>();
        services.AddScoped<GetSecurityOverviewQueryHandler>();

        return services;
    }
}
