using System.Security.Cryptography;
using System.Text;
using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Security;
using ZeroTrustSaaS.Domain.Security.Enums;

namespace ZeroTrustSaaS.Application.Features.Identity.Mfa.VerifyAndEnableMfa;

public sealed class VerifyAndEnableMfaCommandHandler(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IMfaCodeValidator mfaCodeValidator,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    private static readonly char[] RecoveryCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    public async Task<Result<VerifyAndEnableMfaResponse>> Handle(
        VerifyAndEnableMfaCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Result<VerifyAndEnableMfaResponse>.Failure(UserErrors.NotFound);

        var isValid = mfaCodeValidator.Validate(
            command.Base32Secret,
            command.VerificationCode,
            MfaMethod.Totp);

        if (!isValid)
            return Result<VerifyAndEnableMfaResponse>.Failure(UserErrors.InvalidMfaCode);

        var plaintextCodes = GenerateRecoveryCodes(8);
        var hashedCodes = plaintextCodes.Select(HashRecoveryCode).ToList();

        var secretResult = MfaSecret.Create(command.Base32Secret);
        if (secretResult.IsFailure)
            return Result<VerifyAndEnableMfaResponse>.Failure(secretResult.Error);

        var now = dateTimeProvider.UtcNow;
        var enableResult = user.EnableMfa(MfaMethod.Totp, secretResult.Value, now);
        if (enableResult.IsFailure)
            return Result<VerifyAndEnableMfaResponse>.Failure(enableResult.Error);

        user.SetRecoveryCodeHashes(hashedCodes);
        userRepository.Update(user);

        var logResult = AuditLog.Create(
            SecurityEventType.MfaEnabled,
            AuditSeverity.High,
            now,
            userId: user.Id,
            tenantId: null);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<VerifyAndEnableMfaResponse>.Success(
            new VerifyAndEnableMfaResponse(plaintextCodes));
    }

    private static List<string> GenerateRecoveryCodes(int count)
    {
        var codes = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            var part1 = GenerateCodeSegment(4);
            var part2 = GenerateCodeSegment(4);
            codes.Add($"{part1}-{part2}");
        }
        return codes;
    }

    private static string GenerateCodeSegment(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => RecoveryCodeChars[b % RecoveryCodeChars.Length]).ToArray());
    }

    internal static string HashRecoveryCode(string code)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
