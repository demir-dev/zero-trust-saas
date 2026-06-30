using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Domain.Tenants;

public sealed class TenantMembership : Entity
{
    private TenantMembership()
    {
    }

    private TenantMembership(
        Guid id,
        Guid tenantId,
        Guid userId,
        bool isOwner,
        TenantMembershipStatus status,
        DateTime joinedAtUtc,
        Guid? invitedByUserId)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        IsOwner = isOwner;
        Status = status;
        JoinedAtUtc = joinedAtUtc;
        InvitedByUserId = invitedByUserId;
        CreatedAtUtc = joinedAtUtc;
    }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public bool IsOwner { get; private set; }

    public TenantMembershipStatus Status { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    public DateTime? AcceptedAtUtc { get; private set; }

    public Guid? InvitedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsActive => Status == TenantMembershipStatus.Active;

    public static Result<TenantMembership> Create(
        Guid tenantId,
        Guid userId,
        DateTime joinedAtUtc,
        bool isOwner = false,
        Guid? invitedByUserId = null)
    {
        if (tenantId == Guid.Empty)
            return Result<TenantMembership>.Failure(TenantMembershipErrors.InvalidTenantId);

        if (userId == Guid.Empty)
            return Result<TenantMembership>.Failure(TenantMembershipErrors.InvalidUserId);

        var status = invitedByUserId.HasValue
            ? TenantMembershipStatus.Pending
            : TenantMembershipStatus.Active;

        var membership = new TenantMembership(
            Guid.NewGuid(), tenantId, userId, isOwner, status, joinedAtUtc, invitedByUserId);

        if (status == TenantMembershipStatus.Active)
            membership.AcceptedAtUtc = joinedAtUtc;

        return Result<TenantMembership>.Success(membership);
    }

    public Result Accept(DateTime acceptedAtUtc)
    {
        if (Status != TenantMembershipStatus.Pending)
            return Result.Failure(TenantMembershipErrors.NotPending);

        Status = TenantMembershipStatus.Active;
        AcceptedAtUtc = acceptedAtUtc;
        UpdatedAtUtc = acceptedAtUtc;

        return Result.Success();
    }

    public Result Suspend(DateTime suspendedAtUtc)
    {
        if (Status == TenantMembershipStatus.Suspended)
            return Result.Failure(TenantMembershipErrors.AlreadySuspended);

        if (Status == TenantMembershipStatus.Removed)
            return Result.Failure(TenantMembershipErrors.AlreadyRemoved);

        Status = TenantMembershipStatus.Suspended;
        UpdatedAtUtc = suspendedAtUtc;

        return Result.Success();
    }

    public Result Reactivate(DateTime reactivatedAtUtc)
    {
        if (Status == TenantMembershipStatus.Active)
            return Result.Failure(TenantMembershipErrors.AlreadyActive);

        if (Status == TenantMembershipStatus.Removed)
            return Result.Failure(TenantMembershipErrors.AlreadyRemoved);

        Status = TenantMembershipStatus.Active;
        UpdatedAtUtc = reactivatedAtUtc;

        return Result.Success();
    }

    public Result Remove(DateTime removedAtUtc)
    {
        if (Status == TenantMembershipStatus.Removed)
            return Result.Failure(TenantMembershipErrors.AlreadyRemoved);

        Status = TenantMembershipStatus.Removed;
        UpdatedAtUtc = removedAtUtc;

        return Result.Success();
    }

    public Result PromoteToOwner()
    {
        if (IsOwner)
            return Result.Failure(TenantMembershipErrors.AlreadyOwner);

        IsOwner = true;

        return Result.Success();
    }

    public Result DemoteOwner()
    {
        if (!IsOwner)
            return Result.Failure(TenantMembershipErrors.NotOwner);

        IsOwner = false;

        return Result.Success();
    }
}
