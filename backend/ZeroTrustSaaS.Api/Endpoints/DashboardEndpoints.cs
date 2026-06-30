using ZeroTrustSaaS.Api.Helpers;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Features.Audit.GetAuditLogs;
using ZeroTrustSaaS.Application.Features.Dashboard.GetSecurityOverview;

namespace ZeroTrustSaaS.Api.Endpoints;

internal static class DashboardEndpoints
{
    internal static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/security-overview", async (
            GetSecurityOverviewQueryHandler handler,
            ICurrentUserContext currentUser,
            CancellationToken ct) =>
        {
            var query = new GetSecurityOverviewQuery(currentUser.TenantId);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });

        group.MapGet("/audit", async (
            GetAuditLogsQueryHandler handler,
            ICurrentUserContext currentUser,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            var query = new GetAuditLogsQuery(currentUser.TenantId, null, page, pageSize);
            var result = await handler.Handle(query, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiErrors.Problem(result.Error);
        });
    }
}
