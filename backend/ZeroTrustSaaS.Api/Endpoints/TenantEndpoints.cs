using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class TenantEndpoints
{
    internal static void MapTenantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/tenants").WithTags("Tenants");

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
