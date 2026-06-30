using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Infrastructure.Settings;

namespace ZeroTrustSaaS.Infrastructure.Services;

internal sealed class JwtTokenGenerator(IOptions<JwtSettings> jwtSettings) : ITokenGenerator
{
    private readonly JwtSettings _settings = jwtSettings.Value;

    public string GenerateJwtToken(
        Guid userId,
        string email,
        string securityStamp,
        IEnumerable<string> platformRoles,
        Guid? tenantId,
        string? tenantRole,
        IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("security_stamp", securityStamp),
        };

        foreach (var role in platformRoles)
            claims.Add(new Claim("platform_role", role));

        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));

        if (!string.IsNullOrEmpty(tenantRole))
            claims.Add(new Claim("tenant_role", tenantRole));

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshTokenValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
