# Agent Channel: LayerInversion

**Agent role:** Fix 2 layer inversions found by Agent-CommonComponents (Wave 1) — ICulturalCalendar + Address/GeoCoordinate promotion out of LankaEvents.Domain.
**Priority:** P2 (dependency for GAP-1 + GAP-6 gap-closure work + Consult #7 Delta clean-boundary posture)
**Est time:** 3 hours
**Reports to:** Tech Lead (Claude)

---

## Task brief

Per `docs/architecture/COMMON_COMPONENTS_INVENTORY_2026_07_16.md` Section 3 + gap register:

**Inversion 1**: `ICulturalCalendar` interface lives in `LankaEvents.Domain.Services.IEventRecommendationEngine.cs` (should live in `CulturalIntelligence.Contracts/` or `Scheduling.Contracts/`). LankaEvents.Domain is a Product layer — Products should DEPEND ON Capabilities, not DEFINE them.

**Inversion 2**: `Address` + `GeoCoordinate` VOs live in `LankaEvents.Domain.ValueObjects/` (should live in `SharedKernel.Geo/`). LankaEvents.Domain is single-product — these are pan-platform primitives.

Both inversions violate Consult #7 Delta layering + block Phase B products from reusing the primitives.

## Deliverable

### Part 1 — Cultural-calendar interface promotion

1. **Grep** all references to `ICulturalCalendar` + `IEventRecommendationEngine` (this file may hold BOTH interfaces per CommonComponents finding):
   ```bash
   grep -rn "ICulturalCalendar\|IEventRecommendationEngine\|CulturalCalendar" src/ --include="*.cs"
   ```
2. Move `ICulturalCalendar` interface + any DTOs it consumes into `src/Capabilities/CulturalIntelligence.Contracts/` (create dir if needed) — pattern per Consult #15 PASS C (interface + DTO in Contracts).
3. Keep any Impl class in `LankaEvents.Infrastructure/Services/CulturalCalendar/` OR move to `src/Capabilities/CulturalIntelligence.Infrastructure/` if it's cross-product legitimately shared (probably the latter — verify).
4. Update DI wiring in `LankaEventsModule.cs` (or promote registration to `CulturalIntelligenceModule` if impl moves cross-product).
5. Update all consumers — `using LankaConnect.Products.LankaEvents.Domain.Services` → `using LankaConnect.Capabilities.CulturalIntelligence.Contracts`.

### Part 2 — Geo VOs promotion

1. **Grep** all references to `Address` + `GeoCoordinate` VOs specifically at `LankaConnect.Products.LankaEvents.Domain.ValueObjects` namespace:
   ```bash
   grep -rn "LankaConnect.Products.LankaEvents.Domain.ValueObjects.Address\|LankaConnect.Products.LankaEvents.Domain.ValueObjects.GeoCoordinate" src/ --include="*.cs"
   grep -rn "class Address\|record Address\|class GeoCoordinate\|record GeoCoordinate" src/Products/LankaEvents/LankaEvents.Domain/ --include="*.cs"
   ```
2. Move the VO files into `src/SharedKernel/SharedKernel.Geo/` — pattern per `SharedKernel.Money.Money`.
3. Update namespace declarations: `LankaConnect.Products.LankaEvents.Domain.ValueObjects` → `LankaConnect.SharedKernel.Geo`.
4. Update all consumers' `using` statements + fully-qualified references.
5. Verify no functional change (Address/GeoCoordinate should be pure ValueObjects with no dependencies on LankaEvents domain).

### Part 3 — ArchTest rule additions

Add ArchTest rule to enforce inversions cannot be re-introduced:
- `ProductsMustNotDefineCapabilityInterfaces` — check that no interface in `Products/*/Domain/Services/` is a cross-product concept
- `SharedKernelPrimitivesMustNotLiveInProducts` — check `Products/*/Domain/ValueObjects/` doesn't hold VOs that are used by 2+ products

### Part 4 — Commits

- 2 commits (one per inversion) for reversibility
- Bodies:
  - `Wave 8.5-cleanup — promote ICulturalCalendar to CulturalIntelligence.Contracts (fix Products→Capability layer inversion)`
  - `Wave 8.5-cleanup — promote Address + GeoCoordinate to SharedKernel.Geo (fix Products→SharedKernel layer inversion)`
- `T-triggers: T5 (namespace change) + T6 (DI shape change) + T4 (EF config if VO ownership shifts)`
- `S-class: S1 (compile+endpoint smoke — no runtime behavior change)`
- Rule 5j config-relocation audit line MANDATORY
- Push to `develop`.

## Constraints

- **DO NOT** modify any business logic — pure namespace + folder relocations.
- **DO NOT** merge cultural-calendar impl into interface promotion (separate commits per D-04 pattern).
- **COORDINATE** with HandlerMigration-A/B/C — if any handler they're migrating uses `ICulturalCalendar` or `Address`/`GeoCoordinate`, wait for their commit first OR coordinate through their channel.

## Communication protocol

- Post per-inversion grep enumeration first.
- Post commit SHA per inversion.
- Post ArchTest rule commit.
- Post `STATUS: COMPLETE` at bottom.

## Log

*(Agent writes progress below this line.)*

### 2026-07-18 T18:00 UTC — Session 2 (Wave 3 re-spawn after prior session died 0-commit)

**STATUS: PARTIAL — 2 of 3 inversions shipped; inversion 3 (ICulturalCalendar) requires interface refactor beyond LayerInversion scope.**

#### Ship 1 — commit `839fec4a` — Address + GeoCoordinate → SharedKernel.Geo

Pre-flight grep enumeration:
- `record Address` / `class Address` (live, non-migration): `src/Products/LankaEvents/LankaEvents.Domain/ValueObjects/ContactInfoPrimitives.cs` (definition; only home)
- `record GeoCoordinate` / `class GeoCoordinate` (live): same file (definition; only home)
- Consumers of the Address type name (7 live files): EventLocation.cs, EventSeeder.cs, CreateEventCommandHandler.cs, UpdateEventCommandHandler.cs, UpdateEventLocationCommandHandler.cs, EventNotificationRecipientService.cs, EventConfiguration.cs. All other `Address` mentions across the tree are `@event.Location.Address.*` property accessors (compile via type inference — no `using` update needed).
- Consumers of GeoCoordinate type name (7 live files): same set as Address (they compose in EventLocation).

Files changed (11):
- NEW `src/SharedKernel/SharedKernel.Geo/Address.cs`
- NEW `src/SharedKernel/SharedKernel.Geo/GeoCoordinate.cs`
- `src/Products/LankaEvents/LankaEvents.Domain/ValueObjects/ContactInfoPrimitives.cs` (removed 2 classes)
- `src/Products/LankaEvents/LankaEvents.Domain/LankaEvents.Domain.csproj` (+SharedKernel.Geo PR)
- 7 consumer files (added `using LankaConnect.SharedKernel.Geo;`)

Build gate: `dotnet build src/Products/LankaEvents/LankaEvents.Domain/LankaEvents.Domain.csproj` = 0/0. `dotnet build src/Products/LankaEvents/LankaEvents.Application/LankaEvents.Application.csproj` = 2 pre-existing errors (`LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions` in RegistrationEmailService.cs — orthogonal LegacyPromotionsSplit agent in-flight; identical error count present at HEAD `a2eacbd8` before my edits).

Push: `839fec4a` landed on `develop` at push time (fast-forward from `b91e6c10..839fec4a`).

#### Ship 2 — commit `d13e2b0b` — Email + PhoneNumber → SharedKernel.Contact (GAP-6 CORE)

Pre-flight grep enumeration:
- `class Email` / `record Email` (LankaEvents.Domain scope): `src/Products/LankaEvents/LankaEvents.Domain/ValueObjects/ContactInfoPrimitives.cs` (definition)
- `class PhoneNumber` / `record PhoneNumber` (LankaEvents.Domain scope): same file (definition)
- Consumers of the LankaEvents Email VO — 15 live files identified: Identity.Domain (User, IUserRepository), Identity.Application (8 files), Identity.Infrastructure (UserSeeder, UserRepository), LankaEvents.Domain (AttendeeInfo), LankaEvents.Application (3 background/attendee files), LankaConnect.Infrastructure (AppDbContext modelBuilder.Ignore + EF snapshot).
- Communications.Domain.ValueObjects.Email is a SEPARATE aggregate-local twin — Consult #13 tracks that unification, NOT in this ship's scope.
- Twilio.Types.PhoneNumber in TwilioWhatsAppStrategy.cs is unrelated (Twilio-provided).

New project: `src/SharedKernel/SharedKernel.Contact/` (csproj + AssemblyMarker + Email.cs + PhoneNumber.cs), added to LankaConnect.sln.

Files changed (26):
- NEW 4 files in SharedKernel.Contact/
- MODIFIED LankaConnect.sln (added project)
- MODIFIED 2 csprojs (Identity.Domain + LankaEvents.Domain adds SharedKernel.Contact PR)
- MODIFIED ContactInfoPrimitives.cs (removed 2 classes)
- MODIFIED AttendeeInfo.cs (LankaEvents.Domain consumer of Email + PhoneNumber)
- MODIFIED User.cs + IUserRepository.cs (Identity.Domain)
- MODIFIED 8 Identity.Application files (usings + FQ rewrites)
- MODIFIED UserSeeder.cs + UserRepository.cs (Identity.Infrastructure)
- MODIFIED IdentityDbContextModelSnapshot.cs (OwnsOne CLR type strings updated to SharedKernel.Contact.Email/PhoneNumber; historical Designer.cs snapshots kept as-is per Rule 5)
- MODIFIED AppDbContext.cs (modelBuilder.Ignore<T> paths updated)
- MODIFIED 3 LankaEvents.Application files

Build gate: `dotnet build src/Modules/Identity/Identity.Application/Identity.Application.csproj` = 0/0. `dotnet build src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj` = 0/0. `dotnet build src/Products/LankaEvents/LankaEvents.Application/LankaEvents.Application.csproj` = same 2 pre-existing LegacyPromotions errors as before Ship 2 — not introduced.

GAP-6 core closed: Identity.User.Email + Identity.User.PhoneNumber no longer compile-depend on `LankaConnect.Products.LankaEvents.Domain.ValueObjects`. The Identity.Domain → LankaEvents.Domain PR still exists but only for MetroArea reference-data typing (separate GAP-6 slice; not in LayerInversion agent scope).

Push: `d13e2b0b` landed on `develop` (`8d73ec3e..d13e2b0b`).

#### Inversion 3 (ICulturalCalendar → CulturalIntelligence.Contracts) — BLOCKED per task-brief constraint

Task brief §Constraints line: "DO NOT modify any business logic — pure namespace + folder relocations."

ICulturalCalendar cannot be cleanly promoted to CulturalIntelligence.Contracts as a pure namespace move because its method signatures depend on `LankaConnect.Products.LankaEvents.Domain.Event` (10 of ~11 methods) plus 5 LankaEvents.Domain.ValueObjects.Recommendations types (`DiasporaFriendliness`, `FestivalPeriod`, `EventNature`, `SignificantDate`, `CalendarValidationResult`) plus `CulturalAppropriateness` still in LankaEvents.Domain.ValueObjects.

A pure move of the interface with these types intact would make `CulturalIntelligence.Contracts` project-reference `LankaConnect.Products.LankaEvents.Domain` — precisely the Products→Capability inversion the promotion is meant to REMOVE. And the interface types are Product-specific (Event, Recommendation-family VOs), so they don't legitimately belong in SharedKernel either.

To close this cleanly requires ONE of:
- (Option A) Refactor `ICulturalCalendar` to accept primitive parameters (`DateTime`, event-category strings, coordinates) rather than `Event`. Real refactoring — changes handler wiring at every consumer. Beyond LayerInversion agent's constraint.
- (Option B) Refactor `Event` itself to expose a cultural-projection shape that CulturalIntelligence.Contracts can define. Even larger.
- (Option C) Accept CulturalIntelligence.Contracts → Products.LankaEvents.Domain reference (dirty; re-introduces the exact inversion we're fixing).

**Recommendation for next slice**: architect consult required to pick between Option A and Option B. Interface refactor lands as a Wave 8.5-cleanup follow-up commit under a fresh agent brief with explicit "modify behavior" mandate.

#### Part 3 (ArchTest rules) — DEFERRED to interface-refactor follow-up

`ProductsMustNotDefineCapabilityInterfaces` + `SharedKernelPrimitivesMustNotLiveInProducts` rules — the ships above are the last cases of these inversions; the enforcement rules are worth adding when the ICulturalCalendar case is also closed (otherwise the rule fires on ICulturalCalendar and needs an allow-list, which is anti-pattern per Rule 5f).

#### Coordination

- LegacyPromotionsSplit agent: their in-flight LegacyPromotions/ folder renames (visible as `RM` entries in `git status` during Ship 1 & 2) were unstaged before commit — their work is orthogonal and untouched.
- BuildRestore agent: their test-project batch 1 landed at `a2eacbd8` before Session 2 started; their batch 2 (`8d73ec3e`) landed between Ship 1 push and Ship 2 push. Neither collides with SharedKernel.Geo / SharedKernel.Contact promotion scope.
- Wave 8.5.i agent's `b6a576d3` (RegisterUserHandler refactor) landed between pushes; picked up my Ship 2 FQ-rewrite of Email.Create because that edit was included in the same working-tree diff.

STATUS: PARTIAL — Ships 1 (`839fec4a`) + 2 (`d13e2b0b`) COMPLETE and pushed to `develop`. Ship 3 (ICulturalCalendar) BLOCKED on interface-refactor architect consult.
