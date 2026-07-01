using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ZeroTrustSaaS.Api.Endpoints;
using ZeroTrustSaaS.Api.Services;
using ZeroTrustSaaS.Application;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Infrastructure;
using ZeroTrustSaaS.Infrastructure.Persistence;
using ZeroTrustSaaS.Infrastructure.Persistence.Seeding;
using ZeroTrustSaaS.Infrastructure.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var log = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ZeroTrustSaaS.Auth.OnTokenValidated");

                var path   = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;

                var userIdClaim = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var stampClaim  = context.Principal?.FindFirstValue("security_stamp");
                var sessionIdClaim = context.Principal?.FindFirstValue("session_id");

                log.LogInformation(
                    "[OnTokenValidated] ENTER  {Method} {Path}  user={UserId}  session={SessionId}",
                    method, path, userIdClaim ?? "(none)", sessionIdClaim ?? "(none)");

                if (userIdClaim is null || stampClaim is null
                    || !Guid.TryParse(userIdClaim, out var userId))
                {
                    log.LogWarning("[OnTokenValidated] FAIL missing sub/stamp claims");
                    return;
                }

                var stampCache = context.HttpContext.RequestServices
                    .GetRequiredService<ISecurityStampCache>();

                var currentStamp = await stampCache.GetOrFetchStampAsync(
                    userId, context.HttpContext.RequestAborted);

                if (currentStamp is null || currentStamp != stampClaim)
                {
                    log.LogWarning(
                        "[OnTokenValidated] REJECT stamp_mismatch  user={UserId}  " +
                        "jwt_stamp={JwtStamp}  db_stamp={DbStamp}",
                        userId, stampClaim, currentStamp ?? "(null)");
                    context.Fail("Security stamp mismatch.");
                    return;
                }

                log.LogDebug("[OnTokenValidated] stamp OK  user={UserId}", userId);

                var tenantIdClaim = context.Principal?.FindFirstValue("tenant_id");
                if (tenantIdClaim is not null && Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    var tenantStatusCache = context.HttpContext.RequestServices
                        .GetRequiredService<ITenantStatusCache>();
                    if (!await tenantStatusCache.IsActiveAsync(tenantId, context.HttpContext.RequestAborted))
                    {
                        log.LogWarning("[OnTokenValidated] REJECT tenant_suspended  tenant={TenantId}", tenantId);
                        context.Fail("Tenant is suspended.");
                        return;
                    }
                    log.LogDebug("[OnTokenValidated] tenant OK  tenant={TenantId}", tenantId);
                }

                if (sessionIdClaim is not null && Guid.TryParse(sessionIdClaim, out var sessionId))
                {
                    var sessionCache = context.HttpContext.RequestServices
                        .GetRequiredService<ISessionStatusCache>();
                    bool active = await sessionCache.IsActiveAsync(sessionId, context.HttpContext.RequestAborted);
                    log.LogDebug(
                        "[OnTokenValidated] session check  session={SessionId}  active={Active}",
                        sessionId, active);
                    if (!active)
                    {
                        log.LogWarning("[OnTokenValidated] REJECT session_revoked  session={SessionId}", sessionId);
                        context.Fail("Session has been revoked.");
                        return;
                    }
                }
                else
                {
                    log.LogDebug("[OnTokenValidated] no session_id claim — skipping session check");
                }

                var deviceIdClaim = context.Principal?.FindFirstValue("device_id");
                if (deviceIdClaim is not null && Guid.TryParse(deviceIdClaim, out var deviceId))
                {
                    var deviceCache = context.HttpContext.RequestServices
                        .GetRequiredService<IDeviceStatusCache>();
                    var deviceStatus = await deviceCache.GetStatusAsync(deviceId, context.HttpContext.RequestAborted);
                    if (deviceStatus is DeviceStatus.Blocked or DeviceStatus.Revoked)
                    {
                        log.LogWarning(
                            "[OnTokenValidated] REJECT device_blocked  device={DeviceId}  status={Status}",
                            deviceId, deviceStatus);
                        context.Fail("Device is blocked or revoked.");
                        return;
                    }
                }

                log.LogInformation(
                    "[OnTokenValidated] PASS  {Method} {Path}  user={UserId}  session={SessionId}",
                    method, path, userId, sessionIdClaim ?? "(none)");
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                builder.Configuration["AllowedOrigins"] ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

{
    using var startupScope = app.Services.CreateScope();

    // Migrations must run before any seeder — applies in all environments
    var db = startupScope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Platform configuration sentinel row + platform roles — runs in ALL environments
    var platformSeeder = startupScope.ServiceProvider.GetRequiredService<PlatformConfigurationSeeder>();
    await platformSeeder.SeedAsync();

    // Permissions are application metadata — seed in all environments
    var permissionSeeder = startupScope.ServiceProvider.GetRequiredService<PermissionRegistrySeeder>();
    await permissionSeeder.SeedAsync();

    // Set SEED_DEV_DATA=false to skip pre-seeding and test the Setup Wizard flow manually
    var skipSeed = string.Equals(
        app.Configuration["SEED_DEV_DATA"], "false",
        StringComparison.OrdinalIgnoreCase);

    if (app.Environment.IsDevelopment() && !skipSeed)
    {
        var devSeeder = startupScope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
        await devSeeder.SeedAsync();
    }
}

app.MapSetupEndpoints();
app.MapAuthEndpoints();
app.MapPlatformEndpoints();
app.MapDeviceEndpoints();
app.MapTenantEndpoints();
app.MapAuthorizationEndpoints();
app.MapUserEndpoints();
app.MapDashboardEndpoints();

app.Run();
