using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Devices;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDevice>
{
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.ToTable("trusted_devices");

        builder.HasKey(td => td.Id);

        builder.Property(td => td.UserId).IsRequired();

        builder.OwnsOne(td => td.Name, name =>
        {
            name.Property(n => n.Value)
                .HasColumnName("name")
                .HasMaxLength(DeviceName.MaxLength)
                .IsRequired();
        });

        builder.OwnsOne(td => td.ClientInfo, ic =>
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
                .HasMaxLength(100);

            ic.Property(c => c.Browser)
                .HasColumnName("browser")
                .HasMaxLength(200)
                .IsRequired();

            ic.Property(c => c.OperatingSystem)
                .HasColumnName("operating_system")
                .HasMaxLength(200)
                .IsRequired();
        });

        builder.Property(td => td.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(td => td.UserId)
            .HasDatabaseName("ix_trusted_devices_user_id");
    }
}
