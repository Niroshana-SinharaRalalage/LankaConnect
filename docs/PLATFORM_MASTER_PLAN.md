<!--
================================================================================
MACHINE-READABLE STATUS HEADER (grep-friendly; update with every status flip)
================================================================================
-->
```
CURRENT_PHASE: Phase A.5 (Stabilization CLOSED 2026-07-19)
CURRENT_WAVE: Wave 8.5 (Post-Sprint Debt) — Tech Lead sprint 2026-07-16 → 2026-07-19 CLOSED with 10/12 items delivered + 2 carry-forwards
CURRENT_WAVE_STATUS: STABILIZATION-CLOSED — Head `baa373aa` on `develop`. Wave 8.5: 8.5.a Part 1+4 (`bcf435c6`+`2f0f257d`) + Parts 2+3 (`c2a6e3fc`) + 8.5.b Part 5 (5 relocations landed; migration re-parenting permanent-defer per Consult #26 Q4) + 8.5.d LegacyPromotions split (`ba25bc4e`+`2aed1ded`+`910dc7a9`) + 8.5.f interceptor complete (`dcd6c492`) + 8.5.g ~116 handlers direct-SaveChanges (9 commits `eaea551d`→`c66e1607`) + 8.5.h retire (`2d296aca`+`6b4b4676`) + 8.5.i metro-area contracts (`7e98bf94`+`b6a576d3`) + 8.5.j JSON drift normalization + ADR-007 (`31e2ac41`+`ff02b13b`+`bffbb357`) + 8.5.k Businesses removal (`c9df3599`) + 8.5.l PhotoAlbums verified via 8.5.f+g. Carry-forwards: **8.5.c ApiRename** (bounded, no Phase-B block per Consult #27 Q4.b) + **8.5.e workflow-restore tail** (test-project compile debt Batches 1-3+ shipped, workflow full-solution restore blocked on IEventRecommendationEngine.cs residual). Consult #28 R1-R5 all CLOSED or de-escalated (see PHASE_A_STABILIZATION_CLOSED.md). Wave 9 baseline: 348/6/35/389 (89.46% pass) at Wave 5 fresh run 2026-07-19 (`reports/wave-9-20260719-173313/`; +57 pass / -15 fail / -53 skip / +16.71 pp vs Phase-A-close 291/21/88). ArchTest: 51/0/10/61 (0 CI-blocking, +2 pass vs Phase-A-close 49/0/9).
ACTIVE_WORK: Wave 5 verification pass complete — evidence bundle at `docs/sprint/PHASE_A_STABILIZATION_CLOSED.md`. Post-close residuals: (1) 8.5.c ApiRename queued behind founder ratification; (2) 8.5.e workflow-restore blocked on IEventRecommendationEngine.cs GAP-1 tail (interface refs 5 types deleted by `302af044`) — fix path: delete unused interface OR reduce to primitive parameters per D-13 Option A; (3) Wave 3 GAP-2/3/4/5 briefs authored, execution deferred to Phase A.5 v2 window. Phase B FULL kick-off unblocked pending founder ratification per RISK_MATRIX_2026_07_19.md R6/R7/R8.
ACTIVE_SUB_SLICE: Founder-ratification block (Wave 5 briefing pack D2+D3+D5+D6 + PHASE_A_STABILIZATION_CLOSED.md sign-off) → Phase A.5 v2 window scheduling (GAP-2/3/4/5 + 8.5.c ApiRename + 8.5.e workflow-restore tail).
SEQUENCE_AFTER_PHASE_A_5: Phase B (LankaTemples first-slice UNBLOCKED post founder ratification → LankaBusiness → LankaHomes → LankaMart → LankaSeyla → LankaNivasa) + Wave 7 Frontend Mirror (frontend-team-owned per D-05) + Wave 8 Prod Cutover PREP (runbook authoring per D-06). Cross-module writes NOW UNBLOCKED (Wave 8.5.f + Wave 8.5.h both closed).
PHASE_A_CLOSED_EVIDENCE: See docs/sprint/PHASE_A_CLOSED.md (2026-07-15 f3033074) AND docs/sprint/PHASE_A_STABILIZATION_CLOSED.md (2026-07-19 baa373aa — Wave 8.5 evidence + Consult #28 disposition + Phase B gate reassessment).
CONSULT_28_RISKS: R1 (LIVE dispatch drop) CLOSED at `dcd6c492` | R2 (write-loss surface) CLOSED — ~116 handlers migrated | R3 (money-flow 5 residuals) ISOLATED + ADR-007 locked (`bffbb357`) | R4 (SKIP inflates green rate) CLOSED — 88→50 via SkipAudit `1ffee920` + ResidualFails cascade | R5 (doc drift) CLOSED at `b320c6ce` + this Wave 5 refresh commit.
POST_SPRINT_RISKS_R6_R7_R8: R6 (8.5.c ApiRename ratification) | R7 (extraction-runbook untested — pilot recommended) | R8 (LankaTemples first-slice smoke-discipline reboot). See docs/coordination/RISK_MATRIX_2026_07_19.md — awaits founder decision.
DBCONTEXT_COUNT_CANONICAL: 7 operational DbContexts (AppDbContext + LankaEvents + Identity + Communications + Notifications + Media + Forms) + 1 Phase B scaffold (LankaTemples, frozen per D-02). See docs/architecture/DBCONTEXT_OWNERSHIP_MATRIX.md.
ONGOING_DISCIPLINE: API smoke suite (pwsh ./scripts/smoke/Run-Wave9.ps1) remains per-slice testing mechanism. New floor = 348/6/35/389 (89.46% pass) per Wave 5 fresh run; regression below is hard-STOP.
ARCHITECT_RULING_APPLIED: 2026-07-16 Consult #28 (all R1-R5 disposed per Wave 5). Wave 5 doc reconciliation applies Consult #28 Q6 briefing structure fully (D2 review + D3 readiness + D5 risk matrix + D6 sequencing memo shipped + PHASE_A_STABILIZATION_CLOSED evidence bundle shipped).
LAST_UPDATED_BY: Wave 5 Agent-Verification — doc reconciliation + PHASE_A_STABILIZATION_CLOSED.md authoring
LAST_UPDATED: 2026-07-19
```

# LankaConnect — Platform Master Plan

> **THE single source of truth for the LankaConnect platform.** Every human contributor and every AI agent reads this document first before any work — planning, implementation, architecture, or refactoring. If you are reading this for the first time, stop after this paragraph and complete the **Agent Operating Protocol** below.

---

## 1. Vision

LankaConnect is a multi-product platform for the global Sri Lankan diaspora. The technical mission is a **single modular monolith foundation** that hosts seven distinct customer-facing products against shared, reusable capabilities — so the marginal cost of launching the *next* product approaches zero re-architecture.

The first product is **LankaEvents** (live in production). Six more land on the same foundation across the next 12-18 months: **LankaTemples**, **LankaBusiness**, **LankaHomes**, **LankaMart**, **LankaSeyla**, **LankaNivasa**. As products mature, the foundation enables clean microservice extraction without code rewrites — making "monolith-first, extract-when-needed" an architectural reality, not a marketing slogan.

The platform vision is the contract every wave, every commit, every architectural decision serves. Anything that doesn't trace back to the vision via the hierarchy below is out of scope until the vision is amended (a founder-level decision requiring System Architect pairing).

---

## 2. Document Hierarchy + Source of Truth

```
docs/AGENT_START_HERE.md                                       ← Human-readable onboarding companion
docs/PLATFORM_MASTER_PLAN.md  (THIS DOCUMENT)                  ← THE single source of truth
├── docs/TRACEABILITY_MATRIX.md                                ← Task → Sub-slice → Wave → Phase → Vision map
├── docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md     ← Authoritative architecture
├── docs/architecture/decisions/                               ← Architectural Decision Records (ADRs)
│   ├── README.md
│   ├── ADR-001-i18n.md
│   ├── ADR-002-layer-topology.md  (= legacy ADR-006-layer-topology-phase-a)
│   ├── ADR-003-auditable-interceptor.md  (= legacy ADR-007-auditable-interceptor-phase-a)
│   ├── ADR-004-cultural-shared-kernel.md  (= legacy ADR-008-cultural-shared-kernel-phase-a)
│   ├── ADR-005-outbox-everything.md  (= legacy ADR-009-outbox-everything-phase-a)
│   └── ADR-006-repository-per-aggregate.md  (= legacy ADR-010-repository-per-aggregate-phase-a)
├── docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md               ← Phase A active work plan
├── docs/MASTER_TODO_PHASE_B_PRODUCTS_AND_EXTRACTION.md        ← Phase B future work plan
├── (Phase C plan: see §6 below — placeholder content, embedded here)
└── docs/architect-consults/<YYYY-MM-DD>-<topic>.md            ← Append-only architect ruling log
```

**Status, checkboxes, sub-slice state, and STAGING-VERIFIED stamps live ONLY in the phase-level plan.** Architectural rulings live ONLY in `architect-consults/`. Long-form audit history lives ONLY in `PROGRESS_TRACKER.md`. The hierarchy has no duplication and no per-feature TODO files — those are an anti-pattern and the historical instances are archived under `docs/archive/`.

---

## 3. Agent Operating Protocol (MANDATORY)

**Every agent — AI or human — must complete this protocol before doing any work.** This is non-negotiable.

### 3.1 Pre-Work Checklist (5 steps)

1. **Read `docs/PLATFORM_MASTER_PLAN.md`** (this document, end-to-end)
2. **Read relevant architecture documents** — `ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` + any ADRs in `docs/architecture/decisions/` referenced by the task
3. **Read the active phase work plan** — the file named by `CURRENT_PHASE` in the status header above
4. **Verify current wave status** — find `CURRENT_WAVE` + `ACTIVE_SUB_SLICE` in the phase plan; confirm the work-item exists and is the next valid step
5. **Check architect consultation notes** in `docs/architect-consults/` for any open rulings affecting the work-item

### 3.2 Prohibitions

Agents must **NOT**:

- ❌ **Change architecture without ADR + architect approval** — any deviation from the 5-layer model, the dependency direction, or the D1-D10 design decisions requires a new ADR + System Architect ruling (via `SendMessage` to the architect persona)
- ❌ **Add TODO items outside the hierarchy** — every TODO must be traceable Task → Wave → Phase → Vision via the TRACEABILITY_MATRIX
- ❌ **Reprioritize work without updating the master plan** — re-sequencing waves is a Master Plan edit, which requires System Architect pairing
- ❌ **Continue implementation when documents conflict** — STOP, request architect review via `SendMessage`, do not pick a side

### 3.3 Conflict Resolution

If you discover a documentation conflict (e.g., Phase A plan says X but blueprint implies Y), **STOP immediately**. Do not implement the more-recent change as authoritative. Do not pick the one you agree with. Send a focused consult to the System Architect describing the conflict; wait for the ruling.

### 3.4 Escalation Triggers

You **MUST** escalate to the System Architect (via `SendMessage`) when:

- A planning question has no clear answer in the docs you've read
- You would need to add or rename a Wave, Phase, or Sub-slice
- You would need to add or modify an ADR
- You would need to create a new doc at `docs/` root
- A founder request appears to conflict with an architect ruling or the blueprint
- A code change touches multiple Capabilities/Products and the boundary is unclear

### 3.5 Founder Pairing Rule

For any of the following, **always pair with the System Architect** (non-negotiable):

- Planning changes
- Roadmap refactoring
- Architecture modifications
- Major documentation restructuring

Even when you think you know the right answer. Even when an earlier architect ruling exists — if the founder overrules part of it or the scope shifts, re-consult rather than re-interpret.

---

## 4. Phase Overview

| Phase | Title | Plan File | Status | Estimated Duration |
|---|---|---|---|---|
| **Phase A** | **Modular Monolith Refactor** | [MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md](MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md) | 🟡 **IN-PROGRESS** — Wave 5 mid-execution | 25 weeks calendar / ~35 weeks under testing-discipline overlay |
| **Phase B** | **Products and Service Extraction** | [MASTER_TODO_PHASE_B_PRODUCTS_AND_EXTRACTION.md](MASTER_TODO_PHASE_B_PRODUCTS_AND_EXTRACTION.md) | ⏳ PLANNED — triggers when Phase A Wave 8 completes | 40-50 weeks (6 products + extraction track) |
| **Phase C** | **Platform Expansion / Ecosystem** | (Embedded in this document — see §6) | ⏳ PLACEHOLDER — defined when Phase B is well underway | TBD |

---

## 5. Current State Snapshot (always-current)

> **Update this section with every wave/sub-slice status flip.** This is the at-a-glance answer for "where are we right now?".

**Phase A, Wave 5 (Products carve-out)** is the active work surface as of 2026-06-29.

| Wave | Status | Notes |
|---|---|---|
| Wave 0 — Architecture ratification | ✅ SHIPPED | Blueprint + 5 ADRs + Master TODO update (2026-06-04) |
| Wave 1 — BuildingBlocks + SharedKernel skeleton | ✅ SHIPPED | 8 sub-tasks 1A-1H (2026-06-04) |
| Wave 2 — SharedKernel.Cultural untangling (MVP scope) | ✅ SHIPPED | Sub-tasks 2A-2G (2026-06-05) |
| Wave 3 — 79-entity migration to BB.Entity<TId> | 🟡 IN-FLIGHT | Via Wave 4 capability extractions |
| Wave 4 — Capability extractions | 🟡 PARTIAL | 4.0/4.0b/4.2/4.3/4.5 SHIPPED; 4.4 STAGING-VERIFIED; 4.6 IN-PROGRESS; 4.7 consumer migrations IN-PROGRESS; 4.1 Communications partially shipped (legacy numbering) |
| Wave 4.9 — Testing-discipline overlay | 🟡 PARTIAL | 4.9.0 + 4.9.1 sub-tasks shipping; **4.9.6 PLANNED (per-controller API smoke suite)** |
| Wave 5 — Products carve-out (LankaEvents → `Products/LankaEvents`) | ✅ **SHIPPED + CLOSED 2026-06-30 (FINAL)** | All 16 commits 5.0-5.5.d landed. **20 of 20 Wave 5 repositories (100%) smoke-covered** via Wave 9.a/c/e/f/h.3/h.5/h.7/h.8. Baseline: 182 PASS / 0 FAIL / 79 SKIP / 261 total / 69.73%. NO Wave 5 regressions; 4 pre-existing real platform bugs F16/F17/F18/F20 banked for hardening. |
| Wave 6 — ArchTest hardening (28+ rules) | ⏳ Pending — architect-consult NEXT | Includes 14+14 violators tracked under Wave 6.X.Y/Z (Skip-fact deferrals from W5.5.a) + 4 confirmed REAL platform bugs F16/F17/F18/F20 from Wave 9.h.2 |
| Wave 6.5 — Outbox cutover | ⏳ Pending | LankaEventsDbContext extraction + EF Configurations move + cross-schema FK policy + transitional [Wave6_5TransitionalException] cleanup |
| Wave 7 — Frontend mirror (Turborepo + feature packages) | ⏳ Pending | |
| Wave 8 — Production cutover + stabilization | ⏳ Pending | |
| Wave 9 — API Smoke Suite | ✅ **SHIPPED + CLOSED 2026-06-30 (FINAL)** | 13 sub-waves shipped (9.a-g + 9.h.1/2/3/4/5/7/8); 25 of 42 controllers; 261-test baseline (182 PASS / 0 FAIL / 79 SKIP / 69.73% pass rate). 9.h.6 (auth/comms/webhook surface -- NOT Wave 5 repos) DEFERRED for founder manual UAT. Catalog: `docs/wave-9-findings.md`. |

**Wave 5 + Wave 9 CONDITIONALLY CLOSED 2026-06-30** per architect ruling at Wave 9.h closeout consult. Conditional close = SHIPPED + CLOSED with explicit annotation that Wave 9.h.5 (Stripe test mode + isolated admin -- ~22 endpoints) + Wave 9.h.6 (smoke mailbox + LC_DISABLE_WEBHOOK_SIG_VALIDATION + OAuth issuance + inbox-token probe -- ~10 endpoints) are DEFERRED for founder-manual-UAT per founder ruling 2026-06-30.

**Discipline lesson banked** (per architect Q2 ruling): not all "incomplete" work blocks close. Founder-authorized parallel workstreams (manual UAT) are differentiated from agent-incomplete work (lazy skips). The Wave 5 double-revert pattern was the right call for Wave 9.g lazy-skip closeout; conditional close is the right call here. The distinction is **who owns the remaining work** — if it's founder operationally, close+annotate; if it's the agent technically, keep open.

**Immediate next**: Wave 6 architect consult on scope (ArchTest hardening + 4 confirmed real bugs + 14+14 Skip-fact violators). Per `[[founder-stated-ordering-is-law]]` rule, Wave 5 + Wave 9 close conditionally satisfies the ordering requirement; Wave 6 consult ≠ execution; scope ruling can happen now in parallel with founder's manual UAT on 9.h.5/6.

---

## 6. Phase C Placeholder

**Phase C — Platform Expansion / Ecosystem**

Status: ⏳ PLACEHOLDER. Defined when Phase B is well underway (~mid-2027).

Anticipated themes:

- **Platform Expansion** — third-party integration surface, public API marketplace, partner onboarding programs
- **Ecosystem** — multi-region scale, regulatory expansion beyond Sri Lankan diaspora, third-party developer surface

Specifics deferred to evidence from Phase B execution. Resisting any expansion of this placeholder now — speculative content invites scope-creep edits and risks founder reading the stub as commitment.

When Phase C activation approaches, the System Architect will draft `docs/MASTER_TODO_PHASE_C_PLATFORM_EXPANSION.md` and replace this section with a one-line pointer.

---

## 7. Architecture Summary

The full authoritative architecture lives in [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md). Brief summary for orientation:

### 5-Layer Topology

```
BuildingBlocks → SharedKernel → Capabilities → Products → Hosts
```

| Layer | Purpose | Examples |
|---|---|---|
| **BuildingBlocks** | Framework primitives, ZERO domain knowledge | `Entity<TId>`, `ValueObject`, `IAuditable`, `IUnitOfWork`, base DbContext, base Repository<T> |
| **SharedKernel** | LankaConnect-specific cross-domain primitives, NO behavior | `Money`, `Currency`, `Locale`, `CulturalContext`, `UserId`, `GeoCoordinate`, `IClock` |
| **Capabilities** | Reusable infrastructure modules | `Identity`, `Notifications`, `Communications`, `Media`, `Forms`, `Payments`, `Scheduling`, `CulturalIntelligence` |
| **Products** | Business products composing Capabilities | `LankaEvents`, `LankaTemples`, `LankaBusiness`, `LankaHomes`, `LankaMart`, `LankaSeyla`, `LankaNivasa` |
| **Hosts** | Composition roots | `Host.AllInOne` (current); future split per-Capability for microservice extraction |

### Dependency Direction (non-negotiable, enforced by NetArchTest in Wave 6)

- Each layer references only layers below it
- Capabilities reference each other ONLY via `*.Contracts` assemblies (never `*.Domain`/`*.Application`/`*.Infrastructure`)
- Products reference Capability `*.Contracts` only
- Hosts reference everything (composition is their job)

### Key Design Decisions (D1-D10, founder-approved 2026-06-04)

See blueprint §2 for full detail. Quick reference:

- **D1** IAuditable + interceptor (refinements: IConcurrencyToken + IMultiTenant<T>)
- **D2** Cultural in SharedKernel (54 types, 410 references)
- **D3** Enum partition by audience (per blueprint §2.D3 map)
- **D4** Repository-per-aggregate (kill generic `IRepository<T>`)
- **D5** Outbox-everything (eventual consistency between modules)
- **D6** Money moves `BuildingBlocks` → `SharedKernel.Money`
- **D7** Identity split: `SharedKernel.Identity` (typed IDs) + `Capabilities/Identity` (User aggregate)
- **D8** Frontend mirrors backend layering
- **D9** Delete `ISpecification<T>` pattern
- **D10** `IDomainEvent` stays in-module; cross-module signaling = `IIntegrationEventV1` only

---

## 8. Onboarding Flow (new agent or contributor)

If you are joining the project for the first time:

1. **Read this document** end-to-end (~15 minutes)
2. **Read [AGENT_START_HERE.md](AGENT_START_HERE.md)** for human-readable orientation, common pitfalls, and grep recipes for the machine-readable status (~10 minutes)
3. **Read [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md)** — the architecture is the contract every commit serves (~30 minutes)
4. **Read the active phase plan** — per `CURRENT_PHASE` in the status header. As of 2026-06-29 that's [MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md](MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md) (~45 minutes for an active wave; longer for full read)
5. **Read [CLAUDE.md](../CLAUDE.md)** for the testing discipline, mandatory project rules, and AI-agent-specific operational constraints (~15 minutes)
6. **Skim [docs/architect-consults/](architect-consults/)** for any open ruling threads affecting your work area (~10 minutes per relevant consult)

After this ~2-hour onboarding, you are equipped to take a work item from the current Wave's sub-slice list, locate it in the [TRACEABILITY_MATRIX](TRACEABILITY_MATRIX.md), and begin work under the Agent Operating Protocol.

---

## 9. Glossary

| Term | Definition |
|---|---|
| **Phase** | A multi-month band of work delivering a top-level platform outcome (Phase A = modular monolith refactor; Phase B = 6 new products + extraction; Phase C = platform expansion) |
| **Wave** | A 1-7 week chunk of work inside a Phase, with clear gate-in/gate-out criteria (e.g., Wave 5 = Products carve-out for LankaEvents) |
| **Sub-slice** | A 1-3 day chunk of work inside a Wave (e.g., Wave 5.3.a1 = MetroAreaRepository relocation, single commit) |
| **T-trigger** | A condition (T1-T8) that mandates unit-test coverage for a commit — per CLAUDE.md §13.1. Examples: new public method on aggregate, new EF Core configuration, new REST endpoint. |
| **S-class** | A smoke-test class (S1-S6) that defines what staging-API smoke is required post-deploy — per CLAUDE.md §13.2. S1 = read-only smoke; S2 = mutator smoke; S4 = full lifecycle. |
| **STAGING-VERIFIED** | A wave or sub-slice status indicating: deployed to staging + smoke executed successfully + log silence asserted + (for render-surface) operator UAT confirmed |
| **Capability** | Reusable infrastructure module (e.g., Identity, Payments, Communications) consumed by Products |
| **Product** | Customer-facing business product composing one or more Capabilities (e.g., LankaEvents, LankaTemples) |
| **ADR** | Architecture Decision Record — formal documentation of an architectural choice with Context / Alternatives / Final Reasoning / Consequences. Lives in `docs/architecture/decisions/`. |
| **Architect Consult** | A focused planning/architecture question routed to the System Architect persona via `SendMessage`; ruling logged in `docs/architect-consults/`. |

---

## 10. Cross-Reference Index

| Resource | Location |
|---|---|
| Authoritative architecture | [docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) |
| Phase A work plan | [docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md](MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md) |
| Phase B work plan | [docs/MASTER_TODO_PHASE_B_PRODUCTS_AND_EXTRACTION.md](MASTER_TODO_PHASE_B_PRODUCTS_AND_EXTRACTION.md) |
| Traceability matrix | [docs/TRACEABILITY_MATRIX.md](TRACEABILITY_MATRIX.md) |
| ADRs (canonical) | [docs/architecture/decisions/](architecture/decisions/) |
| Architect consult log | [docs/architect-consults/](architect-consults/) |
| Append-only audit journal | [docs/PROGRESS_TRACKER.md](PROGRESS_TRACKER.md) |
| AI-agent operational rules | [../CLAUDE.md](../CLAUDE.md) |
| Human-readable onboarding | [docs/AGENT_START_HERE.md](AGENT_START_HERE.md) |
| Testing discipline ruling | [docs/architecture/TESTING_DISCIPLINE_RULING.md](architecture/TESTING_DISCIPLINE_RULING.md) |
| Operations runbooks | [docs/operations/](operations/) |
| Archived legacy tracking docs | [docs/archive/](archive/) |

---

## Document Maintenance

This document is updated:

- **On every wave or sub-slice status flip**: refresh the machine-readable status header (lines 4-12) + the Current State Snapshot (§5)
- **On every Phase status change**: refresh the Phase Overview Table (§4)
- **When a new wave is added or renamed**: update both the Phase plan AND the §5 snapshot in this doc
- **When the architecture evolves**: the blueprint changes first, then update §7 here to match
- **When a new ADR is authored**: list it in §2 hierarchy diagram

Every update must be paired with the System Architect (rule 5 of the Agent Operating Protocol). No exceptions.

---

*This document was authored 2026-06-29 by Claude Opus 4.7 (planning agent) in pairing with the System Architect (Opus 4.7) persona, on founder direction (2026-06-29 session) to establish a single durable source of truth for multi-agent platform development.*
