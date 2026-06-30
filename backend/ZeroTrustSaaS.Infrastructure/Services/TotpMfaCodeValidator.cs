using OtpNet;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class TotpMfaCodeValidator : IMfaCodeValidator
{
    public bool Validate(string secret, string code, MfaMethod method)
    {
        if (method != MfaMethod.Totp)
            return false;

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(secret))
            return false;

        try
        {
            var keyBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(keyBytes);
            return totp.VerifyTotp(
                code.Trim(),
                out _,
                window: new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }
}
