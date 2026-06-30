using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization.Errors;

public static class UserRoleErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Authorization.UserRole.NotFound", "User role assignment was not found.");

    public static readonly Error AlreadyAssigned =
        Error.Conflict("Authorization.UserRole.AlreadyAssigned",
            "This role is already assigned to the user.");

    public static readonly Error InvalidUserId =
        Error.Validation("Authorization.UserRole.InvalidUserId", "User ID is required.");

    public static readonly Error InvalidRoleId =
        Error.Validation("Authorization.UserRole.InvalidRoleId", "Role ID is required.");
}
