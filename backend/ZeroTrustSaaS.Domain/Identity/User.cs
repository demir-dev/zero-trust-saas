using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Enums;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Domain.Identity;

public sealed class User : SecureAggregateRoot
{
    private const int MaxFailedLoginAttemptsBeforeLockout = 5;
    private static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<LoginAttempt> _loginAttempts = [];
    private List<string> _mfaRecoveryCodes = [];

    private User()
    {
        // Required by EF Core.
    }

    private User(
        Guid id,
        Email email,
        PasswordHash passwordHash,
        DateTime createdAtUtc,
        string? firstName,
        string? lastName)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Status = UserStatus.PendingVerification;
        IsEmailConfirmed = false;
        IsMfaEnabled = false;
        MfaMethod = MfaMethod.None;
        RegisteredAtUtc = createdAtUtc;
        PasswordChangedAtUtc = createdAtUtc;
    }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(FirstName) ? Email.Value : $"{FirstName} {LastName}".Trim();

    public Email Email { get; private set; } = null!;

    public PasswordHash PasswordHash { get; private set; } = null!;

    public UserStatus Status { get; private set; }

    public bool IsEmailConfirmed { get; private set; }

    public DateTime RegisteredAtUtc { get; private set; }

    public DateTime? EmailVerifiedAtUtc { get; private set; }

    public DateTime PasswordChangedAtUtc { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public DateTime? LastLoginUtc { get; private set; }

    public DateTime? LastFailedLoginUtc { get; private set; }

    public bool IsMfaEnabled { get; private set; }

    public MfaMethod MfaMethod { get; private set; }

    public MfaSecret? MfaSecret { get; private set; }

    public List<string> MfaRecoveryCodeHashes
    {
        get => _mfaRecoveryCodes;
        private set => _mfaRecoveryCodes = value;
    }

    public IReadOnlyCollection<LoginAttempt> LoginAttempts => _loginAttempts.AsReadOnly();

    public bool IsActive => Status == UserStatus.Active;

    public bool IsLocked =>
        Status == UserStatus.Locked &&
        LockedUntilUtc is not null &&
        LockedUntilUtc > DateTime.UtcNow;

    public bool CanAuthenticate =>
        Status == UserStatus.Active &&
        !IsLocked;

    public static Result<User> Register(
        Email email,
        PasswordHash passwordHash,
        DateTime createdAtUtc,
        string? firstName = null,
        string? lastName = null)
    {
        var user = new User(
            Guid.NewGuid(),
            email,
            passwordHash,
            createdAtUtc,
            firstName?.Trim(),
            lastName?.Trim());

        return Result<User>.Success(user);
    }

    public Result VerifyEmail(DateTime verifiedAtUtc)
    {
        if (Status == UserStatus.Active && IsEmailConfirmed)
            return Result.Failure(UserErrors.EmailAlreadyVerified);

        if (Status != UserStatus.PendingVerification)
            return Result.Failure(UserErrors.InvalidStatusForEmailVerification);

        IsEmailConfirmed = true;
        EmailVerifiedAtUtc = verifiedAtUtc;
        Status = UserStatus.Active;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result ChangeEmail(Email email, DateTime changedAtUtc)
    {
        if (Status is UserStatus.Disabled or UserStatus.Suspended)
            return Result.Failure(UserErrors.UserNotAllowedToChangeEmail);

        if (Email.Equals(email))
            return Result.Failure(UserErrors.EmailAlreadyInUseByUser);

        Email = email;
        IsEmailConfirmed = false;
        EmailVerifiedAtUtc = null;
        Status = UserStatus.PendingVerification;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result ChangePassword(PasswordHash passwordHash, DateTime changedAtUtc)
    {
        if (Status is UserStatus.Disabled or UserStatus.Suspended)
            return Result.Failure(UserErrors.UserNotAllowedToChangePassword);

        PasswordHash = passwordHash;
        PasswordChangedAtUtc = changedAtUtc;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result EnableMfa(MfaMethod method, MfaSecret secret, DateTime enabledAtUtc)
    {
        if (Status != UserStatus.Active)
            return Result.Failure(UserErrors.UserMustBeActive);

        if (method == MfaMethod.None)
            return Result.Failure(UserErrors.InvalidMfaMethod);

        if (IsMfaEnabled)
            return Result.Failure(UserErrors.MfaAlreadyEnabled);

        IsMfaEnabled = true;
        MfaMethod = method;
        MfaSecret = secret;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result DisableMfa(DateTime disabledAtUtc)
    {
        if (Status != UserStatus.Active)
            return Result.Failure(UserErrors.UserMustBeActive);

        if (!IsMfaEnabled)
            return Result.Failure(UserErrors.MfaAlreadyDisabled);

        IsMfaEnabled = false;
        MfaMethod = MfaMethod.None;
        MfaSecret = null;
        _mfaRecoveryCodes.Clear();

        RefreshSecurityStamp();

        return Result.Success();
    }

    public void SetRecoveryCodeHashes(IReadOnlyList<string> hashes)
    {
        _mfaRecoveryCodes = hashes.ToList();
    }

    public bool ConsumeRecoveryCode(string hash)
    {
        return _mfaRecoveryCodes.Remove(hash);
    }

    public Result LockUntil(DateTime lockedUntilUtc, DateTime lockedAtUtc)
    {
        if (Status is UserStatus.Disabled or UserStatus.Suspended)
            return Result.Failure(UserErrors.UserCannotBeLocked);

        if (lockedUntilUtc <= lockedAtUtc)
            return Result.Failure(UserErrors.InvalidLockoutDate);

        Status = UserStatus.Locked;
        LockedUntilUtc = lockedUntilUtc;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result Unlock(DateTime unlockedAtUtc)
    {
        if (Status != UserStatus.Locked)
            return Result.Failure(UserErrors.UserIsNotLocked);

        Status = UserStatus.Active;
        LockedUntilUtc = null;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result Suspend(DateTime suspendedAtUtc)
    {
        if (Status == UserStatus.Suspended)
            return Result.Failure(UserErrors.AlreadySuspended);

        if (Status == UserStatus.Disabled)
            return Result.Failure(UserErrors.DisabledUserCannotBeSuspended);

        Status = UserStatus.Suspended;
        LockedUntilUtc = null;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result Resume(DateTime resumedAtUtc)
    {
        if (Status != UserStatus.Suspended)
            return Result.Failure(UserErrors.UserIsNotSuspended);

        Status = IsEmailConfirmed
            ? UserStatus.Active
            : UserStatus.PendingVerification;

        LockedUntilUtc = null;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result Disable(DateTime disabledAtUtc)
    {
        if (Status == UserStatus.Disabled)
            return Result.Failure(UserErrors.AlreadyDisabled);

        Status = UserStatus.Disabled;
        LockedUntilUtc = null;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result Reactivate(DateTime reactivatedAtUtc)
    {
        if (Status != UserStatus.Disabled)
            return Result.Failure(UserErrors.UserIsNotDisabled);

        Status = IsEmailConfirmed
            ? UserStatus.Active
            : UserStatus.PendingVerification;

        LockedUntilUtc = null;

        RefreshSecurityStamp();

        return Result.Success();
    }

    public Result RecordSuccessfulLogin(LoginAttempt attempt, DateTime loggedInAtUtc)
    {
        if (Status != UserStatus.Active)
            return Result.Failure(UserErrors.UserMustBeActive);

        if (IsLocked)
            return Result.Failure(UserErrors.UserIsLocked);

        _loginAttempts.Add(attempt);

        LastLoginUtc = loggedInAtUtc;
        LastFailedLoginUtc = null;

        return Result.Success();
    }

    public Result RecordFailedLogin(LoginAttempt attempt, DateTime failedAtUtc)
    {
        if (Status is UserStatus.Disabled or UserStatus.Suspended)
            return Result.Failure(UserErrors.LoginNotAllowed);

        _loginAttempts.Add(attempt);
        LastFailedLoginUtc = failedAtUtc;

        if (ShouldLockAfterFailedLogin(failedAtUtc))
        {
            Status = UserStatus.Locked;
            LockedUntilUtc = failedAtUtc.Add(DefaultLockoutDuration);
            RefreshSecurityStamp();
        }

        return Result.Success();
    }

    // Rotates the security stamp, immediately invalidating all issued JWTs via the stamp check.
    // Bulk session revocation is handled separately by handlers via ISessionRepository.
    public Result RevokeAllUserRefreshTokens(DateTime revokedAtUtc)
    {
        RefreshSecurityStamp();
        return Result.Success();
    }

    private bool ShouldLockAfterFailedLogin(DateTime failedAtUtc)
    {
        var recentFailedAttempts = _loginAttempts
            .Where(x => x.OccurredAtUtc <= failedAtUtc)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(MaxFailedLoginAttemptsBeforeLockout)
            .Count();

        return recentFailedAttempts >= MaxFailedLoginAttemptsBeforeLockout;
    }

    private void RefreshSecurityStamp()
    {
        RotateSecurityStamp();
    }
}
