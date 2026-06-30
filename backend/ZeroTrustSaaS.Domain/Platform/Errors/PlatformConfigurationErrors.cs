using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Platform.Errors;

public static class PlatformConfigurationErrors
{
    public static readonly Error AlreadyInitialized =
        Error.Conflict(
            "Platform.AlreadyInitialized",
            "The platform has already been initialized.");

    public static readonly Error NotFound =
        Error.NotFound(
            "Platform.ConfigurationNotFound",
            "Platform configuration not found. Ensure database migrations have been applied.");
}
