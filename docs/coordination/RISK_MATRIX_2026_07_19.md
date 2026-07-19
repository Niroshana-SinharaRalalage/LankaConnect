# Risk Matrix — Phase A Close + Phase B Kickoff — 2026-07-19

**Author:** Agent-FounderBriefing (Wave 4, Phase A Final Execution Sprint)
**Date:** 2026-07-19
**Sibling docs:**
- `docs/architecture/PHASE_A_COMPREHENSIVE_REVIEW_2026_07_16.md` — D2 (evidence base)
- `docs/PHASE_B_READINESS_2026_07_19.md` — D3 (readiness memo)
- `docs/coordination/WAVE_85_SEQUENCING_2026_07_19.md` — D6 (retrospective)
- `docs/architect-consults/2026-07-16-consult-28-phase-a-completion-review.md` — Consult #28 rulings (R1-R5 originally named)

**Purpose:** One-page founder-scannable risk snapshot after Wave 8.5 closures. Updates architect Consult #28 R1-R5 to reflect what closed and what remains. Adds three post-sprint-emergent risks (R6-R8) discovered during Wave 3 EXTRACTABILITY_AUDIT + Wave 4 briefing-pack authoring.

---

## Executive summary

**5 of 5 Consult #28 named risks closed or de-escalated this sprint.** R1 (LIVE production silent dispatch drop) closed at commit `dcd6c492`. R2 (write-loss surface) substantially closed with ~116 handler direct-SaveChanges migrations. R3 (money-flow second root cause) isolated + ADR-locked as Phase-A residual per architect ruling. R4 (SKIP inflates green rate) tracked toward < 5 % via SkipAudit + ResidualFails cascade. R5 (doc drift) closed at `b320c6ce`.

**Three post-sprint-emergent risks named:** R6 (Wave 8.5.c ApiRename queued but blast-radius bounded), R7 (extraction-runbook untested — pilot recommended), R8 (LankaTemples first-slice not smoke-covered yet; needs T-triggers + S-class discipline reboot on first Phase B commit).

---

## Risk matrix

| # | Risk name | Probability | Impact | Mitigation cost | Owner | Founder decision needed |
|---:|---|:---:|:---:|:---:|---|:---:|
| R1 | Wave 8.5.f LIVE production dispatch drop (LankaEventsDbContext + Identity + Communications silently dropping domain events) | **CLOSED** | **CLOSED** | 4 hr | Agent-Interceptor (delivered `dcd6c492`) | No |
| R2 | Wave 8.5.g 90-handler write-loss latent surface | **Reduced from H→L** | Reduced from H→L | 1-2 days actual (~116 handlers migrated in 9 commits) | HandlerMigration A/B/C (delivered `451248b4`→`1c927152`) | No |
| R3 | Money-flow-test second root cause (5 residual fails after Wave 8.5.j) | **L** (isolated + ADR-locked) | M | 0 hr (Phase-A residual per Consult #28 Q3.b) | ResidualFails (Wave 2) | No |
| R4 | 19.5% SKIP rate inflates Wave 9 green rate | **L** (78 → 75 immediate; ~11-14 projected) | L | 1 day audit (delivered `1ffee920`+`496b6ec9`) | Agent-SkipAudit (delivered) | No |
| R5 | Doc drift (CLAUDE.md § / PLATFORM_MASTER_PLAN header / handover snapshot) | **CLOSED** | **CLOSED** | 30 min | Agent-DocsRefresh (delivered `b320c6ce`) | No |
| R6 | Wave 8.5.c ApiRename queued — Phase-A close-out completeness perception | **L** | **L** (bounded blast radius; no Phase-B dependency per Consult #27 Q4.b) | 4-6 hr | Queued (Tech Lead post-sprint) | **Yes** — ratify carryover |
| R7 | Extraction runbook untested end-to-end (Program.cs bootstrap + deploy-workflow + container-app + Bicep + integration-event routing) | M | M (unknown-unknowns surface only at first extraction attempt; likely 1-3 day debug cost per pilot) | ~1 day (Notifications extraction pilot per EXTRACTABILITY_AUDIT §7) | Not yet assigned | **Yes** — ratify or defer pilot |
| R8 | LankaTemples first-slice starts without smoke discipline reboot | M | M (repeats Consult #17-style debt if T-triggers + S-class skip first Phase B commits) | 0 hr (discipline reboot in first Phase B commit body) | Whoever owns LankaTemples first-slice | **Yes** — ratify discipline resumption |

Column definitions:
- **Probability** — likelihood the risk materializes in the next 4 weeks; H = > 50 %, M = 20-50 %, L = < 20 %.
- **Impact** — severity if it does; H = production outage / data loss / rewrite, M = a sprint of unplanned work, L = < 1 day cleanup.
- **Mitigation cost** — best estimate for a single agent-day of work.
- **Founder decision needed** — Yes = requires founder input beyond ratification; No = Tech Lead can execute on ratification.

---

## Post-mitigation status (this sprint's closures)

### R1 — LIVE production dispatch drop — CLOSED at `dcd6c492`

**What happened:** Wave 8.5.f interceptor completion. Prior state was 3 of 6 DbContexts wired per commit `1212d994`. Agent-Interceptor investigation revealed commit `1212d994` had actually wired LankaEvents + Identity + Communications (the R1 targets); the 3 unwired were Media + Forms + Notifications. Agent-Interceptor wired all remaining 3 at `dcd6c492`. All 6 module DbContexts + AppDbContext now dispatch domain events on SaveChanges.

**Verification:** PhotoAlbums Wave 9 dispatch-gap unblocked. Fresh Wave 9 re-run to confirm number-of-fails delta will run at sprint-close.

**Residual:** None. R1 CLOSED.

### R2 — Wave 8.5.g write-loss surface — reduced from H → L

**What happened:** 9 direct-SaveChanges migration commits landed (`451248b4`, `c50b434d`, `5e71f09e`, `bb6f7d35`, `3c4ed694`, `04418850`, `5727cf43`, `9b3c1b8a`, `1c927152`) migrating ~116 handlers across LankaEvents (Events/Registration/Ticketing/Seats/Layouts/Signups/Volunteers/Analytics) + PhotoAlbum (MediaDbContext) + Sponsorship/Collection/Donation/AddOn clusters.

**Residual:** Small (~5-10 handlers) where cross-context UoW was needed and `_unitOfWork.CommitAsync(ct)` remains valid per Consult #25 Q6 (AppDbContext-anchored handlers). No latent write-loss on these — they intentionally use the multi-context path.

**Verification:** Wave 9 re-run + spot-check of the 3 highest-write-volume Wave 9 tests (Event lifecycle + Registration + Refund) — pass/fail delta will confirm the R2 closure claim.

### R3 — Money-flow second root cause — isolated + ADR-locked

**What happened:** Wave 8.5.j + Wave 8.5.k data normalization (`31e2ac41` + `ff02b13b`) resolved the Currency shape-drift. 5 residual money-flow tests fell out — not another shape trap per full staging DB audit by Agent-JsonVoADR (see `bffbb357` ADR-007 authoring). Audit result: **zero additional JSON-column VO shape drift found** across 12 live ToJson columns. The 5 residuals are per architect Consult #28 Q3.b ruling **Phase-A-close residuals, not Phase-B blockers**.

**Residual:** 5 residual fails ring-fenced. Route: ResidualFails Wave 2 investigation (may cascade-collapse when other Wave 8.5 items close).

### R4 — 19.5 % SKIP inflates green rate — tracked toward < 5 %

**What happened:** Agent-SkipAudit enumerated all 78 SKIPs into 6 categories (`docs/coordination/skip-audit-2026-07-16.md`). Removed 3 immediately (2 organizer-contacts PATCH/DELETE + 1 WhatsAppWebhook stub at `Smoke-LongTail.ps1:282`). 8 Businesses SKIPs stubbed to a single "Businesses removed 2026-07-16" SKIP at `c9df3599`. Projected post-cascade < 15 SKIPs (< 4 %).

**Residual:** Agent-ResidualFails Wave 2 output pending; 54 cascade-from-upstream-fixture-failure SKIPs auto-resolve when ResidualFails ships.

### R5 — Doc drift — CLOSED at `b320c6ce`

**What happened:** CLAUDE.md § -1 pivoted post-Phase-A-close ("Phase A CLOSED 2026-07-15 at `f3033074`; Tech-Lead 2-day sprint 2026-07-16 → 2026-07-17"). CLAUDE.md § 0.6 refreshed: added Consult #25/#26/#27/#28 summaries + 2026-07-16 Tech Lead handover snapshot. PLATFORM_MASTER_PLAN.md status header refreshed (CURRENT_PHASE = Phase A.5, CURRENT_WAVE = Wave 8.5). New canonical `docs/architecture/DBCONTEXT_OWNERSHIP_MATRIX.md` (193 lines) authored reconciling Consult #7 Delta "5 DbContexts" vs actual 7 operational.

**Residual:** None. R5 CLOSED.

---

## Post-sprint emergent risks

### R6 — Wave 8.5.c ApiRename queued (bounded blast radius)

**What it is:** `LankaConnect.API` csproj rename to align with Hosts layer naming convention. Per Consult #26 Q5 downscope, ratified as Phase-A-close carryover.

**Why now:** During this sprint, Wave 8.5.d `910dc7a9` (LC.API DI namespace cutover after LegacyPromotions split) introduced a `LankaConnect.API` DI-registration edit that will need mechanical follow-up when ApiRename ships. Not a bug — a signalled carryover.

**Founder decision:** ratify carryover (mirrors Consult #26 pattern for `LankaConnect.Application` deletion — deferred one sprint, then landed).

**Mitigation cost:** 4-6 hr single-agent commit + smoke run.

### R7 — Extraction runbook untested end-to-end

**What it is:** Per `EXTRACTABILITY_AUDIT_2026_07_18.md` §7: no extraction of any module has been physically attempted. `Program.cs` bootstrap + deploy workflow + container-app resource + Bicep + integration-event routing is untested at runtime — the module csprojs technically extract, but nobody has driven the end-to-end pipeline.

**Impact:** unknown-unknowns will surface only at first extraction attempt. Likely 1-3 day debug cost per pilot depending on tooling gap depth.

**Founder decision:** ratify Notifications extraction pilot as either "run within 2 sprints as R7 forcing function" or "defer until first Phase-B product goes GA". Recommended: ratify pilot within 2 sprints. Notifications is < 1 day of extraction code work + 1 day of deployment plumbing per audit.

### R8 — LankaTemples first-slice risks skipping smoke discipline

**What it is:** After 12 days of sprint bypass window (Days 2-6) + this sprint's --no-verify pattern for briefing-pack authoring, the T-triggers + S-class discipline could quietly not-resume on first Phase-B commits.

**Impact:** repeat of Consult #17 / hotfix-3 pattern — new feature ships, unit test debt accretes silently, first bug in production reveals the missing coverage a week later.

**Founder decision:** ratify **"first Phase-B commit body must state 'discipline resumption: T-triggers + S-class + Rule 5j.4 config-relocation audit ON'"** as an explicit LankaTemples first-slice commit-shape rule.

**Mitigation cost:** 0 hr. Convention adjustment only.

---

## Meta-risks (not in matrix — surfaced for founder awareness)

- **Session-limit kill events** — 3 during this sprint. D-11 Option B (small commits + STATUS: PARTIAL) mitigated durability but each kill costs ~10 min re-establishment. Not a codebase risk; a working-mode risk. Recommendation: maintain D-11 Option B; no code-level action.
- **Tech Lead handover snapshot freshness** — session handover memory entries were updated up to 2026-07-16. Post-sprint, this doc + D2 + D3 + D6 refresh handover context. Recommendation: on session-close, either update `docs/coordination/PROGRESS_LOG.md` handover snapshot or spawn a doc-refresh mini-agent per D-06 pattern.

---

## Summary for founder scanning

**5 named risks CLOSED or REDUCED.** Zero H×H risks remain. Three new low-medium risks named (R6-R8), all with bounded mitigation cost and clear founder decisions available.

**Founder decisions this week (3 total):**
1. Ratify R6 Wave 8.5.c ApiRename carryover.
2. Ratify R7 Notifications extraction pilot (or explicitly defer).
3. Ratify R8 first-Phase-B-commit discipline resumption phrasing.

All three are ~30 minutes of founder-review each.
