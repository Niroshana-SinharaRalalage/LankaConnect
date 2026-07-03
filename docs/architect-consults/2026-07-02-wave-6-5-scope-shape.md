# Architect Ruling — Wave 6.5 Scope Shape

**Date**: 2026-07-02
**Participants**: Founder (Niroshana), System Architect (Claude Opus 4.7 persona), Planning Agent (Claude Opus 4.7)
**Status**: BINDING — Wave 6.5 execution proceeds per the sub-slice enumeration below
**Related**: `ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` §7.4, §7.8, §7.16; ADR-005 (Outbox-Everything); ADR-006 (Repository-per-Aggregate); Wave 6.b baseline `Wave6_5TransitionalBaseline.json`; F30a commit `f0b684a0`

---

## 1. Context

Wave 6 SHIPPED + CLOSED 2026-07-01 with two irreversible artefacts: the 20-class Rule 12 baseline JSON (all `Products.LankaEvents.Infrastructure.Repositories.*` still on `AppDbContext` + `Repository<T>`) and the composite Rule 13 escape-hatch gate (only decorated classes may touch the two legacy dependencies). Two Skip-fact rules remain: Rule 5 (14 legacy `LankaConnect.Infrastructure` types referencing `Products.LankaEvents.Application`) and Rule 9b (11 `Payments.Application` types with the same violation). Wave 6.5 owns the un-skip of both. Master-plan tabulation calls it "14 + 11 = 25" — the baseline JSON is a distinct 20 entries; these are three overlapping-but-distinct debt buckets.

Wave 9.h.10.6 finding F30a (commit `f0b684a0`, 2026-07-02) elevated Wave 6.5 from a pure architectural cleanup into a production-data-integrity operation. Seven PhotoAlbum mutating command handlers were silently data-losing in production because `IUnitOfWork.CommitAsync()` only saves `AppDbContext` while `PhotoAlbum` is owned by `MediaDbContext`. The workaround (call `repo.UpdateAsync(album, ct)` before `_unitOfWork.CommitAsync(ct)`, since `UpdateAsync` self-saves the module context) unblocks the smoke suite but leaves domain events raised on `PhotoAlbum` orphaned — they never dispatch because the module context has no event-dispatch override. Wave 6.5 must retire the self-saving pattern AND close the domain-event dispatch gap in one coherent motion, else F30a will re-appear in a different shape.

Sub-scope enumeration below reflects (a) that the outbox mechanism is 80 % shipped at BuildingBlocks (`OutboxProcessor<TDbContext>`, `OutboxIntegrationEventDispatcher<TDbContext>`, `MediatRIntegrationEventDispatcher`, `IntegrationEventBase`, `IIntegrationEventV1` all exist and compile) but 0 % exercised end-to-end (only one integration event `NotificationCreatedIntegrationEventV1` is declared, no module has registered a producer or consumer, no OutboxProcessor is DI-registered per module), and (b) that the four self-saving repos (Notifications, Media, Forms×2) and the 20-class Products.LankaEvents baseline are structurally different problems with different acceptance criteria.

---

## 2. Sub-slice enumeration

Wave 6.5 decomposes into eight sub-slices sequenced across three tracks. Track A ships the mechanism; Track B migrates consumers; Track C retires the baseline. Every sub-slice is commit-sized, each has a matching baseline-JSON diff where applicable, and each ships behind the existing pre-push T-trigger / S-class discipline.

### 6.5.a — Multi-context UnitOfWork extension + integration-event dispatch primitive

**Scope**: Extend `IUnitOfWork` so `CommitAsync` accepts an optional set of module `DbContext` references and drives `SaveChangesAsync` across all of them inside a single Postgres transaction (SET LOCAL transaction; `TransactionScope` explicitly rejected per ADR-005). Add `IIntegrationEventOutbox<TDbContext>` façade in `BuildingBlocks.Application` so handlers can enqueue without depending on the concrete dispatcher. Ship an `OutboxRegistrationExtensions.AddModuleOutbox<TDbContext>()` DI extension.

**Acceptance criteria**:
- `IUnitOfWork.CommitAsync(CancellationToken)` behaviour unchanged for callers that don't inject module contexts (source-compatible)
- New overload `CommitAsync(params DbContext[] moduleContexts, CancellationToken)` opens ONE transaction on `AppDbContext.Database`, enrolls each module context via `context.Database.UseTransaction(existingTransaction)`, calls `SaveChangesAsync` on each in order, dispatches domain events, then commits
- `IIntegrationEventOutbox<TDbContext>.EnqueueAsync(IntegrationEventBase e)` writes to the module context via the existing `OutboxIntegrationEventDispatcher<TDbContext>` (staging only — no SaveChanges)
- `AddModuleOutbox<TDbContext>()` registers `OutboxProcessor<TDbContext>` as `IHostedService` + `OutboxIntegrationEventDispatcher<TDbContext>` as scoped
- One unit test with two `SqliteInMemory` DbContexts proves atomic rollback: throw between context #1 SaveChanges and context #2 SaveChanges → both roll back
- No baseline JSON change (mechanism only)

**T-triggers fired**: T1 (new public method on `IUnitOfWork`), T3 (implicit — new pipeline behavior for domain-event dispatch), T6 (new DI extension)
**S-class**: S1 (read-only unit tests + build assertion; nothing exercised on staging yet)
**Dependencies**: none — this is the mechanism the rest of Wave 6.5 pulls from
**Estimated sessions**: one-session

---

### 6.5.b — PhotoAlbum canary migration (Media module)

**Scope**: Wire `MediaDbContext` into `IUnitOfWork.CommitAsync(mediaContext, ct)` at the seven F30a handler call-sites; delete self-save inside `PhotoAlbumRepository.AddAsync/UpdateAsync/DeleteAsync`; author `PhotoAlbumPublishedIntegrationEventV1` + `PhotoUploadedToAlbumIntegrationEventV1` in `Media.Contracts` (new sealed records, NOT retrofit of MediatR `IDomainEvent` types — see Q4 ruling); register `OutboxProcessor<MediaDbContext>` in `MediaModule.AddMediaModule()`.

**Acceptance criteria**:
- `PhotoAlbumRepository` no longer calls `_context.SaveChangesAsync` from any of AddAsync/UpdateAsync/DeleteAsync — repository stages via `_dbSet.Add/Update/Remove` only
- Class-remark XML doc in `PhotoAlbumRepository` deleted (comment lines 17-23) — the self-save-because-CommitAsync-only-saves-AppDbContext rationale is no longer true and must not persist as fossil documentation
- All seven F30a-touched handlers now inject `MediaDbContext` and call `_unitOfWork.CommitAsync(mediaContext, ct)` instead of `_photoAlbumRepository.UpdateAsync(album, ct); _unitOfWork.CommitAsync(ct)`. F30a workaround is fully retired.
- New `PhotoAlbumPublishedIntegrationEventV1` sealed record in `Media.Contracts` (with `AlbumId`, `EventId`, `EventTitle`, `AlbumName`, `PublishedByUserId` fields — CLR primitives only, no `Media.Domain` types)
- `PublishPhotoAlbumCommandHandler` calls `IIntegrationEventOutbox<MediaDbContext>.EnqueueAsync(new PhotoAlbumPublishedIntegrationEventV1 { ... })` BEFORE `_unitOfWork.CommitAsync(mediaContext, ct)` so the outbox row is atomic with the state change
- One consumer wired: relocate the existing `PhotoAlbumPublishedDomainEventHandler` (Product-internal — sends the notification email) to subscribe to `PhotoAlbumPublishedIntegrationEventV1` instead of the domain event via MediatR at the outbox dispatcher boundary
- Smoke: `Smoke-PhotoAlbumsController.ps1` Pass 3 continues to prove `template-photo-album-published` fires; assertion added: `POST /api/events/{eventId}/albums/{albumId}/publish` → 200; `GET /api/media/outbox/latest` returns at least one row with `EventType` matching the V1 record's `AssemblyQualifiedName` (new admin-only diagnostic endpoint, per §7.11 outbox-observability blueprint call-out)
- No baseline JSON change (Media is not in the baseline; this migrates the FOUR-self-saving-repo debt, not the 20-Products-repo debt)

**T-triggers**: T2 (mutator refactor touching domain events), T3 (7 command handlers changed), T6 (Module registration), T7 (namespace additions in Media.Contracts)
**S-class**: S2 (mutator refactor — full lifecycle: create → upload photo → publish → notify → GET photos assert non-empty → dead-letter table empty)
**Dependencies**: 6.5.a MUST ship first (needs the CommitAsync overload + OutboxProcessor DI extension)
**Estimated sessions**: one-session (large but concentrated — 7 handlers + repo + 2 events + DI + smoke assertion)

---

### 6.5.c — Notifications self-save retirement

**Scope**: Structural twin of 6.5.b for `NotificationRepository`. `NotificationRepository.AddAsync/Update` stop calling `_context.SaveChangesAsync`; all consumers (there are fewer — Notifications is a leaf-most module) migrate to `_unitOfWork.CommitAsync(notificationsContext, ct)`. Retrofit `NotificationCreatedIntegrationEventV1` to be produced through the new outbox path (currently declared but not emitted).

**Acceptance criteria**:
- Same self-save deletion + XML-doc cleanup as 6.5.b applied to `NotificationRepository`
- Every caller of `INotificationDispatcher.DispatchAsync` now flows through `IIntegrationEventOutbox<NotificationsDbContext>` — the existing `NotificationCreatedIntegrationEventV1` becomes the wire format
- One consumer subscribing (chosen for evidence): the LankaEvents feature that reads unread-count on dashboard gets its cache invalidation moved onto the outbox path
- `OutboxProcessor<NotificationsDbContext>` registered in `NotificationsModule`
- Smoke: `Smoke-NotificationsController.ps1` proves notification-create round-trips via outbox — new assertion asserts `_dispatched_at` field is populated within 10 s poll window

**T-triggers**: T2, T3, T6
**S-class**: S2
**Dependencies**: 6.5.a; can ship parallel with 6.5.b (independent DbContext)
**Estimated sessions**: half-session

---

### 6.5.d — Forms self-save retirement (Form + FormResponse)

**Scope**: Same pattern applied to both `FormRepository` and `FormResponseRepository`. Combined because the two repos share `FormsDbContext` and their consumers are always paired (a Form and its Responses are always co-mutated).

**Acceptance criteria**:
- Both repositories' AddAsync/UpdateAsync/DeleteAsync stripped of self-saves; XML-doc rationale deleted
- `FormPublishedIntegrationEventV1` and `FormResponseSubmittedIntegrationEventV1` authored in `Forms.Contracts`
- All Forms command handlers migrated to `_unitOfWork.CommitAsync(formsContext, ct)`
- Smoke: `Smoke-FormsController.ps1` (Wave 9.d gap) — publish form + submit response + assert outbox row per event

**T-triggers**: T2, T3, T6
**S-class**: S2
**Dependencies**: 6.5.a; can ship parallel with 6.5.b and 6.5.c
**Estimated sessions**: half-session

---

### 6.5.e — LankaEventsDbContext extraction + EF Configurations relocation

**Scope**: The prerequisite the master plan §7.16 flagged: create `LankaEventsDbContext` under `Products/LankaEvents/LankaEvents.Infrastructure/Data/` mapping every entity currently in `AppDbContext` that belongs to the LankaEvents aggregate family (`Event`, `Registration`, `EventPass`, `TicketTier`, `Sponsor`, `SponsorshipPackage`, `Donation`, `Collection`, `AddOnDefinition`, `AddOnPurchase`, `RegistrationPayment`, `RegistrationAddition`, `Ticket`, `TicketScanLog`, `EventNotificationHistory`, `EventReminder`, `VenueLayout`, `SeatHold`, `SeatReservation`, `MetroArea`, `EventAnalytics`, `EventViewRecord`). Relocate EF Configurations from `LankaConnect.Infrastructure/Data/Configurations/` to `Products/LankaEvents.Infrastructure/Configurations/`. Ship the DbContext, register it in DI, but do NOT yet flip any repository to it — that's 6.5.f.

**Acceptance criteria**:
- New `LankaEventsDbContext.cs` compiles + has `HasDefaultSchema("events")` + owns operational tables (`events.outbox`, `events.outbox_dead_letter`, `events.idempotency_keys`)
- All 22+ `IEntityTypeConfiguration<T>` classes for Event-family entities physically live in `Products/LankaEvents/LankaEvents.Infrastructure/Configurations/`
- `AppDbContext` still maps every one of those entities (dual mapping is intentional and temporary — see hard-STOP #3 below); one EF migration ships that adds `events.outbox` / `events.outbox_dead_letter` / `events.idempotency_keys` if not already present via prior module migrations
- Snapshot drift baseline in `SnapshotDriftRules.cs` extended to permit the dual mapping without failing
- No baseline JSON change yet — the 20 repos still use AppDbContext

**T-triggers**: T4 (many EF configs moved — configs migrate but semantics preserved), T6 (new DbContext DI registration), T7 (namespace moves), T8 (one operational-table migration)
**S-class**: S3 (EF config move) + S5 (schema migration) + S6 (module DbContext touch) — full matrix; requires a staging DbContext apply
**Dependencies**: 6.5.a (needs the outbox DI extension for the new context's operational tables); does NOT depend on 6.5.b/c/d
**Estimated sessions**: two-session (config move is mechanical but voluminous; migration + snapshot syncing is the operator-UAT surface)

---

### 6.5.f — Products.LankaEvents repository cutover (baseline shrinkage: 20 → 0)

**Scope**: Migrate all 20 repositories in the Rule 12 baseline off `AppDbContext` + `Repository<T>` onto `LankaEventsDbContext` + a hand-rolled Products-local repository base (`ProductRepositoryBase<TAggregate, TId>` in `Products/LankaEvents/LankaEvents.Infrastructure/Common/`). Each PR retires 1-3 repositories from the baseline JSON. Cutover order per README-BASELINE.md: leaf reads first, sub-aggregate writes second, payment-cluster writes third, aggregate roots (Registration, Event) last.

**Acceptance criteria** (per repo):
- Constructor takes `LankaEventsDbContext` instead of `AppDbContext`
- `[Wave6_5TransitionalException(...)]` attribute removed
- `using LankaConnect.Infrastructure.Data;` + `.Data.Repositories;` directives removed
- The class FQCN removed from `Wave6_5TransitionalBaseline.json` — SAME COMMIT
- All existing tests for the repo still pass (`dotnet test --filter FullyQualifiedName~<Repo>` green)
- Smoke assertion tied to the repo's owning controller still green (e.g., cutting over `SponsorRepository` → `Smoke-SponsorsController.ps1` full-suite green)

**Acceptance criteria** (aggregate — after all 20 migrated):
- Rule 12 baseline reduces from 20 → 0 (JSON becomes `[]`)
- ArchTest `Rule12_Wave6_5TransitionalException_BaselineNotExpanded` continues to pass
- Rule 13 continues to pass (no non-baseline classes reference AppDbContext/Repository<T> in Products.Infrastructure)
- The `[Wave6_5TransitionalException]` attribute type itself can now be marked `[Obsolete("Wave 6.5 complete; retained as breadcrumb for any Phase B module extraction repeat")]` — retention over deletion because Phase B will reuse the mechanism per §7.14

**T-triggers**: T4 (repo→context wiring is EF-adjacent), T6 (DI registrations flip context type per repo)
**S-class**: S2 for every touched controller (mutator smoke); S6 (module DbContext touch on every repo)
**Dependencies**: 6.5.e (needs `LankaEventsDbContext` to exist)
**Estimated sessions**: three-to-four-session (parallelizable per repo cluster; each cluster is a session)

**Sub-slice sequencing within 6.5.f** (per README-BASELINE.md):
- **6.5.f.1** — Leaf reads (MetroArea, EventAnalytics, EventViewRecord) — three repos, one commit
- **6.5.f.2** — Sub-aggregate writes cluster A (AddOnDefinition, Sponsor, SponsorshipPackage, Donation, Collection) — five repos, one commit
- **6.5.f.3** — Sub-aggregate writes cluster B (VenueLayout, SeatHold, SeatReservation, TicketScanLog, EventNotificationHistory, EventReminder) — six repos, one commit
- **6.5.f.4** — Payment-cluster writes (AddOnPurchase, RegistrationPayment, RegistrationAddition, Ticket) — four repos, one commit (touches Wave 6.5.g Rule 9b integration events; order matters — do 6.5.g first if possible)
- **6.5.f.5** — Aggregate roots (Registration, Event) — two repos, one commit; last, riskiest

---

### 6.5.g — Rule 9b un-skip (Payments.Application 11 handlers → integration events)

**Scope**: Retire the 11 `Payments.Application` handlers/services that directly reference `Products.LankaEvents.Application`. Replace direct references with new `Products.LankaEvents.Contracts` integration events: `RegistrationConfirmedIntegrationEventV1`, `PaymentCompletedIntegrationEventV1`, `RefundApprovedIntegrationEventV1`, `RefundCompletedIntegrationEventV1`. Consumers move to Payments-owned handlers subscribing to these V1 records. Un-skip `Rule9b_PaymentsApplication_DoesNotReferenceProducts_LankaEvents_Internals`.

**Acceptance criteria**:
- Zero `using LankaConnect.Products.LankaEvents.Application*;` directives remain in any of the 12 files (`AddOnRefundService`, `RefundExecutionService`, `RefundLineDispatcher`, `RefundReconciliationService`, `RefundTotalCalculator`, `RegistrationRefundService`, `PaymentCompletedEventHandler`, `RefundCompletedEventHandler`, `RegistrationPendingPaymentEventHandler`, `ApproveRefundRequestCommandHandler`, `CreateOrganizerInitiatedRefundCommandHandler`, `RefundCompletedWhatsAppHandler` — planning agent noted "11" in Skip-fact; actual grep hit is 12; execute against the grep count, not the doc count)
- Where Payments genuinely needs Event/Registration data (e.g., `PaymentCompletedEventHandler` reads `IEventRepository`, `IRegistrationRepository`), the DEPENDENCY inverts: Products.LankaEvents becomes a subscriber that publishes a `PaymentReceiptData` payload as part of the outbound integration event, so Payments receives everything it needs in the event payload rather than reaching across
- `IFormQueries` (currently referenced in `PaymentCompletedEventHandler`) already lives in `Forms.Contracts` per Wave 6.a.1 — no additional work
- Rule 9b `[Fact]` un-skipped and passing green
- Un-skip visible in commit diff (removes `Skip = "..."` param)
- Smoke: full `Run-Wave9.ps1` green

**T-triggers**: T2 (mutator refactor via subscription), T3 (11-12 handlers rewired), T7 (namespace changes)
**S-class**: S2 (mutator — payment flow smoke: register → pay → confirm → refund; all four V1 events fire)
**Dependencies**: 6.5.a (outbox mechanism); 6.5.b consumer-migration pattern serves as reference implementation
**Estimated sessions**: two-session (11-12 files is large; payment cluster is highest-risk surface — Stripe webhooks, Wave 4.4 territory)

---

### 6.5.h — Rule 5 un-skip (legacy LankaConnect.Infrastructure 14 services → integration events)

**Scope**: The 14 legacy service/handler/background-service types that directly reference `Products.LankaEvents.Application` (`RegistrationEmailService`, `PdfTicketService`, `TicketService`, `CsvExportService`, `ExcelExportService`, 7 `*WebhookHandler` classes, `RefundReconciliationBackgroundService`, `SeatHoldCleanupService`). Structural twin of 6.5.g — every violator either becomes a subscriber to a Products.LankaEvents.Contracts integration event, or (for services genuinely called from the outer host) moves into a Product-owned assembly. `DependencyInjection.cs` remains permanently allowed per Rule 5's composition-root exclusion.

**Acceptance criteria**:
- Zero `using LankaConnect.Products.LankaEvents.Application*;` directives in the 14 legacy files (excluding `DependencyInjection.cs`)
- Rule 5 `[Fact]` un-skipped and passing green
- Any Product-specific service that structurally belongs in a Product assembly (e.g., `TicketService` handles LankaEvents ticket generation exclusively) physically relocates to `Products/LankaEvents/LankaEvents.Infrastructure/Services/` with the corresponding interface promoted to `Products/LankaEvents/LankaEvents.Contracts/` — this is the "outbox is not the only tool" observation (see Q1 ruling)
- Every webhook handler subscribes to an integration event; the webhook endpoint stays in `LankaConnect.API` (composition-root), publishes a `StripeWebhookReceivedIntegrationEventV1`, and Products.LankaEvents.Application subscribes
- Smoke: full `Run-Wave9.ps1` green + Stripe webhook e2e smoke green

**T-triggers**: T2, T3, T6 (DI moves), T7 (namespace moves)
**S-class**: S2 for the Stripe webhook surface (S2 with idempotency); S4 (new endpoints if any relocate)
**Dependencies**: 6.5.a; 6.5.g provides the payment-cluster reference implementation
**Estimated sessions**: two-session

---

## 3. Sequenced dependency graph

```
                     6.5.a  (mechanism: multi-context UoW + outbox DI)
                        │
        ┌───────────────┼───────────────┬─────────────────┐
        ▼               ▼               ▼                 ▼
      6.5.b           6.5.c           6.5.d             6.5.e
     (Media           (Notif          (Forms          (LankaEvents
     canary)          leaf)           twin)            DbContext
        │               │               │              extraction)
        │               │               │                 │
        └───── parallel ──── shipping ───┘                 ▼
                                                        6.5.f.1  (leaf reads)
                                                           │
                                                        6.5.f.2  (sub-agg A)
                                                           │
                                                        6.5.f.3  (sub-agg B)
                                                           │
                                                        6.5.g   (Rule 9b — sequence BEFORE 6.5.f.4)
                                                           │
                                                        6.5.f.4  (payment-cluster)
                                                           │
                                                        6.5.f.5  (agg roots)
                                                           │
                                                        6.5.h   (Rule 5)
                                                           │
                                                        Wave 6.5 CLOSE
```

Critical path: 6.5.a → 6.5.e → 6.5.f.1-5 (linear because each repo cutover depends on the same LankaEventsDbContext). Track 1 (6.5.b/c/d) can ship in parallel with 6.5.e once 6.5.a lands.

6.5.g intentionally sequences BEFORE 6.5.f.4 because the payment-cluster repositories touch Registration payment state, and un-skipping Rule 9b defines the V1 integration events that 6.5.f.4's handlers publish. Doing 6.5.f.4 first would force temporary domain-event shims that 6.5.g then deletes — wasted work.

---

## 4. Rulings on Q1–Q8

### Q1 — Sub-slice sequencing (mechanism-first vs debt-migration-first)

**Verdict**: Mechanism-first. Ship 6.5.a before any consumer migration. Non-negotiable.

**Reasoning**: The outbox infrastructure is 80 % built (`OutboxProcessor<TDbContext>`, `OutboxIntegrationEventDispatcher<TDbContext>`, `MediatRIntegrationEventDispatcher`, `IntegrationEventBase`, `IIntegrationEventV1` all compile) but has ZERO exercised producer/consumer pairs. `NotificationCreatedIntegrationEventV1` exists in `Notifications.Contracts` — nothing publishes it, nothing subscribes to it. Attempting a debt-migration-first path forces every migration to invent multi-context UoW handling ad-hoc — that's how F30a happened (self-save was the ad-hoc invention).

**How to apply**: 6.5.a is a single-session mechanism commit. It ships one unit test (two SqliteInMemory contexts, throw between saves, assert rollback) and the DI extension. NO consumer migration in 6.5.a. Do NOT bundle it with 6.5.b even though 6.5.b needs it — separate commits let the mechanism be tested in isolation and let a rollback of 6.5.b not blow away the mechanism other slices depend on.

---

### Q2 — Per-repository cutover ordering within 6.5.a-d

**Verdict**: PhotoAlbum (6.5.b) ships first as the canary. Notifications (6.5.c) and Forms (6.5.d) parallel-migrate on top.

**Reasoning**: F30a is proven production data-loss, live-verified via staging probes. It is the highest-impact test surface — if 6.5.a's mechanism doesn't correctly retire self-save + wire outbox atomically, the F30a symptom returns immediately and is visible in `Smoke-PhotoAlbumsController.ps1 Pass 3`. This is a rollback-proof canary because Wave 9.h.10.6 has already established the smoke assertion. Notifications (single aggregate, leaf-most, lowest coupling) is second because a failure there rolls back cheaply. Forms (dual repository, session-scoped mutations, no user-facing regression path) is third — highest complexity but lowest visibility if it breaks.

**How to apply**: Every 6.5.b/c/d migration PR follows the same 5-step template: (1) inject module DbContext into command handler, (2) delete `_context.SaveChangesAsync` from repo's Add/Update/Delete, (3) delete the class-remark XML doc that documents the self-save rationale, (4) author the V1 integration event in the module's `.Contracts` assembly + wire producer via `IIntegrationEventOutbox<TDbContext>.EnqueueAsync`, (5) update the module's smoke script to assert an outbox row after the mutation. Cross-schema dependencies are irrelevant here because these three modules don't have cross-schema FKs to each other — they share only `identity.users.id` which is read-only from their perspective.

---

### Q3 — DbContext extraction inside Wave 6.5?

**Verdict**: YES — `LankaEventsDbContext` extraction is IN scope as sub-slice 6.5.e. Non-negotiable.

**Reasoning**: The 20-class Rule 12 baseline cannot shrink without a LankaEvents-owned DbContext because Rule 13 explicitly forbids `AppDbContext` in `Products.LankaEvents.Infrastructure` except for the transitional set. Retiring the transitional set means migrating to something — and per `MODULE_EXTRACTION_PLAYBOOK.md` + master plan §7.16, that "something" is a per-module DbContext. Banking DbContext extraction as a separate Wave 6.5.X.Z creates a circular blocker: Wave 6.5 nominal cannot close (baseline stays at 20), and Wave 7 (Frontend mirror) blocks on Wave 6.5 per master-plan sequence. That's an unshippable chain.

**How to apply**: 6.5.e ships the DbContext + Configurations relocation + operational-tables migration as one two-session commit. The dual-mapping (AppDbContext still knows about every LankaEvents entity for the duration of 6.5.f cutover) is intentional and temporary — it's the only way to migrate repositories one cluster at a time without breaking every query. The dual-mapping is deleted in a post-6.5.f.5 cleanup commit (call it 6.5.f.6): remove `modelBuilder.Entity<T>()` calls for LankaEvents-family entities from `AppDbContext.OnModelCreating`, regenerate snapshot, verify no snapshot drift. Master plan's "3 weeks" estimate holds: 6.5.a (1s) + 6.5.b (1s) + 6.5.c (0.5s) + 6.5.d (0.5s) + 6.5.e (2s) + 6.5.f (3-4s) + 6.5.g (2s) + 6.5.h (2s) = 12-13 sessions ≈ 2-3 weeks at founder-pace, matching the master-plan estimate exactly.

---

### Q4 — Integration event contract shape

**Verdict**: NEW sealed `*IntegrationEventV1` records in each module's `.Contracts` assembly. Do NOT retrofit MediatR `IDomainEvent` types to implement `IIntegrationEventV1`. Non-negotiable.

**Reasoning**: ADR-005 §"Integration event versioning" mandates sealed records named `*IntegrationEventV1`. Retrofit of existing MediatR types would leak module internals (domain events like `PhotoAlbumPublishedDomainEvent` live in `Media.Domain` which is NOT referenceable across modules per §7.4). More importantly, the semantic layers are different: `IDomainEvent` fires WITHIN the module's aggregate transaction; `IIntegrationEventV1` fires AFTER the transaction commits, deserialized from JSON on the subscriber side. Merging them collapses D10 (blueprint §2.D10 — Domain Event vs Integration Event boundary), which is a hard rule. The 6.5.b canary confirms the pattern via `PhotoAlbumPublishedIntegrationEventV1` (parallel to the existing `PhotoAlbumPublishedDomainEvent`, mapped in the handler).

**How to apply**: For every migrated module, the handler pattern becomes: (1) call aggregate method → domain event enqueued on aggregate, (2) map domain event → integration event V1 record inline in the handler, (3) `IIntegrationEventOutbox<TContext>.EnqueueAsync(v1Event)`, (4) `_unitOfWork.CommitAsync(moduleContext, ct)`. Domain events STAY intra-module (the interceptor still dispatches them on the module context's SaveChanges via a new module-level dispatcher, added as part of 6.5.a — inline this as an acceptance criterion: the multi-context CommitAsync overload dispatches domain events across ALL enrolled contexts, not just AppDbContext). The V1 record fields are CLR primitives + Contracts-local enums only (no `Media.Domain` types) — this is the rule that makes future microservice extraction free.

---

### Q5 — Baseline gate migration during cutover

**Verdict**: Incremental. Every PR that removes an `[Wave6_5TransitionalException]` attribute decoration MUST edit `Wave6_5TransitionalBaseline.json` in the SAME COMMIT. Non-negotiable.

**Reasoning**: Rule 12 gates the SIZE of the transitional set. The whole point of the baseline JSON is that it forces friction at the exact moment a class is added or removed — that's the design of the "atomic change, single review" clause in `README-BASELINE.md`. Batching PR-level baseline drops defeats this: two contributors could concurrently remove one class each, the JSON conflicts, and the resolver either merges the conflict wrong (baseline still has 20 while attribute count says 18) or manually reconciles (defeating the automation). Incremental edits enforce "the PR changing behaviour also changes the baseline" — that's the review invariant.

**How to apply**: Every 6.5.f.N sub-slice commit removes N-to-3 lines from the JSON (matching the repos it cutovers) plus the corresponding attribute removals from the .cs files. Merge conflicts on the JSON become a positive signal — they mean two people worked in this territory in parallel, and Rule 12 catches the resulting drift before it lands. Friction is the feature.

---

### Q6 — F30a workaround-vs-fix trade-off

**Verdict**: F30a's `repo.UpdateAsync(album, ct); _unitOfWork.CommitAsync(ct)` pattern MUST be fully retired in 6.5.b. The workaround is ephemeral by construction. Non-negotiable.

**Reasoning**: F30a's fix is data-loss-preventing but architecturally worse than the pre-Wave 4.2 state: it makes the handler take on transactional-coordination responsibility that belongs in `IUnitOfWork`. If 6.5.b were to preserve `repo.UpdateAsync(album, ct)` and only replace the dispatch path, the semantic is: "the mutation still self-saves, but now we also enqueue an outbox row." That's TWO SaveChangesAsync calls — the mutation commits first, then the outbox row commits second, and a crash between them loses the outbox row while keeping the state change. That's the exact anti-pattern ADR-005 was created to prevent.

**How to apply**: 6.5.b's acceptance criteria explicitly delete `repo.UpdateAsync(album, ct)` from all 7 F30a handlers AND delete the self-save inside the repo methods. The new pattern in every handler becomes: `album.MutateInDomain(...); await _integrationEventOutbox.EnqueueAsync(new XxxIntegrationEventV1(...)); await _unitOfWork.CommitAsync(_mediaContext, ct);` — one save, one transaction, atomic state + outbox row. The reviewer's checklist for 6.5.b PR: (a) `repo.UpdateAsync` calls in handlers removed, (b) `_context.SaveChangesAsync` calls in repo removed, (c) class-remark XML doc citing "IUnitOfWork.CommitAsync still only saves AppDbContext" removed. If any of the three remain, the PR is not ready.

---

### Q7 — Testing discipline overlay per commit

**Verdict**: Wave 6.5 sub-slice T-trigger profile is **T2 + T3 + T6** (± T4 for 6.5.e's config move, ± T7 for namespace changes, ± T8 for 6.5.e's operational-tables migration). S-class is **S2 for every consumer migration; S6 for every DbContext touch; S3 for 6.5.e config move; S5 for 6.5.e migration**.

**Reasoning**: The planning agent's read is correct: every migration touches domain-event mutators (T2), rewires command/query handlers (T3), and changes DI registration for the outbox processor / module context (T6). T5 (endpoint signature) does NOT fire because HTTP surface is unchanged — this is Wave 6.5's superpower, the entire refactor is behavior-preserving externally. That's why "convert existing tested behaviors" is the right framing.

**How to apply**: Every 6.5 commit message includes:
- `T-triggers: T2, T3, T6` (± T4/T7/T8 per slice)
- `S-class: S2` (± S3/S5/S6 per slice)
- Wave 9 smoke assertion listed by name (e.g., `Smoke-PhotoAlbumsController.ps1 Pass 3 asserts outbox row post-publish`)

The pre-push hook already enforces the annotation. What Wave 6.5 adds beyond the baseline: for every 6.5.b/c/d/g/h commit, `Run-Wave9.ps1` must be run and its 182 PASS / 0 FAIL baseline preserved. Any regression means the outbox path lost a producer-to-consumer link — halt and reconsult (hard-STOP #2 below).

---

### Q8 — Wave 7.X.R sequencing dependency

**Verdict**: Wave 7.X.R (EF shadow-metadata Roslyn analyzer) is a POST-6.5 deliverable. Wave 6.5 execution MUST NOT introduce any new EF shadow-metadata references (baseline stays at 3 instances: F17, F18, F21; F20-shape stays at 1). This is a temporal sequencing rule AND a compositional rule.

**Reasoning**: The 6.5.e DbContext extraction is EXACTLY the surface where EF-shadow-metadata misuse tends to reappear — new configurations, new junction tables, new value-object mappings, new column overrides. If Wave 6.5 introduces a fourth F17-family instance, the analyzer authoring becomes more expensive (more distinct shapes to detect) AND the codebase temporarily carries the new bug into production. Both are avoidable. Sequence discipline: Wave 6.5 respects the current shape count so Wave 7.X.R can author against a stable target.

**How to apply**: 6.5.e's PR review checklist adds: (a) grep for `EF.Property<T>(entity, "...")` in the new configurations — must be zero, use CLR properties; (b) grep for `Set<Dictionary<string,object>>` in new code — must be zero, use typed junction entities; (c) grep for `.IsRequired()` chained on `Nullable<T>` domain properties — must be zero. If a shape needs to appear (rare — sometimes required for value-object migrations), it counts as a fourth instance and TRIGGERS Wave 7.X.R immediately at that PR, blocking Wave 6.5 close until the analyzer ships. Practical: this trigger is highly unlikely to fire because 6.5.e is a config MOVE, not a config CHANGE — the existing configs already avoid the shape.

---

## 5. Hard-STOP triggers

The executing agent MUST halt and re-consult with the architect BEFORE proceeding if ANY of the following conditions surface:

1. **Fifth self-saving repo discovered**. If grep on `_context.SaveChangesAsync` inside a `Modules/*/Infrastructure/Repositories/*.cs` file returns any hit outside the four known (`NotificationRepository`, `PhotoAlbumRepository`, `FormRepository`, `FormResponseRepository`), Wave 6.5 scope has been under-specified. Halt 6.5.a-d and re-consult on the fifth repo's ownership and cutover cluster.

2. **Wave 9 smoke regression**. If any commit inside Wave 6.5 causes `Run-Wave9.ps1` baseline to drop below the 2026-06-30 baseline (182 PASS / 0 FAIL / 79 SKIP). The Wave 6.5 pattern is behaviour-preserving; a regression means the outbox producer/consumer wiring lost a link. Halt, revert the offending commit, re-consult before re-attempting.

3. **`LankaEventsDbContext` operational-tables collision**. If the 6.5.e migration cannot create `events.outbox` / `events.outbox_dead_letter` / `events.idempotency_keys` because a prior module migration already created them under a different name/shape, halt and re-consult. The likely resolution is either (a) reuse the existing table via `ToTable("existing_name", "events")` in the outbox configuration, or (b) accept a schema-rename migration under the `SCHEMA-DESTRUCTIVE-APPROVED` rule — either way the founder-pairing rule applies.

4. **Multi-context transaction enrollment fails on Npgsql**. The 6.5.a mechanism relies on `context.Database.UseTransaction(existingTransaction)` succeeding for a second DbContext when the first has already opened the connection. If Npgsql throws (unlikely — it's a supported pattern, but no in-tree usage yet), halt and re-consult. Fallback path: two-phase-commit via `TransactionScope` is EXPLICITLY REJECTED per ADR-005; alternate path is per-module save with compensating actions on failure, which is a bigger architectural pivot requiring a new ADR.

5. **Integration event subscriber missing at runtime**. If a Wave 6.5 outbox migration reveals a subscriber never registered a handler for the integration event (i.e., the OutboxProcessor dispatches, `MediatRIntegrationEventDispatcher` finds zero handlers, event silently succeeds-with-no-effect), halt on that sub-slice. The behavior-preservation contract for Wave 6.5 requires that every domain event that produced an observable side-effect pre-6.5 continues to produce it via an integration event subscriber post-6.5. Zero-subscriber events mean the side-effect was lost.

6. **Rule 12 baseline JSON conflict during merge**. If two contributors' PRs both touch the baseline JSON and produce a merge conflict, halt the second PR. Rule 12 exists precisely to make this friction visible; the resolution is not to reconcile the JSON mechanically but to sequence the two PRs so the second re-generates against the first's post-merge baseline. Merge conflicts here are a design feature.

7. **`AppDbContext` snapshot drift regression**. If the 6.5.f cutover's dual-mapping deletion (6.5.f.6) reveals that `AppDbContext.OnModelCreating` still contains a `modelBuilder.Entity<X>()` for a LankaEvents-family entity AFTER the corresponding repo has been cutover, halt. This would mean two DbContexts both claim ownership — a snapshot drift bug. Trace back to which 6.5.f.N sub-slice missed the AppDbContext delete and address at that layer.

---

## 6. Migration-order matrix (baseline shrinkage tracker)

The 20-class Rule 12 baseline shrinks as follows. Column "Sub-slice" is the ONE sub-slice whose PR removes the row.

| Baseline class (Products.LankaEvents.Infrastructure.Repositories.*) | Sub-slice | Cluster | Notes |
|---|---|---|---|
| MetroAreaRepository | 6.5.f.1 | Leaf reads | Read-only, no writes, safest first |
| EventAnalyticsRepository | 6.5.f.1 | Leaf reads | Read-only |
| EventViewRecordRepository | 6.5.f.1 | Leaf reads | Read-only |
| AddOnDefinitionRepository | 6.5.f.2 | Sub-aggregate writes A | Standalone aggregate |
| SponsorRepository | 6.5.f.2 | Sub-aggregate writes A | Standalone aggregate |
| SponsorshipPackageRepository | 6.5.f.2 | Sub-aggregate writes A | Standalone aggregate |
| DonationRepository | 6.5.f.2 | Sub-aggregate writes A | Standalone aggregate |
| CollectionRepository | 6.5.f.2 | Sub-aggregate writes A | Standalone aggregate |
| VenueLayoutRepository | 6.5.f.3 | Sub-aggregate writes B | Venue cluster |
| SeatHoldRepository | 6.5.f.3 | Sub-aggregate writes B | Venue cluster |
| SeatReservationRepository | 6.5.f.3 | Sub-aggregate writes B | Venue cluster |
| TicketScanLogRepository | 6.5.f.3 | Sub-aggregate writes B | Operational log |
| EventNotificationHistoryRepository | 6.5.f.3 | Sub-aggregate writes B | Operational log |
| EventReminderRepository | 6.5.f.3 | Sub-aggregate writes B | Scheduling |
| AddOnPurchaseRepository | 6.5.f.4 | Payment cluster | Touches Rule 9b events; ship 6.5.g FIRST |
| RegistrationPaymentRepository | 6.5.f.4 | Payment cluster | Touches Rule 9b events |
| RegistrationAdditionRepository | 6.5.f.4 | Payment cluster | Touches Rule 9b events |
| TicketRepository | 6.5.f.4 | Payment cluster | Ticket generation on payment |
| RegistrationRepository | 6.5.f.5 | Aggregate root | Highest-touch — do last |
| EventRepository | 6.5.f.5 | Aggregate root | Highest-touch — do last |

Beyond the 20-class baseline, three separate debt buckets shrink under Wave 6.5:

| Debt bucket | Rule | Count | Retired by |
|---|---|---|---|
| Self-saving repos in Modules capabilities | (not gated — behavioural bug) | 4 (Notification, PhotoAlbum, Form, FormResponse) | 6.5.b + 6.5.c + 6.5.d |
| Payments.Application → Products.LankaEvents.Application | Rule 9b | 11 (Skip-fact enumeration) or 12 (actual grep) | 6.5.g |
| Legacy LankaConnect.Infrastructure → Products.LankaEvents.Application | Rule 5 | 14 | 6.5.h |

Total classes touched across Wave 6.5: 20 baseline + 4 self-saving + 11-12 Payments + 14 legacy = 49-50 classes. Each is a distinct compile-unit; expect 15-25 total commits across the wave.

---

## 7. Estimate

| Sub-slice | Sessions | Notes |
|---|---|---|
| 6.5.a | 1.0 | Mechanism-only; heavy test emphasis |
| 6.5.b | 1.0 | Canary; 7 handlers + repo + 2 events |
| 6.5.c | 0.5 | Leaf module |
| 6.5.d | 0.5 | Twin repo pattern |
| 6.5.e | 2.0 | DbContext + configs + operational-tables migration + staging apply |
| 6.5.f.1 | 0.5 | Leaf reads (3 repos) |
| 6.5.f.2 | 1.0 | Sub-agg cluster A (5 repos) |
| 6.5.f.3 | 1.0 | Sub-agg cluster B (6 repos) |
| 6.5.f.4 | 1.0 | Payment cluster (4 repos) — after 6.5.g |
| 6.5.f.5 | 1.0 | Aggregate roots (2 repos) — riskiest |
| 6.5.f.6 | 0.5 | AppDbContext dual-mapping cleanup |
| 6.5.g | 2.0 | Rule 9b (11-12 handlers) |
| 6.5.h | 2.0 | Rule 5 (14 legacy services) |
| **Total** | **14 sessions** | ≈ 3 weeks calendar at founder-pace with weekly UAT gates |

Calendar wall-clock: **2.5–3 weeks** at Approach 3 discipline (parallel structural + testing tracks), matching master-plan estimate. Under Approach 2 sequential fallback: **4–5 weeks**.

**Blocking risks** (in order of likelihood):
1. Wave 9.h.6 (auth/comms/webhook UAT) DEFERRED status — if founder UAT surfaces bugs in the webhook surface, those bugs must be fixed before 6.5.h can migrate the 7 `*WebhookHandler` files. Interaction risk: medium.
2. Wave 7.X.R trigger fires from 6.5.e config move (per Q8 ruling). Interaction risk: low.
3. Cross-schema FK dependencies between `events.registrations.user_id` and `identity.users.id` may reveal migration-ordering constraints during 6.5.e's staging apply. Interaction risk: medium — the FK is documented in §7.4 but no migration_ordering.md exists yet. Founder decision required if the staging apply fails: either (a) accept temporary FK removal + re-add in Phase B microservice extraction per §7.14, or (b) author `migration_ordering.md` with `[RequiresCapability]` attributes now (blueprint §7.4 originally deferred to Wave 4 alongside first cross-capability outbox use — this is that moment).
4. F30a-style bugs elsewhere. If 6.5.b canary reveals a fifth self-saving repo (hard-STOP #1), calendar slips by 0.5-1.0 sessions per additional repo.

**Non-blocking risks** (surfaced during execution, mitigated by discipline):
- Snapshot drift regression in 6.5.f.6 (mitigated by SnapshotDriftRules.cs already in place)
- Merge conflicts on Wave6_5TransitionalBaseline.json (per Q5 ruling, this is a feature)
- Domain event → integration event mapping ambiguity (mitigated by 6.5.b canary establishing the pattern)

---

## 8. Ruling summary

Wave 6.5 is one coherent slice with eight sub-slices, sequenced mechanism-first (6.5.a) then parallel-migrate the four self-saving capability repos (6.5.b canary + 6.5.c/d parallel) alongside the LankaEventsDbContext extraction (6.5.e), then linear-migrate the 20-class Products baseline in five clusters (6.5.f.1-5 with 6.5.f.6 cleanup), with the two Skip-fact debt buckets un-skipped in dedicated sub-slices (6.5.g Payments 9b before 6.5.f.4; 6.5.h Rule 5 last). F30a's workaround is fully retired in 6.5.b, not preserved. Integration events are NEW sealed V1 records in each module's `.Contracts` — not retrofits of MediatR domain events. Baseline JSON edits are atomic per PR — merge friction is intentional. Wave 7.X.R stays post-6.5. Every hard-STOP trigger routes back through the architect before proceeding.

The founder empowerment used in this ruling: Q1 mechanism-first (rejected alternate path), Q3 DbContext extraction IN scope (rejected banking-as-separate-wave), Q4 new records (rejected retrofit), Q5 incremental JSON (rejected batching), Q6 F30a full retirement (rejected preservation), Q8 Wave 7.X.R post-6.5 (rejected pull-forward). Q2 and Q7 confirmed the planning agent's reads. All eight questions have verdicts; none deferred back to founder.

Wave 6.5 is ready for execution. Handoff to Planning Agent for Master TODO update and founder approval on the 3-week calendar commitment.

---

### Critical Files for Implementation

The five files that carry the most weight when executing Wave 6.5:

- `C:\Work\LankaConnect\src\BuildingBlocks\BuildingBlocks.Abstractions\IUnitOfWork.cs` — the extension point for 6.5.a's multi-context `CommitAsync` overload; every downstream sub-slice depends on this signature
- `C:\Work\LankaConnect\src\BuildingBlocks\BuildingBlocks.Infrastructure\Outbox\OutboxIntegrationEventDispatcher.cs` — the transactional-write contract for module-context outbox rows; 6.5.a's `IIntegrationEventOutbox<TDbContext>` façade wraps this
- `C:\Work\LankaConnect\src\Modules\Media\Media.Infrastructure\Repositories\PhotoAlbumRepository.cs` — the 6.5.b canary + F30a evidence; class-remark XML doc lines 17-23 must be deleted, self-save calls on lines 117, 123, 129 must be removed
- `C:\Work\LankaConnect\tests\architecture\LankaConnect.ArchitectureTests\Wave6_5TransitionalBaseline.json` — the shrinkage tracker; every 6.5.f.N PR touches this file
- `C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\AppDbContext.cs` (lines 700-826, CommitAsync body) — the domain-event dispatch pattern that must be lifted into the new multi-context CommitAsync so module-context entities' domain events dispatch alongside AppDbContext's
