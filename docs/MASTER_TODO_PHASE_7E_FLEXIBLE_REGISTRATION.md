# Master TODO — Phase 7E: Flexible Event Registration Modes

**Status**: 🔧 IN PROGRESS — 7E.0 ✅ · 7E.1 ✅ · 7E.2 ✅ · 7E.3a ✅ · 7E.4 ✅ core (chunks 1+2 shipped: registration confirmation HTML + cancellation contract; chunks 3-5 deferred — their existing templates don't render attendee details in any mode) · 7E.5 next
**Architect-approved**: ✅ yes (review iteration 2, 2026-04-25)
**Plan reference**: `C:\Users\Niroshana\.claude\plans\now-show-me-the-shiny-pine.md`
**Master index entry**: [PHASE_6A_MASTER_INDEX.md § Phase 7E](./PHASE_6A_MASTER_INDEX.md)
**Operator**: Niroshana (single)
**Started**: TBD
**Target completion**: TBD (10 vertical slices, ~3–4 weeks)

---

## Goal

Allow event organisers to choose, per event, **how much detail registration captures** — or whether registration happens at all. Eliminates the current friction of forcing per-attendee Name + Age + Gender on every event.

Six modes:
- **A** `DetailedAttendees` — current behaviour (default for back-compat)
- **B1** `HeadCountOnly` — lead name + total
- **B2** `HeadCountByAge` — lead name + adults + children
- **B3** `HeadCountByGender` — lead name + males + females
- **B4** `HeadCountByAgeAndGender` — lead name + 4 leaf counts
- **C** `NoRegistration` — no registration UI; standalone donations/sponsors/add-ons/collections still work

---

## Compatibility rules (architect-locked, see plan §2 for detail)

Mode C iff: free attendance AND no seating.
Mode A required iff any of: per-ticket name, named seating, identity-bound add-on, tier × age matrix pricing.

---

## Working agreement (CLAUDE.md Part A — non-negotiable)

- TDD: red → green → refactor; 90% coverage minimum; zero compile warnings.
- EF Core migrations only via `dotnet ef migrations add` (never hand-author — memory: missing `.Designer.cs` → silent failure).
- Backend changes deploy via `deploy-staging.yml`; frontend via `deploy-ui-staging.yml`.
- Curl-test every backend change against staging API after deploy. Container-log scan for errors / migration failures.
- Update PROGRESS_TRACKER.md, STREAMLINED_ACTION_PLAN.md, TASK_SYNCHRONIZATION_STRATEGY.md after every slice.
- Tick checkboxes in this document as work completes. Be honest — never tick an unverified item.
- All commits include the slice number in the message (e.g. `feat(7E.1): add RegistrationMode enum`).

---

## Pre-flight (before 7E.0)

- [x] Plan written and architect-approved
- [x] Phase 7E reserved in [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md)
- [x] Master TODO doc created (this file)
- [x] PROGRESS_TRACKER.md entry added (Phase 7E header + 7E.0 complete marker)
- [x] STREAMLINED_ACTION_PLAN.md entry added
- [x] TASK_SYNCHRONIZATION_STRATEGY.md entry added
- [ ] Confirm staging API auth works:
  ```bash
  curl -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
    -H 'accept: application/json' -H 'Content-Type: application/json' \
    -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}'
  # Expected: 200 with accessToken
  ```

---

## Slice 7E.0 — Call-site sweep & checklist

**Status**: ✅ **COMPLETE** (2026-04-25)
**Goal**: Read-only audit — every consumer of mode-affecting fields catalogued in `docs/PHASE_7E0_CALLSITE_CHECKLIST.md`. No code changes.
**Deploy**: none.
**Output deliverable**: ✅ [docs/PHASE_7E0_CALLSITE_CHECKLIST.md](./PHASE_7E0_CALLSITE_CHECKLIST.md) — **163 entries** across 12 categories.

### Steps

- [x] Grep backend for `IsFreeEvent` consumers — **11 entries** (§1 of checklist).
- [x] Grep backend for `.Registrations.Sum(` and `.Registrations.Count` aggregations — **9 entries** (§2).
- [x] Grep backend for `Capacity` reads on `Event` — **17 entries** (§3).
- [x] Grep backend for `.Attendees.Count` and per-attendee enumerations — **45 entries** (§4 backend).
- [x] Grep backend for `INNER JOIN`/`Join(...)` patterns from `AddOnPurchase`/`Donation`/`Sponsor`/`Collection` onto `Registration` — **4 `left-join-fix` entries** (§5). `Sponsor`/`Collection` have no FK at all (verified clean).
- [x] Grep frontend (`web/src/`) for: `event.isFreeEvent`, `event.capacity`, `r.attendees`, `attendees.length`, `spotsLeft`, `IsFreeEvent` — **§§ 8–10**.
- [x] Grep for defensive-read sites — **2 entries** (§11).
- [x] Verify `Event.SetRegistrationMode` guard scope — **0 `guard-scope-fix` entries** (§6: standalone shapes are nullable `*Configuration` value-objects, NOT collections — automatically excluded by current aggregate shape).
- [x] Cross-reference with the Risks section below — every numbered risk maps to ≥1 checklist entry (matrix in §Risk-traceability of checklist).
- [x] Write checklist to `docs/PHASE_7E0_CALLSITE_CHECKLIST.md`.
- [x] Update PROGRESS_TRACKER.md, STREAMLINED_ACTION_PLAN.md, TASK_SYNCHRONIZATION_STRATEGY.md with 7E.0 completion.

**Acceptance**: ✅ every risk maps to ≥1 checklist row; ✅ checklist row count = 163 (≥30 sanity threshold).
**Done when**: ✅ doc exists, ✅ all checkboxes ticked, ✅ three tracking docs updated, ✅ committed (next step).

**Tag breakdown**: 149 `needs-mode-aware-update` · 4 `left-join-fix` · 2 `defensive-read` · 0 `guard-scope-fix` · 8 `unchanged`.

---

## Slice 7E.1 — Domain model + migration + EF config

**Status**: pending
**Goal**: Domain model + persistence ready; legacy events round-trip with `registrationMode = "DetailedAttendees"`.
**Deploy**: `deploy-staging.yml`; verify migration applied.

### Files

- New: `src/LankaConnect.Domain/Events/Enums/RegistrationMode.cs`
- New: `src/LankaConnect.Domain/Events/ValueObjects/HeadCountBreakdown.cs`
- New: `src/LankaConnect.Domain/Events/ValueObjects/DemographicBreakdown.cs`
- New: `src/LankaConnect.Domain/Events/ValueObjects/TierCount.cs`
- Modified: `src/LankaConnect.Domain/Events/Event.cs` (add field + `SetRegistrationMode`)
- Modified: `src/LankaConnect.Domain/Events/Registration.cs` (add snapshot field + lead name + head count)
- Modified: `src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs`
- Modified: `src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs`
- New (auto-generated): `src/LankaConnect.Infrastructure/Data/Migrations/<timestamp>_Phase7E1_AddRegistrationMode.cs` + `.Designer.cs`

### TDD steps (RED → GREEN → REFACTOR)

- [ ] **Test (red)**: `Event_DefaultsTo_DetailedAttendees_WhenConstructed`
- [ ] **Test (red)**: `Event_SetRegistrationMode_ThrowsWhenRegistrationsExist`
- [ ] **Test (red)**: `Event_SetRegistrationMode_DoesNotThrowWhenOnlyStandaloneDonationsExist` *(architect: guard scope = `Registrations` only)*
- [ ] **Test (red)**: `HeadCountBreakdown_ForTotalOnly_AcceptsTotalDirectly`
- [ ] **Test (red)**: `HeadCountBreakdown_ForByAge_DerivesTotalFromLeaves` (Total = Adults + Children)
- [ ] **Test (red)**: `HeadCountBreakdown_ForByGender_DerivesTotalFromLeaves`
- [ ] **Test (red)**: `HeadCountBreakdown_ForByAgeAndGender_DerivesTotalFromFourLeaves`
- [ ] **Test (red)**: `HeadCountBreakdown_TierCounts_SumMustEqualTotal_OrThrow`
- [ ] **Test (red)**: `Registration_SnapshotsRegistrationModeFromEvent_OnConstruction`
- [ ] **Test (red)**: `Registration_AttendeesAndHeadCount_AreMutuallyExclusive`
- [ ] **Test (red)**: `TierCount_TierName_IsSnapshot_NotReference` (mutating tier on event later doesn't change registration's TierCount.TierName)
- [ ] **Implement** `RegistrationMode` enum + VOs + Event/Registration changes → make tests green.
- [ ] **Test (red)**: EF round-trip — `Event` with `RegistrationMode.HeadCountByAge` saves and loads correctly.
- [ ] **Test (red)**: EF round-trip — `Registration` with `HeadCountBreakdown.ForByAgeAndGender(...)` saves and loads via JSONB column.
- [ ] **Test (red, architect-required)**: load registration, mutate `headCount.TierCounts[0].Count` in-place, `SaveChanges`, re-load, assert change persisted (catches the 6A.129 reference-snapshot trap).
- [ ] **Test (red)**: legacy registration with `head_count = NULL` deserialises as `null`, NOT empty `HeadCountBreakdown`.
- [ ] **Implement** EF configuration with custom `JsonValueConverter` + deep-copy `ValueComparer` covering `Demographics` record AND `TierCounts` list with element-level structural equality.
- [ ] **Generate migration**:
  ```bash
  dotnet ef migrations add Phase7E1_AddRegistrationMode \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --context AppDbContext
  ```
- [ ] **Verify migration files**: both `<timestamp>_Phase7E1_AddRegistrationMode.cs` AND `<timestamp>_Phase7E1_AddRegistrationMode.Designer.cs` exist (memory: hand-created migrations without Designer are invisible).
- [ ] **Verify migration SQL**: `events.events.registration_mode` `smallint NOT NULL DEFAULT 0`; `events.event_registrations.registration_mode` `smallint NOT NULL DEFAULT 0`; `lead_attendee_name` `text NULL`; `head_count` `jsonb NULL`. **DB-level DEFAULT 0** must be present (memory: 6A.123).
- [ ] Local migration test: `dotnet ef database update` against local DB succeeds.
- [ ] All tests green: `dotnet test` (≥90% coverage on new code).
- [ ] Commit: `feat(7E.1): add RegistrationMode + HeadCountBreakdown VO + migration`

### Deploy & verify

- [ ] Push to `develop`. `deploy-staging.yml` runs.
- [ ] Wait for staging deploy green.
- [ ] DB verify migration applied:
  ```sql
  SELECT migration_id FROM "__EFMigrationsHistory"
  WHERE migration_id LIKE '%Phase7E1%';
  -- Expected: 1 row
  ```
- [ ] DB verify columns added:
  ```sql
  \d events.events            -- registration_mode column present, default 0
  \d events.event_registrations  -- registration_mode/lead_attendee_name/head_count present
  ```
- [ ] DB verify legacy data unchanged:
  ```sql
  SELECT registration_mode, count(*) FROM events.events GROUP BY 1;
  -- Expected: only "0" rows (DetailedAttendees), count = pre-deploy event count
  SELECT registration_mode, count(*) FROM events.event_registrations GROUP BY 1;
  -- Expected: only "0" rows
  ```
- [ ] Container-log scan (no errors):
  ```bash
  az containerapp logs show --name lankaconnect-api-staging \
    --resource-group lankaconnect-staging --tail 200
  # Scan for "Migration", "Error", "Exception"
  ```

### API smoke

- [ ] Get token (see Pre-flight curl above).
- [ ] Fetch a legacy event:
  ```bash
  curl -X GET 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/<legacy-event-id>' \
    -H 'Authorization: Bearer <token>'
  # Expected 200; response.registrationMode == "DetailedAttendees" (string-valued enum).
  ```
- [ ] Fetch attendees of a legacy registration:
  ```bash
  curl -X GET '.../api/events/<event-id>/attendees' -H 'Authorization: Bearer <token>'
  # Expected: per-attendee rows exactly as before; no headCount/leadAttendeeName fields populated.
  ```

**Acceptance**:
- Migration row in `__EFMigrationsHistory`.
- All legacy events default to `DetailedAttendees`.
- API round-trips the new field as a string.
- Round-trip mutation test passes (no JSONB silent-no-op regression).

**Done when**: all checkboxes ticked + three tracking docs updated.

---

## Slice 7E.2 — Event create/update API + validator + EmailTemplateContract

**Status**: pending
**Goal**: Create/update events with mode; validator enforces 14-row compatibility table; email contract constants land (gates 7E.4).
**Deploy**: `deploy-staging.yml`.

### Files

- Modified: `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommand.cs` + handler + validator
- Modified: `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommand.cs` + handler + validator
- New: `src/LankaConnect.Application/Events/Queries/GetAllowedRegistrationModes/GetAllowedRegistrationModesQuery.cs` + handler
- Modified: `src/LankaConnect.Shared/Email/Contracts/EmailTemplateContract.cs` (add Phase 7E constants)
- Modified: `src/LankaConnect.API/Controllers/EventsController.cs` (expose new query)

### TDD steps

- [ ] **Test (red, `[Theory]`-driven)** — validator data table mirrors all 14 rows of plan §2 compatibility table. Each row: `(IsFreeEvent, HasSeating, SeatingType, HasTiers, TierShape, AddOnShape, RegistrationMode, ExpectedResult)`. Architect-required pattern.
- [ ] **Test (red)**: `CreateEventCommand_DefaultsRegistrationModeToDetailedAttendees_WhenAbsent`
- [ ] **Test (red)**: `CreateEventCommand_RejectsModeC_WhenPaidAttendance` (400 with clear message)
- [ ] **Test (red)**: `CreateEventCommand_RejectsModeC_WhenSeating`
- [ ] **Test (red)**: `CreateEventCommand_AcceptsB4_WhenDualPricing` (architect refinement: B4 carries adult/child counts via leaves)
- [ ] **Test (red)**: `UpdateEventCommand_RejectsModeChange_WhenRegistrationsExist`
- [ ] **Test (red)**: `UpdateEventCommand_AcceptsModeChange_WhenOnlyStandaloneDonationsExist` (architect: standalone contributions are mode-agnostic)
- [ ] **Test (red)**: `GetAllowedRegistrationModesQuery_ReturnsAll6_ForFreeEventNoAddOns`
- [ ] **Test (red)**: `GetAllowedRegistrationModesQuery_ReturnsAOnly_ForNamedSeating`
- [ ] **Test (red)**: `GetAllowedRegistrationModesQuery_ReturnsAB2B4_ForDualPricing`
- [ ] **Implement** validator + query + handler changes → green.
- [ ] **Test (red)**: `EmailTemplateContract` includes constants: `HasDetailedAttendees`, `HasHeadCount`, `HasHeadCountBreakdown`, `HasTierBreakdown`, `HeadCountTotal`, `HeadCountBreakdownLine`, `TierBreakdownLine`.
- [ ] **Implement** contract additions; verify `EmailTemplateValidationService` still passes at startup (no template references the new params yet — validation neutral).
- [ ] All tests green: `dotnet test`.
- [ ] Commit: `feat(7E.2): event create/update API + validator + email contract constants`.

### Deploy & verify

- [ ] Push, wait for staging green.
- [ ] Container-log: confirm `EmailTemplateValidationService` startup pass.

### API matrix tests (curl)

For each row of the 14-row compatibility table, POST a draft event with that shape × each of the 6 modes. **Run as a script** in `scripts/test_phase_7e_compatibility.sh` (commit alongside).

Sample:
```bash
# Mode A on free event → 200
curl -X POST '.../api/events' -H 'Authorization: Bearer <token>' \
  -H 'Content-Type: application/json' \
  -d '{"title":"7E.2 test A free","isFreeEvent":true,"capacity":50,"registrationMode":"DetailedAttendees", ... }'
# Expected: 201 with registrationMode echoed.

# Mode C on paid event → 400
curl -X POST '.../api/events' ... \
  -d '{"title":"7E.2 test C paid","isFreeEvent":false,"ticketPrice":{"amount":10},"registrationMode":"NoRegistration", ...}'
# Expected: 400 with message containing "NoRegistration requires free attendance".

# B4 on dual-pricing event → 200
curl -X POST '.../api/events' ... \
  -d '{"title":"7E.2 test B4 dual","pricing":{"adult":15,"child":7},"registrationMode":"HeadCountByAgeAndGender", ...}'
# Expected: 201.

# B1 on dual-pricing event → 400
curl -X POST '.../api/events' ... \
  -d '{... "pricing":{"adult":15,"child":7},"registrationMode":"HeadCountOnly"}'
# Expected: 400 "HeadCountOnly cannot be used with dual pricing — choose A, B2, or B4".
```

- [ ] Run all 14 × 6 = 84 combinations; expected 200/400 per plan §2 table.
- [ ] Test `GetAllowedRegistrationModesQuery`:
  ```bash
  curl -X GET '.../api/events/<event-id>/allowed-registration-modes' -H 'Authorization: Bearer <token>'
  # Expected: array of allowed mode strings matching event's pricing shape.
  ```

**Acceptance**: 84/84 combinations return expected status; query returns correct mode set; no startup errors.
**Done when**: checkboxes ticked + tracking docs updated.

---

## Slice 7E.3 — RSVP API for B modes; Mode C rejection

**Status**: pending
**Goal**: RSVP works in B modes; Mode C rejects RSVP cleanly; standalone contributions unchanged.
**Deploy**: `deploy-staging.yml` after each sub-slice.

### Sub-slices

#### 7E.3a — Free B-mode RSVP (no tiers)

- [ ] **Test (red)**: `RsvpToEvent_ModeB1Free_CreatesRegistrationWithLeadAndTotal`
- [ ] **Test (red)**: `RsvpToEvent_ModeB2Free_StoresAdultsAndChildrenWithDerivedTotal`
- [ ] **Test (red)**: `RsvpToEvent_ModeB3Free_StoresMalesAndFemales`
- [ ] **Test (red)**: `RsvpToEvent_ModeB4Free_StoresFourLeafCounts`
- [ ] **Test (red)**: `RsvpToEvent_ModeC_Returns400_WithClearMessage`
- [ ] **Test (red)**: `RegisterAnonymousAttendee_ModeB_Works` (anonymous + head count)
- [ ] **Test (red)**: `UpdateRsvp_ModeB_AcceptsHeadCountDelta`
- [ ] **Implement** changes to `RsvpToEventCommand`, `RegisterAnonymousAttendeeCommand`, `UpdateRsvpCommand`. Reuse existing flow paths; no fork.
- [ ] **Test (red)**: capacity aggregator updated — `Event.SpotsLeft = capacity - Σ(r.HeadCount?.Total ?? r.Attendees.Count)`. Test pre-7E (mode A) registrations + new mode-B registrations mixed.
- [ ] All tests green; commit `feat(7E.3a): RSVP API for free B modes`.
- [ ] Deploy + curl smoke:
  ```bash
  # Mode B1 free RSVP
  curl -X POST '.../api/events/<modeB1-event-id>/rsvp' -H 'Authorization: Bearer <token>' \
    -d '{"leadAttendeeName":"Niroshana","headCount":{"total":3}}'
  # Expected: 201, response shows registrationMode "HeadCountOnly", headCount.total=3.

  # Mode C RSVP → 400
  curl -X POST '.../api/events/<modeC-event-id>/rsvp' ...
  # Expected: 400 "Registration not required for this event".
  ```

#### 7E.3b — Paid B-mode RSVP (single + dual price, Stripe amount-calc)

**Architect-gated**: explicit Stripe amount-calc tests required before merge.

- [ ] **Test (red)**: `RsvpToEvent_ModeB1Paid_SinglePrice_TotalPriceEquals_TotalTimesPrice`
- [ ] **Test (red)**: `RsvpToEvent_ModeB2Paid_DualPrice_TotalPriceEquals_AdultsTimesAdultPrice_PlusChildrenTimesChildPrice`
- [ ] **Test (red)**: `RsvpToEvent_ModeB4Paid_DualPrice_DerivesAdultsAndChildren_FromFourLeaves_AndPricesCorrectly`
- [ ] **Test (red)**: Stripe Checkout session creation for paid mode-B uses correct line-item amount.
- [ ] **Implement** — reuse existing pricing service from mode A. Do not fork pricing math.
- [ ] All tests green; commit `feat(7E.3b): paid B-mode RSVP + Stripe amount-calc`.
- [ ] Deploy + Stripe test-mode end-to-end:
  ```bash
  # Mode B2 paid (dual-price 15/7) RSVP — 2 adults + 1 child = $37
  curl -X POST '.../api/events/<modeB2-paid-event-id>/rsvp' ...
    -d '{"leadAttendeeName":"Niroshana","headCount":{"adults":2,"children":1}}'
  # Expected: 201 + Stripe redirect URL with amount=3700 cents.
  ```
- [ ] Complete Stripe test-mode payment via test card `4242 4242 4242 4242`. Verify webhook → `registration.paymentStatus = Completed`, confirmation email sent.

#### 7E.3c — Paid B-mode RSVP with TierCounts axis

- [ ] **Test (red)**: `HeadCountBreakdown_TierCounts_SumEqualsTotal_OrThrow` (already in 7E.1; re-verify here)
- [ ] **Test (red)**: `RsvpToEvent_ModeBwithTiers_PricesCorrectly` (e.g. "VIP × 2 + General × 3" with VIP=$50 General=$30 → $190).
- [ ] **Test (red)**: `RsvpToEvent_ModeBwithTiers_TierNameIsSnapshotted` — rename tier on event after RSVP, registration retains old name.
- [ ] **Test (red)**: amount-calc parity — same basket via mode A vs mode B + tier counts produces identical `TotalPrice`.
- [ ] **Implement** — extend RSVP command to accept `tierCounts: [{tierId, count}]`; reuse mode-A tier-pricing service.
- [ ] All tests green; commit `feat(7E.3c): tier counts axis on B-mode RSVP`.
- [ ] Deploy + curl test:
  ```bash
  curl -X POST '.../api/events/<tiered-modeB-event-id>/rsvp' ...
    -d '{"leadAttendeeName":"Niroshana","headCount":{"total":5,"tierCounts":[{"tierId":"<vip>","count":2},{"tierId":"<gen>","count":3}]}}'
  # Expected: 201 + Stripe redirect amount = (2 × VIP) + (3 × General) cents.
  ```

**Acceptance for 7E.3 overall**: free B modes work; Mode C rejects with 400; paid B modes (single, dual, tiered) compute correct Stripe amounts; mode-A regression baseline unchanged.
**Done when**: 3 sub-slices complete + tracking docs updated.

---

## Slice 7E.4 — Email templates

**Status**: pending (gated on 7E.2 — `EmailTemplateValidationService` must be green)
**Goal**: Affected templates (~9) updated with mode-aware Handlebars blocks; tone-A vs tone-B copy; Mode C non-firing.
**Deploy**: `deploy-staging.yml`.

### Files

- New template versions in `Templates/Email/`:
  - `event-registration-confirmation.html` (v2)
  - `event-anonymous-registration-confirmation.html` (v2)
  - `event-cancellation.html` (v2)
  - `event-organizer-cancelled-the-event.html` (v2)
  - `event-reminder.html` (v2)
  - `event-registration-modified.html` (v2)
  - `event-add-attendees-confirmation.html` (v2)
  - `organizer-new-registration-notification.html` (v2)
  - `event-waitlist-promoted.html` (v2 — if waitlist applies)
- Modified: handlers populating new email params (in `src/LankaConnect.Application/Events/EventHandlers/`).
- Modified: `src/LankaConnect.Infrastructure/Data/Migrations/<timestamp>_Phase7E4_SeedV2EmailTemplates.cs` (auto-generated, seeds new template rows via standard seeder).

### Steps

- [ ] **Read each affected template** in `Templates/Email/` first (memory: never inline-mutate).
- [ ] **Author v2 template** for each, wrapping mode-aware section with anchor comments `<!-- attendee-block-7e --> ... <!-- /attendee-block-7e -->`. Include both subject-line and body conditional per plan §6.1 example.
- [ ] **Test (red)**: each handler populates the new params (`HasDetailedAttendees`, `HasHeadCount`, `HasHeadCountBreakdown`, `HasTierBreakdown`, `HeadCountTotal`, `HeadCountBreakdownLine`, `TierBreakdownLine`) for both true AND false (never omitted).
- [ ] **Implement** handler changes → green.
- [ ] **Generate seeding migration**:
  ```bash
  dotnet ef migrations add Phase7E4_SeedV2EmailTemplates \
    --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API \
    --context AppDbContext
  ```
  Hand-edit the migration to insert v2 templates via the standard `SeedTemplate(...)` helper or equivalent. **Never `REGEXP_REPLACE`** (memory 7C.2 / 6A.117 / 6A.122).
- [ ] **Test (red)**: render each template via `IEmailTemplateRenderer` with mode-A params → assert subject/body match pre-7E baseline (regression).
- [ ] **Test (red)**: render with mode-B1 params → assert tone-B subject ("You're confirmed for ...") and body shows lead + total only (no breakdown).
- [ ] **Test (red)**: render with mode-B2/B3/B4 → assert `HeadCountBreakdownLine` rendered.
- [ ] **Test (red)**: render with tiers → assert `TierBreakdownLine` rendered.
- [ ] All tests green; commit `feat(7E.4): mode-aware email templates v2 + seeding migration`.

### Deploy & verify

- [ ] Push; staging deploy green.
- [ ] DB verify: `SELECT name, version FROM email_templates WHERE name LIKE 'event-%' ORDER BY name, version;` — v2 rows present.
- [ ] Container-log: confirm `EmailTemplateValidationService` passes at startup with new contract constants populated.

### Email rendering verification (per plan §11.6b)

- [ ] **Mode A** RSVP on staging (using `niroshhh@gmail.com`); fetch ACS sent log; assert subject + body unchanged from pre-7E baseline.
- [ ] **Mode B1** RSVP; fetch sent log; subject = "You're confirmed for ..."; body shows "Lead: <name> · Total: 3", no breakdown line.
- [ ] **Mode B2** RSVP; assert breakdown line "2 adults · 1 child".
- [ ] **Mode B + tiers** RSVP; assert tier line "VIP × 2, General × 3".
- [ ] **Tier rename test**: register, rename tier, trigger reminder cron, fetch reminder email — old tier name renders.

### Mode C non-firing verification (per plan §11.6c)

- [ ] Create Mode C event → assert no email queued.
- [ ] Cancel Mode C event as organiser → assert no `event-organizer-cancelled-the-event` broadcast (no recipients); audit log records cancellation.
- [ ] Trigger reminder cron for Mode C event → 0 emails sent.
- [ ] POST standalone donation against Mode C event → donation-receipt email DOES fire.

**Acceptance**: 9 v2 templates seeded; mode A unchanged; mode B emails carry tone-B copy + correct breakdown; Mode C silent except for standalone-contribution receipts.
**Done when**: tracking docs updated.

---

## Slice 7E.5 — Frontend mode picker (event create/edit)

**Status**: pending
**Goal**: Organiser sees a Mode picker; options reactive to pricing/seating/tier toggles via `GetAllowedRegistrationModesQuery`.
**Deploy**: `deploy-ui-staging.yml`.

### Files

- Modified: event create form (likely `web/src/app/events/create/page.tsx` or component).
- Modified: event edit form.
- Modified: `web/src/types/Event.ts` (or equivalent) — add `RegistrationMode` enum (string-valued per memory 6A.124).
- New: `web/src/presentation/hooks/useAllowedRegistrationModes.ts` (React Query hook).
- New: `web/src/presentation/components/features/events/RegistrationModePicker.tsx`.

### Steps

- [ ] **Locate** existing event create/edit form via grep.
- [ ] **Read** the form component fully before modifying (UI fragility: CLAUDE.md §3).
- [ ] Add TS enum `RegistrationMode` with string values matching backend.
- [ ] Implement `useAllowedRegistrationModes(draftShape)` React Query hook calling new endpoint.
- [ ] Implement `RegistrationModePicker` component — radio group with descriptions, disabled options driven by allowed-modes query; current selection auto-cleared if no longer allowed (architect hot-spot #5).
- [ ] **Test (red, component test)**: picker re-queries on `isPaid` toggle change.
- [ ] **Test (red)**: picker auto-clears Mode C when `hasSeating` flips on.
- [ ] Wire into create/edit form; default `DetailedAttendees`.
- [ ] Lint + type-check (`pnpm lint && pnpm tsc --noEmit`).
- [ ] Commit: `feat(7E.5): registration mode picker on event create/edit`.

### Deploy & browser verify

- [ ] Push; `deploy-ui-staging.yml` green.
- [ ] Open event create form on staging.
- [ ] Verify all 6 modes shown when free + no constraints.
- [ ] Toggle "paid" → Mode C disabled with tooltip "NoRegistration requires free attendance".
- [ ] Toggle "seating" + "named seats" → only Mode A enabled.
- [ ] Toggle "dual pricing" → A, B2, B4 enabled.

**Acceptance**: picker reactive; disabled-option tooltips clear; default selection back-compat.
**Done when**: tracking docs updated.

---

## Slice 7E.6 — Frontend RSVP form (A/B1–B4/C conditional rendering)

**Status**: pending
**Goal**: RSVP form renders the right fields per mode; tier-count selector when event has tiers; sum mirror invariant.
**Deploy**: `deploy-ui-staging.yml`.

### Files

- Modified: `web/src/presentation/components/features/events/EventRegistrationForm.tsx`
- Modified: `web/src/presentation/components/features/events/AddAttendeesModal.tsx`
- Modified: `web/src/app/events/[id]/page.tsx` (CTA label switching: Register/RSVP/none per plan §7.1)

### Steps

- [ ] Read all three files fully before edits (UI fragility).
- [ ] Add per-mode rendering branches in `EventRegistrationForm`:
  - A: existing per-attendee fields (untouched). **Verify visual diff is zero** for Mode A.
  - B1: lead name + total spinner.
  - B2: lead name + adults spinner + children spinner; live Total = sum mirror.
  - B3: lead name + males + females; live Total = sum mirror.
  - B4: lead name + 4 leaf spinners; live Total = sum.
  - C: not rendered.
- [ ] If event has tiers (any mode B): additional per-tier counter section; sum invariant check (`Σ tier counts = Total`).
- [ ] CTA label on event detail page: switch by mode (`Register` / `RSVP` / hidden).
- [ ] `AddAttendeesModal` under B: simplified "Add N more" form (reuses pricing pipeline).
- [ ] Defensive read everywhere: `event.registrationMode ?? RegistrationMode.DetailedAttendees`.
- [ ] **Test (red, component)**: each B-mode submits payload matching API expectation.
- [ ] **Test (red, component)**: tier-count sum mismatch is blocked at submit time.
- [ ] **Test (red, component)**: Mode A form renders identically to pre-7E (snapshot test or pixel diff).
- [ ] React Query: invalidate `events` cache on first load post-deploy (architect §6).
- [ ] Lint + type-check.
- [ ] Commit: `feat(7E.6): mode-aware RSVP form rendering + RSVP CTA label`.

### Deploy & browser verify

- [ ] Push; UI deploy green.
- [ ] Mode A event: visual identical to pre-7E.
- [ ] Mode B1 event: form shows lead + total; submit → 201 + confirmation email tone-B.
- [ ] Mode B2 with dual pricing: form shows adults + children spinners, Total mirrors, submit → Stripe redirect.
- [ ] Mode B + tiers event: tier-count selector visible; sum invariant enforced.
- [ ] Mode C event: no Register/RSVP button; donation/sponsor actions visible if applicable.

**Acceptance**: each mode submits correct payload; Mode A visually identical to pre-7E.
**Done when**: tracking docs updated.

---

## Slice 7E.7 — Frontend AttendeeManagementTab row-template branching

**Status**: pending
**Goal**: AttendeeManagementTab structurally unchanged; only the row template branches by mode. "Mark Attendees" button visibility per plan §7.1.
**Deploy**: `deploy-ui-staging.yml`.

### Files

- Modified: AttendeeManagementTab component (locate via grep — likely `web/src/presentation/components/features/events/AttendeeManagementTab.tsx` or similar).
- Modified: event detail page conditional showing/hiding "Mark Attendees" button.

### Steps

- [ ] Read tab component fully; understand current data flow + Mark Attendees integration.
- [ ] **Add row template branch** by `event.registrationMode`:
  - Mode A: existing per-attendee rows + checkbox — **no visual change**.
  - Mode B (any): per-registration row template showing `Lead name · "+N attendees" · breakdown if applicable · tier breakdown if tiers`.
  - Mode C: empty-state copy `"This event doesn't require registration."` — tab still visible.
- [ ] **Hide "Mark Attendees" button** for Mode B and Mode C (event detail page).
- [ ] **Test (red, component)**: Mode A row template renders identically (snapshot).
- [ ] **Test (red, component)**: Mode B row shows lead name + count + breakdown.
- [ ] **Test (red, component)**: Mode C empty state visible; "Mark Attendees" hidden.
- [ ] **Test (red, component)**: Mode A "Mark Attendees" check-off flow unchanged (regression).
- [ ] Lint + type-check.
- [ ] Commit: `feat(7E.7): AttendeeManagementTab mode-aware rows`.

### Deploy & browser verify

- [ ] Mode A event with registrations: tab visually identical to pre-7E; check-off works.
- [ ] Mode B event with registrations: rows show lead + count; no Mark Attendees button.
- [ ] Mode C event: empty-state copy visible.

**Acceptance**: zero regression on Mode A; B/C render correctly; no new organiser CTA.
**Done when**: tracking docs updated.

---

## Slice 7E.8 — Organiser dashboard / analytics / CSV export

**Status**: pending
**Goal**: Organiser-facing reports respect the mode; CSV export includes tier breakdown columns when applicable; spots-left widget mode-aware. **Architect hot-spot #6**: any `INNER JOIN` from `AddOnPurchase`/`Donation`/`Sponsor`/`Collection` to `Registration` flagged in 7E.0 must be converted to `LEFT JOIN`.
**Deploy**: `deploy-staging.yml` + `deploy-ui-staging.yml`.

### Files

- Backend: dashboard query handlers (located via 7E.0 sweep).
- Backend: CSV export endpoint(s).
- Frontend: organiser dashboard widgets, attendee CSV export trigger.

### Steps

- [ ] Reference 7E.0 checklist — fix all `INNER JOIN Registration` → `LEFT JOIN` for the four standalone-payment entities.
- [ ] Update spots-left widget formula: `capacity - Σ(r.HeadCount?.Total ?? r.Attendees.Count)`.
- [ ] Update CSV export columns:
  - Mode A row: existing columns (no change).
  - Mode B row: `LeadAttendeeName, Total, Adults, Children, Males, Females, AdultMales, AdultFemales, ChildMales, ChildFemales, TierBreakdown` (only filled per mode).
  - Mode C: empty CSV with header + footer note "No registrations — Mode: NoRegistration".
- [ ] **Test (red)**: CSV export for mode-B-with-tiers event has correct columns + values.
- [ ] **Test (red)**: spots-left widget across mixed mode-A and mode-B registrations on the same event (impossible by design — but verify aggregation).
- [ ] **Test (red, regression)**: standalone donation/addon/sponsor/collection counts correct on a Mode C event (LEFT JOIN fix).
- [ ] All tests green; commit `feat(7E.8): organiser dashboard + CSV export mode-aware`.

### Deploy & verify

- [ ] Backend deploy green; UI deploy green.
- [ ] Curl CSV export for a Mode B with-tiers event:
  ```bash
  curl -X GET '.../api/events/<event-id>/attendees.csv' -H 'Authorization: Bearer <token>'
  # Expected: header row includes tier breakdown columns; rows have correct values.
  ```
- [ ] Browser: organiser dashboard for Mode C event with standalone donations — donation count correct (regression for INNER→LEFT JOIN fix).

**Acceptance**: CSV correct per mode; standalone-contribution counts correct under Mode C.
**Done when**: tracking docs updated.

---

## Slice 7E.9 — End-to-end staging validation + regression sweep

**Status**: pending
**Goal**: Verify everything in 7E.0 checklist + plan §11 verification sections.
**Deploy**: none (verification only).

### Steps

#### Per-mode lifecycle (free events)

- [ ] Mode A free: create → RSVP → email → cancel → email. Match pre-7E baseline.
- [ ] Mode B1 free: create → RSVP → email tone-B → cancel → email. Confirm.
- [ ] Mode B2 free: ditto with adults/children breakdown in email.
- [ ] Mode B3 free: ditto with males/females breakdown.
- [ ] Mode B4 free: ditto with 4-leaf breakdown.
- [ ] Mode C free: create → no RSVP path → no emails. Donate → donation receipt fires.

#### Per-mode lifecycle (paid where allowed)

- [ ] Mode A paid (single price, dual price, tiers): regression baseline.
- [ ] Mode B1 paid (single price): RSVP → Stripe → webhook → confirmation tone-B.
- [ ] Mode B2 paid (dual price): RSVP → Stripe with correct amount → webhook.
- [ ] Mode B4 paid (dual price): ditto, derived adults/children pricing.
- [ ] Mode B + tiers paid: tier-count basket pricing parity vs Mode A equivalent.

#### Edge cases

- [ ] Tier-rename-after-RSVP (per §11.6 plan): registration retains snapshot name in re-rendered emails.
- [ ] Mode-change attempt with one registration → 400.
- [ ] Mode-change attempt with zero registrations + standalone donation present → 200 (not blocked by donation).
- [ ] Pre-7E `Registration` row: `head_count` JSONB is NULL, deserialises as null.
- [ ] Frontend with stale React Query cache: invalidation on first post-deploy load surfaces new field.

#### Capacity aggregation

- [ ] Mode A event with 5 registrations × 3 attendees = 15 → spots left correct.
- [ ] Mode B event with 5 registrations × `Total=3` each = 15 → spots left correct.
- [ ] Mode C event: capacity displayed (informational), no enforcement.

#### Standalone-contribution path under Mode C (per §11.4)

- [ ] Donation, sponsor, addon, collection on a Mode C event → all 201 + Stripe + webhook → no Registration created.
- [ ] Refund a donation on Mode C event → refund flow OK without Registration.

#### Container logs sweep

- [ ] No `Exception`, `Error`, `Migration` errors in last 1000 lines of staging logs across all of 7E.

#### Tracking docs final pass

- [ ] All 9 slices' progress entries present in PROGRESS_TRACKER, STREAMLINED_ACTION_PLAN, TASK_SYNCHRONIZATION_STRATEGY.
- [ ] `PHASE_6A_MASTER_INDEX.md` updated: all 7E rows = ✅ Complete.
- [ ] Phase summary doc created: `docs/PHASE_7E_FLEXIBLE_REGISTRATION_SUMMARY.md`.

**Acceptance**: every checklist row above ticked. Plan §11 fully verified.
**Done when**: phase summary doc committed.

---

## Risk register & guards (architect-flagged)

Each risk traces to ≥1 mitigation in the slices.

1. **`Event.SpotsLeft` aggregation drift** — 7E.0 sweep + 7E.1 unit tests + 7E.9 capacity test.
2. **Email template parameter contract drift** — 7E.2 lands constants, `EmailTemplateValidationService` gates 7E.4.
3. **JSONB null vs missing on legacy registrations** — 7E.1 explicit pre-7E load test.
4. **`AddAttendeesModal` / `UpdateRsvpCommand` delta pricing fork** — 7E.3b/c reuse mode-A pipeline; tests assert basket parity.
5. **Stripe `TotalPrice` for paid HeadCountByAge / TierCounts** — 7E.3b/c gated sub-slices with explicit amount-calc tests.
6. **AddOnPurchase reports under Mode C (`INNER JOIN` drops standalone)** — 7E.0 grep + 7E.8 fix to `LEFT JOIN`.
7. **Tier rename/delete vs snapshot** — `TierName` snapshot on `TierCount`; 7E.9 tier-rename test.
8. **`SetRegistrationMode` guard scope** — must NOT lock based on standalone contributions; 7E.1 + 7E.2 tests.
9. **Validator combinatorics** — `[Theory]`-driven across the 14-row table in 7E.2.
10. **Frontend mode picker reactivity** — 7E.5 re-query on every form-state change.

---

## Out of scope (Phase 7F)

- `(tier, age)` matrix axis on `HeadCountBreakdown` (unlocks tier × age matrix pricing in B modes).
- Optional `HeadCountByTier`-only mode (if tier-without-demographic proves common).
- A↔B mode change with attendee backfill (today: forbidden once registrations exist).
- Mode B organiser-side attendance tracking (`actualHeadCountAttended` field + organiser CTA).
- Mode C add-on identity-bound shapes (engraved name plate per attendee on Mode C events — would require an "anonymous purchase" identity field that is not a Registration FK).

---

## Status reporting cadence

After **each slice**:
1. Tick all checkboxes in this file (be honest — only tick what's verified).
2. Update [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) with a session entry.
3. Update [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md) with action-item status.
4. Update [TASK_SYNCHRONIZATION_STRATEGY.md](./TASK_SYNCHRONIZATION_STRATEGY.md) with phase overview.
5. Update [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) row status.
6. Commit with slice-tagged message.

After **deployment**:
1. `gh run watch` until green.
2. Container-log scan for the 5 minutes following deploy.
3. DB verification queries (migration row in `__EFMigrationsHistory`, schema columns present).
4. API curl smoke per slice.

After **phase complete (7E.9)**:
1. Phase summary doc.
2. Master index marks all 7E rows ✅.
3. Memory entries updated if any new pattern/lesson emerged.

---

## Last updated

2026-04-25 — Phase 7E created, architect-approved (iteration 2). Slice 7E.0 ready to start.
