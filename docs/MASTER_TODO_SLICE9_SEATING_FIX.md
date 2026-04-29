# Master TODO — Slice 9 Seating Layout Fix

**Created**: 2026-04-29
**Owner**: Niroshana
**Plan source**: `C:\Users\Niroshana\.claude\plans\stateful-soaring-galaxy.md` + architect Revisions 1/2/3 dossier (this session)
**Goal**: Fix the four cooperating defects (RC-1 through RC-4) that produce the user-reported "Theater Classic · 0 seats" + "Customize doesn't apply" symptoms. End-state: preset/template selection on a tiered event attaches correctly, customize round-trips without wiping seats, layout swap is guarded by a confirmation dialog, no orphan accumulation.

## RCA recap (architect Revision 1)

- **RC-1** (Backend / Domain): `VenueLayout.ValidateForEvent` requires every zone to have a tier_assignment row, but `from-preset` and `from-template` create zones with NO mapping. → 100% broken on tiered events.
- **RC-2** (Backend / Database access): `GetByEventIdAsync` filters `WHERE event_id = X` instead of joining via `events.venue_layout_id`. → Orphan layouts visible.
- **RC-3** (Backend / API contract): `BatchUpdateLayoutCommandHandler` treats `zones: null/[]` as full replacement → silently deletes pre-existing zones.
- **RC-4** (Frontend / Orchestration): `SeatingLayoutPicker.handlePresetSelected` doesn't roll back orphan when assign fails.

## User-confirmed design choices (from architect Rev 2/3)

- **No auto-tier-mapping**: zones come in tier-less; organizer maps in Customize.
- **Two-stage validation**: structural at apply-time (`requireTierMapping=false`); strict at publish-time via new `Event.CheckLayoutPublishReadiness(layout)` method.
- **Hard-delete orphans** via migration with audit snapshot in generic `events.deleted_layouts_audit` table.
- **PUT semantics retained** for `/batch` with explicit `deletedZoneIds` + 409 ambiguity guard.
- **Old endpoints removed immediately** in both staging and prod (zero external consumers, never shipped to prod).
- **Change-layout confirmation dialog** in Slice 9.4 (reuses existing `ConfirmDialog`).
- **`Event.Publish()` signature unchanged** (Option D): new sibling method `CheckLayoutPublishReadiness(layout)` called by handler before `Publish()`.

## Ship order

**9.3 → 9.1 → 9.2 → 9.4** (architect-recommended; user-approved).

---

## Slice 9.3 — Repository fix + orphan reclamation migration

**Goal**: Stop returning orphan layouts via `by-event/{id}`. Hard-delete existing orphans on staging with audit snapshot. Production has zero orphans (Slice 8 never shipped to prod) so migration is a no-op there.

### TDD red phase
- [ ] Write failing repo test: `GetAssignedLayoutForEventAsync_returns_layout_when_event_attached` (current code passes via accidental orphan match — need to assert it returns ONLY the assigned one).
- [ ] Write failing repo test: `GetAssignedLayoutForEventAsync_returns_null_when_event_has_no_layout` (currently returns orphan if event_id matches).
- [ ] Write failing repo test: `GetAssignedLayoutForEventAsync_ignores_orphans_with_matching_event_id_but_no_back_reference` (the core bug-fix test).
- [ ] Run tests → verify red.

### Implementation
- [ ] Rename `IVenueLayoutRepository.GetByEventIdAsync` → `GetAssignedLayoutForEventAsync` (forces compile-time discovery of all callers).
- [ ] Rewrite repo SQL to JOIN via `events.events.venue_layout_id`.
- [ ] Add `GetOrphansForEventAsync(Guid eventId)` for diagnostic tooling (tester confidence).
- [ ] Audit all callers (grep `GetByEventIdAsync` across `src/`) → update each call site. Approximate caller count: ~6 (per earlier file-list audit).
- [ ] Update frontend repo `getLayoutByEvent` (no rename — same URL).
- [ ] Run tests → verify green.

### Migration (`Slice93HardDeleteOrphanLayouts`)
- [ ] Create via `dotnet ef migrations add Slice93HardDeleteOrphanLayouts --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --context AppDbContext` (ensure `.Designer.cs` is generated — per CLAUDE.md memory).
- [ ] Migration `Up()` body:
  - Create generic `events.deleted_layouts_audit` table if not exists (`layout_id, layout_name, event_id, original_created_at, zone_count, seat_count, deleted_at, deleted_by_migration`).
  - Pre-flight: count orphans → `RAISE NOTICE`.
  - Pre-flight: ensure no live `seat_holds` reference orphan-layout seats → `RAISE EXCEPTION` if nonzero (cascade safety).
  - Snapshot orphan summary into audit table.
  - Hard `DELETE` from `venue_layouts` matching orphan condition.
  - Post-condition: deletion count == orphan count → `RAISE EXCEPTION` on mismatch (Phase 6A.122 silent-failure guard).
- [ ] Migration `Down()` body: `RAISE NOTICE` only (hard-delete is irreversible; audit table preserves the forensic trail).
- [ ] Verify `.Designer.cs` exists in commit (per CLAUDE.md MEMORY note on hand-rolled migrations being invisible to EF Core).

### Verification + deploy
- [ ] Run all backend tests locally → green.
- [ ] Commit with message: `fix(events/seating-9.3): correct GetAssignedLayoutForEventAsync + hard-delete orphan layouts migration`.
- [ ] Push to develop → triggers `deploy-staging.yml`.
- [ ] Wait for deploy success.
- [ ] Verify migration applied on staging: query `events.deleted_layouts_audit` → has rows for known orphans (we cleaned up 4+1 today, but newer orphans `cf41b216-…` from RCA repro should be deleted too).
- [ ] Verify `GET /api/venue-layouts/by-event/e4792b64-...` returns 400 "Venue layout not found" (was returning the orphan with 200 seats).
- [ ] Run Slice 8 API smoke deck (15 tests) → still 15/15 PASS (no regression).
- [ ] Update tracker docs (PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN).
- [ ] Tick all checkboxes above.

---

## Slice 9.1 — Domain `CheckLayoutPublishReadiness` + handler integration

**Goal**: Add publish-time strict validation for events with attached layouts WITHOUT touching the `Event.Publish()` signature (preserves all 32 existing tests).

### TDD red phase
- [ ] Write failing domain tests for `Event.CheckLayoutPublishReadiness(layout)`:
  - Valid layout → success.
  - Layout-id mismatch → failure.
  - No zones → failure.
  - Zone with no seats → failure.
  - Seat with null tier → failure.
  - Seat with unknown tier → failure.
- [ ] Write failing handler tests for `PublishEventCommandHandler`:
  - Non-seated event (`VenueLayoutId == null`) → does not load layout, calls `Publish()` directly.
  - Seated event, readiness fails → returns failure, does not publish.
  - Seated event, readiness succeeds → publishes.
  - Seated event, layout fetch returns null → returns failure with specific message.
- [ ] Run → verify red.

### Implementation
- [ ] Add `Event.CheckLayoutPublishReadiness(VenueLayout layout)` to `Event.Seating.cs`. Returns `Result.Success()` when all invariants pass; `Result.Failure(specific message)` on first failure (fail-fast).
- [ ] Update `PublishEventCommandHandler`:
  1. After loading event, if `event.VenueLayoutId.HasValue`, fetch layout via `IVenueLayoutRepository.GetAssignedLayoutForEventAsync(event.Id)` (using new name from 9.3).
  2. If layout is null → return `Result.Failure("Event references a venue layout that could not be loaded")` + `LogWarning`.
  3. Call `event.CheckLayoutPublishReadiness(layout)` → if failure, return that.
  4. Call `event.Publish()` (unchanged).
- [ ] Add structured logging at every step: layoutId, eventId, readiness outcome, durationMs.
- [ ] Run tests → verify green. The 32 existing `Publish()` tests must remain untouched and passing.

### Verification + deploy
- [ ] All backend tests green.
- [ ] Commit, push, deploy.
- [ ] API smoke: publish a seated tiered event without tier mappings → expect 400 with descriptive failure message.
- [ ] API smoke: publish a non-seated event (general admission) → expect 200 (back-compat preserved).
- [ ] API smoke: publish a seated event with full tier mappings → expect 200.
- [ ] Update tracker docs.
- [ ] Tick checkboxes.

---

## Slice 9.2 — `ApplyPresetToEventCommand` + `ApplyTemplateToEventCommand` (atomic)

**Goal**: Collapse from-preset+assign and from-template+assign into single transactional commands. Eliminates orphan-on-partial-failure. No auto-tier-mapping.

### TDD red phase
- [ ] Write failing handler tests for `ApplyPresetToEventCommand`:
  - Tiered event, single tier, success → layout created with zones (no tier_assignments), event.VenueLayoutId set, event.SeatingMode = AssignedSeating, response includes TierMappingStatus = Unmapped.
  - Tiered event, multiple tiers → same as above (no auto-map).
  - General-admission event → layout created without tier-validation; event updated.
  - Preset id invalid → throws, no layout created.
  - Event already has layout → detaches old (emits domain event), attaches new, returns 200.
  - Persistence fails mid-transaction → no orphan, event unchanged (transaction rollback verification).
  - Published event → 422 (cannot modify).
- [ ] Write failing handler tests for `ApplyTemplateToEventCommand` (mirror).
- [ ] Run → verify red.

### Implementation
- [ ] Domain: add `Event.AttachVenueLayout(VenueLayout layout)` method (validates `layout.EventId == this.Id`). Replaces direct setter.
- [ ] Domain: extend `VenueLayout.ValidateForEvent(IEnumerable<TicketTier> tiers, bool requireTierMapping)` — flag parameter; structural-only when `false`.
- [ ] Application: new `ApplyPresetToEventCommand` + handler. Single `IUnitOfWork` transaction. Steps:
  1. Load event with tiers.
  2. Detach old layout if any (emits `EventLayoutDetached`).
  3. Build layout from preset blueprint (no tier mappings).
  4. `layout.ValidateForEvent(tiers, requireTierMapping: false)` → structural validity.
  5. Persist layout.
  6. `event.AttachVenueLayout(layout)`.
  7. Persist event.
  8. Commit.
- [ ] Application: `ApplyTemplateToEventCommand` + handler — mirror, uses `VenueLayout.CloneFromTemplate`.
- [ ] Application: extend `LayoutSummaryDto` (or `VenueLayoutDto`) with `TierMappingStatus` (FullyMapped | PartiallyMapped | Unmapped).
- [ ] API: new endpoints `POST /api/venue-layouts/apply-preset` + `POST /api/venue-layouts/apply-template`.
- [ ] API: filter for `LayoutNotPublishReadyException` → 422 with `remediation` block (deferred to 9.4 if it's only used by publish; verify scope).
- [ ] Domain events: `EventVenueLayoutAttached`, `EventLayoutDetached` (verify exist; add if missing).

### Verification + deploy
- [ ] All backend tests green.
- [ ] Commit, push, deploy.
- [ ] API smoke (against user's event `e4792b64-...`):
  ```
  POST /api/venue-layouts/apply-preset {presetId:"theater-classic",eventId:"..."}
  → 200, layoutId, tierMappingStatus: "Unmapped"
  GET /api/Events/{id} → venueLayoutId set, seatingMode = AssignedSeating
  GET /api/venue-layouts/by-event/{eventId} → returns the layout (now via 9.3's fixed JOIN)
  ```
- [ ] API smoke: re-apply on same event with different preset → old detached, new attached, no orphan accumulation (verify via 9.3's audit table).
- [ ] Run Slice 8 smoke (15/15).
- [ ] Update tracker docs.
- [ ] Tick checkboxes.

---

## Slice 9.4 — UI cutover + `BatchUpdate.deletedZoneIds` + endpoint removal + change-layout confirmation dialog

**Goal**: Frontend cuts over to apply-* endpoints. Customize-save can no longer destructively wipe (explicit `deletedZoneIds` + 409 ambiguity guard). Remove dead endpoints. Add change-layout confirmation dialog.

### TDD red phase (frontend)
- [ ] Write failing tests for `useApplyPresetToEvent`, `useApplyTemplateToEvent`.
- [ ] Write failing tests for `SeatingLayoutPicker.handlePresetSelected` cutover.
- [ ] Write failing test for `SeatingLayoutPicker_ExistingLayout_ChangeButtonOpensConfirmDialog`.
- [ ] Write failing test for `SeatingLayoutPicker_ConfirmReplace_OpensPresetModal`.
- [ ] Write failing test for `SeatingLayoutPicker_CancelReplace_DoesNotOpenPresetModal`.
- [ ] Write failing tests for `CanvasEditorModal` save-with-`deletedZoneIds` + 409 handling.
- [ ] Write failing tier-mapping-status affordance test.

### TDD red phase (backend)
- [ ] Write failing handler tests for `BatchUpdateLayoutCommandHandler` with new `deletedZoneIds`:
  - Null zones + null deletedZoneIds → 400.
  - Empty zones + empty deletedZoneIds → 400.
  - Full zones, no deletedZoneIds → existing behavior (back-compat).
  - Partial zones + explicit deletedZoneIds → patches.
  - Ambiguous omission (zone exists in DB, missing from both arrays) → 409 with `ambiguousZoneIds` list.
  - Zone deletion → audit log emitted.

### Implementation
- [ ] Backend: `BatchLayoutPayload` extended with `DeletedZoneIds: IReadOnlyList<Guid>?`.
- [ ] Backend: new `BatchAmbiguousZoneOmissionException` → 409 filter.
- [ ] Backend: handler logic per architect spec; structured audit log on every zone delete.
- [ ] Frontend: new `useApplyPresetToEvent` + `useApplyTemplateToEvent` hooks.
- [ ] Frontend: new `applyPresetToEvent` + `applyTemplateToEvent` repo methods.
- [ ] Frontend: `SeatingLayoutPicker` rewrites both `handlePresetSelected` + `handleTemplateSelected` to use apply-*. ConfirmDialog wraps "Change layout" button when `event.VenueLayoutId != null`.
- [ ] Frontend: tile renders TierMappingStatus affordance ("N zones not yet assigned to tiers — Open Customize") with deep-link to customize.
- [ ] Frontend: `CanvasEditorModal` Save composes `deletedZoneIds` from draft state delta. 409 handling shows ambiguous-zones list with retry.
- [ ] Backend cleanup (after frontend cutover proven):
  - Delete `POST /from-preset` endpoint + `CreateLayoutFromPresetCommand/Handler` + tests.
  - Delete `POST /from-template` endpoint + `CreateLayoutFromTemplateCommand/Handler` + tests.
  - Delete `POST /assign` endpoint + `AssignLayoutToEventCommand/Handler` + tests.
- [ ] Frontend cleanup:
  - Delete `useCreateLayoutFromPreset`, `useCreateLayoutFromTemplate`, `useAssignLayoutToEvent` hooks + tests.
  - Delete `createFromPreset`, `createFromTemplate`, `assignLayoutToEvent` repo methods + tests.
  - Delete request types from `events.types.ts`.

### Verification + deploy
- [ ] All tests green (frontend + backend).
- [ ] Commit, push, deploy backend → deploy frontend.
- [ ] Manual UI smoke on user's event `e4792b64-...`:
  1. Pick Theater Classic → tile shows "Theater Classic · 200 seats · 1 zone · 1 zone not yet assigned to tiers — Open Customize" affordance.
  2. Click Customize → modal subtitle shows "Theater · 200 seats · 1 zone" (matches!).
  3. Add Zone 2 → click Save → 200 (or 409 if ambiguous, in which case test the ambiguity flow).
  4. Click "Change layout" → confirmation dialog appears.
  5. Cancel → no change. Confirm → preset modal opens.
  6. Pick a different preset → old detached + audit-snapshotted, new attached.
  7. Refresh → tile shows new layout.
- [ ] API smoke: 4 deleted endpoints return 404; new endpoints work.
- [ ] Run Slice 8 smoke deck (15/15) — note: 3 tests need updating since they target the removed endpoints; replace with apply-* tests.
- [ ] Update tracker docs.
- [ ] Tick checkboxes.

---

## Final acceptance criteria

- [ ] User picks any preset on a tiered event → tile shows correct seat count + "tiers not yet assigned" affordance.
- [ ] User picks any saved template on a tiered event → same behavior.
- [ ] User customizes (assigns tiers, adds/removes zones) → save round-trips persists; explicit `deletedZoneIds` for removals.
- [ ] Customize-save with incomplete zones (no `deletedZoneIds`) → 409, no destruction.
- [ ] User clicks "Change layout" with existing layout → confirmation dialog. Cancel preserves; confirm opens preset modal.
- [ ] Publish without mapping → 422 with specific zone names + "Fix in Customize" CTA.
- [ ] No orphan accumulation (verified via `events.deleted_layouts_audit`).
- [ ] All 4 deleted endpoints (`from-preset`, `from-template`, `assign`, plus the deprecated others) → 404.
- [ ] All Slice 8 tests pass (with apply-* replacing the 3 removed endpoints' tests).
- [ ] No ERROR logs in staging during smoke.

## Run history

| Date | Slice | Result | Notes |
| --- | --- | --- | --- |
| 2026-04-29 | Plan created | n/a | Architect Rev 3 design approved by user; ready to implement 9.3 first |
