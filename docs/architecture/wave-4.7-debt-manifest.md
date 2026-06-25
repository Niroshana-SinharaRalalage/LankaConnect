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
