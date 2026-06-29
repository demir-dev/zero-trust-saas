using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Identity.Errors;

public static class UserErrors
{
    // Registration
    public static readonly Error InvalidTenantId =
        Error.Failure(
            "Users.InvalidTenantId",
            "The tenant identifier is invalid.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict(
            "Users.EmailAlreadyExists",
            "A user with the specified email already exists.");

    // Email Verification
    public static readonly Error EmailAlreadyVerified =
        Error.Conflict(
            "Users.EmailAlreadyVerified",
            "The email address has already been verified.");

    public static readonly Error InvalidStatusForEmailVerification =
        Error.Conflict(
            "Users.InvalidStatusForEmailVerification",
            "Only users pending verification can verify their email.");

    // User Status
    public static readonly Error AlreadyActive =
        Error.Conflict(
            "Users.AlreadyActive",
            "The user is already active.");

    public static readonly Error AlreadyDisabled =
        Error.Conflict(
            "Users.AlreadyDisabled",
            "The user is already disabled.");

    public static readonly Error AlreadySuspended =
        Error.Conflict(
            "Users.AlreadySuspended",
            "The user is already suspended.");

    public static readonly Error UserMustBeActive =
        Error.Conflict(
            "Users.UserMustBeActive",
            "The user must be active to perform this operation.");

    public static readonly Error UserIsLocked =
        Error.Conflict(
            "Users.UserIsLocked",
            "The user account is locked.");

    public static readonly Error UserIsNotLocked =
        Error.Conflict(
            "Users.UserIsNotLocked",
            "The user is not locked.");

    public static readonly Error UserIsSuspended =
        Error.Conflict(
            "Users.UserIsSuspended",
            "The user account is suspended.");

    public static readonly Error UserIsNotSuspended =
        Error.Conflict(
            "Users.UserIsNotSuspended",
            "The user is not suspended.");

    public static readonly Error UserIsNotDisabled =
        Error.Conflict(
            "Users.UserIsNotDisabled",
            "The user is not disabled.");

    public static readonly Error DisabledUserCannotBeSuspended =
        Error.Conflict(
            "Users.DisabledUserCannotBeSuspended",
            "A disabled user cannot be suspended.");

    public static readonly Error UserCannotBeLocked =
        Error.Conflict(
            "Users.UserCannotBeLocked",
            "The user cannot be locked in the current state.");

    // Email
    public static readonly Error UserNotAllowedToChangeEmail =
        Error.Conflict(
            "Users.UserNotAllowedToChangeEmail",
            "The user cannot change the email address.");

    public static readonly Error EmailAlreadyInUseByUser =
        Error.Conflict(
            "Users.EmailAlreadyInUseByUser",
            "The specified email address is already assigned to this user.");

    // Password
    public static readonly Error UserNotAllowedToChangePassword =
        Error.Conflict(
            "Users.UserNotAllowedToChangePassword",
            "The user cannot change the password.");

    public static readonly Error InvalidPassword =
        Error.Validation(
            "Users.InvalidPassword",
            "The password is invalid.");

    // MFA
    public static readonly Error InvalidMfaMethod =
        Error.Validation(
            "Users.InvalidMfaMethod",
            "The selected MFA method is invalid.");

    public static readonly Error MfaAlreadyEnabled =
        Error.Conflict(
            "Users.MfaAlreadyEnabled",
            "Multi-factor authentication is already enabled.");

    public static readonly Error MfaAlreadyDisabled =
        Error.Conflict(
            "Users.MfaAlreadyDisabled",
            "Multi-factor authentication is already disabled.");

    // Login
    public static readonly Error LoginNotAllowed =
        Error.Conflict(
            "Users.LoginNotAllowed",
            "The user cannot authenticate in the current state.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized(
            "Users.InvalidCredentials",
            "The provided credentials are invalid.");

    public static readonly Error InvalidLockoutDate =
        Error.Validation(
            "Users.InvalidLockoutDate",
            "The specified lockout expiration is invalid.");

    // Refresh Tokens
    public static readonly Error RefreshTokenNotFound =
        Error.NotFound(
            "Users.RefreshTokenNotFound",
            "The specified refresh token could not be found.");

    public static readonly Error RefreshTokenAlreadyRevoked =
        Error.Conflict(
            "Users.RefreshTokenAlreadyRevoked",
            "The refresh token has already been revoked.");

    public static readonly Error RefreshTokenExpired =
        Error.Conflict(
            "Users.RefreshTokenExpired",
            "The refresh token has expired.");

    // Generic
    public static readonly Error NotFound =
        Error.NotFound(
            "Users.NotFound",
            "The requested user could not be found.");
}