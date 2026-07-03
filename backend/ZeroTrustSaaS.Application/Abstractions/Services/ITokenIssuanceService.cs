using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Identity;

namespace ZeroTrustSaaS.Application.Abstractions.Services;

public sealed record TokenIssuanceResult(
    string AccessToken,
    string RawRefreshToken,
    Guid SessionId);

public interface ITokenIssuanceService
{
    Task<Result<TokenIssuanceResult>> IssueAsync(
        User user,
        Guid? tenantId,
        Guid? deviceId,
        ClientInfo clientInfo,
        string? userAgent,
        DateTime now,
        CancellationToken ct = default);
}
