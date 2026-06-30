using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Audit;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(al => al.Severity)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(al => al.OccurredAtUtc).IsRequired();

        builder.OwnsOne(al => al.IpAddress, ip =>
        {
            ip.Property(i => i.Value)
                .HasColumnName("ip_address")
                .HasMaxLength(45);
        });

        builder.Property(al => al.UserAgent)
            .HasMaxLength(512);

        builder.Property(al => al.Metadata)
            .HasMaxLength(AuditLog.MaxMetadataLength);

        builder.HasIndex(al => al.UserId)
            .HasDatabaseName("ix_audit_logs_user_id");

        builder.HasIndex(al => al.TenantId)
            .HasDatabaseName("ix_audit_logs_tenant_id");

        builder.HasIndex(al => al.OccurredAtUtc)
            .HasDatabaseName("ix_audit_logs_occurred_at");
    }
}
