# Phase 6A.151 — Sponsor Inline Image + Edit Existing Sponsorship

**Date opened:** 2026-05-18
**Branch:** `feat/phase-6a-148-refund-approval-workflow` (rides the same branch as 6A.148/149/150 per established pattern; surgical staging by path required)
**Status:** 📋 Master TODO ready — awaiting user approval before code changes
**Architect validation:** ✅ APPROVED-WITH-CHANGES (10 holes folded in across two stress-test passes)
**Phase-number 4-source check:** ✅ clean — master index / branches / MASTER_TODO docs / git log all show 148-150 in flight, 151 free

---

## Goal in one sentence

Make sponsorships editable after creation — fix the inline registration-form panel's missing image upload (Issue 1) and add a state-aware PATCH surface so sponsors and organizers can amend amount / notes / organization / item details / image / name on existing sponsors (Issue 2).

---

## Operator's verbatim ask (2026-05-18)

> Next issue:
> 1. At the registration, when I want to be an sponser, there is no way to add an image for the sponsoship.
> 2. Currently, unable to edit sponsorship details by the sponser or event organizer such as edit the description, increase the amount, change or upload an image..etc.

User also explicitly asked: *"Please check whether this is an UI issue, Auth Issue, Backend API issue, a Database issue or a feature missing case."*

---

## Classification (architect-confirmed)

| Issue | Category | Justification |
|---|---|---|
| 1 — inline panel no image | **Feature missing (UI + minor backend protocol gap)** | Standalone `SponsorSection.tsx` already has image upload + `POST /sponsors/{id}/image`. The inline `SponsorOptionInForm.tsx` (built money-only-no-asset per 6A.137E DonationOptionInForm pattern) was never retrofitted in 6A.145. Not Auth, DB, or broken-API — stale scope omission. |
| 2 — no edit | **Feature missing (Domain + Application + API + UI)** | Sponsor aggregate has factories + lifecycle + image mutators but **zero content-update methods**. No `PATCH /sponsors/{id}`. No edit UI. DB shape OK — columns exist, just write-once today. |

---

## Decisions locked-in (architect + product owner)

| # | Decision | Source | Locked |
|---|---|---|---|
| D1 | **Inline image approach** = (a) pre-submit staging blob with **server-generated** correlation GUID, picked up by registration handler during in-transaction Sponsor row create | Architect pass 1 + H1 hardening | ✅ |
| D2 | **Edit surfaces** = both in v1 (organizer modal on `SponsorsManagementTab` + sponsor self-edit on "Your Sponsorships" panel inside `SponsorSection.tsx`) | Architect pass 1 | ✅ |
| D3 | **Amount edits on Stripe-completed sponsors** = hard-block v1; Stripe top-up/refund flow deferred to v2 | Architect pass 1 | ✅ |
| D4 | **Amount edits on off-platform-completed** = organizer-only (revenue impact) | Architect pass 1 | ✅ |
| D5 | **Anonymous-sponsor edits** = out of scope v1; sponsor must be authed and own the row | Architect pass 1 | ✅ |
| D6 | **No event re-emission** on edits (no email/WhatsApp re-fire) | Architect pass 1 | ✅ |
| D7 | **No `xmin` concurrency** v1 (low-frequency edits; revisit if conflicts surface) | Architect pass 1 | ✅ |
| D8 | **Branch** = ride existing `feat/phase-6a-148-refund-approval-workflow` (matches 6A.149/150 pattern); surgical staging by path; rebase forward when 148 ships | H8 revised + 148 branch pattern | ✅ |
| D9 | **Name edits** = included; **email/phone edits** = deferred (Stripe receipt routing + identity-claim semantics) | H7 | ✅ |
| D10 | **Failed/Abandoned/Refunded** = organizer **notes-only** edits; everything else 🚫 | H6 tightened | ✅ |

---

## State-edit matrix (final, post-stress-test)

(✅ both / 👤 organizer-only / 🚫 disallowed; **sponsor-self column requires `Sponsor.UserId != deterministicAnonId && currentUserId == Sponsor.UserId`** per H2)

| State | Name | Notes | Org | Amount | Item Name/Desc/Value | Image |
|---|---|---|---|---|---|---|
| Pending Money | ✅ | ✅ | ✅ | 🚫 (Stripe session stale) | n/a | ✅ |
| Completed Money (Stripe) | ✅ | ✅ | ✅ | 🚫 v1 (Stripe top-up/refund deferred to v2) | n/a | ✅ |
| Completed Money (off-platform, `PaymentIntentId="off-platform"`) | ✅ | ✅ | ✅ | 👤 (revenue impact) | n/a | ✅ |
| RecordedItem | ✅ | ✅ | ✅ | n/a | ✅ | ✅ |
| Failed / Abandoned | 👤 notes-only | 🚫 | 🚫 | 🚫 | 🚫 | 🚫 |
| Refunded | 👤 notes-only | 🚫 | 🚫 | 🚫 | 🚫 | 🚫 |

---

## Architect stress-test findings (pass 2) — all folded into plan

### 🔴 Must-fix-before-C1

| # | Finding | Action |
|---|---|---|
| H1 | Staging-blob endpoint is a free DoS / abuse vector | Staging endpoint stays `[AllowAnonymous]` BUT enforces: (a) max 5 MB, (b) MIME allowlist `image/jpeg\|png\|webp`, (c) per-IP rate limit `10/hour` via existing `AspNetCoreRateLimit`, (d) correlation ID = **server-generated** `Guid.NewGuid()` returned to client (single-use; registration handler `MoveBlobAsync` rejects already-moved), (e) janitor sweep at **6h** not 24h. |
| H2 | Sponsor self-edit on anonymous-deterministic UserId is ambiguous to FE | Add `isEditableBySponsor: bool` to `SponsorDto`, computed server-side as `Sponsor.UserId != deterministicAnonId && currentUserId == Sponsor.UserId`. FE hides the Edit button when false. Backend authz still 403s on tampering. Claim-by-email magic-link deferred to backlog. |

### 🟠 Must-fix-during-implementation

| # | Finding | Action |
|---|---|---|
| H3 | `MinSponsorAmount` post-hoc validation will block legitimate edits | Validate **only when `Amount` field is present in the patch AND new value < current min**. No-change and notes-only edits bypass entirely. |
| H4 | Off-platform amount edit needs audit columns, not just no-op | Add `LastEditedAt: DateTime?`, `LastEditedBy: Guid?` to Sponsor entity. **NEW EF migration C1.5** required — corrects the original "no migration" claim. All three mutators set them. No domain event. |
| H5 | Cached aggregates / event-level sponsor totals will drift on amount edit | In handler post-`SaveChangesAsync`, invalidate `IDistributedCache` keys like `event:{id}:sponsor-totals` if any exist. **Audit task during C2**, finding documented in commit message. |
| H7 | Edit modal should include `Name` | Add `Name` to edit surface (sponsor + organizer). Email/phone deferred. Add `UpdateName(string)` domain mutator in C1. |
| H8 | Branch off `feat/phase-6a-148-refund-approval-workflow`, not main | Confirmed — rides the established 148/149/150 pattern. Avoids three-way merge on `SponsorsController.cs`. |
| H9 | Image endpoints' authz predates sponsor-self-edit | Extend authz on `POST/DELETE /sponsors/{id}/image` to allow `currentUserId == sponsor.UserId` when sponsor non-anonymous. One predicate change; matches PATCH endpoint's authz. Add tests to C3. |

### 🟡 Backlog follow-ups

| # | Finding | Action |
|---|---|---|
| H6 | Failed/Abandoned/Refunded edits half-justified | Tightened matrix — organizer notes-only ✅, everything else 🚫. Refunded image also 🚫 (image was tied to original transaction; re-uploading muddies history). |
| H10 | FE inline-panel upload UX silent in pass 1 | Parallel upload starts on file select; submit button shows "Uploading image…" disabled state if upload not complete; on upload failure, inline retry but allow "submit without image" as acceptable degradation. |
| (separate phase) | Anonymous-sponsor edits via claim-by-email magic link | Out of scope 6A.151. Threat model needed. |
| (separate phase) | Email / phone editability on Sponsor | Out of scope — Stripe receipt routing + identity-claim semantics warrant a dedicated flow. |
| (separate phase) | Stripe top-up / partial-refund for Completed-via-Stripe amount edits | v2 problem. Filed as follow-up. |

---

## Pre-flight (before any code)

- [ ] Reserve phase number **6A.151** in `docs/PHASE_6A_MASTER_INDEX.md` (add row after 6A.150)
- [ ] Confirm branch decision (D8) — stay on `feat/phase-6a-148-refund-approval-workflow`
- [ ] Verify `AspNetCoreRateLimit` middleware is wired in `Program.cs` and has `/api/Auth` rule (architect H1 assumes this; confirm before C4)
- [ ] Confirm deterministic-anonymous-GUID derivation function exists in shared utilities (Phase 6A.140) — needed for H2 server-side computation
- [ ] Run `git log --oneline origin/Production_05_09_2026..HEAD` survey to inventory in-flight 148/149/150 changes touching `SponsorsController.cs` / `Sponsor.cs` / DTOs so my edits don't collide

---

## Commit plan (11 commits, TDD-first)

| # | Title | Files | Tests-first | Acceptance |
|---|---|---|---|---|
| **C1** | Domain mutators + `LastEditedAt`/`LastEditedBy` + `UpdateName` | `Sponsor.cs` + `SponsorTests.cs` | ✅ RED first | All state-matrix cells (28+) tested; `UpdateContactFields`, `UpdateAmount(actorIsOrganizer)`, `UpdateItemDetails`, `UpdateName`; audit fields set on every mutation. |
| **C1.5** | EF migration `Phase6A151_AddSponsorEditAuditColumns` | `Infrastructure/Data/Migrations/` + `SponsorConfiguration.cs` | n/a (schema) | Two nullable columns on `events.sponsors`; `[Migration("…")]` on Designer.cs; Down() drops cleanly; reference_data drift hand-removed per project convention. |
| **C2** | `UpdateSponsorCommand` + handler + authz + cache-audit | `Application/Events/Commands/UpdateSponsor/` + tests | ✅ RED first | Sponsor-self / organizer / stranger authz matrix tested; `MinSponsorAmount` re-validated only when Amount in patch; cache invalidation documented in commit msg (or noted "no cache present"). |
| **C3** | `PATCH .../sponsors/{id}` endpoint + image-endpoint authz extension | `SponsorsController.cs` + integration tests | ✅ RED first | PATCH wired; existing `POST/DELETE /sponsors/{id}/image` authz extended to allow `userId == sponsor.UserId` (non-anon). |
| **C4** | Staging-blob endpoint `POST /sponsors/staging-image` + sweep stub | `SponsorsController.cs` + new janitor service | ✅ RED first | Rate-limited 10/hr per IP; 5MB cap; MIME allowlist `jpeg/png/webp`; server-gen GUID; sweep job (`IHostedService`) stub with 6h schedule + log emit; `MoveBlobAsync` rejects already-moved blob. |
| **C5** | Registration handler wired to consume `sponsorStagingBlob` | `RegisterAttendees…Handler` (or `BundledRegistration…`) + tests | ✅ Test the in-transaction move | When `sponsorStagingBlob != null`, handler calls `Sponsor.SetImage(url, blobName)` inside the same DB tx as ticket purchase. Orphan blob counted on failure. |
| **C6** | FE inline panel image — `SponsorOptionInForm.tsx` | `SponsorOptionInForm.tsx` + parent registration form + types + repo | Vitest if reasonable | Image picker visible; pre-uploads to staging endpoint on file select; submit button shows "Uploading image…" disabled state if upload in progress; upload failure → inline retry + "submit without image" fallback. JSDoc "no image" caveat removed. |
| **C7** | FE organizer Edit modal on `SponsorsManagementTab` | new `EditSponsorModal.tsx` + `SponsorsManagementTab.tsx` row action + repo + hook | Vitest | Row "Edit" action opens modal; modal respects state-matrix per-field disablement; PATCH on submit; success toast + table refresh. |
| **C8** | FE sponsor self-edit modal on "Your Sponsorships" | reuse `EditSponsorModal.tsx` (variant) + `SponsorSection.tsx` "Your Sponsorships" row | Vitest | Edit button rendered only when `isEditableBySponsor=true`; constrained fields per matrix; PATCH on submit. |
| **C9** | Staging deploy via `deploy-staging.yml` + `deploy-ui-staging.yml` + UAT curls | n/a | Manual smoke matrix | (a) curl PATCH on staging API verifying state-matrix enforcement (200 vs 400 vs 403 per cell), (b) inline panel image-via-registration end-to-end test, (c) organizer edit-amount on off-platform end-to-end, (d) sponsor self-edit notes end-to-end, (e) Stripe-completed amount-edit denied. Per memory rule [feedback_post_deploy_api_test.md] — hit every changed endpoint. |
| **C10** | Docs sync — `PROGRESS_TRACKER.md` + `STREAMLINED_ACTION_PLAN.md` + `PHASE_6A_MASTER_INDEX.md` + this `MASTER_TODO` close-out | docs only | n/a | All three PRIMARY trackers updated with one paragraph + status flip; phase row in master index marked 🔧 → ✅ STAGING-DEPLOYED; close-out note added to bottom of this file. |

**Deployment**:
- C1, C1.5, C2, C3, C4, C5 → backend → `deploy-staging.yml`
- C6, C7, C8 → UI → `deploy-ui-staging.yml`
- C9 → both deploys + UAT (verify staging endpoints with token from `niroshhh@gmail.com` / `1qaz!QAZ`)

---

## Observability plan

- **Structured logs in `UpdateSponsorCommandHandler`**: `SponsorId`, `ActorUserId`, `IsOrganizerActor`, `FieldsChanged[]`, `State`. Info per allowed edit; warn per rule-rejection with reason code.
- **Counter metrics** (existing telemetry surface): `sponsor_edits_total{actor=sponsor|organizer, result=ok|denied, reason=<code>}`, `sponsor_staging_uploads_total`, `sponsor_staging_orphans_swept_total`.
- **Try/catch wrappers**: staging blob endpoint + sweep job both wrap external calls (Azure Blob, DB) in try/catch with structured logging.
- **Sweep job log line**: `[SponsorStagingBlobSweeper] swept N orphan blobs older than 6h, oldest age=X`.

---

## Documentation checklist

- [ ] `docs/PHASE_6A_MASTER_INDEX.md` — new 6A.151 row (after 6A.150)
- [ ] `docs/PROGRESS_TRACKER.md` — latest paragraph entry for 6A.151
- [ ] `docs/STREAMLINED_ACTION_PLAN.md` — 6A.151 action item
- [ ] `docs/MASTER_TODO_PHASE_6A_151_SPONSOR_EDIT_2026_05_18.md` — this file
- [ ] Update `SponsorOptionInForm.tsx` JSDoc to remove "no image" caveat after C6
- [ ] XML doc on new `Sponsor.UpdateXxx` mutators citing the state matrix

---

## Risk register

| Risk | Severity | Mitigation | Owner |
|---|---|---|---|
| Stale Stripe session if Pending amount edited | High | Block Pending amount edits v1 | Domain validation |
| Stripe ↔ DB drift on Completed amount edits | High | Hard 🚫 v1 with `InvalidOperationException` carrying actionable message | Domain validation |
| `MinSponsorAmount` from `SponsorConfiguration` JSONB bypassed | Medium | Re-run config validation in `UpdateSponsorCommand` handler before applying — only when Amount in patch | Handler |
| Anonymous-sponsor accidentally seeing Edit button | Low | `isEditableBySponsor` flag on DTO + FE conditional render + backend 403 belt-and-braces | DTO mapping + FE |
| Co-organizer authz regression | Medium | Reuse `Event.IsOrganizer` (Phase 6A.133); never reimplement | Handler |
| Audit/notification re-fire on edit | Low | No event emission; if newsletter wants it later, add `SponsorUpdatedEvent` then — silent v1 | Handler comment |
| Optimistic concurrency on simultaneous edit | Low | Skip `xmin` v1; revisit if conflicts surface | (deferred) |
| Staging blob abuse / orphans | Medium-High | Server-gen GUID + rate limit + MIME allowlist + 5MB cap + 6h sweep | C4 |
| Three-way merge on `SponsorsController.cs` | Medium | Ride 148 branch; surgical staging by path; rebase forward when 148 lands | D8 |
| Edit modal accidentally shows fields for wrong state | Medium | Component reads state from DTO + matrix-driven disabled/hidden logic; component test per state | C7/C8 |
| Cache aggregate drift on amount edit | Low-Medium | C2 audit task; invalidate keys if found | C2 |

---

## Acceptance criteria (release-ready)

- [ ] All 11 commits land on `feat/phase-6a-148-refund-approval-workflow`
- [ ] Backend Application + Domain test suites GREEN
- [ ] Web vitest suite GREEN
- [ ] Typecheck clean (`tsc --noEmit`)
- [ ] Staging API smoke 5/5 cells PASS
- [ ] Staging UI manual UAT 4 surfaces verified (inline panel image, organizer edit modal, sponsor self-edit modal, anonymous sponsor sees NO edit button)
- [ ] `sponsor_edits_total` counter increments visible in staging logs
- [ ] All three PRIMARY tracking docs updated
- [ ] Phase row in master index flipped 🔧 → ✅ STAGING-DEPLOYED

---

## Out of scope (explicit non-goals)

- Stripe top-up / partial-refund flow for Completed-via-Stripe amount changes (filed as v2)
- Cross-event "My Sponsorships" management page (per-event Your Sponsorships panel covers v1)
- Anonymous-sponsor edits via claim-by-email magic link (separate phase; needs threat model)
- Email / phone editability on Sponsor (separate phase; Stripe receipt routing implications)
- `Sponsor.UpdatedEvent` domain event for newsletter / WhatsApp triggers (silent v1)
- Optimistic concurrency via `xmin` (low-frequency edits; revisit if conflicts)
- Audit trail UI surfacing `LastEditedAt` / `LastEditedBy` to attendees (data captured, not surfaced)

---

**Awaiting user green light to begin C1.**
