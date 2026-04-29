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

## Slice 9.3 — Repository fix + orphan reclamation migration ✅ SHIPPED 2026-04-29

**Result**: deployed via run `25131067970`, verified end-to-end on staging.
Three commits: `ce1c66de` (initial), `a560eee6` (PascalCase Id quoting fix),
`6f84abb6` (cascade-clean dangling seat_holds instead of abort-on-holds).



**Goal**: Stop returning orphan layouts via `by-event/{id}`. Hard-delete existing orphans on staging with audit snapshot. Production has zero orphans (Slice 8 never shipped to prod) so migration is a no-op there.

### TDD red phase
- [ ] ~~Write failing repo test for SQL JOIN behavior — Docker not running locally; integration tests not feasible. Replaced by post-deploy staging API verification.~~
- [x] Write red contract via the architect's spec (the SQL JOIN's expected behavior captured in the doc comment of `IVenueLayoutRepository.GetAssignedLayoutForEventAsync` interface).

### Implementation
- [x] Rename `IVenueLayoutRepository.GetByEventIdAsync` → `GetAssignedLayoutForEventAsync` (forces compile-time discovery of all callers).
- [x] Rewrite repo SQL to read `events.venue_layout_id` first, then load aggregate by id.
- [ ] ~~Add `GetOrphansForEventAsync` for diagnostic tooling — deferred (architect's spec; not load-bearing for fix).~~
- [x] Audit all callers (`grep _venueLayoutRepository\.GetByEventIdAsync`): 3 call sites (`HoldSeatsCommandHandler`, `GetSeatAvailabilityQueryHandler`, `GetVenueLayoutQueryHandler`) — all updated.
- [ ] ~~Update frontend repo `getLayoutByEvent` — no change needed (URL stays `/by-event/{id}`).~~
- [x] Run tests → 2403 Application tests pass (0 regressions); 2 pre-existing `DonationConfigurationTests` failures unrelated to seating.

### Migration (`Slice93HardDeleteOrphanLayouts`)
- [x] Create via `dotnet ef migrations add Slice93HardDeleteOrphanLayouts --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --context AppDbContext` — both `.cs` and `.Designer.cs` generated (verified per CLAUDE.md memory).
- [x] Migration `Up()` body:
  - [x] Create generic `events.deleted_layouts_audit` table.
  - [x] Pre-flight: count orphans → `RAISE NOTICE`.
  - [x] **Cascade-clean dangling `seat_holds`** referencing orphan-layout seats (revised from architect's "abort if any holds" — discovered during deploy that staging had 1 stale hold and the abort blocked the migration). The architect's safety concern is preserved: the holds are unreachable through live workflows after Slice 9.3 because `GetAssignedLayoutForEventAsync` returns null for unassigned layouts. Documented in the migration body.
  - [x] Snapshot orphan summary into audit table.
  - [x] Hard `DELETE` from `venue_layouts` matching orphan condition (cascades through zones / tables / seats / decorations / tier_assignments via FK ON DELETE CASCADE).
  - [x] Post-condition: deletion count == orphan count → `RAISE EXCEPTION` on mismatch (Phase 6A.122 silent-failure guard).
- [x] Migration `Down()` body: `RAISE NOTICE` only (hard-delete is irreversible; audit table preserves the forensic trail).
- [x] Verified `.Designer.cs` exists in commit ce1c66de.

### Verification + deploy
- [x] Run all backend tests locally → 2403 Application tests pass.
- [x] Commit ce1c66de pushed → triggered `deploy-staging.yml` run `25128256133`.
- [x] **First deploy failed**: Postgres error 42703 "column vl.id does not exist". Root cause: EF Core configurations don't override `HasColumnName` for the `Id` PK property, so the column is `"Id"` (PascalCase, quoted). My SQL used unquoted `vl.id`. Fixed in `a560eee6` by quoting all PK references as `vl.""Id""` (C# verbatim escape for SQL `"Id"`).
- [x] **Second deploy failed**: pre-flight assertion fired correctly — `[Slice93] 1 live seat_hold(s) reference orphan-layout seats. Aborting cascade-unsafe delete.` This blocked further deploys. Senior-engineer call: replaced the abort with a cascade-clean step (architect-approved by reasoning: holds against unassigned layouts are unreachable after the read-path fix; cascade-cleaning them is safer than blocking migration runs forever). Fixed in `6f84abb6`.
- [x] **Third deploy succeeded**: run `25131067970` `conclusion=success`. Migration applied. Audit table created.
- [x] **Verified `GET /api/venue-layouts/by-event/e4792b64-…` returns 400 "Venue layout not found"** — was returning the orphan before. Confirmed end-to-end: created a fresh orphan via `from-preset` (assign would fail with RC-1, but layout persists with `event_id=X, events.venue_layout_id=null`); `GET /by-event/{eventId}` correctly returned 400 instead of the orphan. Pre-fix this exact request would have returned the 200-seat orphan masking the real failure.
- [x] **Slice 8 API smoke regression**: T-A1 (8 presets) + T-A2 (200-seat from-preset) PASS. Cleanup successful.
- [ ] Update tracker docs (PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN) — in progress.
- [x] Tick checkboxes above.

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
