using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Identity.Enums;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;

namespace ZeroTrustSaaS.Domain.Identity;

public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        RefreshTokenHash tokenHash,
        ClientInfo issuedClient,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        IssuedClient = issuedClient;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public RefreshTokenHash TokenHash { get; private set; } = null!;

    public ClientInfo IssuedClient { get; private set; } = null!;

    public DateTime IssuedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? UsedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public IpAddress? RevokedByIp { get; private set; }

    public RefreshTokenRevocationReason RevocationReason { get; private set; }

    public Guid? ReplacedByRefreshTokenId { get; private set; }

    public bool IsUsed =>
        UsedAtUtc.HasValue;

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsExpired =>
        DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsActive =>
        !IsUsed &&
        !IsRevoked &&
        !IsExpired;

    public static Result<RefreshToken> Create(
        Guid userId,
        RefreshTokenHash tokenHash,
        ClientInfo issuedClient,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc)
    {
        if (expiresAtUtc <= issuedAtUtc)
        {
            return Result<RefreshToken>.Failure(
                RefreshTokenErrors.InvalidExpiration);
        }

        var token = new RefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            issuedClient,
            issuedAtUtc,
            expiresAtUtc);

        return Result<RefreshToken>.Success(token);
    }

    public Result MarkAsUsed(
        DateTime usedAtUtc)
    {
        if (IsRevoked)
            return Result.Failure(
                RefreshTokenErrors.AlreadyRevoked);

        if (IsExpired)
            return Result.Failure(
                RefreshTokenErrors.Expired);

        if (IsUsed)
            return Result.Failure(
                RefreshTokenErrors.AlreadyUsed);

        UsedAtUtc = usedAtUtc;

        return Result.Success();
    }

    public Result Rotate(
        Guid replacementRefreshTokenId,
        DateTime rotatedAtUtc)
    {
        if (replacementRefreshTokenId == Guid.Empty)
        {
            return Result.Failure(
                RefreshTokenErrors.InvalidReplacementToken);
        }

        if (IsRevoked)
            return Result.Failure(
                RefreshTokenErrors.AlreadyRevoked);

        if (IsExpired)
            return Result.Failure(
                RefreshTokenErrors.Expired);

        if (IsUsed)
            return Result.Failure(
                RefreshTokenErrors.AlreadyUsed);

        ReplacedByRefreshTokenId = replacementRefreshTokenId;
        UsedAtUtc = rotatedAtUtc;

        return Result.Success();
    }

    public Result Revoke(
        DateTime revokedAtUtc,
        IpAddress revokedByIp,
        RefreshTokenRevocationReason reason)
    {
        if (IsRevoked)
            return Result.Failure(
                RefreshTokenErrors.AlreadyRevoked);

        RevokedAtUtc = revokedAtUtc;
        RevokedByIp = revokedByIp;
        RevocationReason = reason;

        return Result.Success();
    }
}