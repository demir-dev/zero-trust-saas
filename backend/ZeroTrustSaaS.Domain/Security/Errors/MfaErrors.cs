using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Security.Errors;

public static class MfaErrors
{
    public static readonly Error AlreadyEnabled =
        Error.Conflict(
            "Mfa.AlreadyEnabled",
            "Multi-factor authentication is already enabled.");

    public static readonly Error AlreadyDisabled =
        Error.Conflict(
            "Mfa.AlreadyDisabled",
            "Multi-factor authentication is already disabled.");

    public static readonly Error SecretRequired =
        Error.Validation(
            "Mfa.SecretRequired",
            "MFA secret is required.");

    public static readonly Error InvalidMethod =
        Error.Validation(
            "Mfa.InvalidMethod",
            "The MFA method is invalid.");
}