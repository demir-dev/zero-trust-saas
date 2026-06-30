using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface IMfaCodeValidator
{
    bool Validate(string secret, string code, MfaMethod method);
}
