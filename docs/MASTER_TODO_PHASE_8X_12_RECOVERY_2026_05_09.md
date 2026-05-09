# Phase 8X.12 — Combined Recovery Slice (D1 + D2 + D3)

**Date:** 2026-05-09
**Status:** IN PROGRESS
**Architect-approved.** Single recovery slice covering three defects from real browser UAT after Phase 8X.11.

---

## Decisions (User-Locked 2026-05-09)

| # | Question | Decision |
|---|---|---|
| 1 | D2 — early-return CTA gate | **B** — only when `!isUserRegistered && !isCancelled` |
| 2 | D2 — search-card "External" badge | **B** — defer to later cosmetic slice |
| 3 | D3 — public page pricing summary when null | **Custom**: copy = `"See external site or reach out organizer for pricing"` |

---

## Discipline Rules (Carried Forward from 8X.11)

1. Master TODO before code.
2. Both `deploy-staging.yml` AND `deploy-ui-staging.yml` must be GREEN before "DEPLOYED".
3. Per-file `git add` only — never whole-file staging (8X.11 EventHeroImage lesson).
4. API smoke matrix BEFORE flipping status (`feedback_cross_surface_matrix_smoke.md`).
5. Operator UAT gate BEFORE "Shipped" (`feedback_operator_uat_gate.md`).
6. No author-laundering — never `git add` parallel author's untracked files.
7. Honest end-of-turn — plain English, no commit-hash dumps.

---

## Defects

### D1 — `/events/create` shows OLD UX
- **Surface:** `web/src/presentation/components/features/events/EventCreationForm.tsx`
- **Root cause:** Phase 8X.11 form surgery never landed in CreateForm. EditForm has 4 Phase 8X.11 markers; CreateForm has 0.
- **Symptoms:** No 3-way payment radio. "External Registration" card greyed out. No way to create ExternalPaid event from `/events/create`.

### D2 — `/events/{id}` renders attendee form for ExternalPaid events
- **Surface:** `web/src/app/events/[id]/page.tsx`
- **Root cause:** Only 1 of 5 RsvpFormSection mount sites is gated on `isExternalPaid` (line 1149). Other 4 mounts (1608, 1858, 1876, 1896) are ungated.
- **Symptoms:** UAT event `6d202a73` (ExternalPaid) renders attendee form via standard fallback path.

### D3 — Cannot create ExternalPaid event without ticket price
- **Surfaces:** Domain (`Event.cs:2680`) + Application (`CreateEventCommandHandler.cs:391`, `UpdateEventCommandHandler.cs:421`) + Frontend (`event.schemas.ts:390, 922`)
- **Root cause:** Architect's earlier rule "External requires pricing for display" is overturned. Organizers may publish ExternalPaid events with no on-platform price.
- **Symptoms:** Create form rejects with `"Price and currency are required for paid events"`; payout summary shows `$NaN`.

---

## HS.5 Audit (Pre-Flight Hard-Stop)

Architect-mandated audit: grep for `HasPaidPricingConfigured`, `TicketPrice.Amount`, `Pricing.`, `!IsFreeEvent`. **>3 structural sites assuming `!IsFreeEvent → has pricing` = stop and re-architect.**

**Findings:**
- `Event.cs:1265` — inside `CalculatePriceForAttendees`. **SAFE** — only fires on on-platform registration calc paths; ExternalPaid never reaches (RegistrationMode = External, no on-platform regs).
- `Event.RegistrationMode.cs:777` — inside `CalculatePriceForHeadCount`. **SAFE** — same reason.
- `Event.cs:2766` — guards `OnPlatformPaid` transition only. **SAFE** — explicit `mode == OnPlatformPaid` check.
- `EventMappingProfile.cs:77, 169` — nullable mappings already. **SAFE**.

**Verdict:** 0 additional structural sites. Below 3-site threshold. **Proceed.**

---

## Pre-Flight Diagnostics (PF.1–PF.5)

- [x] PF.1 — Confirm 5 D3 enforcement sites via grep
- [x] PF.2 — Confirm 5 RsvpFormSection mount sites in `page.tsx`
- [x] PF.3 — Confirm 0 Phase 8X.11 markers in CreateForm vs 4 in EditForm
- [x] PF.4 — HS.5 audit (above) — under threshold
- [ ] PF.5 — `git pull origin develop` to ensure clean base

---

## Implementation Order

D3 first (smallest, lowest blast radius), then D2 (refactor), then D1 (port).

### D3 — Drop `Pricing is required for ExternalPaid` rule (5 sites)

- [ ] D3.1 — `Event.cs:2680` — remove `if (pricing == null) return Result.Failure(...)` from `SetExternalPayment`
- [ ] D3.2 — `CreateEventCommandHandler.cs:391` — remove `if (pricing == null) return Result.Failure("Pricing is required for ExternalPaid events")` (keep handler logic for null pricing — pass `null` through to `SetExternalPayment`)
- [ ] D3.3 — `UpdateEventCommandHandler.cs:421` — same as above
- [ ] D3.4 — `event.schemas.ts:381` — scope refine to `paymentMode !== ExternalPaid`
- [ ] D3.5 — `event.schemas.ts:913` — same as above
- [ ] D3.6 — Verify `SetExternalPayment` accepts `TicketPricing? pricing` (nullable) — if not, change signature
- [ ] D3.7 — Add unit test: `Event.SetExternalPayment(null, null)` returns Success
- [ ] D3.8 — Audit `ApplyPricingForExternalPayment` for null safety

### D2 — Early-return CTA refactor in `page.tsx`

- [ ] D2.1 — Add `priorRegistrationNotice?: string` prop to `ExternalRegistrationCta`
- [ ] D2.2 — Adapt `ExternalRegistrationCta` pricing copy: when `event.pricing == null`, render `"See external site or reach out organizer for pricing"`
- [ ] D2.3 — In `page.tsx`, immediately after computing `isExternalPaid`, `isUserRegistered`, `isCancelled`, add early-return:
  ```tsx
  if (isExternalPaid && !isUserRegistered && !isCancelled) {
    return <ExternalRegistrationCta event={event} priorRegistrationNotice={...} />;
  }
  ```
- [ ] D2.4 — Verify the gate at line 1149 becomes redundant; remove it (kept clean)
- [ ] D2.5 — Verify `isUserRegistered` and `isCancelled` are computed before the early-return point

### D1 — Port Phase 8X.11 surgery from EditForm to CreateForm

- [ ] D1.1 — Read EventEditForm.tsx Phase 8X.11 surgery sites (3-way radio, External Registration card, monetisation gating, isFree mirror, registrationMode coercion)
- [ ] D1.2 — Read EventCreationForm.tsx structure to find equivalent insertion points
- [ ] D1.3 — Port 3-way payment-mode radio Controller block
- [ ] D1.4 — Port External Registration card (URL / instructions / vendor)
- [ ] D1.5 — Port monetisation cluster gating (hide donations/sponsors/collections/signup-lists when ExternalPaid)
- [ ] D1.6 — Port `setValue('isFree', mode === Free, ...)` mirror on payment-mode change
- [ ] D1.7 — Port `setValue('registrationMode', External, ...)` when ExternalPaid selected
- [ ] D1.8 — Verify Zod schema accepts `paymentMode` field (and after D3, accepts ExternalPaid + null pricing)

---

## API Smoke (S.1–S.12)

After backend deploy-staging GREEN:

| # | Cell | Expected |
|---|---|---|
| S.1 | C1 — ExternalPaid + URL only → 201 | regMode = External |
| S.2 | C2 — ExternalPaid + instructions only → 201 | url null, instructions present |
| S.3 | C3 — ExternalPaid + all-three-empty → 201 | "Contact organiser" path |
| S.4 | C4 — ExternalPaid + regMode=NoRegistration → 400 | strict Q1 |
| S.5 | C5 — ExternalPaid + regMode=External (explicit) → 201 | redundant explicit |
| S.6 | C6 — Free + regMode=External → 400 | External requires ExternalPaid |
| S.7 | C7 — OnPlatformPaid + regMode=External → 400 | mode mismatch |
| S.8 | C8 — ExternalPaid + donationsEnabled → 400 | Q5=B blocker |
| **S.9** | **D3.A — ExternalPaid + null pricing → 201** | **NEW — D3 acceptance** |
| **S.10** | **D3.B — ExternalPaid + null pricing GET — pricing summary null** | **NEW — D3 acceptance** |
| **S.11** | **D3.C — ExternalPaid + price=25 → 201 (regression)** | **NEW — pricing still allowed** |
| **S.12** | **D3.D — Update existing ExternalPaid: clear pricing → 200** | **NEW — D3 update path** |

Re-use `scripts/phase8x11_smoke.py` as base; add 4 new cells.

---

## Operator Browser UAT (U.1–U.15) — User-Driven

**Cannot self-attest. Hand-off to user with explicit checklist.**

### D1 verifications:
- [ ] U.1 — `/events/create` shows 3-way payment radio
- [ ] U.2 — Selecting "Paid — external registration link" enables External Registration card
- [ ] U.3 — Donations / sponsors / collections / signup-lists hidden when ExternalPaid
- [ ] U.4 — Submit ExternalPaid + URL only → 201, redirects to event detail
- [ ] U.5 — Submit ExternalPaid + all-empty → 201 (CTA shows "Contact organiser")

### D2 verifications:
- [ ] U.6 — Open existing ExternalPaid event `6d202a73` → CTA shown, NO attendee form
- [ ] U.7 — Open same event in refund-in-progress state → CTA shown
- [ ] U.8 — Open same event in expired-checkout state → CTA shown
- [ ] U.9 — Open same event in incomplete-payment state → CTA shown
- [ ] U.10 — User who is already registered → still sees their RSVP card (Decision #1 = B)
- [ ] U.11 — Cancelled event → still shows cancel-state UI, not CTA
- [ ] U.12 — Click "Register externally" → opens external URL or shows instructions

### D3 verifications:
- [ ] U.13 — `/events/create` ExternalPaid + leave price blank → submits successfully
- [ ] U.14 — Detail page for ExternalPaid + null pricing → shows `"See external site or reach out organizer for pricing"`
- [ ] U.15 — `/events/{id}/edit` ExternalPaid → can clear price → save succeeds

---

## Status Reporting Phases

| Phase | Reportable State |
|---|---|
| Implementation done locally | `LOCAL-VERIFIED` |
| `deploy-staging.yml` GREEN | `BACKEND-DEPLOYED` |
| `deploy-ui-staging.yml` GREEN | `UI-DEPLOYED` |
| API smoke 12/12 PASS | `API-VERIFIED` |
| Operator browser UAT 15/15 PASS | `STAGING-VERIFIED` |
| Doc updates + commit pushed | `SHIPPED` |

**Never `SHIPPED` on BE-only evidence. Never claim before user signs off on UAT cells.**
