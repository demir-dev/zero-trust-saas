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
}
