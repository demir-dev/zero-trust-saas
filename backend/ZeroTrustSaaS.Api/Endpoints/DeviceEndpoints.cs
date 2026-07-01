using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Devices.BlockDevice;
using ZeroTrustSaaS.Application.Features.Devices.GetDevices;
using ZeroTrustSaaS.Application.Features.Devices.RevokeDevice;
using ZeroTrustSaaS.Application.Features.Devices.TrustDevice;
using ZeroTrustSaaS.Application.Features.Devices.TrustDeviceById;
using ZeroTrustSaaS.Application.Features.Devices.UnblockDevice;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class DeviceEndpoints
{
    internal static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/devices")
            .WithTags("Devices")
            .RequireAuthorization();

        group.MapGet("/", async (
            GetDevicesQueryHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            var query = new GetDevicesQuery(currentUser.UserId);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

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

        group.MapPost("/{id:guid}/trust", async (
            Guid id,
            TrustDeviceByIdCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new TrustDeviceByIdCommand(id);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/unblock", async (
            Guid id,
            UnblockDeviceCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new UnblockDeviceCommand(id);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/revoke", async (
            Guid id,
            RevokeDeviceCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RevokeDeviceCommand(id, DateTime.UtcNow);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/block", async (
            Guid id,
            BlockDeviceCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new BlockDeviceCommand(id);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
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
