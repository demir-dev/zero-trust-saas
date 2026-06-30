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
        TenantPlan plan,
        DateTime createdAtUtc)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Plan = plan;
        Status = TenantStatus.Provisioning;
        CreatedAtUtc = createdAtUtc;
    }

    public TenantName Name { get; private set; } = null!;

    public TenantSlug Slug { get; private set; } = null!;

    public TenantPlan Plan { get; private set; }

    public TenantStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public DateTime? ActivatedAtUtc { get; private set; }

    public DateTime? SuspendedAtUtc { get; private set; }

    public IReadOnlyCollection<TenantMembership> Memberships => _memberships.AsReadOnly();

    public bool IsActive => Status == TenantStatus.Active;

    public static Result<Tenant> Create(
        TenantName name,
        TenantSlug slug,
        TenantPlan plan,
        DateTime createdAtUtc)
    {
        var tenant = new Tenant(Guid.NewGuid(), name, slug, plan, createdAtUtc);

        return Result<Tenant>.Success(tenant);
    }

    public Result Activate(DateTime activatedAtUtc)
    {
        if (Status != TenantStatus.Provisioning && Status != TenantStatus.Suspended)
            return Result.Failure(TenantErrors.InvalidStatusTransition);

        Status = TenantStatus.Active;
        ActivatedAtUtc = activatedAtUtc;
        UpdatedAtUtc = activatedAtUtc;

        return Result.Success();
    }

    public Result Rename(TenantName name, DateTime updatedAtUtc)
    {
        if (Status == TenantStatus.Deleted)
            return Result.Failure(TenantErrors.AlreadyDeleted);

        Name = name;
        UpdatedAtUtc = updatedAtUtc;

        return Result.Success();
    }

    public Result ChangeSlug(TenantSlug slug, DateTime updatedAtUtc)
    {
        if (Status == TenantStatus.Deleted)
            return Result.Failure(TenantErrors.AlreadyDeleted);

        Slug = slug;
        UpdatedAtUtc = updatedAtUtc;

        return Result.Success();
    }

    public Result Suspend(DateTime suspendedAtUtc)
    {
        if (Status == TenantStatus.Suspended)
            return Result.Failure(TenantErrors.AlreadySuspended);

        if (Status == TenantStatus.Deleted)
            return Result.Failure(TenantErrors.AlreadyDeleted);

        Status = TenantStatus.Suspended;
        SuspendedAtUtc = suspendedAtUtc;
        UpdatedAtUtc = suspendedAtUtc;

        return Result.Success();
    }

    public Result Reactivate(DateTime reactivatedAtUtc)
    {
        if (Status == TenantStatus.Active)
            return Result.Failure(TenantErrors.AlreadyActive);

        if (Status == TenantStatus.Deleted)
            return Result.Failure(TenantErrors.CannotReactivateDeleted);

        Status = TenantStatus.Active;
        ActivatedAtUtc = reactivatedAtUtc;
        UpdatedAtUtc = reactivatedAtUtc;

        return Result.Success();
    }

    public Result Delete(DateTime deletedAtUtc)
    {
        if (Status == TenantStatus.Deleted)
            return Result.Failure(TenantErrors.AlreadyDeleted);

        Status = TenantStatus.Deleted;
        UpdatedAtUtc = deletedAtUtc;

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
