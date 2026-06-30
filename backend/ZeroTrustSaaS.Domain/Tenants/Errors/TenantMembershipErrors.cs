using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Domain.Tenants.Errors;

public static class TenantMembershipErrors
{
    public static readonly Error InvalidTenantId =
        Error.Validation("Tenants.Membership.InvalidTenantId", "Tenant ID is required.");

    public static readonly Error InvalidUserId =
        Error.Validation("Tenants.Membership.InvalidUserId", "User ID is required.");

    public static readonly Error AlreadyOwner =
        Error.Conflict("Tenants.Membership.AlreadyOwner", "This member is already the owner.");

    public static readonly Error NotOwner =
        Error.Conflict("Tenants.Membership.NotOwner", "This member is not the owner.");

    public static readonly Error NotPending =
        Error.Conflict("Tenants.Membership.NotPending", "Membership is not in Pending state.");

    public static readonly Error AlreadySuspended =
        Error.Conflict("Tenants.Membership.AlreadySuspended", "Membership is already suspended.");

    public static readonly Error AlreadyActive =
        Error.Conflict("Tenants.Membership.AlreadyActive", "Membership is already active.");

    public static readonly Error AlreadyRemoved =
        Error.Conflict("Tenants.Membership.AlreadyRemoved", "Membership has been removed.");

    public static readonly Error NotFound =
        Error.NotFound("Tenants.Membership.NotFound", "Membership was not found.");
}
