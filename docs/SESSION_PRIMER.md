# Session Primer — Full-Context Briefing for a Fresh Agent

> **YOU ARE JOINING A MODULAR-MONOLITH REFACTOR IN PROGRESS.**
>
> This is not a greenfield project, not a design exercise, not a "let's plan the architecture" conversation. **A live production platform (LankaEvents, serving customers today at lankaconnect.app) is being surgically restructured from a legacy layout into a target 5-layer modular-monolith topology while it continues to serve traffic.** The refactor has been running for months. As of today we are on **Day 5 of a 2-week compressed final-push sprint** — the last 10 working days before Phase A closes and Phase B begins.
>
> Your job is to help finish it correctly, not to design what to build. If a proposed action doesn't move an existing piece of code from a legacy location to its correct modular-monolith location — or doesn't remove a legacy coupling that blocks a module boundary from being clean — it's out of scope. Read this document front-to-back before doing anything else.

---

## 1. What LankaConnect is

LankaConnect is a **multi-product platform for the global Sri Lankan diaspora**, built as a single modular monolith foundation that hosts seven distinct customer-facing products against shared, reusable capabilities. The whole point of the architecture is that adding the *next* product costs near-zero re-architecture, and any product can later be extracted into its own microservice without a rewrite.

**Seven products, one foundation**:

| Product | Purpose | Status |
|---|---|---|
| **LankaEvents** | Community events — organizers list events, attendees register/pay, sponsors + donations flow | **LIVE in production** (lankaconnect.app/lanka-events, serving customers today) |
| **LankaTemples** | Poya calendar + temple event listings, powered by `Capabilities/Scheduling` | Phase B |
| **LankaBusiness** | Sri Lankan business directory + reviews (the aggregate deleted 2026-07-06 per Consult #12 Option D re-surfaces here) | Phase B |
| **LankaHomes** | Housing/real-estate marketplace for diaspora | Phase B |
| **LankaMart** | Marketplace for goods, powered by `Capabilities/Payments` (Stripe integration reused, not reimplemented) | Phase B |
| **LankaSeyla** | Community-service registry (skilled trades, tutoring, help networks) | Phase B |
| **LankaNivasa** | Immigration/settlement resources for new arrivals | Phase B |

Each product lives under `src/Products/<Name>/` and depends only on `SharedKernel/`, `BuildingBlocks/`, and `Capabilities/*` interfaces — never on another `Products/*`. That constraint is what makes the extraction promise real.

The technical mission is one target architecture: **5-layer topology** (BuildingBlocks → SharedKernel → Capabilities → Products → Hosts) enforced by ArchTest rules and D1-D10 architectural decisions in `docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md`.

---

## 2. Three phases (where we are in the arc)

- **Phase A** (in-progress, mid-2026 → mid-July 2026): **The refactor**. Move code out of the legacy `LankaConnect.{Domain,Application,Infrastructure,API}` layout into the target 5-layer topology while LankaEvents stays live in production. Ship `Products/LankaEvents` as the proof point. Originally scoped ~25-35 weeks. Compressed into a 2-week bulk-move sprint (see §4) to finish decisively.
- **Phase B** (~Oct-Dec 2026 → ~mid-2027): **Build the six new products** on the foundation + extract microservices as scale demands. ~40-50 weeks.
- **Phase C** (~mid-2027+): Platform expansion + ecosystem (third-party integration, multi-region scale, public API marketplace).

**We are mid-Phase-A**, executing the compressed completion sprint. LankaEvents is serving real customers right now on the legacy layout — production traffic doesn't pause for our refactor. Every commit is engineered so production is untouched.

---

## 3. Where the refactor stands (the momentum leading into this sprint)

The refactor is NOT a new effort. Since mid-2026 the platform has moved through:

- **Waves 1-4** — foundation: BuildingBlocks + SharedKernel + Modules (Notifications, Media, Forms, Identity, Payments, Communications) carved out with their own Domain/Application/Infrastructure csprojs, own repositories, own EF configurations.
- **Wave 5** — Products carve-out: `Products/LankaEvents/` created + populated with the Event aggregate family (1 aggregate + 30 sub-aggregates + ~458 Application files + 20 repos). Marked complete 2026-06-29.
- **Wave 6.5.a-e** — multi-DbContext machinery: `IMultiContextUnitOfWork.CommitAsync(new DbContext[]{...})` façade, per-module outbox (`AddModuleOutbox<T>()`), `LankaEventsDbContext` extraction. Complete pre-sprint.

The refactor discipline has produced **17 architect consults** (`docs/architect-consults/*.md`) that shape decisions:
- **Consult #7 Delta**: multi-DbContext-per-module retained as target (5 contexts: AppDbContext + Notifications/Media/Forms/LankaEvents; Identity + Payments PERMANENTLY in AppDbContext — later amended by Consult #14).
- **Consult #12 Option D**: `Business/Service/Review` aggregate DELETED (LankaBusiness re-surfaces in Phase B). Any reference in new code is a bug.
- **Consult #13**: `[SetsRequiredMembers]` scope + Money-operator-form ruling.
- **Consult #14 PASS B**: `IApplicationDbContext` teardown as sub-slices 4C.a-h. New code MUST NOT inject `IApplicationDbContext` — inject module DbContexts.
- **Consult #15 PASS C**: Interfaces + DTO signatures live in `Module.Contracts`, never `Module.Application`. Permanent rule.
- **Consult #16** (Sprint Day 4): User caller migration mixed pattern (Contracts surface for reads, physical move for cross-module seeders).
- **Consult #17** (Sprint Day 4): Wave 6.5.f cycle-break via `<Module>.Contracts/LegacyPromotions/` temporary bucket. Two commits per module.

CLAUDE.md SECTION 0.6 summarizes these; ALL are load-bearing on current work.

---

## 4. The 2-week bulk-move sprint (the acceleration)

**Approved 2026-07-04 by founder + architect (Consult #9)**. Bible: `docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md`.

**Sprint window**: Mon 2026-07-06 → Fri 2026-07-19 (10 working days) + buffer.

**Scope IN**:
- Bulk-move all ~1,391 legacy `.cs` files from 5 legacy projects into target `src/Modules/` and `src/Products/` layout.
- Wave 6.5.f handler migration (~120 LankaEvents handlers → `IMultiContextUnitOfWork`).
- Wave 6.5.g Payments un-skip (11 handlers → integration events).
- Wave 6.5.h Rule 5 legacy Infrastructure un-skip (14 services + 7 webhook handlers).
- DELETE 5 legacy `LankaConnect.*` csproj files.
- Wave 9 smoke suite restored to 182/0/79 baseline.

**Scope OUT (deferred to Phase A.5 or Phase B)**: Wave 7 Frontend Mirror (~180h, ~4-6 wk independent track by frontend team), Wave 8 Production Cutover (~150h, founder timeline), Wave 4.9.1 retroactive testing gap-fill (DELETED not deferred).

**Fail-state stop conditions**:
1. Day 1 EOD: hotfix stack not on develop → sprint fails.
2. **Day 6 EOD (2026-07-11)**: `bulk-move/integration` not merged to `develop` → **THE BIG ONE**, sprint FAILS, rolls to Consult #8 12-16 week plan.
3. Day 9 EOD: Wave 9 smoke below 100/261 → sprint fails.
4. Day 11 EOD: staging soak reveals systemic runtime write-loss on LankaEvents → sprint fails.

**Discipline bypass window (Days 2-6)** (per sprint bible §"What Breaks"):
- Rule 5j.4 handler-audit line SUSPENDED (no `T-triggers:` / `S-class:` in commit bodies during this window).
- PR-validation CI gate BYPASSED via admin merge.
- Migration snapshot integrity DEGRADED (Rule 5c staging-smoke deferred to Day 6 develop-merge).
- Unit tests + `develop` RED tolerated.
- **Substitute commit annotation REQUIRED**: `Sprint-Day: <N> — <plan slot>` + `Discipline bypass: Rule 5j.4 SUSPENDED Days 2-6 per sprint bible.`
- **STILL MANDATORY even in bypass**: Rule 5j config-relocation audit (old-path → new-path mapping in commit body for any config/interface/DTO relocation).

**Day 7+ discipline resumes fully** (Rule 5j.4 back ON, staging-smoke required, ArchTest gate resumes).

---

## 5. Current position — Sprint Day 5 (2026-07-10)

**Head commit on `bulk-move/integration`**: `19966c72` (as of Day 4 EOD wrap).

**Day 4 landed** (in order): 4C.e (User → IdentityDbContext, 3 commits), 4C.f (Communications no-op — DbSets already excised in 4C.c), 4C.g (ReferenceValue → AppDbContext direct), Wave 6.5.f LankaEvents cycle-break (`8c912ca1`, 11 files promoted to `LankaEvents.Contracts/LegacyPromotions/`), Wave 6.5.f LankaEvents handler migration (`24b06dc8`, 21 handlers), Wave 6.5.f Communications mirror (`f8ce2ee4`, 2 files + 8 Newsletter handlers), residual injector sweep (`c6f2826b`), 4C.h ATTEMPTED-REVERTED (`5500a82c`, blocker documented), CLAUDE.md refresh + Day 5 slot A URGENT documentation (`c7a18e6d`), SECTION -1 vision anchor (`19966c72`).

**IApplicationDbContext live-injector count**: 84 → 3 (only `IApplicationDbContext.cs` interface + `AppDbContext.cs` impl marker + DI registration in `LegacyInfrastructureDependencyInjection.cs`).

**Day 4 gate**: `dotnet build LankaConnect.sln` incremental = 0 errors. Test-project errors at sprint-tolerance baseline (Domain.Tests 90 + Payments.App.Tests 18 + TestUtilities 14 + Notifications.Domain.Tests 10 + Communications.App.Tests 2 = 134).

**Day 4 gate NOT MET**: `dotnet restore` cold-run fails with MSB4006 circular dependency (see §6).

---

## 6. Day 5 slot A URGENT — the cycle-break

**Discovered during 4C.h attempt**: the Wave 6.5.f cycle-break commits added `<Module>.Application → <Module>.Infrastructure` reverse-direction PRs, but the legacy `LankaConnect.Infrastructure` still holds PRs to `Communications.Application` + `LankaEvents.Application`. Two 3-node cycles form:

```
LC.Infrastructure → LankaEvents.Application → LankaEvents.Infrastructure
  → LC.Infrastructure (transitional AppDbContext + Repository<T> dep)

LC.Infrastructure → Communications.Application → Communications.Infrastructure
  → LC.Infrastructure (same transitional dep)
```

**Why it's urgent**: Day 6 EOD develop-merge triggers a CI cold restore. Cold restore hits this blocker. Sprint fails per Stop Condition #2 unless resolved before Day 6 EOD.

**Fix path (Clean-Arch relocations — NOT "just make it compile")**:
1. Move 4 Email repo IMPLS from `LC.Infrastructure/Data/Repositories/` → `Communications.Infrastructure/Data/Repositories/` (they belong there anyway — module-owned Infrastructure).
2. Move 2 Export services from `LC.Infrastructure/Services/Export/` → `LankaEvents.Infrastructure/Services/Export/` (LankaEvents-owned services in LankaEvents Infrastructure).
3. Drop `LC.Infrastructure → Communications.Application` PR (was for the Email repos; no longer needed).
4. Drop `LC.Infrastructure → LankaEvents.Application` PR (was for the Export services; no longer needed).
5. **Cascade fix**: Export services reference `LankaEvents.Application.Common.*` DTOs (SignUpListDto, EventAttendeesResponse, EventDonationsResponse, etc. — ~9-15 DTOs). Options: promote to `LankaEvents.Contracts/LegacyPromotions/` per Consult #15 PASS C, OR restructure the Export services to not require them, OR move the formatter services into LankaEvents.Application. **ARCHITECT CONSULT REQUIRED** to choose.
6. Re-attempt 4C.h delete `IApplicationDbContext` + add Rule 14 ArchTest (draft was in `5500a82c` — reverted; restore from git).
7. **Verification**: `dotnet restore LankaConnect.sln --force 2>&1 | grep -i circular` MUST return empty before commit.

**After 4C.h closes**, Day 5 slots C + D remain:
- Slot C: Wave 6.5.g Payments un-skip (11 handlers → integration events).
- Slot D: Wave 6.5.h Rule 5 un-skip (14 services + 7 webhook handlers).

Slot B is LIBERATED — Wave 6.5.f LankaEvents handler migration was done Day 4.

---

## 7. Team + rules of engagement

- **Founder**: **Niroshana**. The one human in the loop with full authority. Approves architectural decisions. Sets phase/wave priorities. Operates under tight context budget (a few minutes per response); plan accordingly.
- **System Architect**: an AI persona reachable via `SendMessage` or via the `Agent` tool with `subagent_type: architecture`. Pairs with the planning agent on every architectural decision, doc refactoring, and major plan change. **FOUNDER-MANDATED as non-negotiable pairing.**
- **Planning Agent**: the agent doing planning + doc work + day-to-day coordination. Typically Claude Opus 4.7 (you).
- **Implementation Agents**: agents that ship code. May be the same as Planning Agent or separate spawned `Task` agents.
- **Explore Agent**: read-only research agent for lookups. Cannot edit.

**Founder Pairing Rule** (non-negotiable per PLATFORM_MASTER_PLAN.md §3.5): **always pair with System Architect** on planning / roadmap / architecture / major doc changes. Even when you think you know the answer. Always.

**6 mechanical triggers for architect consult** (memory `[[always-consult-architect-at-arch-decisions]]`): file delete/restore, 2+ options between arch surfaces, plan changes mid-execution, self-hedging phrases in your own reasoning, before escalating A/B/C to founder, error scale >2x plan estimate.

**Sub-slice discipline** (guardrail added 2026-07-09 after 4C.d's `.i-.xiii` fanout anti-pattern): cap each 4C.* / Wave-slice at ONE commit. Sub-sub-slice IDs (`.1`, `.2`, `.3`) are permitted ONLY when the slice must physically split across days AND architect approves at consult time.

---

## 8. Anti-patterns to reject with prejudice

1. **"Just make it compile."** Under sprint pressure especially, the trap opens easily. A shortcut here (dumping DTOs somewhere expedient, promoting types to `LegacyPromotions/` without architect sign-off, letting a cycle stand because incremental builds pass, injecting `AppDbContext` where a module context is correct) re-tangles the boundaries Phase B viability depends on. The architect gate is not bureaucracy; it is the load-bearing wall of Phase B viability.
2. **Inventing new wave labels** (see CLAUDE.md SECTION 0.5). Wave 6.5.f/g/h have BINDING meanings from the sprint bible. If you feel the urge to call something "Wave 6.5.g Application-layer compile debt cleanup", stop — that's exactly the mistake that produced the sub-slice granularity anti-pattern in the first place.
3. **Second-guessing approved plans** (memory `[[dont-second-guess-approved-plans]]`). Once founder + architect approve a plan, execute it. Concerns route through architect re-consult, not through re-litigation.
4. **Bypassing the architect for "small" architectural decisions**. There are no small architectural decisions during Phase A. The 6 mechanical triggers cover this.
5. **Reading only CLAUDE.md and skipping the sprint bible or memory entries**. This document exists because that shortcut is tempting under time pressure. Read the mandatory read order per CLAUDE.md SECTION 0.

---

## 9. Immediate task on-ramp

1. Read CLAUDE.md SECTION -1 (Platform Vision Anchor) + SECTION 0 (mandatory read order) — 2 minutes.
2. Read this file — 10 minutes (you're doing it now).
3. Read `docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md` §"Day 5 slot A URGENT" — 3 minutes.
4. Read the Day 5 slot A memory entries (`[[wave-6-5-f-cycle-block-day5-urgent]]` + `[[sprint-day4-5-handover]]`) — 2 minutes.
5. **Open an architect consult** on the DTO cascade decision from §6 step 5 above. Do NOT touch code until the ruling is in hand.
6. Execute the architect ruling. `dotnet restore` cold-run MUST pass zero cycles before you commit.

---

## 10. Self-test — can you answer these without re-reading?

- What are the 7 LankaConnect products, and which one is live in production today?
- Which phase are we in, and what does the phase after this unlock?
- Why does the cycle-break in Day 5 slot A matter beyond "fix a nuget error"?
- What's the fail-state trigger for the whole sprint, and when does it fire?
- Under Days 2-6 discipline bypass, which rule stays mandatory anyway?
- What's the difference between "consult architect first" and "just make it compile"?

If you can answer all six in your own words, you're grounded. If not, re-read this document before touching code.
