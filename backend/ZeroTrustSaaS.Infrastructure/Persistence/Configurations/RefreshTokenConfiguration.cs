using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Identity;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.UserId).IsRequired();

        builder.Property(rt => rt.SessionId)
            .HasColumnName("session_id")
            .IsRequired();

        builder.HasIndex(rt => rt.SessionId)
            .HasDatabaseName("ix_refresh_tokens_session_id");

        builder.Property(rt => rt.TenantId);

        builder.Property(rt => rt.TrustedDeviceId)
            .HasColumnName("trusted_device_id")
            .IsRequired(false);

        builder.OwnsOne(rt => rt.TokenHash, th =>
        {
            th.Property(h => h.Value)
                .HasColumnName("token_hash")
                .HasMaxLength(128)
                .IsRequired();

            th.HasIndex(h => h.Value)
                .IsUnique()
                .HasDatabaseName("ix_refresh_tokens_token_hash");
        });

        builder.OwnsOne(rt => rt.IssuedClient, ic =>
        {
            ic.OwnsOne(c => c.DeviceFingerprint, df =>
            {
                df.Property(d => d.Value)
                    .HasColumnName("device_fingerprint")
                    .HasMaxLength(256)
                    .IsRequired();
            });

            ic.OwnsOne(c => c.IpAddress, ip =>
            {
                ip.Property(i => i.Value)
                    .HasColumnName("issued_ip")
                    .HasMaxLength(45)
                    .IsRequired();
            });

            ic.Property(c => c.Country)
                .HasColumnName("country")
                .HasMaxLength(100)
                .IsRequired();

            ic.Property(c => c.Browser)
                .HasColumnName("browser")
                .HasMaxLength(200)
                .IsRequired();

            ic.Property(c => c.OperatingSystem)
                .HasColumnName("operating_system")
                .HasMaxLength(200)
                .IsRequired();
        });

        builder.OwnsOne(rt => rt.RevokedByIp, ip =>
        {
            ip.Property(i => i.Value)
                .HasColumnName("revoked_by_ip")
                .HasMaxLength(45);
        });

        builder.Property(rt => rt.RevocationReason)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");
    }
}
