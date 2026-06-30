using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Identity;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.TenantId)
            .IsRequired();

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(320)
                .IsRequired();
        });


        builder.OwnsOne(u => u.PasswordHash, ph =>
        {
            ph.Property(p => p.Value)
                .HasColumnName("password_hash")
                .HasMaxLength(128)
                .IsRequired();
        });

        builder.OwnsOne(u => u.SecurityStamp, ss =>
        {
            ss.Property(s => s.Value)
                .HasColumnName("security_stamp")
                .IsRequired();
        });

        builder.Property(u => u.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.IsEmailConfirmed).IsRequired();

        builder.Property(u => u.RegisteredAtUtc).IsRequired();

        builder.Property(u => u.IsMfaEnabled).IsRequired();

        builder.Property(u => u.MfaMethod)
            .HasConversion<int>()
            .IsRequired();

        builder.OwnsOne(u => u.MfaSecret, ms =>
        {
            ms.Property(s => s.Value)
                .HasColumnName("mfa_secret")
                .HasMaxLength(256);
        });

        builder.Property(u => u.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100);

        builder.Property(u => u.Version).IsRequired();

    }
}
