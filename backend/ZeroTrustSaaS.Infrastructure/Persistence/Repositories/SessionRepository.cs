using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class SessionRepository(AppDbContext dbContext) : ISessionRepository
{
    public Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IReadOnlyList<Session>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.Sessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.LastSeenAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Session>> GetRecentByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        return await dbContext.Sessions
            .Where(s => s.UserId == userId
                && (s.Status == SessionStatus.Active
                    || (s.RevokedAtUtc.HasValue && s.RevokedAtUtc.Value >= cutoff)
                    || s.ExpiresAtUtc >= cutoff))
            .OrderByDescending(s => s.LastSeenAtUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Session>> GetActiveByDeviceIdAsync(Guid deviceId, CancellationToken ct = default)
    {
        return await dbContext.Sessions
            .Where(s => s.TrustedDeviceId == deviceId && s.Status == SessionStatus.Active)
            .ToListAsync(ct);
    }

    public Task AddAsync(Session session, CancellationToken ct = default)
    {
        return dbContext.Sessions.AddAsync(session, ct).AsTask();
    }

    public void Update(Session session)
    {
        dbContext.Sessions.Update(session);
    }

    public async Task<IReadOnlyList<Guid>> RevokeAllByUserIdAsync(
        Guid userId,
        DateTime revokedAtUtc,
        SessionRevocationReason reason,
        CancellationToken ct = default)
    {
        var sessions = await dbContext.Sessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke(revokedAtUtc, reason);

        return sessions.Select(s => s.Id).ToList();
    }
}
