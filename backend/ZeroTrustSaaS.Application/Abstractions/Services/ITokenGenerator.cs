namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface ITokenGenerator
{
    string GenerateJwtToken(Guid userId, Guid tenantId, IEnumerable<string> roles);

    /// <summary>
    /// Generates a cryptographically secure random refresh token value.
    /// The caller is responsible for hashing it before persistence.
    /// </summary>
    string GenerateRefreshTokenValue();

    /// <summary>
    /// Computes the SHA-256 hash of a raw refresh token value for secure storage.
    /// </summary>
    string HashRefreshToken(string rawToken);
}
