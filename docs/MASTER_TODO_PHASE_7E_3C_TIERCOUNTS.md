# Master TODO — Phase 7E.3c: Paid B-mode RSVP with TierCounts axis

**Status**: ✅ ARCHITECT-APPROVED with 5 edits applied (review iteration 1, 2026-04-29) — awaiting user sign-off, then implementation.
**Origin**: continuation of Phase 7E.3b (paid Mode-B RSVP). 7E.3b shipped single-price + dual-price (Adult/Child) paid B-mode and gated TierCounts behind `RegistrationModeErrorCodes.PaidHeadCountTiersDeferred`. 7E.3c lifts that gate.
**Classification (per architect RCA pattern)**: **feature missing** — TierCounts pricing for paid Mode-B RSVP. Validator already accepts the mode/shape combination; the gate lives in `Event.CalculateHeadCountPrice` + a defensive check on `TicketingMode == Tiered`.
**Out of scope (Phase 7F)**: tier × age matrix pricing — applying both TierCounts AND dual-price age axis simultaneously. Today, an event is EITHER `TicketingMode.Tiered` OR `SingleTier` (mutually exclusive), so this question only arises if a future change relaxes that mutual exclusion.

---

## 1. What's in scope

| Mode | TicketingMode = Tiered | Pricing path |
|---|---|---|
| B1 HeadCountOnly | ✅ (TierCounts required) | `sum(tier.AdultPrice × count)` |
| B2 HeadCountByAge | ✅ (TierCounts required; demographics ignored for pricing) | `sum(tier.AdultPrice × count)` |
| B3 HeadCountByGender | ✅ (TierCounts required) | `sum(tier.AdultPrice × count)` |
| B4 HeadCountByAgeAndGender | ✅ (TierCounts required; demographics ignored for pricing) | `sum(tier.AdultPrice × count)` |

**Why "demographics ignored for pricing" on B2/B4**: tiered events use TIER pricing (per-tier flat rate), not age-based dual pricing. The demographic axis still captures **organiser reporting** info (how many adults vs children attended) but doesn't drive the bill. This matches Mode A's behaviour today: when `TicketingMode == Tiered`, `Event.CalculateTieredPriceForAttendees` is called instead of `CalculatePriceForAttendees` (which honours dual age pricing).

**Sum invariant**: `sum(tierCounts.Count) == headCount.Total` — already enforced by `HeadCountBreakdown` factories (verified in 7E.1 unit tests). Domain re-asserts defensively.

**Out of scope (Phase 7F per architect plan §12)**: actual tier × age matrix pricing where each tier has separate adult/child prices. Today `TicketTier.AdultPrice + ChildPrice` exist but Mode A's `CalculateTieredPriceForAttendees` uses only `tier.AdultPrice` for non-attendee pricing — same simplification carries to 7E.3c.

---

## 2. Layer-by-layer changes

### Domain (`src/LankaConnect.Domain`)

- [`Event.RegistrationMode.cs : CalculateHeadCountPrice`](../src/LankaConnect.Domain/Events/Event.RegistrationMode.cs):
  - **Lift the two `PaidHeadCountTiersDeferred` gates** (the `if (TicketingMode == Tiered)` block and the `if (headCount.TierCounts != null)` block).
  - Add new branch BEFORE the existing dual-pricing / standard branches:
    ```csharp
    if (TicketingMode == Enums.TicketingMode.Tiered)
    {
        // Architect-required: NO fork of pricing math. Resolve each TierCount against
        // the event's TicketTiers + sum (tier.AdultPrice × count) using Money arithmetic.
        return CalculateTierCountsPrice(headCount.TierCounts);
    }
    ```
  - New private helper `CalculateTierCountsPrice(IReadOnlyList<TierCount>? tierCounts)`:
    - Reject if null/empty (TierCounts required for tiered events).
    - For each `TierCount`: look up `_ticketTiers.FirstOrDefault(t => t.Id == tc.TierId)` — defensive 404 if tier missing.
    - Sum `tier.AdultPrice.Multiply(tc.Count)` — Money arithmetic preserves currency.
    - Return aggregate `Money`.
    - **Architect edit #4**: inline code comment `// Parity with Mode A's Event.CalculateTieredPriceForAttendees — uses tier.AdultPrice for all attendees regardless of category. ChildPrice for tiered pricing belongs in Phase 7F (tier × age matrix).` so the next reader sees the deliberate parity.

- **`PaidHeadCountTiersDeferred` constant**: stays as no-op for one release (architect convention, mirrors `PaidHeadCountDeferred`).

- **Sum-invariant defensive check** in `RegisterWithHeadCount`: already enforced by `HeadCountBreakdown.ForByAge/ByGender/etc.` factories. Add a unit test that sums `TierCounts.Count` ≠ `Total` triggers factory-level Failure (regression guard for 7E.1 work).

- **Architect edit #2**: per-tier capacity reservation moved to `RegisterWithHeadCount` BEFORE the price-calculation branches. Mirrors Mode A's behaviour (`tier.Reserve(count)` per tier on line 446-451 of `Event.cs`). Applies to BOTH free and paid tiered events (free events still need to prevent over-selling a tier). Atomic — if any tier reservation fails, the whole RSVP rejects and no partial reserve is held.

### Application (`src/LankaConnect.Application`)

- **No handler changes expected**. The handler already builds `TierCount` VOs from request DTOs and resolves names from `event.TicketTiers` (lines 1086-1102 of `RsvpToEventCommandHandler.HandleHeadCountRsvp`). The pricing helper will start returning success instead of `PaidHeadCountTiersDeferred`. Existing Stripe-checkout wiring through `IRegistrationCheckoutService` works unchanged.

- **Anonymous handler**: same — already resolves tier counts via `event.TicketTiers`.

### Frontend (`web/src`)

- [`web/src/presentation/components/features/events/HeadCountRsvpForm.tsx`](../web/src/presentation/components/features/events/HeadCountRsvpForm.tsx):
  - **New tier-count selector** rendered when `event.ticketingMode === 'Tiered'` AND mode is any B variant.
  - For each `event.ticketTiers[i]`: render a counter (label "VIP — $50", value `tierCounts[i].count`, +/- buttons).
  - **Sum mirror**: display "Total: 5 attendees" computed from `sum(tierCounts.count)`. Auto-updates as user changes tier counts.
  - **B2/B4 + tiered (architect edit #3)**: BOTH tier-count selector AND demographic spinners are shown. Demographics capture organiser-reporting info (how many adults vs children attended) but DON'T drive pricing. Render a small italic helper line under the demographic block: *"Demographics are for organiser reporting only — pricing is per tier"* to prevent user confusion about double-paying.
  - **B1/B3 + tiered**: tier-count selector only (no demographic spinners — the mode doesn't capture them).
  - **Validation**: form rejects submit if `sum(tierCounts) === 0`. For B2/B4, also enforce `sum(demographics) === sum(tierCounts)` so the user doesn't over- or under-report demographics.
  - On submit, build `headCount.tierCounts: TierCountDto[]` payload alongside the demographic counts (B2/B4) or alone (B1/B3).

- TS types already include `tierCounts?: TierCountDto[]` on `HeadCountDto` (shipped in 7E.5 — verify).

### Frontend tests

- Update `RsvpFormSection.test.tsx` if the dispatch logic changes (it shouldn't — the form rendering choice still happens inside `HeadCountRsvpForm`).
- Add `HeadCountRsvpForm` paid + tiered RTL test if architect requires it (otherwise rely on handler tests).

---

## 3. TDD plan

### RED — domain pricing tests (new file `Phase7E3cTierCountsPricingTests.cs`)

- `RsvpToEvent_ModeB1Paid_TierCounts_PricesCorrectly` — VIP=$50 × 2 + General=$30 × 3 → $190 EXACT.
- `RsvpToEvent_ModeB3Paid_TierCounts_PricesIdenticallyToB1` (tier prices ignore gender).
- `RsvpToEvent_ModeBPaid_TierCounts_RejectsUnknownTierId` — passing a `tierId` not on the event → clear failure.
- `RsvpToEvent_ModeBPaid_TierCounts_SumNotEqualsTotal_RejectedAtFactoryLevel` — regression on the 7E.1 invariant.
- **Architect edit #1 (drop)**: NO new tier-rename snapshot test. Existing 7E.1 JSON round-trip + handler-level resolution at registration time already cover snapshot semantics. Skipped to avoid duplicate coverage.
- **Architect edit #2 (capacity)**: `RsvpToEvent_ModeBPaid_TierCounts_RejectsWhenTierOversold` — RSVP that exceeds VIP capacity → whole RSVP rejected, no partial reserve held (atomic).
- **Architect edit #2 (race)**: `RsvpToEvent_ModeBPaid_TierCounts_TwoConcurrentRsvps_OnlyOneSucceeds` — two RSVPs racing the last seat in a tier — second fails cleanly with capacity error.

### RED — Mode A vs Mode B parity test (architect-required from 7E.3b plan)

- `RsvpHandler_PaidBwithTiers_HasIdenticalTotalPrice_To_PaidA_WithSameBasket` — same basket: 2 attendees with `TicketTierId = VIP` + 3 attendees with `TicketTierId = General` (Mode A) vs `TierCounts = [VIP × 2, General × 3]` (Mode B) → identical `Money` value. Anti-fork guard.

### RED — Stripe wiring (regression — confirm 7E.3b path still works)

- `RegistrationCheckoutService_PaidB_TierCounts_PassesExactAmount_ToStripe` — same shape as 7E.3b's existing test but with TierCounts. Asserts the cents-exact value flows through unchanged.

### GREEN order

1. New `CalculateTierCountsPrice` helper + the 5 RED domain pricing tests → green.
2. Lift the two `PaidHeadCountTiersDeferred` gates in `CalculateHeadCountPrice`.
3. Mode A vs Mode B parity test → green (no new code; the helpers should agree by construction).
4. Frontend tier-count selector + form-state integration.
5. Full regression suite green.

---

## 4. End-to-end staging Stripe smoke (architect-gated, post-deploy)

```bash
TOKEN=$(...login...)

# 1. Create paid B2 event with TIERED ticketing (VIP=$50, General=$30)
curl -X POST '.../api/Events' -d '{
  "title": "7E.3c paid B2 + TierCounts smoke",
  "isFree": false,
  "ticketingMode": "Tiered",
  "ticketTiers": [
    {"name":"VIP","adultPriceAmount":50,"adultPriceCurrency":"USD","totalCapacity":10},
    {"name":"General","adultPriceAmount":30,"adultPriceCurrency":"USD","totalCapacity":40}
  ],
  "registrationMode":"HeadCountByAge",
  ...
}'

# 2. Publish.

# 3. RSVP with tier counts: VIP × 2 + General × 3 = $190 = 19000 cents
curl -X POST '.../api/events/<id>/rsvp' -d '{
  "userId":"...","leadAttendeeName":"Niroshana",
  "headCount":{"total":5,"adults":3,"children":2,"tierCounts":[
    {"tierId":"<vip-id>","count":2},
    {"tierId":"<gen-id>","count":3}
  ]},
  "email":"...","phoneNumber":"...","successUrl":"...","cancelUrl":"..."
}'
# Expected: HTTP 200 + Stripe URL.
# Verify via /my-registration: totalPriceAmount=190.0 EXACT.
```

Repeat with B1 + tiers. Both must produce cents-exact totals.

---

## 5. Slice ordering + commit boundaries

```
Slice 1: Domain (TierCounts pricing helper + lift gates) + tests          → 1 commit, deploy-staging
Slice 2: Frontend (tier-count selector in HeadCountRsvpForm)              → 1 commit, deploy-ui-staging
Slice 3: API smoke + tracking docs                                          → 1 commit
```

Question for architect: should Slice 1 stay separate from Slice 2 (typical pattern) OR merge if the gate-removal could leak before the FE ships? My read: keep separate — gate removal makes API accept tier-counts; API is well-tested via 7E.3b's wiring + the new domain tests. FE just exposes the new shape. Low blast risk.

---

## 6. Risk register + mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Pricing math off (incorrect tier price summed) | Medium | HIGH (real money) | Architect-required parity test (Mode A vs Mode B same basket) + cents-exact Stripe assertion. |
| TierName snapshot drift on tier rename | Low | Medium | Snapshot test asserts rename doesn't propagate to existing registrations. |
| Sum invariant bypass (tierCounts sum ≠ Total) | Low | Medium | `HeadCountBreakdown` factories enforce this; defensive test added. |
| Webhook fails to confirm Mode B + tiered registration | Low | HIGH | Existing webhook handler is mode-agnostic; same code path as 7E.3b. Smoke covers it. |
| Frontend tier-count selector confuses mode + demographics | Medium | Low | UX decision: hide demographic spinners when `ticketingMode === 'Tiered'` (architect Q below). |

---

## 7. Architect questions (for review)

1. **Slice 1+2 merging**: keep separate (my preference) or merge?
2. **B2/B4 + tiered ticketing UX** — when an event is tiered AND mode is B2/B4, should the form:
   - (a) Show tier-count selector ONLY (hide adults/children spinners — pricing is per-tier; demographics aren't useful)
   - (b) Show BOTH tier-count selector AND demographic spinners (capture demographics for organiser reporting; pricing still per-tier)
   - My recommendation: (b). Demographics ARE useful for the organiser even when pricing is tier-driven.
3. **B1/B3 + tiered**: tier-count selector only (no demographic spinners). Confirm.
4. **Anonymous handler**: same wiring as auth (it already is). Confirm no additional anon-specific tests required beyond reusing the parity test.
5. **Refund invariant for tier-counts registrations**: `Registration.RequestRefund` is mode-agnostic; existing 7E.3b refund test covers this. No new refund test needed. Confirm.
6. **TierCount snapshot on rename**: my plan tests this domain-level. Should there also be a handler-level test (handler resolves tier names from `event.TicketTiers` at registration time — the snapshot is built there)? My read: domain test covers the invariant; handler test would be redundant.
7. **Capacity/tier capacity guard**: today's flow checks event capacity but tiered ticketing has per-tier capacity. Mode A's `RegisterWithAttendees` does `tier.Reserve(count)` per tier (line 446-451). Should `RegisterWithHeadCount` mirror this for TierCounts? My read: yes — defensive, prevents over-selling a tier. Adds 1-2 tests.

---

## 8. Definition of Done

- [ ] 6+ RED→GREEN domain pricing/capacity tests + 1 parity test
- [ ] Both `PaidHeadCountTiersDeferred` gates lifted from `CalculateHeadCountPrice`
- [ ] Per-tier capacity reservation (`tier.Reserve`) added to `RegisterWithHeadCount` — applies to free + paid tiered events (architect edit #2)
- [ ] Frontend tier-count selector in HeadCountRsvpForm + B2/B4 demographic-helper-text (architect edit #3) + integration with form state
- [ ] All deploys (backend + UI) `conclusion=success`
- [ ] Stripe end-to-end smoke: B1 + tiers + B2 + tiers each create Stripe sessions with EXACT integer-cents amounts
- [ ] **Architect edit #5**: capacity-overflow smoke — RSVP exceeding tier capacity → clean 4xx, NO Stripe session created, no stuck tier reservation
- [ ] **Architect edit #5**: idempotency assertion — same TierCounts payload retried doesn't double-reserve
- [ ] 7E.3b regression: paid B without tiers still works (single + dual price)
- [ ] Free B regression: free B + tiers + tier counts (free events with tiers — should pass through to TotalPrice = 0; tier capacity STILL reserved per architect edit #2)
- [ ] Mode A regression: paid Mode A with tiered ticketing still works (no fork in pricing math)
- [ ] Container logs scanned post-deploy: zero unexpected exceptions
- [ ] PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN updated
- [ ] `MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md`: 7E.3c gate-removal note in 7F section if applicable

---

## 9. Architect notes

**Review iteration 1** (2026-04-29, 5 edits applied):

| # | Edit | Where applied |
|---|---|---|
| 1 | Drop the tier-rename snapshot test (already covered by 7E.1 round-trip + handler resolution) | §3 RED list |
| 2 | Move `tier.Reserve(count)` per TierCount to `RegisterWithHeadCount` BEFORE pricing branches; applies to free + paid tiered events | §2 Domain + §3 RED |
| 3 | B2/B4 + tiered: render BOTH tier-count selector AND demographic spinners + helper text "Demographics are for organiser reporting only — pricing is per tier" | §2 Frontend |
| 4 | Code comment in `CalculateTierCountsPrice` referencing Mode A's `CalculateTieredPriceForAttendees` for the deliberate AdultPrice-only parity | §2 Domain |
| 5 | Add capacity-overflow + idempotency assertions to Stripe smoke | §8 DoD |

Skipped: paid-B+tiered refund regression test — 7E.3b coverage + mode-agnostic refund handler is enough.
