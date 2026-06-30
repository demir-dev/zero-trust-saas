using System.Security.Claims;
using ZeroTrustSaaS.Application.Abstractions.Services;

namespace ZeroTrustSaaS.Api.Services;

internal sealed class HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext
{
    public Guid UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public Guid? TenantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public IEnumerable<string> PlatformRoles =>
        httpContextAccessor.HttpContext?.User.FindAll("platform_role").Select(c => c.Value)
        ?? [];

    public bool IsPlatformUser =>
        httpContextAccessor.HttpContext?.User.FindAll("platform_role").Any() == true;

    public IEnumerable<string> Permissions =>
        httpContextAccessor.HttpContext?.User.FindAll("permission").Select(c => c.Value)
        ?? [];

    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
