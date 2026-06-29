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
