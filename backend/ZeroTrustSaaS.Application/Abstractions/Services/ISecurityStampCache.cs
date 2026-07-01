namespace ZeroTrustSaaS.Application.Abstractions.Services;

public interface ISecurityStampCache
{
    Task<string?> GetOrFetchStampAsync(Guid userId, CancellationToken ct = default);
    void Invalidate(Guid userId);
}
