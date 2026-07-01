# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build
```bash
dotnet build backend/ZeroTrustSaaS.slnx --no-restore
```

### Run locally (with Docker)
```bash
docker compose up --build          # start all services
docker compose down -v             # stop + wipe DB
docker compose logs -f api         # stream API logs
```

### EF Core migrations
```bash
# Add a migration (always run from repo root)
dotnet ef migrations add <Name> \
  --project backend/ZeroTrustSaaS.Infrastructure \
  --startup-project backend/ZeroTrustSaaS.Api

# Apply to local DB
dotnet ef database update \
  --project backend/ZeroTrustSaaS.Infrastructure \
  --startup-project backend/ZeroTrustSaaS.Api
```

### Frontend
```bash
cd frontend && npm install && npm run dev
# Regenerate lock file using the exact Docker image (keeps npm 10.x parity):
docker run --rm -v "$(pwd)/frontend":/app -w /app node:22-alpine sh -c "npm install"
```

## Solution layout

```
backend/ZeroTrustSaaS.slnx      ← use this, not .sln
  ZeroTrustSaaS.Domain/         ← entities, value objects, errors, enums
  ZeroTrustSaaS.Application/    ← handlers, repository interfaces, service interfaces
  ZeroTrustSaaS.Infrastructure/ ← EF Core, repositories, JWT, BCrypt, TOTP
  ZeroTrustSaaS.Api/            ← Minimal API endpoints, DI wiring, Program.cs
frontend/src/
  app/                          ← router, layouts, providers, theme
  features/                     ← feature-sliced: auth, platform/, tenant/, profile/, etc.
  shared/                       ← shared components (DataTable, StatCard, PageHeader, …)
```

## Architecture rules

### Backend: Clean Architecture + DDD + CQRS (no MediatR)
- **No MediatR.** Handlers are registered as `AddScoped<XxxCommandHandler>()` in `Application/DependencyInjection.cs` and injected directly into endpoints.
- Every handler takes its dependencies via primary constructor; call `handler.Handle(command, ct)` in the endpoint.
- Domain errors live in `Domain/<Aggregate>/Errors/`. All operations return `Result` or `Result<T>` — never throw for business logic.
- New handler → register in `Application/DependencyInjection.cs` → wire endpoint in `Api/Endpoints/`.

### Result pattern
```csharp
// Failure: return early
if (result.IsFailure) return result;          // propagate Result
if (result.IsFailure) return Result<T>.Failure(result.Error);  // lift to Result<T>

// Convert Problem response in endpoints:
return result.IsSuccess ? Results.Ok(result.Value) : ApiErrors.Problem(result.Error);
```

### Permission checks in handlers
```csharp
var permCheck = currentUser.RequirePermission(WellKnownPermissions.XxxYyy);
if (permCheck.IsFailure) return permCheck;
```
Platform users bypass all tenant permission checks automatically (`IsPlatformUser` short-circuits in `RequirePermission`).

### Value objects
Use `ValueObject.Create(...)` for validated construction, `.From(...)` for trusted internal construction. Never new them up directly outside the domain.

Key value objects: `Email`, `PasswordHash`, `TenantName`, `TenantSlug`, `MfaSecret`, `PermissionCode`, `RoleName`, `DeviceFingerprint`, `DeviceName`, `ClientInfo`, `IpAddress`.

### Aggregates
- `User` — `SecureAggregateRoot` (has `SecurityStamp`, auto-rotated on sensitive changes)
- `Tenant`, `Role`, `TrustedDevice` — `AuditableEntity`
- `User.RevokeAllUserRefreshTokens(now)` requires the user to be loaded **with** `RefreshTokens` included; use `GetByIdWithTokensAsync` when you need to revoke tokens.

### Repository pattern
Repositories expose simple query methods + `Add`/`Update`/`Remove`. `IUnitOfWork.SaveChangesAsync()` flushes everything. The `UserRepository.Update()` method handles the EF state dance for new LoginAttempts / RefreshTokens — read its comment before modifying.

### EF Core OwnedEntity mapping
Value objects are mapped as `OwnsOne(...)` with explicit column names in `Infrastructure/Persistence/Configurations/`. ClientInfo is a nested owned type inside TrustedDevice.

### Seeding
- `PlatformConfigurationSeeder` — sentinel row + platform roles (runs always)
- `PermissionRegistrySeeder` — `WellKnownPermissions` rows (runs always)
- `DevelopmentDataSeeder` — skipped when `SEED_DEV_DATA=false` in docker-compose

## JWT claims model
| Claim | Platform login | Tenant login |
|---|---|---|
| `platform_role` | role names | absent |
| `tenant_id` | absent | tenant GUID |
| `tenant_role` | absent | role name |
| `permission` | absent | permission codes |

`ICurrentUserContext` reads these from `HttpContext`. `isPlatformUser` is true when `platform_role` is present.

## WellKnownPermissions
12 codes: `tenant.view`, `tenant.manage`, `user.view`, `user.create`, `user.manage`, `role.view`, `role.manage`, `device.view`, `device.manage`, `audit.view`, `mfa.manage`, `security.manage`.

5 default tenant roles seeded per new tenant: Owner (12 perms), Administrator (10), Manager (7), Auditor (4), Employee (2).

## SecurityEventType enum
30 values. Latest additions (22–30): `TenantCreated`, `UserCreated`, `RoleDeleted`, `PermissionRemoved`, `SessionsRevoked`, `UserActivated`, `PasswordResetForced`, `MfaFailed`, `MfaSucceeded`.

## MFA
- OTP.NET v1.4.1. Secret stored as Base32. Verification uses `Totp.VerifyTotp` with `VerificationWindow(1,1)`.
- Recovery codes: 8 codes hashed with SHA-256, stored as JSON text column `mfa_recovery_code_hashes` on `users`.
- Flow: `SetupTotpQuery` (generates secret, no persist) → `VerifyAndEnableMfaCommandHandler` (validates + persists) → `VerifyMfaCommandHandler` (login second factor).

## Frontend

React 19, React Router 7, MUI v9, TanStack Query v5, React Hook Form + Zod, `qrcode.react`.

- **State**: `authStore.jsx` (Zustand) — `accessToken` in sessionStorage, `refreshToken` in localStorage.
- **Auth guards**: `RequireAuth`, `RequirePlatform`, `RequireTenant` in `app/router/guards/`.
- **Permission gating in UI**: `const canXxx = hasPermission('xxx.yyy')` then conditionally render — hide, don't disable.
- **API client**: `shared/api/axiosInstance.js` — attaches Bearer token, handles 401 refresh rotation.
- **Shared components**: `DataTable` (MUI DataGrid wrapper), `StatCard`, `PageHeader`, `SeverityBadge`, `EmptyState`, `ConfirmDialog`.
- **Icons**: `@mui/icons-material` only — no other icon libraries.
- **No inline emoji** in UI components.

## Docker services
| Service | URL |
|---|---|
| Frontend (nginx) | http://localhost:5173 |
| API | http://localhost:8080 |
| OpenAPI | http://localhost:8080/openapi |
| pgAdmin | http://localhost:5050 |
| PostgreSQL | localhost:5432 |

Set `SEED_DEV_DATA=true` in docker-compose to auto-seed dev credentials and skip the Setup Wizard.

## Commit rules
- No AI co-author footers. Commit only as `demir-dev`.
- Human-friendly messages consistent with prior commit style.
- Never commit secrets, `bin/`, `obj/`, `.idea`, `.vs`, `node_modules`, `appsettings.Development.json`, `.env`.
