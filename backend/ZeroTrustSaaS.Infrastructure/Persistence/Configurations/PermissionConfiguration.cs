using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Authorization;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);

        builder.OwnsOne(p => p.Code, code =>
        {
            code.Property(c => c.Value)
                .HasColumnName("code")
                .HasMaxLength(100)
                .IsRequired();

            code.HasIndex(c => c.Value)
                .IsUnique()
                .HasDatabaseName("ix_permissions_code");
        });

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .IsRequired();
    }
}
