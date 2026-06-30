using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Authorization;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.RoleId).IsRequired();

        builder.OwnsOne(rp => rp.Code, code =>
        {
            code.Property(c => c.Value)
                .HasColumnName("code")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(rp => rp.AssignedAtUtc).IsRequired();
    }
}
