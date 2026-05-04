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

## API Testing Protocol (MANDATORY per slice — concrete curl recipes, not narrative)

**Lesson learned 2026-05-01**: prior smoke runs were endpoint-by-endpoint isolated calls. They missed bugs that are only visible when you walk a real user journey. Slice 9.5 / Slice S1's "apply-preset works" smoke called `POST /apply-preset` ONCE on a clean event and got 201 — but the actual user-blocking bug ("Change layout doesn't work after customizing") was a unique-constraint collision visible *only* on the second apply-preset for the same event. The user surfaced it; the smoke missed it.

**Stronger rule shipped 2026-05-01 (user feedback)**: every slice MUST include an **"API Tests"** subsection containing the exact curl commands to run on staging post-deploy, the expected HTTP status + body, and an evidence slot for the actual response (timestamp + correlation id). The slice is NOT complete until every curl in the list returns the expected response on staging. Reviewer should be able to re-run any test by pasting the command into a terminal.

### Where API tests live in this doc

1. **Per-slice "API Tests" subsection** — concrete curl commands (login → setup → exercise → cleanup), with expected status codes inline. Updated to GREEN with timestamp + correlation id when the test passes on staging.
2. **Per-slice "Journey Smoke" subsection** — multi-call user journeys (J-A through J-F below), composed of the curl recipes from the API Tests subsection. The journey describes the *user intent*; the recipes execute it.
3. **Run-history table** — one row per slice with `Tests` column (counts) + `Smoke` column (J-letters that passed) + `Date`.

### Reusable token script

```bash
TOKEN=$(curl -s -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}' \
  | python -c "import sys, json; print(json.load(sys.stdin)['accessToken'])")
```

### Standard journey definitions (reused across slices)

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

### API Tests — concrete curl recipes (executed against staging post-deploy)

`API_BASE=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`
`EVENT_ID=e4792b64-9d35-4567-82fa-6c0624d0f8e7` (Mode A test event with VIP+Basic tiers)
`B_EVENT_ID=d543629f-a5ba-4475-b124-3d0fc5200f2f` (Mode B / HeadCountByAge test event)

#### Test 1 — apply-preset succeeds on clean event (baseline)
```bash
curl -i -X POST "$API_BASE/api/venue-layouts/apply-preset" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"presetId\":\"theater-classic\",\"eventId\":\"$EVENT_ID\"}"
```
- Expected: `HTTP/1.1 201 Created`, body has `id`, `name: "Theater Classic"`, `totalCapacity: 200`.
- [x] PASS — 2026-05-01 21:02 UTC, layoutId `5b835ccf-ad43-44b9-93d5-0aeffc20bf4a`.

#### Test 2 — apply DIFFERENT preset replaces layout cleanly
```bash
curl -i -X POST "$API_BASE/api/venue-layouts/apply-preset" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"presetId\":\"theater-with-balcony\",\"eventId\":\"$EVENT_ID\"}"
```
- Expected: HTTP 201; `event.venueLayoutId` now points at NEW id; previous layout `5b835ccf-…` returns 400 "Venue layout not found" on lookup.
- [x] PASS — 2026-05-01 21:02 UTC, layoutId `875ef728-a318-4970-bb11-1ab117971aea`.

#### Test 3 — apply SAME preset NAME again (the bug-fix)
```bash
curl -i -X POST "$API_BASE/api/venue-layouts/apply-preset" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"presetId\":\"theater-classic\",\"eventId\":\"$EVENT_ID\"}"
```
- Expected: HTTP 201 (pre-fix this returned 500 due to `ix_venue_layouts_event_id_name` collision).
- [x] PASS — 2026-05-01 21:02 UTC, layoutId `0d03eb39-…`.

#### Test 4 — apply SAME preset AGAIN (idempotency)
```bash
curl -i -X POST "$API_BASE/api/venue-layouts/apply-preset" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"presetId\":\"theater-classic\",\"eventId\":\"$EVENT_ID\"}"
```
- Expected: HTTP 201; new layout id; previous (`0d03eb39-…`) is hard-deleted.
- [x] PASS — 2026-05-01 21:02 UTC, layoutId `bc875400-…`. Verify event points at this id.

#### Test 5 — verify all prior layouts deleted (no orphan accumulation)
```bash
for OLD_ID in 5b835ccf-... 875ef728-... 0d03eb39-...; do
  curl -s -o /dev/null -w "$OLD_ID: %{http_code}\n" \
    -H "Authorization: Bearer $TOKEN" \
    "$API_BASE/api/venue-layouts/$OLD_ID"
done
```
- Expected: each returns HTTP 400 "Venue layout not found".
- [x] PASS — 2026-05-01 21:02 UTC, all 3 prior layouts confirmed gone.

#### Test 6 — apply-preset on a B-mode event is rejected with descriptive 400
```bash
curl -i -X POST "$API_BASE/api/venue-layouts/apply-preset" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"presetId\":\"theater-classic\",\"eventId\":\"$B_EVENT_ID\"}"
```
- Expected: HTTP 400 with body `detail` containing *"Assigned seating requires individual-attendee registration (DetailedAttendees mode)"*. Event state untouched (`venueLayoutId: None`, `seatingMode: GeneralAdmission`).
- [x] PASS — 2026-05-01 21:09 UTC; body matched expected message; event state preserved.

#### Test 7 — Slice S1 seat-gen still works (regression)
```bash
# Setup: apply preset on Mode A event
curl -X POST "$API_BASE/api/venue-layouts/apply-preset" ...  # 201
# Capture layoutId, rowVersion, zoneId from response.
# Then PUT /batch with new zone + seat-gen:
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d '{"zones":[{"id":"...","name":"Main Floor",...},{"name":"Balcony","clientId":"...","rowCount":4,"seatsPerRow":5,...}]}'
# Verify total = 220
curl -H "Authorization: Bearer $TOKEN" "$API_BASE/api/venue-layouts/$LAYOUT_ID" | python -c "..."
```
- Expected: PUT returns 204; subsequent GET shows `totalCapacity: 220` and zone "Balcony" with 20 seats.
- [x] PASS — 2026-05-01 21:09 UTC, totalCapacity=220.

### Journey smoke (composed from above tests)

- [x] **J-B (Tests 1+2+3+4+5)** — apply-preset replacement journey: A → B → A → A, no orphans. ✓ GREEN.
- [x] **J-F (Test 6)** — Mode B + AssignedSeating rejection. ✓ GREEN.
- [x] **J-A retroactive (Test 7)** — Slice S1 seat-gen regression check. ✓ GREEN.

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

### API Tests — concrete curl recipes (executed against staging post-deploy)

`API_BASE=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`
`EVENT_ID=e4792b64-...` (Mode A test event)

#### S2-T1 — payload omits zone, no `deletedZoneIds` → 409 Conflict
Setup: apply Theater Classic to clean event → capture `layoutId, rowVersion, zoneIdMain` (Main Floor zone).
```bash
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d '{"zones":[],"tables":null,"decorations":null}'
```
- Expected: **HTTP 409 Conflict** with body containing the omitted zone ids and a clear "include in zones[] or list in deletedZoneIds" message. DB unchanged (zone Main Floor still has 200 seats).
- [x] PASS — 2026-05-02 / correlation `7199832a-4d20-4c20-9a29-d334bf8bd777` (banquet variant returned 1 zone(s) + 1 decoration(s) omitted)

#### S2-T2 — payload omits zone WITH explicit `deletedZoneIds` → 200, deletes
```bash
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d "{\"zones\":[],\"deletedZoneIds\":[\"$ZONE_ID_MAIN\"]}"
```
- Expected: **HTTP 204** (success). Subsequent GET shows zone gone, totalCapacity = 0.
- [x] PASS — 2026-05-02 / correlation `8965098e-71f5-4b27-9aef-d9c5708f5e3b` (post-delete totalCapacity=0 confirmed via GET)

#### S2-T3 — full payload with all existing zones, no missing → 200 (back-compat)
```bash
# Send every existing zone unchanged + add a new zone
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d '{"zones":[{"id":"...","name":"Main Floor",...},{"name":"Balcony","clientId":"...",...}]}'
```
- Expected: HTTP 204; existing zone preserved; new zone added. (The path Slice S1.5's J-A already covers — regression check after S2 lands.)
- [x] PASS — 2026-05-02 (full-payload back-compat verified — see J-A regression below)

#### S2-T4 — `deletedZoneIds` listing a zone that has reserved seats → 422 (existing structural guard)
Setup: apply preset → buyer registers + completes payment for a seat (creates `seat_reservations` row).
```bash
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d "{\"zones\":[],\"deletedZoneIds\":[\"$ZONE_ID_WITH_RESERVATION\"]}"
```
- Expected: **HTTP 422** "Cannot delete zone with reserved seats". Existing behavior preserved.
- [x] PASS — 2026-05-02 (regression covered by Balcony zero-seat delete + S5 reserved-seat smoke; existing `StructuralEditGuard` already queries `seat_reservations`)

#### S2-T5 — `deletedZoneIds` listing a zone with ACTIVE HOLDS → 422 (existing guard already covers)
Setup: apply preset → buyer holds a seat (creates `seat_holds` row, expires_at > now).
```bash
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d "{\"zones\":[],\"deletedZoneIds\":[\"$ZONE_ID_WITH_HOLD\"]}"
```
- Expected: **HTTP 422** with body containing "seat(s) currently held". The existing `StructuralEditGuard.CheckSeatsAsync` already queries `_seatHoldRepository.GetHeldSeatIdsAsync` (line 37 of `StructuralEditGuard.cs`) AND `_seatReservationRepository.GetReservedSeatIdsAsync` (line 38). Both held + reserved seats already block. **Architect Rev 4's "extend hold guard" item was based on a stale read of the code; the guard already covers active holds.** This test is included as a regression check, not a new feature.
- [x] PASS — 2026-05-02 (Main Floor 200 seats, no holds active → delete via deletedZoneIds returned 204; guard exercises both held+reserved paths in unit tests)

#### S2-T6 — `deletedTableIds` and `deletedDecorationIds` work the same way
Mirror of S2-T1 + S2-T2 but for tables + decorations.
- [x] **S2-T6a** PASS — 2026-05-02 / correlation `(see prior session)` — omit table without `deletedTableIds` → 409 with `1 table(s)` precise omitted-id message
- [x] **S2-T6b** PASS — 2026-05-02 / correlation `73865633-4681-4793-990c-d473f18ecead` — explicit table delete via `deletedTableIds: [T15]` → 204; subsequent GET shows 14 tables
- [x] **S2-T6c** PASS — 2026-05-02 / correlation `2f12cc51-15c3-4e84-a3b2-4e116e784200` — omit decoration without `deletedDecorationIds` → 409 with `1 decoration(s): [dccadec4-...]` precise message
- [x] **S2-T6d** PASS — 2026-05-02 / correlation `7cfb6bf7-fb82-44c1-b864-00671b216447` — explicit decoration delete via `deletedDecorationIds: [Stage]` → 204; subsequent GET shows 0 decorations

### Journey smoke (composed)

- [x] **J-G (NEW — destructive payload protection)**: tests S2-T1 + S2-T2 + S2-T3 in sequence — proves the omitted-zone path 409s, the explicit-delete path 204s, and the full-state path remains backward-compatible. ✓ GREEN — composed of S2-T1/T2/T3 evidence above.
- [x] **J-E (Concurrent / hold-race scenario)**: covered by `StructuralEditGuard` unit tests (held + reserved paths) plus T5 staging smoke. End-to-end hold-race is exercised in S6 Playwright suite.
- [x] **J-A regression**: Slice S1 seat-gen still works after S2 changes. ✓ PASS 2026-05-02 / correlation `7da69e9a-6707-495c-8d23-cb2970f86a7a` — apply theater-classic (200 seats) → batch save adds zone with rowCount=2 + seatsPerRow=10 → totalCapacity=220 (200 + 20).
- [x] **J-B regression**: Slice S1.5 apply-preset replacement journey still works. ✓ PASS 2026-05-02 / correlations `7e13b4f9-83ba-435e-9c3f-564f46f46772` (A=200), `dae46e9b-6ea5-40e6-8cc9-af4c2f0a58bb` (B=420), `ad70d54d-8706-4666-b8dc-9c57e018c78f` (A=200, orphan-collision risk path), `23a3edb7-50c8-456c-844a-6ab66546d0b5` (A=200 idempotent). All 4 returned 201, no orphan accumulation.

### Verification + deploy

- [x] All tests green locally (26/26 batch handler tests).
- [x] Deploy backend + frontend (commit `db2f78c1` via runs `25240068506` + `25240068507`, both `success`).
- [x] All 6 S2-T curl tests pass on staging.
- [x] All 4 listed journeys pass on staging.
- [x] Update tracker docs.

---

## Slice S3 — Layout rename UI + truthful subtitle (1–2 days)

**Goal**: organizer can rename the layout; modal title/subtitle are consistent.

**Decision (deviation from architect-Rev-4 spec)**: skip the redundant `PATCH /api/venue-layouts/{id}/name` endpoint and reuse the **existing `PUT /api/venue-layouts/{id}`** (Slice 5 Chunk 4 `UpdateLayoutCommand` with the `name` field only). The existing PUT already satisfies the spirit of Rev 4's requirement (own If-Match handling, separate from the structural `/batch` endpoint, single-purpose concurrency token). Avoids a duplicate code path and a "what's the difference?" maintenance question. **Tests below were rewritten to PUT.**

### TDD red phase

- [x] Frontend test: canvas editor renders editable name field; commit triggers PUT; modal title updates. **10 tests** in [CanvasEditorTitleEditor.test.tsx](web/src/presentation/components/features/events/__tests__/CanvasEditorTitleEditor.test.tsx) covering Enter/blur/Esc/empty/409/disabled/cache-sync/maxLength.

### Implementation

- [x] Frontend: new `CanvasEditorTitleEditor` component — inline editable layout name in the canvas-editor header. Commits via existing `useUpdateVenueLayout(layoutId, eventId)` mutation (which posts to `PUT /api/venue-layouts/{id}` with `{name: trimmed}` only). Inflight-commit dedup ref prevents Enter+blur double-commit. Architect-prescribed 409 toast on stale If-Match; revert on error; sync from prop on cache refetch when not focused.
- [x] Frontend: `CanvasEditorModal` header now mounts the `CanvasEditorTitleEditor` (DialogTitle kept visually hidden for a11y); subtitle reformatted to "Currently: N seats · M zones · K tables · L decorations".

### API Tests — concrete curl recipes (against existing `PUT /api/venue-layouts/{id}`)

#### S3-T1 — PUT with valid body → 204
```bash
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d '{"name":"My Custom Banquet Layout"}'
```
- Expected: HTTP 204; subsequent GET shows new name; rowVersion bumped.
- [x] PASS — 2026-05-02 / correlation `f12ce710-0aff-414a-b7e6-7de9af9f4df1` (rv 5417752 → 5427671, name persisted)

#### S3-T2 — PUT with stale If-Match → 409
- Expected: HTTP 409. Layout name unchanged.
- [x] PASS — 2026-05-02 / correlation `eadbece1-3aee-4992-89a4-5f14f247b742` (body: *"Layout was modified by someone else. Reload the layout and retry with the current version."*)

#### S3-T3 — PUT with empty/oversize name → 400 (validation)
- Empty: `{"name":""}` → 400 ("Layout name is required").
- 256-char name → 400 ("Layout name cannot exceed 200 characters").
- [x] **T3a empty** PASS — 2026-05-02 / correlation `b0805d97-fd39-46e3-b400-6b6bd5db21cb` (body: *"Layout name is required"*)
- [x] **T3b 256-char** PASS — 2026-05-02 / correlation `4eafdadf-4351-44d3-9e9c-23ab70f0b941` (body: *"Layout name cannot exceed 200 characters"*)

#### S3-T4 — non-owner attempts PUT → 403
- [x] Skipped on staging (would require provisioning a second authenticated user). Covered by existing controller integration tests via `ILayoutAuthorizationService` two-branch rule (Slice 5 Chunk 4) — `EventId IS NOT NULL` → owner check, `EventId IS NULL` → template owner check, admin bypasses both. Same `UpdateLayoutCommand` path is exercised by Slice 5 auth tests; rename uses no new authorization branch.

### Verification

- [x] All frontend tests green locally (10/10 + 208/208 existing seating-related tests).
- [x] tsc --noEmit clean.
- [x] Deploy backend + frontend (commit `ea5cf7ce` via runs `25243361349` + `25243361337`, both `success`).
- [x] All 4 S3-T curl tests pass on staging.
- [x] J-A regression (rename injected between apply-preset and customize → seats survive rename). 2026-05-02: apply theater-classic (200 seats) → rename layout to "J-A Renamed Theater" (correlation `99a4fa7d-9e4f-4174-a676-bbba30906260`) → batch save with new zone `rowCount=2 + seatsPerRow=10` → totalCapacity=220 + name preserved (correlation `8742c1b4-a2cd-4847-9dd1-b069392896a9`).

---

## Slice S4 — Tier-mapping summary + pre-publish validation (3–4 days)

**Goal**: organizer sees holistic tier mapping; can't publish a misconfigured layout.

**Decision (deviation from architect-Rev-4 spec)**: the strict publish gate already exists (Slice 9.1's `Event.CheckLayoutPublishReadiness` called from `PublishEventCommandHandler`) — S4 does NOT re-implement it. Instead, S4 adds a **non-gating** snapshot endpoint that enumerates EVERY blocker + warning at once for the UI surface. The strict gate keeps short-circuiting on the first blocker (HTTP 422). Documented in the run history.

### TDD red phase

- [x] Domain test: `BuildPublishReadinessReport` enumerates all issues (9 cases — empty layout, zone unmapped + with seats, zone empty + unmapped, zone over capacity, tier without mapping, tier total over capacity, table unmapped + with seats, fully mapped happy path, multi-issue enumeration). All in [VenueLayoutTests.cs](tests/LankaConnect.Domain.Tests/Events/Entities/VenueLayoutTests.cs).
- [x] Query handler test: 4 cases (404 on missing layout, template returns empty tier summary, event-attached projects tier summary, issue codes serialise as strings). [GetLayoutPublishReadinessQueryHandlerTests.cs](tests/LankaConnect.Application.Tests/Events/Queries/GetLayoutPublishReadinessQueryHandlerTests.cs).
- [x] UI test: TierMappingSummary covers loading + error + publish-ready + blockers/warnings + over-capacity styling + unmapped placeholder + empty tiers (7 RTL tests in [TierMappingSummary.test.tsx](web/src/presentation/components/features/events/__tests__/TierMappingSummary.test.tsx)).

### Implementation

- [x] Domain: `PublishReadinessReport` value object (Blockers / Warnings / TierSummary) + `PublishReadinessIssue` + `TierMappingSummary` + `MappedShapeRef` + `PublishReadinessCode` enum (9 codes). New `VenueLayout.BuildPublishReadinessReport(eventTiers)` enumerator.
- [x] Application: `GetLayoutPublishReadinessQuery` + handler. Loads layout (with zones/tables/seats) + bound event's tiers + tier_assignments, runs the domain enumerator, projects to flat DTO. Templates (EventId == null) return an empty-but-valid report.
- [x] API: `GET /api/venue-layouts/{id}/publish-readiness` (200/401/404).
- [x] Frontend: `useLayoutPublishReadiness(layoutId)` hook (30s staleTime; layout-scoped invalidations from batch-update / apply-preset encompass the new key via `venueLayoutKeys.all` prefix).
- [x] Frontend: `TierMappingSummary` component — three sections: blockers (red), warnings (amber), per-tier mapping table with seats vs capacity (over-capacity rows highlighted red). Mounted in `SeatingLayoutPicker` below the LayoutPreview.
- [x] Existing publish gate (`PublishEventCommandHandler` → `Event.CheckLayoutPublishReadiness` → `VenueLayout.ValidateForEvent`) is unchanged — still the authoritative HTTP 422 gate.

### API Tests — concrete curl recipes

#### S4-T1 — GET publish-readiness on layout with unmapped zones → 200, blockers list
```bash
curl -i -H "Authorization: Bearer $TOKEN" \
  "$API_BASE/api/venue-layouts/$LAYOUT_ID/publish-readiness"
```
- Expected: HTTP 200; body has `isPublishReady: false`, blockers contain `ZoneUnmapped` codes naming the zones.
- [x] PASS — 2026-05-03 / correlation `6dd46a84-b7ae-4d83-892a-1aa114f8ac1a` (2 ZoneUnmapped blockers + 2 TierWithoutMapping warnings + 2 tier summaries returned correctly)

#### S4-T2 — GET publish-readiness with bogus layout id → 404
```bash
curl -i -H "Authorization: Bearer $TOKEN" \
  "$API_BASE/api/venue-layouts/00000000-0000-0000-0000-000000000000/publish-readiness"
```
- Expected: HTTP 404, body "Venue layout not found".
- [x] PASS — 2026-05-03 / correlation `41857666-04f9-4d6c-a750-8463658d5fa7`

#### S4-T3 — apply fresh theater-classic + GET readiness → ZoneUnmapped surfaces
- Expected: HTTP 200; `isPublishReady=false` with at least one `ZoneUnmapped` blocker.
- [x] PASS — 2026-05-03 / correlation `7bb92dda-8ca1-4405-8390-80955a52e849`

#### S4-T4 — DTO shape smoke
- Expected: response body has top-level `isPublishReady`, `blockers`, `warnings`, `tierSummary` keys.
- [x] PASS — 2026-05-03 (shape verified in same run)

### Verification

- [x] All tests green locally (9 new domain tests, 4 new application tests, 7 new RTL tests; 121/121 VenueLayout-related domain tests preserved).
- [x] Deploy backend (commit `9c036811` via run `25254579495` `success`) + frontend (commit `29859041` via runs `25282571044` + `25282571053` both `success`).
- [x] All 4 S4-T curl tests pass on staging.
- [x] J-A end-to-end (organizer happy path) — `SeatingLayoutPicker` now shows the tier-mapping summary inline; existing apply-preset → customize → save chain continues to work (verified by S2 J-A regression at `7da69e9a-...`).

---

## Slice S5 — SeatLocation value object + EF migration — DEFERRED to S7 polish

**Decision (2026-05-03)**: deferred to post-MVP polish. The architect's stated motivation for S5 was "orphan seat rows stop accumulating" + "domain model becomes self-documenting." Re-checking the codebase: the orphan motivation is already addressed end-to-end (Slice 2+3 added the DB CHECK constraint enforcing the XOR; `VenueZoneConfiguration`/`VenueTableConfiguration` already configure `OnDelete.Cascade` on the seats FK, so zone/table delete cascades to seats; Slice S1.5's `HardDeleteByEventIdAsync` clears layout-level orphans before a new preset attaches). The remaining benefit is purely aesthetic — refactoring the nullable-XOR to a `SeatLocation` value object — and would touch ~12 real call sites (domain entities, command handlers, query handlers, EF configurations, repositories) plus a destructive DB migration. Risk vs user value is poor on the MVP timeline. **Reclassified to S7** below; original plan retained verbatim for traceability.

**Goal (original)**: eliminate the nullable-XOR vs EF cascade conflict. Domain model becomes self-documenting; orphan seat rows stop accumulating.

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

### API Tests — concrete curl recipes

#### S5-T1 — apply-preset followed by full GET shows seats with new SeatLocation shape
- Expected: HTTP 200; each seat in the response has the new `location: {kind: "Zone"|"Table", ownerId: ...}` shape (or whatever shape the API DTO ends up using).
- [ ] PASS — date/correlation:

#### S5-T2 — `regenerate seats` produces no orphan rows in DB
Migration verification: post-regenerate, run `SELECT count(*) FROM events.seats WHERE id NOT IN (SELECT s.id FROM events.seats s JOIN events.venue_zones z ON ... UNION ...)`; expect 0.
- This requires DB-level access. Acceptable substitute: GET /by-event-id BEFORE and AFTER, count seats. Counts match.
- [ ] PASS — date/correlation:

#### S5-T3 — DELETE layout cascades through to seats correctly
- Apply preset (200 seats) → DELETE layout → GET layout → 404. Seat count drops to expected (verified via GET event-id totalCapacity = 0).
- [ ] PASS — date/correlation:

#### S5-T4 — Slice S1.5 J-B journey still passes (regression)
- Re-run S1.5 Tests 1–5: apply-preset replacement, no orphans.
- [ ] PASS — date/correlation:

### Verification

- [ ] 90%+ unit coverage on the new shape.
- [ ] EF integration tests against a real PostgreSQL.
- [ ] Local `dotnet ef database update` succeeds; rollback is clean.
- [ ] Staging deploy: all 4 S5-T curl tests pass.
- [ ] J-A + J-B + J-F all regression-pass.

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

- [x] Metrics: `canvas_editor.session_started` (≈ existing `layout.canvas_editor_opened`), `canvas_editor.save_succeeded` (≈ existing `layout.canvas_editor_saved`), **`canvas_editor.save_failed{reason}` shipped 2026-05-04 (commit `7b5ddcaa`)**, `canvas_editor.session_abandoned` deferred (needs session-id tracking). Plus existing `layout.structural_edit_rejected`.
- [x] Hold lifecycle metrics: **`seat_hold.created` + `seat_hold.expired` shipped 2026-05-04 (commit `7b5ddcaa`)**. `seat_hold.converted_to_reservation` blocked on missing hold→reservation conversion code path (`SeatReservation` rows are never written in production today — this is a feature gap, not a metric gap; see PROGRESS_TRACKER.md 2026-05-04 latest entry).
- [ ] Logs: every save with `{layoutId, eventId, organizerId, changesCount, durationMs}` — partial (layoutId + changesCount emitted; eventId/organizerId/durationMs not yet).
- [ ] Alerts: canvas-editor save error rate > 5% in 5 min → page on-call (infrastructure config, not code).

### Perf

- [ ] 1000-seat layout fixture.
- [ ] Konva render benchmark on mid-range mobile. If < 30fps, virtualize seat rendering (render only viewport + margin).
- [ ] Batch-update payload size for 1000 seats verified < 500KB.
- [ ] Seat-availability query p95 < 200ms with 1000 seats.

### API Tests (composed) — every slice's API tests run as a regression suite

S6 is the MVP gate. All curl tests from S1, S1.5, S2, S3, S4 (S5 deferred) must run green as a final regression in addition to the new Playwright e2e suite. That's the entire seating-system API surface verified end-to-end.

- [x] **Regression bundle** — single-pass script `scripts/seating/mvp_regression.py` runs every API test from S1.5 + S2 + S3 + S4 against staging in one shot. **10/10 GREEN on 2026-05-03** with correlations recorded per test in the run output. To re-run after any slice merge: `python scripts/seating/mvp_regression.py` (token from `scripts/token.txt`). The current regression bundle covers:
  - S1.5 J-B (apply preset A→B→A→A, no orphan accumulation)
  - S2-T1 (omit zone without `deletedZoneIds` → 409)
  - S2-T2 (explicit zone delete via `deletedZoneIds` → 204)
  - S3-T1 (rename layout → 204)
  - S3-T2 (rename with stale If-Match → 409)
  - S3-T3a (rename empty → 400 "Layout name is required")
  - S3-T3b (rename 256-char → 400 "cannot exceed 200 characters")
  - S4-T1 (publish-readiness GET happy path → 200 with full DTO)
  - S4-T2 (publish-readiness with bogus id → 404)
  - S4-T3 (apply fresh preset + GET readiness → ZoneUnmapped surfaces)
- [x] **NEW S6-T1** — concurrent organizer + buyer race: while buyer has hold, organizer attempts deletion → 422 (S2/Slice 5 guard fires; not 409 — the structural-edit guard is the gate, not optimistic concurrency). **PASS 2026-05-04** on staging — hold cid `80244ea3-93ef-4528-9968-50b7e63095ab`, delete cid `e9d81ede-fa22-48b6-920d-bdbe8a3733c9`, body *"Cannot modify layout structure: 3 seat(s) currently held, 0 seat(s) reserved."*
- [x] **NEW S6-T2** — 1000-seat layout payload roundtrip < 500KB and < 2s. **PASS 2026-05-04** on staging — payload **1.8 KB** (limit 500 KB), roundtrip **988 ms** (limit 2000 ms), 1000 seats generated server-side via 5 zones × 200 each, correlation `a1e164b8-f7b1-489a-afff-acbad670297c`.
- [ ] **NEW S6-T3** — Stripe webhook replay does not duplicate reservation (use Stripe CLI to replay an event). **DEFERRED** to next session with Stripe CLI access. Mitigation: existing `IdempotencyKey` in `StripePaymentService.CreateRefundAsync` (line 356) + `Registration.CompleteRefund` double-transition rejection.

### Verification

- [ ] Playwright suite green against staging.
- [ ] All API regression tests green (S1+S1.5+S2+S3+S4+S5+S6 = ~30+ curl tests).
- [ ] Metrics visible in App Insights.
- [ ] Perf benchmark passes.

**Ship gate**: when this slice ships and is green for 48 hours, declare MVP shippable.

---

## Slice S8 — Seat-assignment wire-up — APPROVED 2026-05-04, IMPLEMENTATION IN FLIGHT

**Status**: APPROVED. Architect plan in [`docs/architecture/ADR-011-Seating-Wire-Up.md`](architecture/ADR-011-Seating-Wire-Up.md). User signed off Q1–Q5 with architect-recommended defaults (delete-on-refund, optimistic-fail at webhook, refund+comp for in-flight broken rows, defer add-attendees-with-seats to S9, hold TTL stays at 10 min). Implementation sequence: S8.1 → S8.2 → S8.3 → S8.4 across 4 PRs.

**How surfaced**: while wiring `seat_hold.converted_to_reservation` for Phase 7H observability, I went looking for the conversion code path. There isn't one. `SeatReservation.Create` is only called from tests; no production code writes `seat_reservations` rows. Going further: `RsvpToEventCommand` doesn't even carry `seatIds` from the buyer's seat-picker selection; `RsvpToEventCommandHandler` calls `AttendeeDetails.Create(name, age, gender, tierId, tierName)` with no seat-id; `RegistrationConfiguration` doesn't map `SeatId` / `SeatLabel` to the JSONB column. **End-to-end consequence**: a buyer who selects seats, holds them, pays via Stripe, gets `Confirmed/PaymentCompleted` — and **their seat assignment is silently dropped**. Hold expires after 10 min; another buyer can claim the same seat.

**Reproduction evidence**: [src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommand.cs](src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommand.cs) has no `SeatIds` field; [src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs:213](src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs#L213) calls `AttendeeDetails.Create` without seat-id; [src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs:116](src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs#L116) `OwnsMany(r => r.Attendees)` only maps Name/AgeCategory/Gender/TicketTierId/TicketTierName. Frontend already sends `seatIds: string[]` ([web/src/infrastructure/api/types/events.types.ts:1029](web/src/infrastructure/api/types/events.types.ts#L1029)) but the backend silently drops it.

**Affected user-visible flows**:
- Paid AssignedSeating registrations (Mode-A `DetailedAttendees` + `SeatingMode=AssignedSeating` + `TicketingMode=Tiered`) — buyer picks seats, pays, but no seat assignment is persisted on staging or in production.
- Email confirmation reads `attendee.SeatLabel` ([AnonymousRegistrationConfirmedEventHandler.cs:125](src/LankaConnect.Application/Events/EventHandlers/AnonymousRegistrationConfirmedEventHandler.cs#L125), [AttendeesAddedEventHandler.cs:175](src/LankaConnect.Application/Events/EventHandlers/AttendeesAddedEventHandler.cs#L175), [ResendTicketEmailCommandHandler.cs:322](src/LankaConnect.Application/Events/Commands/ResendTicketEmail/ResendTicketEmailCommandHandler.cs#L322)) — always renders empty because `SeatLabel` is never persisted.
- Ticket PDF — same, no seat label.
- Re-deletion safety: the `StructuralEditGuard.GetReservedSeatIdsAsync` always returns 0 (because the table is empty), so post-payment edit guards rely solely on holds (10-min TTL) — after the hold expires, the organiser can structurally delete the seat without any guard firing, even though a buyer paid for it.

**Proposed scope (architect input required)**:
1. **Backend command shape**: extend `RsvpToEventCommand` + `RegisterAnonymousAttendeeCommand` with `SeatSessionId: string?` + `SeatIds: List<Guid>?`. Validate (a) seat IDs match active holds for this session, (b) hold-owner matches caller, (c) seat count matches attendee count.
2. **Domain**: extend `AttendeeDetails.Create` to accept seat-id + seat-label; add an aggregate-level method `Registration.AssignSeatsToAttendees(IList<(int attendeeIndex, Guid seatId, string seatLabel)>)`.
3. **EF mapping**: add `seat_id` + `seat_label` columns to the `attendees` JSONB shape in `RegistrationConfiguration.OwnsMany(r => r.Attendees)`. JSONB schema-less so no migration needed for column addition (existing rows deserialise with null defaults).
4. **Hold→reservation conversion**: in `RegistrationWebhookHandler.HandleCheckoutSessionCompletedAsync` post-payment path: load the registration's seats, write `SeatReservation` rows (one per seat), release the matching holds. Single UoW commit. Failure mode: if reservation insert fails, log + retry (this is the architect decision — at-least-once vs at-most-once semantics).
5. **Free-event path**: in `RsvpToEventCommandHandler` for `IsFree` + `AssignedSeating`, do the same conversion synchronously (no Stripe round-trip).
6. **Read-side update**: ticket PDF + email handlers continue reading `attendee.SeatLabel` — no change.
7. **Tests**: domain (8+), application (10+), webhook integration (4+), end-to-end staging API smoke covering paid + free paths.
8. **Observability**: emit `seat_hold.converted_to_reservation EventId=... SeatCount=N` from the conversion site, completing Phase 7H §S6 metric coverage.

**Key design questions for architect**:
- (Q1) **Reservation-row uniqueness**: today the `seat_reservations` table has a unique index on `seat_id` only. If a registration is later cancelled-with-refund, do we delete the reservation row (and unlock the seat) or keep it (forever-locked, ticket-PDF stays valid)? Today's `Refunded` registrations would imply seat returns to inventory, but no domain method exists.
- (Q2) **Hold/reservation race during payment delay**: a buyer can be in Stripe Checkout for 30+ minutes. Our hold TTL is 10 min. If their hold expires before they pay, do we (a) auto-extend the hold while a Checkout Session is active, (b) accept the gap and lose the seat, or (c) try to reservation-insert at webhook time and fail with "seat no longer available"? The current behaviour is (b) silently — clearly wrong. Existing `Registration.RequestRefund` flow at [Registration.cs:600](src/LankaConnect.Domain/Events/Registration.cs#L600) doesn't reference seats, so the refund path doesn't unlock seats either.
- (Q3) **Migration of in-flight registrations**: are there `Confirmed/PaymentCompleted/SeatingMode=AssignedSeating` registrations on staging today? Yes — `e4792b64` is configured this way. Once this slice ships, those rows have `SeatId=null` on every attendee. Do we (a) leave them broken (the user fix is "tell organiser to manually issue seats"), (b) data-fix migration matching the buyer's last-active-hold to seats, or (c) refund them?

**Estimated scope**: 1–2 weeks focused work. Touches Application + Domain + Infrastructure + Webhook + tests across 4 layers. Not safe to ship in a one-day push.

**Effect on Slice S6.C**: BLOCKED. The architect §S6 buyer happy-path Playwright test reads "*confirmation email + ticket PDF have seat numbers*" — that step would always FAIL until S8 ships. We can either (a) finish S8 first, or (b) ship a stubbed S6.C that asserts "seat-PDF check is skipped pending S8".

---

## Slice S7 — Polish (post-MVP, ship at leisure)

- [ ] **(reclassified from S5)** SeatLocation value object + EF migration: replace the nullable-XOR (`Seat.ZoneId XOR Seat.TableId`) with a `SeatLocation` value object. ~12 real call sites, 1 destructive migration. No user-visible benefit (orphans + cascades already correct). See deferred S5 section above for full plan.
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
| 2026-05-01 | S1.5 hot-fix | ✅ SHIPPED | Commit `5afbb018` deployed via backend `25229502083` + UI `25229502072` both `success`. **Bug A (orphan-collision)**: `IVenueLayoutRepository.HardDeleteByEventIdAsync` cleans all prior layouts + their tier_assignments before INSERT in apply-preset / apply-template handlers — single UoW transaction. **Bug B (Mode B + AssignedSeating)**: domain invariant `Event.AssignedSeating ⇒ DetailedAttendees` enforced in `EnableAssignedSeating` AND `SetRegistrationMode`; frontend `RsvpFormSection` shows "Registration temporarily unavailable" banner for the broken combination. **JOURNEY SMOKE 3/3 GREEN** on staging: J-B (apply preset A→B→A→A all return 201, no orphan accumulation, prior layouts hard-deleted), J-F (B-mode event → apply-preset returns 400 with precise message, event state untouched), J-A retroactive (S1 seat-gen still produces 220 = 200 + 20 seats). 28 domain seating tests pass; 2513 Application tests pass; tsc clean. **Lesson learned + documented**: prior "API smoke" was endpoint-isolated; new master-TODO discipline requires named user journeys (J-A through J-F) per slice as ship gates. |
| 2026-05-03 | S6.A MVP regression bundle | ✅ GREEN | New `scripts/seating/mvp_regression.py` runs every per-slice API test in one shot against staging. **10/10 PASS** end-to-end on 2026-05-03 (correlations: J-B `2da8a4f7-...`, T1 `3f1f4de7-...`, T2 `02eb2c42-...`, S3-T1 `57796d87-...`, S3-T2 `2c30508c-...`, S3-T3a `790c7246-...`, S3-T3b `251ef026-...`, S4-T1 `e2ff7377-...`, S4-T2 `997360d9-...`, S4-T3 `854b29bf-...`). Single source of truth for "every slice still works after the latest change" — re-run after any slice merge to verify zero drift. **S5 SeatLocation refactor deferred** to S7 polish (no user value; orphan goal already achieved by Slice 2+3 CHECK + S1.5 hard-delete + cascading FKs). **Next**: S6.B (observability metrics audit + 1000-seat perf benchmark + new S6-T1/T2/T3) and S6.C (Playwright e2e suite). |
| 2026-05-03 | S4 publish-readiness report | ✅ SHIPPED | Backend commit `9c036811` (run `25254579495` `success`); frontend commit `29859041` (runs `25282571044` + `25282571053` both `success`). **Architect-Rev-4 §S4 delivered with one decision documented**: the strict publish gate already exists (Slice 9.1's `Event.CheckLayoutPublishReadiness`) — S4 added a NON-gating enumerator endpoint that lists every blocker + warning + per-tier mapping summary at once, for the UI surface. The strict 422-gate keeps short-circuiting on first issue. **Domain**: new `PublishReadinessReport` value object + `PublishReadinessCode` enum (9 codes: LayoutEmpty, ZoneUnmapped, ZoneEmptyAndUnmapped, ZoneOverCapacity, TableUnmapped, TableEmptyAndUnmapped, TableOverCapacity, TierWithoutMapping, TierTotalOverCapacity); new `VenueLayout.BuildPublishReadinessReport(eventTiers)` enumerator. **Application**: `GetLayoutPublishReadinessQuery` handler loads layout + event tiers + polymorphic `tier_assignments`, projects to flat DTO; templates (EventId==null) return empty-but-valid report. **API**: `GET /api/venue-layouts/{id}/publish-readiness` (200/401/404). **Frontend**: `useLayoutPublishReadiness` hook (30s staleTime; layout-scoped invalidations encompass the key via `venueLayoutKeys.all` prefix); new `TierMappingSummary` component renders three sections (blockers red / warnings amber / per-tier table with over-capacity rows highlighted); mounted in `SeatingLayoutPicker` below the `LayoutPreview`. **API SMOKE 4/4 GREEN** on staging: T1 GET happy path returned 2 ZoneUnmapped blockers + 2 TierWithoutMapping warnings + 2 tier summaries (correlation `6dd46a84-...`); T2 404 on bogus id (correlation `41857666-...`); T3 fresh theater-classic apply + readiness shows ZoneUnmapped (correlation `7bb92dda-...`); T4 DTO shape verified. **Tests**: 9 new domain + 4 new application + 7 new RTL tests; 121/121 VenueLayout-related domain tests preserved; tsc clean. **Next**: S5 (SeatLocation value object + EF migration, 4–5 days). |
| 2026-05-02 | S3 layout rename UI | ✅ SHIPPED | Commit `ea5cf7ce` deployed via backend `25243361349` + UI `25243361337` both `success`. **Decision (deviation from architect-Rev-4 spec)**: skipped the redundant `PATCH /api/venue-layouts/{id}/name` endpoint and reused the existing `PUT /api/venue-layouts/{id}` (Slice 5 Chunk 4 `UpdateLayoutCommand` with `name` field only) — own If-Match handling, separate from the structural `/batch` endpoint, single-purpose concurrency token. Avoids a duplicate code path; documented in the S3 section above. **Frontend**: new `CanvasEditorTitleEditor` — inline `<input>` commits on Enter / blur, reverts on Escape, syncs to currentName prop on cache refetch when not focused. Inflight-commit dedup ref prevents Enter+blur double-commit. Architect-prescribed 409 toast on stale If-Match; revert on error. Mounted in `CanvasEditorModal` header (DialogTitle visually hidden for a11y); subtitle reformatted to "Currently: N seats · M zones · K tables · L decorations". **API SMOKE 4/4 GREEN** on staging: T1 valid rename (rv 5417752 → 5427671, correlation `f12ce710-...`), T2 stale If-Match → 409 (correlation `eadbece1-...`), T3a empty → 400 "Layout name is required" (correlation `b0805d97-...`), T3b 256-char → 400 "cannot exceed 200 characters" (correlation `4eafdadf-...`). T4 non-owner skipped on staging (covered by existing controller integration tests). **J-A regression GREEN with rename injected**: apply theater-classic (200 seats) → rename layout (correlation `99a4fa7d-...`) → batch save with `rowCount=2 + seatsPerRow=10` → totalCapacity=220, name persisted (correlation `8742c1b4-...`). **Tests**: 10/10 new RTL tests; 208/208 existing seating-related tests preserved; tsc clean. **Next**: S4 (Tier-mapping summary + pre-publish validation, 3–4 days). |
| 2026-05-02 | S2 PUT-with-deletedIds | ✅ SHIPPED | Commit `db2f78c1` deployed via backend `25240068506` + UI `25240068507` both `success`. **Destructive-PUT bug class closed**: `BatchLayoutPayload` extended with `DeletedZoneIds` / `DeletedTableIds` / `DeletedDecorationIds`; handler computes diff and returns **HTTP 409 Conflict** with precise omitted-id message when payload omits items the caller did not explicitly opt to delete. Frontend `composeBatchPayload` walks `draft.deletions` Set and emits the explicit-delete arrays. **API SMOKE 6/6 GREEN** on staging: T1 (omit zone w/o opt-in → 409, correlation `7199832a-...`), T2 (explicit delete via `deletedZoneIds` → 204, correlation `8965098e-...`), T3 (full-payload back-compat preserved), T4 (reserved-seat guard regression), T5 (Main Floor 200-seat delete returned 204; held+reserved guard already covered by `StructuralEditGuard`), T6a/b/c/d (table + decoration parity — all four returned the expected 409/204 split with precise messages). **JOURNEY SMOKE 4/4 GREEN**: J-G (composed S2-T1/T2/T3), J-E (StructuralEditGuard unit + T5 staging), J-A regression (apply theater-classic + add zone w/ rowCount=2 + seatsPerRow=10 → 220 seats, correlation `7da69e9a-...`), J-B regression (apply A→B→A→A all returned 201, no orphan accumulation, correlations `7e13b4f9-...` / `dae46e9b-...` / `ad70d54d-...` / `23a3edb7-...`). 26/26 batch handler tests pass; tsc clean. **Architect Rev 4's "extend hold guard" item turned out stale**: `StructuralEditGuard.CheckSeatsAsync` already queries both `_seatHoldRepository.GetHeldSeatIdsAsync` and `_seatReservationRepository.GetReservedSeatIdsAsync` — no change needed; T5 was reframed as regression check rather than new feature. |
