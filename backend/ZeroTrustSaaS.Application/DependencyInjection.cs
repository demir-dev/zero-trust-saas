using Microsoft.Extensions.DependencyInjection;
using ZeroTrustSaaS.Application.Features.Audit.GetAuditLogs;
using ZeroTrustSaaS.Application.Features.Authorization.AssignPermissionToRole;
using ZeroTrustSaaS.Application.Features.Authorization.AssignRole;
using ZeroTrustSaaS.Application.Features.Authorization.CreateRole;
using ZeroTrustSaaS.Application.Features.Authorization.GetPermissions;
using ZeroTrustSaaS.Application.Features.Authorization.GetRoles;
using ZeroTrustSaaS.Application.Features.Dashboard.GetSecurityOverview;
using ZeroTrustSaaS.Application.Features.Devices.BlockDevice;
using ZeroTrustSaaS.Application.Features.Devices.GetDevices;
using ZeroTrustSaaS.Application.Features.Devices.RevokeDevice;
using ZeroTrustSaaS.Application.Features.Devices.TrustDevice;
using ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;
using ZeroTrustSaaS.Application.Features.Identity.GetUsers;
using ZeroTrustSaaS.Application.Features.Identity.LockUser;
using ZeroTrustSaaS.Application.Features.Identity.Login;
using ZeroTrustSaaS.Application.Features.Identity.Logout;
using ZeroTrustSaaS.Application.Features.Identity.Mfa;
using ZeroTrustSaaS.Application.Features.Identity.RefreshToken;
using ZeroTrustSaaS.Application.Features.Identity.Register;
using ZeroTrustSaaS.Application.Features.Identity.UnlockUser;
using ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenant;
using ZeroTrustSaaS.Application.Features.Platform.CheckPlatformStatus;
using ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenants;

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
        services.AddScoped<EnableMfaCommandHandler>();
        services.AddScoped<DisableMfaCommandHandler>();
        services.AddScoped<LockUserCommandHandler>();
        services.AddScoped<UnlockUserCommandHandler>();
        services.AddScoped<TrustDeviceCommandHandler>();
        services.AddScoped<RevokeDeviceCommandHandler>();
        services.AddScoped<BlockDeviceCommandHandler>();
        services.AddScoped<CreateTenantCommandHandler>();
        services.AddScoped<AssignRoleCommandHandler>();
        services.AddScoped<CreateRoleCommandHandler>();
        services.AddScoped<AssignPermissionToRoleCommandHandler>();
        services.AddScoped<CheckPlatformStatusQueryHandler>();
        services.AddScoped<InitializePlatformCommandHandler>();

        // Queries
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<GetUsersQueryHandler>();
        services.AddScoped<GetTenantsQueryHandler>();
        services.AddScoped<GetTenantQueryHandler>();
        services.AddScoped<GetDevicesQueryHandler>();
        services.AddScoped<GetRolesQueryHandler>();
        services.AddScoped<GetPermissionsQueryHandler>();
        services.AddScoped<GetAuditLogsQueryHandler>();
        services.AddScoped<GetSecurityOverviewQueryHandler>();

        return services;
    }
}
