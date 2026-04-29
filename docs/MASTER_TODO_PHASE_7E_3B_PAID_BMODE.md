# Master TODO — Phase 7E.3b: Paid B-mode RSVP + Stripe Checkout

**Status**: ✅ ARCHITECT-APPROVED with 5 edits applied (review iteration 1, 2026-04-29) — awaiting user sign-off, then implementation.
**Origin**: continuation of Phase 7E. Slice 7E.3a shipped FREE B-mode; the paid path was deferred per architect risk #5 ("Stripe `TotalPrice` for paid HeadCountByAge / TierCounts — gated 7E.3b/c sub-slices with explicit amount-calc tests").
**Out of scope (lands in 7E.3c)**: TierCounts axis pricing (e.g. "VIP × 2 + General × 3"). 7E.3b ships single-price + dual-price (Adult/Child) paid B-mode only.
**Classification (per architect RCA pattern)**: **feature missing** — paid B-mode + Stripe checkout. Validator already accepts paid+B "in target state" but is currently gated by the `PaidHeadCountDeferred` rule shipped in 2026-04-29; this slice ships the implementation and lifts the gate.

---

## 1. What's in scope

| Mode | Single price | Dual price | Tiered |
|---|---|---|---|
| B1 HeadCountOnly | ✅ | n/a (B1 + dual already excluded by validator) | 7E.3c |
| B2 HeadCountByAge | ✅ | ✅ | 7E.3c |
| B3 HeadCountByGender | ✅ | n/a (B3 + dual already excluded) | 7E.3c |
| B4 HeadCountByAgeAndGender | ✅ | ✅ (derive adults/children from 4 leaves) | 7E.3c |

Pricing semantics (mirrors Mode A):
- **B1 single price**: `TotalPrice = Total × ticketPrice`
- **B2 dual price**: `TotalPrice = Adults × adultPrice + Children × childPrice`
- **B3 single price**: same as B1 (gender doesn't affect price; just demographic capture)
- **B4 dual price**: `Adults = AM + AF`, `Children = CM + CF`, then `Adults × adultPrice + Children × childPrice`

What's NOT in this slice:
- TierCounts axis (7E.3c)
- Paid B-mode add-attendees delta-payment (7E.4 chunk dependency)
- Paid B-mode email template variants (7E.4 follow-up; existing v2 templates already populate `HasHeadCount` so they render correctly — no new template seeding required for 7E.3b)

---

## 2. Layer-by-layer changes

### Domain (`src/LankaConnect.Domain`)

- [`Event.RegistrationMode.cs`](../src/LankaConnect.Domain/Events/Event.RegistrationMode.cs): inside `RegisterWithHeadCount`:
  - Remove the "free events ONLY" guard at lines 207-212.
  - Add a `CalculateHeadCountPrice(headCount)` private helper that produces `Money` based on the event's pricing mode + B-mode shape (see pricing table above).
  - For B1/B3 + dual pricing: defensive failure (compatibility validator already excludes this combo, but defence-in-depth — the new tests assert the exact failure message).
  - Build registration via `Registration.CreateWithHeadCount(...)` with `isPaidEvent: true` (existing parameter; matches Mode A path).
  - Handle `Status = Preliminary` correctly — `CreateWithHeadCount` already does this for paid events (free returned `Confirmed`, paid returns `Preliminary`).

- [`RegistrationModeCompatibility.cs`](../src/LankaConnect.Domain/Events/Services/RegistrationModeCompatibility.cs):
  - **Lift the existing gate**: remove the `PHASE_7E_3B` `IsFreeAttendance` check inside `CheckCommonHeadCountConstraints`. The `RegistrationModeErrorCodes.PaidHeadCountDeferred` constant stays (one-release no-op).
  - **Add a NEW gate (architect edit #3)** for TierCounts in paid B-mode until 7E.3c ships:
    - New constant `RegistrationModeErrorCodes.PaidHeadCountTiersDeferred`.
    - Reject in `CheckCommonHeadCountConstraints` (or in the RSVP handler) when `headCount.tierCounts != null && !@event.IsFree()`.
    - Inline `// PHASE_7E_3C: remove this gate when paid B-mode + TierCounts ships` breadcrumb.
    - Without this gate, lifting the 7E.3b gate would expose paid B-mode + tier counts with no pricing path → undefined behaviour. Architect-required.

- [`RegistrationModeCompatibilityTests.cs`](../tests/LankaConnect.Application.Tests/Events/Domain/Phase7E2RegistrationModeCompatibilityTests.cs): revert rows 5/7/8/9 to target-state expectations:
  - Row 5: "Paid single price → A + all B (no C)"
  - Row 7: "Paid dual pricing → A, B2, or B4"
  - Row 8: "Paid + group-tier discount → A + all B (no C)"
  - Row 9: "Paid + ticket tiers → A + all B (no C)"
  - Remove `Check_Fails_WithPaidHeadCountDeferred_ForPaidEvents` and `Check_Succeeds_ForFree_BModes_UnchangedByPaidGate` (both no longer relevant after gate removal).
  - Keep `AllowedModes_ExcludesAllBModes_ForPaidEvents` invariant test — but flip its expectation to "paid context now INCLUDES B-modes".

### Application (`src/LankaConnect.Application`)

- **NEW** [`src/LankaConnect.Application/Events/Services/IRegistrationCheckoutService.cs`](../src/LankaConnect.Application/Events/Services/IRegistrationCheckoutService.cs) (architect edit #2): dedicated service `CreateSessionAsync(@event, registration, successUrl, cancelUrl, ct)`. Encapsulates revenue-breakdown calculation + Stripe session creation + storing session ID. Single test surface for the money path. Implementation in `Infrastructure` (or `Application` if it has no Stripe SDK touchpoint beyond `IStripePaymentService`).

- [`RsvpToEventCommandHandler.HandleHeadCountRsvp`](../src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs): after `RegisterWithHeadCount` succeeds and `!@event.IsFree() && registration.TotalPrice.Amount > 0`, call `_checkoutService.CreateSessionAsync(...)` and return the Stripe URL. NO inline wiring.

- [`RsvpToEventCommandHandler.HandleMultiAttendeeRsvp`](../src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs) (Mode A path): **migrate to the new service in the SAME commit** (architect edit #2 — anti-fork). The ~80 lines of inline Stripe wiring move to the service implementation. Mode A regression covered by existing handler tests + new service unit test.

- [`RegisterAnonymousAttendeeCommandHandler`](../src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/): same pattern — anonymous head-count + multi-attendee paths both call the new service.

### Frontend (`web/src`)

- [`HeadCountRsvpForm.tsx`](../web/src/presentation/components/features/events/HeadCountRsvpForm.tsx): remove the paid-event short-circuit at lines 127-133. Add Stripe redirect on success — copy the existing pattern from `EventRegistrationForm.tsx` paid-event onSubmit.
- [`useEvents.ts:useRsvpToEvent`](../web/src/presentation/hooks/useEvents.ts): already handles the `checkoutUrl` return for Mode A; verify it works identically for Mode B (no change expected).
- No mode picker changes required — the validator gate removal cascades to `GetAllowedRegistrationModes` automatically.
- Frontend `RsvpFormSection`: no change. The `registrationModeStatus = 'active'` will start emitting for paid+B events automatically once the gate is removed; this means the `HeadCountRsvpForm` renders instead of the "coming soon" panel.

### Frontend tests

- Update `RsvpFormSection.test.tsx`: the test `'renders HeadCountRsvpForm for free Mode B'` should be generalised to "renders HeadCountRsvpForm for any active Mode B" (paid or free).
- Update `HeadCountRsvpForm` tests if any (none exist today; if added, must cover paid-event Stripe redirect).

---

## 3. TDD plan

### RED — domain pricing tests

- `RsvpToEvent_ModeB1Paid_SinglePrice_TotalPriceEquals_TotalTimesPrice`
- `RsvpToEvent_ModeB2Paid_DualPrice_TotalPriceEquals_AdultsTimesAdultPrice_PlusChildrenTimesChildPrice`
- `RsvpToEvent_ModeB4Paid_DualPrice_DerivesAdultsAndChildren_FromFourLeaves_AndPricesCorrectly`
- `RsvpToEvent_ModeB3Paid_SinglePrice_TotalPriceEquals_TotalTimesPrice` *(parity with B1)*
- `RsvpToEvent_ModeB1Paid_DualPricing_Rejected` (defensive — should never happen via validator but domain enforces too)
- `RsvpToEvent_ModeB3Paid_DualPricing_Rejected` (same)

### RED — Stripe wiring tests

- `RsvpHandler_ModeBPaid_CreatesStripeCheckoutSession_WithCorrectAmount` — mock `IStripePaymentService`, assert called with `amount` in cents matching the calculated `TotalPrice`.
- `RsvpHandler_ModeBPaid_ReturnsStripeCheckoutUrl` — handler returns the session URL.
- `RsvpHandler_ModeBPaid_RegistrationStatus_IsPreliminary` — paid path leaves the row in Preliminary until webhook completes payment.
- `RsvpHandler_ModeBFree_StillReturnsNullCheckoutUrl_AndConfirmedStatus` — regression: free B path unchanged.

### Architect-required parity test

- `RsvpHandler_PaidB2_HasIdenticalTotalPrice_To_PaidA_WithSameAdultChildCounts` — same basket via Mode A's `CalculatePriceForAttendees` and Mode B2's new pricing helper produces the same `Money` value. Anti-fork guard.

### Architect-required refund test (edit #4)

- `RefundHandler_PaidBRegistration_RefundsTotalPrice_Successfully` — exercise the existing refund handler against a paid B-mode registration (no new functionality, just a regression guard). Existing handler reads `Registration.TotalPrice` (mode-agnostic) so should work; the test prevents a future change from silently breaking it.

### Webhook parity flag (architect non-blocking)

- If existing webhook tests parameterise by registration shape, add a Mode-B row asserting Preliminary → Confirmed transition. If not, the smoke step covers it.

### GREEN order

1. Domain `CalculateHeadCountPrice` helper + tests → green
2. Remove "free only" guard in `RegisterWithHeadCount` + paid path tests → green
3. Lift validator gate + revert compatibility test rows → green
4. Application handler Stripe wiring (auth + anonymous) + handler tests → green
5. Frontend short-circuit removal + RTL test update → green
6. Full suite green + zero TS errors

---

## 4. Stripe end-to-end smoke (architect-gated, post-deploy)

```bash
# Setup: free → paid+B2 event
TOKEN=$(...login...)

# 1. Create paid B2 event with dual pricing (15 adult / 7 child)
curl -X POST '.../api/Events' -d '{
  "title": "7E.3b paid B2 smoke",
  "isFree": false, "ticketPriceAmount": 15,
  "enableDualPricing": true, "adultPriceAmount": 15, "childPriceAmount": 7,
  "registrationMode": "HeadCountByAge", ...
}'

# 2. Publish.

# 3. RSVP — 2 adults + 1 child = $37
curl -X POST '.../api/events/<id>/rsvp' -d '{
  "userId": "...", "leadAttendeeName": "Niroshana",
  "headCount": {"adults": 2, "children": 1},
  "email": "niroshhh@gmail.com", "phoneNumber": "+18609780124",
  "successUrl": "...", "cancelUrl": "..."
}'
# Expected: HTTP 200 + Stripe redirect URL.
# Verify amount via Stripe dashboard test mode: $37.00 = 3700 cents.

# 4. Complete payment with test card 4242 4242 4242 4242 (any CVC, future date).

# 5. Verify webhook hit: GET event detail → currentRegistrations: 3
# 6. Verify registration: paymentStatus=Completed, status=Confirmed
# 7. Verify confirmation email landed (ACS sent log).
```

Repeat with B4 dual price + B1 single price + B3 single price.

---

## 5. Slice ordering + commit boundaries (architect-approved)

```
Slice 1+2 (MERGED per architect edit #1):
  Domain pricing helper + remove "free only" guard + lift validator gate
  + add PaidHeadCountTiersDeferred gate + revert compatibility tests
  → 1 commit, deploy-staging
Slice 3: IRegistrationCheckoutService extraction + Mode A migration + Mode B paid wiring
  → 1 commit, deploy-staging
  (Stays separate so handler regression is bisectable.)
Slice 4: Frontend short-circuit removal + RTL test
  → 1 commit, deploy-ui-staging
Slice 5: Stripe end-to-end smoke (B1+B2+B4 cents-exact) + paid-B refund test + tracking docs
  → 1 commit (refund test + docs), 0 commits for smoke (evidence captured)
```

Architect rationale (edit #1): merging Slice 1+2 prevents the dead state where the validator
says "paid + B = OK" but the handler still rejects (or worse, accepts at zero amount).
Slice 3 stays separate because the service extraction touches Mode A code and a regression
must be bisectable.

---

## 6. Risk register + mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Stripe amount calc off by one cent | Medium | HIGH (real money) | Architect-required parity test (Mode A vs Mode B2 same basket) + amount-in-cents assertion. |
| Webhook handler drops Mode B Preliminary → Confirmed transition | Low | HIGH | Verify webhook end-to-end in smoke step 4-6. Existing webhook handler reads `Registration.TotalPrice` not mode-specific data; no change expected. |
| Free B-mode regression (gate removal accidentally breaks free path) | Low | Medium | Existing free B-mode unit tests remain; smoke against `16eeb15c-…` (existing free B2 event). |
| Mode A regression (handler refactor) | Low | High | Mode A tests in the impacted suite remain; smoke against any paid Mode A event. |
| Add-attendees / cancel / refund flows misbehave for paid B | Medium | Medium | Out of scope for 7E.3b — those are 7E.4 dependencies. Test that EXISTING flows don't crash on a paid B registration; don't ship new functionality. |
| Email template assumes free-event copy | Low | Low | v2 templates from 7E.4 chunk 1 already populate `HasHeadCount` regardless of paid/free. Verify in smoke. |

---

## 7. Definition of Done

- [ ] Domain pricing helper + 6 RED→GREEN domain pricing tests
- [ ] 4 RED→GREEN handler Stripe-wiring tests + 1 architect-required parity test
- [ ] Validator gate removed + compatibility tests reverted to target state
- [ ] Frontend short-circuit removed + RTL test updated
- [ ] All deploys (backend + UI) `conclusion=success`
- [ ] End-to-end Stripe smoke: B1, B2, B4 paid RSVPs each create Stripe sessions with correct amounts; test-card payment completes; webhook flips status; confirmation email lands
- [ ] **Architect-required (edit #5)**: Stripe Dashboard / API verification asserts the EXACT integer cents value (e.g. 3700 not 3699/3701) for each smoke. Currency-rounding bugs hide here.
- [ ] **Architect-required (edit #4)**: refund handler test against paid B registration green.
- [ ] Free B-mode regression: register against `16eeb15c-…` (free HeadCountByAge) → still works HTTP 204
- [ ] Mode A regression: register against `c0cd6cfd-…` (paid Mode A) → still works
- [ ] Container logs scanned post-deploy: zero unexpected exceptions
- [ ] PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN updated
- [ ] 7E.3c gate-removal breadcrumb in `MASTER_TODO_PHASE_7E_FLEXIBLE_REGISTRATION.md` updated to reflect 7E.3b completion
- [ ] `MASTER_TODO_PHASE_7E_PAID_BMODE_GATE.md` final note: gate lifted in 7E.3b commit X
