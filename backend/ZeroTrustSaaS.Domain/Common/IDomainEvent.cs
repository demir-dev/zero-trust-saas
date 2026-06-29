namespace ZeroTrustSaaS.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}