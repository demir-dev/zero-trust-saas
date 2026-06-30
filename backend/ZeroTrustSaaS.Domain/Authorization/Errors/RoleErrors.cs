using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Authorization.Errors;

public static class RoleErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Authorization.Role.NotFound", "Role was not found.");

    public static readonly Error NameRequired =
        Error.Validation("Authorization.Role.NameRequired", "Role name is required.");

    public static readonly Error NameTooLong =
        Error.Validation("Authorization.Role.NameTooLong",
            $"Role name must not exceed {RoleName.MaxLength} characters.");

    public static readonly Error PermissionAlreadyAssigned =
        Error.Conflict("Authorization.Role.PermissionAlreadyAssigned",
            "This permission is already assigned to the role.");

    public static readonly Error PermissionNotAssigned =
        Error.NotFound("Authorization.Role.PermissionNotAssigned",
            "This permission is not assigned to the role.");

    public static readonly Error SystemRoleCannotBeModified =
        Error.Conflict("Authorization.Role.SystemRoleCannotBeModified",
            "System roles cannot be modified.");

    public static readonly Error CannotDeleteSystemRole =
        Error.Conflict("Authorization.Role.CannotDeleteSystemRole",
            "System roles cannot be deleted.");
}
