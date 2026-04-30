# Master TODO — Phase 7F sub-feature B: A↔B mode change with attendee backfill

**Status**: 📋 ARCHITECT-APPROVED WITH EDITS (review iteration 1, 2026-04-30; 13 edits applied). No code changes yet — depends on 7F-C landing first.
**Ship order**: **Second** of {7F-C, 7F-B, 7F-D}. Architect rationale: A→B conversion needs to populate `TierCount.AdultCount/ChildCount` to satisfy 7F-C's strict cross-axis invariant. Also gives operator-relief for the "wrong mode picked" case before the more complex 7F-D ships.
**Classification**: Feature missing. Not a bug — the current "throws if active registrations exist" guard in [`Event.SetRegistrationMode`](../src/LankaConnect.Domain/Events/Event.RegistrationMode.cs#L45-L95) is deliberate scope discipline shipped in 7E.1; 7F-B is forward-feature work that picks up the migration semantics that were intentionally deferred.
**Layers touched**: Domain (new aggregate operation + migration semantics) → Application+API (command + handler + email + endpoint) → Persistence (audit/snapshot tables — pair) → Frontend (confirmation dialog with diff preview).

---

## 1. Why this exists

### 1.1 Operator pain
Today, once any active (non-Cancelled / non-Refunded / non-Abandoned) registration lands on an event, the organiser cannot change the registration mode. They get a hard error citing the blocking-status breakdown — and the only resolution is to cancel every registration (refunding paid ones) and re-open. This is heavyweight for cases like:
- "We picked Detailed-Attendees thinking we'd need each name; nobody actually cares for this kind of community event — let's switch to head-count B2."
- "We picked head-count B1 but turns out the venue requires per-attendee names — let's switch to A."

### 1.2 What's deliberately deferred
The original Phase 7E plan §3.2 + §12 (Phase 7F follow-up #3) explicitly defers "A↔B mode change with attendee backfill" because the migration semantics (what happens to per-attendee data on A→B; what fabricated names/age categories to put on B→A) require a domain decision per event-type and a confirmation UX that surfaces the data loss to the organiser.

---

## 2. Conversion semantics (architect-ratified iteration 1)

### 2.1 A → B (collapse per-attendee rows into head-count) — architect edit #1

| Target mode | Total | Demographics | Lead | TierCount per-tier-age (7F-C dep.) | Per-attendee data fate |
|---|---|---|---|---|---|
| **B1** (HeadCountOnly) | `Attendees.Count` | `null` | `Registration.Contact.FullName ?? FirstAttendee.Name` | `AdultCount = ChildCount = null` (legacy axis) | Snapshot to audit table; live row drops attendee rows |
| **B2** (HeadCountByAge) | `Attendees.Count` | `Adults = count(AgeCategory==Adult)`, `Children = count(AgeCategory==Child)` | Same | `AdultCount = count(AgeCategory==Adult per tier)`, `ChildCount = count(AgeCategory==Child per tier)` | Same |
| **B3** (HeadCountByGender) | `Attendees.Count` | `Males = count(Gender==Male)`, `Females = count(Gender==Female)` | Same | `AdultCount = ChildCount = null` (B3 has no age axis) | Same; **reject conversion** if any `Gender==Other` (B3 doesn't model `Other`) — surface in `ConversionReport.Skipped` |
| **B4** (HeadCountByAgeAndGender) | `Attendees.Count` | 4-leaf from `(AgeCategory, Gender)` cross | Same | Per-tier `AdultCount = AdultMales + AdultFemales`, `ChildCount = ChildMales + ChildFemales` | Same; reject if any `Gender==Other` |

**Per-tier reservation accounting** (architect §3 omission): collapsing A→B does *not* change the registration's contribution to `TicketTier.ReservedCount` — same attendees, same tiers, same numbers. The conversion code path MUST NOT call `Reserve()` or `Release()`. (Test case in 7F-B.1: per-tier reservation totals identical pre- and post-conversion.)

**B-mode `Total ≥ 1` invariant** (`HeadCountBreakdown.cs:53`): if all attendees are `Gender==Other` and target is B3, the conversion would produce `Total=0` from the rejection rule. Reject the *whole* conversion request, not silently produce a malformed VO (architect §3 missing edge case).

### 2.2 B → A (explode head-count into placeholder attendee rows) — architect edits #2, #3, #4, #5

Create N=`HeadCount.Total` placeholder `AttendeeDetails` rows. **Deterministic ordering** so re-runs of the same conversion produce identical placeholder rows.

#### 2.2.1 Name fabrication (architect edit #3)
- **Row 1**: `LeadAttendeeName` unmodified (preserves the registrant's identity).
- **Rows 2..N**: `{LeadName} (n)` (e.g. `"Niroshana (2)"`, `"Niroshana (3)"`).

(Earlier draft default was `{LeadName} (n)` for *all* rows; architect changed row 1 to keep the lead's bare name — `"Niroshana (1)"` reads artificial.)

#### 2.2.2 AgeCategory + Gender fabrication
| Source mode | AgeCategory | Gender |
|---|---|---|
| B1 | All `Adult` | All `Unspecified` |
| B2 | `Adults`-many `Adult` rows + `Children`-many `Child` rows | All `Unspecified` |
| B3 | All `Adult` (B3 doesn't capture age) | `Males`-many `Male` + `Females`-many `Female` |
| B4 | 4-leaf cross | 4-leaf cross |

**Deterministic order within mode** (architect edit #5): Adults before Children; Males before Females; (Adult, Male) → (Adult, Female) → (Child, Male) → (Child, Female) for B4.

#### 2.2.3 Email + Phone fabrication (architect edit #2 — missing in v0)
- **Row 1**: inherits `Registration.Contact.Email` and `Registration.Contact.Phone` (architect-ratifiable; current default — surface as architect Q9 if review wants `null`).
- **Rows 2..N**: `Email = null`, `Phone = null` — placeholder rows are organiser-completable later.
- ⚠️ If `AttendeeDetails.Email` / `Phone` columns are NOT NULL, schema can't accept `null` here. *Pre-condition check #6 below*.

#### 2.2.4 TicketTierId allocation (architect edit #4)
- For tiered events, allocate from `HeadCount.TierCounts` group-by → assign tier in **stable sort order**: `TierCount` ordered by `TierCount.SortOrder` (snapshotted at registration time) ascending; if `SortOrder` not snapshotted in the existing `TierCount` VO (likely true today), fall back to `TierName` ASC.
- Within each tier, assign `AgeCategory` per the per-tier-age axis if present (requires 7F-C live — pre-condition #5):
  - VIP × `(AdultCount=2, ChildCount=1)` → 2 placeholder rows with `(VIP, Adult)` + 1 placeholder row with `(VIP, Child)`.
- Without 7F-C's per-tier-age axis, all placeholder rows in a given tier default to `Adult` (lossy). 7F-B blocks on 7F-C for this reason.

#### 2.2.5 Email side-effect
A new `event-registration-format-changed` template fires to each registrant. Default `notifyAttendees = false` (organiser opts in via the request body); template authored via embedded-resource seeder per memory 7C.2 / 6A.117 / 6A.122. **Mode-aware copy**: A→B vs B→A vs same-family (B2→B4) get distinct paragraphs.

### 2.3 A↔C and B↔C — explicit decisions (architect edit #6)

| From → To | Allowed when | Rationale |
|---|---|---|
| A → C | No active registrations exist | Mode C is "no registration"; converting an event with attendees is meaningless. **Same "active" definition as A↔B** — current guard's "any registrations" rule is loosened uniformly so error-message wording stays consistent across all conversion paths. |
| B → C | Same | Same |
| C → A / B | Always (no registrations to migrate) | Already handled by current guard. |

In other words, 7F-B loosens the guard's *active* definition for **all** conversion paths uniformly. C-conversions still require zero active registrations.

### 2.4 Compatibility re-check on conversion

After conversion:
- **Event-shape compatibility** (named seating + B mode rejected, etc.) — reuse 7E.2 validator from compatibility table §2.
- **Per-registration compatibility** (architect §3 missing): a registration that has `AttendeeDetails` with explicit named-seat assignments cannot collapse to B (named seats require A's per-attendee identity). Surface as `ConversionReport.Skipped[i]` with reason `"NamedSeatsRequireDetailedAttendees"`.

### 2.5 RegistrationMode snapshot flip (architect §3 missing)

`Registration.RegistrationMode` is a snapshot field. After conversion, the snapshot **flips to the new mode**. Consequence: emails re-rendered for cancellation/reminder after conversion will use the *new* mode's template branch — that's intentional and prevents stale rendering. Document explicitly so the implementer doesn't try to preserve the old snapshot for "audit purposes" (the audit table covers that role).

### 2.6 ActualHeadCountAttended fate (architect §3 missing)

`Registration.ActualHeadCountAttended` (added in 7E for B-mode check-in) is dropped on B→A — Mode A doesn't have a head-count concept. The audit table preserves the original B-mode shape including `ActualHeadCountAttended`; the live row simply loses the field.

---

## 3. Persistence — split into two tables (architect edit #8)

### 3.1 `events.registration_mode_conversions` (one row per organiser action)

| Column | Type | Purpose |
|---|---|---|
| `Id` | uuid PK | |
| `EventId` | uuid | |
| `OrganiserId` | uuid | Who triggered |
| `FromMode` / `ToMode` | smallint | |
| `StartedAt` / `CompletedAt` | timestamptz | |
| `TotalCount` / `MigratedCount` / `SkippedCount` / `FailedCount` | int | |
| `EventRowVersion` | bytea | Snapshot for audit replay |

Cleaner reporting; cheap dashboard queries (no per-registration aggregation).

### 3.2 `events.registration_mode_conversion_rows` (per-registration detail)

| Column | Type | Purpose |
|---|---|---|
| `Id` | uuid PK | |
| `AggregateConversionId` | uuid FK → `registration_mode_conversions.Id` (architect edit #7) | |
| `RegistrationId` | uuid | |
| `ConversionOutcome` | smallint (`Migrated=0 \| Skipped=1 \| Failed=2`) (architect edit #7) | |
| `OutcomeReason` | text NULL (architect edit #7) | E.g. `"GenderOtherNotSupportedByB3"`, `"NamedSeatsRequireDetailedAttendees"` |
| `RegistrationRowVersionSnapshot` | bytea (architect edit #7) | For support-side replay |
| `BeforeShape` | jsonb | Snapshot of pre-conversion shape (Attendees array OR HeadCountBreakdown) |
| `AfterShape` | jsonb | Snapshot of post-conversion shape |
| `ConvertedAt` | timestamptz | |

EF Core migration auto-scaffolded via `dotnet ef migrations add Phase7FB_AddRegistrationModeConversionAuditTables` per CLAUDE.md memory 6A.133 (no hand-authoring).

---

## 4. Slice plan (6 slices — architect edit #13 merged 7F-B.4 into 7F-B.3)

| Slice | Focus | Tests | Deploy |
|---|---|---|---|
| **7F-B.0** | Architect-approved decisions for §2 + §3 (this doc). | — | — |
| **7F-B.1** | Domain — `Event.ConvertRegistrationMode(targetMode, conversionPolicy)` returns `Result<ConversionReport>` with `Lost` / `Synthesised` / `Skipped`. Each per-registration migration delegates to a `IModeConversionStrategy`. **Per-tier reservation accounting unchanged** (verified by test). **Concurrency guard inside the method** (architect edit #10): re-fetch active-registration count under transaction; abort if count grew between preview-time and execute-time. | TDD ≥36 cases (architect-revised floor): every (A → B1/B2/B3/B4) + (B1/B2/B3/B4 → A) + edge cases (B3-with-Other-gender skipped per registration, B2 with all-Adult collapses, lead-name preservation, tier snapshot, Total=0 corner case rejected globally, named-seat-assigned registration skipped, registration with pending `RegistrationAddition` rejected, deterministic placeholder ordering, stable sort by tier `SortOrder`/`TierName`, AgeCategory+Gender deterministic order, RegistrationMode snapshot flips, `ActualHeadCountAttended` preserved in audit, dropped from live, batch-cap > 500 rejected). 90%+ coverage. | — |
| **7F-B.2** | Persistence — two new audit tables per §3 + EF config + repository. | Round-trip serialisation tests for `BeforeShape` / `AfterShape` jsonb; deep-copy `ValueComparer` tests (memory 6A.129); FK cascade test (deleting aggregate row cascades to detail rows, or `ON DELETE RESTRICT` if architect prefers — open call). | `deploy-staging.yml` |
| **7F-B.3** | Application + API (architect edit #13: merged) — `ConvertRegistrationModeCommand` + handler + `POST /api/events/{id}/convert-registration-mode` controller (with `If-Match` rowVersion + body `{ targetMode, notifyAttendees: bool, dryRun: bool }`). Validates organiser owns the event; calls `Event.ConvertRegistrationMode`; persists audit rows; raises `RegistrationModeConvertedDomainEvent` per registration. **Concurrency guard** re-checks the active-registration set (architect edit #10). | Handler tests with Mock<IRepo>; FluentValidation cases; integration test via TestServer; preview vs execute consistency test (preview-then-execute with no concurrent edits should yield identical `ConversionReport`). 90%+ coverage. | `deploy-staging.yml` |
| **7F-B.4** | Email — new `event-registration-format-changed` template + handler subscribing to `RegistrationModeConvertedDomainEvent`. **Hangfire fire-and-forget** with dedupe by `(RegistrationId, ConversionId)` (architect Q3 + Q4 calls). Default-off (organiser opts in via request body); template authored via embedded-resource seeder. **Mode-aware copy**: A→B / B→A / same-family (B2→B4) get distinct paragraphs. | Template-validation passes at startup; rendered-content unit test; idempotent retry test (same `(RegistrationId, ConversionId)` doesn't double-send). | `deploy-staging.yml` |
| **7F-B.5** | Frontend — `ConfirmDialog` (`danger` variant) for the conversion. Preview comes from a `?dryRun=true` flag on the same `POST` endpoint (architect edit #13: the dryRun branch lives inside the same command, not a separate `GET .../preview`). UI shows a *diff table*: per-registration rows highlighting `"lose 5 names"`, `"create 5 placeholders named LeadName, LeadName (2)…(5)"`, plus `Skipped` rows with reason. `notifyAttendees` checkbox makes the email choice explicit. | Preview-on-mode-change unit test in EventEditForm; ConfirmDialog content-rendering RTL test; dry-run-vs-real consistency (same payload, dry run output matches subsequent real run output if no concurrent edits). | `deploy-ui-staging.yml` |
| **7F-B.6** | Staging end-to-end smoke — create A event → register 3 attendees → preview B2 conversion → execute → verify head-count = `(2 Adults, 1 Child)` + lead name = `Registration.Contact.FullName` + audit row written + per-tier-age axis populated (depends on 7F-C live) + (if `notifyAttendees=true`) email rendered with the expected paragraph. Then convert back B2→A → verify 3 placeholder rows with names per §2.2.1 + correct AgeCategory split + deterministic order. **Cross-doc smoke**: register, then *attempt* an `add-attendees` flow in parallel → conversion rejects ("registration has pending addition"). | — | — |

**Tracking-doc updates** after every slice per CLAUDE.md §7.

---

## 5. Risks & guards

| Risk | Mitigation |
|---|---|
| Conversion silently loses real names → angry support tickets | Preview-before-convert in the UI; audit table preserves the BeforeShape jsonb so support can recover names. |
| B3-with-`Other`-gender / B4-with-`Other`-gender — silent data loss | Reject *per-registration*; surface in `ConversionReport.Skipped`. Architect Q1 ratified. Surface skipped rows in preview UI so organiser can pre-cancel offenders. |
| Tier rename between conversion and re-conversion → tier-name drift | Tier-name snapshot at conversion time (already the convention from 7E.3c — `TierCount.TierName` is snapshotted). |
| Concurrent organiser edits — two browsers fighting over mode | Standard `If-Match` rowVersion gate. Plus per-handler concurrency guard (architect edit #10): re-fetch active-registration count under transaction; abort if grew. |
| Compatibility re-check after conversion — e.g. event has named seats; converting to B is invalid | Reuse 7E.2 validator at event level + per-registration named-seat check (§2.4). |
| Email blast on a 100-registrant event when organiser flips mode | `notifyAttendees` defaults to false; UI checkbox makes the choice explicit; mode-aware copy avoids alarm. |
| Audit table grows unbounded | Architect Q5 = indefinite. Immutable audit; if pruning becomes necessary later, add a Hangfire job. |
| Cancelled / Refunded / Abandoned registration re-render risk — those rows aren't migrated, but their snapshotted `RegistrationMode` still says A; emails sent later (e.g. refund completion) will render in the old mode | Acceptable; documented in §2.5. Audit table records the conversion-time event so support can correlate. |
| `RegistrationAddition` in `Pending` / `PaymentCompleted-not-yet-Merged` state attached to a converting registration (architect edit #11 / Q8) | Conversion **rejects** the registration with reason `"PendingAdditionMustResolveFirst"`. Surfaces in `ConversionReport.Skipped`. |
| Registration arrival between preview and execute (architect edit #10) | Concurrency guard re-fetches active set inside the command's transaction; abort with `409 Conflict` and "set changed; re-preview before re-trying." |
| Conversion of >500 active registrations — single-transaction blast (architect Q7) | Hard-cap; reject with "split into batches." |

---

## 6. Out of scope

- **Tier × age matrix on Mode B** — separate ticket (7F-C). 7F-B's A→B2/B4 conversion populates `Demographics` from per-attendee age AND populates per-tier `AdultCount`/`ChildCount` *iff 7F-C is live*. Without 7F-C, per-tier-age fields stay null (lossy on tiered events) — that's why 7F-B blocks on 7F-C in the ship order.
- **Mode B add-attendees** (separate ticket: 7F-D).
- **Bulk re-import attendees from CSV** to populate placeholder names after B→A. Useful, but separate UX work.
- **C ↔ A/B with refund-on-conversion** for events with paid registrations. Forbidden — organiser must cancel + refund manually first.

---

## 7. Architect questions — answered

| # | Question | Architect call |
|---|---|---|
| Q1 | B↔A conversion when registrants have `Gender==Other` — reject or downgrade? | **Reject** per-registration; surface in `ConversionReport.Skipped`. Surface skipped rows in preview UI. |
| Q2 | B→A placeholder name scheme | Row 1 = unmodified `LeadName`; rows 2..N = `{LeadName} (n)`. |
| Q3 | Notification email — sync or Hangfire? | **Hangfire fire-and-forget** with dedupe by `(RegistrationId, ConversionId)`. |
| Q4 | Audit row written — per-registration or aggregate? | **Both** — split into two tables (§3). Aggregate row for dashboard joins; per-registration rows for detail. |
| Q5 | Audit retention | Indefinite v1. |
| Q6 | Lead name source on A→B | `Registration.Contact.FullName ?? FirstAttendee.Name`. |
| Q7 (architect-added) | Conversion batch cap | **Hard-cap at 500 per call**; reject larger with "split into batches" guidance. |
| Q8 (architect-added) | Pending `RegistrationAddition` during conversion | **Reject** the registration per §2 / §5. |

---

## 8. Pre-conditions

| # | Item | Status |
|---|---|---|
| 1 | Plan §2 conversion semantics architect-ratified | ✅ ratified by review iteration 1 (this doc) |
| 2 | Mode A's `CalculateTieredPriceForAttendees` confirmed using per-attendee `AgeCategory` | ✅ verified at [Event.TicketTiers.cs:198](../src/LankaConnect.Domain/Events/Event.TicketTiers.cs#L198) |
| 3 | Compatibility validator from 7E.2 covers post-conversion event shape | ✅ already reusable |
| 4 | EF Core migration auto-scaffolding pattern for jsonb audit tables | ✅ pattern established in Phase 7E.1 |
| 5 | **7F-C live** so per-tier `AdultCount` / `ChildCount` axis exists for A→B placeholder allocation | ⏳ blocks on 7F-C |
| 6 | `AttendeeDetails.Email` and `AttendeeDetails.Phone` columns are nullable (or have a defaultable value) for B→A placeholder rows | ⏳ to verify in 7F-B.0; if NOT NULL, add migration + DB default first |
