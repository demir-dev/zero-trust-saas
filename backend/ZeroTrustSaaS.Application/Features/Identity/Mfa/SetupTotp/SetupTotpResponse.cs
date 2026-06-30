namespace ZeroTrustSaaS.Application.Features.Identity.Mfa.SetupTotp;

public sealed record SetupTotpResponse(string Secret, string QrCodeUri);
