# Master TODO — Phase 7D.1 Volunteer Signup Feature

**Created:** 2026-04-20
**Owner:** current session
**Architect approval:** ✅ Option A′ approved — reuse `SignUpList` aggregate with a `SignUpKind` discriminator (`Items=0`, `Volunteers=1`) rather than a parallel `VolunteerList` aggregate
**Scope:** Enable event organizers to recruit a capped number of volunteers for specific roles on an event (e.g. *Food Committee: 5 volunteers*). Dedicated organizer tab + public nav button + CSV/Excel export. Reuses existing slot-based sign-up item primitive.
**Source of truth:** This file. Mirrored into in-session TodoWrite. Tracking docs (`PROGRESS_TRACKER.md`, `STREAMLINED_ACTION_PLAN.md`, `TASK_SYNCHRONIZATION_STRATEGY.md`) get a closing entry per phase.
**Architect plan file:** `C:\Users\Niroshana\.claude\plans\another-emhancement-1-we-cheerful-valley.md`

---

## Acceptance criteria (whole feature)

- [ ] Organizer can create a volunteer list with slot-based roles (1 volunteer = 1 slot); quantity-based items are rejected.
- [ ] Volunteer lists appear in a dedicated "Volunteers" tab on the event management page, separate from sign-up lists.
- [ ] Public event page shows a "Volunteer" quick-nav button (only when the event has at least one volunteer list) that scrolls to a dedicated Volunteers section.
- [ ] Authenticated + anonymous users can commit to a volunteer role; remaining-slots decrements correctly; over-commit is rejected.
- [ ] Volunteer commitment confirmation email uses `template-volunteer-commitment-confirmation`; cancellation uses `template-volunteer-commitment-cancellation`; non-volunteer signup lists remain on the original templates.
- [ ] CSV + Excel export of a volunteer list shows volunteer-specific column headers ("Volunteer Role / Volunteers Needed / Volunteer Name / Committed").
- [ ] All existing sign-up list behaviour unchanged (regression-guarded).
- [ ] All three tracking docs updated per `TASK_SYNCHRONIZATION_STRATEGY.md`.

---

## Phase A — Backend Domain & Migration ✅ COMPLETE

**Status**: ✅ deployed + staging-verified (commit `ddd946d2`, deploy `24646994787`)

- [x] **1.** Domain: add `SignUpKind` enum + `Kind` property on `SignUpList`
- [x] **2.** Domain: change `Event.AddSignUpList` uniqueness to `(Kind, Category)`
- [x] **3.** Domain event: add `Kind` to `UserCommittedToSignUpEvent`
- [x] **4.** Infrastructure: map `Kind` in `SignUpListConfiguration` (NOT `builder.Ignore` — MEMORY 6A.123)
- [x] **5.** Migration `20260420023008_AddSignUpKindDiscriminator` generated + applied
- [x] **6.** Commit + push → staging auto-deployed

---

## Phase B — Backend Application & API ✅ COMPLETE

**Status**: ✅ deployed + staging-verified (commits `c68fd24b` + `20d350a1`, deploy `24680214036`)

- [x] **7.** Application: extend `CreateSignUpListWithItemsCommand` with `Kind`, add `CreateVolunteerListCommand`
- [x] **8.** Application: extend `GetEventSignUpListsQuery` with optional `kind` filter + add `Kind` to `SignUpListDto`
- [x] **9.** API: `EventsController` — `GET /signups?kind=Volunteers`; `POST /signups` accepts `Kind`
- [x] **10.** Commit + push → staging deploy. Curl tests PASS (6 scenarios) on list `e644703e-b592-469c-94ba-7b804357f918`

---

## Phase C — Email Pipeline ✅ COMPLETE

**Status**: ✅ deployed + staging-verified (commits `7ba600cb` → failed deploy `24682332058` (fixed in `a1243853`) → deploy `24683062394` SUCCESS; doc commit `0a027646`)

- [x] **11.** Contract: `EmailTemplateContract.VolunteerCommitmentConfirmation` / `VolunteerCommitmentCancellation` constants
- [x] **12.** Migration `20260420175444_Phase7D1_SeedVolunteerEmailTemplates` — inline SQL clone from signup-list templates via REGEXP_REPLACE
- [x] **13.** Handler branching by `Kind`: `UserCommittedToSignUpEventHandler.AsVolunteerConfirmation()`, `CommitmentCancelledEmailHandler.AsVolunteerCancellation()`
- [x] **14.** Commit + push → staging. Azure ACS logs confirmed both templates resolve and emails send (Operation `3589fe7e-...` for confirmation, 5541ms for cancellation)

### Phase C follow-ups (non-blocking, tracked for later)
- [ ] **C16a** — REGEXP_REPLACE rewrote Handlebars block names inside the cloned HTML; 6 `{{VolunteerListUrl}}` / `{{HasVolunteerLists}}` placeholders render blank because `SignupCommitmentEmailParams.ToDictionary()` still emits `SignUpListUrl` / `HasSignupLists`. Email delivery unaffected. Fix: narrow REGEXP to skip `{{...}}` contents OR add volunteer-keyed entries to ToDictionary with identical values.
- [ ] **C16b** — `CommitmentUpdatedEventHandler` lacks Kind-branching; same-user repeat-commit still resolves `template-signup-list-commitment-update` regardless of kind. Architect call on mirror vs. YAGNI.

---

## Phase D — Exports ✅ COMPLETE

**Status**: ✅ deployed + staging-verified (commits `9f8d6997` + `6029236d` + `9dda25bb`, deploy `24696959681`)

**Goal:** Volunteer-list CSV + Excel exports show volunteer-specific column labels ("Volunteer Role / Volunteers Needed / Volunteer Name / Committed"). Two new `ExportFormat` enum values (`VolunteersZip`, `VolunteersExcel`).

- [x] **15.** Export service: `SignUpExportLabels` record + optional `labels` parameter on `ExportSignUpListsToZip` / `ExportSignUpListsToExcelZip`. Default `ForItems()` keeps existing callers unchanged; `ForVolunteers()` relabels to "Volunteer Role / Volunteers Needed / Volunteer Name / Committed". Covered by 4 unit tests (2 CSV + 2 Excel) — all green.
- [x] **16.** Query: `ExportFormat.VolunteersZip` / `VolunteersExcel` enum values added. Handler filters `SignUpLists` by `Kind` (Volunteers vs Items) so the two export endpoints return disjoint sets; volunteer branch passes `SignUpExportLabels.ForVolunteers()` through the shared export services. Missing-lists returns a Kind-specific error ("No volunteer lists found for this event" vs "No signup lists found").
- [x] **17.** Controller: `volunteerszip` / `volunteersexcel` query-param values mapped in `EventsController.ExportEventAttendees`. Staging curl-test script `scripts/test_volunteer_export_staging.py` exercises all four scenarios and passed end-to-end on event `4378a7d9-280e-4322-9ca2-a17e27061ae8` (list "Phase 7D.1 Test - Food Committee").

### Phase D acceptance
- [x] Unit tests for volunteer-label CSV + Excel exports (TDD red-first) — 4 tests, all green.
- [ ] Integration test: 3-slot volunteer role + 2 commitments → Excel has correct headers + row count. *(Deferred — covered by staging curl + unit-level header assertions; revisit if regression found.)*
- [x] Staging curl: `GET /api/events/{id}/export?format=volunteersexcel` returns xlsx with volunteer headers ("Volunteer Role / Volunteers Needed / Volunteer Name / Committed" — verified via sharedStrings probe).
- [x] Existing sign-up list export regression-guarded: `format=signuplistsexcel` keeps "Item Description / Requested Quantity / Contact Name" headers; no leak of "Volunteer Role".

---

## Phase E — Frontend Types & Hooks ✅ COMPLETE

- [x] **18.** Types: add `SignUpKind` string enum (`'Items' | 'Volunteers'` — MEMORY 6A.124) + `kind` field on DTOs
- [x] **19.** Hooks: extend `useEventSignUps` with optional `kind` filter; separate query keys per kind
- [x] **20.** Zod: new `volunteerListSchema` (slot-based only, relabeled)
- [x] **21.** Component refactor: add optional `labels` prop to `SignUpManagementSection` + `SignUpCommitmentModal` (default labels keep existing UX 100% identical — CLAUDE.md Section 3)

**Evidence:** 20 unit tests green — 5 hook key/filter tests ([useEventSignUps.kind.test.ts](../web/tests/unit/presentation/hooks/useEventSignUps.kind.test.ts)), 8 Zod schema tests ([volunteer-list.schema.test.ts](../web/src/presentation/lib/validators/__tests__/volunteer-list.schema.test.ts)), 7 modal-labels regression-guard tests ([SignUpCommitmentModal.labels.test.tsx](../web/tests/unit/presentation/components/features/events/SignUpCommitmentModal.labels.test.tsx)). `npx tsc --noEmit` clean. Defaults preserve pre-Phase-7D.1 UX verbatim.

---

## Phase F — Frontend Organizer UI 🚧 IN PROGRESS

- [x] **22.** Create `VolunteerListsTab.tsx` (reuses `SignUpManagementSection` with `kind={SignUpKind.Volunteers}` + `volunteerSectionLabels`). Also threaded new optional `kind?: SignUpKind` prop through `SignUpManagementSection` → `useEventSignUps` so kind-scoped fetches cache independently. `SignUpListsTab` now passes `kind={SignUpKind.Items}` to isolate the existing tab. Edit-button routes now branch on `list.kind` (`/volunteer-lists/:id` vs `/signup-lists/:id`).
- [x] **23.** Added new `volunteers` tab to [manage/page.tsx](../web/src/app/events/%5Bid%5D/manage/page.tsx) tab array using the `Users` lucide icon.
- [x] **24.** Created [create-volunteer-list/page.tsx](../web/src/app/events/%5Bid%5D/manage/create-volunteer-list/page.tsx) — streamlined slot-only form (role name + volunteers-needed + notes), submits with `kind: SignUpKind.Volunteers`, `hasOpenItems: false`, items categorised as `Mandatory` per architect plan, redirects to `?tab=volunteers` after create.
- [x] **25.** Created [volunteer-lists/[signupId]/page.tsx](../web/src/app/events/%5Bid%5D/volunteer-lists/%5BsignupId%5D/page.tsx) — slot-only edit page with list-details save + inline per-role edit/remove/add. Fetches via `useEventSignUps(eventId, SignUpKind.Volunteers)` so the cached volunteer slice is reused.
- [ ] **26.** Commit + push → frontend staging deploy. Manual UI verification (create "Food Committee: 5 slots")

**Evidence (pre-deploy):** `npx tsc --noEmit` clean. 20 regression-guard unit tests green (5 hook, 8 Zod, 7 modal labels). No existing consumers of `SignUpManagementSection` affected — `kind` prop defaults to undefined (unfiltered fetch).

---

## Phase G — Frontend Public UI ✅ COMPLETE (API-smoke verified; UI-interactive deferred to user)

- [x] **G1–G5.** RED→GREEN: `hideQuantitySelector` prop on `SignUpCommitmentModal` + kind-conditional threading from `SignUpManagementSection` (14/14 Phase G tests green; 4 unrelated pre-existing Phase 6A.118 failures flagged separately — stash test confirmed 10→4 net-improvement)
- [x] **G6/G7.** RED scope decision: page-level render tests skipped (cost/value — 2800-line page with 20+ hooks); coverage deferred to G3 kind-thread test + staging smoke in G11. Architect approved.
- [x] **G8.** GREEN: edit [events/[id]/page.tsx](../web/src/app/events/%5Bid%5D/page.tsx) — add `HandHeart` + `SignUpKind` + `volunteerSectionLabels` imports, page-scope `useEventSignUps(id, SignUpKind.Volunteers)` to derive `hasVolunteerLists`, insert conditional "Volunteer" nav button, add `kind={SignUpKind.Items}` to existing signup-lists mount, add new `<div id="volunteers">` CollapsibleSection mounting `<SignUpManagementSection kind={Volunteers} labels={volunteerSectionLabels}/>` (YAGNI: skipped `VolunteerListSection.tsx` wrapper — direct mount is clearer)
- [x] **G9.** `tsc --noEmit` clean. Phase G vitest subset 14/14 green.
- [x] **G10.** Commit `8626a7c1` pushed to develop → `deploy-ui-staging.yml` run `24734887290` **succeeded** (4m35s).
- [x] **G11.** API-smoke (curl on staging) PASS: (a) `GET /signups?kind=Volunteers` returns disjoint list from `?kind=Items`; (b) volunteer slot item has `itemType=Slot`, `totalSlots=3`; (c) `POST /commit {quantity:1}` → `remainingSlots` decrements 3→2, commitment persists `quantity=1`; (d) cancel via `POST {quantity:0}` → slots restore 2→3. **UI-interactive checks deferred to user browser smoke** (nav-button click + scroll + modal render without slots input + cancel-dialog flow require a browser — cannot be verified via curl).
- [x] **G12.** Email routing verified via Azure Container Apps logs — cancel flow resolved `template-volunteer-commitment-cancellation` (send to `niroshhh@gmail.com` succeeded in 9145ms, subject "Commitment Cancelled for Christmas Dinner Dance 2025"). WhatsApp side sent via `signup_commitment_cancelled` Twilio template. Commit-confirmation template routing inherited from Phase C staging evidence (commit `7ba600cb` / deploy `24683062394`). **Follow-up flagged**: `template-volunteer-commitment-cancellation` has 7 unreplaced placeholders (6 HTML `{{#HasVolunteerLists}}`/`{{VolunteerListUrl}}`/`{{#HasVolunteerForms}}`/`{{VolunteerFormsUrl}}` + 1 text `{{ItemName}}`) — Phase C/D EmailTemplateContract/TypedEmailParams mismatch, **not a Phase G regression**, email still sends. See `C16a` follow-up box and new `G14` below.

### Phase G follow-ups (non-blocking)
- [ ] **G13.** Full-browser UI smoke on staging (user action) — event "Christmas Dinner Dance 2025": (a) Volunteer nav button appears in quick-nav bar; (b) click scrolls to `#volunteers` section; (c) Signup Lists section no longer contains volunteer tabs (kind-filtered); (d) volunteer modal title reads "Volunteer for This Role" with NO slots input visible; (e) submit + cancel work visually end-to-end.
- [ ] **G14.** Fix `template-volunteer-commitment-cancellation` placeholder mismatches — 7 unreplaced Handlebars tokens observed in delivered email. Same class of issue as `C16a` (REGEXP_REPLACE rewrote block/param names but `SignupCommitmentEmailParams.ToDictionary()` still emits the pre-clone names). Architect call on narrow-REGEXP vs. dual-key-dictionary.

---

## Phase H — End-to-End Verification ⏳ PENDING

- [ ] **31.** E2E smoke on staging (organizer creates role → anonymous signs up → slots decrement → organizer exports Excel → Azure logs clean)
- [ ] **32.** Documentation updates: PROGRESS_TRACKER.md, STREAMLINED_ACTION_PLAN.md, TASK_SYNCHRONIZATION_STRATEGY.md
- [ ] **33.** Final commit with phase summary + PR against `develop`

---

## Ongoing risks & mitigations

1. **Silent migration failure on `kind` column** — mitigated in Phase A via `HasDefaultValue(0)` + DB `DEFAULT 0` (MEMORY 6A.123 defence-in-depth). Status: ✅ no silent failures observed.
2. **TypeScript enum mismatch with JSON string serialization** — MEMORY 6A.124. To be enforced in Phase E step 18.
3. **Existing SignUp UI regression** — Phase E/F refactor keeps `labels` prop optional with identical defaults. CLAUDE.md Section 3.

---

## How to read this file

- **Checkboxes** are the ground truth. A step is done only when its checkbox is ticked.
- **Status badges** on phase headers (`✅ COMPLETE` / `🚧 IN PROGRESS` / `⏳ PENDING`) reflect the current state at the phase granularity.
- **Every commit that advances a step must tick its checkbox in the same commit or the immediate follow-up.**
- **End-of-turn statuses** reference step numbers from this file (e.g. "Step 15 done, step 16 next"), not private labels.
