using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class TotpMfaCodeValidator : IMfaCodeValidator
{
    public bool Validate(string secret, string code, MfaMethod method)
    {
        // TOTP validation requires a library like OtpNet.
        // This is a stub that returns true for the diploma demo.
        // Replace with real OtpNet implementation before production.
        return !string.IsNullOrWhiteSpace(code);
    }
}
