using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.Register;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);

        if (tenant is null)
            return Result<Guid>.Failure(UserErrors.InvalidTenantId);

        var emailResult = Email.Create(command.Email);

        if (emailResult.IsFailure)
            return Result<Guid>.Failure(emailResult.Error);

        bool emailExists = await userRepository.ExistsByEmailAsync(
            command.Email,
            command.TenantId,
            cancellationToken);

        if (emailExists)
            return Result<Guid>.Failure(UserErrors.EmailAlreadyExists);

        string hash = passwordHasher.Hash(command.Password);
        var passwordHashResult = PasswordHash.Create(hash);

        if (passwordHashResult.IsFailure)
            return Result<Guid>.Failure(passwordHashResult.Error);

        var userResult = User.Register(
            command.TenantId,
            emailResult.Value,
            passwordHashResult.Value,
            dateTimeProvider.UtcNow);

        if (userResult.IsFailure)
            return Result<Guid>.Failure(userResult.Error);

        await userRepository.AddAsync(userResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(userResult.Value.Id);
    }
}
