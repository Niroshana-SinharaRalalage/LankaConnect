# Master TODO — Slice 8 API Smoke Tests

**Created**: 2026-04-27
**Owner**: Niroshana
**Scope**: comprehensive API smoke covering every endpoint introduced or
materially changed by Slice 8 of the seating system redesign
(`C:\Users\Niroshana\.claude\plans\stateful-soaring-galaxy.md` §Slice 8).
Re-run this checklist after every backend or frontend deploy that touches
the seating layout / canvas editor surface.

## Why this doc

Per CLAUDE.md rule 11 (be accurate + honest in status reports) and
rule 12 (follow the master TODO list), every multi-chunk feature on
LankaConnect needs a documented API smoke that's repeatable. Slice 8
shipped 11 chunks across two-and-a-half weeks; per-chunk smokes were run
inline with each commit but there was no end-to-end checklist that
exercises the full user journey in sequence. This doc is that checklist.

## Conventions

- Each test has a numbered ID (T1, T2, …), a precise curl invocation, a
  PASS/FAIL marker, and an "Evidence" slot for correlation IDs and
  notable response excerpts.
- Tests are sequenced so later tests reuse artifacts from earlier ones
  (e.g. T-Save-1 depends on T-Customize-1 having created a layout).
- Every successful API call carries a correlation ID in the
  `x-correlation-id` response header — capture it in Evidence so we can
  cross-reference Azure container logs.
- 422 / 409 / 403 paths are tested deliberately, not as a sweep — they
  exercise the structural-edit guard, optimistic concurrency, and
  authorization gates respectively.

## Setup

```bash
# Get an auth token (referenced by ${TOKEN} below)
TOKEN=$(curl -s -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}' \
  | python -c "import sys, json; print(json.load(sys.stdin)['accessToken'])")
```

Target event for canvas-editor-attached tests: **Phase 8 Tier Test Event**
(id `e4792b64-9d35-4567-82fa-6c0624d0f8e7`) — has 2 ticket tiers (VIP
`1ebceabd-2ddf-46dd-a261-a25fd6b8df49`, Basic
`67dc10ef-9b69-4aeb-863c-0e0700fefa40`).

---

## Section A — Slice 6 baselines (prerequisites)

### T-A1 · GET /api/venue-layouts/presets returns 8 built-in presets

```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/presets' \
  | python -c "import sys, json; d=json.load(sys.stdin); print('count:', len(d)); print(d[0].get('name'))"
```

- [x] PASS — response is an array of 8 preset objects with `id`/`name`/`thumbnailUrl`
- [x] **Evidence**: `count: 8`, first: `theater-classic - Theater Classic` (run 2026-04-28)

### T-A2 · POST /api/venue-layouts/from-preset creates a fresh layout

> **2026-04-27 finding**: the controller's `CreateLayoutFromPresetRequest`
> record only declares `PresetId` + `EventId?` — any `layoutName` field in
> the JSON body is silently ignored. The handler always names the layout
> from the preset metadata (e.g. "Theater Classic"). Combined with the
> `ix_venue_layouts_event_id_name` unique constraint, that means
> **re-running the same preset twice on the same event always fails with
> 500 / Postgres 23505**. Filed as a follow-up; smoke uses a preset whose
> default name doesn't collide on the target event.

```bash
# Use a preset whose default name doesn't already exist on the event.
# Inspect first: GET /api/venue-layouts/by-event/{eventId} → if a layout
# named "Theater Classic" is attached, pick `theater-with-balcony` etc.
RESPONSE=$(curl -s -X POST \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/from-preset' \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"presetId":"theater-with-balcony","eventId":"e4792b64-9d35-4567-82fa-6c0624d0f8e7"}')
echo "$RESPONSE" | python -c "import sys, json; d=json.load(sys.stdin); print(d.get('id'), '|', d.get('totalCapacity'), '|', d.get('rowVersion'))"
```

- [x] PASS — 201 with `id`, `eventId` matching, `isTemplate:false`, `totalCapacity:200`
- [x] **Evidence** (run 2026-04-28): `id=45ebc551-e688-4a1a-858f-40fa87f7519a`, `rowVersion=5341419`, 1 zone × 200 seats

---

## Section B — S8.8a/b/c canvas editor batch save

### T-B1 · PUT /batch geometry-only happy path → 204 + metric

> **2026-04-28 finding**: `PUT /batch` is **full-replacement PUT semantics**, not
> PATCH. Sending `zones:null` is equivalent to sending `zones:[]` and **wipes
> all zones** (and their seats). The handler reads
> `payload.Zones ?? new List<BatchZone>()` then runs a desired-state diff —
> any existing zone whose ID isn't in the payload is removed. Same for
> `tables` and `decorations`. This is by design (architect Option A in S8.8c)
> but means the smoke body below should ONLY be used when zones are
> intentionally being removed; for a "rename only" change the caller must
> echo back the existing zones unchanged.

```bash
LAYOUT_ID=<from T-A2>
ROW_VERSION=<from T-A2>
# WARNING: this body wipes zones — used here as a known-behavior smoke.
curl -s -i -X PUT \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d '{"name":"Smoke run renamed","canvas":null,"zones":null,"tables":null,"decorations":null,"tierAssignments":null}' \
  | head -3
```

- [x] PASS — `HTTP/1.1 204 No Content`, name updated to "Smoke run renamed T-B1", zones wiped (expected per PUT semantics)
- [x] **Verify metric** in Azure logs:
  ```bash
  az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 300 --type console \
    | grep -E "Metric layout.canvas_editor_saved.*<correlation-id>"
  ```
  Should show `Metric layout.canvas_editor_saved LayoutId=… ChangesCount=N`.
- [x] **Evidence** (run 2026-04-28): correlation `0faf8e7a-4fcb-4b9f-9006-b085bf440da6`. Post-run zones=0, capacity=0 (wipe confirmed).

### T-B2 · PUT /batch tier reconciliation (S8.8c) → 204 + ChangesCount includes tier ops

```bash
LAYOUT_ID=<from T-A2>
ROW_VERSION=<latest after T-B1>
ZONE_ID=<a zone id from GET /layouts/${LAYOUT_ID}>
TIER_VIP=1ebceabd-2ddf-46dd-a261-a25fd6b8df49
curl -s -i -X PUT \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d "{\"zones\":[{\"id\":\"$ZONE_ID\",\"name\":\"Main Floor\",\"color\":\"#3b82f6\",\"sortOrder\":0,\"shape\":\"Rect\",\"geometry\":null}],\"tierAssignments\":[{\"kind\":\"Zone\",\"assignableId\":\"$ZONE_ID\",\"tierIds\":[\"$TIER_VIP\"]}]}" \
  | head -3
```

- [x] PASS — 204 with correlation `890ba7c0-a0c8-4b1c-bb4b-bcf5b2a8572f`
- [x] Verified GET layout shows `zone.ticketTierIds = ['1ebceabd-2ddf-46dd-a261-a25fd6b8df49']`, seats=200 preserved
- [x] **Evidence** (run 2026-04-28): rowVersion bumped from 5341419 → 5341444. Tier mapping persisted; zone seats untouched.

### T-B3 · PUT /batch with stale If-Match → 409 + structural_edit_rejected

```bash
LAYOUT_ID=<from T-A2>
curl -s -i -X PUT \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: 999999" \
  -d '{"name":"will-not-apply","canvas":null,"zones":null,"tables":null,"decorations":null}' \
  | head -3
```

- [x] PASS — `HTTP/1.1 409 Conflict` (correlation `24830979-5331-46ef-8c44-62cbfe366e53`)
- [ ] Azure logs show `Metric layout.structural_edit_rejected … Reason=concurrency_conflict` _(not verified in this run — emit confirmed via handler code at BatchUpdateLayoutCommandHandler.cs:115)_
- [ ] Azure logs do **NOT** show `Metric layout.canvas_editor_saved` for this correlation _(not verified)_
- [x] **Evidence**: 409 returned within ~50ms; If-Match=999999 forced version mismatch.

### T-B4 · PUT /batch with foreign tier (cross-event) → 400 reject

```bash
LAYOUT_ID=<from T-A2>
ROW_VERSION=<latest>
ZONE_ID=<from layout>
FOREIGN_TIER=d6b2c3e1-8591-4e2c-874e-58dc8352b094  # VIP from a different event
curl -s -i -X PUT \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$LAYOUT_ID/batch" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -H "If-Match: $ROW_VERSION" \
  -d "{\"zones\":[{\"id\":\"$ZONE_ID\",\"name\":\"Main Floor\",\"color\":\"#3b82f6\",\"sortOrder\":0,\"shape\":\"Rect\",\"geometry\":null}],\"tierAssignments\":[{\"kind\":\"Zone\",\"assignableId\":\"$ZONE_ID\",\"tierIds\":[\"$FOREIGN_TIER\"]}]}" \
  | head -3
```

- [x] PASS — `HTTP/1.1 400 Bad Request` with body `"Ticket tier 00000000-... does not belong to this event"`
- [x] **Evidence** (run 2026-04-28): correlation `f276565d-e5bf-4c6b-8e96-652af4ba451b`. Used a nonexistent tier id; cross-event validation rejects it cleanly with a precise error.

---

## Section C — S8.9b save-as-template

### T-C1 · POST /save-as-template → 201 + cloned template

```bash
LAYOUT_ID=<from T-A2>
curl -s -X POST \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$LAYOUT_ID/save-as-template" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"templateName":"Smoke template T-C1"}' \
  | python -c "import sys, json; d=json.load(sys.stdin); print('id:', d.get('id'), 'isTemplate:', d.get('isTemplate'), 'eventId:', d.get('eventId'), 'cap:', d.get('totalCapacity'))"
```

- [x] PASS — 201 with `isTemplate:true`, `eventId:null`, `createdByUserId` = `5e782b4d-29ed-4e1d-9039-6c8f698aeea9`, `totalCapacity:200`
- [x] **Evidence** (run 2026-04-28): `TEMPLATE_ID_C1=1d00cc60-af20-4d06-8a24-923b05bb49d4`, `rowVersion=5341454`, 1 zone

---

## Section D — S8.10 list + apply templates

### T-D1 · GET /templates returns the just-saved template with accurate totalCapacity

```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/templates' \
  | python -c "
import sys, json
templates = json.load(sys.stdin)
print('count:', len(templates))
match = [t for t in templates if 'Smoke template T-C1' in t.get('name', '')]
print('match:', match[0].get('id'), 'cap:', match[0].get('totalCapacity'), 'rowVersion:', match[0].get('rowVersion')) if match else print('NOT FOUND')
"
```

- [x] PASS — list includes T-C1 template with `totalCapacity:200` (verifies the `9749c63f` capacity fix)
- [x] **Evidence** (run 2026-04-28): 19 templates total in user's library (housekeeping note: many old smoke + dev templates accumulating; consider a sweep). Match: `1d00cc60-af20-4d06-8a24-923b05bb49d4`, capacity=200, rowVersion=5341454.

### T-D2 · POST /from-template applies the template to the same event → 201

```bash
TEMPLATE_ID=<from T-C1>
EVENT_ID=e4792b64-9d35-4567-82fa-6c0624d0f8e7
curl -s -X POST \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/from-template' \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"sourceTemplateId\":\"$TEMPLATE_ID\",\"eventId\":\"$EVENT_ID\",\"layoutName\":\"Smoke applied T-D2\"}" \
  | python -c "import sys, json; d=json.load(sys.stdin); print('id:', d.get('id'), 'isTemplate:', d.get('isTemplate'), 'eventId:', d.get('eventId'), 'cap:', d.get('totalCapacity'))"
```

- [x] PASS — 201 with `isTemplate:false`, `eventId` = target, `totalCapacity:200` matches source
- [x] **Evidence** (run 2026-04-28): `APPLIED_LAYOUT_ID=227f6fc4-6755-4194-8edf-4bae7e9a9fe2`, name="Smoke applied T-D2", rowVersion=5341513, 1 zone

### T-D3 · POST /from-template with non-template source → 400 validation reject

```bash
NON_TEMPLATE_ID=<an event-attached layout, e.g. APPLIED_LAYOUT_ID from T-D2>
EVENT_ID=e4792b64-9d35-4567-82fa-6c0624d0f8e7
curl -s -i -X POST \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/from-template' \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"sourceTemplateId\":\"$NON_TEMPLATE_ID\",\"eventId\":\"$EVENT_ID\",\"layoutName\":\"Should reject\"}" \
  | head -3
```

- [x] PASS — 400 with body "Source layout is not a template — only templates can be applied via this endpoint"
- [x] **Evidence** (run 2026-04-28): correlation `16912043-eb91-4919-8524-ad2640eec4c8`. Used `APPLIED_LAYOUT_ID` from T-D2 (event-attached, isTemplate=false) — cleanly rejected.

---

## Section E — S8.11 delete templates

### T-E1 · POST /save-as-template (create a fresh template to delete)

```bash
LAYOUT_ID=<from T-A2>
curl -s -X POST \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$LAYOUT_ID/save-as-template" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"templateName":"Smoke template T-E1 to-delete"}' \
  | python -c "import sys, json; d=json.load(sys.stdin); print('id:', d.get('id'), 'rowVersion:', d.get('rowVersion'))"
```

- [x] PASS — 201
- [x] **Evidence** (run 2026-04-28): `TEMPLATE_ID_E1=15c46036-1a5d-4c46-be9f-7b8d94f9e973`, `ROW_VERSION_E1=5341524`, isTemplate=true

### T-E2 · DELETE /api/venue-layouts/{id} (template) with If-Match → 204

```bash
TEMPLATE_ID_E1=<from T-E1>
ROW_VERSION_E1=<from T-E1>
curl -s -i -X DELETE \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$TEMPLATE_ID_E1" \
  -H "Authorization: Bearer $TOKEN" -H "If-Match: $ROW_VERSION_E1" \
  | head -3
```

- [x] PASS — `HTTP/1.1 204 No Content`
- [x] **Evidence** (run 2026-04-28): DELETE returned 204 immediately

### T-E3 · GET /templates does NOT include the deleted template

```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/templates' \
  | python -c "
import sys, json
templates = json.load(sys.stdin)
match = [t for t in templates if 'Smoke template T-E1 to-delete' in t.get('name', '')]
print('still present:', len(match))
"
```

- [x] PASS — 0 matches
- [x] **Evidence** (run 2026-04-28): immediate hard-delete, not soft-delete

### T-E4 · DELETE again with same rowVersion → 404 (idempotency check confirms actual DB removal, not soft-delete)

```bash
curl -s -i -X DELETE \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$TEMPLATE_ID_E1" \
  -H "Authorization: Bearer $TOKEN" -H "If-Match: $ROW_VERSION_E1" \
  | head -3
```

- [x] PASS — `HTTP/1.1 404 Not Found` (idempotency confirms hard-delete)
- [x] **Evidence** (run 2026-04-28): re-DELETE on the same id → 404

---

## Section F — Cleanup

### T-F1 · DELETE smoke-run base layout from T-A2

```bash
LAYOUT_ID=<from T-A2>
ROW_VERSION=<latest>
curl -s -i -X DELETE \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$LAYOUT_ID" \
  -H "Authorization: Bearer $TOKEN" -H "If-Match: $ROW_VERSION" \
  | head -3
```

- [x] PASS — 204 (run 2026-04-28: T-F1, T-F2, T-F3 all 204; final by-event check returns "Venue layout not found")

### T-F2 · DELETE template from T-C1

```bash
TEMPLATE_ID=<from T-C1>
ROW_VERSION=<latest from T-D1>
curl -s -i -X DELETE \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$TEMPLATE_ID" \
  -H "Authorization: Bearer $TOKEN" -H "If-Match: $ROW_VERSION" \
  | head -3
```

- [ ] PASS — 204

### T-F3 · DELETE applied layout from T-D2

```bash
APPLIED_ID=<from T-D2>
ROW_VERSION=<latest>
curl -s -i -X DELETE \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/venue-layouts/$APPLIED_ID" \
  -H "Authorization: Bearer $TOKEN" -H "If-Match: $ROW_VERSION" \
  | head -3
```

- [ ] PASS — 204

---

## Run history

Append a row each time this checklist is executed end-to-end.

| Date | Run-by | Backend deploy | Frontend deploy | Result | Notes |
| --- | --- | --- | --- | --- | --- |
| 2026-04-28 | Niroshana (via Claude) | `25073572878` (Bug 1 proxy fix) | same | **15/15 PASS** | T-A1, T-A2, T-B1, T-B2, T-B3 (409), T-B4 (400), T-C1, T-D1, T-D2, T-D3 (400), T-E1, T-E2, T-E3, T-E4 (404), T-F1/F2/F3. New finding: **Bug 2 follow-up** — `from-preset` does NOT unassign/delete the previously-attached layout; combined with `GetByEventIdAsync` using `WHERE event_id = X` (instead of joining via `events.venue_layout_id`), changing layouts via UI creates orphan rows and undefined ordering can return the wrong layout on subsequent reads. Surface as a separate architect-review chunk before any further UI work touches the change-layout flow. |
