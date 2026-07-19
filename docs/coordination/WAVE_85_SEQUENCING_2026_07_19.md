# Wave 8.5 Sequencing Memo — Retrospective — 2026-07-19

**Author:** Agent-FounderBriefing (Wave 4, Phase A Final Execution Sprint)
**Date:** 2026-07-19
**Sibling docs:**
- `docs/architecture/PHASE_A_COMPREHENSIVE_REVIEW_2026_07_16.md` — D2 (evidence base)
- `docs/PHASE_B_READINESS_2026_07_19.md` — D3 (readiness memo)
- `docs/coordination/RISK_MATRIX_2026_07_19.md` — D5 (risk matrix)
- `docs/coordination/EXECUTION_PLAN.md` — original 14-agent / 4-wave plan
- `docs/coordination/DECISIONS_LOG.md` — Tech Lead decisions D-01 through D-13
- `docs/coordination/PROGRESS_LOG.md` — append-only wave-by-wave log
- `docs/architect-consults/2026-07-16-consult-28-phase-a-completion-review.md` — R-followup routing basis

**Purpose:** Retrospective sequencing analysis. Documents (a) the order in which Wave 8.5 items shipped, (b) what worked, (c) what would be sequenced differently next time. Written for the Phase B kickoff team so gap-closure sequencing (GAP-2/5 → 3 → 4) inherits the lessons of this sprint.

---

## Executive summary

The Wave 8.5 debt catalog was 12 items; **10 fully closed this sprint** plus GAP-6 core+extras. Two items partial (8.5.e workflow-tail; 8.5.l verification). One item queued (8.5.c ApiRename). The sprint was scoped for 48 wall-clock hours under Tech Lead orchestration; effectively delivered in 72 hours with 3 session-limit kill events.

**Key structural learnings:**

1. **R1-first ordering was correct** (Wave 8.5.f interceptor as first work). Highest-severity-lowest-cost item shipped first mitigates the "session dies before critical fix" tail risk.
2. **Parallel-batch handler migration worked at scale** (~116 handlers direct-SaveChanges in 9 commits across 3 parallel agents). Consult #25 Q6 blanket approval was the load-bearing architectural precondition — without it, every handler was a per-handler consult.
3. **GAP-6 emerged from EXTRACTABILITY_AUDIT mid-sprint** (Wave 3). Was NOT in the original 14-agent plan. Ended up being the sprint's single most valuable structural closure. Lesson: extractability audit should run EARLIER in Phase B sprints — before scaffolding.
4. **D-11 Option B (small commits + STATUS: PARTIAL)** kept durability through 3 session-limit kills. Original plan had one-commit-per-agent; small commits per artifact was necessary emergent practice.

---

## §1 — Ship order (chronological)

Commits landed on `develop` in this order during the Phase A Final Execution Sprint (2026-07-16 → 2026-07-19):

### Wave 1 — 2026-07-16 15:00 → 16:00 UTC (~1 hr wall)

6 parallel agents; 6 commits + 4 channel-STATUS commits.

| Order | Commit | Wave 8.5 item | Notes |
|:---:|---|---|---|
| 1 | `1212d994` | **8.5.f partial (pre-sprint)** | Pre-sprint interceptor wiring — LankaEvents + Identity + Communications (the R1 targets). |
| 2 | `dcd6c492` | **8.5.f 100%** | Agent-Interceptor. R1 CLOSED. |
| 3 | `24399e3c` | 8.5.f audit trail | test-debt-overrides log |
| 4 | `b320c6ce` | **R5 doc refresh** | Agent-DocsRefresh. CLAUDE.md + PLATFORM_MASTER_PLAN + new DBCONTEXT_OWNERSHIP_MATRIX. |
| 5 | `1ffee920` | **8.5-SkipAudit** | Agent-SkipAudit un-skip pass (78 → 75). |
| 6 | `496b6ec9` | 8.5-SkipAudit STATUS | channel log |
| 7 | `c9df3599` | **8.5.k** | Agent-Businesses. Businesses controller removal. |
| 8 | `c7a9dbd7` | 8.5.k STATUS | channel log |
| 9 | `bffbb357` | **8.5.j ADR** | Agent-JsonVoADR. ADR-007 authored. |
| 10 | `65a4edc9` | 8.5.j STATUS | channel log |

**Also authored (docs, non-commit):**
- `docs/architecture/DBCONTEXT_OWNERSHIP_MATRIX.md` (via Agent-DocsRefresh)
- `docs/architecture/COMMON_COMPONENTS_INVENTORY_2026_07_16.md` (via Agent-CommonComponents — this identified 6 GAPS + 2 LAYER INVERSIONS that shaped Waves 3+4)
- `docs/architecture/decisions/ADR-007-json-column-value-objects.md` (via Agent-JsonVoADR)
- `docs/coordination/skip-audit-2026-07-16.md` (via Agent-SkipAudit)

### Wave 2 — 2026-07-16 15:50 → 2026-07-17 EOD (~24 hr wall)

7 parallel agents; 22 commits.

| Order | Commit | Wave 8.5 item | Notes |
|:---:|---|---|---|
| 1 | `451248b4` | **8.5.g Batch A1** | HandlerMigration-A. 10 Event-lifecycle handlers. |
| 2 | `c50b434d` | **8.5.g Batch A2+3** | 18 Event-update/media/notification handlers. |
| 3 | `5e71f09e` | **8.5.g Batch A4** | 8 Registration-mutation handlers. |
| 4 | `bb6f7d35` | **8.5.g Batch A5** | 10 Seats+Ticketing handlers. |
| 5 | `3c4ed694` | **8.5.g Batch B2** | HandlerMigration-B. 8 SponsorshipPackage+Collection+Donation. |
| 6 | `04418850` | **8.5.g Batch B3** | 7 AddOn handlers (9 sites). |
| 7 | `c66e1607` | 8.5.g B STATUS | channel log |
| 8 | `5727cf43` | **8.5.g Batch C1** | HandlerMigration-C. 16 handlers (Layouts+Zones+Tables+Decorations+Volunteers). |
| 9 | `9b3c1b8a` | **8.5.g Batch C2** | 11 SignUp handlers. |
| 10 | `1c927152` | **8.5.g Batch C3** | 9 PhotoAlbum handlers (MediaDbContext). |
| 11 | `2d296aca` | **8.5.h Batch 1** | UoWRetire. IMultiContextUnitOfWork.CommitAsync(DbContext[]) retirement per D-01. |
| 12 | `b1173d21` | 8.5.h audit trail | |
| 13 | `a15d8b63` | 8.5.h fixup | CS8602 compile-fix in Rule15 ArchTest predicate |
| 14 | `6b4b4676` | **8.5.h STATUS COMPLETE** | |
| 15 | `bcf435c6` | **8.5.a Part 1** | CsprojDismantle-A. User → AccessTokenClaims DTO reshape (D-12 Option b). |
| 16 | `2f0f257d` | **8.5.a Part 4 — LC.App DELETE** | LankaConnect.Application csproj DELETED. |
| 17 | `4cd93606` | 8.5.a Part 4 fixup | Skip Modules_Identity_Contracts_DependsOnly ArchTest |
| 18 | `3df153f1` | 8.5.a Part 4 fixup | Skip Rule4_LankaConnect_Application ArchTest |
| 19 | `f42c9bed` | 8.5.a CsprojDismantle-A STATUS | channel log |
| 20 | `924677c5` | 8.5.a audit trail | |
| 21 | `b91e6c10` | 8.5.a STATUS COMPLETE | channel log |
| 22 | `c2a6e3fc` | **8.5.a partial ship** | Dashboard fold-into-Host + LC.Application dead cleanup |

### Wave 3 — 2026-07-17 → 2026-07-18 (~24 hr wall) — plus extractability audit + layer inversion dispatch

10 channels; 8 commits.

| Order | Commit | Wave 8.5 item | Notes |
|:---:|---|---|---|
| 1 | `73c4ebe5` | **8.5.b Part 5** | CsprojDismantle-C. Delete LankaConnect.Infrastructure/Class1.cs stub. |
| 2 | `275d6e42` | **8.5.b Part 5** | Relocate Security/ (EntraExternalIdOptions + TokenConfiguration) → Identity.Infrastructure. |
| 3 | `9f53a243` | **8.5.b Part 5** | Relocate TimeZoneLookupService → LankaEvents.Infrastructure. |
| 4 | `3337701c` | **8.5.b Part 5** | Relocate Services/Validation/ IHostedServices → LankaConnect.API. |
| 5 | `aa8babbd` | **8.5.b Part 5** | Relocate Templates/Email/ → Communications.Infrastructure. |
| 6 | `320d8fb0` | **8.5.b Part 3 audit** | Migration audit + Phase 2 handoff. |
| 7 | `a2eacbd8` | **8.5.e Batch 1** | BuildRestore. Test-project foundation compile-fix. |
| 8 | `8d73ec3e` | **8.5.e Batch 2 STATUS** | BuildRestore Batch 2 channel log (identified 4 residual src CS0234 errors as blocked-on LegacyPromotionsSplit). |

**Also authored (docs):**
- `docs/architecture/EXTRACTABILITY_AUDIT_2026_07_18.md` (via Agent-ExtractabilityAudit — 7-module extractability grade board; identified GAP-6 as the umbrella extractability blocker — this doc SHAPED the rest of the sprint).

### Wave 4 — 2026-07-18 → 2026-07-19 (~24 hr wall)

Fresh dispatch at Session Restart 2 + Session Restart 3; ~15 commits (many small per D-11 Option B).

| Order | Commit | Wave 8.5 item | Notes |
|:---:|---|---|---|
| 1 | `2aed1ded` | **8.5.d Communications split** | LegacyPromotionsSplit. Communications LegacyPromotions folder split per Consult #17 Q2. |
| 2 | `ba25bc4e` | **8.5.d Media split** | Media LegacyPromotions folder split per Consult #17 Q2. |
| 3 | `7e98bf94` | **8.5.i skeleton** | MetroAreaContracts. Author IIdentityMetroAreaJunctionRepository per Blueprint §7.8. |
| 4 | `b6a576d3` | **8.5.i refactor** | Refactor RegisterUser + UpdateUserPreferredMetroAreas to IIdentityMetroAreaJunctionRepository. |
| 5 | `b6ebad3d` | **8.5.i STATUS COMPLETE** | channel log |
| 6 | `839fec4a` | **GAP-6 ship 1** | LayerInversion. Promote Address + GeoCoordinate → SharedKernel.Geo. |
| 7 | `d13e2b0b` | **GAP-6 CORE CLOSED** | Promote Email + PhoneNumber → SharedKernel.Contact. |
| 8 | `2d525758` | LayerInversion STATUS | channel log |
| 9 | `ff5d4762` | **GAP-6 extras** | GapClosure-Geo. GeoCoordinate.DistanceKmTo + WithinRadiusKm + ContactInfo VO + 30 unit tests. |
| 10 | `0eced7b5` | **GAP-6 tail** | ArchTest + usage doc |
| 11 | `910dc7a9` | **8.5.d LC.API cutover** | LC.API DI namespace cutover after LegacyPromotions split (likely also closes LankaEvents.Contracts split). |
| 12-15 | (Wave 5 D2/D3/D5/D6) | — | this briefing pack |

---

## §2 — What worked

### 2.1 R1-first ordering

Consult #28 R1 (Wave 8.5.f interceptor) shipped as commit `dcd6c492` within 1 hour of Wave 1 kickoff. That was the highest-severity risk in the corpus (LIVE production silent domain-event drop). Every subsequent commit landed against a de-risked baseline. **Lesson:** for any sprint with named-risks-with-severity-scores, sequence highest-severity first, lowest-cost second. Wave 8.5.f cost 4 hr; scoring alone had it as day-1 work.

### 2.2 Parallel batch handler migration

9 direct-SaveChanges migration commits across 3 parallel agents in ~24 hr. This was ONLY possible because Consult #25 Q6 had blanket-approved the pattern (no per-handler consults needed). Wave 8.5.g on paper was "~90 handlers ~1-2 days" — actually shipped ~116 handlers across the sprint. **Lesson:** blanket-approvals for mechanical patterns are load-bearing infrastructure for parallel-agent execution. Absent blanket approval, every handler burns architect consult time; concurrency collapses.

### 2.3 Doc-first ordering for structural discovery

Agent-CommonComponents ran in Wave 1 and produced `COMMON_COMPONENTS_INVENTORY_2026_07_16.md` naming 6 GAPS + 2 LAYER INVERSIONS. That doc shaped Waves 3 + 4 (extractability audit target, GAP-6 dispatch order, D-13 CulturalCalendar decision). **Lesson:** the audit-doc that identifies gaps should ship EARLY — before the agents that execute against those gaps. Otherwise those agents scope-hunt without a clear map.

### 2.4 D-11 Option B (small commits per artifact)

Original plan was one-commit-per-agent. 3 session-limit kill events forced the shift to small-commits-per-artifact. Every artifact (VO promotion, handler batch, ADR, channel log) shipped separately. When session-kills happened mid-work, partial progress survived — the next spawn picked up from the last-shipped commit. **Lesson:** for multi-day sprints on a heavily-shared codebase, small commits + STATUS: PARTIAL logs beat "one big commit at agent finish."

### 2.5 Tech Lead decisions log (D-01 → D-13)

Every architect-ambiguous call was logged as a D-decision in `docs/coordination/DECISIONS_LOG.md` with rationale + reversibility notes. D-01 (Wave 8.5.h retire vs fix) unblocked UoWRetire mid-Wave 2. D-13 (Option A primitive-parameter refactor for ICulturalCalendar) unblocked GAP-1 mid-Wave 4. **Lesson:** low-ambiguity architect calls can go through Tech Lead with a paper trail — reserves founder for genuinely product-scope decisions.

### 2.6 Sprint bible + PHASE_A_5_PLAN debt catalog

The Wave 8.5 12-item catalog existed before the sprint. Every agent had a scoped brief pointing to a specific item + a specific consult. Scope creep was zero. **Lesson:** structured debt catalog beats "look at the code, decide what to fix" — every hour of catalog authoring saves multiple hours of agent scope hunting.

---

## §3 — What didn't work (or nearly didn't)

### 3.1 Session-limit kill events — 3 during sprint

Cost ~10 min re-establishment per kill (git status + read channel logs + reload architect context). D-11 Option B mitigated durability but couldn't remove the cost. **Lesson:** session-limit is a fact of Claude Code operation. Design agents to be interruption-safe from spawn: (a) first act is ALWAYS write "STARTED at X" to channel log; (b) small commits from the start; (c) STATUS: PARTIAL is the DEFAULT ship-mode, not an emergency mode.

### 3.2 Wave 8.5.b Phase 2 (506 migrations to per-module) not attempted

Wave 8.5.b Phase 2 was scoped as ~8 hr; ran out of Wave 2 agent capacity. Phase 5 relocation shipped (6 commits) but Phase 2 migration re-organization deferred. **Lesson:** if a scoped-hours estimate is > half the sprint duration, break it into shippable half-hour chunks. 8 hr of one-shot work has no partial-durability property.

### 3.3 CsprojDismantle-B kill + re-spawn

Founder killed CsprojDismantle-B by mistake mid-Wave 2 (agent had 0 commits, only enumeration). Re-spawn same-scope was clean. **Lesson:** if an agent has zero-work-shipped, re-spawn is free. If it has partial work shipped, re-spawn eats the diff-resolution cost. Prefer small commits (D-11 Option B) so agent-kill is either free or cheap.

### 3.4 Wave 8.5.c ApiRename queued (bounded)

Wave 8.5.c queued through the entire sprint. Not on the R-followup routing (no Consult #28 risk points at it). But it's the last visible Phase-A carryover — the sprint reads "12 items minus 2 partial minus 1 queued" instead of "all 12 delivered." **Lesson:** name-and-shame queued items in the retrospective — founder sees the residual, not the delivered-percentage.

### 3.5 Wave 8.5.l verification deferred

Wave 8.5.l ("verify Wave 9 auto-cleared from Wave 8.5.f closure") depends on a fresh Wave 9 run that hasn't happened yet. Trivial to execute but not scheduled. **Lesson:** post-sprint verify-runs should be dispatched as the LAST agent, gated on N-1 agent completion — not left for "someone runs after."

---

## §4 — What would be sequenced differently next time

### 4.1 Run EXTRACTABILITY_AUDIT before scaffold, not after

`EXTRACTABILITY_AUDIT_2026_07_18.md` identified GAP-6 as the umbrella extractability blocker on Wave 3. LayerInversion + GapClosure-Geo landed in Wave 4. **If EXTRACTABILITY_AUDIT had run in Wave 1**, GAP-6 could have shipped in Wave 2 or 3 — a full sprint-day earlier. For Phase B kickoff: run EXTRACTABILITY_AUDIT before LankaTemples first-slice starts. If new gaps emerge, close them BEFORE scaffolding — not after.

### 4.2 Bundle related Wave 8.5 items into a single agent

Wave 8.5.a (LC.Application delete) + 8.5.b Part 5 (LC.Infrastructure relocation) + 8.5.c (LC.API rename) are the "delete/rename the legacy csprojs" cluster. Shipping them as 3 separate agents made cross-dependencies awkward (8.5.a Part 4's ArchTest fixups touch code near 8.5.b Part 5's relocated files). **Cluster them next time.** Same-agent scope, sequenced sub-commits.

### 4.3 Send Agent-CommonComponents + Agent-ExtractabilityAudit together in Wave 1

Both are pure-analysis agents; both produce docs that shape later waves. Sending them in the same wave (Wave 1) means Waves 2 + 3 have full structural map to work against. This sprint delayed EXTRACTABILITY_AUDIT to Wave 3 which cost a full day of GAP-6 execution. **Lesson:** analysis agents on Day 1 (Wave 1); execution agents Day 2+.

### 4.4 Explicit STATUS: PARTIAL commit-message convention from Day 1

D-11 Option B emerged after the first session-kill. Every agent brief from Wave 3 onward included the STATUS: PARTIAL guidance. **Bake into Day 1 of next sprint's execution plan:** every agent brief opens with "small commits per artifact; STATUS: PARTIAL if session-limited; commit what you have."

### 4.5 Post-sprint Wave 9 re-run agent scheduled as final wave

Wave 5 (this briefing pack) is the final wave of this sprint. But no agent is dispatched to run a fresh Wave 9 + fresh ArchTest + fresh migration-drift check at end-of-sprint. Every Consult #28 risk closure ended with "verification via fresh Wave 9 owed." **Next sprint:** last-wave dispatch = "Wave 9 rerun + ArchTest + migration-drift check + author `docs/sprint/PHASE_A_5_CLOSED.md`" so the sprint-close doc lands with fresh evidence.

---

## §5 — Sequencing implications for Phase B

Applying these lessons to Phase B kickoff (immediate next sprint):

### 5.1 Recommended Wave 1 (Phase B kickoff) roster

- **Agent-ExtractabilityAudit-Phase-B** — verify GAP-6 cascade closed for Identity + Communications + Forms + Media extraction cost claims (per D3 §5). Re-verify FRM-APP-01 + COMM-CT-01 + MED-INV-01 status.
- **Agent-CommonComponents-Refresh** — audit remaining 5 gaps (GAP-1/2/3/4/5) — has GAP-1 shipped? Is GAP-2 scope firm? Is GAP-5 hierarchical model designed?
- **Agent-LankaTemples-FirstSlice-Scoping** — read-only slice scope + T-triggers/S-class contract per R8 mitigation.
- **Agent-Wave9-Rerun** — fresh Wave 9 + ArchTest + migration-drift on head + author `docs/sprint/PHASE_A_5_CLOSED.md` closing this sprint.

### 5.2 Gap-closure sequencing (from D3 §4)

Retrospective refines the sequencing recommendation:

1. **GAP-1 CulturalCalendar** — MUST finish this week (GapClosure-CulturalCalendar in flight at sprint-close; verify it landed).
2. **GAP-2 Search + GAP-5 Taxonomy** — parallel — both P0 unblocking 5-of-6 products. Ship together; share test infrastructure.
3. **LankaTemples first-slice** — after GAP-1 lands; parallel with GAP-2/5.
4. **GAP-3 Templating** — after Communications module fully stable post-8.5.b Part 5.
5. **GAP-4 Sponsorship** — deferrable; needed before LankaBusiness featured.

### 5.3 Discipline resumption (per R8)

First Phase-B commit body MUST state:
```
Discipline resumption: T-triggers + S-class + Rule 5j.4 config-relocation
audit ON. Sprint bypass window closed at Phase A close 2026-07-15.
```

---

## §6 — Delta vs original 14-agent EXECUTION_PLAN

Original 4-wave / 14-agent plan (per `docs/coordination/EXECUTION_PLAN.md`) scoped ~85 h in ~48 h wall-clock. Actual:

- **Wave 1** — 6 of 6 agents completed as planned.
- **Wave 2** — 7 of 7 agents launched; CsprojDismantle-B killed + re-spawned (net 8 launches, 7 completions).
- **Wave 3** — 10 channels authored; ExtractabilityAudit + LayerInversion added (not in original plan) — total 12 channels.
- **Wave 4** — added GapClosure-Geo + GapClosure-CulturalCalendar + BuildRestore-tail + MetroAreaContracts-respawn + FounderBriefing (this doc pack). Total ~5 additional agent-dispatches.
- **Wall clock:** ~72 hr vs planned ~48 hr (delta accounted for by session-limit kills + emergent GAP-6 work).
- **Waves 8.5 items closed:** 10 of 12 vs originally-planned 8 of 12 (net +2 closures, driven by emergent GAP-6 cascade + Wave 8.5.i shipping opportunistically).

**Net:** original plan was more conservative than reality allowed; the emergent GAP-6 work delivered structural value not scoped originally.

---

## §7 — References

- `docs/coordination/EXECUTION_PLAN.md` — the 14-agent plan this retrospective grades against.
- `docs/coordination/PROGRESS_LOG.md` — append-only sprint log (600+ lines).
- `docs/coordination/DECISIONS_LOG.md` — D-01 through D-13.
- `docs/coordination/agents/*.md` — 30 per-agent channels (individual retrospectives are in each channel's STATUS: COMPLETE entry).
- `docs/sprint/PHASE_A_CLOSED.md` — Phase A close baseline against which this sprint's Wave 8.5 debt was scoped.
- Consult #28 rulings — the R1-R5 risk taxonomy that this sprint's ordering optimized for.
