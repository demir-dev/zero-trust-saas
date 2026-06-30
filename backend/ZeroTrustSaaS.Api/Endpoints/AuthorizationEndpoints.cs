using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Features.Authorization.AssignRole;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class AuthorizationEndpoints
{
    internal static void MapAuthorizationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/authorization")
            .WithTags("Authorization")
            .RequireAuthorization();

        group.MapPost("/roles/assign", async (
            AssignRoleRequest request,
            AssignRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new AssignRoleCommand(
                request.UserId,
                request.RoleId,
                request.TenantId);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });
    }
}

internal sealed record AssignRoleRequest(
    Guid UserId,
    Guid RoleId,
    Guid? TenantId);
