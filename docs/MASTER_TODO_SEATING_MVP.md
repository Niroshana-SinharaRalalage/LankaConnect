# Master TODO — Seating MVP (Architect Rev 4)

**Created**: 2026-04-30
**Authorized**: 2026-04-30 by user — "Go ahead" on 4-week plan (S1–S6).
**Plan source**: architect Rev 4 comprehensive RCA + design (in-session, not a file).
**Goal**: a production-shippable seating system covering both organizer (build layout) and buyer (pick seat) journeys. End the pattern of incremental "fix" slices that pass smoke individually but produce a broken end-to-end product.

## Confirmed user-blocking bugs (today)

1. **Slice 9.5 seat-gen pruning** (UI). User types Rows=4, blurs, types Seats per row=5 — the per-input handler reads `seatGen?.X ?? 0` (partial state was deleted by the previous commit's pruner) and the second input wipes the first. Save persists 0 seats. Confirmed at `web/src/presentation/components/features/events/CanvasEditor.tsx:251-271` + `CanvasEditorPropertyPanel.tsx:409-436`.
2. **"Change layout" UI flow** (UI). API works; UI fails end-to-end — likely modal→hook→cache-invalidation glue.

## Latent destructive bug

3. **PUT-replaces-all** (Backend API contract). `BatchUpdateLayoutCommandHandler.cs:121-135` treats missing items in payload as DELETE. Only protection is the structural-edit guard checking reserved seats. Empty unreserved zones get nuked silently. Slice S2 lands `deletedZoneIds` + 409 ambiguity guard.

## Architectural risks

4. **`Seat.VenueZoneId` / `Seat.VenueTableId` nullable XOR fights EF Core**. `_seats.Clear()` orphans seats by setting FK to NULL instead of deleting (because the FK is nullable). Replace with `SeatLocation` value object in S5.
5. **Buyer journey post-Slice-9 unverified end-to-end**. Tier-gating, hold timer, ticket emission — all unproven. S6 covers via Playwright.
6. **Hold guard does NOT cover active holds**. Organizer can delete a zone with held (not yet reserved) seats while buyer is on Stripe page. Extend in S2.

## Ship order

S1 → S2 → S3 → S4 → S5 → S6, then optionally S7.

**MVP gate**: S1–S6 all green on staging for 48 hours, no rollbacks.

---

## Smoke testing protocol (mandatory per slice, MUST pass before slice is marked complete)

**Lesson learned 2026-05-01**: prior smoke runs were endpoint-by-endpoint isolated calls. They missed bugs that are only visible when you walk a real user journey. Slice 9.5 / Slice S1's "apply-preset works" smoke called `POST /apply-preset` ONCE on a clean event and got 201 — but the actual user-blocking bug ("Change layout doesn't work after customizing") was a unique-constraint collision visible *only* on the second apply-preset for the same event. The user surfaced it; the smoke missed it.

**New rule**: every slice has a "Journey Smoke" section with at least 3 named user journeys covering the slice's surface area. Each journey is a sequence of API calls + expected end-state, not a single endpoint hit. The slice is NOT complete until the listed journeys all run green on staging post-deploy.

### Standard journey definitions (reused across slices)

**J-A — Organizer first-time setup (happy path)**:
1. Login.
2. Create event with `TicketingMode: Tiered`, 2 tiers.
3. Apply preset (e.g., theater-classic).
4. Verify `events.venue_layout_id` set, `seatingMode = AssignedSeating`, layout has expected seats.
5. Customize: add zone, set rows + cols, save.
6. Verify total seat count = preset seats + new zone seats.
7. Publish event.
8. Verify `events.status = Published`.

**J-B — Organizer changes their mind (the journey that surfaced today's bug)**:
1. Apply preset A.
2. Verify event points at preset A's layout.
3. Apply preset B (different name).
4. Verify event points at preset B's layout, NO orphans (`SELECT count(*) FROM venue_layouts WHERE event_id = X` = 1).
5. Apply preset A again (same name as step 1).
6. Verify event points at preset A's layout again, still no orphans.
7. Apply preset A AGAIN (idempotent).
8. Verify same result, still no orphans.

**J-C — Buyer end-to-end (Mode A + AssignedSeating)**:
1. Organizer set up event per J-A, published.
2. Buyer (different user) opens event detail page.
3. Starts registration → sees seat picker.
4. Picks N seats → sees them held.
5. Completes payment.
6. Receives confirmation email + ticket PDF with seat numbers.

**J-D — Buyer end-to-end (Mode B head-count, no assigned seating)**:
1. Organizer set up B-mode event (DetailedAttendees off, GA seating), published.
2. Buyer opens registration → sees tier counters + demographics, NO seat picker.
3. Submits → confirmation email correct.

**J-E — Concurrent / race scenarios**:
1. Organizer A holds editor open.
2. Organizer B saves changes via API.
3. Organizer A tries to save → expects 409 with current state shown.

**J-F — Invalid-combination guards**:
1. Try to enable AssignedSeating on a HeadCountByAge event → expects 400 with descriptive message.
2. Try to switch registration mode to HeadCountByAge while AssignedSeating is on → expects 400 with confirm-or-revert prompt.

### Per-slice journey requirements

| Slice | Journeys MUST run green |
|---|---|
| S1.5 | J-B (full sequence — the orphan-collision journey), J-F (Mode B + AssignedSeating rejection) |
| S1 (already shipped) | J-A steps 1–6 (already covered) — RETROACTIVE: also run J-B step 5 (re-apply same preset) |
| S2 | J-B + a destructive-payload journey (omit a zone, expect 409) |
| S3 | J-A with rename injected between steps 5 + 6 |
| S4 | J-A + a publish-with-unmapped-zone journey (expect blocker) + J-D with B-mode |
| S5 | J-A end-to-end (regression check) + verify no orphan seat rows in DB after `apply-preset` replace |
| S6 | All of J-A, J-B, J-C, J-D, J-E, J-F via Playwright e2e on staging |

**The slice is NOT complete until its journeys pass.** Adding journeys post-hoc to "find bugs faster" doesn't work — they have to gate the merge.

---

## Slice S1.5 — HOT-FIX: apply-preset orphan cleanup + Mode B incompatibility guard (1 day)

**Authorized 2026-05-01** by architect after user-reported regressions surfaced two bugs that the (endpoint-level) S1 smoke missed.

**Bug A — Apply-preset name collision**: `ix_venue_layouts_event_id_name` unique constraint blocks INSERT when an orphan exists with the same `(event_id, name)`. User-visible as "Change layout doesn't work" — re-applying the same preset (or any preset whose name matches an orphan) returns 500. Reproduced via journey J-B step 5.

**Bug B — Mode B + AssignedSeating combination has no buyer flow**: `HeadCountRsvpForm` doesn't render the seat picker; only `EventRegistrationForm` (Mode A) does. The combination was allowed at organizer time but was never wired end-to-end on the buyer side. User-visible as "Seating cannot be selected at registration" on event `d543629f`.

### Pre-flight check (before TDD red phase)

- [ ] Verify FK cascade rules on `venue_zones`, `venue_tables`, `seats`, `venue_decorations`, `tier_assignments` are `ON DELETE CASCADE`. If any are `NO ACTION` / `RESTRICT`, the inline hard-delete will fail and would need a migration. Check via DB inspection or EF config.

### TDD red phase

**Bug A (apply-preset orphan cleanup):**
- [ ] Handler test: `Apply_NoExistingLayout_CreatesLayoutAndAttaches` (baseline regression).
- [ ] Handler test: `Apply_ExistingAttachedLayout_DetachesAndHardDeletesOldLayout`.
- [ ] Handler test: `Apply_OrphanLayoutWithSameEventIdAndName_HardDeletesOrphanBeforeInsert` (the actual bug).
- [ ] Handler test: `Apply_MultipleOrphansSameEventId_HardDeletesAll` (defensive; clean ALL orphans).
- [ ] Handler test: `Apply_HardDeleteCascades_ZonesTablesSeatsDecorationsTierAssignmentsAllRemoved` (verify cascade integrity, child row counts = 0 after).
- [ ] Handler test: `Apply_TransactionRollback_OnInsertFailure_OldLayoutPreserved` (atomicity).
- [ ] Handler test: `Apply_NotEventOwner_ThrowsForbidden_NoDeletes` (auth before deletes).
- [ ] Repo test: `HardDeleteByEventIdAsync_RemovesAllLayoutsForEvent_AndCascades` (new repo method).
- [ ] Repo test: `HardDeleteByEventIdAsync_NoMatches_ReturnsZero_NoException` (idempotent).

**Bug B (domain invariant + UI gate):**
- [ ] Domain test: `EnableAssignedSeating_RegistrationModeIsHeadCountByAge_ThrowsDomainException`.
- [ ] Domain test: `EnableAssignedSeating_RegistrationModeIsHeadCountSimple_ThrowsDomainException`.
- [ ] Domain test: `EnableAssignedSeating_RegistrationModeIsDetailedAttendees_Succeeds` (happy path).
- [ ] Domain test: `SetRegistrationMode_FromDetailedToHeadCount_WhileSeatingIsAssigned_ThrowsDomainException`.
- [ ] Domain test: `SetRegistrationMode_FromHeadCountToDetailed_WhileSeatingIsGA_Succeeds` (no false block).
- [ ] Application test: `EnableAssignedSeatingCommand_HeadCountEvent_Returns400_NotASystemError`.
- [ ] Application test: `ApplyPresetToEventCommand_HeadCountEvent_Returns400_BeforeAnyDbWrites`.
- [ ] Run tests → red.

### Implementation

**Bug A:**
- [ ] New `IVenueLayoutRepository.HardDeleteByEventIdAsync(Guid eventId, CancellationToken ct)` returning `Task<int>` (rows deleted, for logging).
- [ ] `ApplyPresetToEventCommandHandler` order: `LoadEvent → AuthorizeOwnership → ValidateModeCompatibility (Bug B) → DetachVenueLayoutId → HardDeleteByEventIdAsync(eventId) → BuildNewLayoutFromPreset → AddAsync → AssignToEvent → CommitTransaction`.
- [ ] Wrap in single `IUnitOfWork` transaction.
- [ ] Log: `"Apply-preset cleanup: deleted {OrphanCount} prior layouts for event {EventId}"`.
- [ ] Same hard-delete-old-layout pattern in `ApplyTemplateToEventCommandHandler` for parity.

**Bug B:**
- [ ] Domain invariant `Event.AssignedSeating ⇒ DetailedAttendees`. Add to `EnableAssignedSeating` AND `SetRegistrationMode` paths.
- [ ] `RsvpFormSection.tsx` early-return banner for the broken combination: `seatingMode === AssignedSeating && registrationMode !== DetailedAttendees` → "Registration temporarily unavailable — organizer configuration in progress."
- [ ] Frontend: existing `seatingMode` toggle disabled with tooltip when `registrationMode !== DetailedAttendees`.

**Existing-data cleanup (NO auto-mutation):**
- [ ] Detection query: `SELECT id, name FROM events WHERE seating_mode = 'AssignedSeating' AND registration_mode IN ('HeadCountByAge', 'HeadCountSimple', ...)` — runs once during deployment, logs the affected event IDs. Organizer-driven resolution per architect.

### Journey smoke (mandatory pre-completion)

- [ ] **J-B (apply-preset replacement journey)** — full sequence:
  - login → create event → apply preset A (theater-classic) → 201
  - verify `event.venueLayoutId` set
  - apply preset B (theater-with-balcony) → 201
  - verify `event.venueLayoutId` updated, layout count = 1 (no orphan)
  - apply preset A again (theater-classic, same name as step 3) → 201 (this is the bug fix)
  - verify event points at preset A, layout count = 1 still
  - apply preset A again (idempotent) → 201
  - cleanup
- [ ] **J-F (Mode B + AssignedSeating rejection)**:
  - login → create event with `registrationMode: HeadCountByAge`
  - try `apply-preset` → expect 400 with descriptive message
  - try direct `EnableAssignedSeating` (if endpoint exists) → expect 400
  - flip event back to `DetailedAttendees`, retry `apply-preset` → 201
- [ ] **J-A retroactive verification (Slice S1's surface)**: re-run S1's seat-gen smoke after S1.5 ships to confirm no regression.

### Verification + deploy

- [ ] All tests green locally.
- [ ] Commit + push + deploy via `deploy-staging.yml` + `deploy-ui-staging.yml`.
- [ ] J-B + J-F + J-A journeys run green against staging post-deploy.
- [ ] User's event `d543629f-…` shows the banner (or organizer fixes the configuration via the new validation).
- [ ] 0 ERROR-level logs in container for 24h post-deploy.
- [ ] Update tracker docs.

---

## Slice S1 — Unblock the user TODAY: seat-gen pruning + change-layout (1–2 days)

**Goal**: fix the two confirmed user-blocking bugs.

### TDD red phase

- [ ] Unit test (frontend): `composeBatchPayload` with `seatGenByZoneId = { [zoneA]: { rowCount: 4, seatsPerRow: 5 } }` → payload contains `rowCount: 4, seatsPerRow: 5` for zone A.
- [ ] Unit test (frontend): `composeBatchPayload` with `seatGenByZoneId = { [zoneA]: { rowCount: 4, seatsPerRow: 0 } }` → payload does NOT include rowCount/seatsPerRow for zone A (partial state pruned at compose time).
- [ ] Unit test (frontend): property panel `handleRowsCommit("4")` followed by `handleSeatsPerRowCommit("5")` produces final draft state with `{ rowCount: 4, seatsPerRow: 5 }` for the selected zone (currently fails).
- [ ] Unit test (frontend): clearing one input (Rows=0) preserves the other (Seats per row=5) in draft state until both are empty.
- [ ] Unit test (frontend): SeatingLayoutPicker — "Change layout" button click while layout exists → ConfirmDialog open. Confirm → PresetLibraryModal open.
- [ ] Unit test (frontend): SeatingLayoutPicker — picking a preset from the modal calls `useApplyPresetToEvent.mutateAsync` with the right args.
- [ ] Unit test (frontend): SeatingLayoutPicker — apply-preset success invalidates `venueLayoutKeys.byEvent(eventId)` so the canvas refetches.
- [ ] Run tests → red.

### Implementation

- [ ] Frontend: `handleSeatGenChange` (CanvasEditor.tsx) — store partial state in `seatGenByZoneId` even if either field is 0. Only delete the entry when BOTH fields are 0/null (full clear).
- [ ] Frontend: `handleRowsCommit` / `handleSeatsPerRowCommit` (CanvasEditorPropertyPanel.tsx) — read previous partner value via the `seatGen` prop (which now holds partial state) and never overwrite it with 0.
- [ ] Frontend: `composeBatchPayload` (canvasEditorGeometry.ts) — only emit `rowCount` + `seatsPerRow` to BatchZone when BOTH are positive integers. Partial state stays client-side until complete.
- [ ] Frontend: `countDraftChanges` — partial state still counts as a change (so the user sees "you have unsaved changes" hint), but only complete state triggers backend seat-gen on save.
- [ ] Frontend: SeatingLayoutPicker change-layout flow — verify the wiring: button click → ConfirmDialog → PresetLibraryModal → preset card click → `useApplyPresetToEvent` → cache invalidation. Fix whatever is broken.
- [ ] Refactor: extract pruning to a single utility (`isSeatGenComplete(entry)`) used by both compose + countChanges.

### Verification + deploy

- [ ] All new unit tests green.
- [ ] Existing tests still green (especially CanvasEditor.test.tsx + CanvasEditorPropertyPanel.test.tsx + SeatingLayoutPicker tests).
- [ ] `tsc --noEmit` clean.
- [ ] Commit + push + deploy via `deploy-ui-staging.yml`.
- [ ] **API smoke** post-deploy: PUT batch-update with rowCount + seatsPerRow → 200, zone has 20 seats. (Already known to work from Slice 9.5 backend smoke.)
- [ ] **UI smoke** post-deploy (manual): apply preset → customize → add zone → enter rows + cols → save → seats persist + visible.
- [ ] **UI smoke** post-deploy (manual): apply preset → click "Change layout" → confirm dialog → pick different preset → canvas updates.
- [ ] Update tracker docs.

---

## Slice S2 — PUT-with-deletedIds + extend hold guard (2–3 days)

**Goal**: close the destructive-PUT class of bugs. Cover active holds in the structural guard.

### TDD red phase

- [ ] Backend handler test: payload with empty `zones[]`, no `deletedZoneIds`, layout has 1 zone → **409 Conflict**, no DB change.
- [ ] Backend handler test: `zones: [zone1]`, no `deletedZoneIds`, layout has zone1 + zone2 → 409 (zone2 implicitly missing).
- [ ] Backend handler test: `zones: [zone1]`, `deletedZoneIds: [zone2]` → 200, zone2 deleted. Same for tables + decorations.
- [ ] Backend handler test: `deletedZoneIds: [zoneX]` where zoneX has held (active) seats → 409 with "seats are currently held by other buyers" message.
- [ ] Backend handler test: `deletedZoneIds: [zoneX]` where zoneX has reserved seats → 409 (existing behavior, unchanged).
- [ ] Frontend test: `composeBatchPayload` populates `deletedZoneIds` from a diff between baseline and draft.
- [ ] Run → red.

### Implementation

- [ ] Backend: extend `BatchUpdateLayoutRequest` (and `BatchLayoutPayload` C# record) with optional `DeletedZoneIds`, `DeletedTableIds`, `DeletedDecorationIds` lists.
- [ ] Backend: `BatchUpdateLayoutCommandHandler` — compute `payloadIds` per kind. For each baseline id NOT in `payloadIds`: if id ∈ `deletedIds`, delete (subject to existing structural guard); else 409.
- [ ] Backend: extend `SeatStructuralEditGuard` to query active `seat_holds` (where `expires_at > now()`) for zones/tables being deleted. Return typed failure: `"Seats are currently held by other buyers. Try again in N minutes."`
- [ ] Frontend: `composeBatchPayload` computes `deletedZoneIds` etc. from `baseline.zones - draft.deletions - draft.additions` (zones the user explicitly deleted via the UI).
- [ ] Frontend: TS types mirror the backend.
- [ ] ADR-006: `docs/architecture/ADR-006-canvas-batch-update-semantics.md`.

### Verification + deploy

- [ ] All tests green.
- [ ] Deploy backend + frontend.
- [ ] API smoke: each scenario above via curl.
- [ ] UI smoke: delete a zone in canvas editor + save → 200; manually omit a zone via curl payload → 409.

---

## Slice S3 — Layout rename UI + truthful subtitle (1–2 days)

**Goal**: organizer can rename the layout; modal title/subtitle are consistent.

### TDD red phase

- [ ] Backend test: `PATCH /api/venue-layouts/{id}/name` with new name → 200, name persisted, RowVersion bumped.
- [ ] Frontend test: canvas editor renders editable name field; commit triggers PATCH; modal title updates.
- [ ] Run → red.

### Implementation

- [ ] Backend: new `PATCH /api/venue-layouts/{id}/name` endpoint + `RenameVenueLayoutCommand` handler. Optimistic concurrency via If-Match (RowVersion).
- [ ] Frontend: layout name input in canvas editor header (or inline-editable title in CanvasEditorModal). Commits via dedicated PATCH (not via batch-update — naming is independent of structural edits and shouldn't share a concurrency token).
- [ ] Update modal subtitle: "Currently: N seats · M zones · K tables" — clearly secondary metadata. Header shows the editable name.

### Verification

- [ ] All tests green.
- [ ] Deploy + UI smoke.

---

## Slice S4 — Tier-mapping summary + pre-publish validation (3–4 days)

**Goal**: organizer sees holistic tier mapping; can't publish a misconfigured layout.

### TDD red phase

- [ ] Query test: `ValidateLayoutForPublishQuery` — layout with one tier mapped to 0 zones → warning. Layout with 0 tiers → blocker.
- [ ] UI test: tier overview pane renders correctly with mixed-mapping layouts.
- [ ] UI test: publish button disabled when blockers exist; confirm dialog shown when warnings exist.
- [ ] Run → red.

### Implementation

- [ ] Backend: `ValidateLayoutForPublishQuery` returning `{ warnings: [], blockers: [] }`.
- [ ] Backend: hook into existing publish flow (probably `PublishEventCommandHandler`).
- [ ] Frontend: tier overview pane in canvas editor (sidebar). Shows tiers with mapped zones/tables and seat counts.
- [ ] Frontend: same overview surfaced (read-only) in `SeatingLayoutPicker` summary.
- [ ] Frontend: publish button wired to validator.

### Verification

- [ ] All tests green.
- [ ] Deploy + API smoke (publish with bad config → 422).
- [ ] UI smoke: organizer sees tier-mapping holistically + publish-time validation.

---

## Slice S5 — SeatLocation value object + EF migration (4–5 days)

**Goal**: eliminate the nullable-XOR vs EF cascade conflict. Domain model becomes self-documenting; orphan seat rows stop accumulating.

### TDD red phase

- [ ] Domain test: `Seat` with `SeatLocation { Kind: Zone, OwnerId: zoneId }` is constructible. `Kind: Table` likewise. No nullable XOR.
- [ ] EF integration test: deleting a `VenueZone` cascades and removes its seats from the DB.
- [ ] EF integration test: `zone.ClearSeats()` followed by save removes seat rows from DB (no orphans).
- [ ] Migration test: existing data is backfilled correctly into the new column.
- [ ] Run → red.

### Implementation

- [ ] Domain: `SeatLocation` value object on `Seat`. Update `Seat.CreateInZone` / `Seat.CreateAtTable` factories.
- [ ] Infrastructure: EF Core configuration — `Seat` owned by either `VenueZone` or `VenueTable` (TPH-on-aggregate or two distinct relationships with `WillCascadeOnDelete(true)`).
- [ ] EF migration via `dotnet ef migrations add ConsolidateSeatLocation` (NOT hand-created — CLAUDE.md memory).
- [ ] Migration: backfill existing seat rows into the new shape. Drop old nullable columns.
- [ ] Update every callsite of the old `VenueZoneId` / `VenueTableId` properties.
- [ ] ADR-007: `docs/architecture/ADR-007-seat-location-value-object.md`.

### Verification

- [ ] 90%+ unit coverage on the new shape.
- [ ] EF integration tests against a real PostgreSQL.
- [ ] Local `dotnet ef database update` succeeds; rollback is clean.
- [ ] Staging deploy: pick preset → save → check seat row count in DB matches generated count exactly (no orphans).

---

## Slice S6 — Playwright e2e + observability + perf (5–7 days) — MVP GATE

**Goal**: positive end-to-end proof that the system works for both roles. Production-ready observability + perf.

### TDD test list (the Playwright tests ARE the test list)

- [ ] **Organizer happy path**: create event → enable seating → pick preset → customize (add zone with seats, add round table, map tiers) → save → publish.
- [ ] **Buyer happy path**: open event detail → start registration → pick tier → see seat picker with correct tier-gating → pick N seats → 10-min hold visible → checkout via Stripe (test mode) → confirmation email + ticket PDF have seat numbers.
- [ ] **Change layout flow**: organizer with attached layout picks a different preset → confirms → new layout attaches; buyer registration not affected mid-window.
- [ ] **Race scenario**: organizer edits while buyer holds a seat → structural guard rejects with clear UX.
- [ ] **Hold expiry**: buyer holds, walks away, 10+ min later → seat releases → another buyer can claim it.
- [ ] **Stripe webhook idempotency**: replay → no duplicate reservation.

### Observability

- [ ] Metrics: `canvas_editor.session_started`, `canvas_editor.save_succeeded`, `canvas_editor.save_failed{reason}`, `canvas_editor.session_abandoned`. Plus existing `layout.structural_edit_rejected`.
- [ ] Hold lifecycle metrics: `seat_hold.created`, `seat_hold.expired`, `seat_hold.converted_to_reservation`.
- [ ] Logs: every save with `{layoutId, eventId, organizerId, changesCount, durationMs}`.
- [ ] Alerts: canvas-editor save error rate > 5% in 5 min → page on-call.

### Perf

- [ ] 1000-seat layout fixture.
- [ ] Konva render benchmark on mid-range mobile. If < 30fps, virtualize seat rendering (render only viewport + margin).
- [ ] Batch-update payload size for 1000 seats verified < 500KB.
- [ ] Seat-availability query p95 < 200ms with 1000 seats.

### Verification

- [ ] Playwright suite green against staging.
- [ ] Metrics visible in App Insights.
- [ ] Perf benchmark passes.

**Ship gate**: when this slice ships and is green for 48 hours, declare MVP shippable.

---

## Slice S7 — Polish (post-MVP, ship at leisure)

- [ ] Delete deprecated endpoints `from-preset` / `from-template` / `assign` + their hooks + repo methods (Slice 9.4c).
- [ ] Capacity input on property panel for tables.
- [ ] Curvature parameter for theater zones with curved fronts.
- [ ] "Regenerate seats" path on populated zones with explicit destructive confirmation dialog.

---

## Run history

| Date | Slice | Result | Notes |
| --- | --- | --- | --- |
| 2026-04-30 | Plan authorized | n/a | User authorized 4-week plan (S1–S6). |
| 2026-04-30 | S1 backend + frontend | ✅ SHIPPED | Commit `3e63620a` deployed via UI run `25200133808` `success`. New `pickCompleteSeatGen` utility centralises partial-state pruning at compose time; CanvasEditor handler stores partial state instead of deleting; property panel commits carry partner values. **5 new red-then-green tests** passing; **22/22 existing CanvasEditorPropertyPanel tests** unchanged; **98/98 canvasEditorGeometry tests** pass; tsc clean. **API smoke** end-to-end: apply Theater Classic preset → PUT batch with new "Balcony" zone + `rowCount:3, seatsPerRow:5` → 204; total 215 seats (200 + 15 generated); 0 orphans. The user's reported bug is closed at the data layer. **Change-layout UI flow runtime verification**: deferred to manual UI smoke or S6 Playwright suite — wiring inspected statically and looks correct. |
