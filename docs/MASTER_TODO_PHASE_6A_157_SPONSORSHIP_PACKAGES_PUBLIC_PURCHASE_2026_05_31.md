# Master TODO — Phase 6A.157: Sponsorship Packages Public Purchase Flow

**Date opened**: 2026-05-31
**Branch**: `feat/phase-6a-157-sponsorship-public-purchase` (off 6A.156 tip `6733e5f5`; will rebase onto `main` once 6A.156 PR merges)
**Depends on**: 6A.156 (foundation, staging-deployed + operator-verified through fix-1/2/3)
**Status**: 🚧 In Progress (locked plan, RCA + 3 architect passes done, implementation starting)
**Architect**: 4 RCA passes — initial scope (locked 16 decisions) → delta after user pivot #1 (no auto-ticket-issuance) → delta after user pivot #2 (cancel 6A.158/6A.159, package-tickets = pure indication) → 4th-pass validation against actual codebase catching 3 corrections + 1 bonus

## 4th-pass validation corrections (locked 2026-05-31)

The 4th architect pass read the actual codebase files line-by-line and caught:

1. **Commit boundary fix** — Original commit 2 (application handler) called `IStripePaymentService.CreatePackageSponsorCheckoutSessionAsync` which was scheduled to land in original commit 3 (Stripe service). Build would fail between commits. **Resolution**: merge old commits 2 + 3 into a single `[2/6] app + infra` commit. **New total: 6 commits + docs** (was 7).
2. **Email needs a REAL EF migration** — Templates are stored as **database rows** (via `EmailTemplateRepository`), NOT disk files / embedded resources. The exploration agent's earlier claim was inaccurate. New email template `template-package-sponsor-confirmation` requires a migration to seed the row, identical to how `Phase6A137B_AddReceiptEmailTemplates.cs` seeded the existing templates. **The "no EF migration" claim is WRONG**. New migration in commit `[3/6] email`.
3. **Snapshot field count** — It's **6 nullable fields**, not 7 (`SponsorshipPackageId`, `RegistrationId`, `PackageNameSnapshot`, `PackageTierSnapshot`, `PackagePriceSnapshot`, `IncludedTicketCountSnapshot`). Re-counted from `Sponsor.cs:130-171`. References throughout this doc corrected.
4. **Bonus catch — `CompleteAsOrganizerCash()`** (`Sponsor.cs:501`) also raises `SponsorPaymentCompletedEvent` (line 517) — same mutual-guard treatment as `CompletePayment` needed. Test added in commit 1.
5. **Bonus catch — `payment_type` literal MUST be byte-identical** across `StripePaymentService` impl + `PaymentsController` dispatcher case + any tests. Don't repeat the existing `addon_purchase` vs `add_on_purchase` footgun (a pre-existing inconsistency where the caller sends one string and the dispatcher routes on another — silently harmless because the impl overrides the caller metadata, but a footgun for cargo-cult copying). Lock literal: `"package_sponsor"`.

---

## Classification

**Feature-missing.** Schema and aggregate primitives already in place from 6A.156 (snapshots on `Sponsor`, atomic stock methods on `SponsorshipPackage`, `EnablePackages` flag, organiser CRUD endpoints). The buyer-facing vertical slice was scope-locked at 6A.156 design time as the second of 5 reserved slots. Operator's "go ahead" on 2026-05-30 + 2026-05-31 simplifications are the green light to start that slot — not a new scope discovery.

Not UI (no display bug), not Auth (anonymous-allowed per existing sponsor pattern), not Backend-API-broken (endpoints don't exist yet, that's by design), not DB-broken (schema already in place from 6A.156).

---

## What changed across the 3 architect passes (audit trail)

### Pass 1 — Initial scope (locked 16 decisions)
- Full public purchase flow + auto-ticket-issuance forward-compat to 6A.158
- Buyer copy: "Includes 3 event tickets — your tickets will be issued shortly and emailed to you separately."
- 6A.158 stays as "ticket bundling auto-issuance" phase
- 6 commits

### Pass 2 — User pivot #1 (2026-05-30)
> "Issuing tickets with sponsorship payment is too complicated, so lets not issue tickets with sponsorship package payment. If any package included tickets, just give a friendly warning message when trying to buy ticket and sponsorships together."
- Auto-ticket-issuance dropped
- Modal warning UX added (Option α — modal-only, dismissal via `localStorage` keyed `eventId:packageId`)
- 6A.158 re-scoped to "organizer manual ticket issuance"
- Still 6 commits

### Pass 3 — User pivot #2 (2026-05-31, FINAL)
> "Free Tickets for sponsors is just an indication. No need to handle issuing ticket by the system at all. Organizer will separately take care of that and let them in at the gate. System should mention about the free tickets during the checkout as well as in the sponsorship confirmation email which details are included in the package."
- 6A.158 **CANCELLED entirely** — organizer handles tickets purely off-platform at the gate
- 6A.159 **CANCELLED entirely** — same "keep ticket purchase and sponsorship separate" principle
- 6A.160 **stays reserved** — sponsor wall tier grouping is pure display polish
- Modal: gray info note (NOT yellow warning) — coordination-warning tone removed
- Email: FORK approach (Option β) — new `PackageSponsorConfirmationEmailParams` + new handler + new template `template-package-sponsor-confirmation` (existing money-sponsor handler unchanged)
- 7 commits + docs (email fork is a new commit)

---

## Locked architectural decisions (final, ~16)

| # | Decision | Rationale |
|---|---|---|
| 1 | `[AllowAnonymous]` GET active packages + POST purchase | Parity with add-on + money-sponsor; `Sponsor.SponsorUserId` already nullable |
| 2 | ONE package per checkout (no multi-package cart) | `Sponsor` aggregate 1:1 with `SponsorshipPackageId` by design; atomic stock trivially correct |
| 3 | Free packages (Price=0) → instant complete, no Stripe | Mirrors `PurchaseAddOnCommandHandler.cs:150-182` byte-for-byte; stock still atomically reserved |
| 4 | NEW `IStripePaymentService.CreatePackageSponsorCheckoutSessionAsync` + DTOs | Separate method; metadata `payment_type: "package_sponsor"` (distinct from existing `"sponsor"` for generic money-sponsor) |
| 5 | NEW dedicated `PackageSponsorCompletedEvent` | Separate event makes the email handler subscribe cleanly without null-checks; future tier-grouping phase (6A.160) can subscribe too |
| 6 | Sponsor sibling method `CompletePackagePayment(intentId)` + mutual guards on BOTH `CompletePayment` AND `CompleteAsOrganizerCash` | Both existing methods raise `SponsorPaymentCompletedEvent` (the wrong event for package sponsors); guards prevent silent misuse. Wrong-method calls return `Result.Failure(...)` — never throw |
| 7 | EF migration: **YES — ONE** in commit `[3/6]` seeding `template-package-sponsor-confirmation` DB row | 4th-pass correction: templates are DB rows, not files. The migration mirrors `Phase6A137B_AddReceiptEmailTemplates.cs` exactly. Domain/aggregate schema needs no migration (6A.156 already covers it) — only the email template row insert |
| 8 | ExternalPaid events → 400 rejected | Stripe-vs-off-platform reconciliation nightmare; matches event payment-mode contract |
| 9 | Backend gate: `event.SponsorConfig.EnablePackages == true` enforced in handler AND `/active` GET returns empty list when off | No awkward error UX for events that haven't opted in |
| 10 | Concurrency: existing `SponsorshipPackageRepository.TryReserveStockAsync` (raw-SQL atomic UPDATE) | Lifted from AddOn in 6A.156; no pessimistic locks needed; idempotent restore via `GREATEST(0, ...)` |
| 11 | Buyer UX: click package card → modal with package summary + buyer form + image upload + Stripe-or-free CTA | Matches organiser-side `SponsorshipPackageEditModal` pattern from 6A.156 fix-3; isolates buyer flow from surrounding form state; mobile-friendly |
| 12 | Webhook dispatcher: BOTH `HandleCheckoutCompleted` AND `HandleCheckoutExpired` get `payment_type: "package_sponsor"` cases | Expired-session stock restoration is easy to miss; tested in commit 3 |
| 13 | Modal copy: gray info note (NOT yellow warning) when `IncludedTicketCount > 0` | Coordination-warning tone removed per user pivot #2; just informational |
| 14 | Email approach: FORK (Option β) — new `PackageSponsorConfirmationEmailParams` + new template + new handler | Package email content materially different from money-sponsor (perks list, tier badge, included-tickets line, different voice); forcing one template to branch on `PackageId.HasValue` would rot |
| 15 | Buyer logo upload reuses existing pattern | Same flow as `SponsorSection.tsx:138-145` Money branch (create Sponsor first, then `POST /sponsors/{id}/image` before Stripe redirect, non-fatal on failure) |
| 16 | Snapshot drift mitigation: all sponsor-facing displays read from snapshot fields, never live `SponsorshipPackage` values | If organizer edits package details after sponsors purchase, snapshots remain immutable (correct); domain test asserts this |

---

## Locked buyer-visible copy (final)

**Package card (compressed, when `IncludedTicketCount > 0`):**
> Includes N tickets — admission arranged by organizer

**Purchase modal (info note, gray, when `IncludedTicketCount > 0`):**
> Includes N adult ticket(s) to the event. The organizer will arrange admission separately — no ticket will be issued through the platform.

**Stripe line-item description (appended when `IncludedTicketCount > 0`):**
> (N ticket(s) included — issued by organizer)

**Success page (after payment, when `IncludedTicketCount > 0`):**
> The organizer will contact you about your N included ticket(s).

**Email (PackageSponsorConfirmationEmail, when `IncludedTicketCount > 0`):**
> Your package includes N adult ticket(s). The event organizer will coordinate your admission directly — please watch for a follow-up message from them.

When `IncludedTicketCount == 0`, all "tickets" copy is omitted (no empty placeholders).

---

## Out of Scope / Operator Responsibility

Documented here so it's not re-litigated later:

- **Ticket admission at the event venue** is 100% the organizer's responsibility. The system records `IncludedTicketCountSnapshot` on the Sponsor row and includes it in the buyer-visible copy + confirmation email — but the platform DOES NOT create `Registration` rows, `Ticket` entities, QR codes, or any admission artefact for package sponsors.
- **No "tickets owed" badge** on the Sponsors management tab (would have been 6A.158 scope before cancellation).
- **No refund policy enforcement** if the organizer fails to admit a sponsor at the gate. The confirmation email is the contract; dispute resolution is between sponsor and organizer.
- **No audit trail of admission**. Off-platform admission is invisible to the system.

These are intentional simplifications per user direction 2026-05-31.

---

## Scope — IN 6A.157

### Backend (Domain + Application + Infrastructure + API)
- New domain method `Sponsor.CreatePackageSponsor(eventId, package, buyerName/email/phone/org/notes, userId?)` — writes all 6 snapshot fields atomically; sets `Type=Money`, `Status=Pending` (or `Completed` for free packages), `Amount=package.Price`
- New domain method `Sponsor.CompletePackagePayment(paymentIntentId)` — sibling to existing `CompletePayment`; guards: errors if `SponsorshipPackageId == null`; raises `PackageSponsorCompletedEvent` (NOT generic `SponsorPaymentCompletedEvent`)
- Mutual guard on existing `Sponsor.CompletePayment` — errors if `SponsorshipPackageId.HasValue` (force callers to use the right method)
- New domain event `PackageSponsorCompletedEvent` (sibling to `SponsorPaymentCompletedEvent`) — carries `SponsorshipPackageId`, snapshots, buyer info, amount, currency
- New application command `CreatePackageSponsorCommand` + handler — 11-step flow mirroring `PurchaseAddOnCommandHandler.cs:50-297` (validate event/sponsor-config/packages-enabled/not-ExternalPaid → atomic stock reserve → snapshot → create entity → free vs Stripe branch → save+commit → restore stock on failure)
- New application query `GetActiveSponsorshipPackagesQuery` + handler — returns `IReadOnlyList<SponsorshipPackagePublicDto>` filtered to `IsActive=true AND (QuantityLimit IS NULL OR QuantitySold < QuantityLimit)`, sorted by `SortOrder`
- New public DTO `SponsorshipPackagePublicDto` — display-relevant fields only (no `QuantitySold`, no `QuantityLimit`, no `ImageBlobName`, no audit timestamps, no `IsActive` — server-filtered)
- New `IStripePaymentService.CreatePackageSponsorCheckoutSessionAsync` method + request/result DTOs — metadata `payment_type: "package_sponsor"`, line-item description includes package name + tier + (conditional) ticket-count appendix
- New `PackageSponsorWebhookHandler` — mirrors `SponsorWebhookHandler` shape (HandleCheckoutCompletedAsync / HandleCheckoutExpiredAsync / HandlePaymentFailedAsync); routes to `Sponsor.CompletePackagePayment` on success, `Sponsor.MarkAsAbandoned` + stock restore on expiry, `Sponsor.MarkAsFailed` on failure
- Webhook dispatcher in `PaymentsController.cs` extended: BOTH `HandleCheckoutSessionCompletedAsync` AND `HandleCheckoutSessionExpiredAsync` add `case "package_sponsor"` routing to the new handler
- New email params `PackageSponsorConfirmationEmailParams` (fields: SponsorName, EventTitle, PackageNameSnapshot, PackageTierSnapshot, AmountPaid, Currency, PaymentDate, PerksHtml pre-rendered `<ul>`, IncludedTicketCountText conditional)
- New email handler `PackageSponsorCompletedEventHandler` subscribing to `PackageSponsorCompletedEvent` — calls existing `ITypedEmailService.SendEmailAsync(params, cts)`; existing money-sponsor handler unchanged
- New email template file `template-package-sponsor-confirmation` in same folder as `template-sponsor-confirmation`
- New API endpoint `[AllowAnonymous] GET /api/events/{eventId}/sponsorship-packages/active`
- New API endpoint `[AllowAnonymous] POST /api/events/{eventId}/sponsorship-packages/{packageId}/purchase` — body `{ buyerName, buyerEmail, buyerPhone?, buyerOrganization?, buyerNotes?, successUrl, cancelUrl }`, response `{ checkoutUrl, sponsorId }` (mirrors 6A.145 widening pattern so FE can attach image to Pending sponsor before Stripe redirect)

### Frontend (hooks + components + layout integration)
- New types in `events.types.ts`: `SponsorshipPackagePublicDto`, `PurchasePackageSponsorRequest`, `PurchasePackageSponsorResponse`
- 2 new methods in `events.repository.ts`: `getActivePackages(eventId)`, `purchasePackage(eventId, packageId, request)`
- New hooks in `useSponsorshipPackages.ts`: `usePublicSponsorshipPackages(eventId, enabled)`, `usePurchasePackageSponsor()`
- New component `PurchaseSponsorshipPackageModal.tsx` — package summary (tier badge, name, price, included-tickets line, perks bullet list, optional image) + buyer form (name/email/phone/org/notes/logo upload) + gray info note when `IncludedTicketCount > 0` + Stripe/free CTA + ESC/click-outside close + portal'd to `document.body` (consistent with 6A.156-fix-2 modal pattern)
- New presentational component `SponsorshipPackageCard.tsx` — public buyer-facing card variant (different from organiser-side `SponsorshipPackageCard` used in editor); shows tier badge, name, price, image, included-tickets compressed copy, perks count, "Select this package" CTA
- Modify `SponsorSection.tsx` — insert package grid + divider above mode toggle when `sponsorConfig.enablePackages === true AND activePackages.length > 0`; hide divider when packages section is hidden; existing custom-amount/item form unchanged

---

## Scope — OUT 6A.157

- **Ticket issuance.** Cancelled entirely per user pivot #2 (was 6A.158).
- **RSVP-bundled package selection.** Cancelled per user pivot #2 (was 6A.159).
- **Tier wall grouping / polish.** Stays reserved as 6A.160 — future phase.
- **Organizer "Issue Tickets" UI / "tickets owed" badge.** Cancelled (was 6A.158 re-scope before user said "no system handling at all").
- **Refund/dispute resolution UX.** Existing sponsor refund flow handles money-back; package-specific refund UX (e.g., perk-claim revocation) is out of scope v1.
- **Sponsor self-edit of package selection.** Existing self-edit endpoint from 6A.151 stays — package snapshot fields are read-only on edit (architect-locked snapshot immutability).
- **EF migration.** None needed; 6A.156 schema covers it.

---

## Commit sequence (6 + docs, REVISED after 4th architect pass)

Each commit: TDD-first (red tests → minimal impl → green), zero compilation errors before next commit, `dotnet build && dotnet test` passes before pushing.

| # | Commit | Files (approx) | Tests (target) |
|---|---|---|---|
| 1 | `feat(events 6A.157) [1/6]: domain Sponsor.CreatePackageSponsor + CompletePackagePayment + CompleteAsOrganizerCash guard + PackageSponsorCompletedEvent` | Sponsor.cs (+factory + sibling complete method + mutual guards on BOTH CompletePayment AND CompleteAsOrganizerCash) / PackageSponsorCompletedEvent.cs (new) / SponsorTests.cs (+cases) | ~14 new SponsorTests: 6 snapshot fields populated; free $0 immediately Completed; distinct Money instances for Amount + PackagePriceSnapshot (EF owned-type trap); guards prevent generic/package method misuse on BOTH paths; CompletePackagePayment raises new event not old; CompleteAsOrganizerCash rejected for package sponsors; state guards |
| 2 | `feat(events 6A.157) [2/6]: app + infra — command + handler + query + Stripe service method + webhook handler + dispatcher` | CreatePackageSponsorCommand.cs + Handler.cs / GetActiveSponsorshipPackagesQuery.cs + Handler.cs / SponsorshipPackagePublicDto.cs / IStripePaymentService.cs (+method) / StripePaymentService.cs (+impl with `payment_type: "package_sponsor"` literal) / 2 new DTOs (request + result) / PackageSponsorWebhookHandler.cs (new, mirrors AddOnPurchaseWebhookHandler) / PaymentsController.cs (+2 switch cases: completed + expired) / DI registration | ~26 tests: 14 handler tests (event not found, not published, sponsors disabled, packages disabled, ExternalPaid rejected, package not found, package inactive, sold-out → Failure, free-package instant complete, paid-package Stripe call, snapshots populated, restore on Stripe fail, restore on save fail, idempotent re-call); 4 query tests; 8 webhook tests (completed→CompletePackagePayment, expired→MarkAsAbandoned+stock restore, failed→MarkAsFailed, idempotent, metadata routing, literal `"package_sponsor"` byte-match in 3 places) |
| 3 | `feat(events 6A.157) [3/6]: email — migration seeds template-package-sponsor-confirmation + params + handler` | NEW EF migration `Phase6A157_AddPackageSponsorEmailTemplate` (mirrors `Phase6A137B_AddReceiptEmailTemplates`) seeding 1 template row into `email_templates` table / PackageSponsorConfirmationEmailParams.cs (in Shared/Email/Contracts) / PackageSponsorCompletedEventHandler.cs (subscribes to PackageSponsorCompletedEvent, calls ITypedEmailService) / template HTML content (in migration as embedded string) | 6 email tests: params constructor sets all fields, ToDictionary maps correctly, handler subscribes to right event, handler calls SendEmailAsync with right params, IncludedTicketCount > 0 includes tickets copy, IncludedTicketCount == 0 omits tickets copy |
| 4 | `feat(events 6A.157) [4/6]: API — SponsorshipPackagesController public endpoints` | SponsorshipPackagesController.cs (+`/active` GET, +`/{packageId}/purchase` POST) / CreatePackageSponsorRequest.cs / response DTO | 6 controller tests: anonymous GET works, anonymous POST works, 404 on bad eventId, 404 on bad packageId, 409 on sold-out, success returns `{checkoutUrl, sponsorId}` |
| 5 | `feat(events 6A.157) [5/6]: FE hooks + types + PurchaseSponsorshipPackageModal` | events.types.ts (+3 types) / events.repository.ts (+2 methods) / useSponsorshipPackages.ts (+2 hooks: usePublicSponsorshipPackages, usePurchasePackageSponsor) / PurchaseSponsorshipPackageModal.tsx (new, portal'd) / SponsorshipPackageCard.tsx public variant (new) | 18 Jest/RTL tests on the modal: renders package details, validates required fields, calls hook with right payload, disables CTA on sold-out, free-package CTA copy, Stripe-package CTA copy, image upload flow, error states, gray info note appears when IncludedTicketCount > 0, gray info note omitted when 0, accessibility — ESC closes, focus trap, ARIA labels |
| 6 | `feat(events 6A.157) [6/6]: SponsorSection integration + cards above custom form` | SponsorSection.tsx (insert package grid + divider above mode toggle) / SponsorshipPackageCard public component finalization | 9 SponsorSection.test.tsx cases: shows packages when enabled + present, hides section when EnablePackages off, hides section when empty array, divider only when packages present, click card opens modal, custom-amount form still works, mode toggle still works, "Your Sponsorships" still renders, package modal close doesn't reset custom-amount form state |

Then `docs(6A.157): tracking docs for Sponsorship Packages public purchase` updating PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN + TASK_SYNCHRONIZATION_STRATEGY + flipping master index row 128 to `✅ STAGING-DEPLOYED` (after both backend + UI deploys verify) and eventually `✅ Shipped` (after operator UAT signs off).

**Total new tests target: ~79** across all layers. Coverage target 90%+ per CLAUDE.md Section 2.

---

## Deploy plan

After final code commit:
1. Push branch
2. Trigger BOTH `deploy-staging.yml` (backend) AND `deploy-ui-staging.yml` (UI) in the SAME chain per CLAUDE.md memory `feedback_deploy_backend_and_ui_together` (backend-only ships leave the feature half-broken)
3. Monitor both deploys to SUCCESS
4. Post-deploy API smoke per CLAUDE.md memory `feedback_post_deploy_api_test`:
   - `POST /api/Auth/login` with the staging creds (password: `1qaz!QAZ` per memory `reference_staging_creds`)
   - `GET /api/events/{eventId}/sponsorship-packages/active` → expect 200 + array of active packages
   - `POST /api/events/{eventId}/sponsorship-packages/{packageId}/purchase` with valid body → expect 200 + `{ checkoutUrl, sponsorId }` OR a 409 if sold-out
   - For free-package: verify response includes successUrl (not Stripe URL)
5. Operator browser UAT — visit public event detail page → see package cards above custom-amount form → click a package → modal opens → fill form → Stripe redirect → simulate Stripe webhook → confirm email received with package details + (conditional) included-tickets line

Status flips: `🚧 In Progress` → `✅ STAGING-DEPLOYED` (after staging API smoke 6/6 GREEN) → `✅ Shipped` (after operator browser UAT signs off, per CLAUDE.md memory `feedback_word_shipped`).

---

## Risks (top 5)

1. **Stripe `payment_type` collision.** `"sponsor"` already taken by generic money-sponsor. Tests in commit 3 assert literal `"package_sponsor"` value. Code review must catch any drift.
2. **Sponsor aggregate growing two near-identical complete methods.** `CompletePayment` (existing, generic money) vs `CompletePackagePayment` (new). Mutual guards (commit 1) prevent silent misuse — wrong method returns Failure, not throws.
3. **Snapshot drift on package edits post-purchase.** If organizer edits `IncludedTicketCount` or `Tier` after sponsors have purchased, all sponsor-facing displays MUST read from snapshot fields, never live `SponsorshipPackage` values. Domain test in commit 1 asserts snapshot immutability post-payment. UI in commit 7 reads from Sponsor.PackageTierSnapshot etc., never re-fetches the live package.
4. **Off-platform admission has zero audit trail.** Buyer disputes "I never got my N included tickets" — platform has no record beyond the confirmation email. Mitigated by stating organizer responsibility explicitly in copy ("organizer will arrange admission separately"). Documented under "Out of Scope / Operator Responsibility" above so this isn't relitigated post-launch.
5. **Free-package double-tap creates 2 Completed sponsors.** Unlike paid (where Stripe session uniqueness deduplicates), free packages complete inline. Atomic `TryReserveStockAsync` serialises stock reservation — only first succeeds; second returns sold-out if `QuantityLimit=1` reached, otherwise claims a second slot. Accepted v1; documented; buyer can self-edit/contact organiser. Test in commit 2 covers the race.

---

## Phase number reservation audit trail

Per CLAUDE.md memory `feedback_phase_number_check.md` — 4-source check before reserving:
1. ✅ Master index — row 128 was `⏳ Reserved (slot 2 of 5)` on 2026-05-30; flipping to `🚧 In Progress (2026-05-31)` in same commit as this Master TODO doc
2. ✅ Git log — no commits on the branch yet; this is commit 1 (Master TODO + index update)
3. ✅ Branches — `feat/phase-6a-157-sponsorship-public-purchase` newly created on this turn (no sibling agent owns it)
4. ✅ Master TODO docs — no prior `MASTER_TODO_PHASE_6A_157*` doc exists; THIS doc is the first

**Cancellations recorded in same commit:**
- Row 129 (6A.158): strike-through with `~~...~~ CANCELLED 2026-05-31 — see 6A.157 final scope; organizer handles ticket admission off-platform`
- Row 130 (6A.159): strike-through with `~~...~~ CANCELLED 2026-05-31 — see 6A.157 final scope; ticket-purchase and sponsorship stay separate transactions`
- Row 131 (6A.160): unchanged, stays reserved

---

## Open questions — NONE

Architect locked all decisions across 3 RCA passes. User locked 2 scope pivots. Operator-verified 6A.156 foundation. Ready to implement.

---

**Implementation start**: 2026-05-31 (this turn, after this commit reserving the slot).
