# Master TODO — Phase 7F sub-feature C: Tier × age matrix pricing on Mode B

**Status**: 📋 ARCHITECT-APPROVED WITH EDITS (review iteration 1, 2026-04-30; 11 edits applied). No code changes yet — ready to begin Slice 7F-C.0.
**Ship order**: **First** of {7F-C, 7F-B, 7F-D}. Architect rationale: smallest blast radius (no Stripe / aggregate / template-HTML change), lifts a real pricing-fidelity gap, establishes the per-tier-per-age axis that 7F-B and 7F-D both consume.
**Classification**: Feature missing — *not* a regression. Mode A already supports tier × age matrix today via [`TicketTier.CalculatePriceForAttendee(AgeCategory)`](../src/LankaConnect.Domain/Events/Entities/TicketTier.cs#L230) — verified during scoping. Mode B's Phase 7E.3c implementation deliberately collapses to `tier.AdultPrice` for *all* attendees regardless of age — see the explicit parity comment in [`Event.RegistrationMode.cs:436-440`](../src/LankaConnect.Domain/Events/Event.RegistrationMode.cs#L436-L440). 7F-C lifts that collapse.
**Layers touched**: Domain (HeadCountBreakdown axis extension + pricing) → Application (validator + DTO) → Frontend (per-tier-per-age selector) → Email (extended TierBreakdownLine) → Persistence (jsonb shape only — column unchanged).

---

## 1. Why this exists

### 1.1 Pricing fidelity gap
A B-mode organiser with tiered pricing today gets billed `tier.AdultPrice × count` for *every* attendee in that tier, even children. Mode A organisers with the same event get the cheaper `tier.ChildPrice` per child. This is asymmetric and surfaces on the user's bill.

### 1.2 What 7E.3c shipped + why
7E.3c shipped Mode B + tier-counts pricing as `count-per-tier` (one axis). The architect-required parity test in `Phase7E3cTierCountsPricingTests` *deliberately* asserts that Mode B's bill matches Mode A's bill **only when all attendees are adults** — not because that's the correct long-term behaviour, but because it kept the slice scope-disciplined. The breadcrumb in `CalculateTierCountsPrice` (`Event.RegistrationMode.cs:407-456`) explicitly points at 7F as the place to lift the AdultPrice-only choice.

### 1.3 What 7F-C lifts
Add a `(tierId, ageCategory)` axis to `HeadCountBreakdown.TierCounts` so a B2 / B4 mode event can express "VIP × (2 adults, 1 child) + General × (5 adults, 0 children)". Pricing then routes through the same `tier.CalculatePriceForAttendee(ageCategory)` Mode A uses — no fork.

---

## 2. Domain shape change

### 2.1 New axis on `HeadCountBreakdown.TierCounts`

Today's `TierCount` (from [`TierCount.cs`](../src/LankaConnect.Domain/Events/ValueObjects/TierCount.cs)):
```csharp
public sealed class TierCount : ValueObject
{
    public Guid TierId; public string TierName; public int Count;
}
```

Proposed extension:
```csharp
public sealed class TierCount : ValueObject
{
    public Guid TierId; public string TierName;
    public int Count;                      // existing — total in tier
    public int? AdultCount;                // NEW — null means "no age split on this tier"
    public int? ChildCount;                // NEW — null means "no age split on this tier"
    public bool HasAgeSplit => AdultCount.HasValue;  // derived for call-site readability
}
```

**Invariants enforced in factory** (architect edits #1, #2):
1. **Both `AdultCount` and `ChildCount` must be set, or both null.** Half-set (e.g. `AdultCount=2, ChildCount=null`) is rejected — eliminates ambiguity between "no age split" and "zero of that category."
2. **When the tier has `HasChildPricing == false`** (i.e. `tier.ChildPrice == null`), `ChildCount` must be 0 or null. Prevents the silent `CalculatePriceForAttendee(Child)` → `AdultPrice` coalesce that would otherwise undercut the user's bill.
3. **When `HasAgeSplit == true`**: `AdultCount + ChildCount == Count`.
4. **B1 / B3 modes** (no age axis) MUST emit `AdultCount = ChildCount = null`. This is a guard on the entry shape, not just a UI default.

### 2.2 Cross-axis invariants on `HeadCountBreakdown`

Today's invariants stay (`sum(TierCounts.Count) == Total`; `sum(Demographics leaves) == Total`).

7F-C adds (architect edit #3):
- **B2 + tiered with age splits**: `sum(TierCounts.AdultCount) == Demographics.Adults` AND `sum(TierCounts.ChildCount) == Demographics.Children`.
- **B4 + tiered with age splits**: `sum(TierCounts.AdultCount) == Demographics.AdultMales + Demographics.AdultFemales` AND `sum(TierCounts.ChildCount) == Demographics.ChildMales + Demographics.ChildFemales`. (Per architect-clarification: Demographics is 4-leaf in B4; there is no `Demographics.Adults` to compare against.)

**Tier × gender axis is NOT added** (architect edit #4). Only tier × age. Gender continues to live exclusively on `Demographics`.

Architect Q1 default = **strict**: cross-axis must agree. The demographic line drives email rendering (`HeadCountBreakdownLine = "3 adults · 2 children"`); an inconsistent tier breakdown looks wrong in the email.

### 2.3 Pricing change in `Event.CalculateTierCountsPrice` — single-shape refactor (architect edit #5)

Old branched approach (rejected — still a fork *inside* the same method):
```csharp
if (tc.AdultCount.HasValue) { /* age-split path */ }
else { /* AdultPrice-only path */ }
```

Architect-approved single-shape rewrite — derive once, multiply twice:
```csharp
foreach (var tc in tierCounts) {
    var tier = ResolveTier(tc.TierId);
    var adultCount = tc.AdultCount ?? tc.Count;   // legacy/B1/B3 → all "adults" for pricing purposes
    var childCount = tc.ChildCount ?? 0;          // null path = no children counted for pricing

    var adultLine = tier.AdultPrice.Multiply(adultCount);
    var childLine = tier.CalculatePriceForAttendee(AgeCategory.Child).Multiply(childCount);
    var lineTotal = adultLine.Add(childLine);
    // accumulate into total
}
```

This shape:
- Has **one branch** (the `??` coalesce on the inputs), not two pricing paths.
- Calls `tier.CalculatePriceForAttendee(AgeCategory.Child)` even when childCount is 0; `Money.Multiply(0)` returns `Money.Zero(currency)` so the bill is unchanged. *Required test case*: legacy B1+tiered payload still calculates the same `AdultPrice × Count` total.
- Routes through the same helper Mode A uses — single source of truth.

Compatibility-table row update (Phase 7E plan §2): "tier × age matrix → A only" → "A or B2/B4 with per-tier-by-age counts".

### 2.4 What does NOT change

- `TicketTier` shape — `AdultPrice` + `ChildPrice` already exist.
- `events.event_registrations.head_count` jsonb column — same column, new optional fields inside the array elements. **Migration NOT required**: jsonb additive fields hit the existing deserialiser; legacy rows materialise with `AdultCount = ChildCount = null`.
- Mode A's pricing path — already correct.
- The `Phase7E3cTierCountsPricingTests` parity test — kept as-is for the AdultPrice-only path; new tests cover the per-age path. Per architect Q7 default: null-axis stays a valid choice indefinitely.

### 2.5 EF `ValueComparer` deep-copy update (architect edit #6, #7)

The existing `ValueComparer` for `HeadCountBreakdown` in [`RegistrationConfiguration.cs`](../src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs) was hand-written for `TierCount`'s 3 fields. Adding two nullable ints means:
- `Equality` lambda must compare `AdultCount` + `ChildCount`.
- `Snapshot` lambda must deep-copy them.
- `HashCode` must include them.
- `TierCount.GetEqualityComponents()` override in the value object must yield them.

Round-trip mutation test (mirrors Phase 6A.129 trap): load a registration, mutate `TierCounts[0].AdultCount` only, `SaveChanges`, re-load, assert change persisted. Catches the reference-snapshot trap.

---

## 3. Slice plan (5 slices)

| Slice | Focus | Tests | Deploy |
|---|---|---|---|
| **7F-C.0** | Architect-approved domain shape + decisions captured. (This doc.) | — | — |
| **7F-C.1** | Domain — extend `TierCount` per §2.1 + new factory invariants in `HeadCountBreakdown` per §2.2 + single-shape pricing refactor in `Event.CalculateTierCountsPrice` per §2.3. | TDD ≥18 cases (architect-revised floor): tier × age matrix B2 (3 adults VIP × $50 + 2 children VIP × $25 = 200), B4, mixed-tier (VIP×(2A,1C) + General×(5A) = 285), B1+tiers (legacy path — `AdultPrice × Count`), B3+tiers (legacy path), invariant violations (half-set AdultCount, tier-age sum disagrees with demographic-age, B1 with non-null AdultCount, tier with `HasChildPricing=false` × ChildCount > 0, ChildCount = 0 with `HasChildPricing=false` allowed), legacy payload deserialisation (no AdultCount field), `Money.Multiply(0)` on child line returns Zero. 90%+ coverage. | — |
| **7F-C.1b** | Persistence — update `RegistrationConfiguration.cs` `ValueComparer` lambdas + `TierCount.GetEqualityComponents()` per §2.5 (architect edit #7). | Round-trip mutation test (load → mutate `AdultCount` only → save → re-load → assert persisted). 6A.129-style. | `deploy-staging.yml` (no schema migration; just config change). |
| **7F-C.2** | Application — `TierCountDto` gains optional `AdultCount` / `ChildCount`; FluentValidation rule for the dual axis (rejecting half-set, rejecting `ChildCount > 0` on tier without `ChildPrice`); validator tests `[Theory]`-driven mirroring §2.1+§2.2 invariants. `IRegistrationCheckoutService` reuses the same `Event.CalculateHeadCountPrice` (no new code). | Validator tests; handler tests. 90%+ coverage. | `deploy-staging.yml` |
| **7F-C.3** | Frontend — per-tier-per-age selector in [`HeadCountRsvpForm`](../web/src/presentation/components/features/events/HeadCountRsvpForm.tsx). UI rules (architect Q2 + Q6 calls): age-unaware default + opt-in toggle per tier; **toggle hidden when the tier has no `ChildPrice`** with helper text *"This tier doesn't have child pricing — children are billed at adult price."* When toggle is on for B2/B4: two spinners (Adults / Children) per tier. Live "tier-age sum mirror" against `Demographics.Adults` / `Demographics.Children`. Submit-time validation rejects divergence (architect Q1 strict). Per-tier subtotal preview per architect Q3. | RTL: tier-age form B2 + B4; sum-mirror invariant; submit blocked + inline error when divergence; legacy B1+tiered + B3+tiered paths unchanged; `tier.HasChildPricing == false` hides the toggle; **architect edit #10** "edit Demographics.Adults so tier-age sum disagrees → submit disabled with inline error". | `deploy-ui-staging.yml` |
| **7F-C.4** | Email — extended `TierBreakdownLine` (e.g. `"VIP: 2 adults · 1 child · General: 5 adults"`) computed by [`HeadCountEmailFormatter`](../src/LankaConnect.Application/Events/Emails/HeadCountEmailFormatter.cs). Mode-aware copy retained (no template HTML change — formatter substitution-only). **Architect edit #11**: when `AdultCount.HasValue == false`, line stays in legacy format `"VIP × 3"` — no `adults` / `children` words. | Formatter unit tests: B2+tiered+age-split → `"VIP: 2 adults · 1 child"`; B4+tiered+age-split → same shape; B1+tiered (no age split) → legacy `"VIP × 3"`; B3+tiered → legacy. | `deploy-staging.yml` (formatter ships in backend; no template-HTML change). |
| **7F-C.5** | Staging end-to-end smoke — create paid B2 + tiered event with VIP `Adult=$50, Child=$25` + General `Adult=$30`. RSVP `VIP×(2A,1C) + General×(5A)`. Expected `totalPrice = 2×50 + 1×25 + 5×30 = 175`. Verify Stripe Checkout returns 17500 cents. **Mode A parity test on staging**: same event, same basket, register Mode A → identical bill. | — | — |

**Tracking-doc updates** after every slice per CLAUDE.md §7.

---

## 4. Risks & guards

| Risk | Mitigation |
|---|---|
| Cross-axis invariant (§2.2) over-constrains the UX | Architect Q1 = strict; clear inline error in 7F-C.3; sum-mirror updates as user types so divergence is visible immediately. |
| Tier with no `ChildPrice` × `ChildCount > 0` — silent under-charge via fallback to AdultPrice (architect edit #8) | Domain factory invariant §2.1 #2 rejects with explicit error; UI toggle is hidden for those tiers; helper text explains. |
| Legacy B-mode-with-tiers registrations (already in DB from 7E.3c) deserialise without the new fields | `TierCount.AdultCount` / `ChildCount` are nullable; deserialisation hits the existing path; `CalculateTierCountsPrice` keeps the legacy `Count`-only behaviour because `adultCount = tc.AdultCount ?? tc.Count`. Migration NOT required — jsonb column shape allows additive fields. |
| Mode A bill ≠ Mode B bill for identical baskets after 7F-C | Architect-required parity test on staging (slice 7F-C.5). Single-shape refactor in §2.3 routes both modes through the same `tier.CalculatePriceForAttendee` helper. |
| Pricing math drift across A and B's two pricing helpers | `CalculateTierCountsPrice` continues to use `tier.CalculatePriceForAttendee(ageCategory)` — same helper Mode A uses. No fork. |
| `Money.Multiply(0)` behaviour on the child line — must return zero in same currency, not fail | Verified by test in slice 7F-C.1; `Money` already handles this correctly. |
| Stripe checkout line-item description loses age detail | Out of scope; flagged as a known reporting limitation. Stripe shows the rolled-up amount with one description; the email's `TierBreakdownLine` is the canonical breakdown. |
| Frontend tier-age form race — user edits Demographics, then tier-age | RTL test (slice 7F-C.3) covers the divergence-blocks-submit case. |
| `RegistrationConfiguration.ValueComparer` reference-snapshot trap | Slice 7F-C.1b explicitly updates the comparer + adds the round-trip mutation test (memory 6A.129). |

---

## 5. Out of scope

- **Tier × gender matrix** — would require adding a `MaleCount` / `FemaleCount` axis to `TierCount`. Architect call: NOT added (§2.2). Defer.
- **Tier × (age, gender) matrix** — i.e. mode B4's full 4-leaf cross applied per tier. Same answer.
- **Stripe line-item per tier-age leaf** — today the Stripe checkout has one line item with the rolled-up amount. 7F-C keeps that — no per-leaf line items, just an accurate total.
- **Refund recompute when tier prices change post-registration** — out of scope (and forbidden by event-publish lockdown today).
- **Backfilling pre-7F-C jsonb rows** — architect Q5 = null-default forever. Additive jsonb is cheap; back-filling is busywork.

---

## 6. Architect questions — answered

| # | Question | Architect call |
|---|---|---|
| Q1 | Cross-axis invariant — strict or derived? | **Strict** (§2.2). Inconsistent demographic vs tier-age numbers in emails is the worst outcome. |
| Q2 | UI default for new tier rows — age-aware or age-unaware? | **Age-unaware default + opt-in toggle.** Mirrors most events; B1/B3 stay simple; toggle disambiguates. |
| Q3 | Pricing transparency in the RSVP form — show running per-tier-per-age subtotal? | **Yes.** Show line items on the post-submit confirmation page too. |
| Q4 | `HeadCountEmailFormatter.TierBreakdownLine` per-age vs rolled-up? | **Show per-age** (`"VIP: 2 adults · 1 child"`) when `HasAgeSplit == true`. Legacy format `"VIP × 3"` when null. |
| Q5 | Pre-7F-C jsonb rows — back-fill or null-default forever? | **Null-default forever.** |
| Q6 (architect-added) | Form behaviour on a tier with `HasChildPricing == false` when user toggles "Add per-age split"? | **Hide the toggle entirely**; show helper note "this tier doesn't have child pricing — children are billed at adult price." |
| Q7 (architect-added) | Should `Phase7E3cTierCountsPricingTests` parity test stay green (null-axis valid) or hard-cutover (age axis required for B2/B4 + tiered)? | **Stay green.** Null-axis is a valid organiser choice (doesn't care about the split). |

---

## 7. Pre-conditions

| # | Item | Status |
|---|---|---|
| 1 | `TicketTier.CalculatePriceForAttendee(ageCategory)` already routes via `ChildPrice` for `AgeCategory.Child` | ✅ verified at [TicketTier.cs:230](../src/LankaConnect.Domain/Events/Entities/TicketTier.cs#L230) |
| 2 | EF jsonb `ValueComparer` deep-copy snapshot for `HeadCountBreakdown.TierCounts` exists | ✅ already in place per Phase 7E.1 — slice 7F-C.1b extends it |
| 3 | `JsonStringEnumConverter` configured globally (Program.cs) | ✅ already configured |
| 4 | Architect Q1 + Q2 + Q4 + Q6 + Q7 ratified | ✅ ratified by review iteration 1 (this doc) |
| 5 | Mode A's `CalculateTieredPriceForAttendees` confirmed using `tier.CalculatePriceForAttendee(attendee.AgeCategory)` for parity claim | ✅ verified at [Event.TicketTiers.cs:198](../src/LankaConnect.Domain/Events/Event.TicketTiers.cs#L198) |
