# Wave 4.6 — Identity.Contracts (IIdentityQueries + IIdentityCommands) — Implementation Plan

**Status**: READY FOR EXECUTION — architect consult 2026-06-24 ruled Risk #2 = Option C, Risk #3 = Option A (semantic mutators), Risk #4 = Option A (with factual correction). 5 additional findings folded in. See "Architect Ruling Addendum" at bottom for the full ruling text + plan corrections.

**Predecessor**: Wave 4.4 Payments.Contracts (shipped + STAGING-VERIFIED `86121c43`). Same Contracts-pattern shape; this wave finishes the 8 Wave 4 capability extractions.

**Memory pins to anchor the plan**:
- `feedback_module_extraction_cross_aggregate_nav.md` — Wave 4.1.2 typed-nav blocker; **NOT applicable here** (User aggregate has no typed navs into other aggregates — confirmed by pre-flight survey).
- `feedback_read_side_bypass_audit.md` — Wave 5.4.d.4 hotfix lesson; the smoke matrix below includes the dbContext.Set bypass grep.
- `feedback_wave_numbering_correction.md` — using master-plan W4.6 numbering (not "W5.5") from the start.

---

## TL;DR

Wave 4.6 splits into **9 atomic commits (4.6.a → 4.6.d.3)**. **STRUCTURALLY EASIER than Wave 4.4** (no typed-nav blocker, no EF migration) but **LARGER in surface area** (26 command handlers + 8 query handlers + 4 security services + 128 cross-module consumer files vs Wave 4.4's 7 + 2 + 6 + ~10).

Final state:
- `IIdentityQueries` + `IIdentityCommands` live in `src/Modules/Identity/Identity.Contracts/`.
- `LankaConnect.Application` consumes the Contracts surface for all read-side User access (no Identity.Domain edge).
- 26 command + 8 query + 1 event handler + 4 security services physically relocated to Identity.{Application,Infrastructure}.
- `User` aggregate + `IUserRepository` physically relocated to Identity.{Domain,Infrastructure}.
- ArchTest Rule 5 `LegacyApplication_DoesNotDependOnIdentityDomain` pins the boundary.
- **No EF migration** under current scope (Risk #2 Option A path).

---

## Pre-flight survey snapshot (2026-06-24)

| Concern | State |
|---|---|
| `src/Modules/Identity/` skeleton | **Does NOT exist** — fresh extraction |
| User aggregate | `src/LankaConnect.Domain/Users/User.cs` (1,057 LOC) |
| User value objects | `src/LankaConnect.Domain/Users/ValueObjects/` (6 files, 541 LOC) |
| User enums | `src/LankaConnect.Domain/Users/Enums/` (5 files, 368 LOC) |
| User domain events | `src/LankaConnect.Domain/Users/DomainEvents/` (1 file, ~50 LOC) |
| **Cross-aggregate typed navs ON User** | **NONE** — collections are pure value objects (`_culturalInterests`, `_languages`, `_refreshTokens`, `_externalLogins`, `_preferredMetroAreaIds`). Risk #1 (W4.4-style) does NOT apply. |
| **Incoming typed navs INTO User** | **NONE** — Event/Registration/Badge reference User by raw `Guid OrganizerId`/`UserId`/`CreatorId` only |
| Repository | `IUserRepository` (89 LOC, 18 explicit query methods) |
| Auth command handlers | 5 (LoginUser, LoginWithEntra, LogoutUser, RefreshToken, RegisterUser) |
| Users command handlers | 21 (Admin\* × 5, RoleUpgrade × 4, ExternalProvider × 2, Profile × 9, ...) |
| Users query handlers | 8 (GetLinkedProviders, GetPendingRoleUpgrades, GetUserById, GetUserPreferredMetroAreas, SearchUsers, GetAdminUserDetails, GetAdminUserStatistics, GetAdminUsersPaged) |
| Users event handlers | 1 (MemberVerificationRequestedEventHandler) |
| Security services | `JwtTokenService` (220 LOC), `EntraExternalIdService` (219 LOC), `PasswordHashingService` (190 LOC — **no interface**), `CurrentUserService` (57 LOC) — total 686 LOC |
| Cross-module User consumers | 128 files import `LankaConnect.Domain.Users.*`; ~14 directly inject `IUserRepository` |
| `IJwtTokenService` location | `src/LankaConnect.Application/Common/Interfaces/` — legacy port (mirrors W4.4 Risk #2 IStripePaymentService) |
| `IPasswordHashingService` | **No interface exists** — `PasswordHashingService` instantiated directly via DI; either gets an interface during 4.6.c.5 or stays as adapter-only |
| Password reset coupling | `User.PasswordResetToken` + `User.PasswordResetTokenExpiresAt` fields owned by User; `SendPasswordResetCommandHandler` + `ResetPasswordCommandHandler` live in **Communications module** — cross-module read+write of User state |
| Existing Identity ArchTests | `SharedKernel_Identity_DependsOnlyOnBuildingBlocks` (different — pins `SharedKernel.Identity.UserId` primitive). **No `Modules.Identity` rules** yet |

---

## Sub-phase decomposition

### 4.6.a — Define `Identity.Contracts` surface (additive)

New files in `src/Modules/Identity/Identity.Contracts/`:
- `IIdentityQueries.cs` — read-side surface for cross-module consumers
- `IIdentityCommands.cs` — mutator surface (empty marker initially per W4.4.a / W5.4.a precedent)
- DTOs: `UserSummaryDto`, `UserDetailDto`, `UserSearchResultDto`, `UserPendingRoleUpgradeDto`
- Enums: `UserRoleDto`, `UserStatusDto`, `IdentityProviderDto`, `FederatedProviderDto` (mirror-cast at byte level)

T-triggers: T1 (new public interfaces). Tests: `Identity.Contracts.Tests/` — shape-pinning (~8 tests including byte-value enum mirroring).

**ArchTest rule lands HERE** (added to `LayeringRules.cs`): `Modules_Identity_Contracts_DependsOnlyOnBuildingBlocksContracts`.

### 4.6.b — Implement `IdentityQueries` + `IdentityModule` DI

- `Identity.Application/Queries/IdentityQueries.cs` — wraps legacy `IUserRepository` (transitional; lives in `LankaConnect.Domain.Users` until 4.6.d.2)
- `Identity.Application/Mappings/UserContractMappings.cs` — `ToSummaryDto()` / `ToDetailDto()` projections
- `Identity.Api/IdentityModule.cs` — DI: `services.AddScoped<IIdentityQueries, IdentityQueries>()` + MediatR/FluentValidation scan placeholder
- `Identity.Application.csproj` adds `<ProjectReference Identity.Contracts>` + transitional `<ProjectReference LankaConnect.Domain>` (User stays here until 4.6.d.2)

T-triggers: T3 + T6. Tests: `Identity.Application.Tests/Queries/IdentityQueriesTests.cs` (~10 tests).

### 4.6.c.1 — Move 5 Auth command handlers into Identity.Application

Files moved from `src/LankaConnect.Application/Auth/` to `src/Modules/Identity/Identity.Application/Commands/Auth/`:
- LoginUser, LoginWithEntra, LogoutUser, RefreshToken, RegisterUser (each = command + handler + validator if any)

API controller (`AuthController.cs`) stays put; only using directives swap. The controller's direct `IUserRepository` injection (supplementary paths) flagged for 4.6.d.1 refactor.

T-triggers: T1+T3+T7. S-class: S2 (POST /api/Auth/login round-trip).

### 4.6.c.2 — Move 21 Users command handlers into Identity.Application

Files moved from `src/LankaConnect.Application/Users/` to `src/Modules/Identity/Identity.Application/Commands/Users/`:
- 21 handlers: AdminUpgradeUser, ApproveRoleUpgrade, CancelRoleUpgrade, CreateUser, DeleteProfilePhoto, LinkExternalProvider, RequestRoleUpgrade, RejectRoleUpgrade, UnlinkExternalProvider, UpdateCulturalInterests, UpdateLanguages, UpdatePreferredMetroAreas, UpdateUserLocation, UploadProfilePhoto, AdminActivateUser, AdminDeactivateUser, AdminDowngradeUser, AdminLockUser, AdminUnlockUser, UpdateUserBasicInfo, UpdateUserEmail

UsersController.cs stays put; using directives swap.

T-triggers: T1+T3+T7. S-class: S2 (mutator round-trip × 2 — Admin path + Profile path).

### 4.6.c.3 — Move 8 Users query handlers into Identity.Application

GetLinkedProviders, GetPendingRoleUpgrades, GetUserById, GetUserPreferredMetroAreas, SearchUsers, GetAdminUserDetails, GetAdminUserStatistics, GetAdminUsersPaged.

T-triggers: T1+T3+T7. S-class: S1.

### 4.6.c.4 — Move 1 Users event handler

MemberVerificationRequestedEventHandler.cs.

T-triggers: T3+T7. S-class: S3 (log silence).

### 4.6.c.5 — Move 4 security services + gap verification

`Identity.Infrastructure/` becomes the new home for the security adapters:
- `JwtTokenService.cs` → Identity.Infrastructure/Security/
- `EntraExternalIdService.cs` → Identity.Infrastructure/Security/
- `PasswordHashingService.cs` → Identity.Infrastructure/Security/ (gets a new `IPasswordHashingService` interface in Identity.Application/Interfaces/ per Risk #4 ruling)
- `CurrentUserService.cs` → Identity.Infrastructure/Security/

**Ports stay in legacy `LankaConnect.Application.Common.Interfaces/`** (`IJwtTokenService`, `ICurrentUserService`) per Risk #2 ruling — same precedent as W4.4 IStripePaymentService.

Gap verification grep:
- `using LankaConnect.Domain.Users` in legacy Application (excluding handlers being moved)
- `dbContext.Set<>` for `users`, `refresh_tokens` table bypass per `[[feedback_read_side_bypass_audit]]`

T-triggers: T6+T7. S-class: S2 (POST /api/Auth/login full round-trip including JWT generation).

### 4.6.d.1 — Swap cross-module read consumers to IIdentityQueries

Replace direct `IUserRepository` injection with `IIdentityQueries` in legacy Application files that READ User data only. Survey at execution time:
- `EventCancellationEmailJob` — uses `IUserRepository.GetEmailsByUserIdsAsync` for bulk recipient lookup
- `CreateEventCommandHandler` — loads organizer for audit fields
- `GetBadgesQueryHandler` — fetches creator names
- AuthController supplementary paths (post-handler-move)
- ~10 additional sites via grep

T-triggers: T3. S-class: S1 + S3.

### 4.6.d.2 — Physical move of User aggregate + IUserRepository

Two atomic moves combined:

**Move 1 — physical relocation** (`git mv`):
- `src/LankaConnect.Domain/Users/User.cs` → `src/Modules/Identity/Identity.Domain/Entities/User.cs`
- `src/LankaConnect.Domain/Users/IUserRepository.cs` → `src/Modules/Identity/Identity.Domain/Repositories/IUserRepository.cs`
- `src/LankaConnect.Domain/Users/ValueObjects/*` → `src/Modules/Identity/Identity.Domain/ValueObjects/`
- `src/LankaConnect.Domain/Users/Enums/*` → `src/Modules/Identity/Identity.Domain/Enums/`
- `src/LankaConnect.Domain/Users/DomainEvents/*` → `src/Modules/Identity/Identity.Domain/DomainEvents/`
- `src/LankaConnect.Infrastructure/Data/Repositories/UserRepository.cs` → `src/Modules/Identity/Identity.Infrastructure/Repositories/UserRepository.cs`

**Move 2 — EF Configuration STAYS** in `src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs` per W5.4.d.2 / W4.4.d.2 precedent (avoids circular ref).

**Move 3 — namespace patches** via sed on the ~128 consumer files.

**Move 4 — DI relocation** from `LankaConnect.Infrastructure.DependencyInjection` to `IdentityModule.cs`.

T-triggers: T1+T3+T6+T7. S-class: S2 + S3.

### 4.6.d.3 — Cut edges + ArchTest Rule 5

1. REMOVE `<ProjectReference LankaConnect.Domain>` from `LankaConnect.Application.csproj` if it still exists ONLY for User access (likely still needed for Event/Registration etc. — verify).
2. ADD ArchTest Rule 5 `LegacyApplication_DoesNotDependOnIdentityDomain` in `LayeringRules.cs`.
3. **Rule 6 NOT NEEDED** under current scope — User has no incoming typed navs from LankaConnect.Domain.Events/Registrations/Badges (verified by survey: all use raw Guid). Document this in Rule 5 docstring.

T-triggers: T6. Tests: ArchTest fact addition. S-class: none.

---

## ProjectReference graph (final state)

```
Identity.Contracts.csproj
  -> BuildingBlocks.Contracts                              (existing)

Identity.Domain.csproj
  -> BuildingBlocks.Abstractions                           (NEW edge - 4.6.d.2)
  -> SharedKernel.Cultural                                 (NEW edge - LanguageCode + CulturalInterest VOs)
  -> SharedKernel.Identity                                 (NEW edge - UserId primitive, if applicable)
  (DELIBERATELY no LankaConnect.Domain edge - User is self-contained)

Identity.Application.csproj
  -> Identity.Contracts                                    (NEW edge - 4.6.b)
  -> Identity.Domain                                       (NEW edge - 4.6.b)
  -> BuildingBlocks.Application                            (existing)
  -> LankaConnect.Application                              (transitional, 4.6.b -- ICommand/ICommandHandler/IUnitOfWork)
  -> LankaConnect.Shared                                   (transitional, 4.6.c.1 -- moved handlers import LankaConnect.Shared.Email.Contracts)

Identity.Infrastructure.csproj
  -> Identity.Application                                  (NEW edge - 4.6.c.5)
  -> Identity.Domain                                       (NEW edge - 4.6.c.5)
  -> LankaConnect.Infrastructure                           (transitional - Repository<T> base + AppDbContext share)

LankaConnect.Application.csproj
  -> Identity.Contracts                                    (NEW edge - 4.6.d.1)
  -> LankaConnect.Domain.Users                             REMOVED - 4.6.d.3 (the cut)

LankaConnect.Infrastructure.csproj
  -> Identity.Domain                                       (NEW edge - 4.6.d.2, mirror of W4.4.d.2)
```

---

## Contract surface (concrete — pending architect ratification at 4.6.a)

```csharp
public interface IIdentityQueries
{
    // -------- Single-user reads --------
    Task<UserSummaryDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDetailDto?> GetUserDetailAsync(Guid id, CancellationToken ct = default);
    Task<UserSummaryDto?> GetByEmailAsync(string email, CancellationToken ct = default);

    // -------- Batch reads (N+1 mitigation) --------
    Task<IReadOnlyList<UserSummaryDto>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsByUserIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    // -------- Search + admin paged list --------
    Task<IReadOnlyList<UserSearchResultDto>> SearchByNameAsync(string term, CancellationToken ct = default);
    Task<(IReadOnlyList<UserSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm, UserRoleDto? roleFilter, bool? activeFilter,
        CancellationToken ct = default);

    // -------- Counters --------
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountActiveUsersAsync(CancellationToken ct = default);
    Task<int> CountLockedAccountsAsync(CancellationToken ct = default);
    Task<IReadOnlyDictionary<UserRoleDto, int>> GetUserCountsByRoleAsync(CancellationToken ct = default);

    // -------- Existence probes --------
    Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct = default);
}

public interface IIdentityCommands
{
    // Populated at 4.6.d.1 based on actual consumer audit. Likely thin.
    // The Communications module's password-reset flow may need a mutator entry point here
    // depending on Risk #3 ruling.
}
```

**Deliberately omitted from IIdentityQueries**:
- `GetByRefreshTokenAsync`, `GetByEmailVerificationTokenAsync`, `GetByPasswordResetTokenAsync`, `GetByExternalProviderIdAsync` — these are auth-internal lookups, used only by handlers INSIDE Identity.Application. Keep them on `IUserRepository` (which becomes Identity-internal post-4.6.d.2).
- `GetUsersWithPendingRoleUpgradesAsync` — same; the GetPendingRoleUpgrades query handler moves WITH the User aggregate, so it can use the repository directly.

---

## Risks (architect-flagged — needs ruling before 4.6.a)

### Risk #1 — Cross-aggregate typed nav blocker

**RESOLVED PRE-FLIGHT.** Survey confirms User aggregate has NO typed navs into other aggregates AND no other aggregate has typed navs into User (all use raw `Guid OrganizerId`/`UserId`/`CreatorId`). This is a structural ADVANTAGE over W4.4 + W4.1. **No Wave 4.6.c.0 surgery sub-phase needed.**

### Risk #2 — `IJwtTokenService` + `ICurrentUserService` port location

`IJwtTokenService` (24 LOC, 6 methods) lives in `src/LankaConnect.Application/Common/Interfaces/`. Consumed by 5 Auth command handlers + AuthController. The adapter `JwtTokenService.cs` (220 LOC) lives in `LankaConnect.Infrastructure/Security/Services/`.

`ICurrentUserService` (57 LOC adapter, port elsewhere) — consumed by ~30 command/query handlers for current-user extraction.

**Three options**:
- **Option A** — port stays in `LankaConnect.Application.Common.Interfaces`, adapter moves to `Identity.Infrastructure/Security/` (mirrors W4.4 Risk #2 Option I, IStripePaymentService stays put).
- **Option B** — port moves to `Identity.Contracts`. Cleaner architecturally but `ICurrentUserService` returns `Guid?` only (no User type) so it fits Contracts purity; `IJwtTokenService.GenerateAccessTokenAsync(User)` takes the User type which would force Identity.Contracts to leak User-typed signatures.
- **Option C** — split: `ICurrentUserService` → Identity.Contracts (purity OK); `IJwtTokenService` stays in legacy per Option A reasoning.

**ARCHITECT RULING 2026-06-24: Option C.** Split is the only option that respects both the Contracts-purity rule AND the "port follows its signature's dirtiest type" rule (same precedent as W4.4 IStripePaymentService).

**Sequencing correction (architect Additional Finding #3)**: move `ICurrentUserService` at 4.6.a (NOT 4.6.c.5) so the **54 consumers** (corrected from "~30") across 6 modules can swap their `using LankaConnect.Application.Common.Interfaces;` → `using LankaConnect.Modules.Identity.Contracts;` in the same wave as Identity DI wire-up. Budget +2 hours for the namespace patch sweep + ban-list ArchTest re-check.

`IJwtTokenService` stays in legacy with a `// Stays per W4.6 Risk #2 ruling — leaks User type` comment.

### Risk #3 — Password reset cross-module coupling

`User.PasswordResetToken` + `User.PasswordResetTokenExpiresAt` are fields owned by User. But `SendPasswordResetCommandHandler` + `ResetPasswordCommandHandler` live in the **Communications module** (presumably because they trigger email side effects).

**The coupling**:
- Communications reads `User.PasswordResetToken` (cross-module READ)
- Communications calls `User.SetPasswordResetToken(token)` then `User.SetPassword(newPassword)` (cross-module WRITE)

**Three options**:
- **Option A** — leave the password-reset state on User aggregate; Communications continues to take `IUserRepository` (legacy edge) until a future wave splits it. Wave 4.6 ships unchanged.
- **Option B** — extract `PasswordReset` as a new Identity-Domain aggregate; Communications calls `IIdentityCommands.InitiatePasswordResetAsync(email)` and `IIdentityCommands.ResetPasswordAsync(token, newPassword)`. Requires migration (new table). Cleaner architecturally but +1-2 days wall-clock.
- **Option C** — defer to a future Wave (5.x or 6.x) after Wave 6.5 Outbox; Identity.Contracts ships with the password-reset surface intentionally omitted.

**ARCHITECT RULING 2026-06-24: Option A — but with SEMANTIC mutators, not raw mutators.** Option B (extract PasswordReset aggregate) is over-engineering for 2 fields. Option C (defer + ban-list exception) is unacceptable because it leaves Communications.Application transitively pulling Identity.Domain and forces Rule 5 to have a "we'll clean it up later" exception — historically rots.

**The architect-mandated `IIdentityCommands` surface**:
```csharp
public interface IIdentityCommands
{
    Task<Result<PasswordResetInitiatedDto>> InitiatePasswordResetAsync(
        string email, TimeSpan tokenLifetime, bool forceResend, CancellationToken ct);

    Task<Result<PasswordResetCompletedDto>> CompletePasswordResetAsync(
        string token, string? emailFallback, string newPlaintextPassword, CancellationToken ct);
}
```

**CRITICAL — do NOT expose raw mutators** like `SetPasswordResetTokenAsync(token, expiresAt)` + `SetPasswordAsync(hashedPassword)`. That would:
- Leak the password-hashing service requirement back to Communications (it currently injects `IPasswordHashingService` + calls `HashPassword()` before mutation)
- Leak the token-lifetime + 5-minute-resend-throttle business rules

The Identity-side adapter (`IdentityCommands` in Identity.Application) owns: hashing, expiry, throttle, refresh-token revocation, recently-sent check. Communications keeps ONLY the email-side-effect orchestration.

**Factual plan correction (architect Additional Finding #2)**: `PasswordResetRequestedAt` field does NOT exist on User. The 5-minute resend throttle is derived as `PasswordResetTokenExpiresAt.AddHours(-1)`. The Identity adapter must replicate that derivation, not reach for a non-existent field.

**Sequencing**: 4.6.d.1 adds the surface + adapter + swaps Communications.SendPasswordReset + ResetPassword handlers to inject IIdentityCommands (dropping IUserRepository + IPasswordHashingService). 4.6.d.3 lands Rule 5 clean.

### Risk #4 — `PasswordHashingService` has no interface

The adapter `PasswordHashingService.cs` (190 LOC) is instantiated directly in `LankaConnect.Infrastructure.DependencyInjection`:
```csharp
services.AddScoped<PasswordHashingService>();
```

No `IPasswordHashingService` interface. Consumers (4 command handlers — Register/Login/ResetPassword/SetPassword) inject the concrete type.

**Options**:
- **Option A** — extract `IPasswordHashingService` interface during 4.6.c.5 + move adapter to `Identity.Infrastructure/Security/`. Consumers swap to the interface. Standard hexagonal pattern.
- **Option B** — leave as-is; Identity.Infrastructure adapters take the concrete type. Ugly but simpler.

**ARCHITECT RULING 2026-06-24: Option A — BUT THE PREMISE IS WRONG.** `IPasswordHashingService` ALREADY EXISTS at `src/LankaConnect.Application/Common/Interfaces/IPasswordHashingService.cs` (3 methods: HashPassword, VerifyPassword, ValidatePasswordStrength). Already registered as `AddScoped<IPasswordHashingService, PasswordHashingService>()` in DependencyInjection.cs:332. RegisterUserHandler / LoginUserHandler / ResetPasswordCommandHandler / AdminController inject the interface, NOT the concrete type.

**The pre-flight survey was wrong** — survey table row "no interface exists" is factually incorrect. So the work at 4.6.c.5 is JUST the adapter move, NOT interface extraction.

The existing `IPasswordHashingService` stays in `LankaConnect.Application.Common.Interfaces` (zero churn) — port has 4 in-Identity consumers + zero cross-module consumers (Communications becomes a caller of `IIdentityCommands.CompletePasswordResetAsync` per Risk #3 ruling, so it stops needing IPasswordHashingService directly). Promotion to Contracts deferred to a future cleanup wave.

**Drop the planned "extract interface" step from the 4.6.c.5 checklist.** Saves ~1 hour wall-clock.

### Risk #5 — `EntraExternalIdService` integration scope

219-LOC service handles Microsoft Entra + federated Facebook/Google/Apple logins via a single adapter. Lives in `Infrastructure/Security/Services/`. Used by `LoginWithEntraCommandHandler` + `LinkExternalProviderCommandHandler`.

**Question**: stays as Identity.Infrastructure-internal, or gets an `IIdentityCommands.LoginWithExternalProviderAsync` surface for cross-module callers? Survey found ZERO cross-module callers (all consumers are within Auth/), so the interface stays internal.

**Recommended path: no change**. EntraExternalIdService stays Identity-Infrastructure-internal at 4.6.c.5.

**No architect input needed** — encoded directly.

### Risk #6 — `IUserRepository` 18-method surface is wide

Of the 18 methods, 11 are auth-internal (token lookups, role-upgrade queries) and stay on the repository (Identity-internal). The other 7 are cross-module reads exposed via `IIdentityQueries` (see Contract surface section above).

**Risk**: future contributors may be tempted to call `IUserRepository` directly from legacy code post-4.6.d.2 (the ProjectReference will still resolve transitively through Identity.Infrastructure). The ArchTest Rule 5 catches that, but the docstring must explain the intent clearly.

**No architect input needed** — encoded in Rule 5 docstring.

### Risk #7 — `dbContext.Set<>` bypass class (W5.4.d.4 lesson)

`[[feedback_read_side_bypass_audit]]`: 4.6.c.5 gap audit MUST grep `dbContext.Set<>` for `users`, `refresh_tokens`, `external_logins` tables across both `src/LankaConnect.Application/` AND `src/Modules/`. Any read-side handler that bypasses the aggregate and queries the table directly would survive unit tests but throw at runtime.

**No architect input needed** — encoded in 4.6.c.5 sub-phase definition.

---

## Implementation checklist (next-session resumption)

- [x] **Pre-flight (DONE 2026-06-24)**: architect ruled Risk #2 = Option C, Risk #3 = Option A (semantic mutators), Risk #4 = Option A (with IPasswordHashingService factual correction). 5 additional findings folded into sub-phases below.
- [ ] **4.6.a**: define Identity.Contracts surface + Contracts.Tests + ArchTest rule
- [ ] **4.6.b**: implement IdentityQueries + IdentityModule DI + ~10 query tests
- [ ] **4.6.c.1**: move 5 Auth command handlers into Identity.Application
- [ ] **4.6.c.2**: move 21 Users command handlers into Identity.Application
- [ ] **4.6.c.3**: move 8 Users query handlers into Identity.Application
- [ ] **4.6.c.4**: move 1 Users event handler into Identity.Application
- [ ] **4.6.c.5**: move 4 security service adapters into Identity.Infrastructure + extract IPasswordHashingService interface (Risk #4 Option A) + gap-verification grep (including W5.4.d.4 bypass check)
- [ ] **4.6.d.1**: swap cross-module read consumers to IIdentityQueries; populate IIdentityCommands surface based on consumer audit (including Communications password-reset surface per Risk #3 Option A)
- [ ] **4.6.d.2**: physical move of User aggregate + value objects + enums + IUserRepository + UserRepository.cs (8+ source files); namespace patches; Identity DI wire-up
- [ ] **4.6.d.3**: cut LankaConnect.Application → LankaConnect.Domain.Users edge + ArchTest Rule 5 (LegacyApplication_DoesNotDependOnIdentityDomain). No Rule 6 (User has no incoming typed navs from LankaConnect.Domain).

---

## Scope comparison: Wave 4.6 vs Wave 4.4

| Dimension | Wave 4.4 Payments | Wave 4.6 Identity |
|---|---|---|
| Cross-aggregate typed nav blocker | ⚠️ YES (Registration.RefundRequests) | ✅ NO (User has no nav into other aggregates) |
| EF migration needed | ✅ NO | ✅ NO |
| Command handlers to move | 7 | 26 (5 Auth + 21 Users) — **3.7× larger** |
| Query handlers to move | 2 | 8 — **4× larger** |
| Event handlers to move | 17 (helper-heavy) | 1 |
| Service implementations to move | 6 (interfaces stay in legacy) | 4 (3 interfaces stay, 1 new interface extracted) |
| Repository interfaces to move | 3 (IStripeCustomer + IStripeWebhook + IRefundRequest) | 1 (IUserRepository — 18 methods) |
| Cross-module consumer files | ~15 | ~128 (a top-10 sample identified) |
| Aggregate physical move risk | LOW (RefundRequest stayed in legacy) | MEDIUM (User aggregate moves; 128 consumers' using directives must update) |
| Permanent structural compromise | Payments.Domain → LankaConnect.Domain (for RefundRequest type) | None expected (User is self-contained) |
| Architect ruling load | 2 risks (Risk #1 Option A, Risk #2 Option I) | 3 risks (Risk #2 Option C recommended, Risk #3 Option A, Risk #4 Option A) |
| Est. wall-clock | 1 day | 2-3 days |

**Bottom line**: Wave 4.6 is **structurally simpler** than W4.4 (no typed-nav blocker, no permanent compromise) but **operationally larger** (3-4× the handler count + 8× the cross-module footprint).

---

## Why labeled `Wave 4.6` (not `Wave 5.5`)

Per `[[feedback_wave_numbering_correction]]` (2026-06-23): per-wave doc + commit + ArchTest labels follow the master-plan Wave 4.6 numbering from the start. No more "W5.x" mis-labeling drift.
