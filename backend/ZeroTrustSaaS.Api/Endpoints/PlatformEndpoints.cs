using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Features.Platform.CheckPlatformStatus;
using ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class PlatformEndpoints
{
    internal static void MapPlatformEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platform").WithTags("Platform");

        group.MapGet("/status", async (
            CheckPlatformStatusQueryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CheckPlatformStatusQuery(), ct);
            return result.IsSuccess
                ? Results.Ok(new PlatformStatusResponse(result.Value))
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/initialize", async (
            InitializePlatformRequest request,
            InitializePlatformCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new InitializePlatformCommand(
                request.OrganizationName,
                request.OrganizationSlug,
                request.AdminFirstName,
                request.AdminLastName,
                request.AdminEmail,
                request.AdminPassword);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { organizationId = result.Value })
                : ApiErrors.Problem(result.Error);
        });
    }
}

internal sealed record InitializePlatformRequest(
    string OrganizationName,
    string OrganizationSlug,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword);
