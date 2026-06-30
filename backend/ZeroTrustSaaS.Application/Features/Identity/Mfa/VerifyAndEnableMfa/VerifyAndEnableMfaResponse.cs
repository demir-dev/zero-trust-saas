namespace ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyAndEnableMfa;

public sealed record VerifyAndEnableMfaResponse(IReadOnlyList<string> RecoveryCodes);
