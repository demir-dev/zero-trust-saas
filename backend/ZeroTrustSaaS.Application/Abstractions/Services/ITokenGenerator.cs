namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface ITokenGenerator
{
    string GenerateJwtToken(
        Guid userId,
        string email,
        string securityStamp,
        IEnumerable<string> platformRoles,
        Guid? tenantId,
        string? tenantRole,
        IEnumerable<string> permissions,
        Guid? sessionId = null,
        Guid? deviceId = null);

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
