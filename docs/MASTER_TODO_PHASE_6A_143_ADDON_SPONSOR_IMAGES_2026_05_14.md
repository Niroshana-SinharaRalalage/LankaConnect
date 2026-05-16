# Phase 6A.143 — Add-On & Sponsor Images + Event Details Layout Widening

**Date opened:** 2026-05-14
**Branch:** TBD (suggest `feat/phase-6a-143-addon-sponsor-images` off current feature branch)
**Status:** 📋 Master TODO ready — pending user approval before code changes
**Architect validation:** ✅ Plan agent (see Architect Findings section below)

---

## Goal in one sentence

Let organizers upload an image per add-on and a sponsor-banner image per event, render those images everywhere the items surface (event detail page + manage tabs + buyer registration flow), and widen the event details page so the larger add-on/sponsor sections have room to breathe.

---

## Operator's verbatim ask

> 1. Currently, we cannot add images for add-ons or sponsors. We should be able to add and later change the image for add-on and sponsors.
> 2. Those images should be shown in the particular section in the event details and event manage pages.
> 3. I think we are not utilizing the event details page effectively.
>    - If possible we need to increase the width of event details to show more details.
>    - Areas for Photos and Video should push down and show the event add-ons and sponsors if they are available for the event.

---

## Decisions locked-in (architect-validated, 2026-05-14)

| # | Decision | Locked |
|---|---|---|
| D1 | **Where the sponsor image lives**: `SponsorConfiguration` value object (event-level banner). NOT on individual `Sponsor` purchase records — there is no tier/option entity to attach per-tier images to. | ✅ |
| D2 | **Schema (AddOnDefinition)**: 2 new columns — `image_url` (text, nullable), `image_blob_name` (text, nullable). Single image; no thumbnail pipeline. | ✅ |
| D3 | **Schema (SponsorConfiguration)**: 2 new fields inside the existing JSONB value object — `SponsorImageUrl`, `SponsorImageBlobName`. Snapshot-only migration (no column add). | ✅ |
| D4 | **Endpoints**: 4 new endpoints, multipart `IFormFile`, matching the `EventsController.AddImageToEvent` pattern. Add-on endpoints on `AddOnsController`; sponsor banner endpoints on `EventConfigController` (where `PUT sponsor-config` already lives at line 71). Organizer auth via existing `EventConfigController.VerifyOrganizerAsync` helper. | ✅ |
| D5 | **Image validation**: reuse `IImageService.ValidateImage` (5MB cap; jpeg/png/webp). NOT `AlbumImageService` (its 3-size WebP pipeline is overkill for these). | ✅ |
| D6 | **Old-blob cleanup on replace** (new logic — NOT in event-hero handler today): handler loads existing `imageBlobName` → after successful upload + persist → calls `IAzureBlobStorageService.DeleteFileAsync(oldBlobName)` best-effort (log + swallow). Clear-image endpoint deletes the current blob. | ✅ |
| D7 | **AddOnSelector display**: 64×64 left-side thumbnail per row, fallback to existing `Package` icon when image is null. | ✅ |
| D8 | **SponsorSection display**: full-width banner above the form header. | ✅ |
| D9 | **Manage tabs**: thumbnail column in list view (AddOnsManagementTab, SponsorsManagementTab). | ✅ |
| D10 | **Container width**: `max-w-7xl` (1280px) → `max-w-screen-2xl` (1536px). Predictable Tailwind breakpoint. | ✅ |
| D11 | **Section order**: leave as-is. Add-Ons + Sponsors already render ABOVE Photos/Videos today (verified at `web/src/app/events/[id]/page.tsx:2076-2122`). User's "push photos down" describes current behavior. | ✅ |
| D12 | **Auto-expand sections that have images**: `defaultOpen={true}` on Sponsors + Add-Ons sections only when an image is set, so the new visuals get exposure. | ✅ |
| D13 | **Commit ordering**: 3 commits — A (backend, both entities), B (frontend, both entities), C (layout). Each independently revertable. | ✅ |
| D14 | **Visual upgrade beyond auto-expand?** — **OPEN QUESTION FOR USER** (see Open Questions section). | ⏳ |

---

## Architect findings (Plan agent, 2026-05-14)

### Validated — no must-fix-before-implementation issues

The architect's review surfaced **no 🔴 blockers**. All decisions above incorporate their recommendations. Key takeaways from the review:

| # | Finding | Status |
|---|---|---|
| F1 | Sponsor image must live on `SponsorConfiguration` VO, NOT on `Sponsor` purchase record — no tier/option entity exists today. | ✅ D1 |
| F2 | `SponsorConfiguration` is a flat-JSONB VO with `private constructor` + `Create()` factory — adding fields requires regenerating the VO. Existing rows have null image fields → VO factory must accept nullable defaults. | ✅ D3 + migration spec below |
| F3 | Reuse `IImageService.ValidateImage` (event-hero pipeline), NOT `AlbumImageService` (3-size WebP overkill for these). | ✅ D5 |
| F4 | Old-blob cleanup on replace mirrors `AddImageToEventCommandHandler:62` rollback pattern. | ✅ D6 |
| F5 | AddOn/Sponsor section order **already** has Add-Ons + Sponsors above Photos. User's "push photos down" describes current behavior. | ✅ D11 (no reorder) |
| F6 | Container widening to `max-w-screen-2xl` (1536px) is a standard Tailwind breakpoint, safer than ad-hoc widths. | ✅ D10 |
| F7 | AddOnSelector (buyer) + SponsorSection (buyer) flows include Stripe checkout — image renders are presentational additions only; no state-shape changes. | ✅ Risk callout |

### Should-address-during-implementation

| # | Finding | Action |
|---|---|---|
| F8 | EF migrations need `[Migration("…")]` attribute on `.Designer.cs` (project rule). | Confirm in Phase B |
| F9 | Use container names `event-addons` and `event-sponsors` for blob storage (NOT the default `event-media` shared container) so cleanup queries are isolated. Containers auto-provision on first upload via `AzureBlobStorageService.EnsureContainerExistsAsync` (line 249); no manual `az storage container create` step needed. | Confirm in Phase A |
| F10 | Domain unit tests must pin `SetImage` / `ClearImage` semantics — replacement preserves other fields, clear leaves entity valid. | Phase A test plan |
| F11 | API smoke must include 403 (non-organizer) + 400 (bad content-type / too large) scenarios alongside the happy path. | Smoke Matrix § below |
| F12 | RTL component tests must snapshot AddOnSelector + SponsorSection in both image-present and image-null states. | Phase D test plan |
| F13 | `SponsorConfiguration.GetEqualityComponents()` (line 142) MUST include the two new image fields — otherwise VO equality breaks silently and EF change-tracking misses updates. | Phase A domain test pins this |
| F14 | Sponsor endpoints route to `EventConfigController`, NOT `SponsorsController` — `SponsorsController` is for purchase records; `EventConfigController.cs:71` already owns `PUT sponsor-config`. Use existing `VerifyOrganizerAsync` helper for auth. | D4 / Files-to-touch updated |

---

## Files to touch

### Backend (Commit A)
- `src/LankaConnect.Domain/Events/AddOnDefinition.cs` — add `ImageUrl`, `ImageBlobName` properties + `SetImage(url, blobName)` + `ClearImage()` methods.
- `src/LankaConnect.Domain/Events/ValueObjects/SponsorConfiguration.cs` — add `SponsorImageUrl`, `SponsorImageBlobName` properties; new `Create` overload accepting nullable defaults; `WithImage(url, blobName)` returning a new VO.
- `src/LankaConnect.Infrastructure/Data/Configurations/AddOnDefinitionEntityConfiguration.cs` — add column mappings.
- `src/LankaConnect.Infrastructure/Data/Migrations/20260514_Phase6A143_AddImageToAddOnDefinition.cs` (+ `.Designer.cs` with `[Migration]`).
- `src/LankaConnect.Infrastructure/Data/Migrations/20260514_Phase6A143_AddSponsorImageToSponsorConfig.cs` (snapshot-only regen).
- `src/LankaConnect.Application/Events/Commands/SetAddOnDefinitionImage/SetAddOnDefinitionImageCommand.cs` + Handler + Validator.
- `src/LankaConnect.Application/Events/Commands/ClearAddOnDefinitionImage/...`
- `src/LankaConnect.Application/Events/Commands/SetSponsorConfigImage/...`
- `src/LankaConnect.Application/Events/Commands/ClearSponsorConfigImage/...`
- `src/LankaConnect.API/Controllers/AddOnsController.cs` — add 2 endpoints (`POST/DELETE /api/events/{eventId}/add-ons/{definitionId}/image`).
- `src/LankaConnect.API/Controllers/EventConfigController.cs` — add 2 endpoints (`POST/DELETE /api/events/{eventId}/sponsor-config/image`). Reuse the existing `VerifyOrganizerAsync` helper at line 71-area. ⚠️ NOT `SponsorsController.cs` — that's for purchase records.
- `src/LankaConnect.Application/Events/Common/AddOnDefinitionDto.cs` — surface `ImageUrl` to FE.
- `src/LankaConnect.Application/Common/Mappings/EventMappingProfile.cs` — map new fields on AddOn DTO; sponsor banner via SponsorConfigDto (find or add).

### Frontend (Commit B)
- `web/src/infrastructure/api/types/events.types.ts` — mirror `imageUrl` on AddOnDefinitionDto + SponsorConfigDto.
- `web/src/infrastructure/api/repositories/events.repository.ts` — add `uploadAddOnImage`, `deleteAddOnImage`, `uploadSponsorImage`, `deleteSponsorImage` methods.
- `web/src/presentation/hooks/useAddOns.ts` — add upload mutations.
- `web/src/presentation/hooks/useSponsors.ts` — add upload mutations.
- `web/src/presentation/components/features/events/AddOnDefinitionEditor.tsx` — image upload widget.
- `web/src/presentation/components/features/events/SponsorConfigForm.tsx` — banner upload widget.
- `web/src/presentation/components/features/events/AddOnSelector.tsx` — render 64×64 thumbnail.
- `web/src/presentation/components/features/events/SponsorSection.tsx` — render banner.
- `web/src/presentation/components/features/events/AddOnsManagementTab.tsx` — thumbnail column.
- `web/src/presentation/components/features/events/SponsorsManagementTab.tsx` — thumbnail column.

### Layout (Commit C)
- `web/src/app/events/[id]/page.tsx` — bump `max-w-7xl` → `max-w-screen-2xl`; pass `defaultOpen={!!image}` on Sponsors + Add-Ons sections.

---

## Phase-by-phase implementation breakdown

### Phase A — Backend domain + infra (Commit A part 1, TDD-first)
1. **Domain methods + tests (red-green-refactor):**
   - Add `AddOnDefinition.SetImage(url, blobName)`, `ClearImage()`; tests: `SetImage_WithValidUrl_Succeeds`, `SetImage_WithEmptyUrl_Fails`, `ClearImage_RemovesBoth`, `ClearImage_WhenNoImage_Succeeds`, `SetImage_PreservesOtherFields`.
   - Add `SponsorConfiguration.WithImage(url, blobName)`; tests: `WithImage_ReturnsNewVoWithImage`, `WithImage_NullClears`, `Create_AcceptsNullImageDefaults`, `Equality_IncludesImageFields_ChangesWhenImageDiffers` (this pins F13 — equality components must include the new fields, else EF change-tracking silently misses updates).
2. **EF configuration update:** add `Property(x => x.ImageUrl).HasColumnName("image_url").HasColumnType("text")` + same for blob name on AddOnDefinitionEntityConfiguration. Sponsor config is JSONB → no column change, just VO regen.
3. **Migration scaffold:**
   - `dotnet ef migrations add Phase6A143_AddImageToAddOnDefinition --project src/LankaConnect.Infrastructure`
   - **For SponsorConfiguration**: even though it's JSONB (no column changes), running `dotnet ef migrations add Phase6A143_AddSponsorImageToSponsorConfig` is still required to regenerate `ApplicationDbContextModelSnapshot.cs`. The generated `Up/Down` will be near-empty (or contain only annotation churn) — that's expected; the value is the snapshot delta. Skipping causes future migrations to error with model-snapshot drift.
   - Per CLAUDE.md §5 (line 119 — "Check for conflicts"): verify only the intended changes appear in the generated `Up/Down`; if unrelated rows surface, abort and reconcile against develop before proceeding.
   - Verify `[Migration("…")]` on `.Designer.cs`.
4. **Migration smoke locally:** `dotnet ef database update` against a local DB or staging readonly replica.

### Phase B — Backend application + API (Commit A part 2, TDD-first)
1. **Command/handler tests (TDD-first):**
   - `SetAddOnDefinitionImageCommandHandlerTests` — happy path uploads blob, persists URL+blobName; auth gate (organizer only); validation failure (bad content-type, too large, empty file); old-blob deletion on replace; rollback on persistence failure.
   - `ClearAddOnDefinitionImageCommandHandlerTests` — happy path deletes blob + clears fields; no-op when no image present.
   - Same shape for sponsor handlers.
2. **Implement handlers** mirroring `AddImageToEventCommandHandler` (cite line 38 for validator usage, line 62 for rollback pattern).
3. **Controllers** add 4 endpoints — multipart `IFormFile` parameter, `[Authorize]`, organizer-scope check via existing `Event.IsOrganizer(userId)`.
4. **DTO + AutoMapper profile** — surface `imageUrl` on `AddOnDefinitionDto`; sponsor banner via `SponsorConfigDto`.

### Phase C — Deploy backend to staging
1. `git commit` Commit A.
2. `git push origin <branch>`.
3. `gh workflow run deploy-staging.yml --ref <branch>`.
4. Wait for green (~10–12min).
5. **Verify migration applied:** check deploy logs for `Phase6A143_AddImageToAddOnDefinition` row in `__EFMigrationsHistory`.
6. **API smoke matrix** (see § Smoke matrix below) — all cells GREEN before proceeding to Phase D.

### Phase D — Frontend (Commit B, TDD where applicable)
1. **Types regen:** add `imageUrl`, `imageBlobName` to `AddOnDefinitionDto` and `SponsorConfigDto` interfaces.
2. **Repository methods:** `eventsRepository.uploadAddOnImage(eventId, addOnId, file)` returns `{ imageUrl, imageBlobName }`. Same shape for clear + sponsor.
3. **React Query hooks:** `useUploadAddOnImage()`, `useDeleteAddOnImage()`, `useUploadSponsorImage()`, `useDeleteSponsorImage()`. Invalidate `useAddOnDefinitions(eventId)` and `useSponsorConfig(eventId)` queries on success.
4. **Editor forms:**
   - `AddOnDefinitionEditor` gets a file-input + preview + replace + remove buttons.
   - `SponsorConfigForm` gets a banner-image upload field after `sponsorMessage`.
5. **Display widgets:**
   - `AddOnSelector` — render 64×64 thumbnail in each row card; fallback to `Package` icon.
   - `SponsorSection` — render banner above form header.
   - Manage tabs — small thumbnail in list rows.
6. **RTL component tests:**
   - AddOnSelector renders thumbnail when `imageUrl` set; renders icon when null.
   - SponsorSection renders banner when image set; no banner when null.
   - Editor forms — file selection triggers POST; preview updates after upload.

### Phase E — Deploy frontend to staging
1. `git commit` Commit B.
2. `git push`.
3. `gh workflow run deploy-ui-staging.yml --ref <branch>`.
4. Wait for green (~5min).

### Phase F — Layout (Commit C)
1. Bump `max-w-7xl` → `max-w-screen-2xl` on the main constrained card on event details page.
2. Pass `defaultOpen={!!addOnConfig.imageUrl}` on Add-Ons section; same for Sponsors.
3. Visual smoke at 1280, 1536, 1920 widths.
4. Commit C + deploy UI.

### Phase G — Operator UAT
- See § UAT cells below.

---

## Smoke matrix (API testing via curl)

Run after Phase C deploys. Token from:

```bash
TOKEN=$(curl -sS -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"niroshhh@gmail.com","password":"1qaz!QAZ","rememberMe":true,"ipAddress":"string"}' \
  | python -c "import sys,json;d=json.load(sys.stdin);print(d.get('accessToken') or d.get('token',''))")
```

| # | Cell | Request | Expected |
|---|---|---|---|
| S1 | AddOn upload happy path | `POST /api/events/{eid}/add-ons/{aid}/image -F image=@test.png` with Bearer | 200 + `{imageUrl, imageBlobName}` |
| S2 | AddOn upload — non-organizer | Same as S1 with different user's token | 403 |
| S3 | AddOn upload — anonymous | Same as S1, no Authorization header | 401 |
| S4 | AddOn upload — too large (>5MB) | Same as S1 with 6MB file | 400 validation error |
| S5 | AddOn upload — bad content-type | Same as S1 with `.exe` | 400 |
| S6 | AddOn upload — replace existing | S1 twice with different files. Capture `OLD_URL=$(jq -r .imageUrl response1.json)`. After S6: `curl -sS -o /dev/null -w "%{http_code}\n" "$OLD_URL"` | Second call returns new URL; first blob deleted (HEAD on old URL returns `404`) |
| S7 | AddOn delete image | `DELETE /api/events/{eid}/add-ons/{aid}/image` | 204; subsequent GET returns null imageUrl |
| S8 | AddOn delete — no image set | DELETE when imageUrl is null | 204 (idempotent) |
| S9 | AddOn GET reflects image | `GET /api/events/{eid}/add-ons` after S1 | imageUrl present in response |
| S10 | Sponsor banner upload happy path | `POST /api/events/{eid}/sponsor-config/image -F image=@banner.png` | 200 + URL |
| S11 | Sponsor banner — non-organizer | Same as S10, different user | 403 |
| S12 | Sponsor banner — replace existing | S10 twice | New URL returned; old blob deleted |
| S13 | Sponsor banner delete | `DELETE /api/events/{eid}/sponsor-config/image` | 204 |
| S14 | Event GET reflects banner | `GET /api/events/{eid}` after S10 | `sponsorConfig.sponsorImageUrl` present |
| S15 | Migration applied (DB check) | Inspect `__EFMigrationsHistory` table on staging | `Phase6A143_AddImageToAddOnDefinition` row present |
| S16 | Old blob cleanup verification | After S6 + S12: `curl -sS -o /dev/null -w "%{http_code}\n" "$OLD_URL"` for both add-on and sponsor old URLs | Both return `404`. (SAS URLs return 404 even with valid token when blob is deleted — `AzureBlobStorageService.cs:97`.) |

---

## Operator UAT cells (after both Commit A & B deploy)

| # | Cell | Steps | Expected |
|---|---|---|---|
| U1 | Add-on with image — organizer creates | Manage page → Add-ons tab → New Add-on → fill name+price+upload image → save | Image preview shown in editor + saved |
| U2 | Add-on with image — buyer view | Public event details → Add-Ons section | 64×64 thumbnail next to add-on name |
| U3 | Add-on without image — buyer view | Same as U2 but for an add-on without image | Falls back to `Package` icon; no broken image |
| U4 | Add-on — replace image | Edit existing add-on → upload different image → save | Old image disappears, new shows |
| U5 | Add-on — clear image | Edit existing add-on → remove image button → save | Editor + selector fall back to icon |
| U6 | Add-on — Stripe checkout regression | Click "Buy" on an add-on with an image | Existing cart/Stripe flow works unchanged |
| U7 | Sponsor banner — organizer uploads | Manage page → Sponsors tab → upload banner | Preview shown + saved |
| U8 | Sponsor banner — buyer view | Public event details → Sponsors section | Full-width banner above form |
| U9 | Sponsor without banner — buyer view | Event without banner uploaded | Section renders without banner (current behavior preserved) |
| U10 | Sponsor — money mode regression | Buyer submits a money sponsor | Existing Stripe flow works |
| U11 | Sponsor — item mode regression | Buyer submits an item sponsor | Existing immediate-record flow works |
| U12 | Layout — wider container | Open event details on a 1440px+ display | Main card uses ~1536px width (was 1280px) |
| U13 | Layout — auto-expand with image | Event with add-on/sponsor image | Those sections open by default |
| U14 | Layout — collapsed without image | Event without images | Sections collapsed (current behavior) |
| U15 | Manage tabs — thumbnail column | Add-ons + Sponsors management tabs list view | Small thumbnails visible next to names |

---

## Effort estimate

| Commit | Hours |
|---|---|
| A — Backend (both entities, schema + endpoints + tests) | 6.0 |
| B — Frontend (both entities, editor + display + manage) | 5.0 |
| C — Layout (width + auto-expand) | 1.5 |
| Subtotal | 12.5 |
| Buffer for EF model-snapshot regen + JSONB equality test gotchas (per architect F13) | +2.0 |
| **Core total** | **14.5h** |

Plus ~1h for staging deploy + smoke + UAT hand-off per commit (3 × 1h = 3h overhead).

**Grand total: ~17.5h** including verification.

---

## Open questions for user (BEFORE implementation starts)

1. **Visual upgrade beyond auto-expand?** — Should we also restyle Add-Ons + Sponsors section cards (bigger headers, banner-style backgrounds, accent colors) when an image is present? Adds ~3h to Commit C. Default plan: no — widen + auto-expand only.

2. **Image dimensions / aspect ratio guidance** — should the editor enforce a particular aspect ratio (16:9 for sponsor banner, square for add-on thumbnail) via client-side crop? Or accept any aspect and let CSS object-fit handle the display side? Default plan: accept any, use `object-cover` + `object-center` in the display widgets.

(Branch strategy is a process choice — I'll ask conversationally, not via this doc.)

---

## Risk callouts

- **Stripe checkout flows** in `AddOnSelector.tsx` (`usePurchaseAddOnCart`) and `SponsorSection.tsx` (`useCreateMoneySponsor`) must remain untouched. Image rendering is a presentational sibling element only.
- **JSONB VO add** for SponsorConfiguration — existing rows have null image fields; VO factory must accept nullable defaults with `null` as the default value to avoid backfill.
- **Migration order** — Migration must run on staging before backend deploy completes. EF discovery requires `[Migration("…")]` on `.Designer.cs` per project rule.
- **Blob naming collision** — use existing `IAzureBlobStorageService.UploadFileAsync` which generates GUID-based names; no manual naming.
- **Container names** — use `event-addons` and `event-sponsors` so cleanup queries are isolated from the shared `event-media` container.
- **Soft-delete add-on with image** — when an add-on is deactivated, the blob remains (soft-delete pattern). Only `SetImage` replacement + `ClearImage` explicit calls remove blobs.

---

## Documentation update steps (per CLAUDE.md project rule)

After Commit A merges to feature branch:
1. Update `docs/PROGRESS_TRACKER.md` with Phase 6A.143 status entry.
2. Update `docs/STREAMLINED_ACTION_PLAN.md` action item status.
3. Update `docs/PHASE_6A_MASTER_INDEX.md` row 113: register `6A.143 | Add-on + Sponsor images + event details layout widening | 🔧 In Progress | 2026-05-14`.

After all commits land + UAT passes:
4. Update master TODO status from "📋 Master TODO ready" → "✅ SHIPPED + STAGING-VERIFIED" with commit SHAs.

---

## Approval checkbox

- [x] **D14 — Auto-expand only** (no visual upgrade). User confirmed 2026-05-14.
- [x] **Aspect ratios — Accept any, use CSS `object-cover`**. No client-side crop modal. User confirmed 2026-05-14.
- [x] **Branch — Bundle on `feat/phase-6a-141-ticket-checkin`** (current scanner branch). User confirmed 2026-05-14.
- [x] **Master TODO approved → proceed to Phase A**.

## Architect sign-off

✅ Plan-agent reviewed 2026-05-14 — APPROVED with 9 minor corrections, all applied to this document:
1. Sponsor endpoints route to `EventConfigController`, not `SponsorsController` (D4 + Files-to-touch updated).
2. Old-blob deletion on replace is NEW logic, not in event-hero handler today (D6 clarified).
3. SponsorConfiguration migration still needs `dotnet ef migrations add` for snapshot regen even though Up/Down will be near-empty (Phase A.3 clarified).
4. Concrete `curl` HEAD-check command in smoke matrix S6 + S16.
5. CLAUDE.md §5 line 119 cited for migration drift check (Phase A.3 footnote).
6. `EventConfigController.VerifyOrganizerAsync` reused for sponsor endpoint auth (D4).
7. Containers `event-addons` and `event-sponsors` auto-provision via `EnsureContainerExistsAsync` — no manual step (F9 footnote).
8. `SponsorConfiguration.GetEqualityComponents()` must include the new image fields (F13 added; Phase A test pins it).
9. Branch strategy moved out of doc (process question, not architectural).

Effort revised by architect from 12.5h+3h = 15.5h → **14.5h core + 3h overhead = 17.5h total** (added 2h buffer for EF snapshot regen + JSONB equality gotchas).
