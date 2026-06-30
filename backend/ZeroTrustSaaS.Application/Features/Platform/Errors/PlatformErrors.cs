using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Features.Platform.Errors;

public static class PlatformErrors
{
    public static readonly Error AlreadyInitialized = Error.Conflict(
        "Platform.AlreadyInitialized",
        "The platform has already been initialized. The setup wizard is no longer available.");

    public static readonly Error NotInitialized = Error.Conflict(
        "Platform.NotInitialized",
        "The platform must be initialized before tenants can be created.");
}
