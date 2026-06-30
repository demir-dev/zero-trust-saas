using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security.Errors;

public static class RefreshTokenHashErrors
{
    public static readonly Error Required =
        Error.Validation("Security.RefreshTokenHash.Required",
            "Refresh token hash is required.");
}
