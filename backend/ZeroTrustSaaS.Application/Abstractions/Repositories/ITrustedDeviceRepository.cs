using ZeroTrustSaaS.Domain.Devices;

namespace ZeroTrustSaaS.Application.Abstractions.Repositories;

public interface ITrustedDeviceRepository
{
    Task<TrustedDevice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TrustedDevice?> GetByFingerprintAsync(
        Guid userId,
        string fingerprint,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrustedDevice>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> CountByStatusAsync(
        DeviceStatus status,
        CancellationToken cancellationToken = default);

    Task<int> CountTotalAsync(CancellationToken cancellationToken = default);

    Task AddAsync(TrustedDevice device, CancellationToken cancellationToken = default);

    void Update(TrustedDevice device);
}
