using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Identity.GetCurrentUser;
using ZeroTrustSaaS.Application.Features.Identity.GetUsers;
using ZeroTrustSaaS.Application.Features.Identity.LockUser;
using ZeroTrustSaaS.Application.Features.Identity.UnlockUser;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class UserEndpoints
{
    internal static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", async (
            GetCurrentUserQueryHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            var query = new GetCurrentUserQuery(currentUser.UserId);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/", async (
            GetUsersQueryHandler handler,
            ICurrentUserContext currentUser,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var query = new GetUsersQuery(currentUser.TenantId, page, pageSize);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/lock", async (
            Guid id,
            LockUserCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new LockUserCommand(id, DateTime.UtcNow);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });

        group.MapPost("/{id:guid}/unlock", async (
            Guid id,
            UnlockUserCommandHandler handler,
            CancellationToken ct) =>
        {
            var command = new UnlockUserCommand(id, DateTime.UtcNow);
            var result = await handler.Handle(command, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : ApiErrors.Problem(result.Error);
        });
    }
}
