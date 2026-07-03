using ZeroTrustSaaS.Domain.Sessions;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Session>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns active sessions plus recently revoked/expired ones (within last 7 days),
    /// so the user's sessions page shows a meaningful history.
    /// </summary>
    Task<IReadOnlyList<Session>> GetRecentByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<Session>> GetActiveByDeviceIdAsync(Guid deviceId, CancellationToken ct = default);

    Task AddAsync(Session session, CancellationToken ct = default);

    void Update(Session session);

    /// <summary>
    /// Bulk-revokes all active sessions for a user and returns the IDs of all revoked sessions
    /// so callers can perform per-session cache invalidation.
    /// </summary>
    Task<IReadOnlyList<Guid>> RevokeAllByUserIdAsync(
        Guid userId,
        DateTime revokedAtUtc,
        SessionRevocationReason reason,
        CancellationToken ct = default);
}
