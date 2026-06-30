using ZeroTrustSaaS.Application.Abstractions.Persistence;
using ZeroTrustSaaS.Application.Abstractions.Repositories;
using ZeroTrustSaaS.Application.Abstractions.Services;
using ZeroTrustSaaS.Application.Common;
using ZeroTrustSaaS.Application.Features.Platform.Errors;
using ZeroTrustSaaS.Domain.Authorization;
using ZeroTrustSaaS.Domain.Common;
using ZeroTrustSaaS.Domain.Identity;
using ZeroTrustSaaS.Domain.Tenants;

namespace ZeroTrustSaaS.Application.Features.Platform.InitializePlatform;

public sealed class InitializePlatformCommandHandler(
    ITenantRepository tenantRepository,
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
        if (await tenantRepository.CountAsync(cancellationToken) > 0)
            return Result<Guid>.Failure(PlatformErrors.AlreadyInitialized);

        var now = dateTimeProvider.UtcNow;

        // --- Tenant ---
        var nameResult = TenantName.Create(command.OrganizationName);
        if (nameResult.IsFailure) return Result<Guid>.Failure(nameResult.Error);

        var slugResult = TenantSlug.Create(command.OrganizationSlug);
        if (slugResult.IsFailure) return Result<Guid>.Failure(slugResult.Error);

        var tenantResult = Tenant.Create(nameResult.Value, slugResult.Value, now);
        if (tenantResult.IsFailure) return Result<Guid>.Failure(tenantResult.Error);
        var tenant = tenantResult.Value;

        // --- Admin User ---
        var emailResult = Email.Create(command.AdminEmail);
        if (emailResult.IsFailure) return Result<Guid>.Failure(emailResult.Error);

        var hash = passwordHasher.Hash(command.AdminPassword);
        var passwordHashResult = PasswordHash.Create(hash);
        if (passwordHashResult.IsFailure) return Result<Guid>.Failure(passwordHashResult.Error);

        var userResult = User.Register(
            tenant.Id, emailResult.Value, passwordHashResult.Value, now,
            command.AdminFirstName, command.AdminLastName);
        if (userResult.IsFailure) return Result<Guid>.Failure(userResult.Error);
        var user = userResult.Value;

        user.VerifyEmail(now);

        // --- Tenant Owner Membership ---
        var membershipResult = TenantMembership.Create(tenant.Id, user.Id, now, isOwner: true);
        if (membershipResult.IsFailure) return Result<Guid>.Failure(membershipResult.Error);
        tenant.AddMembership(membershipResult.Value);

        // --- Tenant Roles (isSystem = false to allow AssignPermission) ---
        // Permissions are pre-seeded by PermissionRegistrySeeder at startup.
        var ownerNameResult = RoleName.Create("Owner");
        var adminNameResult = RoleName.Create("Administrator");
        var memberNameResult = RoleName.Create("Member");
        if (ownerNameResult.IsFailure) return Result<Guid>.Failure(ownerNameResult.Error);
        if (adminNameResult.IsFailure) return Result<Guid>.Failure(adminNameResult.Error);
        if (memberNameResult.IsFailure) return Result<Guid>.Failure(memberNameResult.Error);

        var ownerRoleResult = Role.Create(ownerNameResult.Value, tenant.Id, PermissionScope.Tenant, isSystem: false);
        var adminRoleResult = Role.Create(adminNameResult.Value, tenant.Id, PermissionScope.Tenant, isSystem: false);
        var memberRoleResult = Role.Create(memberNameResult.Value, tenant.Id, PermissionScope.Tenant, isSystem: false);
        if (ownerRoleResult.IsFailure) return Result<Guid>.Failure(ownerRoleResult.Error);
        if (adminRoleResult.IsFailure) return Result<Guid>.Failure(adminRoleResult.Error);
        if (memberRoleResult.IsFailure) return Result<Guid>.Failure(memberRoleResult.Error);

        var ownerRole = ownerRoleResult.Value;
        var adminRole = adminRoleResult.Value;
        var memberRole = memberRoleResult.Value;

        foreach (var code in WellKnownPermissions.OwnerPermissions)
            ownerRole.AssignPermission(PermissionCode.From(code), now);

        foreach (var code in WellKnownPermissions.AdministratorPermissions)
            adminRole.AssignPermission(PermissionCode.From(code), now);

        foreach (var code in WellKnownPermissions.MemberPermissions)
            memberRole.AssignPermission(PermissionCode.From(code), now);

        // --- Assign Owner role to first user ---
        var userRoleResult = UserRole.Create(user.Id, ownerRole.Id, tenant.Id, now);
        if (userRoleResult.IsFailure) return Result<Guid>.Failure(userRoleResult.Error);

        // --- Default Security Configuration ---
        // When PasswordPolicy, MfaPolicy, SessionPolicy domain entities are implemented,
        // their initialization with sensible defaults belongs here (same transaction).

        // --- Activate tenant: Provisioning → Active ---
        var activateResult = tenant.Activate();
        if (activateResult.IsFailure) return Result<Guid>.Failure(activateResult.Error);

        // --- Persist everything in one transaction ---
        await tenantRepository.AddAsync(tenant, cancellationToken);
        await userRepository.AddAsync(user, cancellationToken);
        await roleRepository.AddAsync(ownerRole, cancellationToken);
        await roleRepository.AddAsync(adminRole, cancellationToken);
        await roleRepository.AddAsync(memberRole, cancellationToken);
        await roleRepository.AddUserRoleAsync(userRoleResult.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(tenant.Id);
    }
}
