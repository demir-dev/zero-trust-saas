using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();

        builder.Property(s => s.TenantId);

        builder.Property(s => s.TrustedDeviceId);

        builder.Property(s => s.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.RevokedReason)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Property(s => s.LastSeenAtUtc).IsRequired();
        builder.Property(s => s.LastActivityUtc).IsRequired();
        builder.Property(s => s.ExpiresAtUtc).IsRequired();
        builder.Property(s => s.RevokedAtUtc);

        builder.Property(s => s.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45);

        builder.Property(s => s.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        builder.Property(s => s.Browser)
            .HasColumnName("browser")
            .HasMaxLength(200);

        builder.Property(s => s.OperatingSystem)
            .HasColumnName("operating_system")
            .HasMaxLength(200);

        builder.Property(s => s.Country)
            .HasColumnName("country")
            .HasMaxLength(100);

        builder.Property(s => s.City)
            .HasColumnName("city")
            .HasMaxLength(100);

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("ix_sessions_user_id");

        builder.HasIndex(s => s.TrustedDeviceId)
            .HasDatabaseName("ix_sessions_trusted_device_id");

        builder.HasIndex(s => new { s.UserId, s.Status })
            .HasDatabaseName("ix_sessions_user_id_status");
    }
}
