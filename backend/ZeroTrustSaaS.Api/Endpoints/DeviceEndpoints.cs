using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Features.Devices.TrustDevice;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class DeviceEndpoints
{
    internal static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/devices")
            .WithTags("Devices")
            .RequireAuthorization();

        group.MapPost("/trust", async (
            TrustDeviceRequest request,
            TrustDeviceCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var command = new TrustDeviceCommand(
                request.UserId,
                request.DeviceName,
                request.DeviceFingerprint,
                ip,
                request.Country,
                request.Browser,
                request.OperatingSystem);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Created($"/devices/{result.Value}", new { id = result.Value })
                : ApiErrors.Problem(result.Error);
        });
    }
}

internal sealed record TrustDeviceRequest(
    Guid UserId,
    string DeviceName,
    string DeviceFingerprint,
    string Country,
    string Browser,
    string OperatingSystem);
