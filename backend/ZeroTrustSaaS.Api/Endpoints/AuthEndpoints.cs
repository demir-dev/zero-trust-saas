using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Features.Identity.Login;
using ZeroTrustSaaS.Application.Features.Identity.Mfa;
using ZeroTrustSaaS.Application.Features.Identity.RefreshToken;
using ZeroTrustSaaS.Application.Features.Identity.Register;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            RegisterUserCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new RegisterUserCommand(
                request.TenantId,
                request.Email,
                request.Password);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Created($"/users/{result.Value}", new { id = result.Value })
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/login", async (
            LoginRequest request,
            LoginCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var command = new LoginCommand(
                request.TenantId,
                request.Email,
                request.Password,
                ip,
                userAgent,
                request.DeviceFingerprint,
                request.Country,
                request.Browser,
                request.OperatingSystem);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/refresh", async (
            RefreshRequest request,
            RefreshTokenCommandHandler handler,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            var command = new RefreshTokenCommand(
                request.RefreshToken,
                ip,
                userAgent,
                request.DeviceFingerprint,
                request.Country,
                request.Browser,
                request.OperatingSystem);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/mfa/enable", async (
            EnableMfaRequest request,
            EnableMfaCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new EnableMfaCommand(
                request.UserId,
                request.Method,
                request.Secret);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        }).RequireAuthorization();

        group.MapPost("/mfa/disable", async (
            DisableMfaRequest request,
            DisableMfaCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new DisableMfaCommand(request.UserId);

            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        }).RequireAuthorization();
    }
}

internal sealed record RegisterRequest(
    Guid TenantId,
    string Email,
    string Password);

internal sealed record LoginRequest(
    Guid TenantId,
    string Email,
    string Password,
    string DeviceFingerprint,
    string Country,
    string Browser,
    string OperatingSystem);

internal sealed record RefreshRequest(
    string RefreshToken,
    string DeviceFingerprint,
    string Country,
    string Browser,
    string OperatingSystem);

internal sealed record EnableMfaRequest(
    Guid UserId,
    ZeroTrustSaaS.Domain.Security.Enums.MfaMethod Method,
    string Secret);

internal sealed record DisableMfaRequest(Guid UserId);
