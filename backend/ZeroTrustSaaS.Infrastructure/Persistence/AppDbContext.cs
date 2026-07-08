using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Platform;
using ZeroTrustSaaS.Domain.Sessions;
using ZeroTrustSaaS.Domain.Tenants;

namespace ZeroTrustSaaS.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Session> Sessions => Set<Session>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    public DbSet<TrustedDevice> TrustedDevices => Set<TrustedDevice>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<PlatformConfiguration> PlatformConfigurations => Set<PlatformConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Every aggregate/entity Id is assigned by the domain layer (Guid.NewGuid() in
        // constructors), never by the database. Without this, EF's default
        // ValueGeneratedOnAdd convention for Guid keys makes it use "is the key already
        // set?" as its heuristic for Added vs. Modified when a new entity is discovered
        // through an already-tracked parent's collection navigation (e.g. a new child
        // appended to an Include()-loaded collection). Since our keys are always set
        // before the entity is ever tracked, that heuristic misfires and marks brand-new
        // rows as Modified, producing UPDATE statements against rows that don't exist yet.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.IsPrimaryKey() && property.ClrType == typeof(Guid))
                    property.ValueGenerated = ValueGenerated.Never;
            }
        }
    }
}
