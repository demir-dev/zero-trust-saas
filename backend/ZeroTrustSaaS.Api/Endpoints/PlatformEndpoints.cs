using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Tenants.ActivateTenant;
using ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;
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
    }
}

internal sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    string OwnerPassword);
