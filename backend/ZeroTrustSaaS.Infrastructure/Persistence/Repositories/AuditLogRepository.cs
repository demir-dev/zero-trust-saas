using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Audit;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(AppDbContext dbContext) : IAuditLogRepository
{
    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        return dbContext.AuditLogs.AddAsync(auditLog, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AuditLogs
            .Where(al => al.TenantId == tenantId)
            .OrderByDescending(al => al.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
