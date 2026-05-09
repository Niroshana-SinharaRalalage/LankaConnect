# MASTER TODO — Phase 8X.11 Recovery (External Payment Events D1+D2)

**Created**: 2026-05-08 (UTC)
**Owner**: Engineer (current session)
**Architect approval**: APPROVED 2026-05-08 (recovery plan §8)
**Status**: 🔄 IN PROGRESS

---

## Why this recovery exists (honest reconstruction)

Phase 8X.11 was a combined slice fixing two UAT defects on Phase 8X (External Payment Events):

- **D1**: `ExternalRegistrationUrl` was mandatory → make it OPTIONAL (cash-at-door / bank-deposit / phone-only / email-only / in-person registration patterns).
- **D2**: RegistrationMode picker showed all 6 internal modes for ExternalPaid with NoRegistration greyed out → introduce new `RegistrationMode.External = 6` paired with `EventPaymentMode.ExternalPaid`. Picker shows "External Registration" auto-selected when ExternalPaid; other 6 modes disabled.

Slice was shipped via commits `8d2182d0` (the implementation) + `067c3f80` (doc sync).

### What went wrong

The engineer prematurely marked the slice "✅ SHIPPED + STAGING-VERIFIED" based on **backend-only evidence**:
1. `dotnet test` green
2. `deploy-staging.yml` run `25582399726` succeeded
3. 11/11 curl-based API smoke matrix PASS

The engineer **skipped**:
1. Master TODO file (the product owner explicitly asked "where is the master TODO list?" — this file is the retroactive correction).
2. `gh run list --workflow=deploy-ui-staging.yml` (would have shown 3x failure).
3. Browser UAT on staging (would have shown the OLD UI still rendering — picker had only 6 modes, URL field had red `*`).
4. `git add -p` on staged files (would have caught EventHeroImage pollution from parallel agent's working tree).

When the product owner opened the staging UI to test, they saw the OLD picker + the red asterisk on the URL field. **Three consecutive UI deploys had failed** because `web/src/app/events/[id]/page.tsx:58` imported `EventHeroImage` from a file that was **untracked** on develop. `git blame` confirmed the broken import line was authored by my own commit `8d2182d0` — Phase 8X.11 had unintentionally bundled in parallel-process working-tree changes (a Phase 8YB.1 hero-image refactor in progress by another agent).

### What changed during recovery preparation

**Surprise finding from §A pre-flight (2026-05-08 23:11 UTC)**: The parallel author committed `b3f5afcd` "fix(events): commit Phase 8YB.1 EventHeroImage to unblock UI staging deploy" which committed `EventHeroImage.tsx` to develop. The UI deploy `25584021284` for that commit **succeeded**. `develop` is now green; the architect's planned §B revert is no longer needed.

**Net effect**: Phase 8X.11 UI changes are now live on staging via the `b3f5afcd` deploy build (which includes all develop history up to that point — i.e., the Phase 8X.11 commit `8d2182d0` is included). The original premature SHIPPED claim was technically wrong at the time of writing (UI deploy was failing) but became true ~30 minutes later when the parallel author unblocked the build.

This master TODO documents the recovery sequence + the discipline rules that would have prevented the original false-positive.

---

## Section A — Pre-flight (read-only checks) ✅ COMPLETE

- [x] **A1**. Confirm current branch is `develop` and working tree is clean except for unrelated parallel-process modifications. ✅ branch=develop.
- [x] **A2**. `gh run list --workflow=deploy-ui-staging.yml --limit 5` — record failing run IDs. ✅ documented:
  - `25582158762` (commit `450974f2`) ❌ failure
  - `25582399702` (commit `8d2182d0` Phase 8X.11) ❌ failure
  - `25583096923` (commit `067c3f80` doc sync) ❌ failure
  - `25584021284` (commit `b3f5afcd` parallel author's fix) ✅ **success** ← unblocked
- [x] **A3**. `gh run view 25583096923 --log-failed` — confirmed build error: *"Module not found: Can't resolve '@/presentation/components/features/events/EventHeroImage'"* at `web/src/app/events/[id]/page.tsx:58`.
- [x] **A4**. `git log --oneline -10 web/src/app/events/[id]/page.tsx` — confirmed `8d2182d0` is the commit that introduced line 58 (the broken import).
- [x] **A5**. `git blame -L 56,62 web/src/app/events/[id]/page.tsx` — confirmed line 58 author is `8d2182d0` (engineer's commit). Pollution attribution: **engineer's own commit unintentionally pulled in parallel-process working-tree changes**.
- [x] **A6**. Confirmed origin/develop's `page.tsx` still has the EventHeroImage import (it's the correct state now that the parallel author committed the file).
- [x] **A7**. `gh run list --workflow=deploy-staging.yml --limit 1` — backend deploy is green; latest `df427c91` is in_progress (doc-only commit from parallel agent — non-blocking).
- [x] **A8**. `git ls-files --others --exclude-standard web/src/presentation/components/features/events/EventHeroImage*` returns empty — files are now tracked on develop (committed by `b3f5afcd`). DO NOT need to `git add` them; they're already there.

---

## Section B — Recovery code changes ✅ SKIP (no longer needed)

The architect's recovery plan §B mandated reverting the `EventHeroImage` import line in `page.tsx`. The parallel author's commit `b3f5afcd` resolved the underlying breakage by committing `EventHeroImage.tsx` to develop. The import is now a legitimate reference, not a dangling pollution. **No revert required.**

- [x] **B1-B6**. Skipped per surprise finding in §A.

---

## Section C — Doc retraction (honesty about the timeline) — IN PROGRESS

The previous "✅ SHIPPED + STAGING-VERIFIED" claims in `PROGRESS_TRACKER.md` and `STREAMLINED_ACTION_PLAN.md` were technically wrong at the time of writing. They became true ~30 minutes later when the parallel author unblocked the UI deploy. Honesty requires amending the docs to reflect the timeline:

- [ ] **C1**. `docs/PROGRESS_TRACKER.md` — amend the Phase 8X.11 entry to add a correction paragraph explaining: (a) the backend was verified at 22:36 UTC via 11/11 API smoke; (b) the UI deploy was failing for 3 consecutive runs at the time of the SHIPPED claim; (c) the parallel author's `b3f5afcd` unblocked the UI deploy at 23:11 UTC; (d) the SHIPPED claim was retroactively true but premature when made.
- [ ] **C2**. `docs/STREAMLINED_ACTION_PLAN.md` — same correction.
- [ ] **C3**. Do NOT touch unrelated phase entries.

---

## Section D — Staging discipline (the rule that was missed) — IN PROGRESS

- [ ] **D1**. `git status` — review every file listed before staging.
- [ ] **D2**. `git add -p` interactively for any FE file the engineer didn't author start-to-end this session. Inspect every hunk; reject anything unrelated to the recovery scope.
- [ ] **D3**. `git add` only the recovery-specific files: this master TODO + amended docs.
- [ ] **D4**. `git diff --staged` end-to-end visual scan. Confirm only the intended files + intended hunks are present.

---

## Section E — Commit + push — PENDING

- [ ] **E1**. Commit message: `fix(8x.11): retroactive recovery + master TODO + honest timeline`
- [ ] **E2**. `git push origin develop`.
- [ ] **E3**. Capture the new commit SHA.

---

## Section F — Deploy verification ✅ ALREADY GREEN (parallel author's fix)

- [x] **F1**. UI deploy `25584021284` for `b3f5afcd` ✅ success — verified via `gh run list`.
- [x] **F2**. BE deploy `df427c91` in_progress (doc-only, non-blocking).
- [x] **F3**. Backend regression risk: zero — Phase 8X.11 BE shipped at `25582399726` (success), no subsequent BE changes touch Phase 8X.11 code.
- [x] **F4**. Phase 8X.11 UI bundle is part of the `b3f5afcd` build (Next.js rebuilds the entire FE per deploy; develop history at `b3f5afcd` includes `8d2182d0`).

---

## Section G — API smoke matrix re-run (post-recovery) — PENDING

Re-run the 11-cell smoke against the now-fully-deployed staging. Note: this matrix passed on 2026-05-08 22:36 UTC against the BE-only deploy; re-running confirms no regression after `b3f5afcd` and `df427c91`.

For every cell, capture HTTP status + response body excerpt. Auth token via `POST /api/Auth/login` per CLAUDE.md.

- [ ] **G1**. `GET /api/Events/allowed-registration-modes?paymentMode=ExternalPaid` → `["External"]`.
- [ ] **G2**. `POST /api/Events` ExternalPaid + URL only → 201; DB `registration_mode = 6 (External)`, `external_registration_url` populated.
- [ ] **G3**. `POST /api/Events` ExternalPaid + instructions only (URL null) → 201 (cash-at-door pattern).
- [ ] **G4**. `POST /api/Events` ExternalPaid + all-three-empty → 201 (Q2=B allow-save).
- [ ] **G5**. `POST /api/Events` ExternalPaid + `registrationMode=NoRegistration` → 400 (Q1 strict; External is the right mode).
- [ ] **G6**. `POST /api/Events` ExternalPaid + `registrationMode=External` (explicit) → 201.
- [ ] **G7**. `POST /api/Events` Free + `registrationMode=External` → 400 (External requires ExternalPaid).
- [ ] **G8**. `POST /api/Events` OnPlatformPaid + `registrationMode=External` → 400.
- [ ] **G9**. `POST /api/Events` ExternalPaid + `donationsEnabled=true` → 400 (Q5=B monetisation cluster blocked).
- [ ] **G10**. `GET /api/Events/allowed-registration-modes?paymentMode=Free&isFreeAttendance=true` → 6 internal modes incl. NoRegistration; no External.
- [ ] **G11**. `GET /api/Events/allowed-registration-modes?paymentMode=OnPlatformPaid` → 5 internal modes; no External, no NoRegistration.

All G-cells must be green. If any cell fails, STOP and escalate.

---

## Section H — FE verification (semi-automated) + Browser UAT delegation — PENDING

The engineer cannot launch a real browser in this sandbox; the architect's H-cells require user interaction. Best engineer can do:

- [ ] **H-engineer**. Fetch the staging FE asset bundle and grep for telltale strings — `External Registration`, `(optional)` next to URL field, `paymentMode === ExternalPaid`. If the strings appear in the deployed bundle, the FE deploy materially carries Phase 8X.11.
- [ ] **H-user-1**. Open `/events/{any-externalpaid-event}/edit` on staging in a browser. Confirm: payment-mode radio = ExternalPaid is selectable; on selection, registration mode picker shows "External Registration" auto-selected; other 6 modes greyed out.
- [ ] **H-user-2**. Confirm URL field has "(optional)" suffix — no red asterisk.
- [ ] **H-user-3**. Confirm donation/sponsor/collection/signup-list cluster is hidden when ExternalPaid (replaced by an info card).
- [ ] **H-user-4**. Open the public detail page of an ExternalPaid event with URL only → ExternalRegistrationCta renders the CTA button.
- [ ] **H-user-5**. Same with instructions only → instructions card promoted, no broken button.
- [ ] **H-user-6**. Same with all-empty → friendly "Contact organiser" fallback card.

---

## Section I — Status finalization — PENDING

- [ ] **I1**. Update `docs/PROGRESS_TRACKER.md` with the corrected timeline + recovery commit SHA.
- [ ] **I2**. Update `docs/STREAMLINED_ACTION_PLAN.md` similarly.
- [ ] **I3**. Mark every box in this master TODO `[x]`.
- [ ] **I4**. Commit doc updates.

---

## Section J — Hard-stop conditions (escalation triggers)

- [ ] **J1**. Any §G-cell returns wrong status code → backend regression suspected, escalate.
- [ ] **J2**. §H-engineer grep for telltale strings returns ZERO matches → FE bundle on staging doesn't carry Phase 8X.11; recheck deploy log.
- [ ] **J3**. User reports that browser UAT (H-user-*) shows OLD UI → cache / CDN / wrong env; escalate before claiming success.
- [ ] **J4**. Engineer is tempted to `git add` parallel author's untracked work → STOP. Architect-locked rule.

---

## Section K — Comms — PENDING

- [ ] **K1**. After §I, send single status message to product owner with: recovery commit SHA, both workflow run IDs (success), G-cell pass count, H-engineer grep evidence, and explicit ask for H-user UAT.
- [ ] **K2**. No "✅ SHIPPED + UI-VERIFIED" claim until the user confirms H-user UAT.

---

## Discipline rules going forward (architect-locked, applies to every future slice)

1. **Pre-commit**: `git add -p` on any file the engineer didn't author start-to-end this session. Never whole-file `git add` if not 100% authored.
2. **Pre-commit**: `git diff --staged` end-to-end visual scan. Unrecognised symbols (e.g. `EventHeroImage` in a Phase 8X.11 commit) = parallel-process pollution → unstage.
3. **Pre-push**: `gh run list` for **every** workflow the change touches — `deploy-staging.yml` + `deploy-ui-staging.yml` for cross-stack.
4. **Pre-push**: simulate CI locally for the surface shipped. FE: `npx tsc --noEmit && npm run build`. Phase 8X.11 break would have been caught by `tsc --noEmit` in 30 seconds.
5. **Pre-status-update**: open the actual staging URL in an actual browser and walk the actual user flow. Backend curl smoke is necessary, never sufficient for cross-stack slices.
6. **Master TODO file before any code change** on a multi-step slice. No master TODO = no slice starts.
7. **Never claim SHIPPED on backend evidence alone** for a cross-stack slice.

---

## Lesson logged

The engineer ran `git add web/src/app/events/[id]/page.tsx` whole-file when staging Phase 8X.11. The working tree had unrelated edits from a parallel agent's Phase 8YB.1 work (the `EventHeroImage` refactor). Whole-file staging propagated those edits silently. The companion `EventHeroImage.tsx` was untracked, so the import resolved to a missing module — but only at Next.js build time on CI, not in `dotnet test`. Backend curl smoke didn't catch it because the API doesn't render `page.tsx`.

The engineer claimed "✅ SHIPPED + STAGING-VERIFIED" based on the BE evidence and didn't open the staging UI to confirm. The product owner did, saw the OLD UI, and called it out. This master TODO + the discipline rules above are the durable correction.

End of recovery master TODO.
