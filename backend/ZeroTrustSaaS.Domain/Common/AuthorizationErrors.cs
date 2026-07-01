namespace ZeroTrustSaaS.Domain.Common;

public static class AuthorizationErrors
{
    public static readonly Error InsufficientPermissions =
        Error.Forbidden("Authorization.InsufficientPermissions",
            "You do not have the required permissions to perform this action.");

    public static readonly Error InsufficientHierarchyLevel =
        Error.Forbidden("Authorization.InsufficientHierarchyLevel",
            "You cannot perform this action on a user with equal or higher role level.");
}
