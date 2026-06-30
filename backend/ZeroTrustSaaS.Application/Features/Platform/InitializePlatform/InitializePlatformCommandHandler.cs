using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Application.Features.Platform.Errors;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Platform.Errors;

namespace ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;

public sealed class InitializePlatformCommandHandler(
    IPlatformConfigurationRepository platformConfigRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> Handle(
        InitializePlatformCommand command,
        CancellationToken cancellationToken = default)
    {
        var config = await platformConfigRepository.GetAsync(cancellationToken);

        if (config is not null && config.IsInitialized)
            return Result<Guid>.Failure(PlatformErrors.AlreadyInitialized);

        var now = dateTimeProvider.UtcNow;

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure) return Result<Guid>.Failure(emailResult.Error);

        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return Result<Guid>.Failure(UserErrors.EmailAlreadyExists);

        var hash = passwordHasher.Hash(command.Password);
        var passwordHashResult = PasswordHash.Create(hash);
        if (passwordHashResult.IsFailure) return Result<Guid>.Failure(passwordHashResult.Error);

        var userResult = User.Register(
            emailResult.Value,
            passwordHashResult.Value,
            now,
            command.FirstName,
            command.LastName);

        if (userResult.IsFailure) return Result<Guid>.Failure(userResult.Error);
        var user = userResult.Value;
        user.VerifyEmail(now);

        // PlatformOwner role must have been seeded at startup by PlatformConfigurationSeeder.
        var platformOwnerRole = await roleRepository.GetByNameAsync(
            WellKnownPlatformRoles.PlatformOwner,
            tenantId: null,
            cancellationToken);

        if (platformOwnerRole is null)
            return Result<Guid>.Failure(PlatformConfigurationErrors.NotFound);

        var userRoleResult = UserRole.Create(user.Id, platformOwnerRole.Id, tenantId: null, now);
        if (userRoleResult.IsFailure) return Result<Guid>.Failure(userRoleResult.Error);

        if (config is null)
        {
            config = Domain.Platform.PlatformConfiguration.CreateNew();
            var markResult = config.MarkInitialized(now);
            if (markResult.IsFailure) return Result<Guid>.Failure(markResult.Error);
            await platformConfigRepository.AddAsync(config, cancellationToken);
        }
        else
        {
            var markResult = config.MarkInitialized(now);
            if (markResult.IsFailure) return Result<Guid>.Failure(markResult.Error);
            platformConfigRepository.Update(config);
        }

        await userRepository.AddAsync(user, cancellationToken);
        await roleRepository.AddUserRoleAsync(userRoleResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
