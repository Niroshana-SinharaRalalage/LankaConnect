# Agent Start Here — Onboarding Companion

> **Welcome.** You are joining a multi-agent platform-development effort building LankaConnect, a modular monolith hosting seven Sri Lankan diaspora products. This document is your human-readable orientation. Read it once; then return for FAQ, grep recipes, and common pitfalls whenever you need to refresh.

---

## What this document is for

`PLATFORM_MASTER_PLAN.md` is the formal source of truth. This document is its friendlier sibling — same hierarchy, more prose, more "why" and less "what". Both AI agents and human contributors can use it to bootstrap.

**Read order**: `PLATFORM_MASTER_PLAN.md` first (formal contract), then this file (orientation), then go to your work area.

---

## The 60-second mental model

LankaConnect is being built **wrong on purpose, exactly once**. The codebase started as a single .NET 8 app called `LankaConnect.{Domain,Application,Infrastructure,API}` — the standard Clean Architecture layout. That layout served well for the first product (LankaEvents) but cannot scale to seven products on shared capabilities without paying re-architecture costs every time we add a product.

So we are doing the re-architecture **now**, in a phased way, while LankaEvents stays live and serving customers. The target is a 5-layer enterprise architecture (BuildingBlocks → SharedKernel → Capabilities → Products → Hosts) that lets:

- **LankaTemples** consume `Capabilities/Scheduling` for poya schedules WITHOUT taking `Products/LankaEvents` as a dependency
- **LankaMart** consume `Capabilities/Payments` for Stripe WITHOUT each new product re-implementing its own Stripe integration
- **Any future product** be added by composing existing capabilities + writing its own `Products/<Name>` layer — no re-architecture

This is the "modular monolith first, microservice extraction later" pattern executed properly. Three phases:

- **Phase A** (now): Refactor existing code into the 5-layer architecture; ship `Products/LankaEvents` as the proof point. ~25-35 weeks. We are mid-Phase-A as of 2026-06-29.
- **Phase B** (~Oct-Dec 2026 start): Build 6 new products on the foundation + extract microservices as scale demands. ~40-50 weeks.
- **Phase C** (~mid-2027+): Platform expansion + ecosystem (third-party integration, multi-region scale, public API marketplace). Placeholder; defined later.

---

## Who is on the team

You will see references to several "agents" and "personas". Here's who's who:

- **Founder** — Niroshana, the one human in the loop with full authority. Approves all architectural decisions. Sets phase / wave priorities. Operates under tight context budget (a few minutes per response), so plan accordingly.
- **System Architect** — an AI persona reachable via `SendMessage`. Pairs with the planning agent on every architectural decision, doc refactoring, and major plan change. Founder-mandated as non-negotiable pairing.
- **Planning Agent** — the agent currently doing planning + doc work + day-to-day coordination. Typically Claude Opus 4.7.
- **Implementation Agents** — agents that ship code. May be the same as Planning Agent (most often) or separate spawned `Task` agents for parallel work.
- **Explore Agent** — a read-only research agent useful for "where is X defined" / "which files reference Y" lookups. Cannot edit.

The Founder Pairing Rule (covered in PLATFORM_MASTER_PLAN.md §3.5) is: **always pair with System Architect** on planning / roadmap / architecture / major doc changes. Even when you think you know the answer. Always.

---

## The hierarchy you must respect

```
docs/PLATFORM_MASTER_PLAN.md      ← read first; THE source of truth
├── docs/TRACEABILITY_MATRIX.md   ← Task → Wave → Phase → Vision; no orphan tasks
├── docs/architecture/            ← architecture surface
│   ├── ENTERPRISE_ARCHITECTURE_BLUEPRINT.md
│   ├── decisions/                ← formal ADRs (Decision/Context/Alternatives/Reasoning/Consequences)
│   └── ANALYSIS-*.md             ← historical incident analyses (former ADR-NNN-* files, renamed)
├── docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md   ← active Phase A plan
├── docs/MASTER_TODO_PHASE_B_PRODUCTS_AND_EXTRACTION.md   ← Phase B plan
├── docs/architect-consults/      ← append-only architect ruling log
├── docs/PROGRESS_TRACKER.md      ← append-only audit journal
└── docs/archive/                 ← retired files (don't delete; preserve audit trail)
```

You will NEVER find a `MASTER_TODO_WAVE_<N>_<title>.md` file at `docs/` root in active use. Waves live INSIDE their phase plan. Per-feature `MASTER_TODO_PHASE_6A_<id>_*` files are historical and live in `docs/archive/phase-todos/`. **Do not create new files like these.** If you find yourself wanting to, the answer is "add a Wave section to the phase plan instead."

---

## Reading the machine-readable status header

The top of `PLATFORM_MASTER_PLAN.md` has a grep-friendly status block:

```
CURRENT_PHASE: Phase A
CURRENT_WAVE: Wave 5
CURRENT_WAVE_STATUS: IN-PROGRESS
ACTIVE_SUB_SLICE: Wave 5.3 SHIPPED ...
ARCHITECT_REVIEW_REQUIRED: NO
LAST_UPDATED_BY: ...
LAST_UPDATED: ...
```

This block is the at-a-glance answer for "where are we right now?" — readable by humans and parseable by tools. Useful grep recipes:

| Need | Grep |
|---|---|
| Current phase | `grep "CURRENT_PHASE:" docs/PLATFORM_MASTER_PLAN.md` |
| Is architect review blocking? | `grep "ARCHITECT_REVIEW_REQUIRED:" docs/PLATFORM_MASTER_PLAN.md` |
| When was the plan last updated? | `grep "LAST_UPDATED:" docs/PLATFORM_MASTER_PLAN.md` |
| All STAGING-VERIFIED waves | `grep -n "STAGING-VERIFIED" docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` |
| All IN-PROGRESS waves | `grep -n "IN-PROGRESS" docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md` |

Whenever you flip a wave / sub-slice status, update the header (lines 4-12 of PLATFORM_MASTER_PLAN.md) too. This is part of the discipline.

---

## Common pitfalls (read these — they have cost us hours)

### Pitfall 1 — Following an architect ruling after the founder overruled it

When the founder corrects the direction, **re-consult the System Architect immediately** with the founder's clarified position. Do NOT just execute differently based on your interpretation. Architect rulings get revised; founder overrules are authoritative. The architect needs to know about the overrule.

**Why this is in the FAQ**: this exact pattern wasted ~2 hours on 2026-06-29 when the planning agent (me) tried to implement a hybrid central+per-wave-file pattern based on one architect ruling, then founder overruled, then I started executing the new direction without re-consulting the architect. The architect's *next* ruling was significantly better than my interpretation would have been. Lesson: re-consult on every direction shift.

### Pitfall 2 — Reading partial docs and proposing changes

Before touching ANY planning / architecture / tracking document, **read the full hierarchy top-down**. PLATFORM_MASTER_PLAN → blueprint → active phase plan → relevant Wave section → relevant ADRs. Do NOT propose changes based on a snippet you happened to read. The doc you didn't read probably contains the constraint that would have made your proposal wrong.

### Pitfall 3 — Per-feature MASTER_TODO files

The pattern existed historically (`MASTER_TODO_PHASE_6A_148_REFUND_APPROVAL_WORKFLOW_2026_05_16.md` etc.) and is now retired. Do NOT create new per-feature TODO files. ALL active tracking lives in the phase plans. Closed historical files are archived for audit; do NOT delete them.

### Pitfall 4 — Per-wave MASTER_TODO files

Same anti-pattern, more recent. Even the architect briefly recommended a hybrid central+per-wave-file pattern; founder overruled and was right. Per-wave files create the document fragmentation that makes "single source of truth" impossible. All status, checkboxes, and STAGING-VERIFIED stamps live in the phase plan only.

### Pitfall 5 — Adding new ADRs without checking for collisions

Legacy ADRs at `docs/architecture/ADR-*.md` had number collisions (7 different files all titled `ADR-007-*`). The fix landed in Commit 1b: 6 foundational ADRs migrated to `docs/architecture/decisions/` with globally-unique renumbering; the 28 post-hoc analyses were renamed in place to `ANALYSIS-*.md` to break the collision. Going forward, all NEW ADRs live in `decisions/` with the next available number. Check `ls docs/architecture/decisions/` before assigning.

### Pitfall 6 — Forgetting the smoke matrix

CLAUDE.md §13.2 mandates smoke tests for every commit touching `src/`. The smoke class (S1-S6) gets named in the commit message body. Pre-push hook rejects pushes lacking the annotation. Don't try to skip this — there's a 24-hour test-debt budget and the hook hard-blocks the third unannotated push.

### Pitfall 7 — Architect rulings aren't in PROGRESS_TRACKER

Architect rulings live in `docs/architect-consults/<YYYY-MM-DD>-<topic>.md` as append-only history. PROGRESS_TRACKER is the audit log for shipped work. Do NOT cross-pollinate. If an architect ruling drives a code change, the commit references both the ruling path AND adds a PROGRESS_TRACKER entry.

---

## FAQ

### Q: I want to add a new TODO item. Where does it go?

A: Find the active Wave in the active phase plan. Add a sub-slice under it. Add a row in `TRACEABILITY_MATRIX.md`. That's it. Do NOT create a new file.

### Q: I want to add a new ADR.

A: Author it at `docs/architecture/decisions/ADR-NNN-<slug>.md` using the format in `decisions/README.md`. Use the next available NNN. Pair with System Architect before committing.

### Q: I disagree with the current direction of work.

A: Send a focused consult to the System Architect explaining your concern + alternatives. Do NOT propose to the founder directly — the architect routes ratified concerns up; otherwise the founder gets contradictory inputs.

### Q: I found a documentation conflict.

A: STOP. Send a consult to the System Architect describing the conflict. Do not pick a side. Do not interpret.

### Q: There's an architect-consults entry that contradicts what I'm reading in the phase plan. Which wins?

A: Conflicts of this type are exactly the case for the Agent Operating Protocol's §3.3 (Conflict Resolution): STOP, request architect review. The consult log is authoritative until the phase plan is updated to match.

### Q: Where do I add long-form work history?

A: Append to `docs/PROGRESS_TRACKER.md`. That's the journal. Status updates go in the phase plan; *narrative* history of what was done goes in PROGRESS_TRACKER.

### Q: The founder asked me to do X but it would violate the architecture.

A: Push back via System Architect consult. Frame it as: "Founder asked for X; here's how it conflicts with [blueprint section / ADR / Wave plan]; here are 2-3 alternatives that honor founder intent without the conflict." The architect produces the ruling; you present to founder.

### Q: I'm an AI agent and I notice I might be running out of context.

A: Save what matters to memory IMMEDIATELY via the auto-memory system (Write tool to `~/.claude/projects/<project>/memory/`). Memory persists across sessions. Specifically save: founder corrections, architect rulings, current Wave state, any in-flight gating chain (e.g., "Wave 5.3 is gated on Wave 4.9.6.a").

---

## What to do right now

If you are starting fresh:

1. ✅ Finish reading this document
2. Read `docs/PLATFORM_MASTER_PLAN.md` end-to-end
3. Read `docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` to understand the 5-layer model + D1-D10 decisions
4. Read the active phase plan (per `CURRENT_PHASE` in the status header)
5. Read `../CLAUDE.md` for AI-agent operational rules + testing discipline
6. Skim `docs/architect-consults/` for any open ruling threads

Then start work — but only on a sub-slice that is the active step under `ACTIVE_SUB_SLICE` per the status header. If you can't find your work in the active sub-slice, you're either ahead of the gate or doing something out of plan; consult the architect first.

---

## Closing note

This documentation system exists because **a multi-agent project loses coherence fast**. Without a single forced read order, every agent rediscovers context independently and they drift apart. Without architect pairing, every agent makes architectural decisions in isolation and the architecture becomes the sum of accidents.

Read first. Pair with the architect. Update the central plan. Don't create parallel TODO files. Don't continue when docs conflict.

That's the whole protocol. Welcome.

---

*Maintained by the Planning Agent in pairing with the System Architect. Last updated 2026-06-29. Founder direction: "any new AI agent can join at any time, understand the full state of the platform, and continue work without losing context, focus, or architectural direction."*
