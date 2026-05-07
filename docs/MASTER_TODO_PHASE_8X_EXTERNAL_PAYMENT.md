# Master TODO — Phase 8X: External Payment Events

**Status**: 🟡 PLANNED — not started.
**Workflow**: All commits go directly to `develop` (project policy — no feature branches).
**Architect-approval**: 2026-05-07 (this conversation).
**Origin**: User requirement 2026-05-07 — third event payment mode where pricing is displayed but payment + registration happens off-platform (e.g., Eventbrite, Humanitix, organiser's own page, bank-deposit instructions).
**Classification**: feature missing, cross-stack (Domain + EF + App + API + FE + email).
**Baseline domain test count (pre-flight)**: 642 (snapshotted 2026-05-07; subsequent slices must not drop below this).

## Hard rules (do not violate)
- [ ] Never use `builder.Ignore` — Phase 6A.123 silent INSERT failure lesson
- [ ] Never hand-create migration files — `dotnet ef migrations add …` only; verify `.Designer.cs` exists (Phase 6A.133)
- [ ] Backfill SQL MUST embed `RAISE EXCEPTION` post-assertion (Phase 6A.122 silent-UPDATE lesson)
- [ ] TypeScript enum values must be strings, matching `JsonStringEnumConverter` output (Phase 6A.124)
- [ ] API smoke must trigger real user flows, not just check status codes (memory)
- [ ] Cross-surface smoke matrix defined at slice-plan time — done below (memory 2026-05-04)
- [ ] Operator UAT before flipping render-surface slice to "Shipped" (memory 2026-05-04)
- [ ] Forward-only rollback by default (no `Down()` data loss without archive)
- [ ] Security default: missing `paymentMode` with `isFree != true` → `OnPlatformPaid`, never `Free` (Phase 6A.81)
- [ ] Domain layer never references Stripe / external-payment-provider types (defensive — enforced by project refs)
- [ ] Signup lists are compatible with ALL payment modes (engineer-corrected from architect's earlier draft — confirmed in code search 2026-05-07)

## Final compatibility matrix (locked)

| | Free | OnPlatformPaid | ExternalPaid |
|---|---|---|---|
| Mode A (DetailedAttendees) | yes | yes | blocked |
| Mode B1–B4 (HeadCount) | yes | yes | blocked |
| Mode C (NoRegistration) | yes | blocked (today, unchanged) | FORCED |
| AssignedSeating | yes | yes Mode A only | blocked |
| Add-ons | yes | yes | blocked |
| Waitlist | yes | yes | blocked |
| Check-in QR | yes | yes | blocked |
| Signup lists | yes | yes | yes |
| Donations / Sponsors | yes | yes | yes |
| Ticket tiers | yes | yes | display-only |

## Validator inference table (MUST be copied verbatim into `CreateEventCommandValidator.cs` as XML doc comment)

| `isFree` | `paymentMode` | Result |
|---|---|---|
| true | null | Free |
| true | Free | Free |
| true | OnPlatformPaid / ExternalPaid | 400 inconsistent |
| false | null | OnPlatformPaid (security default) |
| false | OnPlatformPaid | OnPlatformPaid |
| false | ExternalPaid | ExternalPaid |
| false | Free | 400 inconsistent |
| null | null | OnPlatformPaid (security default, Phase 6A.81) |
| null | non-null | as supplied |

---

## Pre-flight (must complete before slice 8X.1)

- [x] Confirm Phase 8X not used elsewhere in `docs/` (grep: zero matches 2026-05-07)
- [x] On `develop`, latest fetched (2026-05-07; HEAD = `e5a4a285`)
- [x] Verify local toolchain: `dotnet --version` (8.x), `node --version` (20.x)
- [x] Snapshot baseline domain test count: 642
- [ ] Register Phase 8X in `docs/PHASE_6A_MASTER_INDEX.md`
- [ ] Confirm staging credentials work via login curl
- [ ] Confirm staging DB connectivity

---

## Slice 8X.1 — Domain enum + ExternalRegistration VO

**Goal**: Add the enum and value object with full validation, no DB, no consumers. Pure domain.

### Files created
- [ ] `src/LankaConnect.Domain/Events/Enums/EventPaymentMode.cs`
- [ ] `src/LankaConnect.Domain/Events/ValueObjects/ExternalRegistration.cs`
- [ ] `tests/LankaConnect.Domain.Tests/Events/Enums/EventPaymentModeTests.cs`
- [ ] `tests/LankaConnect.Domain.Tests/Events/ValueObjects/ExternalRegistrationTests.cs`

### Tests (RED first)
- [ ] `EventPaymentModeTests`: `Free=0`, `OnPlatformPaid=1`, `ExternalPaid=2`, default value of new variable is `Free`
- [ ] `ExternalRegistrationTests.Create_WithValidHttpsUrl_Succeeds`
- [ ] `ExternalRegistrationTests.Create_WithHttpUrl_Fails` (message contains "https")
- [ ] `ExternalRegistrationTests.Create_WithMalformedUrl_Fails`
- [ ] `ExternalRegistrationTests.Create_WithUrlExceeding2048_Fails`
- [ ] `ExternalRegistrationTests.Create_WithEmptyUrl_Fails`
- [ ] `ExternalRegistrationTests.Create_WithNullUrl_Fails`
- [ ] `ExternalRegistrationTests.Create_WithLoopbackHost_Fails` (https://127.0.0.1, https://localhost, https://[::1])
- [ ] `ExternalRegistrationTests.Create_WithRfc1918Host_Fails` (https://10.0.0.1, https://192.168.1.1, https://172.16.0.1)
- [ ] `ExternalRegistrationTests.Create_WithLinkLocalHost_Fails` (https://169.254.0.1)
- [ ] `ExternalRegistrationTests.Create_WithInstructionsExceeding4000_Fails`
- [ ] `ExternalRegistrationTests.Create_WithVendorNameExceeding100_Fails`
- [ ] `ExternalRegistrationTests.Create_WithNullInstructionsAndVendor_Succeeds` (both optional)
- [ ] `ExternalRegistrationTests.Equality_SameUrlAndInstructions_AreEqual` (VO equality contract)

### Implementation
- [ ] Implement `EventPaymentMode` enum
- [ ] Implement `ExternalRegistration` VO with `Result<ExternalRegistration> Create(...)` static factory
- [ ] Run `dotnet test tests/LankaConnect.Domain.Tests` → all green; test count >= baseline (642) + new tests added

### Commit + push
- [ ] Commit: `feat(events): add EventPaymentMode enum + ExternalRegistration VO (Phase 8X.1)`
- [ ] Push to `develop`

### Doc updates
- [ ] Update `docs/STREAMLINED_ACTION_PLAN.md` with Phase 8X.1 status
- [ ] Update `docs/PROGRESS_TRACKER.md` with Phase 8X.1 entry

---

## Slice 8X.2 — EF config + migration + backfill

**Goal**: Add `payment_mode` and `external_registration_*` columns to `events.events`. No behaviour change yet — domain has no setter consumers.

### Files edited / created
- [ ] `src/LankaConnect.Domain/Events/Event.cs` — add `PaymentMode { get; private set; } = EventPaymentMode.Free;` and `ExternalRegistration? ExternalRegistration { get; private set; }` (private setters; no domain methods yet)
- [ ] Keep `IsFreeEvent` as a real entity property (Option B per architect — no `builder.Ignore`, no shadow property)
- [ ] `src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs` — add `builder.Property(e => e.PaymentMode).HasColumnName("payment_mode").HasConversion<short>().IsRequired().HasDefaultValue(EventPaymentMode.Free);` and `builder.OwnsOne(e => e.ExternalRegistration, ext => …)` mapping the three scalar columns
- [ ] Migration `Phase8X2_AddEventPaymentMode` generated via `dotnet ef migrations add Phase8X2_AddEventPaymentMode --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --context AppDbContext`
- [ ] `tests/LankaConnect.Infrastructure.Tests/Migrations/Phase8X2MigrationTests.cs`

### Migration content checklist
- [ ] `.Designer.cs` file exists (Phase 6A.133 — without it the migration is silently skipped)
- [ ] `Up()` adds `payment_mode smallint NOT NULL DEFAULT 0`
- [ ] `Up()` adds `external_registration_url varchar(2048) NULL`
- [ ] `Up()` adds `external_registration_instructions text NULL`
- [ ] `Up()` adds `external_registration_vendor_name varchar(100) NULL`
- [ ] `Up()` runs backfill: `migrationBuilder.Sql("UPDATE events.events SET payment_mode = 1 WHERE \"IsFreeEvent\" = false;")`
- [ ] `Up()` creates index: `CREATE INDEX ix_events_payment_mode ON events.events (payment_mode);`
- [ ] `Up()` embeds post-backfill assertion: `migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM events.events WHERE \"IsFreeEvent\" = false AND payment_mode = 0) THEN RAISE EXCEPTION 'Backfill failed: paid events with payment_mode=0'; END IF; END $$;");`
- [ ] `Down()` drops index then columns (forward-only rollback default — leading comment documents data-loss caveat for ExternalPaid rows)

### Tests
- [ ] `Phase8X2MigrationTests.MigrationApplies_OnFreshDatabase`
- [ ] `Phase8X2MigrationTests.Backfill_PaidEventsGetMode1`
- [ ] `Phase8X2MigrationTests.Backfill_FreeEventsStayMode0`
- [ ] `Phase8X2MigrationTests.Migration_IsIdempotent` (running twice — EF migration history rejects re-application)
- [ ] `dotnet test` whole solution → no regressions

### Local verification (before push)
- [ ] `dotnet ef database update` against local Postgres → success
- [ ] Local psql: `\d events.events` shows the 4 new columns + index
- [ ] Local psql: `SELECT payment_mode, COUNT(*) FROM events.events GROUP BY payment_mode;` → confirms backfill

### Commit + push + deploy
- [ ] Commit: `feat(events): EF config + migration for EventPaymentMode + ExternalRegistration (Phase 8X.2)`
- [ ] Push to `develop`
- [ ] Wait for `deploy-staging.yml` to complete green (poll GitHub Actions; do not sleep blindly)

### Staging verification
- [ ] Staging psql: `SELECT name FROM "__EFMigrationsHistory" WHERE name LIKE '%Phase8X2%'` → returns 1 row (HARD STOP if 0 — Phase 6A.133 silent-skip)
- [ ] Staging psql: `\d events.events` → 4 new columns present, `ix_events_payment_mode` present
- [ ] Staging psql: `SELECT payment_mode, COUNT(*) FROM events.events GROUP BY payment_mode;` → expected distribution
- [ ] Staging psql: `SELECT COUNT(*) FROM events.events WHERE "IsFreeEvent" = false AND payment_mode = 0` → 0 (assertion held)
- [ ] Azure container logs: no migration errors

### API regression smoke (must not have broken anything)
- [ ] Token via login curl from CLAUDE.md → 200
- [ ] `GET /api/Events/{free-event-id}` → 200, payload parses, `isFree: true`
- [ ] `GET /api/Events/{paid-event-id}` → 200, payload parses, `isFree: false`
- [ ] `GET /api/Events?pageSize=10` → 200
- [ ] `POST /api/Events` (existing free-event shape) → 201, persisted with `payment_mode=0`
- [ ] `POST /api/Events` (existing paid-event shape) → 201, persisted with `payment_mode=1`

### Doc updates
- [ ] Update `STREAMLINED_ACTION_PLAN.md` + `PROGRESS_TRACKER.md` with 8X.2 status

---

## Slice 8X.3 — Domain methods (SetExternalPayment, SetPaymentMode, RegisterWith* guards)

**Goal**: Add the domain methods that mutate `PaymentMode`, with active-registration guards. Block internal registration paths.

### Files edited / created
- [ ] `src/LankaConnect.Domain/Events/Event.cs` — add `SetExternalPayment(ExternalRegistration externalReg, TicketPricing pricing)`, `SetPaymentMode(EventPaymentMode mode)`, private `SyncLegacyIsFree()` helper called from each PaymentMode mutation
- [ ] `src/LankaConnect.Domain/Events/Event.RegistrationMode.cs` — add early-return `Result.Failure` when `PaymentMode == ExternalPaid` to RegisterWithAttendees / RegisterWithHeadCount
- [ ] `tests/LankaConnect.Domain.Tests/Events/Event_SetExternalPayment_Tests.cs`
- [ ] `tests/LankaConnect.Domain.Tests/Events/Event_SetPaymentMode_TransitionTests.cs`
- [ ] `tests/LankaConnect.Domain.Tests/Events/Event_RegisterBlockedForExternalPaid_Tests.cs`

### Transition rules (architect-locked, copy verbatim into XML doc comment on `SetPaymentMode`)
- Free → OnPlatformPaid: requires pricing; RegistrationMode unchanged.
- Free → ExternalPaid: requires pricing + ExternalRegistration VO; RegistrationMode forced to NoRegistration; ExternalRegistration set.
- OnPlatformPaid → ExternalPaid: requires no active regs; RegistrationMode forced to NoRegistration; ExternalRegistration set.
- ExternalPaid → OnPlatformPaid: requires no active regs; RegistrationMode auto-resets to **DetailedAttendees**; ExternalRegistration cleared to null.
- ExternalPaid → Free: requires no active regs; RegistrationMode auto-resets to DetailedAttendees; ExternalRegistration cleared to null; pricing cleared.
- OnPlatformPaid → Free: requires no active regs; pricing cleared; RegistrationMode unchanged.

### Tests (RED first)
- [ ] `SetExternalPayment_WithValidVoAndPricing_SetsPaymentModeAndRegMode`
- [ ] `SetExternalPayment_WithActiveRegistrations_Fails`
- [ ] `SetExternalPayment_WithAssignedSeating_Fails`
- [ ] `SetExternalPayment_WithNullPricing_Fails`
- [ ] `SetExternalPayment_Idempotent_SameStateTwice_Succeeds`
- [ ] `SetPaymentMode_FreeToOnPlatformPaid_NoRegs_Succeeds`
- [ ] `SetPaymentMode_OnPlatformPaidToExternalPaid_NoActiveRegs_Succeeds`
- [ ] `SetPaymentMode_OnPlatformPaidToExternalPaid_WithActiveRegs_Fails`
- [ ] `SetPaymentMode_ExternalPaidToOnPlatformPaid_NoRegs_Succeeds_RegistrationModeResetsToDetailedAttendees_ExternalRegistrationCleared`
- [ ] `SetPaymentMode_ExternalPaidToFree_NoRegs_Succeeds_RegistrationModeResetsToDetailedAttendees_PricingCleared`
- [ ] `RegisterWithAttendees_OnExternalPaidEvent_Fails`
- [ ] `RegisterWithHeadCount_OnExternalPaidEvent_Fails`
- [ ] Extend `EventPaidPricingGuardTests`: `ExternalPaidEvent_RequiresPricing`

### Implementation
- [ ] Implement `SetExternalPayment`, `SetPaymentMode`, `SyncLegacyIsFree`, RegisterWith* guards
- [ ] `dotnet test` → green; domain test count >= baseline + new

### Commit + push + deploy + smoke
- [ ] Commit: `feat(events): domain methods SetExternalPayment + SetPaymentMode + RegisterWith* guards (Phase 8X.3)`
- [ ] Push, wait for staging deploy
- [ ] Staging smoke: existing flows still work — `POST /api/Events` paid + `POST /api/Events/{id}/rsvp` → 200/201 (no regression)
- [ ] Doc updates

---

## Slice 8X.3.5 — Domain rules: ExternalPaid blocks add-ons, waitlist, check-in QR

### Tests (RED first)
- [ ] `Event_AddOnsBlockedForExternalPaid` — adding an `AddOnConfiguration` to an ExternalPaid event fails
- [ ] `Event_SetAsExternalPaid_FailsIfAddOnsAlreadyConfigured`
- [ ] `Event_WaitlistBlockedForExternalPaid` — adding to waitlist fails
- [ ] `Event_TicketGenerationBlockedForExternalPaid` — `GenerateTicket()` (or equivalent) returns Failure
- [ ] `Event_TicketTiersAllowedButReadOnlyForExternalPaid` — defining tiers OK, but `Reserve()` per tier fails (display-only)
- [ ] `Event_CheckInQrBlockedForExternalPaid` — explicit test

### Implementation + ship
- [ ] Add domain guards in add-on / waitlist / ticket-generation / check-in QR paths
- [ ] `dotnet test` → green
- [ ] Commit: `feat(events): block add-ons/waitlist/check-in for ExternalPaid (Phase 8X.3.5)`
- [ ] Push, deploy
- [ ] Staging regression smoke (existing add-on / waitlist flows on free + paid events still work)
- [ ] Doc updates

---

## Slice 8X.4a — Command shape + validator (handler still ignores new fields)

**Goal**: API contract live so FE can integrate. Validator enforces the rules; handler is a placeholder. **DO NOT smoke valid-ExternalPaid create as success in 4a — happy path C1 lives in 4b.**

### Files edited
- [ ] `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommand.cs` — add `EventPaymentMode? PaymentMode`, `string? ExternalRegistrationUrl`, `string? ExternalRegistrationInstructions`, `string? ExternalRegistrationVendorName`
- [ ] `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommand.cs` — same fields
- [ ] New `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandValidator.cs` (if doesn't exist; otherwise extend)
- [ ] New `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandValidator.cs`
- [ ] Validator inference table copied verbatim into `CreateEventCommandValidator.cs` as XML doc comment

### Tests (RED first)
- [ ] `CreateEventCommandValidator.ExternalPaid_MissingUrl_Fails`
- [ ] `CreateEventCommandValidator.ExternalPaid_HttpUrl_Fails`
- [ ] `CreateEventCommandValidator.ExternalPaid_LoopbackUrl_Fails_LogsWarningWithUserId`
- [ ] `CreateEventCommandValidator.ExternalPaid_RegMode_DetailedAttendees_Fails`
- [ ] `CreateEventCommandValidator.ExternalPaid_AssignedSeating_Fails`
- [ ] `CreateEventCommandValidator.ExternalPaid_NullPricing_Fails`
- [ ] `CreateEventCommandValidator.ExternalPaid_AddOnsConfigured_Fails`
- [ ] `CreateEventCommandValidator.IsFreeTrue_PaymentModeNull_InfersToFree`
- [ ] `CreateEventCommandValidator.IsFreeFalse_PaymentModeNull_InfersToOnPlatformPaid`
- [ ] `CreateEventCommandValidator.IsFreeNull_PaymentModeNull_InfersToOnPlatformPaid` (security default)
- [ ] `CreateEventCommandValidator.IsFreeTrue_PaymentModeOnPlatformPaid_Inconsistent_Fails`
- [ ] `CreateEventCommandValidator.IsFreeFalse_PaymentModeFree_Inconsistent_Fails`
- [ ] Same matrix for `UpdateEventCommandValidator`
- [ ] `UpdateEventCommandValidator.PaymentModeChange_WithActiveRegs_Fails`
- [ ] `UpdateEventCommandValidator.IdempotentSameExternalPaidPayload_Passes`

### Implementation + ship
- [ ] Validator code matching inference table
- [ ] Loopback/RFC1918/link-local rejection logs at warning level with `userId`
- [ ] `dotnet test` → green
- [ ] Commit: `feat(events): commands + validators accept paymentMode + external registration (Phase 8X.4a)`
- [ ] Push, deploy

### Staging API smoke (validator-only — 400-path cells ONLY)
- [ ] Get token
- [ ] `POST /api/Events` `paymentMode=ExternalPaid` + missing URL → 400 with field-level error mentioning "ExternalRegistrationUrl"
- [ ] `POST /api/Events` `paymentMode=ExternalPaid` + `externalRegistrationUrl=http://example.com` → 400 mentioning "https"
- [ ] `POST /api/Events` `paymentMode=ExternalPaid` + `externalRegistrationUrl=https://127.0.0.1` → 400; Azure log shows warning with userId
- [ ] `POST /api/Events` `paymentMode=ExternalPaid, registrationMode=DetailedAttendees` → 400
- [ ] `POST /api/Events` `isFree=true, paymentMode=ExternalPaid` → 400 inconsistent
- [ ] Azure container logs: no errors
- [ ] Doc updates

---

## Slice 8X.4b — Handler wiring + Stripe webhook defence + full API smoke matrix

**Goal**: ExternalPaid works end-to-end on the API. Defence-in-depth on Stripe webhook. Full 27-cell smoke.

### Files edited / created
- [ ] `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs` — when `PaymentMode == ExternalPaid`, call `Event.SetExternalPayment(externalReg, pricing)`
- [ ] `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs` — same logic on update
- [ ] `src/LankaConnect.API/Controllers/PaymentsController.cs` (or Stripe webhook handler) — early-return: if event is ExternalPaid, log warning + return 200 (Stripe retries on non-2xx)
- [ ] `scripts/stripe_synth_webhook.py` — Python helper signing synthetic Stripe payload with `STRIPE_WEBHOOK_SECRET_STAGING` env var (NEVER embed secret in repo)

### Tests (RED first)
- [ ] `CreateEventCommandHandler.ExternalPaidRequest_PersistsCorrectly` — `PaymentMode=ExternalPaid`, `ExternalRegistration` set, `RegistrationMode=NoRegistration`
- [ ] `CreateEventCommandHandler.LegacyIsFreeFalse_NoPaymentMode_PersistsAsOnPlatformPaid` (security default)
- [ ] `UpdateEventCommandHandler.ChangeToExternalPaid_NoActiveRegs_Succeeds`
- [ ] `UpdateEventCommandHandler.ChangeToExternalPaid_ActiveRegs_Fails400`
- [ ] `UpdateEventCommandHandler.IdempotentSameExternalPaidPayload_Succeeds_NoOp`
- [ ] `StripeWebhookHandler.ExternalPaidEvent_LogsWarningAndReturns200`
- [ ] `StripeWebhookHandler.ExternalPaidEvent_DoesNotInvokeStripeApi`

### Implementation
- [ ] Wire handler to `Event.SetExternalPayment`
- [ ] Add Stripe webhook defence
- [ ] Add structured logging at handler entry/exit per CLAUDE.md observability rule
- [ ] Try/catch around external-VO creation with logged context per CLAUDE.md observability rule
- [ ] `dotnet test` → green

### Commit + push + deploy
- [ ] Commit: `feat(events): handler wiring + Stripe webhook ExternalPaid defence (Phase 8X.4b)`
- [ ] Push, deploy

### Full API smoke matrix (27 cells — execute every cell, record HTTP status + DB row state where applicable)

#### Create / validation
- [ ] **C1** `POST /api/Events` ExternalPaid happy path → 201; verify DB: `payment_mode=2`, `external_registration_url` set, `registration_mode=5`
- [ ] **C2** ExternalPaid + missing URL → 400
- [ ] **C3** ExternalPaid + http URL → 400
- [ ] **C4** ExternalPaid + loopback URL → 400
- [ ] **C5** ExternalPaid + Mode A explicit → 400
- [ ] **C6** ExternalPaid + AssignedSeating → 400
- [ ] **C7** ExternalPaid + add-ons → 400
- [ ] **C8** legacy `isFree=true, paymentMode=null` → 201 with `payment_mode=0`
- [ ] **C9** legacy `isFree=false, paymentMode=null` → 201 with `payment_mode=1`
- [ ] **C10** `isFree=null, paymentMode=null` → 201 with `payment_mode=1` (security default)
- [ ] **C11** `isFree=true, paymentMode=ExternalPaid` → 400 inconsistent
- [ ] **C12** ExternalPaid + `instructions=<script>alert(1)</script>` → 201; DB stores raw text (XSS prevention is render-side)

#### Update / transitions
- [ ] **U1** `PUT /api/Events/{free-event-id}` switch to ExternalPaid (no regs) → 200; DB `payment_mode=2`
- [ ] **U2** `PUT /api/Events/{paid-event-with-regs-id}` switch to ExternalPaid → 400 with active-regs message
- [ ] **U3** `PUT /api/Events/{externalpaid-id}` change URL → 200; DB updated
- [ ] **U4** `PUT /api/Events/{externalpaid-id}` switch back to OnPlatformPaid → 200; DB shows `registration_mode=DetailedAttendees`, `external_registration_*` columns null
- [ ] **U5** `PUT /api/Events/{externalpaid-id}` send identical ExternalPaid payload twice → both 200, no error (idempotent)

#### Registration block
- [ ] **R1** `POST /api/Events/{externalpaid-id}/rsvp` → 400 with documented "external registration" message
- [ ] **R2** `POST /api/Events/{externalpaid-id}/register-anonymous` → 400
- [ ] **R3** `POST /api/Events/{externalpaid-id}/waitlist` → 400 (or 404 if endpoint doesn't exist; record actual)

#### Lifecycle on ExternalPaid
- [ ] **L1** `POST /api/Events/{externalpaid-id}/cancel` → 200 (no internal regs to notify; verify no crash on empty list)
- [ ] **L2** `POST /api/Events/{externalpaid-id}/postpone` → 200
- [ ] **L3** `GET /api/Events/{externalpaid-id}/registrations` → 200 with empty array (NOT 404, NOT 500)
- [ ] **L4** `POST /api/Events/{externalpaid-id}/duplicate` → 201 if endpoint exists; new event preserves `payment_mode=2` + ExternalRegistration fields. If endpoint absent, mark N/A with note.

#### Allowed features on ExternalPaid
- [ ] **A1** `POST /api/Events/{externalpaid-id}/signup-commitments` → 201 (signup lists work for all modes)
- [ ] **A2** `POST /api/Events/{externalpaid-id}/donations` → 201

#### Stripe defence
- [ ] **S1** Run `python scripts/stripe_synth_webhook.py --event-id {externalpaid-id}` → POSTs signed synthetic `checkout.session.completed`; expect 200 response, Azure log shows warning, no Registration row created, no outbound Stripe API call

### Observability
- [ ] Azure container logs after the matrix: only expected warnings (Stripe defence, loopback URL rejections), no unhandled exceptions, no 500s
- [ ] Doc updates

---

## Slice 8X.5 — Read DTO + projections (5 query handlers)

**Goal**: GET responses include `paymentMode` + external fields. `isFree` semantics UNCHANGED on the wire (stale FE bundles must keep working).

### Files edited
- [ ] `src/LankaConnect.Application/Events/Common/EventDto.cs` — add `EventPaymentMode PaymentMode { get; init; } = EventPaymentMode.Free;`, `string? ExternalRegistrationUrl`, `string? ExternalRegistrationInstructions`, `string? ExternalRegistrationVendorName`. Do NOT change `IsFree` to derived getter.
- [ ] `src/LankaConnect.Application/Events/Queries/GetEventById/GetEventByIdQueryHandler.cs`
- [ ] `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs`
- [ ] `src/LankaConnect.Application/Events/Queries/GetMyRegisteredEvents/GetMyRegisteredEventsQueryHandler.cs`
- [ ] `src/LankaConnect.Application/Events/Queries/GetUpcomingEventsForUser/GetUpcomingEventsForUserQueryHandler.cs`
- [ ] `my-rsvps` handler if separate (search and confirm)

### Tests (one per handler — Phase 6A.124 lesson, do not collapse)
- [ ] `GetEventByIdQueryHandlerTests.ExternalPaidEvent_DtoIncludesPaymentModeAndUrl`
- [ ] `GetEventByIdQueryHandlerTests.ExternalPaidEvent_IsFreeReturnsFalse` (stale-FE compat)
- [ ] `GetEventsQueryHandlerTests.ListIncludesPaymentMode`
- [ ] `GetMyRegisteredEventsQueryHandlerTests.ExternalPaidEventsExcluded` (no Registration row → no entry)
- [ ] `GetUpcomingEventsForUserQueryHandlerTests.ExternalPaidEventsIncluded` (visible in browse)
- [ ] `MyRsvpsQueryHandlerTests.ExternalPaidEventsExcluded`

### Commit + push + deploy
- [ ] Commit: `feat(events): EventDto exposes paymentMode + external registration fields (Phase 8X.5)`
- [ ] Push, deploy

### Smoke
- [ ] `GET /api/Events/{free-event-id}` → JSON includes `paymentMode: "Free"`, `isFree: true`, external fields null
- [ ] `GET /api/Events/{onplatformpaid-id}` → `paymentMode: "OnPlatformPaid"`, `isFree: false`
- [ ] `GET /api/Events/{externalpaid-id}` → `paymentMode: "ExternalPaid"`, `externalRegistrationUrl` set, `isFree: false`
- [ ] `GET /api/Events?pageSize=20` → all events have `paymentMode` field
- [ ] `GET /api/Events/my-rsvps` (test user) → ExternalPaid events absent (no Registration row)
- [ ] **Stale-FE compat**: open production-FE bundle (cache-busted) on ExternalPaid event → no crash; falls back to "paid event" rendering (acceptable degradation pre-8X.6)
- [ ] Doc updates

---

## Slice 8X.6 — Frontend types + EventEditForm 3-way radio

### Files edited
- [ ] `web/src/infrastructure/api/types/events.types.ts` — add `EventPaymentMode` string-valued enum (`Free='Free' | OnPlatformPaid='OnPlatformPaid' | ExternalPaid='ExternalPaid'`); add fields to `EventDto` / `CreateEventRequest` / `UpdateEventRequest`
- [ ] `web/src/presentation/components/features/events/EventEditForm.tsx` — replace `isFree` checkbox with 3-way `RadioGroup`; conditional "External registration details" card; force-disable RegistrationMode + SeatingMode pickers when ExternalPaid; on transition AWAY from ExternalPaid show non-blocking info toast "Registration mode reset to Detailed Attendees — adjust if needed before saving"

### Tests (Vitest + RTL)
- [ ] `EventEditForm.Phase8X.test.tsx`: switching radio to ExternalPaid reveals URL/vendor/instructions inputs
- [ ] URL field client-validates: required, https-only, max 2048
- [ ] When ExternalPaid: RegistrationMode picker disabled with "Forced for external payment" label
- [ ] When ExternalPaid: SeatingMode picker disabled
- [ ] Submit happy path: builds correct request payload (paymentMode + URL)
- [ ] Switching back from ExternalPaid → OnPlatformPaid preserves pricing inputs and shows reset-toast
- [ ] Switching back from ExternalPaid → Free clears pricing inputs

### Implementation
- [ ] FE typecheck + tests green: `npm run typecheck && npm run test`

### Local browser smoke
- [ ] `npm run dev`; create free event → still works
- [ ] Create OnPlatformPaid event → still works
- [ ] Create ExternalPaid event → submission succeeds
- [ ] Edit existing free → still works (no regression)

### Commit + push + deploy
- [ ] Commit: `feat(events): 3-way payment-mode radio + external registration card in EventEditForm (Phase 8X.6)`
- [ ] Push, wait for `deploy-ui-staging.yml` green

### Staging UI smoke
- [ ] Open staging FE; create ExternalPaid event end-to-end with real https URL → success; verify in DB
- [ ] Edit existing free event → switch to ExternalPaid → succeeds
- [ ] Edit existing OnPlatformPaid event with active regs → switching to ExternalPaid blocked with clear error toast
- [ ] Switch ExternalPaid event back to OnPlatformPaid → toast appears, RegistrationMode = DetailedAttendees, save succeeds
- [ ] Doc updates

---

## Slice 8X.7+8 (MERGED) — Detail page CTA + ExternalRegistrationCta + list card badge + newsletter + iCal

### Files edited / created
- [ ] New `web/src/presentation/components/features/events/ExternalRegistrationCta.tsx` — pricing summary, vendor name, CTA button, instructions block (whitespace-pre-wrap, plain text via `{text}` not `dangerouslySetInnerHTML`)
- [ ] CTA `<a>` uses `target="_blank" rel="noopener noreferrer nofollow"`
- [ ] Below button: small text "You'll leave LankaConnect to complete registration"
- [ ] `web/src/app/events/[id]/page.tsx` — extend CTA-label decision (line ~317), swap RsvpFormSection (line ~1078) for ExternalRegistrationCta when ExternalPaid, gate TicketSection (lines ~1873/2353)
- [ ] List card component (`EventCard.tsx` or similar) — add "External payment" badge when ExternalPaid
- [ ] Search-result card component (separate from list card — confirm in code search) — same badge
- [ ] Newsletter HTML rendering — add ExternalPaid branch with "Register externally" CTA linking to external URL directly
- [ ] iCal builder — for ExternalPaid events, set `URL:` field to LankaConnect detail page URL; `DESCRIPTION:` field includes external registration URL as plain text

### Tests
- [ ] `ExternalRegistrationCta.test.tsx`: renders pricing, vendor, CTA with correct rel attributes, instructions as plain text (XSS attack vector test: instructions = `<script>alert(1)</script>` rendered as literal text)
- [ ] `events/[id]/page.Phase8X.test.tsx`: CTA label matches; `RsvpFormSection` not rendered for ExternalPaid; `TicketSection` not rendered; pricing card visible
- [ ] `EventCard.Phase8X.test.tsx`: ExternalPaid card shows badge + pricing
- [ ] `SearchResultCard.Phase8X.test.tsx`: ExternalPaid result shows badge
- [ ] Newsletter renderer test: ExternalPaid event renders external CTA URL
- [ ] iCal builder test: ExternalPaid event → `URL:` is detail page; `DESCRIPTION:` contains external URL as plain text

### Local + staging deploy
- [ ] Local browser smoke first
- [ ] Commit: `feat(events): public render surfaces for ExternalPaid (Phase 8X.7+8)`
- [ ] Push, wait for FE staging deploy green

### Cross-surface smoke matrix (12 cells — execute every one)
- [ ] **M1** `/events/{free-id}` unauth → existing Register flow
- [ ] **M2** `/events/{onplatformpaid-id}` unauth → existing ticket flow
- [ ] **M3** `/events/{externalpaid-id}` unauth → ExternalRegistrationCta visible, no RsvpFormSection, no TicketSection, pricing card visible, link `target=_blank rel="noopener noreferrer nofollow"`
- [ ] **M4** `/events/{externalpaid-id}` authed user → identical to M3
- [ ] **M5** `/events/{externalpaid-id}` authed organiser → M3 + edit access visible
- [ ] **M6** `/events/{externalpaid-id}/manage` (organiser) → edit form preserves all external fields
- [ ] **M7** `/events` list page → ExternalPaid card shows pricing + "External payment" badge
- [ ] **M8** `/my-rsvps` (any user) → ExternalPaid events absent (by design)
- [ ] **M9** Newsletter HTML render preview for ExternalPaid event → "Register externally" CTA URL correct
- [ ] **M10** Organiser daily-digest email render for organiser whose only event is ExternalPaid → no template leakage, no empty-list crash
- [ ] **M11** `GET /api/Events/{externalpaid-id}/ical` → 200, iCal `URL:` = LankaConnect detail page, `DESCRIPTION:` contains external URL
- [ ] **M12** `/events?q=…` search results page matching ExternalPaid event → result card shows badge + pricing

### Operator UAT gate (memory rule 2026-05-04)
- [ ] Operator opens browser, walks M1–M12, signs off in this checklist with date + name
- [ ] Stripe pipeline NOT invoked for ExternalPaid: confirmed by inspecting Azure logs across the matrix run
- [ ] Operator confirms instructions field renders as plain text (paste `<b>bold</b>` via UI, see literal `<b>` characters in detail page)
- [ ] Doc updates

---

## Slice 8X.6.5 (optional) — `?paymentMode=` filter on list endpoint

- [ ] Add filter param to `GetEventsQuery`
- [ ] Validator + handler updated
- [ ] Smoke: `GET /api/Events?paymentMode=ExternalPaid` returns only ExternalPaid events
- [ ] Smoke: `GET /api/Events?paymentMode=BadValue` → 400
- [ ] Skippable if PM doesn't need it for v1

---

## Slice 8X.9 — Final sign-off

- [ ] All 12 matrix cells (M1–M12) signed off
- [ ] All 27 API smoke cells signed off
- [ ] Operator UAT signed off (date + name in master TODO)
- [ ] STREAMLINED_ACTION_PLAN.md updated: Phase 8X marked Shipped
- [ ] PROGRESS_TRACKER.md updated with all commit hashes + deploy run numbers
- [ ] PHASE_6A_MASTER_INDEX.md updated: status flipped to Complete with implementation date
- [ ] Domain test count vs baseline: confirm strictly greater than baseline (no accidental deletion)

---

## Slice 8X.10 (DEFERRED — Phase 8Y, do not block this release)

- [ ] Drop legacy `IsFreeEvent` DB column once all reports/exports migrated to read `payment_mode = 0`
- [ ] Production-grade chunked migration (off-peak window, lock-duration mitigation)
- [ ] AppInsights `external_cta_clicked` custom event for funnel analytics
- [ ] Domain allow-list / moderator review for external URLs (anti-phishing v2)

---

## Hard-stop conditions

If any of these occur during Phase 8X execution, STOP and consult architect:
- Migration applies but post-assertion `RAISE EXCEPTION` fires → backfill bug
- Migration `Up()` runs locally but `__EFMigrationsHistory` row absent on staging → Phase 6A.133 silent-skip pattern
- Domain test count drops below the slice baseline → accidental test deletion during refactor
- Any 500 in API smoke matrix → unhandled exception, fix before continuing
- FE typecheck fails after type changes → fix before next commit
- Stripe webhook synthetic test triggers actual outbound Stripe API call → defence-in-depth broken
- Any operator UAT cell fails → don't ship that slice; fix and re-run
- `scripts/stripe_synth_webhook.py` ever committed with embedded webhook secret → revert immediately, rotate staging webhook secret

End of Phase 8X master TODO.
