namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface ITenantStatusCache
{
    Task<bool> IsActiveAsync(Guid tenantId, CancellationToken ct = default);
    void Invalidate(Guid tenantId);
}
