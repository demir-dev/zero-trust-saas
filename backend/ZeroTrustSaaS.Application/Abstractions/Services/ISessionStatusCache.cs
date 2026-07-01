namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface ISessionStatusCache
{
    Task<bool> IsActiveAsync(Guid sessionId, CancellationToken ct = default);
    void Invalidate(Guid sessionId);
}
