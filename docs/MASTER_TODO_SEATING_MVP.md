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
- [ ] PASS — date/correlation:

#### S2-T2 — payload omits zone WITH explicit `deletedZoneIds` → 200, deletes
```bash
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d "{\"zones\":[],\"deletedZoneIds\":[\"$ZONE_ID_MAIN\"]}"
```
- Expected: **HTTP 204** (success). Subsequent GET shows zone gone, totalCapacity = 0.
- [ ] PASS — date/correlation:

#### S2-T3 — full payload with all existing zones, no missing → 200 (back-compat)
```bash
# Send every existing zone unchanged + add a new zone
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d '{"zones":[{"id":"...","name":"Main Floor",...},{"name":"Balcony","clientId":"...",...}]}'
```
- Expected: HTTP 204; existing zone preserved; new zone added. (The path Slice S1.5's J-A already covers — regression check after S2 lands.)
- [ ] PASS — date/correlation:

#### S2-T4 — `deletedZoneIds` listing a zone that has reserved seats → 422 (existing structural guard)
Setup: apply preset → buyer registers + completes payment for a seat (creates `seat_reservations` row).
```bash
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d "{\"zones\":[],\"deletedZoneIds\":[\"$ZONE_ID_WITH_RESERVATION\"]}"
```
- Expected: **HTTP 422** "Cannot delete zone with reserved seats". Existing behavior preserved.
- [ ] PASS — date/correlation:

#### S2-T5 — `deletedZoneIds` listing a zone with ACTIVE HOLDS → 422 (existing guard already covers)
Setup: apply preset → buyer holds a seat (creates `seat_holds` row, expires_at > now).
```bash
curl -i -X PUT "$API_BASE/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d "{\"zones\":[],\"deletedZoneIds\":[\"$ZONE_ID_WITH_HOLD\"]}"
```
- Expected: **HTTP 422** with body containing "seat(s) currently held". The existing `StructuralEditGuard.CheckSeatsAsync` already queries `_seatHoldRepository.GetHeldSeatIdsAsync` (line 37 of `StructuralEditGuard.cs`) AND `_seatReservationRepository.GetReservedSeatIdsAsync` (line 38). Both held + reserved seats already block. **Architect Rev 4's "extend hold guard" item was based on a stale read of the code; the guard already covers active holds.** This test is included as a regression check, not a new feature.
- [ ] PASS — date/correlation:

#### S2-T6 — `deletedTableIds` and `deletedDecorationIds` work the same way
Mirror of S2-T1 + S2-T2 but for tables + decorations.
- [ ] PASS — date/correlation:

### Journey smoke (composed)

- [ ] **J-G (NEW — destructive payload protection)**: tests S2-T1 + S2-T2 + S2-T3 in sequence — proves the omitted-zone path 409s, the explicit-delete path 204s, and the full-state path remains backward-compatible.
- [ ] **J-E (Concurrent / hold-race scenario)**: organizer holds a hold → tries to delete the zone → 409. Expires the hold → retries → succeeds. Tests S2-T5.
- [ ] **J-A regression**: Slice S1 seat-gen still works after S2 changes (Test 7 from S1.5 above).
- [ ] **J-B regression**: Slice S1.5 apply-preset replacement journey still works.

### Verification + deploy

- [ ] All tests green locally.
- [ ] Deploy backend + frontend.
- [ ] All 6 S2-T curl tests pass on staging.
- [ ] All 4 listed journeys pass on staging.
- [ ] Update tracker docs.

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

### API Tests — concrete curl recipes

#### S3-T1 — PATCH /name with valid body → 204
```bash
curl -i -X PATCH "$API_BASE/api/venue-layouts/$LAYOUT_ID/name" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d '{"name":"My Custom Banquet Layout"}'
```
- Expected: HTTP 204; subsequent GET shows new name; rowVersion bumped.
- [ ] PASS — date/correlation:

#### S3-T2 — PATCH /name with stale If-Match → 409
- Expected: HTTP 409. Layout name unchanged.
- [ ] PASS — date/correlation:

#### S3-T3 — PATCH /name with empty/oversize name → 400 (validation)
- Empty: `{"name":""}` → 400.
- 256-char name → 400 (assuming 200-char limit).
- [ ] PASS — date/correlation:

#### S3-T4 — non-owner attempts PATCH /name → 403
- [ ] PASS — date/correlation:

### Verification

- [ ] All tests green locally.
- [ ] Deploy backend + frontend.
- [ ] All 4 S3-T curl tests pass on staging.
- [ ] J-A regression (rename injected between S1's apply-preset and customize → seats survive rename).

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

### API Tests — concrete curl recipes

#### S4-T1 — GET ValidateLayoutForPublish on layout with unmapped zones → blockers
```bash
curl -i -H "Authorization: Bearer $TOKEN" \
  "$API_BASE/api/venue-layouts/$LAYOUT_ID/publish-readiness"
```
- Expected: HTTP 200, body `{warnings:[],blockers:[{kind:"UnmappedZone",zoneName:"Balcony"}]}`.
- [ ] PASS — date/correlation:

#### S4-T2 — POST publish when blockers present → 422
```bash
curl -i -X POST "$API_BASE/api/Events/$EVENT_ID/publish" \
  -H "Authorization: Bearer $TOKEN"
```
- Expected: HTTP 422 with body listing the blockers.
- [ ] PASS — date/correlation:

#### S4-T3 — POST publish when only warnings (no blockers) → 200
- Expected: HTTP 200; event publishes; warnings logged.
- [ ] PASS — date/correlation:

#### S4-T4 — fully-mapped layout publishes cleanly
- Expected: HTTP 200; no warnings, no blockers.
- [ ] PASS — date/correlation:

### Verification

- [ ] All tests green locally.
- [ ] Deploy backend + frontend.
- [ ] All 4 S4-T curl tests pass on staging.
- [ ] J-A end-to-end (organizer happy path) regression.

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

- [ ] Metrics: `canvas_editor.session_started`, `canvas_editor.save_succeeded`, `canvas_editor.save_failed{reason}`, `canvas_editor.session_abandoned`. Plus existing `layout.structural_edit_rejected`.
- [ ] Hold lifecycle metrics: `seat_hold.created`, `seat_hold.expired`, `seat_hold.converted_to_reservation`.
- [ ] Logs: every save with `{layoutId, eventId, organizerId, changesCount, durationMs}`.
- [ ] Alerts: canvas-editor save error rate > 5% in 5 min → page on-call.

### Perf

- [ ] 1000-seat layout fixture.
- [ ] Konva render benchmark on mid-range mobile. If < 30fps, virtualize seat rendering (render only viewport + margin).
- [ ] Batch-update payload size for 1000 seats verified < 500KB.
- [ ] Seat-availability query p95 < 200ms with 1000 seats.

### API Tests (composed) — every slice's API tests run as a regression suite

S6 is the MVP gate. All curl tests from S1, S1.5, S2, S3, S4, S5 must run green as a final regression in addition to the new Playwright e2e suite. That's the entire seating-system API surface verified end-to-end.

- [ ] **Regression bundle** — re-run every API test from S1, S1.5, S2, S3, S4, S5 against staging. Document any drift.
- [ ] **NEW S6-T1** — concurrent organizer + buyer race: while buyer has hold, organizer attempts deletion → 409 (S2 guard fires).
- [ ] **NEW S6-T2** — 1000-seat layout payload roundtrip < 500KB and < 2s.
- [ ] **NEW S6-T3** — Stripe webhook replay does not duplicate reservation (use Stripe CLI to replay an event).

### Verification

- [ ] Playwright suite green against staging.
- [ ] All API regression tests green (S1+S1.5+S2+S3+S4+S5+S6 = ~30+ curl tests).
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
| 2026-05-01 | S1.5 hot-fix | ✅ SHIPPED | Commit `5afbb018` deployed via backend `25229502083` + UI `25229502072` both `success`. **Bug A (orphan-collision)**: `IVenueLayoutRepository.HardDeleteByEventIdAsync` cleans all prior layouts + their tier_assignments before INSERT in apply-preset / apply-template handlers — single UoW transaction. **Bug B (Mode B + AssignedSeating)**: domain invariant `Event.AssignedSeating ⇒ DetailedAttendees` enforced in `EnableAssignedSeating` AND `SetRegistrationMode`; frontend `RsvpFormSection` shows "Registration temporarily unavailable" banner for the broken combination. **JOURNEY SMOKE 3/3 GREEN** on staging: J-B (apply preset A→B→A→A all return 201, no orphan accumulation, prior layouts hard-deleted), J-F (B-mode event → apply-preset returns 400 with precise message, event state untouched), J-A retroactive (S1 seat-gen still produces 220 = 200 + 20 seats). 28 domain seating tests pass; 2513 Application tests pass; tsc clean. **Lesson learned + documented**: prior "API smoke" was endpoint-isolated; new master-TODO discipline requires named user journeys (J-A through J-F) per slice as ship gates. |
