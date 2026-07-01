using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Application.Abstractions.Services;

public static class CurrentUserContextExtensions
{
    public static bool HasPermission(this ICurrentUserContext ctx, string permissionCode)
        => ctx.IsPlatformUser || ctx.Permissions.Contains(permissionCode);

    public static Result RequirePermission(this ICurrentUserContext ctx, string permissionCode)
        => ctx.HasPermission(permissionCode)
            ? Result.Success()
            : Result.Failure(AuthorizationErrors.InsufficientPermissions);

    public static int GetTenantRoleLevel(this ICurrentUserContext ctx) =>
        ctx.IsPlatformUser ? 100 : WellKnownPermissions.GetRoleLevel(ctx.TenantRole ?? string.Empty);
}
