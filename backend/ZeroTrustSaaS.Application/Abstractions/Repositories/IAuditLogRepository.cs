using ZeroTrustSaaS.Domain.Audit;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLog>> GetRecentAsync(
        int count,
        Guid? tenantId,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<int> CountByEventTypeAsync(
        SecurityEventType eventType,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
