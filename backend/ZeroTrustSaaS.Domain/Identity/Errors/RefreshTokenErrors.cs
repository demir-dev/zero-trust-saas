using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Identity.Errors;

public static class RefreshTokenErrors
{
    public static readonly Error AlreadyUsed =
        Error.Conflict(
            "Identity.RefreshToken.AlreadyUsed",
            "The refresh token has already been used.");

    public static readonly Error AlreadyRevoked =
        Error.Conflict(
            "Identity.RefreshToken.AlreadyRevoked",
            "The refresh token has already been revoked.");

    public static readonly Error Expired =
        Error.Validation(
            "Identity.RefreshToken.Expired",
            "The refresh token has expired.");

    public static readonly Error InvalidExpiration =
        Error.Validation(
            "Identity.RefreshToken.InvalidExpiration",
            "The expiration date must be after the issue date.");

    public static readonly Error InvalidReplacementToken =
        Error.Validation(
            "Identity.RefreshToken.InvalidReplacementToken",
            "The replacement refresh token identifier is invalid.");

    public static readonly Error ReasonRequired =
        Error.Validation(
            "Identity.RefreshToken.ReasonRequired",
            "A revocation reason is required.");
}