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

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        return dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();
    }

    public void Update(RefreshToken refreshToken)
    {
        dbContext.RefreshTokens.Update(refreshToken);
    }
}
