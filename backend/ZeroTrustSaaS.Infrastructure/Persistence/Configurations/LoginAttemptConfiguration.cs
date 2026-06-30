using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Identity;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("login_attempts");

        builder.HasKey(la => la.Id);

        builder.Property(la => la.UserId).IsRequired();

        builder.OwnsOne(la => la.ClientInfo, ic =>
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
                    .HasColumnName("ip_address")
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

        builder.Property(la => la.Result)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(la => la.RiskLevel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(la => la.OccurredAtUtc).IsRequired();

        builder.HasIndex(la => la.UserId)
            .HasDatabaseName("ix_login_attempts_user_id");
    }
}
