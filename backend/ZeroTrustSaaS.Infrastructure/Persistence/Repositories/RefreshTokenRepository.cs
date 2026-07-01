using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Identity;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(AppDbContext dbContext) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash.Value == tokenHash, cancellationToken);
    }

    public Task<RefreshToken?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId
                      && rt.RevokedAtUtc == null
                      && rt.UsedAtUtc == null
                      && rt.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(rt => rt.IssuedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        return dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();
    }

    public void Update(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Update(refreshToken);
    }
}
