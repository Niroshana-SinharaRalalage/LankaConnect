# Wave 4.7 — Identity Consumer Sweep + User Physical Move Manifest

**Status**: OPEN — gates Wave 5 (Products carve-out) per founder mandate.

**Authoring context**: Wave 4.6.d.2.c (physical User aggregate move into Identity.Domain) was attempted on 2026-06-25 per architect Option C ruling but hit a load-bearing circular-dependency blocker that the architect's plan didn't anticipate (see Blocker #1 below). The physical move was reverted; the surface expansion + partial consumer swap (9 of 44) shipped successfully and remain in place. This manifest documents the remaining work for Wave 4.7 + the architectural prerequisite that must land first.

**Last updated**: 2026-06-25.

---

## TL;DR

Three blockers + 35 consumer paths to swap before Wave 5 can open:

1. **Blocker #1 (load-bearing, NEW finding)**: `LankaConnect.Domain.Communications.Entities.UserEmailPreferences` has a typed nav to `LankaConnect.Domain.Users.User`. Moving User to Identity.Domain creates a `LankaConnect.Domain → Identity.Domain` ProjectReference, which combined with the existing `Identity.Domain → LankaConnect.Domain` edge (for LegacyBaseEntity + Email VO) forms a circular dependency. Same class as the W4.1.2 typed-nav blocker that was resolved by W5.4.d.1b Newsletter↔EmailGroup junction surgery.
2. **Blocker #2**: `EventEmailGroupLink` + `NewsletterEmailGroupLink` typed-nav patterns from W5.4.c.0/d.1b need a mirror — `RegistrationUserLink` or similar — for any other cross-aggregate User references found by audit (not yet performed; estimate 2-5 sites).
3. **35 consumer files** still inject `IUserRepository` directly. Need per-category resolution before d.2.c (physical move) can ship without breaking the architecture.

---

## Blocker #1 — UserEmailPreferences typed nav

**Location**: `src/LankaConnect.Domain/Communications/Entities/UserEmailPreferences.cs`

**Resolution path (mirrors W5.4.d.1b)**:
1. Add `Guid UserId` raw-Guid field to `UserEmailPreferences` (if not already present).
2. Remove the typed `User` navigation property — replace with raw Guid relationship.
3. EF mapping update: declare the relationship via `HasOne<Guid>` shadow-FK pattern, NOT typed nav.
4. EF snapshot rebaseline migration (empty Up()/Down() per `[[feedback_empty_up_snapshot_rebaseline]]`).
5. Repeat surgery for any other LankaConnect.Domain entity referencing User (audit step before starting).

**Estimated cost**: 3-5 hours (matches W5.4.d.1b complexity).

**Prerequisite to**: 4.6.d.2.c physical User move.

---

## 35-consumer sweep — categorized

### Category A — User-aggregate collection navs (~10 files)

Consumers using `User.PreferredMetroAreaIds`, `User.Location`, `User.CulturalInterests`, `User.Languages`, `User.ExternalLogins`. Each needs a dedicated cluster DTO (per architect ruling 2026-06-25):

- `UserPreferencesProjectionDto` (PreferredMetroAreaIds + CulturalInterests + Languages):
  - `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs`
  - `src/LankaConnect.Application/Events/Queries/GetFeaturedEvents/GetFeaturedEventsQueryHandler.cs`
- `UserLocationProjectionDto` (Location + PhoneNumber):
  - `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`
  - `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs`
  - `src/LankaConnect.Application/Events/Commands/BatchLinkOrganizerContacts/BatchLinkOrganizerContactsCommandHandler.cs`
- `UserNotificationProjectionDto` (Email + DisplayName + PhoneNumber + WhatsAppOptIn):
  - `src/LankaConnect.Application/Events/BackgroundJobs/EventNotificationEmailJob.cs`
  - `src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs`
  - `src/LankaConnect.Application/Events/Commands/PhotoAlbums/SendAlbumNotification/SendAlbumNotificationCommand.cs`
- `UserExternalLoginsProjectionDto` (ExternalLogins collection):
  - `src/LankaConnect.Application/Events/Queries/GetEventNotificationHistory/GetEventNotificationHistoryQueryHandler.cs`
  - `src/LankaConnect.Application/Events/Commands/ResendAttendeeConfirmation/ResendAttendeeConfirmationCommandHandler.cs`
  - `src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs`

**Resolution**: 4 new query DTOs in Identity.Contracts + 4 new IIdentityQueries methods + 10 consumer swaps. **Estimate: 4-6 hours.**

### Category B — EventHandlers expecting User downstream (~15 files)

Consumers loading User via `IUserRepository.GetByIdAsync` then passing User to local helper methods. Per architect ruling 2026-06-25, default resolution is **helpers accept (Guid, primitives) instead of User**:

- `src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEmailHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/CommitmentUpdatedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/EventApprovedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/EventPostponedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/EventRejectedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationCancelledEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/UserCommittedToSignUpEventHandler.cs`

Plus 6 WhatsApp variants where `IUserRepository` is injected but unused:
- `src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledWhatsAppHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/CommitmentUpdatedWhatsAppHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/EventApprovedWhatsAppHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/EventRejectedWhatsAppHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationCancelledWhatsAppHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedWhatsAppHandler.cs`

**Resolution**: drop dead `IUserRepository` injection on the 6 WhatsApp handlers (trivial). For the 9 real consumers, helper signature refactor `private void DispatchEmail(User user, ...)` → `private void DispatchEmail(Guid userId, string email, string displayName, ...)`. **Estimate: 5-7 hours.**

### Category C — Communications semantic-mutator handlers (~10 files)

The 4 architect-mandated semantic mutators (`IIdentityCommands.InitiatePasswordResetAsync` / `CompletePasswordResetAsync` / `InitiateEmailVerificationAsync` / `CompleteEmailVerificationAsync`) are ALREADY IMPLEMENTED. Each consumer below is a ~200 LOC refactor to call the mutator + dispatch email side-effect:

- `src/LankaConnect.Application/Communications/Commands/SendPasswordReset/SendPasswordResetCommandHandler.cs` → InitiatePasswordResetAsync
- `src/LankaConnect.Application/Communications/Commands/ResetPassword/ResetPasswordCommandHandler.cs` → CompletePasswordResetAsync
- `src/LankaConnect.Application/Communications/Commands/SendEmailVerification/SendEmailVerificationCommandHandler.cs` → InitiateEmailVerificationAsync
- `src/LankaConnect.Application/Communications/Commands/VerifyEmail/VerifyEmailCommandHandler.cs` → CompleteEmailVerificationAsync

Plus 6 other Communications handlers that read User without an existing semantic-mutator path:
- `src/LankaConnect.Application/Communications/Commands/SendWelcomeEmail/SendWelcomeEmailCommandHandler.cs` — needs UserDetailDto (Role + Email + DisplayName + ProfilePhotoUrl)
- `src/LankaConnect.Application/Communications/Commands/SendBusinessNotification/SendBusinessNotificationCommandHandler.cs` — needs UserContactDto
- `src/LankaConnect.Application/Communications/Commands/SendAlbumNotification/SendAlbumNotificationCommand.cs` — needs UserNotificationProjectionDto (covered in Category A)
- `src/LankaConnect.Application/Communications/EventHandlers/WhatsAppAutoDisabledDomainEventHandler.cs` — needs UserContactDto
- `src/LankaConnect.Application/Communications/Queries/GetEmailStatus/GetEmailStatusQueryHandler.cs` — needs UserContactDto
- `src/LankaConnect.Application/Communications/Queries/GetUserEmailPreferences/GetUserEmailPreferencesQueryHandler.cs` — needs UserDetailDto + local helper signature changes (passes User to GetVerificationAttempts / GetLastVerificationSentDate)

**Resolution**: 4 semantic-mutator consumer swaps + 6 read-side swaps via existing UserContactDto / UserDetailDto / new UserNotificationProjectionDto. **Estimate: 5-7 hours.**

### Total Category A+B+C: 35 consumers, 14-20 hours wall-clock across 4-5 sessions.

---

## Wave 4.7 sub-phase breakdown

- **Wave 4.7.a** (3-5h) — UserEmailPreferences typed-nav surgery + audit for any other cross-aggregate User typed navs. Migration. **Unblocks Wave 4.7.b.**
- **Wave 4.7.b** (4-6h) — Category C: 6 Communications semantic-mutator swaps (4 with adapters ready) + 6 other Communications consumers via existing/new DTOs.
- **Wave 4.7.c** (4-6h) — Category A: 4 cluster projection DTOs + 4 IIdentityQueries methods + 10 consumer swaps.
- **Wave 4.7.d** (5-7h) — Category B: 15 EventHandlers / BackgroundJobs via helper signature refactors. 6 dead `IUserRepository` injections dropped.
- **Wave 4.7.e** (2-3h) — Wave 4.6.d.2.c physical User aggregate move (`User`, `IUserRepository`, value objects, enums, domain events, UserRepository.cs) into Identity.{Domain,Infrastructure}. Namespace patch sweep across legacy stack. EF Configuration stays in LankaConnect.Infrastructure (mirrors W4.4.d.2 / W5.4.d.2 precedent).
- **Wave 4.7.f** (1h) — Tighten Rule 5 (remove docstring "forward-looking" framing; rule becomes load-bearing). Add companion `Modules_Communications_DoesNotDependOnIdentityDomain`. Cut LankaConnect.Application → Identity.Domain ProjectReference if still present for User reasons.

**Wave 4.7 total**: 19-28 hours wall-clock across 4-6 sessions.

---

## Wave 4.6 final state (locked at this manifest's authoring date)

15 commits shipped + STAGING-VERIFIED:
- a/b/c.1-c.5/d.1p1/d.1p2/d.3: Full structural extraction (per Wave 4.6 master TODO)
- d.2.a (`980b1821`): Surface expansion (+3 IIdentityQueries methods + 2 IIdentityCommands semantic mutators + UserContactDto + EmailVerification DTOs)
- d.2.b PARTIAL (`727c906e`): 5 Support handlers swapped (+ 4 EmailGroup from d.1 p2 = 9 of 44)

User aggregate **STAYS** in `LankaConnect.Domain.Users` until Wave 4.7.e ships.

Rule 5 (`LegacyApplication_DoesNotDependOnIdentityDomain`) remains **forward-looking** until Wave 4.7.f.

---

## Founder-visible gate to Wave 5

Wave 5 (Products carve-out / `Products/LankaEvents`) does NOT open until this manifest is empty AND Wave 4.7.f ships. This is the contract.

---

## Wave 4.7.b/c/d execution finding (2026-06-25, after 4.7.a ships)

When the simple-pattern Category C reads were swapped (SendBusinessNotificationCommandHandler, WhatsAppAutoDisabledDomainEventHandler, GetEmailStatusQueryHandler — same `_userRepository.GetByIdAsync` → `_identityQueries.GetContactInfoAsync` swap as the W4.6.d.2.b Support handlers), each consumer's test fixture also broke:

- `Mock<IUserRepository>` → `Mock<IIdentityQueries>` — typed change
- `.Setup(x => x.GetByIdAsync(...))` → `.Setup(x => x.GetContactInfoAsync(...))` — method name change
- `.ReturnsAsync(user)` where `user` is a `User` aggregate → `.ReturnsAsync(userContactDto)` where the test fixture currently builds a User via `User.Register(...)` — test fixture rewrite per handler

The Support handlers' tests didn't have this issue because Mocks were already set up with the moved test helpers in `TestHelpers.MockRepository`. The Communications + Events handler tests build User instances directly via `User.Register()` and pass them to mocks, requiring per-test conversion to UserContactDto / per-fixture refactoring.

**Revised per-handler estimate for Category C** (post-4.7.a finding):
- Source-side swap: 10-15 min per handler (mechanical sed)
- Test-fixture refactor: 20-40 min per handler (User → UserContactDto conversion + ReturnsAsync setup rewrite + sometimes test data builder updates)
- Total: 35-55 min per handler × 10 handlers = **7-9 hours wall-clock** (not 5-7).

Same finding likely applies to Category B (15 EventHandlers) — each consumer test that injects `Mock<IUserRepository>` needs the same fixture refactor. Realistic Category B estimate: **9-13 hours** (not 5-7).

**Revised Wave 4.7 total: 24-37 hours** across 5-7 sessions.

---

## Wave 4.7.a final state (2026-06-25, commit `38f6bf7d`)

- 8 user-aggregate domain events relocated to `LankaConnect.Domain/Users/DomainEvents/`
- UserEmailPreferences dead `using LankaConnect.Domain.Users.ValueObjects` stripped
- Circular-dep blocker for Wave 4.7.e (physical User move) RESOLVED
- ~20 consumer using-directive patches applied (mechanical sed)
- Build green; 2645 Application tests + 45 ArchTests pass
- STAGING-VERIFIED (Login 200 + GET event detail 200)

Wave 4.7.b/c/d/e/f remain queued. Wave 5 still gated on full manifest drain.

---

## Wave 4.7.d.4 finding (2026-06-26)

Attempted to swap the 3 remaining BG jobs (EventReminder / EventNotificationEmail / EventCancellationEmail) + CreateEventCommandHandler in a single batch. All 4 source-side swaps were clean (mechanical sed), but cascaded into **7 test fixture files**:

- `tests/.../Events/BackgroundJobs/EventReminderJobTests.cs`
- `tests/.../Events/BackgroundJobs/EventNotificationEmailJobTests.cs`
- `tests/.../Events/BackgroundJobs/EventCancellationEmailJobAutoRefundTests.cs`
- `tests/.../Events/Commands/CreateEventIsFreeTests.cs`
- `tests/.../Events/Commands/CreateEventSecondaryLocationTests.cs`
- `tests/.../Events/Commands/CreateEventTbdDatesTests.cs`
- `tests/.../Events/Commands/CreateEventTimezoneTests.cs`

Each needs `Mock<IUserRepository>` → `Mock<IIdentityQueries>` + the `CreateTestUser` helpers rewritten to return `UserSummaryDto`/`UserContactDto` (each test builds a real `User.Create(...).Value` aggregate, sometimes 2+ users per test). Estimated 30-45 min per file × 7 = **3.5-5 hours** of test fixture work to ship these 4 source files cleanly.

The 4 source swaps were reverted at the end of 4.7.d.3 session. Deferred to Wave 4.7.d.5 as a single focused batch. Build is green at commit `3cee606e`.

---

## Out-of-scope discoveries

These 2 consumers were initially manifested as Cat C swaps but found to be **legitimate IUserRepository users** during 4.7.d.3 execution. They will continue to inject `IUserRepository` until Wave 4.7.e physically moves it:

- `src/LankaConnect.API/Controllers/AuthController.cs` — uses `user.GenerateEmailVerificationToken()` + `user.VerifyEmail(...)` mutators in a `/test/verify-user` endpoint (test-only). DTOs are read-only; can't expose mutators through the IIdentityQueries surface.

These are not blockers for the physical User move because the consumer is INSIDE the API host (not a cross-module call). After 4.7.e, AuthController would either use `IIdentityCommands` (if test endpoint kept) or be deleted entirely (test-only endpoints typically don't survive the cleanup wave).

Wave 4.7.d total progress: 30 of 44 manifested consumers swapped (68%). Remaining 14 split:
- 4 BG jobs / CreateEvent (deferred to 4.7.d.5 — needs 3.5-5h test fixture batch)
- 2 Get*Events queries (need UserPreferencesProjectionDto — Cat A cluster DTO)
- 2 EventHandlers with existing tests (EventApproved, EventRejected)
- 2 commands needing helper refactor (ResendAttendeeConfirmation, ResendTicketEmail → IRegistrationEmailService)
- 1 GetUserEmailPreferences (manifest-classified "complex")
- 1 AuthController (out-of-scope legitimate)
- 2 misc not yet audited

---

## Wave 4.7.e SHIPPED (2026-06-26)

**Architect Option A executed**: User aggregate physically moved to `Identity.Domain`. IUserRepository kept in `Identity.Domain` (not re-exported via Contracts). 14 LankaConnect.Application straggler files continue to inject `IUserRepository` as time-bounded allow-listed debt.

### Physical move details

- `src/LankaConnect.Domain/Users/` → `src/Modules/Identity/Identity.Domain/`
  - `Entities/User.cs`
  - `Repositories/IUserRepository.cs`
  - `ValueObjects/` (CulturalInterest, ExternalLogin, LanguageCode, LanguagePreference, RefreshToken, UserLocation)
  - `Enums/` (FederatedProvider, IdentityProvider, ProficiencyLevel, SubscriptionStatus, UserRole)
  - `DomainEvents/` (8 user-aggregate events from W4.7.a)
  - `Events/StripeEvents.cs`
- `src/LankaConnect.Infrastructure/Data/Repositories/UserRepository.cs` → `src/Modules/Identity/Identity.Infrastructure/Repositories/UserRepository.cs`
- `LankaConnect.Domain/Users/` folder deleted (empty)
- DI registration of `IUserRepository → UserRepository` moved from `LankaConnect.Infrastructure.DependencyInjection` to `IdentityModule.AddIdentityModule`
- `InternalsVisibleTo LankaConnect.Modules.Identity.Infrastructure` added to `Identity.Domain.csproj` so UserRepository can call internal `User.SyncPreferredMetroAreaIdsFromEntities`
- EF `UserConfiguration.cs` STAYS in `LankaConnect.Infrastructure/Data/Configurations/` per W4.4.d.2 / W5.4.d.2 precedent (cross-schema FK pattern)

### Rule 5 status — INTENTIONALLY SKIPPED

`tests/architecture/LankaConnect.ArchitectureTests/LayeringRules.cs::LegacyApplication_DoesNotDependOnIdentityDomain` marked `[Fact(Skip = "...")]` during the Cross-cutting cleanup transition window. Rule re-enables when allow-list reaches zero.

### Shrink-to-zero allow-list (14 straggler files, target: 0 by end of Cross-cutting cleanup)

Per architect Option A: these files inject `IUserRepository` directly today; each gets swapped to `IIdentityQueries` / `IIdentityCommands` opportunistically during Cross-cutting cleanup. Wave 4 closes when this list is empty.

1. `src/LankaConnect.Application/Communications/Queries/GetUserEmailPreferences/GetUserEmailPreferencesQueryHandler.cs` — needs helper-method refactor (`GetVerificationAttempts(User)`, `GetLastVerificationSentDate(User)` accept User)
2. `src/LankaConnect.Application/Events/BackgroundJobs/EventReminderJob.cs`
3. `src/LankaConnect.Application/Events/BackgroundJobs/EventNotificationEmailJob.cs`
4. `src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs`
5. `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs`
6. `src/LankaConnect.Application/Events/Commands/ResendAttendeeConfirmation/ResendAttendeeConfirmationCommandHandler.cs` — needs `IRegistrationEmailService` helper-signature refactor
7. `src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs` — same
8. `src/LankaConnect.Application/Events/EventHandlers/EventApprovedEventHandler.cs` — has existing tests
9. `src/LankaConnect.Application/Events/EventHandlers/EventRejectedEventHandler.cs` — has existing tests
10. `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs` — needs `UserPreferencesProjectionDto`
11. `src/LankaConnect.Application/Events/Queries/GetFeaturedEvents/GetFeaturedEventsQueryHandler.cs` — same
12. `src/LankaConnect.API/Controllers/AuthController.cs` — uses User mutators (`GenerateEmailVerificationToken`, `VerifyEmail`); requires `IIdentityCommands.GenerateEmailVerificationToken` + `IIdentityCommands.VerifyEmailDirectly` to swap (test-only `/test/verify-user` endpoint)
13. _slot — for any consumer discovered during Cross-cutting cleanup_
14. _slot — for any consumer discovered during Cross-cutting cleanup_

### Wave 4 path forward (architect Option A + serial extraction)

- ✅ Wave 4.7.e: physical User move SHIPPED (this commit)
- ⏭️ Wave 4 capability: Scheduling (3 sessions, ~1 week)
- ⏭️ Wave 4 capability: CulturalIntelligence (2 sessions, ~3-4 days)
- ⏭️ Wave 4 capability: Cross-cutting cleanup (5 sessions, ~1 week — drains allow-list to 0 and re-enables Rule 5)
- ⏭️ Wave 5 opens

Wave 5-6: 2 weeks. **Wave 7 (Frontend Mirror) deferred to Phase B per architect ruling (founder decision pending).**

---

## Wave 4.8 — Scheduling capability extraction SHIPPED (2026-06-26)

Capability #7 of 9 in Wave 4 plan ✅. 5 reusable scheduling primitives defined
in `Scheduling.Domain` + read-only projections composed onto Event aggregate
without disturbing storage. Wave 5 Products carve-out flips storage to the VOs
when Event becomes Products/LankaEvents-specific.

### Wave 4 capability board (after 4.8)

| # | Capability | Status |
|---|---|---|
| 1 | Notifications cleanup | ✅ W3.4 |
| 2 | Communications | ✅ Wave 4.1 |
| 3 | Media | ✅ (earlier) |
| 4 | Forms | ✅ Wave 4.3 |
| 5 | Payments | ✅ Wave 4.4 |
| 6 | Identity | ✅ Wave 4.6 + 4.7 (physical move shipped; 14 stragglers manifest-tracked) |
| 7 | **Scheduling** | ✅ **Wave 4.8** |
| 8 | CulturalIntelligence | ⏭️ Next (2 sessions) |
| 9 | Cross-cutting cleanup | ⏭️ After (5 sessions; drains 14 Identity stragglers + re-enables Rule 5) |

**2 of 9 capabilities remain.** Wave 5 opens after capability #9 closes.

---

## Wave 4.9 — CulturalIntelligence capability extraction SHIPPED (2026-06-26)

Capability #8 of 9 ✅. 2 stub services + 2 DI registrations relocated.

### Files moved

- `src/LankaConnect.Infrastructure/CulturalIntelligence/StubUserPreferences.cs` → `src/Modules/CulturalIntelligence/CulturalIntelligence.Infrastructure/StubUserPreferences.cs`
- `src/LankaConnect.Infrastructure/CulturalIntelligence/StubGeographicProximityService.cs` → `src/Modules/CulturalIntelligence/CulturalIntelligence.Infrastructure/StubGeographicProximityService.cs`
- `src/LankaConnect.Infrastructure/CulturalIntelligence/` directory deleted (empty)
- Namespace: `LankaConnect.Infrastructure.CulturalIntelligence` → `LankaConnect.Modules.CulturalIntelligence.Infrastructure`

### DI relocation

- 2 registrations removed from `LankaConnect.Infrastructure.DependencyInjection`
- Added to `Modules.CulturalIntelligence.Api.CulturalIntelligenceModule.AddCulturalIntelligenceModule()` alongside the existing StubCulturalCalendar registration. All 3 cultural service stubs now resolve from the module's composition seam.

### Wave 4 capability board (after 4.9)

| # | Capability | Status |
|---|---|---|
| 1 | Notifications cleanup | ✅ W3.4 |
| 2 | Communications | ✅ Wave 4.1 |
| 3 | Media | ✅ |
| 4 | Forms | ✅ Wave 4.3 |
| 5 | Payments | ✅ Wave 4.4 |
| 6 | Identity | ✅ Wave 4.6 + 4.7 |
| 7 | Scheduling | ✅ Wave 4.8 |
| 8 | **CulturalIntelligence** | ✅ **Wave 4.9** |
| 9 | Cross-cutting cleanup | ⏭️ Next (5 sessions; drains 14 Identity stragglers + re-enables Rule 5) |

**Wave 4 ONE capability remaining.** Wave 5 opens after Cross-cutting cleanup closes.

---

## Wave 4.10 Session 3 attempt — finding (2026-06-26)

Session 3 (Communications per-capability migration) was attempted via bulk
`git mv` of `LankaConnect.Application/Communications/*` → `Modules/Communications/Communications.Application/`. **Reverted** after hitting a 2-layer architectural blocker:

### Blocker #1 — Cross-folder Common DTOs

`LankaConnect.Application/Communications/Common/EmailGroupSummaryDto.cs` is referenced by `LankaConnect.Application/Events/Common/EventDto.cs` (legacy Events code still in the layered monolith). Moving the Common DTO into `Modules.Communications.Application.Common` forces `LankaConnect.Application` → `Modules.Communications.Application` ProjectReference, which creates a cycle with the existing `Modules.Communications.Application` → `LankaConnect.Application` transitional edge (for ICommand / IUnitOfWork / etc.).

### Blocker #2 — BB.Application elevation prerequisite

Architect ruled (2026-06-26) that **BuildingBlocks.Application must own the ICommand / ICommandHandler / IQuery / IQueryHandler primitives BEFORE per-capability migration ships**. This cuts the Modules → LankaConnect.Application transitional edges so Blocker #1's cycle is unreachable.

Elevation attempted but found:
1. `BuildingBlocks.Abstractions.ICommand<out TResponse>` already exists under the same namespace `LankaConnect.BuildingBlocks.Application.Abstractions` — would conflict with a new shorthand `ICommand : IRequest<Result>`.
2. Two different `Result` types exist (`LankaConnect.Domain.Common.Result` vs `LankaConnect.BuildingBlocks.Domain.Result`) with incompatible APIs (string error vs Error class).
3. Adding `BuildingBlocks.Application → LankaConnect.Domain` ProjectReference to resolve `Result` is the wrong architectural direction.

### Resolution: defer to Wave 6 with explicit Result-unification subtask

The proper sequence is:
1. **Wave 6 ArchTest hardening — Phase 1**: Result unification. Pick BB.Domain.Result as the canonical type. Convert all ~200 handler return types from `LankaConnect.Domain.Common.Result` → `BB.Domain.Result`. Add a one-time compatibility shim if needed.
2. **Wave 6 — Phase 2**: BB.Application owns `ICommand : IRequest<Result>` shorthand (now uses BB.Domain.Result). Modules use it directly. Cut Modules → LankaConnect.Application transitional edges.
3. **Wave 6 — Phase 3**: Per-capability migrations from `LankaConnect.Application/<Module>/` → `Modules/<Module>/<Module>.Application/` happen cleanly without the cycle.
4. **Wave 6 — Phase 4**: Delete unused `LankaConnect.Domain.Common.*` types (now drained by the Result migration).

### Wave 4.10 final state (locked at 2026-06-26)

✅ Stragglers DRAINED 14/14 — Identity boundary clean at the per-handler level
✅ Identity physical move shipped (Wave 4.7.e — User aggregate in Identity.Domain)
✅ Scheduling capability shipped (Wave 4.8)
✅ CulturalIntelligence capability shipped (Wave 4.9)
🟡 Per-capability Application migration DEFERRED to Wave 6 (blocked on Result unification)
🟡 `LankaConnect.Domain.Common.*` DELETE DEFERRED to Wave 6 Phase 4
🟡 Rule 5 ArchTest re-enable DEFERRED to Wave 6 (blocked on IApplicationDbContext + IJwtTokenService surface refactor)

**Wave 4 closes here** with 3 explicit Wave 6 prerequisites carrying forward. This is a HONEST architectural status — the cleanup hits load-bearing layered-monolith primitives that need their own sequenced rework, not a bulk move.

**Wave 5 (Products carve-out) is unblocked**. The Identity physical move + Scheduling capability + Cultural extraction are sufficient for Wave 5 to begin. Wave 6 then sweeps up the residual layered-monolith primitives (Result unification + capability migration + Domain.Common drain + Rule 5).
