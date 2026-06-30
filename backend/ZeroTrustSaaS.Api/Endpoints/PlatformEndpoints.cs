using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Audit.GetAuditLogs;
using ZeroTrustSaaS.Application.Features.Authorization.GetRoles;
using ZeroTrustSaaS.Application.Features.Devices.GetTenantDevices;
using ZeroTrustSaaS.Application.Features.Identity.ActivateUser;
using ZeroTrustSaaS.Application.Features.Identity.GetUsers;
using ZeroTrustSaaS.Application.Features.Identity.LockUser;
using ZeroTrustSaaS.Application.Features.Identity.Mfa;
using ZeroTrustSaaS.Application.Features.Identity.RevokeUserSessions;
using ZeroTrustSaaS.Application.Features.Identity.SuspendUser;
using ZeroTrustSaaS.Application.Features.Identity.UnlockUser;
using ZeroTrustSaaS.Application.Features.Tenants.ActivateTenant;
using ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenant;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenants;
using ZeroTrustSaaS.Application.Features.Tenants.SuspendTenant;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class PlatformEndpoints
{
    internal static void MapPlatformEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platform")
            .WithTags("Platform")
            .RequireAuthorization();

        group.MapGet("/tenants", async (
            GetTenantsQueryHandler handler,
            ICurrentUserContext currentUser,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var query = new GetTenantsQuery(page, pageSize);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants", async (
            CreateTenantRequest request,
            CreateTenantCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var command = new CreateTenantCommand(
                request.Name,
                request.Slug,
                request.OwnerFirstName,
                request.OwnerLastName,
                request.OwnerEmail,
                request.OwnerPassword);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { tenantId = result.Value })
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/tenants/{id:guid}", async (
            Guid id,
            GetTenantQueryHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct = default) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new GetTenantQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants/{id:guid}/suspend", async (
            Guid id,
            SuspendTenantCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new SuspendTenantCommand(id), ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants/{id:guid}/activate", async (
            Guid id,
            ActivateTenantCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new ActivateTenantCommand(id), ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        // --- Tenant inspection endpoints ---

        group.MapGet("/tenants/{id:guid}/users", async (
            Guid id,
            GetUsersQueryHandler handler,
            ICurrentUserContext currentUser,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new GetUsersQuery(id, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/tenants/{id:guid}/roles", async (
            Guid id,
            GetRolesQueryHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct = default) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new GetRolesQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/tenants/{id:guid}/audit", async (
            Guid id,
            GetAuditLogsQueryHandler handler,
            ICurrentUserContext currentUser,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new GetAuditLogsQuery(TenantId: id, Page: page, PageSize: pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/tenants/{id:guid}/devices", async (
            Guid id,
            GetTenantDevicesQueryHandler handler,
            ICurrentUserContext currentUser,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new GetTenantDevicesQuery(id, page, pageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants/{id:guid}/users/{userId:guid}/lock", async (
            Guid id,
            Guid userId,
            LockUserCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new LockUserCommand(userId, DateTime.UtcNow), ct);
            return result.IsSuccess ? Results.NoContent() : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants/{id:guid}/users/{userId:guid}/unlock", async (
            Guid id,
            Guid userId,
            UnlockUserCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new UnlockUserCommand(userId, DateTime.UtcNow), ct);
            return result.IsSuccess ? Results.NoContent() : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants/{id:guid}/users/{userId:guid}/suspend", async (
            Guid id,
            Guid userId,
            SuspendUserCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new SuspendUserCommand(userId, currentUser.UserId, id), ct);
            return result.IsSuccess ? Results.NoContent() : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants/{id:guid}/users/{userId:guid}/activate", async (
            Guid id,
            Guid userId,
            ActivateUserCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new ActivateUserCommand(userId, currentUser.UserId, id), ct);
            return result.IsSuccess ? Results.NoContent() : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants/{id:guid}/users/{userId:guid}/revoke-sessions", async (
            Guid id,
            Guid userId,
            RevokeUserSessionsCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new RevokeUserSessionsCommand(userId, currentUser.UserId, id), ct);
            return result.IsSuccess ? Results.NoContent() : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/tenants/{id:guid}/users/{userId:guid}/disable-mfa", async (
            Guid id,
            Guid userId,
            DisableMfaCommandHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.IsPlatformUser)
                return Results.Forbid();

            var result = await handler.Handle(new DisableMfaCommand(userId), ct);
            return result.IsSuccess ? Results.NoContent() : ApiErrors.Problem(result.Error);
        });
    }
}

internal sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    string OwnerPassword);
