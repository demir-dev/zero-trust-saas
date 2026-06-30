using ZeroTrustSaaS.Application.Abstractions.Services;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class UtcDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
