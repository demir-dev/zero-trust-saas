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
        DateTime joinedAtUtc,
        bool isOwner)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        JoinedAtUtc = joinedAtUtc;
        IsOwner = isOwner;
    }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    public bool IsOwner { get; private set; }

    public static Result<TenantMembership> Create(
        Guid tenantId,
        Guid userId,
        DateTime joinedAtUtc,
        bool isOwner = false)
    {
        if (tenantId == Guid.Empty)
            return Result<TenantMembership>.Failure(TenantMembershipErrors.InvalidTenantId);

        if (userId == Guid.Empty)
            return Result<TenantMembership>.Failure(TenantMembershipErrors.InvalidUserId);

        return Result<TenantMembership>.Success(
            new TenantMembership(Guid.NewGuid(), tenantId, userId, joinedAtUtc, isOwner));
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
