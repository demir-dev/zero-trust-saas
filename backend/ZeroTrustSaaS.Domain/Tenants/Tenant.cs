using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Domain.Tenants;

public sealed class Tenant : AggregateRoot
{
    private readonly List<TenantMembership> _memberships = [];

    private Tenant()
    {
    }

    private Tenant(
        Guid id,
        TenantName name,
        TenantSlug slug,
        DateTime createdAtUtc)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Status = TenantStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public TenantName Name { get; private set; } = null!;

    public TenantSlug Slug { get; private set; } = null!;

    public TenantStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<TenantMembership> Memberships => _memberships.AsReadOnly();

    public bool IsActive => Status == TenantStatus.Active;

    public static Result<Tenant> Create(
        TenantName name,
        TenantSlug slug,
        DateTime createdAtUtc)
    {
        var tenant = new Tenant(Guid.NewGuid(), name, slug, createdAtUtc);

        return Result<Tenant>.Success(tenant);
    }

    public Result Rename(TenantName name)
    {
        if (Status == TenantStatus.Disabled)
            return Result.Failure(TenantErrors.AlreadyDisabled);

        Name = name;

        return Result.Success();
    }

    public Result ChangeSlug(TenantSlug slug)
    {
        if (Status == TenantStatus.Disabled)
            return Result.Failure(TenantErrors.AlreadyDisabled);

        Slug = slug;

        return Result.Success();
    }

    public Result Suspend()
    {
        if (Status == TenantStatus.Suspended)
            return Result.Failure(TenantErrors.AlreadySuspended);

        if (Status == TenantStatus.Disabled)
            return Result.Failure(TenantErrors.AlreadyDisabled);

        Status = TenantStatus.Suspended;

        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status == TenantStatus.Active)
            return Result.Failure(TenantErrors.AlreadyActive);

        if (Status == TenantStatus.Disabled)
            return Result.Failure(TenantErrors.CannotReactivateDisabled);

        Status = TenantStatus.Active;

        return Result.Success();
    }

    public Result Disable()
    {
        if (Status == TenantStatus.Disabled)
            return Result.Failure(TenantErrors.AlreadyDisabled);

        Status = TenantStatus.Disabled;

        return Result.Success();
    }

    public Result AddMembership(TenantMembership membership)
    {
        if (_memberships.Any(m => m.UserId == membership.UserId))
            return Result.Failure(TenantErrors.MembershipAlreadyExists);

        _memberships.Add(membership);

        return Result.Success();
    }

    public Result RemoveMembership(Guid userId)
    {
        var membership = _memberships.FirstOrDefault(m => m.UserId == userId);

        if (membership is null)
            return Result.Failure(TenantErrors.MembershipNotFound);

        if (membership.IsOwner)
            return Result.Failure(TenantErrors.OwnerCannotBeRemoved);

        _memberships.Remove(membership);

        return Result.Success();
    }

    public Result TransferOwnership(Guid fromUserId, Guid toUserId)
    {
        var currentOwner = _memberships.FirstOrDefault(m => m.UserId == fromUserId);

        if (currentOwner is null)
            return Result.Failure(TenantErrors.MembershipNotFound);

        if (!currentOwner.IsOwner)
            return Result.Failure(TenantMembershipErrors.NotOwner);

        var newOwner = _memberships.FirstOrDefault(m => m.UserId == toUserId);

        if (newOwner is null)
            return Result.Failure(TenantErrors.MembershipNotFound);

        currentOwner.DemoteOwner();
        newOwner.PromoteToOwner();

        return Result.Success();
    }
}
