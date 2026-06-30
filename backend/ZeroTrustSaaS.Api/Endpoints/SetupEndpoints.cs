using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Features.Platform.CheckPlatformStatus;
using ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class SetupEndpoints
{
    internal static void MapSetupEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/setup").WithTags("Setup");

        group.MapGet("/status", async (
            CheckPlatformStatusQueryHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new CheckPlatformStatusQuery(), ct);
            return result.IsSuccess
                ? Results.Ok(new { isInitialized = result.Value })
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/", async (
            SetupRequest request,
            InitializePlatformCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new InitializePlatformCommand(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Ok(new { userId = result.Value })
                : ApiErrors.Problem(result.Error);
        });
    }
}

internal sealed record SetupRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password);
