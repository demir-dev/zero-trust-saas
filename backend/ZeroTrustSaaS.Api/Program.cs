using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ZeroTrustSaaS.Api.Endpoints;
using ZeroTrustSaaS.Api.Services;
using ZeroTrustSaaS.Application;
using ZeroTrustSaaS.Application.Abstractions.Services;
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

app.MapAuthEndpoints();
app.MapPlatformEndpoints();
app.MapDeviceEndpoints();
app.MapTenantEndpoints();
app.MapAuthorizationEndpoints();
app.MapUserEndpoints();
app.MapDashboardEndpoints();

app.Run();
