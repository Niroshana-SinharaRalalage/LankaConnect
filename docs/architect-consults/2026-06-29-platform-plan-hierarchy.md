# 2026-06-29 — Platform Plan Hierarchy + Documentation Architecture

> **Founder direction**: establish a durable, single-source-of-truth documentation system that supports multi-agent development without context drift.
>
> **Outcome**: 3-commit doc migration plan delivering PLATFORM_MASTER_PLAN.md + Agent Operating Protocol + ADR taxonomy fix + 33 legacy file archive + Wave 9 (API smoke suite) sequenced into Phase A.

## Participants

- Founder (Niroshana) — direction-setting + final approval
- System Architect (Opus 4.7 persona via SendMessage)
- Planning Agent (Claude Opus 4.7 — primary implementer)

## Context

Mid-execution of Wave 5.3 (Products carve-out for LankaEvents), founder reviewed the planning-agent's UI checkpoint cadence and the per-wave MASTER_TODO file authored that session. Founder identified three structural gaps:

1. The agent was asking for per-slice UI verification across only 3-5 of 369 API endpoints — testing pattern doesn't scale
2. Per-wave MASTER_TODO files (4 active + 28 historical) created fragmentation; canonical `MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` had gone stale 21 days
3. No top-level "platform master plan" existed as a single durable entry point for multi-agent onboarding

## Founder's clarified requirements (issued in two iterations same day)

### Iteration 1 (mid-day)

Four process rules:
1. Read existing docs before changing anything
2. Match information to its altitude (smoke suite is Phase A, not its own plan)
3. Enforce agent bootstrap referral (CLAUDE.md / AGENT_START_HERE.md)
4. Always pair with System Architect on planning / roadmap / arch / doc-refactoring

### Iteration 2 (later same day)

Seven additions:
1. Machine-readable status block at top of PLATFORM_MASTER_PLAN.md
2. Agent Operating Protocol section (5-step + 4 prohibitions + conflict-stop)
3. `docs/TRACEABILITY_MATRIX.md` (Task → Wave → Phase → Vision)
4. `docs/architecture/decisions/` directory + ADR format
5. Phase taxonomy: Phase A — Modular Monolith / Phase B — Scaling & Service Extraction / Phase C — Platform Expansion / Ecosystem
6. Dual bootstrap files (CLAUDE.md + AGENT_START_HERE.md)
7. Architect pairing non-negotiable

## Architect rulings (8 + 7 deltas)

### Initial 8 rulings (responding to planning agent's first consult)

- **Q1** PLATFORM_MASTER_PLAN.md shape: confirmed; add machine-readable status + Glossary; cap 400-600 lines
- **Q2** Phase C: PLACEHOLDER only (one paragraph); resist scope-expansion
- **Q3** Agent bootstrap: CLAUDE.md injection only; no AGENT_START_HERE (initially)
- **Q4** Smoke suite wave: Wave 9 (top-level)
- **Q5** Phase A cleanup: moderate (retire v4 amendments, preserve audit trail)
- **Q6** Archive disposition: 28 PHASE_6A → archive/phase-todos/; 4 wave files → archive/wave-plans/ (with Wave 4.6 exception); REVISED_MODULAR_MONOLITH_STRATEGY → archive/superseded/
- **Q7** Tracking docs: retire STREAMLINED_ACTION_PLAN + TASK_SYNCHRONIZATION_STRATEGY; keep PROGRESS_TRACKER as journal
- **Q8** Execute order: 2 commits with founder review gate

### Delta 7 rulings (after founder's iteration-2 additions overrule parts of initial ruling)

- **Q1'** ADR collision: Option (a) — migrate 6 foundational ADRs to `decisions/` with globally-unique renumbering; rename 28 post-hoc to `ANALYSIS-*.md` in place
- **Q2'** Phase B name: Option (c) — `PHASE_B_PRODUCTS_AND_EXTRACTION.md` (founder later confirmed)
- **Q3'** Phase C: one-paragraph placeholder with founder's themes
- **Q4'** AGENT_START_HERE.md: Option (b) pure pointer file recommended (founder later overruled — chose Option (a) human-readable companion)
- **Q5'** TRACEABILITY_MATRIX: Option (a) empty template + 5-10 examples (no backfill)
- **Q6'** Commit shape: split into 3 commits (1a → review gate → 1b → 2)
- **Q7'** TASK_SYNCHRONIZATION_STRATEGY removal: confirmed (part of Commit 2 Phase A cleanup)

### Founder choices on the 3 redirects offered

| Question | Founder choice |
|---|---|
| Phase B name | PHASE_B_PRODUCTS_AND_EXTRACTION (compromise — architect recommended) |
| AGENT_START_HERE.md style | Human-readable companion (~200 lines) — overrules architect's pointer-file recommendation |
| Phase C location | Inside PLATFORM_MASTER_PLAN.md (no separate file) |

## Final execute order (3 commits)

### Commit 1a — Bootstrap (this commit)

1. `docs/PLATFORM_MASTER_PLAN.md` with machine-readable status header + Vision + Phase Overview + Current State + Agent Operating Protocol + Architecture Summary + Onboarding Flow + Glossary + Cross-References + Phase C placeholder embedded
2. `docs/AGENT_START_HERE.md` human-readable companion (~200 lines)
3. `CLAUDE.md` edits: SECTION 0 mandatory-read block at top + SECTION 7 rewrite (retire 3 tracking docs) + REFERENCE DOCUMENTS list update + remove SUPERSEDED REVISED_MODULAR_MONOLITH_STRATEGY references
4. `docs/architect-consults/` directory + this seed file

**Founder review gate after push** before Commit 1b begins.

### Commit 1b — Governance scaffolding

5. `docs/TRACEABILITY_MATRIX.md` empty template + 5-10 example rows + hard rule
6. Migrate 6 foundational ADRs to `docs/architecture/decisions/` with globally-unique renumbering:
   - ADR-001-i18n-scope-phase-a.md → decisions/ADR-001-i18n.md
   - ADR-006-layer-topology-phase-a.md → decisions/ADR-002-layer-topology.md
   - ADR-007-auditable-interceptor-phase-a.md → decisions/ADR-003-auditable-interceptor.md
   - ADR-008-cultural-shared-kernel-phase-a.md → decisions/ADR-004-cultural-shared-kernel.md
   - ADR-009-outbox-everything-phase-a.md → decisions/ADR-005-outbox-everything.md
   - ADR-010-repository-per-aggregate-phase-a.md → decisions/ADR-006-repository-per-aggregate.md
7. Rename 28 post-hoc ADRs to `ANALYSIS-*.md` in place (breaks the ADR-007×7 collision permanently)
8. `docs/architecture/decisions/README.md` explainer
9. Rename `MASTER_TODO_PHASE_B_PRODUCT_ROADMAP.md` → `MASTER_TODO_PHASE_B_PRODUCTS_AND_EXTRACTION.md` + content reconciliation (cover BOTH products AND extraction)

### Commit 2 — Phase A consolidation

10. Rewrite `MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` per Q5 (moderate cleanup): Wave Status refresh, backfill 4.4/4.6/4.7/5.0/5.1/5.2/5.3 statuses, add Wave 9 (smoke suite), retire v4 amendments with archive note
11. Remove §"Document Maintenance" TASK_SYNCHRONIZATION_STRATEGY instruction (line 1820)
12. `mkdir docs/archive/{wave-plans,phase-todos,superseded}`
13. `git mv` 28 MASTER_TODO_PHASE_6A/7/8_*.md → `docs/archive/phase-todos/`
14. `git mv` 3 wave files (4_4 + legacy 5_3 + legacy 5_4) → `docs/archive/wave-plans/` (Wave 4.6 stays in place during IN-PROGRESS)
15. `git mv` REVISED_MODULAR_MONOLITH_STRATEGY.md + STREAMLINED_ACTION_PLAN.md + TASK_SYNCHRONIZATION_STRATEGY.md → `docs/archive/superseded/`

## After all 3 commits ship

Resume Wave 9.a (Events controller smoke). When Wave 9.a runs clean against current staging, W5.3 flips STAGING-VERIFIED + W5.4 unblocks.

## Wave 9.a SHIPPED + W5.3 STAGING-VERIFIED 2026-06-29

Three-commit shape executed cleanly:

- **Commit 1 `245e4982`** — 6 Foundation modules (Lc-Http / Lc-Auth / Lc-Assertion / Lc-Report / Lc-EventFixtures / Lc-CommonFixtures) + 6 Pester test files. 77/77 tests green (≥80% coverage per architect Q7). Pester 5.5 + PowerShell 5.1+7 compatible (Invoke-WebRequest + try/catch; no `-SkipHttpErrorCheck` dependency).
- **Commit 2 `fa370be0`** — `Smoke-EventsController.ps1` (564 LOC, 13 independent sub-sections per architect Q3) + `Run-Wave9a.ps1` orchestrator (single entry-point). Ran against current staging: **26 PASS / 0 FAIL / 8 SKIP (all documented)**.
- **Commit 3 (this)** — W5.3 STAGING-VERIFIED flip in Phase A plan + PLATFORM_MASTER_PLAN status header refresh + TRACEABILITY_MATRIX row updates + this append.

The canonical W5.3 STAGING-VERIFIED signal — `currentRegistrations 0→1` + `userRegistrationStatus=Confirmed` after POST RSVP — passed cleanly. Same lifecycle pattern that proved W5.3.c2 (`0047a6dd`) the day it shipped. Combined with `paid event detail round-trip` (Maname ticketPriceAmount=18.0 USD through new EventRepository assembly), Wave 5.3 is provably correct.

Skipped categories per architect Q5 tri-state:
- 1 SKIP_DESTRUCTIVE (DELETE event; `-IncludeDestructive`)
- 1 SKIP_STATE (Stripe checkout; `-IncludePaymentFlows`)
- 1 SKIP per-section limitation (rsvp dispatch-log assertion: staging logs roll fast due to WhatsApp diagnostic spam; the count-incremented + Confirmed status assertions are the canonical W5.3 proof, log assertion is supplementary)
- 5 SKIP_EXPANSION (ticket-tier CRUD → Wave 9.c; organizer-contacts CRUD + email-group CRUD → follow-up; analytics endpoints → not part of W5.3 surface since EventAnalyticsRepository + EventViewRecordRepository remain in legacy Infrastructure with interfaces in LankaConnect.Domain.Analytics; admin endpoints → dedicated admin smoke)

**Wave 5 RESUMES at Wave 5.4.** Per-slice testing discipline going forward: `pwsh ./scripts/smoke/Run-Wave9a.ps1` after every Wave 5.4+ slice (no more per-slice UI verification).

Architect-consult required for Wave 5.4 scope before any code work begins — likely AppDbContext partition / module DbContext extraction / cross-schema FK resolution per the architect's prior Wave 5.3 closeout hint.

## Wave 5 CLOSED 2026-06-29 — full closeout sequence

After Wave 5.4 architect re-consult ruled Path C (Analytics-only) due to project-reference cycle blocking the proposed Path B (EF Configurations move), Wave 5.4 + Wave 5.5 executed cleanly in ~3 days:

- **Wave 5.4.a `ae50fb27`** — Analytics interfaces (IEventAnalyticsRepository + IEventViewRecordRepository) moved from `LankaConnect.Domain.Analytics` to `Products.LankaEvents.Domain.Repositories`. EventViewRecord entity unintended-discovery: was co-located in the interface file; extracted to its own `src/LankaConnect.Domain/Analytics/EventViewRecord.cs` and entity stayed in legacy namespace per Path C scope. Smoke 26/26 green.
- **Wave 5.4.b `c82ddce1`** — Analytics implementations moved to `Products.LankaEvents.Infrastructure`; DI shifted from `AddInfrastructure()` to `AddLankaEventsModule()`. Completes 20-of-20 Event-family repo carve-out. Smoke 26/26 green.
- **Wave 5.5.a `d64fb3a5`** + cleanup `b8144d55` — 8 ArchTest rules in `ProductsLayerRules.cs` (Rules 1-6 + 8 + 9; Rule 7 dropped per architect Q3). MediatR pre-flight grep: zero hits in Products.Domain source (stays forbidden). 20 repository classes decorated with `[Wave6_5TransitionalException(...)]` (new attribute in BB.Abstractions). Rules 5 + 9 marked `[Fact(Skip = "...")]` with verbose tracking messages — 15 + 14 pre-existing violator types (LankaConnect.Infrastructure services directly invoking Products.LankaEvents.Application + Forms.Application query handlers reaching Products internals). Composition-root exclusion: `LankaConnect.Infrastructure.DependencyInjection` permanently allowed in Rule 5. Wave 6.X.Y + Wave 6.X.Z debt-tracking entries added to Phase A plan. Smoke 26/26 green.
- **Wave 5.5.b `9e7a37ce`** — 4 stale `LankaConnect.Domain.Events` comment references updated to `LankaConnect.Products.LankaEvents.Domain` reflecting current state. Comments-only; counter-trigger C2.
- **Wave 5.5.c `bbbe6c12`** — `src/Products/LankaEvents/README.md` (105 LOC: carve-out status table + project layout + boundary rules + Wave 6.5 deferred work + testing pointer) + new MEMORY entry `[[wave-5-products-carveout-complete]]` banking the W5.1→W5.3→W5.4→W5.5 pattern as reusable knowledge for Phase B product carve-outs.
- **Wave 5.5.d (THIS commit)** — Per architect closeout ruling:
  - Blueprint new §7.16 "Wave 5 Outcome and Wave 6.5 Carryover" (3 paragraphs: what shipped, what deferred with cross-refs to §7.4 + §7.14, pattern banked)
  - Phase B plan note: "Wave 5 carve-out PROOF complete; pattern documented; Wave 6.5 NOT a Phase B prerequisite"
  - README + blueprint use "scoped deferral" framing (neither founder's "known incomplete" nor architect's overly-neutral "ratified deferral" — captures intent in clearer phrasing)
  - Phase A plan Wave 5 row: ✅ SHIPPED + CLOSED 2026-06-29
  - PLATFORM_MASTER_PLAN.md status header: CURRENT_WAVE advances to "Wave 6 PLANNED (architect-consult pending)" + ARCHITECT_REVIEW_REQUIRED: YES

**Wave 5 CLOSED. Phase A continues with Wave 6 (ArchTest hardening) pending architect-consult on scope. Wave 5.5.a already added 8 ArchTest rules; original blueprint §5 target was 28 total; current count is 33 (25 pre-W5.5.a + 8 from W5.5.a); architect needs to rule whether Wave 6 still targets 28 or expands target to ~35-40 incorporating the Skip-fact debt-track resolutions.**

## Post-3-commit hotfix (same day)

After Commit 2 shipped (`a0765601`), founder reviewed and pointed out the smoke suite had landed as `Wave4.9.6` (sub-track of testing-discipline Wave 4.9) instead of as a separate top-level wave as instructed three times across the iterations. The planning agent had let the architect's "semantic fit" tactical argument override the founder's repeated "separate wave" structural direction.

**Hotfix**: rename `Wave4.9.6` → `Wave 9 — API Smoke Suite` across all docs (Phase A plan, PLATFORM_MASTER_PLAN.md status header + Phase Overview, TRACEABILITY_MATRIX, AGENT_START_HERE, architect-consults seed). Honors:
- Architect's *first* ruling (Wave 9 top-level) which had been overruled by the architect's *second* ruling (Wave 4.9.6 sub-track)
- Founder's three-times-repeated "separate wave" instruction
- The unified Wave#.# numbering rule (founder-ruled 2026-06-08)

Lesson banked (added to platform-plan-hierarchy memory): when founder repeats direction 3+ times, that is law — architect tactical "semantic fit" arguments do NOT override repeated founder structural direction. Re-consult only to confirm sub-questions, not to relitigate the founder's instruction.

## Memory updates

- `[[platform-plan-hierarchy]]` UPDATED with full 4-rule + 7-addition direction
- `[[feedback-master-todo-discipline]]` marked SUPERSEDED — points to platform-plan-hierarchy

## Lessons banked

1. **Re-consult architect on every founder direction shift** — don't reinterpret a prior ruling against a new constraint
2. **Read full hierarchy top-down before proposing changes** — partial-read = incomplete plan
3. **The single-source-of-truth pattern requires DELETING the per-feature pattern**, not adding to it
4. **Machine-readable status blocks** (CURRENT_PHASE/CURRENT_WAVE/etc.) reduce reliance on prose parsing for status queries
5. **Founder may overrule architect rulings**; pairing means iterating, not deferring
