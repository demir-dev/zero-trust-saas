using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Authorization.AssignPermissionToRole;
using ZeroTrustSaaS.Application.Features.Authorization.AssignRole;
using ZeroTrustSaaS.Application.Features.Authorization.CloneRole;
using ZeroTrustSaaS.Application.Features.Authorization.CreateRole;
using ZeroTrustSaaS.Application.Features.Authorization.DeleteRole;
using ZeroTrustSaaS.Application.Features.Authorization.GetPermissions;
using ZeroTrustSaaS.Application.Features.Authorization.GetRoles;
using ZeroTrustSaaS.Application.Features.Authorization.RemovePermissionFromRole;
using ZeroTrustSaaS.Application.Features.Authorization.RenameRole;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class AuthorizationEndpoints
{
    internal static void MapAuthorizationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/authorization")
            .WithTags("Authorization")
            .RequireAuthorization();

        group.MapGet("/roles", async (
            GetRolesQueryHandler handler,
            Guid? tenantId,
            CancellationToken ct) =>
        {
            var query = new GetRolesQuery(tenantId);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/permissions", async (
            GetPermissionsQueryHandler handler,
            CancellationToken ct) =>
        {
            var query = new GetPermissionsQuery();
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/roles", async (
            CreateRoleRequest request,
            CreateRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new CreateRoleCommand(request.Name, request.TenantId, request.Scope);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Created($"/authorization/roles/{result.Value}", new { id = result.Value })
                : ApiErrors.Problem(result.Error);
        });

        group.MapDelete("/roles/{id:guid}", async (
            Guid id,
            ICurrentUserContext currentUser,
            DeleteRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new DeleteRoleCommand(id, currentUser.UserId, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPut("/roles/{id:guid}", async (
            Guid id,
            RenameRoleRequest request,
            ICurrentUserContext currentUser,
            RenameRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RenameRoleCommand(id, request.Name, currentUser.UserId, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/roles/{id:guid}/clone", async (
            Guid id,
            CloneRoleRequest request,
            ICurrentUserContext currentUser,
            CloneRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new CloneRoleCommand(id, request.Name, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Created($"/authorization/roles/{result.Value}", new { id = result.Value })
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/roles/{id:guid}/permissions", async (
            Guid id,
            AssignPermissionRequest request,
            AssignPermissionToRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new AssignPermissionToRoleCommand(id, request.PermissionCode);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapDelete("/roles/{id:guid}/permissions/{code}", async (
            Guid id,
            string code,
            ICurrentUserContext currentUser,
            RemovePermissionFromRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RemovePermissionFromRoleCommand(id, code, currentUser.UserId, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

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

internal sealed record CreateRoleRequest(
    string Name,
    Guid? TenantId,
    string Scope = "Tenant");

internal sealed record AssignPermissionRequest(string PermissionCode);

internal sealed record RenameRoleRequest(string Name);

internal sealed record CloneRoleRequest(string Name);
