using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Application.Features.Identity.Login;

public sealed record LoginResponse(
    string? AccessToken,
    string? RefreshToken,
    LoginResult Result,
    bool RequiresMfa,
    Guid UserId,
    bool IsPlatformUser = false);
