# Phase 8YB.6 — TBD-as-Regular Event Refinement

**Date:** 2026-05-09
**Status:** API-VERIFIED on staging — awaiting operator browser UAT to flip to SHIPPED.
**User-locked product rule clarification.** Single small slice; 5 files; 4 smoke cells; 5-cell operator UAT.

**Commits:** `b74ce227` (initial slice, 6 files, +154 / -62) → `78adfc70` (hotfix: third enforcement site `RegisterWithAttendees`, 2 files).
**Deploys:** BE runs `25611460146` + `25611967990` GREEN. UI run `25611460142` GREEN.
**API smoke:** **4 / 4 PASS** on staging via `scripts/phase8YB6_smoke.py` (C23 Free TBD RSVP → 204; C24 OnPlatformPaid TBD RSVP + Stripe → 200; C25 ExternalPaid TBD GET surfaces vendor+instructions; C25b Niroshana repro `541876b8` still intact).
**Tests:** 19/19 Event_TbdDates_Tests PASS — flipped Phase 8YA.1 Q2=A test, added 2 new tests covering all 3 register paths (Register, RegisterAnonymous, RegisterWithAttendees).
**HS-8YB.6 audit lesson:** PF audit missed `RegisterWithAttendees` as a third enforcement site; smoke matrix surfaced the hole immediately. Future audits must grep for the entire failure-message string, not just the two known method names.

---

## Product rule clarification (user-locked 2026-05-09)

> "No need to mention coming soon. If registration enable, we should allow them to register. This is a external paid event. don't you remember what we should display here? Even though it is a date or venue TBD event treat it as a regular event."

**New rule: TBD events are treated as regular events. Only the date label says "Date TBD". Registration / payment / external CTA all behave normally.**

This overturns:
- **Phase 8YA.1 Q2=A** — `Register()` blocked on TBD events (DROP)
- **Phase 8YB.5 D7=A** — public detail "Coming soon" CTA (DROP)
- **Phase 8YB.5 D5=A** — listing card "Coming Soon" pill (DROP)

Decisions (user = "Go" with all A):
| # | Question | Decision |
|---|---|---|
| DP1 | Email-on-Publish for TBD events | **A** — keep skip (Phase 8YA.2 logic stays). Cleaner UX than substituting "Date TBD" in templates |
| DP2 | WhatsApp-on-Publish for TBD events | **A** — keep skip. Twilio template requires `{{EventDate}}` |
| DP3 | Manage-page status label | **A** — simplify `"Planning (Date TBD)"` → `"Planning"`. Reduce visual noise |

---

## Discipline

1. Master TODO before code (this file).
2. Both deploy workflows GREEN before "DEPLOYED".
3. Per-file `git add` only.
4. API smoke matrix BEFORE flipping status.
5. Operator UAT gate BEFORE "Shipped".
6. TDD: flip the failing-test assertion FIRST (Phase 8YA.1 test was asserting block; now must assert success).

---

## Sites

| # | File | Today (post 8YB.5) | Fix |
|---|---|---|---|
| 1 | `src/LankaConnect.Domain/Events/Event.cs:339-350` (`Register()`) | Returns Failure when `!StartDate.HasValue` | Drop the null-StartDate check |
| 2 | `src/LankaConnect.Domain/Events/Event.cs:377-396` (`RegisterAnonymous()`) | Returns Failure when `!StartDate.HasValue` | Drop the null-StartDate check |
| 3 | `tests/LankaConnect.Domain.Tests/Events/Event_TbdDates_Tests.cs:174-187` | `Register_OnPublishedTbdEvent_Fails` asserts failure | Flip to assert Success |
| 4 | `web/src/app/events/[id]/page.tsx` (Phase 8YB.5 TBD CTA gate) | Generic "Coming soon" gate fires before ExternalPaid CTA | Drop the gate. ExternalPaid TBD → ExternalRegistrationCta. Free / OnPlatformPaid TBD → RsvpFormSection (now allowed by domain) |
| 5 | `web/src/app/events/page.tsx` (Phase 8YB.5 Coming Soon pill) | Orange pill on listing card | Drop the pill. "Date TBD" text remains as factual indicator |
| 6 | `web/src/app/events/[id]/manage/page.tsx:96` | `[EventStatus.Planning]: 'Planning (Date TBD)'` | Simplify to `'Planning'` |

---

## TDD plan

- [ ] T1 (RED) — flip `Register_OnPublishedTbdEvent_Fails` to `Register_OnPublishedTbdEvent_Succeeds`
- [ ] T2 (RED) — add `RegisterAnonymous_OnPublishedTbdEvent_Succeeds`
- [ ] T3 (RED) — also need to verify `HasSchedulingConflict_OnTbdEvent_ReturnsFailure` still passes (regression guard — that's about scheduling math, not registration)
- [ ] T4 (RED) — add `Event_TbdDates_Tests.RegistrationCalculation_OnTbdEvent_Works` if domain calc paths still work for TBD

Implementation makes all of the above GREEN.

---

## What stays unchanged

- Phase 8YB.5 D1=A (Publish button on Planning) ✓
- Phase 8YB.5 D2=B (TS EventStatus string conversion) ✓
- Phase 8YB.5 D5b=A (backend filter — Upcoming includes TBD) ✓
- Phase 8YB.5 D6=A (Postpone requires HasValue) ✓ — still semantically correct
- Phase 8YB.5 E16 (Unpublish reverts to Planning when StartDate null) ✓ — invariant preservation
- Phase 8YA.2 email/WhatsApp Publish skip when StartDate null (DP1=A + DP2=A) ✓
- iCal 422 ✓
- Reminder cron skip TBD ✓ (no time window to compute)
- Activate cron skip TBD ✓ (no date to transition on)

---

## API smoke (3 new cells C23–C25)

| # | Cell | Expected |
|---|---|---|
| C23 | RSVP on TBD-Published Free event | **200 + RSVP confirmed** (was 400 in 8YA.1) |
| C24 | RSVP on TBD-Published OnPlatformPaid event | **200 / payment URL returned** (Stripe checkout starts) |
| C25 | GET TBD-Published ExternalPaid public detail | DOM contains "Registration handled by XYZ" + instructions text (validates ExternalRegistrationCta renders, not "Coming soon" placeholder) |

Carry forward 17/17 from 8YB.5 — total target 20/20 PASS.

---

## Operator UAT (5 cells)

- [ ] U1 — Open `541876b8` public detail page → renders ExternalRegistrationCta (vendor "XYZ" + "Connect with XYZ for more info" instructions). NO "Coming soon" card
- [ ] U2 — Open the listing page → event has "Date TBD" text but NO orange "Coming Soon" pill
- [ ] U3 — Manage page status badge reads "Planning" (NOT "Planning (Date TBD)")
- [ ] U4 — Create a TBD Free event → Publish → as a different user open the detail page → click Register → 200 + multi-attendee form submits successfully
- [ ] U5 — Create a TBD OnPlatformPaid event → Publish → as a different user start RSVP → Stripe checkout opens (paid TBD registration works end-to-end)

---

## Status reporting phases

| Phase | State |
|---|---|
| Implementation done locally | `LOCAL-VERIFIED` |
| `deploy-staging.yml` GREEN | `BACKEND-DEPLOYED` |
| `deploy-ui-staging.yml` GREEN | `UI-DEPLOYED` |
| API smoke 20/20 PASS | `API-VERIFIED` |
| Operator UAT 5/5 PASS | `STAGING-VERIFIED` |
| Doc updates + commit pushed | `SHIPPED` |
