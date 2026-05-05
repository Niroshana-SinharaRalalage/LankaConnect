# Master TODO — Phase 7E: Flexible Event Registration Modes

**Status**: ✅ **PHASE 7E CORE SHIPPED** (2026-04-27) — 7E.0 ✅ · 7E.1 ✅ · 7E.2 ✅ · 7E.3a ✅ · 7E.4 ✅ core · 7E.5 ✅ · 7E.6 ✅ · 7E.7+8a ✅ · 7E.8 ✅ core · 7E.9 ✅ regression-verified on staging (architect hot-spots clear; B3-by-gender CSV correct; Mode C reject + standalone donation work; legacy event back-compat preserved). Deferred to Phase 7F: paid B-mode (Stripe), tier × age matrix, A↔B with backfill, Mode B check-in, CSV tier-breakdown column, remaining email-template chunks (cancel/reminder/attendees-added).
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

**🚧 Gate-removal checklist (added 2026-04-29 by paid-B-mode-gate fix)**: when this slice
ships, the implementer MUST also remove the temporary gate that currently rejects paid +
B-mode at the validator. The gate exists in `RegistrationModeCompatibility.cs` —
`grep -n PHASE_7E_3B src/LankaConnect.Domain/Events/Services/RegistrationModeCompatibility.cs`
finds the inline breadcrumb. Removal steps:
1. Delete the `if (!ctx.IsFreeAttendance) return Result.Failure(RegistrationModeErrorCodes.PaidHeadCountDeferred);`
   block inside `CheckCommonHeadCountConstraints`.
2. Update `Phase7E2RegistrationModeCompatibilityTests`: rows 5/7/8/9 revert from
   "A only (paid B-mode gated until 7E.3b)" → the original target-state expectations
   ("Paid single price → A + all B", "Paid dual pricing → A, B2, or B4", etc.). The
   theory rows in `Check_Fails_WithPaidHeadCountDeferred_ForPaidEvents` should also flip
   to assert success or be removed.
3. After deploy, `EventDto.RegistrationModeStatus` will start emitting "active" again for
   paid + B-mode events. No mapper change needed — the gate removal cascades.
4. Verify: free Mode B regression still works; paid B-mode RSVP creates registrations as
   expected; the `// PHASE_7E_3B` breadcrumb comments + `RegistrationModeErrorCodes.PaidHeadCountDeferred`
   constant can stay one release as no-ops, then be removed in a separate cleanup commit.
Full RCA + paid-B-mode-gate slice details: [docs/MASTER_TODO_PHASE_7E_PAID_BMODE_GATE.md](MASTER_TODO_PHASE_7E_PAID_BMODE_GATE.md).

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

- [x] **7E.0 INNER JOIN audit complete**: every `Registration` join in standalone-payment query handlers (Donation/AddOnPurchase/Sponsor/Collection) is already a filtered single-column nullable comparison, not an `INNER JOIN`. No code change needed — Phase 1 RCA + 7E.0 grep confirmed.
- [x] **Spots-left aggregation**: `Registration.GetAttendeeCount()` already honors `HeadCount?.Total` (7E.1); every `Sum(r.GetAttendeeCount())` aggregator inherits the Mode-B path automatically.
- [x] **DTO mode-aware demographic fields** (commit 8220b4ca): added `EventAttendeeDto.MaleCount`/`FemaleCount` populated by SQL projection (Mode A) + post-processing override (Mode B).
- [x] **CSV export rewrite** (`CsvExportService.cs`): `MainAttendee` / `AdditionalAttendees` / `Adults` / `Children` / `Males` / `Females` / `GenderDistribution` columns all sourced from the DTO so Mode B exports show lead-name + "+N attendees" + populated demographic counts. Em-dash filtered for legacy single-attendee Mode A parity.
- [x] **Excel export rewrite** (`ExcelExportService.cs`): same DTO-sourced shape; removed per-row male/female recount.
- [x] **All 68 Phase 7E tests green** post-edit.
- [x] Commit `feat(7E.8): mode-aware attendee CSV/Excel exports` pushed → develop deploy 24972376188.
- [ ] Tier breakdown column (deferred to Phase 7F when Mode-B paid + tier counts ship).
- [ ] Mode C empty-CSV "No registrations — Mode: NoRegistration" footer note (cosmetic; deferred — current behavior is empty rows + header which is acceptable).

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

**Status**: ✅ COMPLETE (2026-04-27).
**Goal**: Verify everything in 7E.0 checklist + plan §11 verification sections.
**Deploy**: none (verification only).

### Steps

#### Per-mode lifecycle (free events) — done in 7E.3a/7E.4/7E.8 sub-slice smokes; 7E.9 condensed sweep

- [x] **Mode A free** baseline: legacy event `c0cd6cfd-…` `GET /Events/{id}` → `mode=DetailedAttendees`; `/attendees` returns per-attendee shape unchanged; CSV export 13 cols + populated MaleCount/FemaleCount per row.
- [x] **Mode B2 free** (HeadCountByAge): event `16eeb15c-…` 2 RSVPs (5+4 attendees) → CSV "Smoke Lead Adult"/"+4 attendees"/Adults=3/Children=2 + "Anon Family"/"+3 attendees"/Adults=2/Children=2; TOTAL row aggregates 9.
- [x] **Mode B3 free** (HeadCountByGender): event `69d4c455-…` RSVP `{males:2, females:1}` → `currentRegistrations=3`; CSV M=2/F=1 + GenderDistribution "2 Male, 1 Female"; `/attendees` returns post-processing-populated row.
- [x] **Mode B1 / B4** free: domain unit-tested; deferred from staging smoke (no organiser-visible delta beyond the breakdown line; same code path as B2/B3).
- [x] **Mode C free**: event `64bd61d3-…` (no donations) RSVP rejected HTTP 400 *"Registration is not required for this event…"* (auth + anonymous); event `40c8279a-…` (donations enabled) standalone donation → HTTP 200 + Stripe checkout URL + listed in `/donations` with `regId=None`.

#### Per-mode lifecycle (paid where allowed)

- Deferred to Phase 7F per architect plan (7E.3a was free-only; paid B-mode is 7E.3b/c). Paid Mode A regression untouched by 7E.

#### Edge cases

- [x] **Mode-change guard** (7E.1 unit-tested): `Event.SetRegistrationMode` throws `InvalidOperationException` if `Registrations.Any()`; standalone donations don't lock the mode (architect §6 — confirmed by reading `Event.RegistrationMode.cs` partial; `SetRegistrationMode` only inspects `Registrations`).
- [x] **Pre-7E `Registration` row**: `head_count` column is `NULL`, deserialises as `null`; `Registration.GetAttendeeCount()` falls back to `Attendees.Count`. Verified via legacy event `c0cd6cfd-…` (created 2026-03-08) returning correct attendee shape.
- [x] **Frontend defensive read**: `event.registrationMode ?? RegistrationMode.DetailedAttendees` wired in `AttendeeManagementTab`, `RsvpFormSection`, `EventCreationForm`, `EventEditForm` (verified via grep).
- Tier-rename-after-RSVP: deferred to Phase 7F (paid B-mode + TierCounts is 7F scope; the snapshot mechanism is unit-tested in 7E.1).

#### Capacity aggregation (architect risk #1)

- [x] **Mode A** baseline: legacy event `c0cd6cfd-…` `currentRegistrations=7` matches 7 single-attendee Mode A registrations.
- [x] **Mode B** new code path: B3 event `69d4c455-…` `currentRegistrations=3` (1 registration with HeadCount.Total=3). The canonical `Registration.GetAttendeeCount()` honors `HeadCount?.Total` so every `Sum(r.GetAttendeeCount())` aggregator inherits the Mode-B path automatically (7E.0 §2 sweep — 9 entries, zero needed editing).
- [x] **Mode C**: capacity informational only — RSVP rejection bypasses capacity check. Verified via 400 response on `64bd61d3-…`.

#### Standalone-contribution path under Mode C (per §11.4)

- [x] **Donation** on Mode C event `40c8279a-…` → HTTP 200 + Stripe URL + listed with `regId=None`. INNER JOIN concern empirically resolved (4 `left-join-fix` entries are nullable single-column lookups, not joins).
- Sponsor / addon / collection on Mode C: same code path; deferred (donation was the architect-flagged smoking-gun case; the four standalone-payment entities all have nullable `Guid? RegistrationId` and use the same pattern).
- Donation refund on Mode C: deferred (refund flow is unchanged by Phase 7E; architect-flagged scenario is the create path).

#### Container logs sweep

- [x] **Zero unexpected exceptions** in 500-line `az containerapp logs show --name lankaconnect-api-staging --tail 500` window covering the 7E.9 smoke (filtered out `error_message` SQL-column false positives + EF SELECT noise).

#### Tracking docs final pass

- [x] **PROGRESS_TRACKER.md** — 7E.8 + 7E.9 entry added (top of file).
- [x] **STREAMLINED_ACTION_PLAN.md** — full 7E.8 + 7E.9 entry added.
- [x] **MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md** — header status flipped to ✅; checkboxes updated below.
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

---

# Phase 7F-E — Registration Display Consistency Across Surfaces

**Status**: 🚧 IN FLIGHT (2026-05-05) — Slices 7F-E.1 → 7F-E.6 SHIPPED + STAGING-VERIFIED. **7F-E.7 deployed + smoke-verified** (commit `dfd67280`, deploy run `25358012928` success): closes the 7F-E.6 → 6.A → 6.B bug-find loop by re-opening Phase 7F-C §2.2 #4 deferred decision and storing per-tier 4-leaf demographics on `TicketCount`. Form aggregation now feeds `tierFourLeaf` state into per-tier `tierCounts[].adultMaleCount/...`; formatter renders captured per-tier 4-leaf instead of N/A; legacy registrations keep N/A + Totals row (back-compat). Smoke `27978d36-...` registration on event `87607c7a-...` confirms `head_count.tierCounts[]` JSONB carries all 4 fields. **Operator UAT pending** as final gate (memory `feedback_operator_uat_gate.md`).

**Trigger**: User UI testing on 2026-05-01 surfaced 5 cross-surface display gaps for Mode-B head-count registrations on a paid B2-tiered event (`Christmas Dinner Dance 2025`):
1. Ticket PDF: no tier separation (just `General Admission · $375 · 4 attendee(s)`)
2. Confirmation email: no per-attendee count + tier breakdown
3. Event-detail "You're Registered!" card: only `Number of attendees: 4` — no tier or demographic
4. No demographic placeholders anywhere — even when mode doesn't capture age/gender, user wants explicit `Adult/Child: N/A` / `Male/Female: N/A` (so absence is visible)
5. RSVP form: tier selector and demographic spinners are in two separate sections — user wants merged per-tier (Phase 7F-C's opt-in age-split toggle should be the default for B2/B4 + tiered)

## Classification

- **UI**: ✅ primary issue — 4 surfaces don't render consistently (PDF, email, event-detail card, RSVP form layout)
- **Backend API/DTO**: ⚠️ partial — `TicketPdfData` and `RegistrationDetailsDto` lack Mode-B fields
- **Database**: ❌ data is correctly captured (`head_count` JSONB has all fields)
- **Auth**: ❌ N/A
- **Feature gap**: ✅ architectural — no shared projection of "what does a registration look like to a human"; surfaces drifted because each one formatted independently

## Root cause (architect-confirmed)

The five symptoms manifest one architectural gap: **four surfaces independently formatting the same domain concept with no shared contract**. The existing `HeadCountEmailFormatter` is a partial version, but it's email-coupled (returns flat strings shaped for email rendering). Each new surface (PDF, FE card, RSVP form) re-invented its own rendering, leading to drift.

**The fix is architectural**: promote the existing email-only helper to a shared application-layer projection (`RegistrationBreakdown`) that all 4 surfaces consume. The 5 gaps are downstream symptoms.

## Architect-approved domain shape (Slice 1 ships)

```csharp
public sealed record BreakdownPair {
    public bool Captured { get; init; }     // false → render "N/A"
    public int Left { get; init; }          // adults / males
    public int Right { get; init; }         // children / females
    public string LeftLabel { get; init; }  // "Adult" / "Male"
    public string RightLabel { get; init; } // "Child" / "Female"
}

public sealed record RegistrationBreakdownRow {
    public string? TierName { get; init; }   // null = non-tiered
    public int Count { get; init; }
    public BreakdownPair Age { get; init; }       // Captured iff B2/B4/Mode A
    public BreakdownPair Gender { get; init; }    // Captured iff B3/B4/Mode A
}

public sealed record RegistrationBreakdown {
    public IReadOnlyList<RegistrationBreakdownRow> Rows { get; init; }
    public int TotalAttendees { get; init; }
    public RegistrationMode Mode { get; init; }
    public bool IsTiered { get; init; }
}
```

Architect edit #1: `Captured` boolean makes "N/A" a property of the data, not the renderer — every surface renders identically.

## Architect-approved 4-slice ship order (1 → 2 → 3 → 4a → 4b)

| Slice | Focus | User-visible blast radius | Risk | Independence |
|---|---|---|---|---|
| **7F-E.1** | `RegistrationBreakdown` + `RegistrationBreakdownFormatter` (covers Mode A + B1/B2/B3/B4 × tiered/non-tiered) + ≥24 unit tests + 90% coverage | Zero — pure prep | Low | Hard pre-req for all others |
| **7F-E.2** | `RegistrationDetailsDto` extension + Event-detail "You're Registered!" card render | One surface (FE only) | Low | Ships independently after .1 |
| **7F-E.3** | Email template token migration: `{{HeadCountBreakdownLine}}` + `{{TierBreakdownLine}}` → single `{{RegistrationBreakdownHtml}}` token (pre-rendered HTML fragment) | Email surface only | **High** — template-content invariants | Ships independently after .1 |
| **7F-E.4a** | PDF ticket renderer + `TicketPdfData.RegistrationBreakdown` extension | PDF only | Medium — visual regressions | Ships independently after .1 |
| **7F-E.4b** | `HeadCountRsvpForm` merged tier+demographic layout per architect Q4 auto-detect rules | Form (write-side) | Medium | **MUST ship LAST** — write-side change; if shipped before read sides, new commitments display incorrectly across stale surfaces |

## API testing requirements per slice (operator-mandated, 2026-05-01)

Each slice ships with explicit API verification. No "endpoint registered" claims without exercising the actual code path. The format below mirrors what user expects to see in closeout commits.

### 7F-E.1 — formatter
- No API surface — verified by 25 unit tests + the suite-level regression check (2538/6/0)

### 7F-E.2 — DTO + event-detail card
- **API smoke target**: the registration-details GET endpoint that the FE card consumes (likely `/api/registrations/me?eventId={id}` or `/api/events/{id}/registrations/{regId}` — confirm during implementation)
- **What to verify**: response includes the new `breakdown` field with `Rows[]`, `TotalAttendees`, `Mode`, `IsTiered`. Run for each Mode (A, B1, B2, B3, B4) on staging using existing test registrations.
- **Assertions per mode**:
   - B1 → `Rows[i].Age.Captured = false` AND `Rows[i].Gender.Captured = false`
   - B2 → `Rows[i].Age.Captured = true` AND `Rows[i].Gender.Captured = false`
   - B3 → `Rows[i].Age.Captured = false` AND `Rows[i].Gender.Captured = true`
   - B4 → both Captured = true
   - Mode A → both derived from attendees (Captured iff data present)
- **Cleanup**: no DB mutation expected.

### 7F-E.3 — email template migration
- **API smoke target**: triggering a real registration that fires the confirmation email (anonymous register on a free B-mode event), then inspecting the rendered HTML.
- **What to verify**:
   - psycopg2 probe: `template-event-registration-confirmation` template body contains the new anchor `<!-- registration-breakdown-7e -->` post-migration
   - Run negative-evidence smoke per `feedback_email_smoke.md`: register on B1 event, B2 event, B3 event, B4 event; for each fetch the rendered HTML from ACS log (or `email_messages` table if populated)
   - Assert the rendered fragment contains the per-tier table with N/A placeholders for un-captured axes
- **Mandatory pre-flight**: probe staging DB body BEFORE writing the migration anchor (memory `feedback_template_body_is_authoritative.md`).

#### 7F-E.3 close-out (2026-05-02)
- [x] **7F-E.3a** — `RegistrationBreakdownEmailRenderer` ships inline-styled HTML matching Phase 7F-A warm-card aesthetic (cream background `#fefaf7`, brown border `#f3e4d5`); HTML-encodes lead name + tier name; returns empty string when no rows. 7 TDD tests cover Mode A (1 row), Mode B1 (NotCaptured both), B2 (Age captured), B3 (Gender captured), B4 (4-leaf), tiered (multi-row), and empty fallback.
- [x] **7F-E.3b** — `EmailTemplateContract.RegistrationBreakdownHtml = "RegistrationBreakdownHtml"` constant added. 5 EmailParams classes gained `RegistrationBreakdownHtml` field + ToDictionary entry: `FreeEventRegistrationEmailParams`, `EventCancellationEmailParams`, `EventReminderEmailParams`, `AttendeesAddedEmailParams`, `RegistrationCancellationEmailParams`. 6 producer sites populate via `flex.registrationBreakdownHtml`: `RegistrationConfirmedEventHandler`, `AnonymousRegistrationConfirmedEventHandler`, `AttendeesAddedEventHandler`, `RegistrationCancelledEventHandler`, `EventCancellationEmailJob`, `EventReminderJob` (2 sites). `HeadCountEmailFormatter.Compute()` calls `RegistrationBreakdownEmailRenderer.Render` with try-catch fail-soft so renderer faults don't break email send.
- [x] **7F-E.3c** — Migration `20260502040451_Phase7FE3_RegistrationBreakdownTemplateMigration` UPDATEs 5 templates from embedded resources (`Data/Migrations/Resources/Phase7F_E/*.html`, wired in `LankaConnect.Infrastructure.csproj`). 4 templates have `<!-- attendee-block-7e -->` inner replaced with `{{{RegistrationBreakdownHtml}}}`; paid-with-ticket gets a NEW anchor block inserted before `<!-- PAYMENT CONFIRMATION CARD -->` (fixes the user-reported gap where paid-event emails had no per-tier breakdown at all). Each pre-update body backed up to `communications.email_template_backups` with `migration_tag='Phase7F_E_3'`. `Down()` restores from that table. Build green; 2548/6/0 Application tests; 32 targeted Phase 7F-E tests pass.
- [x] **7F-E.3d** — Pushed (commit `27990602`); `deploy-staging.yml` run `25243524495` SUCCESS. Verification:
   - **psycopg2 probe** (`scripts/verify_phase7fe3_migration.py`): 5/5 templates carry the new token + clean anchor pair + zero legacy tokens; lengths exactly 74185 / 84832 / 86158 / 71726 / 109254. 5/5 backup rows present with `migration_tag='Phase7F_E_3'`.
   - **Email smoke** (`scripts/smoke_phase7fe3_email.py`): B2 (3 adults / 2 children) + B3 (4 males / 3 females) anonymous registrations on staging both returned HTTP 200; container logs show `AnonymousRegistrationConfirmed COMPLETE: Email sent to niroshhh+7fe3@gmail.com, AttendeeCount=5/7, Duration=~10s` with zero exceptions in the pipeline. Inbox-side visual verification by operator (per memory `feedback_email_smoke.md` — `email_messages` table is empty in staging because ACS direct-send doesn't persist; live inbox is the only way to inspect rendered HTML).

### 7F-E.4a — PDF ticket
- **API smoke target**: ticket generation path — `GET /api/registrations/{id}/ticket-pdf` (or wherever)
- **What to verify**:
   - Download the PDF for a Mode-B tiered registration
   - Visually inspect: per-tier table renders with N/A placeholders where mode doesn't capture an axis
   - Mode A regression: existing PDF still shows the attendee-name list AND adds the breakdown summary block

#### 7F-E.4a close-out (2026-05-03)
- [x] **Assembler** — `TicketPdfRegistrationBreakdownAssembler.Build(Registration)` dispatches to `RegistrationBreakdownFormatter.FromAttendees` (Mode A) or `FromHeadCount(headCount, mode)` (Mode B); returns null on null/empty registration. 8 unit tests in `Phase7FE4aTicketPdfBreakdownAssemblerTests.cs` cover Mode A non-tiered + tiered, B1/B2/B3/B4 non-tiered, B2 tiered, and the null-registration defensive path.
- [x] **DTO extension** — `TicketPdfData` (in `IPdfTicketService.cs`) gains `RegistrationBreakdown? RegistrationBreakdown { get; init; }`.
- [x] **Producer wiring** — `TicketService.cs` populates the field at all 3 PDF-build sites (`GenerateTicketAsync`, `RegeneratePdfAsync`, `RegenerateTicketPdfForRegistrationAsync`). The assembler is recomputed from the CURRENT registration each time so post-add-attendee regenerations (slice 7F-D path) reflect new counts.
- [x] **Renderer** — `PdfTicketService.ComposeRegistrationBreakdown` adds a "Registration Breakdown" section after the per-attendee list and before the Payment section. Architect "in addition to" rule preserved: Mode A keeps the existing attendee list AND ALSO surfaces the breakdown; Mode B (no per-attendee data) uses the breakdown as the primary attendee block. Per-row layout: `Tier:` line iff tiered, then `Adult/Child:` and `Male/Female:` with `X/Y` or `N/A` per `BreakdownPair.Captured`.
- [x] **Tests + build** — Application 2560/6/0 + Infrastructure 317/0/0 all green; zero compile warnings introduced.
- [x] **Staging deploy + API smoke** — commit `505ed846` deployed via run `25282974985` (success). `scripts/smoke_phase7fe4a_pdf.py`: logs in as `niroshhh@gmail.com`, clears `PdfBlobUrl` on a paid Mode-A and a paid Mode-B2 ticket to force regeneration via the new code path, downloads each through `GET /api/Events/{id}/my-registration/ticket/pdf` (200 / `application/pdf`), and asserts the extracted text contains the new section. Result PASS:
   - **Mode A** (`fb32341f-...` Phase 6A.136 Payment Test): per-attendee bullet `• Niroshana Sinharage (Adult)` preserved AND new section renders `Total: 1 attendee(s)` / `Adult/Child: 1/0` / `Male/Female: 1/0` (architect "in addition to" rule satisfied).
   - **Mode B2 tiered** (`e6285ea7-...` Christmas Dinner Dance 2025): no per-attendee list (Mode B has no detailed attendees), new section renders `Total: 4 attendee(s)` / `Tier: VIP × 4` / `Adult/Child: 2/2` / `Male/Female: N/A` (B2 captures Age, NotCaptured for Gender).
   - PDFs saved to `c:/tmp/7fe4a-Mode_A-fb32341f.pdf` and `c:/tmp/7fe4a-Mode_B2-e6285ea7.pdf` for operator visual verification.

### 7F-E.4b — RSVP form merge
- **API smoke target**: register through the new merged form on B2 + tiered + ChildPrice event, confirm the `tierCounts[]` payload carries `adultCount`/`childCount` per tier (not separate from a top-level age section)
- **What to verify**:
   - Network panel of the form submission shows the merged payload shape
   - Resulting `head_count` JSONB on the registration row has per-tier `AdultCount`/`ChildCount` fields populated
   - B1 / B3 paths still work (form falls back to non-merged for B1; B3 always merges per architect rule)

#### 7F-E.4b close-out (2026-05-03)
- [x] **Form** — `HeadCountRsvpForm.tsx`: added `mergeAge` / `mergeGender` / `mergeFourLeaf` / `mergedLayout` auto-detect derivations matching architect Q4 rules. Per-tier `tierGenderSplit` (B3) and `tierFourLeaf` (B4) state added with auto-rebalance effects + `updateGenderLeaf` / `updateFourLeaf` clamping helpers.
- [x] **Render** — Per-tier Adults/Children spinners are now ALWAYS visible under each tier card when `mergeAge` is on (B2 + tiered + ChildPrice on at least one tier; opt-in toggle removed for that path). New B3 tiered → per-tier Males/Females. New B4 tiered → per-tier 4-leaf (AM / AF / CM / CF). Tiers without ChildPrice in B2 still surface the "billed at adult price" helper.
- [x] **Top-level visibility** — Top-level demographic block is HIDDEN when `mergedLayout === true` (no double-entry). When the merged layout is OFF (non-tiered B-modes, B1+tiered, or B2+tiered with NO tier offering ChildPrice), the top-level block stays visible and the form behaves exactly as before.
- [x] **Submit aggregation** — Under merged layout, registration-level `adults`/`children`/`males`/`females`/`adultMales`/etc. are derived by summing the per-tier values; under non-merged layout, the user-entered top-level values are sent as-is. Per-tier age uses the existing `TierCountDto.adultCount`/`childCount` wire fields (Phase 7F-C); per-tier gender / 4-leaf are UI-only (gender has no per-tier pricing dependency, so per-tier capture is purely a UX improvement).
- [x] **Validation update** — The 7F-C cross-axis sum check (per-tier adults must equal demographic adults) is skipped when `mergedLayout` is on (the merged layout makes mismatch impossible by construction).
- [x] **Tests** — `Phase7FC.test.tsx` rewritten for the new always-on B2 behavior + B2 no-ChildPrice fall-back; new `Phase7FE4b.test.tsx` covers B3 tiered, B4 tiered, B3 non-tiered, B4 non-tiered, B1 tiered. 9/9 green; full events feature suite 78/78 green.
- [x] **Staging deploy + browser test (2026-05-04)** — operator confirmed merged form rendering, BUT browser test surfaced two follow-up bugs (see 7F-E.5 + 7F-E.6 below). Form layout is correct; submit-side and display-side both have separate issues that need their own slices.

### 7F-E.5 — Pricing-guard fix (architect-approved 2026-05-04)
✅ **CLOSED 2026-05-04 (commit `e30c37d6`).** Latent domain bug surfaced when operator tried to RSVP on the new B4+tiered staging event `616e59f3-...`. Domain guards at `Event.RegistrationMode.cs:740` + `Event.cs:1130` checked legacy `Pricing == null && TicketPrice == null` invariant before falling through to the Tiered branch — but Tiered+active tiers IS itself a complete pricing config. Fix: extracted private `HasPaidPricingConfigured()` helper composing the three valid pricing shapes; replaced both guard sites; sanitised user-facing error message (no longer leaks `SetPricing()`/`SetDualPricing()`/`SetGroupPricing()`). Pre-fix repro confirmed via `scripts/smoke_pricing_guard_b4_tiered.py` HTTP 400; post-fix re-test HTTP 200 + `total_price = 130.00 USD` in `events.registrations`. 5 new TDD tests in `EventPaidPricingGuardTests.cs`. Application 2573/6/0 + Infrastructure 317/0/0 + Domain (2 pre-existing flakes confirmed unrelated via git-stash bisect).

### 7F-E.6 — Breakdown display gap + paid-event email token (architect-approved 2026-05-04)

**Status:** 📋 ARCHITECT-APPROVED, READY TO IMPLEMENT. TDD-first.

**Two bugs surfaced during operator browser test of the B4+tiered RSVP on event `616e59f3-...` (2026-05-04 screenshots).**

#### 7F-E.6.A — Bug 1: formatter loses captured 4-leaf demographics in B4 multi-tier
- **Class**: Backend / Domain formatter display gap
- **Repro**: Operator registered VIP×4 + Standard×4 on B4+tiered event; DB row stores `head_count.demographics = {adultMales:2, adultFemales:2, childMales:2, childFemales:2}` correctly. But event-detail card AND PDF ticket both show `Adult/Child: N/A, Male/Female: N/A` for every tier row — captured data is invisible on the read side.
- **Root cause**: `RegistrationBreakdownFormatter.FromHeadCount` multi-tier branch deliberately marks per-tier age + gender as NotCaptured per architect Phase 7F-C §2.2 #4 (per-tier gender storage deferred). The shape `RegistrationBreakdown { Rows[], TotalAttendees, Mode, IsTiered }` has no field that can carry registration-level demographics for multi-tier display.
- **Architect-approved fix**: Extend the shape with optional `Totals` row (a NEW `RegistrationBreakdownTotals` record holding only Age + Gender pairs — no TierName, no Count). Populated in `FromHeadCount` multi-tier branch when `Rows.Count > 1 && (captureAge || captureGender)`. Per-tier rows keep N/A (preserves architect §2.2 #4 deferred decision); a registration-level Totals row surfaces the captured data.
- **Renderers**: each gains one conditional block at the BOTTOM of the per-tier list (architect: preserves natural reading order):
   - `RegistrationBreakdownEmailRenderer.cs` (HTML email)
   - `PdfTicketService.cs` `ComposeRegistrationBreakdown` (PDF ticket)
   - `web/.../RegistrationBreakdownCard.tsx` + TS DTO type (event-detail card)
- **TDD-first tests** (new file `Phase7FE6_FormatterTotalsRowTests.cs`):
   - B4 multi-tier with captured 4-leaf → Totals row populated with both axes Captured
   - B3 multi-tier with captured gender → Totals row with Gender Captured, Age NotCaptured
   - B2 multi-tier with captured age → Totals row with Age Captured, Gender NotCaptured
   - B1 multi-tier (no demographics axes) → Totals null
   - Single-tier B-mode (Rows.Count == 1) → Totals null (already in Rows[0])
   - Non-tiered → Totals null
   - Mode A multi-tier → Totals null (per-attendee data in Rows is the source of truth)

#### 7F-E.6.B — Bug 2: paid-event email shows literal `{{{RegistrationBreakdownHtml}}}`
- **Class**: Backend / 7F-E.3 missed handler wiring
- **Repro**: Operator's RSVP completed Stripe payment → confirmation-with-ticket email arrived showing the literal token text instead of the rendered breakdown card.
- **Root cause**: 7F-E.3 migration updated `template-paid-event-registration-confirmation-with-ticket.html` body to use the new `{{{RegistrationBreakdownHtml}}}` token. But `TicketConfirmationEmailParams` had no field for it; `PaymentCompletedEventHandler` + `ResendTicketEmailCommandHandler` never populated it. Producer-side gap missed in 7F-E.3 because my smoke registered against a free B3 event and never exercised the paid-event-with-ticket pipeline.
- **Architect-approved fix**:
   1. Add `RegistrationBreakdownHtml` field to `TicketConfirmationEmailParams` + `ToDictionary` entry + a fluent `WithRegistrationBreakdown(RegistrationBreakdown, string? leadName)` setter that calls `RegistrationBreakdownEmailRenderer.Render` (keeps renderer call out of handlers).
   2. Wire setter at both producer sites: `PaymentCompletedEventHandler` (~line 246) + `ResendTicketEmailCommandHandler` (~line 356).
   3. **Sweep grep** for any other `TicketConfirmationEmailParams.Create` callers (e.g. add-attendees-payment-completed, refund-completed) — every site must call the setter or the bug recurs.
   4. **Audit `EmailTemplateValidationService` startup contract**: does it warn when a template body has a token no Params class declares? If yes, find out why it didn't fire on 7F-E.3 deploy. If no, that's a separate observability gap (architect: separate slice).
- **TDD-first tests** (new file `Phase7FE6_TicketConfirmationBreakdownTests.cs`):
   - `WithRegistrationBreakdown(breakdown, leadName)` populates `RegistrationBreakdownHtml` field with rendered HTML
   - `WithRegistrationBreakdown(null, ...)` sets empty string (defensive)
   - `ToDictionary()` includes the `RegistrationBreakdownHtml` key

#### API testing (operator-mandated, per `feedback_email_smoke.md` + `feedback_smoke_user_flows.md`)

- **Smoke matrix expansion** (architect: cross-surface slices need cross-product coverage):
   - Extend `scripts/smoke_phase7fe3_email.py` with a paid-event-with-ticket case
   - Authenticated RSVP on B4+tiered event `616e59f3-...` (now unblocked by 7F-E.5 pricing-guard fix)
   - Stripe-test webhook completion to fire `PaymentCompletedEventHandler`
   - **Negative-evidence assertion**: rendered email body must NOT contain literal `{{{` anywhere (catches Bug 2 class)
   - **Positive-evidence assertion**: rendered body contains the breakdown card HTML signature (`Total attendees`, `Adult/Child`, `Male/Female`)
- **Display-side smoke** (Bug 1):
   - Re-render the existing `f8f28333-...` registration's breakdown card (after deploy) via `GET /api/Events/{id}/my-registration` — assert the response carries the new `Totals` field with `{ age: { captured: true, left: 4, right: 4 }, gender: { captured: true, left: 4, right: 4 } }`
   - Visual regression: operator browser-checks the event-detail card now shows the Total row at the bottom of the per-tier list

#### 7F-E.6 close-out (2026-05-04 — commit `f665a2b6`, deploy run `25341671895` success)
- [x] **Bug 1 RED tests** — 7 cases in `Phase7FE6FormatterTotalsRowTests.cs`; compile-fail confirmed missing `Totals` shape pre-fix.
- [x] **Bug 1 GREEN** — `RegistrationBreakdown` extended with optional `Totals` field (new `RegistrationBreakdownTotals` record); `RegistrationBreakdownFormatter.FromHeadCount` populates Totals when `IsTiered && Rows.Count > 1 && (captureAge || captureGender)`; 3 renderers updated (`RegistrationBreakdownEmailRenderer`, `PdfTicketService.ComposeRegistrationBreakdown`, `web/.../RegistrationBreakdownCard.tsx` + `RegistrationBreakdownDto/TotalsDto` TS types). Architect-mandated bottom-of-list placement preserved on all 3 surfaces.
- [x] **Bug 2 RED tests** — 3 cases in `Phase7FE6TicketConfirmationBreakdownTests.cs`; compile-fail confirmed missing `RegistrationBreakdownHtml` field + `WithRegistrationBreakdownHtml` setter pre-fix.
- [x] **Bug 2 GREEN** — `TicketConfirmationEmailParams` field + `WithRegistrationBreakdownHtml(string?)` setter + ToDictionary entry using `EmailTemplateContract.FlexibleRegistration.RegistrationBreakdownHtml` constant. Setter takes pre-rendered HTML because Shared layer can't reference Application's renderer without inverting the project graph — handler call-sites do the renderer call.
- [x] **Sweep results documented** — `grep -rn "TicketConfirmationEmailParams.Create"` found 3 production sites, ALL wired: `PaymentCompletedEventHandler.cs:226` (Stripe webhook → confirmation email), `ResendTicketEmailCommandHandler.cs:338` (organiser-triggered resend), `RegistrationEmailService.cs:214` (legacy email service path). Each site does `TicketPdfRegistrationBreakdownAssembler.Build(registration) → RegistrationBreakdownEmailRenderer.Render → setter`, with try/catch fallback to empty string + warning log so a renderer fault never breaks email send.
- [x] **EmailTemplateValidationService audit recorded** — validator DOES bidirectional-check (line 372 / 391 in `EmailTemplateValidator.cs`). Latent gap: the per-template HashSet at line 71 wasn't kept in sync with 7F-E.3's template-body update; `RegistrationBreakdownHtml` was missing from the `template-paid-event-registration-confirmation-with-ticket` parameter set, so the "Template uses parameters not provided by code" check didn't fire. Added the constant to the HashSet as a regression guard. Stronger automation (auto-derive HashSet from Params class) flagged as separate observability slice.
- [x] **Build green** — Application 2583/6/0 (up from 2573 — +10 new), Infrastructure 317/0/0, Domain 607/0/2 (the 2 fails — FormResponse + DonationConfiguration — confirmed pre-existing via git-stash bisect, unrelated). Web events feature 78/78. 0 errors, 0 new warnings. Frontend type-check green.
- [x] **Smoke matrix passes** — `scripts/smoke_phase7fe6_paid_email_breakdown.py` exercised the resend-ticket pipeline against the existing paid+B4-tiered registration `f8f28333-...` on event `616e59f3-...`: HTTP 200, container log shows `ResendTicketEmail COMPLETE: Email sent successfully ... Duration=19929ms` with zero `[Phase 7F-E.6.B] Failed to render registration breakdown HTML` warnings — handler ran clean through the new wiring.
- [ ] **Operator browser re-verification** — refresh `https://lankaconnect-ui-staging.../events/616e59f3-...` to see the new "Total (across all tiers)" row showing `Adult/Child: 4/4 / Male/Female: 4/4` at the bottom of the per-tier list. Open the resent email at `niroshhh@gmail.com` and confirm the body no longer contains literal `{{{` AND now shows the breakdown card with the Totals row at the bottom. PDF ticket also gets the new Totals row.
- [x] **Memory saved** — `feedback_cross_surface_matrix_smoke.md` (architect-mandated process discipline; index entry added to `MEMORY.md`).

### 7F-E.7 — Per-tier 4-leaf storage (re-opens Phase 7F-C §2.2 #4 deferred decision)

**Status:** 📋 ARCHITECT-APPROVED 2026-05-04, READY TO IMPLEMENT TDD-FIRST.

**Why this slice exists:** Operator browser-tested 7F-E.6 close-out (commit `f665a2b6`) and rejected the per-tier `N/A` rendering. Architect deep RCA (2026-05-04) classified it as **feature missing (storage gap)**: the 7F-E.4b form captures per-tier 4-leaf, but the submit-aggregation step throws it away on the wire. The Totals row that 7F-E.6.A added shows the captured data at the registration level, but the per-tier rows it sits under say N/A because per-tier storage was deferred per Phase 7F-C §2.2 #4. Operator's intuition is right: capture-without-storage is data loss. Architect rejected Option B (hide per-tier `N/A` lines) as "a 30-min lie — operators will ask the same question in 6 weeks". Architect recommendation: **Option A — re-open §2.2 #4 and store per-tier 4-leaf**.

#### 7F-E.7.A — Domain: per-tier 4-leaf optional fields with all-or-nothing rule
- **Files**: `src/LankaConnect.Domain/Events/ValueObjects/TierCount.cs`
- **Add**: 4 optional fields on `TierCount` — `AdultMaleCount`, `AdultFemaleCount`, `ChildMaleCount`, `ChildFemaleCount`.
- **Rule**: all-or-nothing per tier (any set → all 4 set). Sum equals `Count`.
- **Architect note**: optional augmentation, not required. Legacy registrations with only top-level demographics keep current N/A render — back-compat preserved.

#### 7F-E.7.B — Wire: TierCountDto + value mapper
- **Files**: `src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommand.cs` (TierCountDto), command handler mapper.
- **Add**: 4 optional ints. Map to `TierCount` factory.

#### 7F-E.7.C — ValueComparer audit (memory 6A.129/6A.130)
- **Files**: `src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs` or wherever `head_count` jsonb is mapped.
- **Verify**: `List<int>?` with `private set` (NOT `IReadOnlyList`) inside any `OwnsOne().ToJson()` boundary. Audit existing `TierCount` ValueComparer for the new fields.
- **No EF migration needed**: `head_count` is jsonb, schema-less.

#### 7F-E.7.D — Form: feed per-tier 4-leaf into TierCountDto
- **Files**: `web/src/presentation/components/features/events/HeadCountRsvpForm.tsx`, `web/src/infrastructure/api/types/events.types.ts`.
- **Change**: form submit's `tierCounts[]` payload sends per-tier `adultMaleCount/adultFemaleCount/childMaleCount/childFemaleCount` from `tierFourLeaf` state. Top-level `headCount.adultMales/...` stays populated for back-compat.

#### 7F-E.7.E — Formatter: render per-tier captured 4-leaf
- **Files**: `src/LankaConnect.Application/Events/Common/RegistrationBreakdownFormatter.cs`.
- **Change**: multi-tier B4 branch — when `tc.HasFourLeafSplit` (new helper), mark per-tier age + gender Captured with the per-tier values. Legacy path (no per-tier 4-leaf) renders N/A as today + Totals row at registration level.
- **Totals row visibility**: skipped when ALL per-tier rows are captured (no need to duplicate). Totals row stays for legacy / partial coverage / mixed multi-tier.

#### TDD test plan (RED first)
- **Domain** (`TierCount` tests):
   - `Create` with all 4 leaves → succeeds, sum equals Count
   - `Create` with 1 leaf only → fails (all-or-nothing)
   - `Create` with sum != Count → fails (sum invariant)
   - `Create` with no leaves → succeeds, `HasFourLeafSplit = false` (back-compat)
- **Formatter** (extends `Phase7FE6FormatterTotalsRowTests`):
   - B4 multi-tier with per-tier 4-leaf set → per-tier rows Captured, Totals row null (or redundant; architect to confirm)
   - B4 multi-tier with NO per-tier 4-leaf (legacy) → per-tier rows NotCaptured + Totals row populated (current 7F-E.6.A behaviour, regression guard)
   - B4 multi-tier with PARTIAL per-tier (1 tier captured, 1 not) → architect call: keep both per-tier behaviours and surface Totals row, OR reject as invalid? My read: reject in domain (all-or-nothing across the basket); test asserts factory failure.
- **Wire round-trip**:
   - HeadCountDto with per-tier 4-leaf → registration row's `head_count.tierCounts[i]` carries the 4-leaf in JSONB.
- **Form submit**:
   - `tierFourLeaf` state with values → POST body's `tierCounts[].adultMaleCount` etc. populated.

#### API smoke matrix (operator-mandated, per `feedback_cross_surface_matrix_smoke.md`)
- **Pre-deploy**: capture pre-fix screenshots of operator's existing registration `f8f28333-...` (already shows per-tier N/A — that's the legacy state we're preserving).
- **Post-deploy**:
   - Create a FRESH paid+B4-tiered registration on a NEW test event (legacy `616e59f3` has stale data; we don't backfill it).
   - Authenticated RSVP via API smoke: payload includes per-tier `adultMaleCount/adultFemaleCount/childMaleCount/childFemaleCount`.
   - DB check: `head_count.tierCounts[i]` carries the 4-leaf in JSONB.
   - Operator UAT (per memory `feedback_operator_uat_gate.md`):
      1. Browse the NEW event's "You're Registered" card → per-tier rows show captured 4-leaf, NOT N/A
      2. Open paid-event email → same
      3. Download PDF ticket → same
   - Legacy registration `f8f28333-...` continues to render N/A on per-tier rows + populated Totals row (back-compat regression guard).

#### 7F-E.7 close-out (commit `dfd67280`, deploy run `25358012928` success)
- [x] **Memory saved** — `feedback_operator_uat_gate.md` (architect-mandated process gate for render-surface slices) + index entry in `MEMORY.md`.
- [x] **Master TODO entry written** — this section.
- [x] **Domain RED tests** — `Phase7FE7TierCount4LeafTests` 14 cases covering happy path, all-or-nothing, sum invariant, cross-axis (agree/disagree), back-compat. RED before fix.
- [x] **Domain GREEN** — `TierCount` 4 new optional fields (`AdultMaleCount/AdultFemaleCount/ChildMaleCount/ChildFemaleCount`) + all-or-nothing + sum-equals-Count + cross-axis-agreement-with-7F-C-age-split + auto-derive age split from 4-leaf for back-compat. `HasFourLeafSplit` derived helper. `GetEqualityComponents` extended.
- [x] **Wire/DTO** — `TierCountDto` 4 optional ints + 3 production handler sites mapped (`RsvpToEventCommandHandler`, `RegisterAnonymousAttendeeCommandHandler`, `InitiateAddHeadCountCommandHandler`) + 1 internal merge site (`Registration.cs:1370`). ValueComparer audit complete (JSON-roundtrip pattern picks up new fields automatically; round-trip regression test in `Phase7FE7TierCount4LeafJsonRoundTripTests`).
- [x] **Formatter RED + GREEN** — `RegistrationBreakdownFormatter` multi-tier B-mode renders captured per-tier gender from 4-leaf when `HasFourLeafSplit`; per-tier age covered by existing 7F-C `HasAgeSplit` branch (4-leaf auto-derives that). Totals row gating updated to skip when all per-tier rows are captured (architect "redundant when covered"). Legacy path preserved (regression guard test).
- [x] **Form submit aggregation** — `HeadCountRsvpForm.tsx`: when `mergeFourLeaf` is on, each tier's `tierCounts[]` entry carries `adultMaleCount/adultFemaleCount/childMaleCount/childFemaleCount` from `tierFourLeaf` state. Top-level demographics still aggregated for back-compat.
- [x] **Build green** — Application 2588/6/0 (+5 new) · Infrastructure 317/0/0 · Domain 630/0/2 (+21 new of which 14 are 7F-E.7 Theory cases; 2 fails confirmed pre-existing — FormResponse + DonationConfiguration) · web events feature 78/78. Frontend type-check clean. 0 build errors, 0 new warnings.
- [x] **Staging deploy** — API run `25358012928` SUCCESS · UI run `25358012931` SUCCESS · API health `EF Core DbContext: Healthy`.
- [x] **Smoke matrix** — `scripts/smoke_phase7fe7_per_tier_4leaf.py` PASS: authenticated RSVP on fresh paid+B4-tiered event `87607c7a-...` with per-tier 4-leaf payload (VIP × 4 with children + Standard × 4 adults-only) → HTTP 200 + Stripe Checkout URL · registration `27978d36-...` Preliminary · `total_price = $270.00` · `head_count.tierCounts[]` JSONB carries all 4 fields per tier (DB-verified). Domain pricing-guard correctly rejected the all-children-on-no-ChildPrice variant in initial test, validating cross-axis invariants from outside the unit-test boundary.
- [ ] **Operator UAT** — pending. Visit `https://lankaconnect-ui-staging.../events/87607c7a-9767-4208-8be3-dd0642016d79` and confirm per-tier rows show captured 4-leaf (VIP: 2/2 + 2/2 ; Standard: 4/0 + 4/0), NOT N/A. Also confirm legacy event `616e59f3-...` still renders N/A + Totals row (back-compat regression guard). Status flips to Shipped only after operator confirms.
- [ ] **3-doc sync** per CLAUDE.md §7 — pending after operator UAT pass.

### Cross-slice operator testing checkpoints (UI-testable from user side)

After **7F-E.2** ships → user can test: register on Mode-B event, see per-tier breakdown card on event detail page.
After **7F-E.3** ships → user can test: register, check inbox for confirmation email with per-tier table.
After **7F-E.4a** ships → user can test: register, download PDF ticket with per-tier breakdown.
After **7F-E.4b** ships → user can test: register through merged form on a B2 + tiered + ChildPrice event.

I'll explicitly tell the user "ready for UI testing" after each slice deploy completes.

**Order rationale (architect)**: Slice 1 unblocks all others (hard dep). Slice 2 (FE card) is lowest risk and validates the formatter shape against real rendering before email/PDF lock it in. Slice 3 (email) follows the existing Phase 7C.2 / 6A.122 playbook for template-content migrations. Slice 4 (PDF + form) closes out; 4b is the only write-side change so ships last.

## Architect-mandated UX rules for 7F-E.4b (RSVP form merge)

| Mode + Tiered | Layout |
|---|---|
| B1 + tiered | Tier-count spinner only (no demographic data to capture) |
| B2 + tiered with `ChildPrice` configured | **Merged**: per-tier Adults/Children spinners inline under tier card |
| B2 + tiered without `ChildPrice` | Fall back to current layout (tier-only count + separate Adults/Children) — pricing depends on it |
| B3 + tiered | **Merged**: per-tier Males/Females always (gender is capture-only, no pricing dependency) |
| B4 + tiered | **Merged**: per-tier 4-leaf always |
| Any non-tiered B-mode | Single demographic section (no per-tier dimension) |

## Mode A integration scope

- ✅ Include Mode A in formatter scope (architect-required — same drift otherwise)
- ✅ "In addition to" the existing attendee-name list, NOT replace (operator default 2026-05-01)
- Formatter takes either `HeadCountBreakdown` (Mode B) OR `IReadOnlyList<AttendeeDetails>` (Mode A) and projects both into the same `RegistrationBreakdown`

## Cancel/reminder email scope

- ✅ Include in 7F-E.3 — same flat-token drift
- The existing Phase 7F-A Mode-B blocks in `template-event-cancellation-notifications`, `template-event-reminder`, and `template-attendees-added-confirmation` use the same `{{HeadCountBreakdownLine}}` / `{{TierBreakdownLine}}` tokens; migrating them in the same slice keeps consistency
- Architect-mandated playbook: psycopg2 staging probe → unique HTML comment anchor → row count assertion → negative-evidence smoke per memory `feedback_email_smoke.md`

## Deferred / out of scope

- **Organiser "Manage Attendees" view** — likely has the same drift but operator confirmed defer; can ship as 7F-E.5 if needed after .4b
- **i18n** — bake EN strings into formatter (operator default; Phase A is EN-only per `ADR-001-i18n-scope-phase-a.md`); revisit when i18n hooks are introduced

## Architect-required risks (named explicitly)

1. **Slice 7F-E.3 (email migration) is the riskiest single change.** Apply existing playbook: psycopg2 probe staging template body before writing migration; anchor on unique HTML comment (`<!-- registration-breakdown-7e -->`); verify post-UPDATE row count > 0. **Non-negotiable.**
2. **`RegistrationDetailsDto` schema change** in Slice .2 — verify scope (mobile / external consumers?). If FE-only, fine.
3. **JSONB `headCount` field** ValueComparer audit per memory 6A.129/6A.130.
4. **`HeadCountRsvpForm` usage scan** — `grep -r "HeadCountRsvpForm" web/src/` before .4b changes (could be used in organiser preview/dry-run too).
5. **Form layout regression risk** in .4b — screenshot every (mode × tiered/non-tiered) combination before/after.

## Test floor (architect-mandated)

- **Slice 7F-E.1**: ≥24 cases (6 modes × 2 tiered/non-tiered + edges); 90% coverage on the formatter class — non-negotiable, this is the load-bearing component for 4 surfaces
- **Slice 7F-E.2**: backend DTO snapshot + 1 component test per mode rendering the card
- **Slice 7F-E.3**: golden-file HTML render per mode + staging negative-evidence smoke
- **Slice 7F-E.4a**: structured `RegistrationBreakdown` populated correctly + visual inspection on staging
- **Slice 7F-E.4b**: form component test per mode × `ChildPrice` configured/not branches

## Naming (architect edits #10)

- `RegistrationBreakdown` (not `RegistrationSummary`) — domain-vocabulary consistency with existing `HeadCountBreakdown`
- `BreakdownPair` (not `RegistrationSummaryCell`) — "cell" is rendering vocabulary leaking into the model
- `Captured` flag — correct ("data was collected for this mode")

## Slice 1 scope discipline (architect edit #7)

Frame Slice 7F-E.1 as **"promoting the email-only `HeadCountEmailFormatter` to a shared application projection"** — not as introducing a brand-new abstraction. The behaviour exists; it just needs to move to where 4 surfaces can reuse it.

Old `HeadCountEmailFormatter.FormatDemographicLine` / `FormatTierLine` flat-string methods stay as thin delegators that wrap `RegistrationBreakdownFormatter` output for backward compatibility, then get deleted in Slice 3 once the email template no longer references them.
