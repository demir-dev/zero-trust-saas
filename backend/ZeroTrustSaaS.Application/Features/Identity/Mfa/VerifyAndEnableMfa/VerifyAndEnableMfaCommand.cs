namespace ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyAndEnableMfa;

public sealed record VerifyAndEnableMfaCommand(
    Guid UserId,
    string Base32Secret,
    string VerificationCode);
