using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Platform.Errors;

namespace ZeroTrustSaaS.Domain.Platform;

public sealed class PlatformConfiguration : AggregateRoot
{
    // Sentinel Id — one record ever exists.
    public static readonly Guid SentinelId = new("00000000-0000-0000-0000-000000000001");

    private PlatformConfiguration()
    {
    }

    private PlatformConfiguration(Guid id) : base(id)
    {
        IsInitialized = false;
    }

    public bool IsInitialized { get; private set; }

    public DateTime? InitializedAtUtc { get; private set; }

    public static PlatformConfiguration CreateNew() =>
        new PlatformConfiguration(SentinelId);

    public Result MarkInitialized(DateTime now)
    {
        if (IsInitialized)
            return Result.Failure(PlatformConfigurationErrors.AlreadyInitialized);

        IsInitialized = true;
        InitializedAtUtc = now;

        return Result.Success();
    }
}
