namespace ZeroTrustSaaS.Application.Common;

public static class WellKnownPlatformRoles
{
    public const string PlatformOwner         = "PlatformOwner";
    public const string PlatformAdministrator = "PlatformAdministrator";
    public const string PlatformSupport       = "PlatformSupport";

    public static readonly IReadOnlyList<string> All =
    [
        PlatformOwner,
        PlatformAdministrator,
        PlatformSupport,
    ];
}
