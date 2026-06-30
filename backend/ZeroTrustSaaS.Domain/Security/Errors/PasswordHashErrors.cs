using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security.Errors;

public static class PasswordHashErrors
{
    public static readonly Error Required =
        Error.Validation("Security.PasswordHash.Required",
            "Password hash is required.");

    public static readonly Error Invalid =
        Error.Validation("Security.PasswordHash.Invalid",
            "The provided password hash is invalid.");
}
