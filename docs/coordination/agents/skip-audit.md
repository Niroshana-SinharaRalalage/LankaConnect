# Agent Channel: SkipAudit

**Agent role:** Audit all 78 Wave 9 smoke SKIPs, categorize, un-skip the recoverable ones, produce final report.
**Priority:** P2 (unblocks "test suite adequacy" green rating per architect Consult #28 Q3)
**Est time:** 3 hours
**Reports to:** Tech Lead (Claude)

---

## Task brief

Wave 9 latest run: 310 / 13 / 78 / 401 = 77.31% pass. **78 SKIPs = 19.5% of the suite.** Per `[[feedback-no-skip-without-valid-reason]]` memory: target 95%+ real coverage; >5% SKIP requires architect+founder approval.

Architect Consult #28 Q3: current 19.5% invalidates the "green means green" gate. Founder decisions on Wave 9 pass rate are made on inflated signal.

## Deliverable

1. **Enumerate every SKIP** in the latest Wave 9 report:
   - Path: `reports/wave-9-<latest-timestamp>/` (find with `ls reports/wave-9-* | tail -1`)
   - Read the JSON / test-result files inside; extract per-test: name, category, skip reason
   - Also grep smoke scripts: `grep -rn "Skip-Test\|-Skip\b" scripts/smoke/*.ps1`

2. **Categorize each SKIP** into one of these buckets:
   - **VALID-external** — external dependency (OAuth, ACS quota, inbox token, real-signature endpoint) that cannot be simulated
   - **VALID-founder-UAT** — Wave 9.h.6 chain deferred to founder manual UAT (documented in Phase A close)
   - **VALID-hard-technical** — genuinely unreproducible in staging (e.g. specific cross-region routing)
   - **RECOVERABLE-lazy** — could be un-skipped with a fixture or test-mode toggle
   - **RECOVERABLE-obsolete** — code path removed, test never updated
   - **RECOVERABLE-parked-work** — feature not yet implemented but should be

3. **Un-skip all RECOVERABLE-* SKIPs** you can un-skip in-file (fixtures, isolated users, test-mode toggle). For each un-skipped test, either:
   - Convert to a real assertion that passes
   - Convert to a real assertion that fails + adds to the "residual fails" workload for Agent-ResidualFails to handle
   - If un-skipping requires more than 15 minutes of engineering per test, flag as "deferred to Agent-ResidualFails-followup" and leave the SKIP with an updated reason

4. **Produce `docs/coordination/skip-audit-2026-07-16.md`** — table:
   | Test name | Category | Cluster | Original reason | Action taken | New status |
   | --- | --- | --- | --- | --- | --- |

5. **Final SKIP count target:** below 20 (5% of 401). If you can't get there in your time budget, report the achievable number + list remaining SKIPs with per-item justification.

6. Commit as ONE commit (or two if scope makes it cleaner — grep updates separated from real test-shape refactor):
   - Body: `Wave 9 SKIP audit — <X> tests un-skipped; <Y> remain (categories: VALID-external N, VALID-founder-UAT N, VALID-hard-technical N)`
   - `T-triggers: T5 (test refactor)`
   - `S-class: S1 (re-run Wave 9, verify pass count + skip count changed as expected)`
7. Push to `develop`.

## Constraints

- Do NOT modify any test that is currently passing.
- Do NOT change test assertions to make tests pass; you can un-skip a test only if:
  - The underlying code path exists (grep proves it)
  - AND the SKIP reason doesn't identify a hard blocker (external dep etc.)
- Any SKIP kept must have a specific per-test reason on file.

## Communication protocol

- Post enumeration count first (how many SKIPs, per script).
- Post categorization results.
- Post un-skip actions per test.
- Post `STATUS: COMPLETE` at bottom with commit SHA + new Wave 9 SKIP count.

## Log

### 2026-07-16 — Agent-SkipAudit start

**Enumeration (78 SKIPs, per baseline report `reports/wave-9-20260716-103429/`):**

| Cluster | SKIPs | Notes |
|---|---:|---|
| AddOns | 5 | all cascade from add-on definition create fail |
| AdminRecovery | 1 | destructive replay — opt-in with `-IncludeDestructive` |
| AdminUsers | 1 | F34 probe-ENTRY superseded |
| Auth | 2 | logout bearer conflict + Entra OAuth |
| Businesses | 8 | RECOVERABLE-obsolete — Agent-Businesses owns (stubbing whole script) |
| Content | 1 | opt-in multipart upload |
| Events | 24 | 5 add-attendees + 15 forms-full + 2 organizer-contacts + 1 rsvp-log + 1 admin-403 |
| LongTail | 9 | 3 photo-albums + 5 badges + 1 whatsapp-webhook stub |
| Newsletter | 2 | inbox-token requirement (external) |
| Payments | 1 | Stripe HMAC (external) |
| PhotoAlbums | 11 | all cascade from album create fail |
| Sponsors | 5 | all cascade from off-platform sponsor create fail |
| VenueLayouts | 6 | 2 table + 2 decoration + 2 tier-assignment |
| WhatsAppWebhook | 2 | F31b strict-mode env var config on staging |

**Categorization results:**
- VALID-external: 6
- VALID-hard-technical: 4
- VALID-opt-in-flag: 1
- RECOVERABLE-obsolete: 3 (removable now) + 8 (owned by Agent-Businesses)
- RECOVERABLE-cascade (fixture failure): 54 (auto-resolve once Agent-ResidualFails ships)
- RECOVERABLE-parked-work: 2 (paid-event tier-assignment fixture)

**Un-skip actions:**
- **Smoke-EventsController.ps1:465** — REMOVED SKIP `organizer-contacts :: update organizer contact` (no per-contact PATCH endpoint exists; batch PUT is the only mutator and is tested at line 448)
- **Smoke-EventsController.ps1:466** — REMOVED SKIP `organizer-contacts :: delete organizer contact` (same reason)
- **Smoke-LongTail.ps1:278-283 + manifest line 330** — REMOVED `Test-WhatsAppWebhook` stub function + its `whatsapp-webhook` section entry. Coverage lives in `Smoke-WhatsAppWebhookController.ps1` (Wave 9.h.10.4).

Net immediate: **−3 SKIPs** (78 → 75). Combined with Agent-Businesses's `Smoke-BusinessesController.ps1` stub (−7 net) → 68 SKIPs. Combined with Agent-ResidualFails cascade unlocks (~54 SKIPs collapse to PASS/FAIL) → **≈ 14 SKIPs total** post-sprint (below 20 gate).

**Full report:** `docs/coordination/skip-audit-2026-07-16.md`

### Constraints observed
- Did NOT modify any test that is currently passing.
- Removed SKIPs are for endpoints that either do not exist (organizer-contacts per-item PATCH/DELETE) or are duplicated elsewhere (whatsapp-webhook stub). No assertions weakened.
- Every kept SKIP has a per-item reason preserved in the categorization report.
- Content multipart + Auth logout flagged as candidates for a follow-up un-skip pass (each ~30-45 min); held back because task brief caps per-test un-skip at 15 min.

### Recommendation to Tech Lead
- Agent-ResidualFails scope must include the ~7 *silent* fixture failures (add-on definition, off-platform sponsor, venue table/decoration, badges, forms) in addition to the 13 residual FAILs, to hit the < 20 SKIP gate.

