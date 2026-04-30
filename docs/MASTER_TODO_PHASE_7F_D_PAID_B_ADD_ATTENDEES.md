# Master TODO — Phase 7F sub-feature D: Paid Mode B add-attendees with delta payment

**Status**: 📋 ARCHITECT-APPROVED WITH EDITS (review iteration 1, 2026-04-30; 13 edits applied). No code changes yet — depends on 7F-C and (transitively) 7F-B.
**Ship order**: **Third** of {7F-C, 7F-B, 7F-D}. Architect rationale: largest surface (Stripe checkout, webhooks, two new aggregate methods, frontend modal branching, email template variant). Benefits from 7F-C's per-tier-per-age axis being live (no feature flag in `AddAttendeesModal`) and from 7F-B's "no pending addition during conversion" enforcement (decouples lifecycles).
**Classification**: Feature missing — *not* a regression. Mode A's [`InitiateAddAttendeesCommand`](../src/LankaConnect.Application/Events/Commands/InitiateAddAttendees/InitiateAddAttendeesCommand.cs) and the [`AddAttendeesModal`](../web/src/presentation/components/features/events/AddAttendeesModal.tsx) UI work today on legacy paid events. Mode B has no equivalent — any user who picked Mode B and wants to expand their registration today is stuck. The original 7E plan §5.2 anticipated this ("mode B: head-count delta + tier-count delta — reuses the existing tier-pricing service") but flagged it as "not 7E.3a-c scope".
**Layers touched**: Domain (new aggregate operation on Registration) → Application (new command + handler + Stripe checkout reuse + webhook handler extension) → Persistence (extend `RegistrationAddition` schema with check constraint) → API (new endpoint) → Frontend (Mode-B-aware AddAttendeesModal branch).

---

## 1. Why this exists

### 1.1 Operator + attendee pain
- A B-mode event organiser hosting "+5 of us are coming, can we still join?" requests has no in-product path; the attendee currently has to cancel + re-register at the new size, eating the existing-registration audit trail and (for paid events) hitting Stripe twice.
- For free B-mode events the same problem exists but is less severe (no payment churn) — yet still no UI path.

### 1.2 Why this is forward-feature, not back-compat
Legacy paid events are all Mode A and `AddAttendeesModal` works for them. Mode B was added in Phase 7E and the add-attendees flow was deliberately left unbuilt with the 7E plan §13 risk #4 noting "Same pipeline, no fork; tested in 7E.3b/c" — the *pricing* pipeline got reused; the *aggregate operation + checkout* did not.

### 1.3 What 7F-D delivers
- Free B-mode event: `RegistrationAddition` records the head-count delta + (for tiered events) the tier-counts delta, applies it directly to the registration, sends a confirmation email. No Stripe.
- Paid B-mode event: same shape, but goes through Stripe Checkout for `AdditionalAmount` first; on webhook completion, the head-count delta is merged.
- Behaviour mirrors Mode A's `InitiateAddAttendees` → `RegistrationAddition` → `AddAttendeesPaymentCompletedHandler` lifecycle exactly — *no fork*.

---

## 2. Domain shape

### 2.1 Extend `RegistrationAddition` to be mode-aware (architect edit #1)

Today's [`RegistrationAddition`](../src/LankaConnect.Domain/Events/RegistrationAddition.cs) carries only:
- `_newAttendees: List<AttendeeDetails>` — Mode A shape
- `PreviousTotalPrice` / `NewTotalPrice` / `AdditionalAmount`
- Stripe lifecycle fields

Architect-approved extension:
```csharp
public class RegistrationAddition : BaseEntity
{
    // Existing Mode A field (unchanged):
    private readonly List<AttendeeDetails> _newAttendees = new();
    public IReadOnlyList<AttendeeDetails> NewAttendees => _newAttendees.AsReadOnly();

    // NEW Mode B fields (nullable — Mode A path leaves them null):
    public RegistrationMode RegistrationMode { get; private set; }   // snapshot from parent registration
    public HeadCountBreakdown? HeadCountDelta { get; private set; }  // jsonb — added counts only

    // Discriminator (architect edit #1):
    public bool IsModeBAddition => RegistrationMode.IsHeadCountMode();
    public bool IsModeAAddition => !IsModeBAddition;
    // — DO NOT use `_newAttendees.Count > 0` as the discriminator: it gives false-positives
    //   after Mode A merge (the list is moved to the registration but row stays).
}
```

Two new factories alongside the existing `Create(...)`:
```csharp
public static Result<RegistrationAddition> CreateForHeadCountDelta(
    Guid registrationId, Guid eventId, RegistrationMode mode,
    HeadCountBreakdown headCountDelta,
    Money previousTotal, Money newTotal, Money additionalAmount);
```

**Mutual exclusion** enforced in factory: a single `RegistrationAddition` carries *either* `_newAttendees` (Mode A) *or* `HeadCountDelta` (Mode B), never both. Database CHECK constraint per §2.4 enforces this at storage level too.

### 2.2 New aggregate operation `Registration.MergeHeadCountAddition`

`Registration.MergeHeadCountAddition(HeadCountBreakdown delta)` — adds `delta` to the existing `HeadCount`:
- `Total += delta.Total`
- `Demographics` accumulates leaf counts when both rows have the same axis (B2+B2 ✓; B2+B4 → reject — addition's mode must match parent)
- `TierCounts` merges by `TierId` (sum the count, snapshot tier name from latest; per-tier-age axis from 7F-C — `AdultCount` / `ChildCount` accumulate when present on both sides)
- `LeadAttendeeName` unchanged (architect §2 missing — explicitly preserved)

**Architect-required guards at merge time** (architect edits #2, #3, #4):

1. **Mode-match invariant**: addition's `RegistrationMode == Registration.RegistrationMode`. Cross-mode merges (Mode A registration + Mode B addition or vice versa) are rejected at command-validation AND at domain level. Also rejects `RegistrationMode.NoRegistration` parents defensively (Mode C events have no Registration aggregate, but defence-in-depth).
2. **Per-tier reservation accounting** (architect edit #2): merge atomically `tier.Reserve(delta.TierCounts[i].Count)` per tier. If any tier is unreservable, abort merge and transition `RegistrationAddition.Status` to `Failed` with `OutcomeReason = "TierCapacityExhausted"`. Mirrors Mode A's behaviour exactly.
3. **Tier deactivation/deletion guard** (architect edit #3): if any tier referenced in the delta is deactivated or deleted between addition creation and merge, reject with `OutcomeReason = "TierDeactivatedMidFlight"`.
4. **Event-window guard** (architect edit #4): if `Event.StartDate <= DateTime.UtcNow` at merge time (registration window closed mid-Stripe-checkout), reject with `OutcomeReason = "EventStartedMidFlight"`. The webhook still acks Stripe (we got paid); the merge fails and the addition transitions to `Failed`. Refund handling is out of scope — surface to organiser dashboard.

### 2.3 Pricing delta — reuse `Event.CalculateHeadCountPrice`

Compute `AdditionalAmount = NewTotalPrice − PreviousTotalPrice`:
- `NewTotalPrice = Event.CalculateHeadCountPrice(parent.HeadCount + delta)` — i.e., the post-addition shape
- `PreviousTotalPrice = parent.TotalPrice` (unchanged, snapshot)
- `AdditionalAmount = newTotal.Subtract(previousTotal)`

No new pricing math. The architect-required Mode-A-vs-Mode-B parity (identical bills for identical baskets) carries through.

**Currency-mismatch test** (architect §2 missing risk): Mode A's `AdditionalAmount` rounding tolerance is `Math.Abs(diff) > 0.01m` in `RegistrationAddition.Create`. Mode B inherits this; for tiered B with multi-currency events the rounding can drift. Slice 7F-D.1 includes an explicit currency-mismatch test.

### 2.4 Persistence

`events.registration_additions` table already exists. Migration `Phase7FD_AddHeadCountDeltaToRegistrationAddition`:

- Add column `registration_mode` smallint NOT NULL DEFAULT 0  *(per memory 6A.123: NOT NULL columns need a DB default — legacy rows materialise as Mode A)*
- Add column `head_count_delta` jsonb NULL
- ~~Add column `tier_counts_delta_summary` jsonb NULL~~ **STRUCK** (architect edit #5; Q3 default = compute on read)
- Add CHECK constraint via `migrationBuilder.Sql(...)` (architect edit #6):
  ```sql
  ALTER TABLE events.registration_additions
  ADD CONSTRAINT ck_addition_mode_xor
  CHECK (
    (registration_mode = 0 AND head_count_delta IS NULL)
    OR
    (registration_mode > 0 AND head_count_delta IS NOT NULL)
  );
  ```
  *Note*: the existing `_new_attendees` jsonb-or-relational shape isn't in the constraint because today there's no clean "is empty?" check on the relational side. Mode B path simply leaves it empty; Mode A path leaves `head_count_delta` null — the constraint catches the polymorphic mistake.

EF jsonb config for `head_count_delta` reuses the deep-copy `ValueComparer` pattern from `Registration.HeadCount` (memory 6A.129).

### 2.5 Free-mode addition uses the same code path as paid (architect edit #7)

For free events: `AdditionalAmount = Money.Zero(currency)`. The existing Mode A free-event add-attendees flow already short-circuits Stripe by detecting `AdditionalAmount.IsZero` and merging immediately. Mode B uses the *same* `IRegistrationCheckoutService.InitiateAdditionCheckoutAsync` path — the service detects zero and merges directly without creating a Stripe session. **No "free shortcut" branch in the handler** — keeps anti-fork.

---

## 3. Slice plan (6 slices — architect edit #12 merged 7F-D.4 + 7F-D.5 into 7F-D.3)

| Slice | Focus | Tests | Deploy |
|---|---|---|---|
| **7F-D.0** | Architect-approved §2 decisions captured (this doc). | — | — |
| **7F-D.1** | Domain — new `RegistrationAddition.CreateForHeadCountDelta` factory + invariant tests; new `Registration.MergeHeadCountAddition(delta)` with cross-mode-rejection + per-mode merge tests + reservation accounting + tier-deactivation guard + event-window guard. | TDD ≥24 cases (architect-revised floor): B1+B1 / B2+B2 / B3+B3 / B4+B4 merges; cross-mode B2+B4 rejected; Mode A registration + Mode B addition rejected; tier-counts merge with rename-snapshot; tier-deactivated mid-flight (reject + Failed status); tier-deleted mid-flight (reject + Failed); event-window-closed mid-flight (reject + Failed); per-tier reservation atomic — reserve fails on tier 2 of 3 → abort, no partial reserve; free-mode addition (`AdditionalAmount=0` short-circuits Stripe via shared service); MaxAttendeesPerRegistration cap on the post-merge total; capacity guard on the merged total; double-pending rejected (only one `Pending` `RegistrationAddition` per registration — architect Q8); Mode A registration mistakenly hits add-headcount endpoint (400); rounding edge with mixed currencies; webhook idempotent replay; `LeadAttendeeName` preserved through merge; per-tier-age `AdultCount`/`ChildCount` accumulate correctly when 7F-C-axis is present on both sides. 90%+ coverage. | — |
| **7F-D.2** | Persistence — EF migration `Phase7FD_AddHeadCountDeltaToRegistrationAddition` per §2.4 with CHECK constraint via `migrationBuilder.Sql`. EF jsonb config + repo unchanged. | Round-trip mutation test for `head_count_delta` (memory 6A.129); round-trip for `TierCounts.Count` change inside the delta; CHECK-constraint enforcement test (insert mode=0 + non-null delta → constraint violation). | `deploy-staging.yml` |
| **7F-D.3** | Application + API + Webhook (architect edit #12: merged) — `InitiateAddHeadCountCommand` + handler. **Architect Q1 call**: extend existing `IRegistrationCheckoutService` (from 7E.3b) with `InitiateAdditionCheckoutAsync` so Mode A + Mode B initiate paths share Stripe wiring. **Architect Q2 call**: new endpoint `POST /api/registrations/{id}/add-headcount` (separate from existing `/add-attendees`). Body: `{ headCountDelta: HeadCountDto, tierCountsDelta?: TierCountDto[], successUrl, cancelUrl, notifyAttendees? }`. For free events, returns `201` + the merged registration; for paid events, returns Stripe Checkout URL. **Webhook handler**: extend `AddAttendeesPaymentCompletedHandler` to dispatch by `RegistrationAddition.IsModeBAddition` to either the existing Mode A merge OR the new `Registration.MergeHeadCountAddition`. **Idempotent guard** (architect edit #9): on replay, if `addition.Status != PaymentCompleted`, no-op silently. | Handler tests with Mock<IRepo>; cents-exact Stripe assertion pattern from 7E.3b/c; integration test via TestServer; webhook replay-safety (replay twice → second fires no-op); double-pending rejection test; mode-mismatch endpoint-misuse test (Mode A registration submits to `/add-headcount` → 400). | `deploy-staging.yml` |
| **7F-D.4** | Frontend — branch [`AddAttendeesModal`](../web/src/presentation/components/features/events/AddAttendeesModal.tsx) on `event.registrationMode`. Mode A keeps its existing per-attendee form (regression test required). Mode B gets a head-count delta form (B2 spinner pair, B4 4-leaf, etc.) plus tier-count delta when `event.ticketingMode === 'Tiered'`. **Reuses `HeadCountRsvpForm`'s spinner components** — no copy-paste. Per-tier-per-age selector reused from 7F-C.3 (no feature flag). Submit button shows the calculated `AdditionalAmount` for paid events. | RTL: Mode-A path unchanged (regression); Mode-B path B1/B2/B3/B4 submission shapes; price-preview accuracy; tier-counts delta validation; per-tier-age sum-mirror invariant when 7F-C-axis is shown; double-pending state shows existing pending payment instead of opening fresh form. | `deploy-ui-staging.yml` |
| **7F-D.5** | Email — `event-add-attendees-confirmation` template gains Mode-B variant. **Architect edit #10**: email body lists *both* the delta (what they paid for) and the new totals (for context — `"You added 1 adult; your registration is now 5 (3 adults · 2 children)"`). Reuses `HeadCountEmailFormatter.Compute`-style helpers; HTML body extended via embedded-resource seeder per memory 7C.2 / 6A.117. | Template-validation passes at startup; rendered-content unit tests for B1/B2/B3/B4 deltas; legacy Mode A regression. | `deploy-staging.yml` |
| **7F-D.6** | Staging end-to-end smoke — paid B2+tiered event, register `(2A,1C)` for $X; add `+1A` via the new flow → Stripe Checkout opens for the tier-adult delta amount → complete via Stripe test mode → registration's `HeadCount.Total` = 4, `Demographics.Adults` = 3, `Demographics.Children` = 1. **Mode A regression**: the same `add-attendees` flow on a Mode A event still works end-to-end. **Cents-exact Stripe assertion**. **Cross-doc**: tier-deactivation mid-flight smoke — register, organiser deactivates a tier in the addition, complete payment → addition transitions to `Failed`, organiser dashboard shows the failure. | — | — |

**Tracking-doc updates** after every slice per CLAUDE.md §7.

---

## 4. Risks & guards

| Risk | Mitigation |
|---|---|
| Architect-cited fork risk: a parallel command + handler + service for Mode B that diverges from Mode A's pricing/Stripe wiring over time | Slice 7F-D.3 extends `IRegistrationCheckoutService` (architect Q1). Both initiation paths route through one method that takes a discriminated payload. |
| Stripe webhook ordering — webhook fires before the user lands on the success page; merge-then-redirect race | Same idempotent merge pattern as Mode A's `AddAttendeesPaymentCompletedHandler`. `RegistrationAddition.Status` transitions are guarded by domain rules. |
| Mode-mismatch addition (Mode A registration receives a head-count delta, or vice versa) | Validator rejects at command level; domain-level `MergeHeadCountAddition` also rejects defensively. Endpoint-misuse case rejected with explicit error message (architect Q7). |
| Capacity over-spend — addition pushes registration past `MaxAttendeesPerRegistration` or event capacity | Both checks performed in domain BEFORE pricing — fails fast with clear messages, no Stripe session created. |
| Pricing drift between Mode A and Mode B for the *same* delta basket | Both paths route through `Event.CalculateHeadCountPrice` via 7F-C / 7E.3c. Architect-required parity test in slice 7F-D.6. |
| Tier-name drift between addition initiation and Stripe-completion merge — organiser renames tier in the meantime | `TierCount.TierName` already snapshotted at axis-creation time; merge uses the snapshot. |
| Tier deactivation/deletion between creation and merge (architect edit #3) | Domain rejects at merge; addition transitions to `Failed`. Refund of Stripe-charged amount is out of scope; surface to organiser dashboard. |
| Event start-date crossing the registration window between creation and merge (architect edit #4) | Domain rejects at merge; addition transitions to `Failed`. Same refund-out-of-scope note. |
| Per-tier `TicketTier.Reserve` accounting during merge | Architect edit #2 — atomic reserve per tier; abort on first failure. Mirrors Mode A. |
| Concurrent `RegistrationAddition` creation — two browsers open the modal, both create additions, both pay (architect Q8) | Domain rejects second initiation: only one `Pending` addition per registration. |
| Conversion (7F-B) lands on a registration with a pending addition | 7F-B explicitly rejects this case (cross-doc enforcement). 7F-D doesn't need to handle it. |

---

## 5. Out of scope

- **Add-attendees with mode CHANGE in the same operation** — i.e. "add 3 attendees and switch to B4". Forbidden; mode-change goes through 7F-B; add-attendees keeps the current mode.
- **Bulk additions across multiple registrations** (e.g., organiser-initiated "add 5 to every registration") — separate ops/UX problem.
- **Refund-on-shrink** — i.e., "I had 5, now we're 3, refund the difference". Conceptually inverse of 7F-D; out of scope.
- **AddAttendees → tier change** — moving an attendee from VIP to General mid-event. Same answer.
- **Auto-refund on tier-deactivation / event-window-close mid-merge** — domain transitions addition to `Failed`; organiser-initiated refund handled via existing refund flows.

---

## 6. Architect questions — answered

| # | Question | Architect call |
|---|---|---|
| Q1 | Shared service shape — extend existing `IRegistrationCheckoutService` or new `IRegistrationAdditionCheckoutService`? | **Extend** the existing service with `InitiateAdditionCheckoutAsync`. Discriminator at method level. |
| Q2 | API endpoint shape — new endpoint or extend existing `add-attendees`? | **New endpoint** `POST /api/registrations/{id}/add-headcount`. Different validation rules; conflating them creates a JSON-discriminator nightmare. |
| Q3 | Persist a `tier_counts_delta_summary` jsonb cache, or compute on read? | **Compute on read.** Drop the column from §2.4. |
| Q4 | 7F-C-first vs 7F-D-first | **7F-C first** (cross-doc §). |
| Q5 | Mode-B addition email — to attendee, organiser, or both? | **Same as Mode A** — attendee only; organiser dashboard reflects the delta. |
| Q6 | Capacity check — fail or auto-waitlist? | **Fail.** Match Mode A. |
| Q7 (architect-added) | API handles a Mode-A registration submitted to the new endpoint by mistake | **400 Bad Request** with explicit message: "use /add-attendees for this registration." |
| Q8 (architect-added) | Cap on simultaneous pending `RegistrationAddition` rows per registration | **1.** Reject second initiation: "you have a pending payment; complete or cancel it first." |

---

## 7. Pre-conditions

| # | Item | Status |
|---|---|---|
| 1 | `Event.CalculateHeadCountPrice` shipped + tested (Phase 7E.3b) | ✅ live |
| 2 | `IRegistrationCheckoutService` shipped (Phase 7E.3b) — extension target for shared Stripe wiring | ✅ live |
| 3 | `RegistrationAddition` aggregate exists with EF mapping + Stripe lifecycle | ✅ existing |
| 4 | `AddAttendeesPaymentCompletedHandler` is idempotent on webhook replay | ✅ verified during 7E.3b smoke |
| 5 | Architect Q1 + Q2 + Q4 + Q7 + Q8 ratified | ✅ ratified by review iteration 1 (this doc) |
| 6 | **7F-C live** — `TierCount.AdultCount` / `ChildCount` axis available so the AddAttendeesModal's tiered B-mode form has the per-tier-per-age selector without a feature flag | ⏳ blocks on 7F-C |
| 7 | **7F-B's "no pending addition during conversion" enforcement** in place — decouples 7F-D's lifecycle from B's. (7F-D doesn't *strictly* depend on 7F-B since 7F-B explicitly rejects the conflict; but shipping in this order avoids any race window.) | ⏳ ideally 7F-B live first |
