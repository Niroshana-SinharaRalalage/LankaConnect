# Phase 6A.162 — Sponsor Two Images (Logo + Brochure/Flyer)

**Date opened:** 2026-06-01
**Branch:** `feat/phase-6a-162-sponsor-two-images` off `feat/phase-6a-157-sponsorship-public-purchase` (the latest UAT-confirmed tip; depends on 6A.157 merging to main first)
**Status:** 📋 Master TODO ready — implementation starting per user GO 2026-06-01

## Why off the 6A.157 tip and not main

Operator request item #3 says "We should be able to upload two images everywhere we can add/update sponsor image." Picker surfaces today:

1. `EditSponsorModal.tsx` — exists on main + 6A.157 tip
2. `SponsorSection.tsx` (public custom-amount form) — exists on main + 6A.157 tip
3. `PurchaseSponsorshipPackageModal.tsx` — **only on 6A.157 tip**
4. `AddOffPlatformSponsorModal.tsx` — exists on main + 6A.157 tip
5. `SponsorOptionInForm.tsx` — retired by 6A.157-fix-1 [1/3]; out of scope

If we branched off main, surface #3 would be missed and the operator's "everywhere" requirement would have a gap. Branching off the 6A.157 tip ensures the dual-picker shared component reaches all four live surfaces. 6A.157 has operator-UAT-confirmed working ("Perfect. Everything is working." — 2026-06-01); merge to main is pending PR landing.

## Phase number reservation (4-source check per memory `feedback_phase_number_check`)

| Source | 6A.161 | 6A.162 |
|---|---|---|
| `docs/PHASE_6A_MASTER_INDEX.md` | 📋 Master TODO ready (sibling agent: Attendee Ticket Tier) | ✅ free |
| `git log --grep "6A.16[0-9]"` | n/a — no commits yet | ✅ free |
| `git branch -a` | n/a — sibling not yet pushed | ✅ free |
| `find docs -name "MASTER_TODO_PHASE_*"` | `MASTER_TODO_PHASE_6A_161_ATTENDEE_TICKET_TIER_2026_06_01.md` | ✅ free |

**6A.161 is reserved by a sibling agent's in-flight "Attendee Ticket Tier" work — do NOT use.** 6A.162 is the next free slot.

## User request (verbatim, 2026-06-01)

> 1. Currently, I can add only one image to a sponsorship. I should be able to add/update a logo and brochure/flyer both as images.
> 2. Then the logo image should be shown in event details page. If a user click on the logo, it should open up the brochure/flyer in a popup window. If there is no brochure/flyer added, then just display the logo on a popup window.
> 3. We should be able to upload two images everywhere we can add/update sponsor image.

**Open question answered**: GO + Option (A) — whole-card click opens the popup (replaces existing scroll-to-section UX). Applied to BOTH the public "Sponsors (N)" preview strip AND the in-section "Sponsors" wall inside the Sponsor This Event block.

## Classification

**Feature-missing** with downstream impact in:
- **Database** — 2 new nullable columns (`brochure_url`, `brochure_blob_name`)
- **Domain** — new methods `SetBrochure` / `ClearBrochure` (additive siblings to existing `SetImage` / `ClearImage`)
- **Application** — 2 new commands + DTO field extensions + Create*Sponsor command optional brochure-staging fields
- **API** — 2 new sibling endpoints + reflection-whitelist test extension
- **Frontend** — 6 surfaces (4 upload + 2 display) + 1 new popup component

NOT Auth (existing organizer/buyer-self authz model carries over verbatim).
NOT UI-only (the brochure must persist and serve).

## Locked architectural decisions

1. **Domain shape — Option C (keep + sibling).** Existing `ImageUrl`/`ImageBlobName`/`SetImage`/`ClearImage` stay verbatim (semantically: the *logo*). NEW `BrochureUrl`/`BrochureBlobName`/`SetBrochure`/`ClearBrochure` mirror line-for-line. Zero breakage to ~10 existing Sponsor domain tests + ~12 application-layer tests.
2. **REST shape — Option B (sibling endpoints).** Keep `POST/DELETE /api/events/{eventId}/sponsors/{sponsorId}/image` unchanged. NEW `POST/DELETE /api/events/{eventId}/sponsors/{sponsorId}/brochure` mirrors authz + 5MB cap + MIME guards. Anonymous staging endpoint `POST /sponsors/staging-image` extends with optional `?slot=logo|brochure` query (default `logo` for backward-compat).
3. **DB — 2 nullable text columns.** `brochure_url` (varchar 2048), `brochure_blob_name` (varchar 512). Additive, no backfill, single migration `Phase6A162_AddSponsorBrochure`.
4. **DTOs**: `SponsorDto.BrochureUrl` + `BrochureBlobName` (organizer view). `PublicSponsorDto.BrochureUrl` ONLY — `BrochureBlobName` STAYS ABSENT to preserve the 6A.150 PII-free contract; reflection whitelist test in `SponsorsControllerPublicEndpointTests` extends to assert this.
5. **FE picker — adopt shared component everywhere**. Upgrade `SponsorImagePicker.tsx` to optionally render a second picker when an optional `brochure` prop is provided. When absent → today's single-picker behavior unchanged (zero regression for any caller that has not opted in). Adopt in 4 surfaces (EditSponsorModal, SponsorSection, PurchaseSponsorshipPackageModal, AddOffPlatformSponsorModal). The deprecated `SponsorOptionInForm` is OUT OF SCOPE (architect-locked in 6A.157-fix-1 [1/3]).
6. **Popup — NEW `SponsorBrochurePopup.tsx`**. Portal'd to `document.body` per the 6A.156-fix-2 form-nesting contract. Shows the brochure full-size if present; falls back to the logo when no brochure. ESC + backdrop + X all close.
7. **Click target — whole card** (user-locked Option A). Public `SponsorsPreviewStrip` cards + in-section sponsor-wall cards become buttons that open `SponsorBrochurePopup` instead of scrolling to the section. The "Sponsor this event" header link in `SponsorsPreviewStrip` keeps its scroll behavior.
8. **Per-slot 5MB cap.** Each slot independent → max 10MB combined per sponsor. Two separate multipart POSTs (one per slot); a transient failure on one slot does NOT roll back the other.
9. **Best-effort uploads** in create flows. After `mutateAsync` returns `sponsorId`, attach logo + brochure in `Promise.all` wrapped in try/catch; log on failure but continue to Stripe / SuccessUrl (mirrors 6A.157-fix-1 [2/3] decision for the buyer modal).
10. **Mobile layout**: dual pickers stack full-width on mobile, side-by-side at `md:` breakpoint. Total vertical growth ≈ +90px on mobile in the public form. Acceptable.

## 6-commit sequence

| # | Commit | Files (approx) | Tests (target) |
|---|---|---|---|
| 0 | `docs(6A.162) [0/6]: reserve phase 6A.162 + Master TODO + master-index row` | `docs/MASTER_TODO_PHASE_6A_162_SPONSOR_TWO_IMAGES_2026_06_01.md` (new) + `docs/PHASE_6A_MASTER_INDEX.md` (row added) | — |
| 1 | `feat(events 6A.162) [1/6]: domain Sponsor.SetBrochure + ClearBrochure + BrochureUrl/BlobName` | `Sponsor.cs` (+2 props, +2 methods, +XML-doc clarification on existing ImageUrl) / `SponsorTests.cs` (+8 cases) | 8 new tests: SetBrochure happy path / SetBrochure rejects empty URL / SetBrochure rejects empty blobName / SetBrochure replaces existing brochure / ClearBrochure happy path / ClearBrochure idempotent on null / **independence: SetBrochure does NOT touch ImageUrl / ClearImage does NOT touch BrochureUrl** |
| 2 | `feat(events 6A.162) [2/6]: EF migration + SponsorEntityConfiguration brochure columns` | new `Phase6A162_AddSponsorBrochure.cs` migration (2 nullable cols) + `SponsorEntityConfiguration.cs` (extend property bindings) + auto-regen snapshot | Migration round-trip Up/Down verified locally; `dotnet ef migrations script` produces clean SQL |
| 3 | `feat(events 6A.162) [3/6]: application — brochure commands + DTO extensions + create-flow staging fields` | new `SetSponsorBrochureCommand` + `ClearSponsorBrochureCommand` + handlers; extend `SponsorDto`/`PublicSponsorDto`; extend `UploadSponsorStagingImageCommand` (slot param); extend `CreateMoneySponsorCommand`/`CreateItemSponsorCommand`/`CreateOffPlatformSponsorCommand`/`CreatePackageSponsorCommand` with optional `BrochureStagingBlobName`/`BrochureStagingUrl` | ~12 new app tests covering both commands + create-with-brochure happy paths |
| 4 | `feat(events 6A.162) [4/6]: API — /brochure sibling endpoints + staging slot param + whitelist test extension` | `SponsorsController.cs` (+ 2 endpoints) + extend `[HttpPost("staging-image")]`; `SponsorsControllerPublicEndpointTests.cs` (whitelist extension + 6 new contract tests) | 6 new contract tests: brochure endpoints exist / authz mirrors /image / staging-image accepts slot param / `BrochureUrl` IN PublicSponsorDto / `BrochureBlobName` ABSENT from PublicSponsorDto / 5MB cap enforced |
| 5 | `feat(events 6A.162) [5/6]: FE foundation — SponsorImagePicker dual mode + repo + hook + SponsorBrochurePopup` | `SponsorImagePicker.tsx` (extend props + render dual when brochure prop present); `events.repository.ts` (+2 methods + staging slot param); new `useSponsorBrochureUpload` hook (sibling of existing `useUploadSponsorImage`); new `SponsorBrochurePopup.tsx` (portal'd); type extensions on `SponsorDto`/`PublicSponsorDto` | 6 vitest cases: picker dual-mode rendering / brochure-only set / both staged / popup shows brochure when present / popup falls back to logo when no brochure / popup closes on ESC + backdrop |
| 6 | `feat(events 6A.162) [6/6]: FE integration — 4 picker sites + whole-card popup on both sponsor strips` | `EditSponsorModal.tsx` / `SponsorSection.tsx` (replace inline picker + wrap sponsor-wall cards in click-to-popup buttons) / `PurchaseSponsorshipPackageModal.tsx` (replace inline picker) / `AddOffPlatformSponsorModal.tsx` (replace inline picker) / `SponsorsPreviewStrip.tsx` (whole-card click → popup, retire scroll-to-section UX) | 8 new vitest cases across the 4 picker surfaces + 2 popup-click integration tests on both strips |

Then `docs(6A.162): tracking docs` updating PROGRESS_TRACKER + STREAMLINED_ACTION_PLAN + TASK_SYNCHRONIZATION_STRATEGY + flipping master index row.

**Total new tests target: ~46** (8 domain + 12 application + 6 controller + 14 vitest + 6 popup/picker foundation). Coverage target 90%+ per CLAUDE.md §2.

## Deploy plan

Per CLAUDE.md memory `feedback_deploy_backend_and_ui_together`:

1. Push branch
2. Trigger BOTH `deploy-staging.yml` (backend) + `deploy-ui-staging.yml` (UI) in the same chain
3. Monitor both deploys to SUCCESS
4. Per memory `feedback_post_deploy_api_test` — staging API smoke:
   - `POST /api/Auth/login` (creds per memory `reference_staging_creds`: password `1qaz!QAZ`)
   - `POST /api/events/{eventId}/sponsors/{sponsorId}/brochure` with a real image → 200 + URL in response
   - `GET /api/events/{eventId}/sponsors/public` → assert `brochureUrl` populated in JSON, `brochureBlobName` ABSENT
   - `DELETE /api/events/{eventId}/sponsors/{sponsorId}/brochure` → 204 + subsequent GET shows brochureUrl null
5. Operator browser UAT:
   - On a sponsor edit modal: upload both a logo and a brochure
   - On a public event detail page: see the logo in the sponsor strip
   - Click a sponsor card in the top strip → popup opens showing brochure
   - Click a sponsor card in the in-section wall → same popup behavior
   - Click a sponsor WITHOUT a brochure → popup shows logo (fallback)
   - During package purchase: upload both logo and brochure → check sponsor row has both after Stripe completion

Status flips: `🚧 In Progress` → `✅ STAGING-DEPLOYED` (after staging API smoke 4/4 GREEN) → `✅ Shipped` (after operator browser UAT signs off, per memory `feedback_word_shipped`).

## Risks

1. **Backward compat with existing single-image data.** Mitigation: every new column nullable; every new DTO field nullable; display = `brochureUrl ?? imageUrl`. All ~120 existing sponsor rows continue working unchanged.
2. **`PublicSponsorDto` PII contract.** The reflection whitelist test from 6A.150 fails unless we explicitly add `BrochureUrl` to the allowlist. Easy fix; MUST NOT add `BrochureBlobName`.
3. **Mobile dual-picker layout.** Two stacked pickers consume vertical space (≈+90px on mobile). Smoke on 320 / 768 / 1024.
4. **Per-slot 5 MB cap.** Operator could attach 4.9 MB logo + 4.9 MB brochure ≈ 10 MB combined per sponsor. Storage cost trivial; two separate multipart POSTs keep failures independent.
5. **Branch dependency on 6A.157.** This branch chains behind 6A.157 (which is operator-UAT-confirmed but not yet merged to main). If 6A.157 needs rework, 6A.162 rebases.
6. **Whole-card click changes existing UX on `SponsorsPreviewStrip`.** The top strip currently scrolls to the sponsor section on card click; we replace with popup. The "Sponsor this event" header link keeps scroll behavior. User explicitly confirmed.

## Open questions — NONE

User confirmed: GO + Option A (whole-card click) for BOTH strips (top preview + in-section wall).
