using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Tenants;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships");

        builder.HasKey(tm => tm.Id);

        builder.Property(tm => tm.TenantId).IsRequired();

        builder.Property(tm => tm.UserId).IsRequired();

        builder.Property(tm => tm.IsOwner).IsRequired();

        builder.Property(tm => tm.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(tm => tm.JoinedAtUtc).IsRequired();

        builder.Property(tm => tm.AcceptedAtUtc);

        builder.Property(tm => tm.InvitedByUserId);

        builder.Property(tm => tm.CreatedAtUtc).IsRequired();

        builder.Property(tm => tm.UpdatedAtUtc);

        builder.HasIndex(tm => new { tm.TenantId, tm.UserId })
            .IsUnique()
            .HasDatabaseName("ix_tenant_memberships_tenant_user");
    }
}
