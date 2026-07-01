using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Infrastructure.Persistence;
using ZeroTrustSaaS.Infrastructure.Persistence.Repositories;
using ZeroTrustSaaS.Infrastructure.Persistence.Seeding;
using ZeroTrustSaaS.Infrastructure.Services;
using ZeroTrustSaaS.Infrastructure.Settings;

namespace ZeroTrustSaaS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantMembershipRepository, TenantMembershipRepository>();
        services.AddScoped<ITrustedDeviceRepository, TrustedDeviceRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPlatformConfigurationRepository, PlatformConfigurationRepository>();

        services.AddMemoryCache();
        services.AddScoped<ISecurityStampCache, SecurityStampCache>();
        services.AddScoped<ITenantStatusCache, TenantStatusCache>();
        services.AddScoped<ISessionStatusCache, SessionStatusCache>();
        services.AddScoped<IDeviceStatusCache, DeviceStatusCache>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IDateTimeProvider, UtcDateTimeProvider>();
        services.AddSingleton<IMfaCodeValidator, TotpMfaCodeValidator>();

        services.AddScoped<PlatformConfigurationSeeder>();
        services.AddScoped<PermissionRegistrySeeder>();
        services.AddScoped<DevelopmentDataSeeder>();

        return services;
    }
}
