using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Application.Features.Platform.Errors;
using ZeroTrustSaaS.Domain.Audit;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Identity.Errors;
using ZeroTrustSaaS.Domain.Tenants;
using ZeroTrustSaaS.Domain.Tenants.Errors;

namespace ZeroTrustSaaS.Application.Features.Tenants.CreateTenant;

public sealed class CreateTenantCommandHandler(
    IPlatformConfigurationRepository platformConfigRepository,
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ITenantMembershipRepository membershipRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<Guid>> Handle(
        CreateTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var config = await platformConfigRepository.GetAsync(cancellationToken);
        if (config is null || !config.IsInitialized)
            return Result<Guid>.Failure(PlatformErrors.NotInitialized);

        var nameResult = TenantName.Create(command.Name);
        if (nameResult.IsFailure) return Result<Guid>.Failure(nameResult.Error);

        var slugResult = TenantSlug.Create(command.Slug);
        if (slugResult.IsFailure) return Result<Guid>.Failure(slugResult.Error);

        if (await tenantRepository.ExistsBySlugAsync(slugResult.Value.Value, cancellationToken))
            return Result<Guid>.Failure(TenantErrors.SlugAlreadyExists);

        var now = dateTimeProvider.UtcNow;

        var tenantResult = Tenant.Create(nameResult.Value, slugResult.Value, now);
        if (tenantResult.IsFailure) return Result<Guid>.Failure(tenantResult.Error);
        var tenant = tenantResult.Value;

        // Create 5 tenant roles with permissions
        var roles = new (string Name, IReadOnlyList<string> Permissions)[]
        {
            (WellKnownTenantRoles.Owner,         WellKnownPermissions.OwnerPermissions),
            (WellKnownTenantRoles.Administrator, WellKnownPermissions.AdministratorPermissions),
            (WellKnownTenantRoles.Manager,       WellKnownPermissions.ManagerPermissions),
            (WellKnownTenantRoles.Auditor,       WellKnownPermissions.AuditorPermissions),
            (WellKnownTenantRoles.Employee,      WellKnownPermissions.EmployeePermissions),
        };

        Role? ownerRole = null;
        var createdRoles = new List<Role>(roles.Length);

        foreach (var (name, permissions) in roles)
        {
            var roleNameResult = RoleName.Create(name);
            if (roleNameResult.IsFailure) return Result<Guid>.Failure(roleNameResult.Error);

            var roleResult = Role.Create(roleNameResult.Value, tenant.Id, PermissionScope.Tenant);
            if (roleResult.IsFailure) return Result<Guid>.Failure(roleResult.Error);

            var role = roleResult.Value;
            foreach (var code in permissions)
                role.AssignPermission(PermissionCode.From(code), now);

            if (name == WellKnownTenantRoles.Owner)
                ownerRole = role;

            createdRoles.Add(role);
        }

        // Resolve the tenant owner
        User ownerUser;

        if (command.ExistingOwnerUserId.HasValue)
        {
            var existing = await userRepository.GetByIdAsync(command.ExistingOwnerUserId.Value, cancellationToken);
            if (existing is null)
                return Result<Guid>.Failure(UserErrors.NotFound);
            ownerUser = existing;
        }
        else
        {
            var emailResult = Email.Create(command.OwnerEmail);
            if (emailResult.IsFailure) return Result<Guid>.Failure(emailResult.Error);

            if (await userRepository.ExistsByEmailAsync(command.OwnerEmail, cancellationToken))
                return Result<Guid>.Failure(UserErrors.EmailAlreadyExists);

            var hash = passwordHasher.Hash(command.OwnerPassword);
            var passwordHashResult = PasswordHash.Create(hash);
            if (passwordHashResult.IsFailure) return Result<Guid>.Failure(passwordHashResult.Error);

            var newUserResult = User.Register(
                emailResult.Value,
                passwordHashResult.Value,
                now,
                command.OwnerFirstName,
                command.OwnerLastName);

            if (newUserResult.IsFailure) return Result<Guid>.Failure(newUserResult.Error);
            ownerUser = newUserResult.Value;
            ownerUser.VerifyEmail(now);

            await userRepository.AddAsync(ownerUser, cancellationToken);
        }

        var membershipResult = TenantMembership.Create(tenant.Id, ownerUser.Id, now, isOwner: true);
        if (membershipResult.IsFailure) return Result<Guid>.Failure(membershipResult.Error);

        var userRoleResult = UserRole.Create(ownerUser.Id, ownerRole!.Id, tenant.Id, now);
        if (userRoleResult.IsFailure) return Result<Guid>.Failure(userRoleResult.Error);

        var activateResult = tenant.Activate(now);
        if (activateResult.IsFailure) return Result<Guid>.Failure(activateResult.Error);

        await tenantRepository.AddAsync(tenant, cancellationToken);
        await membershipRepository.AddAsync(membershipResult.Value, cancellationToken);
        foreach (var role in createdRoles)
            await roleRepository.AddAsync(role, cancellationToken);
        await roleRepository.AddUserRoleAsync(userRoleResult.Value, cancellationToken);

        var auditLog = AuditLog.Create(
            SecurityEventType.TenantCreated,
            AuditSeverity.Info,
            now,
            tenantId: tenant.Id,
            userId: ownerUser.Id);
        await auditLogRepository.AddAsync(auditLog.Value, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(tenant.Id);
    }
}
