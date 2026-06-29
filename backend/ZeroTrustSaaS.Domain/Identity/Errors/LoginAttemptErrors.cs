using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Identity.Errors;

public static class LoginAttemptErrors
{
    public static readonly Error InvalidClientInfo =
        Error.Validation(
            "Identity.LoginAttempt.InvalidClientInfo",
            "Client information is required.");

    public static readonly Error InvalidOccurredAt =
        Error.Validation(
            "Identity.LoginAttempt.InvalidOccurredAt",
            "The login attempt timestamp is invalid.");
}