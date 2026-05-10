# Phase 8X.12 — Combined Recovery Slice (D1 + D2 + D3)

**Date:** 2026-05-09
**Status:** ✅ SHIPPED + STAGING-VERIFIED 2026-05-09 — Niroshana's screenshot of `541876b8` confirmed the ExternalRegistrationCta renders with vendor + instructions on the public detail page, and the user-locked "See external site or reach out organizer for pricing" copy fires correctly when no on-platform pricing is set.
**Architect-approved.** Single recovery slice covering three defects from real browser UAT after Phase 8X.11.

**Commit:** `bdfdc149` (`develop`) — 9 files, +462 / -55.
**Deploys:** BE run `25607095872` GREEN. UI run `25607095876` GREEN.
**API smoke:** **13 / 13 PASS** on staging via `scripts/phase8x12_smoke.py` (8 carry-forward + 4 new D3 cells + allowed-modes Q1).

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

- [x] PF.1 — Confirmed 5 D3 enforcement sites via grep
- [x] PF.2 — Confirmed 5 RsvpFormSection mount sites in `page.tsx`
- [x] PF.3 — Confirmed 0 Phase 8X.11 markers in CreateForm vs 4 in EditForm
- [x] PF.4 — HS.5 audit clear (under threshold)
- [x] PF.5 — Branch base verified clean

---

## Implementation Order

D3 first (smallest, lowest blast radius), then D2 (refactor), then D1 (port).

### D3 — Drop `Pricing is required for ExternalPaid` rule (5 sites) — COMPLETE

- [x] D3.1 — `Event.cs:2680` — removed pricing-required guard from `SetExternalPayment`; signature changed to `TicketPricing? pricing`; `ApplyPricingForExternalPayment` skipped on null; explicit null clears legacy pricing.
- [x] D3.2 — `CreateEventCommandHandler.cs:391` — removed null-pricing failure block; passes null through.
- [x] D3.3 — `UpdateEventCommandHandler.cs:421` — removed `if (pricing == null && @event.Pricing == null)` failure; dropped `pricing ?? @event.Pricing!` fallback (caller-as-source-of-truth).
- [x] D3.4 — `event.schemas.ts:381` — refine scoped to `paymentMode !== ExternalPaid`.
- [x] D3.5 — `event.schemas.ts:913` — same scope.
- [x] D3.6 — `SetExternalPayment` signature: `TicketPricing? pricing` confirmed.
- [x] D3.7 — 3 new unit tests added: null-pricing succeeds; null-pricing clears stale legacy pricing; both-null returns friendly empty state. **8 / 8 PASS.**
- [x] D3.8 — `ApplyPricingForExternalPayment` audited; only invoked when caller-side null check passes.

### D2 — Single registration-section gate in `page.tsx` — COMPLETE

- [x] D2.1 — Added `priorRegistrationNotice?: string` prop to `ExternalRegistrationCta`; renders amber notice card when set.
- [x] D2.2 — CTA pricing copy adapter: when `ticketPriceAmount` null/0 + no advanced pricing, renders `"See external site or reach out organizer for pricing"`.
- [x] D2.3 — Inserted single gate inside the registration section's ternary chain (after `isCancelled`): `: isExternalPaid && !isUserRegistered ? <ExternalRegistrationCta event={event} />`. Replaces the 5 mount-site gating problem with one gate.
- [x] D2.4 — Existing inner gate at the cancelled-registrationDetails branch left in place (defense-in-depth; structurally unreachable).
- [x] D2.5 — `isUserRegistered` (line 243) and `isCancelled` (line 739) computed before the registration-section render.

### D1 — Port Phase 8X.11 surgery from EditForm to CreateForm — COMPLETE

- [x] D1.1 — Read EditForm Phase 8X.11 surgery sites (3-way radio 1260-1302, External card 1304-1370, monetisation gate 2087-2216, isFree mirror, registrationMode coercion).
- [x] D1.2 — Located equivalent CreateForm insertion points (free-event toggle 1035-1046, monetisation cluster 1635-1742).
- [x] D1.3 — Ported 3-way payment-mode radio Controller block.
- [x] D1.4 — Ported External Registration card (URL / instructions / vendor) with optional copy.
- [x] D1.5 — Wrapped donations/collections/sponsors/add-ons cluster + appended ExternalPaid info banner.
- [x] D1.6 — `setValue('isFree', mode === Free, ...)` mirror on payment-mode change.
- [x] D1.7 — `setValue('registrationMode', External, ...)` when ExternalPaid; DetailedAttendees otherwise.
- [x] D1.8 — Zod schema accepts `paymentMode` (already present); `paymentMode + externalRegistration{Url,Instructions,VendorName}` added to create payload.

---

## API Smoke (S.1–S.12) — 13 / 13 PASS on staging

Run via `scripts/phase8x12_smoke.py` against `lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`.

| # | Cell | Result |
|---|---|---|
| S.1 | C1 — ExternalPaid + URL only → 201 | **PASS** — regMode=External |
| S.2 | C2 — ExternalPaid + instructions only → 201 | **PASS** — url null, instructions persist |
| S.3 | C3 — ExternalPaid + all-three-empty → 201 | **PASS** — friendly empty state |
| S.4 | C4 — ExternalPaid + regMode=NoRegistration → 400 | **PASS** — strict Q1 |
| S.5 | C5 — ExternalPaid + regMode=External (explicit) → 201 | **PASS** |
| S.6 | C6 — Free + regMode=External → 400 | **PASS** — External requires ExternalPaid |
| S.7 | C7 — OnPlatformPaid + regMode=External → 400 | **PASS** — mode mismatch |
| S.8 | C8 — ExternalPaid + donationsEnabled → 400 | **PASS** — Q5=B blocker |
| S.9 | **D3.A** — ExternalPaid + null pricing → 201 | **PASS** — D3 acceptance |
| S.10 | **D3.B** — ExternalPaid GET pricing null + regMode=External | **PASS** — price=None, payMode=ExternalPaid |
| S.11 | **D3.C** — ExternalPaid + price=25 → 201 | **PASS** — regression covered |
| S.12 | **D3.D** — Update existing ExternalPaid (null pricing) → 200 | **PASS** — pricing stays null |
| Q1 | Allowed-modes endpoint paymentMode=ExternalPaid | **PASS** — `["External"]` |

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
