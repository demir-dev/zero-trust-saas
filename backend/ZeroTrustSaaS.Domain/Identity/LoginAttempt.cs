using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Devices;
using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Domain.Identity;

public sealed class LoginAttempt : Entity
{
    private LoginAttempt()
    {
    }

    private LoginAttempt(
        Guid id,
        Guid userId,
        ClientInfo clientInfo,
        LoginResult result,
        RiskLevel riskLevel,
        DateTime occurredAtUtc)
        : base(id)
    {
        UserId = userId;
        ClientInfo = clientInfo;
        Result = result;
        RiskLevel = riskLevel;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid UserId { get; private set; }

    public ClientInfo ClientInfo { get; private set; } = null!;

    public LoginResult Result { get; private set; }

    public RiskLevel RiskLevel { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public bool IsSuccessful =>
        Result == LoginResult.Success;

    public bool RequiresMfa =>
        Result == LoginResult.MfaRequired;

    public bool IsRejected =>
        !IsSuccessful;

    public static Result<LoginAttempt> Create(
        Guid userId,
        ClientInfo clientInfo,
        LoginResult result,
        RiskLevel riskLevel,
        DateTime occurredAtUtc)
    {
        var attempt = new LoginAttempt(
            Guid.NewGuid(),
            userId,
            clientInfo,
            result,
            riskLevel,
            occurredAtUtc);

        return Result<LoginAttempt>.Success(attempt);
    }
}