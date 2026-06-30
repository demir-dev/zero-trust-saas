using OtpNet;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.Mfa.SetupTotp;

public sealed class SetupTotpQueryHandler(IUserRepository userRepository)
{
    public async Task<Result<SetupTotpResponse>> Handle(
        SetupTotpQuery query,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
            return Result<SetupTotpResponse>.Failure(UserErrors.NotFound);

        var keyBytes = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(keyBytes);

        var uri = $"otpauth://totp/ZeroTrust%3A{Uri.EscapeDataString(user.Email.Value)}" +
                  $"?secret={base32Secret}&issuer=ZeroTrust&algorithm=SHA1&digits=6&period=30";

        return Result<SetupTotpResponse>.Success(new SetupTotpResponse(base32Secret, uri));
    }
}
