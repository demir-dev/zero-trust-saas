using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Application.Features.Identity.Mfa;

public sealed record EnableMfaCommand(
    Guid UserId,
    MfaMethod Method,
    string Secret);
