using Microsoft.Extensions.DependencyInjection;
using ZeroTrustSaaS.Application.Features.Authorization.AssignRole;
using ZeroTrustSaaS.Application.Features.Devices.TrustDevice;
using ZeroTrustSaaS.Application.Features.Identity.Login;
using ZeroTrustSaaS.Application.Features.Identity.Mfa;
using ZeroTrustSaaS.Application.Features.Identity.RefreshToken;
using ZeroTrustSaaS.Application.Features.Identity.Register;
using ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;

namespace ZeroTrustSaaS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<EnableMfaCommandHandler>();
        services.AddScoped<DisableMfaCommandHandler>();
        services.AddScoped<TrustDeviceCommandHandler>();
        services.AddScoped<CreateTenantCommandHandler>();
        services.AddScoped<AssignRoleCommandHandler>();

        return services;
    }
}
