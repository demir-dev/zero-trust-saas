using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZeroTrustSaaS.Domain.Platform;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Configurations;

internal sealed class PlatformConfigurationConfiguration : IEntityTypeConfiguration<PlatformConfiguration>
{
    public void Configure(EntityTypeBuilder<PlatformConfiguration> builder)
    {
        builder.ToTable("platform_configuration");

        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.IsInitialized).IsRequired();

        builder.Property(pc => pc.InitializedAtUtc);
    }
}
