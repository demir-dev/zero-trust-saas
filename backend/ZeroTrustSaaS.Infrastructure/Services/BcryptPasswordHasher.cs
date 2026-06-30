using ZeroTrustSaaS.Application.Abstractions.Services;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
