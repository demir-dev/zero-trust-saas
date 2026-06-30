using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Authorization.Errors;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Tenants;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Application.Features.Identity.CreateTenantUser;

public sealed class CreateTenantUserCommandHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    ITenantMembershipRepository membershipRepository,
    IRoleRepository roleRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> Handle(
        CreateTenantUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);
        if (tenant is null)
            return Result<Guid>.Failure(TenantErrors.NotFound);

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result<Guid>.Failure(emailResult.Error);

        bool emailExists = await userRepository.ExistsByEmailAsync(command.Email, cancellationToken);
        if (emailExists)
            return Result<Guid>.Failure(UserErrors.EmailAlreadyExists);

        string hash = passwordHasher.Hash(command.Password);
        var passwordHashResult = PasswordHash.Create(hash);
        if (passwordHashResult.IsFailure)
            return Result<Guid>.Failure(passwordHashResult.Error);

        var now = dateTimeProvider.UtcNow;

        var userResult = User.Register(
            emailResult.Value,
            passwordHashResult.Value,
            now,
            command.FirstName,
            command.LastName);

        if (userResult.IsFailure)
            return Result<Guid>.Failure(userResult.Error);

        var user = userResult.Value;
        user.VerifyEmail(now);

        await userRepository.AddAsync(user, cancellationToken);

        var membershipResult = TenantMembership.Create(command.TenantId, user.Id, now, isOwner: false);
        if (membershipResult.IsFailure)
            return Result<Guid>.Failure(membershipResult.Error);

        await membershipRepository.AddAsync(membershipResult.Value, cancellationToken);

        if (command.RoleId.HasValue)
        {
            var role = await roleRepository.GetByIdAsync(command.RoleId.Value, cancellationToken);
            if (role is null)
                return Result<Guid>.Failure(RoleErrors.NotFound);

            var userRoleResult = UserRole.Create(user.Id, role.Id, command.TenantId, now);
            if (userRoleResult.IsSuccess)
                await roleRepository.AddUserRoleAsync(userRoleResult.Value, cancellationToken);
        }

        var logResult = AuditLog.Create(
            SecurityEventType.UserCreated,
            AuditSeverity.Info,
            now,
            userId: user.Id,
            tenantId: command.TenantId);

        if (logResult.IsSuccess)
            await auditLogRepository.AddAsync(logResult.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
