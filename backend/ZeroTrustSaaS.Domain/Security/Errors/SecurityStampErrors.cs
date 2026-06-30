using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security.Errors;

public static class SecurityStampErrors
{
    public static readonly Error Empty =
        Error.Validation("Security.SecurityStamp.Empty",
            "Security stamp must not be empty.");
}
