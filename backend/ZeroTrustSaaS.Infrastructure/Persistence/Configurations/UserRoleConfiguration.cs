using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Authorization;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(ur => ur.Id);

        builder.Property(ur => ur.UserId).IsRequired();

        builder.Property(ur => ur.RoleId).IsRequired();

        builder.Property(ur => ur.AssignedAtUtc).IsRequired();

        builder.HasIndex(ur => new { ur.UserId, ur.RoleId, ur.TenantId })
            .HasDatabaseName("ix_user_roles_user_role_tenant");
    }
}
