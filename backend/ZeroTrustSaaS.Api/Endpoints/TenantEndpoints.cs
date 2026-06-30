using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenant;
using ZeroTrustSaaS.Application.Features.Tenants.GetTenants;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class TenantEndpoints
{
    internal static void MapTenantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenants")
            .WithTags("Tenants")
            .RequireAuthorization();

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
        });

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
        });
    }
}
