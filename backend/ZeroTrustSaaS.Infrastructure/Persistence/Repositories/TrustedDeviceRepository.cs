using Microsoft.EntityFrameworkCore;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Domain.Devices;

namespace ZeroTrustSaaS.Infrastructure.Persistence.Repositories;

internal sealed class TrustedDeviceRepository(AppDbContext dbContext) : ITrustedDeviceRepository
{
    public Task<TrustedDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.TrustedDevices
            .FirstOrDefaultAsync(td => td.Id == id, cancellationToken);
    }

    public Task<TrustedDevice?> GetByFingerprintAsync(
        Guid userId,
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        return dbContext.TrustedDevices
            .FirstOrDefaultAsync(
                td => td.UserId == userId && td.ClientInfo.DeviceFingerprint.Value == fingerprint,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TrustedDevice>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TrustedDevices
            .Where(td => td.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrustedDevice>> GetByTenantIdAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TenantMemberships
            .Where(m => m.TenantId == tenantId)
            .Join(dbContext.TrustedDevices, m => m.UserId, d => d.UserId, (m, d) => d)
            .OrderByDescending(d => d.TrustedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByStatusAsync(
        DeviceStatus status,
        CancellationToken cancellationToken = default)
    {
        return dbContext.TrustedDevices.CountAsync(td => td.Status == status, cancellationToken);
    }

    public Task<int> CountByStatusAndTenantAsync(
        DeviceStatus status,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.TenantMemberships
            .Where(m => m.TenantId == tenantId)
            .Join(dbContext.TrustedDevices, m => m.UserId, d => d.UserId, (m, d) => d)
            .CountAsync(d => d.Status == status, cancellationToken);
    }

    public Task<int> CountTotalAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.TrustedDevices.CountAsync(cancellationToken);
    }

    public Task AddAsync(TrustedDevice device, CancellationToken cancellationToken = default)
    {
        return dbContext.TrustedDevices.AddAsync(device, cancellationToken).AsTask();
    }

    public void Update(TrustedDevice device)
    {
        dbContext.TrustedDevices.Update(device);
    }
}
