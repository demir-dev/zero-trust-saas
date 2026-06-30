namespace ZeroTrustSaaS.Application.Features.Platform.CheckPlatformStatus;

public sealed record CheckPlatformStatusQuery;

public sealed record PlatformStatusResponse(bool IsInitialized);
