using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenant;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenants;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class TenantEndpoints
{
    internal static void MapTenantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenants").WithTags("Tenants");

        group.MapGet("/", async (
            GetTenantsQueryHandler handler,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var query = new GetTenantsQuery(page, pageSize);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        }).RequireAuthorization();

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetTenantQueryHandler handler,
            CancellationToken ct) =>
        {
            var query = new GetTenantQuery(id);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        }).RequireAuthorization();

        group.MapPost("/", async (
            CreateTenantRequest request,
            CreateTenantCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new CreateTenantCommand(
                request.Name,
                request.Slug,
                request.OwnerUserId);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Created($"/tenants/{result.Value}", new { id = result.Value })
                : ApiErrors.Problem(result.Error);
        });
    }
}

internal sealed record CreateTenantRequest(
    string Name,
    string Slug,
    Guid OwnerUserId);
