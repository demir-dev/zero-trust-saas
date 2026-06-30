namespace ZeroTrustSaaS.Application.Features.Identity.RefreshToken;

public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken);
