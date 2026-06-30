using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization.Errors;

public static class PermissionErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Authorization.Permission.NotFound", "Permission was not found.");

    public static readonly Error CodeRequired =
        Error.Validation("Authorization.Permission.CodeRequired", "Permission code is required.");

    public static readonly Error InvalidCodeFormat =
        Error.Validation("Authorization.Permission.InvalidCodeFormat",
            "Permission code must be dot-separated lowercase segments, e.g. 'users.read'.");

    public static readonly Error DescriptionRequired =
        Error.Validation("Authorization.Permission.DescriptionRequired",
            "Permission description is required.");
}
