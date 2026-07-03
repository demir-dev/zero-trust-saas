using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Sessions.Errors;

public static class SessionErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Sessions.NotFound", "The specified session could not be found.");

    public static readonly Error AlreadyRevoked =
        Error.Conflict("Sessions.AlreadyRevoked", "The session has already been revoked.");

    public static readonly Error Inactive =
        Error.Conflict("Sessions.Inactive", "The session is not active.");

    public static readonly Error NotOwnedByUser =
        Error.Forbidden("Sessions.NotOwnedByUser", "The session does not belong to the specified user.");
}
