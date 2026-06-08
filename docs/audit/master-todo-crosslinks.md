# Master-TODO Cross-Link Inventory (P2 audit)

**Date**: 2026-06-08
**Purpose**: capture every document that cross-links to `docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` so the fat-task surgery preserves every anchor.

## Documents that cite master TODO sections by name

| Doc | Section anchors cited | Count |
|---|---|---|
| `docs/PROGRESS_TRACKER.md` | `§"Phase A.W2"` (4×), `§"Plan Delta Amendments"` (2×), `§"W1 — Execution Status (2026-05-11)"` (1×) | 7 |
| `docs/STREAMLINED_ACTION_PLAN.md` | `§"Phase A.W2"` (5×), `§"Phase A.W2 — BuildingBlocks + Observability"` (2×), `§ "W1 — Execution Status (2026-05-11)"` (1×) | 8 |
| `docs/TASK_SYNCHRONIZATION_STRATEGY.md` | `§"Phase A.W2"` (4×), `§"Phase A.W2 — BuildingBlocks + Observability"` (3×), `§"Plan Delta Amendments"` (1×), `§ "W1 — Execution Status (2026-05-11)"` (3×) | 11 |
| `docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` | various §3 wave references | n/a (architectural, not anchor-linked) |
| `docs/architecture/MODULE_EXTRACTION_PLAYBOOK.md` | playbook step references | n/a |

## ANCHOR PRESERVATION SET (must remain verbatim in master TODO)

These section heading texts MUST stay byte-identical after surgery. Markdown auto-generates anchors from heading text; changing one character rots the link.

```
## Phase A.W0 — Test Hardening (Week 0, 5 days)
## Phase A.W1 — Hygiene & Foundation (Week 1, 5 days)
## Phase A.W2 — BuildingBlocks + Observability (Week 2, 5 days)
## Phase A.W3 — Module Extraction #1: Notifications (Week 3, 5 days)
## Phase A.W4 — Modules #2 + #3: Communications + Media (Week 4, 5 days)
## Phase A.W5 — Module #4: Forms (Week 5, 5 days)
## Phase A.W6 — Module #5: Payments (Week 6, 5 days)
## Phase A.W7 + W8 — Module #6: Events (2 weeks, 10 days)
## Plan v5 Amendment — Enterprise Wave Plan (founder-approved 2026-06-04) — READ FIRST
## Plan Delta Amendments (re-baselined 2026-05-11) — historical reference (v4)
## Architect Review v3 — Critical Amendments (read before any execution)
```

## Surgery rule

- **Fat-task blocks slot in UNDER each section heading.** Heading text never edits.
- New sections added BETWEEN existing sections, never replacing them.
- Adding a Quick Index near the top of the file is fine — it lives ABOVE the existing v5 amendment section.
- Historical sections (`Plan Delta Amendments re-baselined 2026-05-11`, `Architect Review v3`) get a `<!-- HISTORICAL-FROZEN: 2026-06-08 -->` HTML comment but content stays as-is.
