# MASTER TODO — Phase 6A.156: Sponsorship Packages Foundation (Domain + Application + Organiser CRUD)

**Status**: Approved — awaiting implementation start
**Branch**: `feat/phase-6a-156-sponsorship-packages-foundation` (off `main` @ `7f04ef1d`)
**Created**: 2026-05-30
**Estimated effort**: 3.5 dev days
**Architect-approved across 2 RCA passes**: 2026-05-28 (initial) + 2026-05-29 (delta with user decisions integrated)

---

## Phase Block Reservation (this row in master index)

Reserves **6A.156 → 6A.160** for the Sponsorship Packages feature, mapped to the architect's 5 phases:

| Slot | Architect phase | Scope |
|---|---|---|
| **6A.156** *(THIS DOC)* | A+B+C bundle | Domain + Migration + Application + Organiser CRUD UI |
| 6A.157 | D | Public purchase flow (modify `SponsorSection.tsx`, modal, standalone purchase, webhook event) |
| 6A.158 | E | Ticket bundling (`Event.RegisterSponsorPackageBundle` + event handlers + refund cascade) |
| 6A.159 | F | RSVP bundling (extend `RsvpToEventCommand` with package selections) |
| 6A.160 | G | Sponsor wall tier grouping + preview strip + management tab polish |

---

## Goal

Today, anyone can add a generic sponsorship to an event via the existing flat `Sponsor` flow. There is **no way for the organiser to define curated sponsorship packages** (Gold / Silver / Bronze, "Stage Sponsor", "Beverage Sponsor") that buyers can purchase like add-ons.

This phase introduces the **catalogue** half of the package system:
- New `SponsorshipPackage` aggregate (organiser-defined tiers with price, perks, included tickets, stock cap, image)
- New `events.sponsorship_packages` table
- Additive columns on `events.sponsors` for the future purchase flow (FK + snapshots + included-ticket-count snapshot)
- Additive `EnablePackages` boolean on the existing `SponsorConfiguration` JSONB VO
- Organiser CRUD API + management UI

**Public buyer experience remains 100% unchanged in 6A.156.** Packages exist but are not yet purchasable. Buyer-facing wiring lands in 6A.157.

---

## Locked Architectural Decisions (from RCA delta v2)

1. **Naming**: New aggregate is `SponsorshipPackage` (catalogue). Existing `Sponsor` continues as the purchase row. No rename of `Sponsor`.
2. **Coexistence**: Generic sponsorship = `Sponsor` row with `SponsorshipPackageId IS NULL`. Same table, nullable FK.
3. **Item-type packages**: Money-only v1. Existing free-form item sponsorship stays unchanged.
4. **Perks**: Informational only — Postgres `text[]`, max 10 entries × 200 chars. No fulfilment checklist.
5. **Included tickets per package**: `IncludedTicketCount int` on `SponsorshipPackage` (default 0, range 0–20). Standalone-purchase ticket allocation deferred to 6A.158; 6A.156 only persists the field.
6. **Configuration flag**: `EnablePackages` boolean added to `SponsorConfiguration` JSONB VO (default `false`). When `false`, the organiser CRUD UI is hidden.
7. **Cart**: NO multi-package cart in v1 — deferred to a later phase. One-package-per-checkout matches the existing single-AddOn pattern.
8. **Capacity guard for included tickets**: Reserve-at-purchase, NOT at definition-time. Definition-time UI shows an informational warning if `Σ(QuantityLimit × IncludedTicketCount) > Event.MaxCapacity`.
9. **Capacity-race edge case (6A.158 territory)**: Partial fulfilment — sponsor stays Completed, no tickets created, buyer notified to request refund or accept perks-only. *User-locked decision.*
10. **UI surface**: Reuse the existing `SponsorSection.tsx` (no new public section). Packages render as cards above the existing custom-amount form. *6A.157 work; 6A.156 only ships organiser-side UI.*
11. **Stock management**: Lift `TryReserveStockAsync` / `TryRestoreStockAsync` raw-SQL pattern from `IAddOnDefinitionRepository` byte-for-byte; concurrency-safe.
12. **Price snapshot at purchase**: Mirror `AddOnPurchase.UnitPrice` snapshot pattern. *6A.157 work; 6A.156 only adds the schema columns.*

---

## File-Touch List

### NEW — Backend
- `src/LankaConnect.Domain/Events/SponsorshipPackage.cs` — new aggregate
- `src/LankaConnect.Domain/Events/Repositories/ISponsorshipPackageRepository.cs`
- `src/LankaConnect.Domain/Events/DomainEvents/SponsorshipPackageCreatedEvent.cs`
- `src/LankaConnect.Domain/Events/DomainEvents/SponsorshipPackageUpdatedEvent.cs`
- `src/LankaConnect.Domain/Events/DomainEvents/SponsorshipPackageDeactivatedEvent.cs`
- `src/LankaConnect.Infrastructure/Data/Configurations/SponsorshipPackageEntityConfiguration.cs`
- `src/LankaConnect.Infrastructure/Data/Repositories/SponsorshipPackageRepository.cs`
- `src/LankaConnect.Infrastructure/Data/Migrations/*_Phase6A156_AddSponsorshipPackages.cs` (single migration: new table + sponsor columns + JSONB config field)
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/CreateSponsorshipPackage/CreateSponsorshipPackageCommand.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/CreateSponsorshipPackage/CreateSponsorshipPackageCommandHandler.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/UpdateSponsorshipPackage/UpdateSponsorshipPackageCommand.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/UpdateSponsorshipPackage/UpdateSponsorshipPackageCommandHandler.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/DeleteSponsorshipPackage/DeleteSponsorshipPackageCommand.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/DeleteSponsorshipPackage/DeleteSponsorshipPackageCommandHandler.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/SetSponsorshipPackageImage/SetSponsorshipPackageImageCommand.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/SetSponsorshipPackageImage/SetSponsorshipPackageImageCommandHandler.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/ClearSponsorshipPackageImage/ClearSponsorshipPackageImageCommand.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/ClearSponsorshipPackageImage/ClearSponsorshipPackageImageCommandHandler.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/ReorderSponsorshipPackages/ReorderSponsorshipPackagesCommand.cs`
- `src/LankaConnect.Application/Events/Commands/SponsorshipPackages/ReorderSponsorshipPackages/ReorderSponsorshipPackagesCommandHandler.cs`
- `src/LankaConnect.Application/Events/Queries/SponsorshipPackages/GetEventSponsorshipPackagesQuery.cs`
- `src/LankaConnect.Application/Events/Queries/SponsorshipPackages/GetEventSponsorshipPackagesQueryHandler.cs`
- `src/LankaConnect.Application/Events/Common/SponsorshipPackageDto.cs`
- `src/LankaConnect.API/Controllers/SponsorshipPackagesController.cs`

### NEW — Frontend
- `web/src/presentation/components/features/events/SponsorshipPackagesManagementSection.tsx`
- `web/src/presentation/components/features/events/SponsorshipPackageEditModal.tsx`
- `web/src/presentation/components/features/events/SponsorshipPackageCard.tsx` (shared between organiser admin grid and future public surface in 6A.157)
- `web/src/presentation/hooks/useSponsorshipPackages.ts`
- `web/src/infrastructure/api/repositories/sponsorshipPackages.repository.ts`

### NEW — Tests
- `tests/LankaConnect.Domain.Tests/Events/SponsorshipPackageTests.cs` (≥ 25 tests)
- `tests/LankaConnect.Application.Tests/Events/SponsorshipPackages/*Tests.cs` (≥ 15 handler tests)
- `tests/LankaConnect.Infrastructure.Tests/Data/Repositories/SponsorshipPackageRepositoryTests.cs` (stock-reservation atomicity)
- `web/src/presentation/components/features/events/__tests__/SponsorshipPackageEditModal.test.tsx`

### MODIFY — Backend
- `src/LankaConnect.Domain/Events/Sponsor.cs` — add nullable properties `SponsorshipPackageId`, `RegistrationId`, `PackageNameSnapshot`, `PackageTierSnapshot`, `PackagePriceSnapshot` (Money?), `IncludedTicketCountSnapshot` (int?). No new factory in 6A.156 (deferred to 6A.157). EXISTING `CreateMoneySponsor` / `CreateItemSponsor` / `CompleteAsOrganizerCash` factories unchanged.
- `src/LankaConnect.Domain/Events/ValueObjects/SponsorConfiguration.cs` — add `bool EnablePackages` (default `false`); update `Create` factory + value-equality components.
- `src/LankaConnect.Infrastructure/Data/Configurations/SponsorEntityConfiguration.cs` — add the 6 nullable columns, FK to `events.sponsorship_packages` ON DELETE SET NULL, FK to `events.registrations` ON DELETE SET NULL, partial index on `sponsorship_package_id`, CHECK constraint `(sponsorship_package_id IS NULL) OR (package_name_snapshot IS NOT NULL AND package_price_amount_snapshot IS NOT NULL)`.
- `src/LankaConnect.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — DI for `ISponsorshipPackageRepository`.
- `src/LankaConnect.Application/Events/Commands/UpdateSponsorConfig/UpdateSponsorConfigCommand.cs` — add `EnablePackages` to request.
- `src/LankaConnect.Application/Events/Common/SponsorConfigurationDto.cs` — add `EnablePackages` field.
- `src/LankaConnect.Application/Events/Common/SponsorDto.cs` — add nullable `SponsorshipPackageId`, `PackageNameSnapshot`, `PackageTierSnapshot` fields (read-only DTO surface for 6A.157 consumers).

### MODIFY — Frontend
- `web/src/infrastructure/api/types/events.types.ts` — add `SponsorshipPackage`, `CreateSponsorshipPackageRequest`, `UpdateSponsorshipPackageRequest`, `ReorderSponsorshipPackagesRequest` types; extend `SponsorConfiguration` type with `enablePackages`.
- `web/src/presentation/components/features/events/SponsorConfigForm.tsx` — add `EnablePackages` toggle + "Manage Packages" link (gated visible only when toggle ON).
- `web/src/app/events/[id]/manage/page.tsx` OR appropriate management page — mount `SponsorshipPackagesManagementSection` as a tab/section visible to organisers when `EnablePackages` is ON.

---

## Database Migration Scope

**Single EF Core migration**: `Phase6A156_AddSponsorshipPackages`

```sql
-- 1. New table
CREATE TABLE events.sponsorship_packages (
    id                       uuid PRIMARY KEY,
    event_id                 uuid NOT NULL REFERENCES events.events(id) ON DELETE CASCADE,
    name                     varchar(200) NOT NULL,
    description              varchar(1000) NULL,
    price_amount             numeric(10,2) NOT NULL CHECK (price_amount >= 0),
    price_currency           varchar(3) NOT NULL,
    quantity_limit           int NULL CHECK (quantity_limit IS NULL OR quantity_limit > 0),
    quantity_sold            int NOT NULL DEFAULT 0 CHECK (quantity_sold >= 0),
    is_active                boolean NOT NULL DEFAULT true,
    sort_order               int NOT NULL DEFAULT 0,
    image_url                varchar(1000) NULL,
    image_blob_name          varchar(500) NULL,
    tier                     varchar(100) NULL,
    perks                    text[] NULL,
    included_ticket_count    int NOT NULL DEFAULT 0 CHECK (included_ticket_count BETWEEN 0 AND 20),
    created_at               timestamptz NOT NULL,
    updated_at               timestamptz NOT NULL
);
CREATE INDEX idx_sponsorship_packages_event_id ON events.sponsorship_packages(event_id);
CREATE INDEX idx_sponsorship_packages_event_active ON events.sponsorship_packages(event_id, is_active, sort_order);

-- 2. Sponsor extensions
ALTER TABLE events.sponsors
    ADD COLUMN sponsorship_package_id           uuid NULL REFERENCES events.sponsorship_packages(id) ON DELETE SET NULL,
    ADD COLUMN registration_id                  uuid NULL REFERENCES events.registrations(id) ON DELETE SET NULL,
    ADD COLUMN package_name_snapshot            varchar(200) NULL,
    ADD COLUMN package_tier_snapshot            varchar(100) NULL,
    ADD COLUMN package_price_amount_snapshot    numeric(10,2) NULL,
    ADD COLUMN package_price_currency_snapshot  varchar(3) NULL,
    ADD COLUMN included_ticket_count_snapshot   int NULL,
    ADD CONSTRAINT chk_sponsors_package_snapshot
        CHECK (sponsorship_package_id IS NULL
            OR (package_name_snapshot IS NOT NULL AND package_price_amount_snapshot IS NOT NULL));
CREATE INDEX idx_sponsors_package_id        ON events.sponsors(sponsorship_package_id) WHERE sponsorship_package_id IS NOT NULL;
CREATE INDEX idx_sponsors_registration_id   ON events.sponsors(registration_id)        WHERE registration_id IS NOT NULL;
```

The `SponsorConfiguration.EnablePackages` JSONB field is a pure model-level change — no DDL.

**Migration verification reminders (per project rules)**:
- Confirm `[Migration("...")]` attribute lands in `.Designer.cs` (EF won't apply otherwise)
- Test `Down()` rollback locally before staging deploy
- Existing ~50k `sponsors` rows unaffected — all new columns nullable + CHECK short-circuits on NULL FK

---

## API Endpoints (Organiser-only — `[Authorize]` + `IsCurrentUserOrganizer` guard)

Base route: `api/events/{eventId}/sponsorship-packages`

| Method | Route | Request | Response |
|---|---|---|---|
| GET | `/` | — | `List<SponsorshipPackageDto>` (organiser sees all incl. inactive) |
| GET | `/{packageId}` | — | `SponsorshipPackageDto` |
| POST | `/` | `CreateSponsorshipPackageRequest` | `{ id: Guid }` |
| PUT | `/{packageId}` | `UpdateSponsorshipPackageRequest` | 200 OK |
| DELETE | `/{packageId}` | — | 204 (soft-delete via `IsActive = false`; hard-delete blocked if `QuantitySold > 0`) |
| POST | `/{packageId}/image` | `multipart/form-data` | `{ imageUrl, blobName }` |
| DELETE | `/{packageId}/image` | — | 204 |
| POST | `/reorder` | `{ packageIds: Guid[] }` | 204 |

**No public endpoints yet** — `GET /` is organiser-only in 6A.156. Anonymous `GET` of active packages lands in 6A.157 alongside the public purchase flow.

---

## Exit Criteria (must all be GREEN before next phase)

### Local
- [ ] `dotnet test` — all suites GREEN, ≥ 90% coverage on new SponsorshipPackage code
- [ ] `npm test` — frontend test suite GREEN incl. new component tests
- [ ] Migration `Up()` applies cleanly against fresh DB
- [ ] Migration `Down()` rolls back cleanly
- [ ] `dotnet build` zero warnings on new code

### Staging
- [ ] Backend deploys via `deploy-staging.yml` — green
- [ ] Frontend deploys via `deploy-ui-staging.yml` — green
- [ ] Migration applied on staging DB (verified via psql query `SELECT * FROM events.sponsorship_packages LIMIT 1` + `\d events.sponsors` showing new columns)
- [ ] Curl test: get auth token, then `POST` a Gold package, `GET` to confirm, `PUT` to update, `POST /reorder`, `DELETE` — all 200/204
- [ ] Organiser logs into staging, navigates to event manage page, toggles `EnablePackages`, creates a package, edits it, uploads image, deletes it — all working
- [ ] Existing generic sponsor flow still works on staging (regression check) — anonymous user can still create a money sponsor + item sponsor on a separate event

### Documentation
- [ ] `PHASE_6A_MASTER_INDEX.md` row added for 6A.156 with link to this doc
- [ ] `PROGRESS_TRACKER.md` session entry added
- [ ] `STREAMLINED_ACTION_PLAN.md` action item updated
- [ ] `TASK_SYNCHRONIZATION_STRATEGY.md` phase overview updated

---

## TDD Execution Order (small, testable steps)

### Step 1 — Domain (RED → GREEN → REFACTOR)
1. Write `SponsorshipPackageTests.cs` with failing tests:
   - `Create_WithValidArgs_ReturnsPackage`
   - `Create_WithBlankName_ReturnsFailure`
   - `Create_WithNegativePrice_ReturnsFailure`
   - `Create_WithNegativeIncludedTicketCount_ReturnsFailure`
   - `Create_WithIncludedTicketCount_ExceedingMax_ReturnsFailure` (max 20)
   - `Create_WithMoreThanTenPerks_ReturnsFailure`
   - `Create_WithPerkOverTwoHundredChars_ReturnsFailure`
   - `Create_WithZeroPrice_Succeeds` (recognition-only packages allowed)
   - `UpdateDetails_*` cases
   - `Deactivate_*` cases (soft delete via `IsActive = false`)
   - `SetImage_*` / `ClearImage_*`
   - `HasAvailableStock_*` (unlimited vs limited vs sold-out)
   - `RemainingStock_*`
2. Implement `SponsorshipPackage.cs` to make tests GREEN
3. Refactor for clarity, re-run all tests

### Step 2 — Sponsor extensions (RED → GREEN)
1. Add tests to existing `SponsorTests.cs`:
   - `CreateMoneySponsor_DoesNotSetPackageFields` (regression — generic flow unchanged)
   - Property accessor tests for new nullable fields
2. Add the 6 nullable properties + private setters
3. Verify NO existing sponsor tests break

### Step 3 — SponsorConfiguration extension (RED → GREEN)
1. Add tests:
   - `Create_DefaultsEnablePackagesToFalse`
   - `Create_AcceptsEnablePackages_True`
   - `Equals_DifferentEnablePackages_ReturnsFalse` (value-equality)
2. Add field + update `Create` + `GetEqualityComponents`
3. Verify EXISTING SponsorConfiguration tests pass unchanged

### Step 4 — Infrastructure: EF config + migration
1. Write `SponsorshipPackageEntityConfiguration.cs` mirroring `AddOnDefinitionEntityConfiguration`
2. Update `SponsorEntityConfiguration.cs` to map new columns + FK + index + CHECK
3. Run `dotnet ef migrations add Phase6A156_AddSponsorshipPackages --project src/LankaConnect.Infrastructure`
4. Inspect generated migration — confirm column order, indexes, CHECK constraint
5. Apply locally + verify schema
6. Test `Down()`

### Step 5 — Infrastructure: Repository + raw-SQL stock methods
1. Write `SponsorshipPackageRepositoryTests.cs` (integration test against test DB):
   - `TryReserveStockAsync_UnlimitedPackage_AlwaysSucceeds`
   - `TryReserveStockAsync_WithCapacity_DecrementsCorrectly`
   - `TryReserveStockAsync_AtLimit_ReturnsFalse`
   - `TryRestoreStockAsync_AfterReservation_RestoresCorrectly`
   - `Concurrent reservations don't oversell` (2 parallel tasks each reserving 1 of a 1-cap package — exactly one succeeds)
2. Implement repository lifting from `AddOnDefinitionRepository`

### Step 6 — Application layer (RED → GREEN per handler)
1. Write handler tests with mocked `ISponsorshipPackageRepository`, `IEventRepository`, `ICurrentUserService`
2. Implement command + handler per (Create / Update / Delete / SetImage / ClearImage / Reorder / Query)
3. Each handler wrapped in try/catch with `_logger.LogError(ex, "...{context}", ...)` per project rule

### Step 7 — API controller
1. Implement `SponsorshipPackagesController` mirroring `AddOnsController` patterns
2. Integration test via `WebApplicationFactory` for each endpoint (happy path + auth-denial path)

### Step 8 — Frontend
1. Add types + repository
2. Add hooks (`useSponsorshipPackages`, `useCreateSponsorshipPackage`, etc.)
3. Build `SponsorshipPackageCard.tsx` (shared component — UI-test for rendering)
4. Build `SponsorshipPackageEditModal.tsx` (form + validation + image upload via existing pattern)
5. Build `SponsorshipPackagesManagementSection.tsx` (list + create CTA + reorder drag-handles)
6. Wire `SponsorConfigForm.tsx` to expose `EnablePackages` toggle
7. Wire into event management page as a new section/tab, gated on `EnablePackages`

### Step 9 — Local validation
- Run all backend tests, all frontend tests, both lint passes, typecheck
- Run app locally if feasible, smoke-test the new tab

### Step 10 — Commit + Deploy + Verify
- Commit 1: backend domain + migration + tests
- Commit 2: backend application + API
- Commit 3: frontend organiser UI
- Push to staging via `deploy-staging.yml` and `deploy-ui-staging.yml` chained
- Verify migration applied (psql query)
- Verify API endpoints via curl with auth token
- Verify UI on staging in browser
- Update tracking docs
- Commit 4: tracking-doc updates

---

## Observability & Error-Handling Requirements

Per CLAUDE.md Section 4, every new handler and controller method must:

```csharp
_logger.LogInformation(
    "Creating sponsorship package {PackageName} for event {EventId} by user {UserId}",
    request.Name, eventId, currentUser.Id);

try
{
    // ... work ...
    _logger.LogInformation("Sponsorship package {PackageId} created successfully", packageId);
    return Ok(new { id = packageId });
}
catch (Exception ex)
{
    _logger.LogError(ex,
        "Failed to create sponsorship package for event {EventId}. Name: {Name}",
        eventId, request.Name);
    throw;
}
```

Same pattern for Update / Delete / Image operations / Reorder.

**Domain methods** raise domain events; the existing `IDomainEventDispatcher` pipeline handles publication. No try/catch inside domain factories — they return `Result<T>` per established pattern.

---

## Risks & Mitigations (this phase only)

| Risk | Mitigation |
|---|---|
| Migration breaks existing 50k+ sponsor rows | All new columns NULLABLE; CHECK short-circuits on NULL FK. Verified via test-DB integration test before staging deploy. |
| `SponsorConfiguration` JSONB deserialisation fails for existing rows missing `EnablePackages` | EF JSONB ValueComparer (6A.129) handles missing keys as default value (`false`). Add explicit unit test for "deserialise pre-6A.156 JSONB into new shape". |
| Stock race in `TryReserveStockAsync` lift introduces regression | Lift from `AddOnDefinitionRepository` byte-for-byte; concurrent reservation integration test in Step 5 above. |
| Image upload code duplication | Reuse existing `IImageUploadService` (or whatever the add-on image upload uses) — search before duplicating. |
| `Sponsor.RegistrationId` FK breaks `Registration` deletion flows | FK is `ON DELETE SET NULL` — Registration deletion preserves Sponsor with null FK. No regression to existing Registration tests. |
| UI tab leaks to non-organiser users | Frontend gating + backend `IsCurrentUserOrganizer` guard in every command handler. Integration test for 403 path. |

---

## What is NOT in Scope for 6A.156

- ❌ Public/anonymous endpoint for listing active packages (lands in 6A.157)
- ❌ `POST /{packageId}/purchase` standalone purchase command (lands in 6A.157)
- ❌ Stripe checkout session creation for packages (lands in 6A.157)
- ❌ Webhook handler extension for `PackageSponsorCompletedEvent` (lands in 6A.157/6A.158)
- ❌ `Event.RegisterSponsorPackageBundle` for ticket allocation (lands in 6A.158)
- ❌ Refund cascade handler (lands in 6A.158)
- ❌ RSVP-bundled package purchase (lands in 6A.159)
- ❌ Sponsor wall tier grouping (lands in 6A.160)
- ❌ Multi-package cart (deferred indefinitely; not in scope for any 6A.15x slot)
- ❌ Item-type packages (deferred to v2 indefinitely)
- ❌ Off-platform package recording by organiser (deferred — easy follow-up but not in scope)

---

## Definition of Done — Phase 6A.156

Phase is "shipped and verified" only when:
1. All 4 commits pushed to `feat/phase-6a-156-sponsorship-packages-foundation`
2. Backend staging deploy GREEN (deploy-staging.yml run #N)
3. Frontend staging deploy GREEN (deploy-ui-staging.yml run #M)
4. Migration applied on staging DB (verified via psql)
5. Organiser CRUD smoke-tested via curl on staging (auth token + create + get + update + image upload + delete)
6. Organiser CRUD smoke-tested in browser on staging
7. Existing generic sponsor flow regression-tested on staging (anonymous money + item sponsor on a non-package event)
8. 4 tracking docs updated (PHASE_6A_MASTER_INDEX, PROGRESS_TRACKER, STREAMLINED_ACTION_PLAN, TASK_SYNCHRONIZATION_STRATEGY)
9. PR opened to main (or held for batched merge per project convention)

Status will be reported per CLAUDE.md Section 8 honesty rule: no "shipped" claim without operator UAT — until then, "STAGING-DEPLOYED awaiting UAT".
