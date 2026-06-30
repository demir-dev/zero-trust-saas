using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Tenants.Errors;

public static class TenantErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Tenants.Tenant.NotFound", "Tenant was not found.");

    public static readonly Error NameRequired =
        Error.Validation("Tenants.Tenant.NameRequired", "Tenant name is required.");

    public static readonly Error NameTooLong =
        Error.Validation("Tenants.Tenant.NameTooLong",
            $"Tenant name must not exceed {TenantName.MaxLength} characters.");

    public static readonly Error SlugRequired =
        Error.Validation("Tenants.Tenant.SlugRequired", "Tenant slug is required.");

    public static readonly Error InvalidSlug =
        Error.Validation("Tenants.Tenant.InvalidSlug",
            "Tenant slug must be 3–50 lowercase alphanumeric characters or hyphens, " +
            "and must not start or end with a hyphen.");

    public static readonly Error SlugAlreadyExists =
        Error.Conflict("Tenants.Tenant.SlugAlreadyExists", "A tenant with this slug already exists.");

    public static readonly Error AlreadyActive =
        Error.Conflict("Tenants.Tenant.AlreadyActive", "Tenant is already active.");

    public static readonly Error AlreadySuspended =
        Error.Conflict("Tenants.Tenant.AlreadySuspended", "Tenant is already suspended.");

    public static readonly Error AlreadyDisabled =
        Error.Conflict("Tenants.Tenant.AlreadyDisabled", "Tenant is already disabled.");

    public static readonly Error CannotReactivateDisabled =
        Error.Conflict("Tenants.Tenant.CannotReactivateDisabled",
            "A disabled tenant cannot be reactivated directly. Use Restore instead.");

    public static readonly Error MembershipAlreadyExists =
        Error.Conflict("Tenants.Tenant.MembershipAlreadyExists",
            "This user is already a member of the tenant.");

    public static readonly Error MembershipNotFound =
        Error.NotFound("Tenants.Tenant.MembershipNotFound", "Membership was not found.");

    public static readonly Error OwnerCannotBeRemoved =
        Error.Conflict("Tenants.Tenant.OwnerCannotBeRemoved",
            "The owner cannot be removed. Transfer ownership first.");

    public static readonly Error InvalidStatusTransition =
        Error.Conflict("Tenants.Tenant.InvalidStatusTransition",
            "The tenant is not in a state that allows this transition.");
}
