using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Audit.GetAuditLogs;
using ZeroTrustSaaS.Application.Features.Authorization.AssignRole;
using ZeroTrustSaaS.Application.Features.Authorization.RevokeUserRole;
using ZeroTrustSaaS.Application.Features.Devices.GetDevices;
using ZeroTrustSaaS.Application.Features.Identity.ActivateUser;
using ZeroTrustSaaS.Application.Features.Identity.CreateTenantUser;
using ZeroTrustSaaS.Application.Features.Identity.ForcePasswordReset;
using ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;
using ZeroTrustSaaS.Application.Features.Identity.GetUserDetails;
using ZeroTrustSaaS.Application.Features.Identity.GetUserSessions;
using ZeroTrustSaaS.Application.Features.Identity.GetUsers;
using ZeroTrustSaaS.Application.Features.Identity.LockUser;
using ZeroTrustSaaS.Application.Features.Identity.Mfa;
using ZeroTrustSaaS.Application.Features.Identity.RevokeUserSession;
using ZeroTrustSaaS.Application.Features.Identity.RevokeUserSessions;
using ZeroTrustSaaS.Application.Features.Identity.SuspendUser;
using ZeroTrustSaaS.Application.Features.Identity.UnlockUser;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class UserEndpoints
{
    internal static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", async (
            GetCurrentUserQueryHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            var query = new GetCurrentUserQuery(currentUser.UserId);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/", async (
            GetUsersQueryHandler handler,
            ICurrentUserContext currentUser,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            if (!currentUser.TenantId.HasValue)
                return Results.Forbid();

            var query = new GetUsersQuery(currentUser.TenantId.Value, page, pageSize);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/", async (
            CreateTenantUserRequest request,
            ICurrentUserContext currentUser,
            CreateTenantUserCommandHandler handler,
            CancellationToken ct) =>
        {
            if (!currentUser.TenantId.HasValue)
                return Results.Forbid();

            var command = new CreateTenantUserCommand(
                currentUser.TenantId.Value,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password,
                request.RoleId);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Created($"/users/{result.Value}", new { id = result.Value })
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ICurrentUserContext currentUser,
            GetUserDetailsQueryHandler handler,
            CancellationToken ct) =>
        {
            var query = new GetUserDetailsQuery(id, currentUser.TenantId);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/lock", async (
            Guid id,
            LockUserCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new LockUserCommand(id, DateTime.UtcNow);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/unlock", async (
            Guid id,
            UnlockUserCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new UnlockUserCommand(id, DateTime.UtcNow);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/suspend", async (
            Guid id,
            ICurrentUserContext currentUser,
            SuspendUserCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new SuspendUserCommand(id, currentUser.UserId, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            ICurrentUserContext currentUser,
            ActivateUserCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new ActivateUserCommand(id, currentUser.UserId, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/revoke-sessions", async (
            Guid id,
            ICurrentUserContext currentUser,
            RevokeUserSessionsCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RevokeUserSessionsCommand(id, currentUser.UserId, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/force-password-reset", async (
            Guid id,
            ICurrentUserContext currentUser,
            ForcePasswordResetCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new ForcePasswordResetCommand(id, currentUser.UserId, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/disable-mfa", async (
            Guid id,
            ICurrentUserContext currentUser,
            DisableMfaCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new DisableMfaCommand(id);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/assign-role", async (
            Guid id,
            AssignRoleToUserRequest request,
            ICurrentUserContext currentUser,
            AssignRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new AssignRoleCommand(id, request.RoleId, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/revoke-role", async (
            Guid id,
            RevokeRoleFromUserRequest request,
            ICurrentUserContext currentUser,
            RevokeUserRoleCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RevokeUserRoleCommand(id, request.RoleId, currentUser.TenantId, currentUser.UserId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/{id:guid}/sessions", async (
            Guid id,
            ICurrentUserContext currentUser,
            GetUserSessionsQueryHandler handler,
            CancellationToken ct) =>
        {
            var query = new GetUserSessionsQuery(id, currentUser.TenantId);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/sessions/{sid:guid}/revoke", async (
            Guid id,
            Guid sid,
            ICurrentUserContext currentUser,
            RevokeUserSessionCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RevokeUserSessionCommand(id, sid, currentUser.TenantId);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/{id:guid}/devices", async (
            Guid id,
            GetDevicesQueryHandler handler,
            CancellationToken ct) =>
        {
            var query = new GetDevicesQuery(id);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/{id:guid}/audit", async (
            Guid id,
            ICurrentUserContext currentUser,
            GetAuditLogsQueryHandler handler,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var query = new GetAuditLogsQuery(currentUser.TenantId, id, page, pageSize);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });
    }
}

internal sealed record CreateTenantUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid? RoleId = null);

internal sealed record AssignRoleToUserRequest(Guid RoleId);

internal sealed record RevokeRoleFromUserRequest(Guid RoleId);
