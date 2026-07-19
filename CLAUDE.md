# Claude Code Configuration - LankaConnect Development

**CRITICAL: This file is read at the start of EVERY conversation and by EVERY spawned agent.**
**ALL rules here are MANDATORY and MUST be followed without exception.**

---

# 🎯 SECTION -1: PLATFORM VISION ANCHOR (30-SECOND ORIENTATION)

**Every agent reads this before touching any code, sprint task, or plan document.**

**YOU ARE JOINING A MODULAR-MONOLITH REFACTOR THAT IS SUBSTANTIALLY DONE — PHASE A CLOSED 2026-07-15 at commit `f3033074`, TAG `phase-a-close`.** A live production platform (**LankaEvents, serving customers today at lankaconnect.app**) has been surgically restructured from a legacy `LankaConnect.{Domain,Application,Infrastructure,API}` layout into the target **5-layer modular-monolith topology** (BuildingBlocks → SharedKernel → Capabilities → Products → Hosts) while continuing to serve traffic. The 2-week bulk-move sprint (2026-07-06 → 2026-07-15) delivered 4 calendar days early: Wave 9 API smoke 291/21/88 at close (72.75%, +109 pass over sprint gate), ArchTest 49/0/9 (all skips Wave 8.5-tracked), zero migration drift across all 7 DbContexts, frontend `web` build green against refactored backend.

**AS OF TODAY (2026-07-19) Phase A STABILIZATION IS CLOSED per Wave 5 verification pass** at head `baa373aa`. The Tech-Lead-led sprint (2026-07-16 → 2026-07-19) closed 10 of 12 Wave 8.5 debt items + all Consult #28 R1-R5 risks. Wave 9 API smoke moved 291/21/88 (72.75%) → 348/6/35 (89.46%, +16.71 pp); ArchTest 49/0/9 → 51/0/10 (0 CI-blocking). Evidence bundle: `docs/sprint/PHASE_A_STABILIZATION_CLOSED.md`. Two carry-forwards awaiting founder ratification: (1) 8.5.c ApiRename (bounded blast radius, no Phase-B block per Consult #27 Q4.b); (2) 8.5.e workflow-restore tail (blocked on IEventRecommendationEngine.cs GAP-1 residual). Phase B FULL kick-off (scaffolding + first-slice + cross-module writes) NOW UNBLOCKED per Consult #27 Q5 gates + Consult #28 Q4 ratification. Wave 3 GAP-2/3/4/5 briefs authored during sprint; execution deferred to Phase A.5 v2 window post founder ratification.

**LankaConnect is a multi-product platform for the global Sri Lankan diaspora.** LankaEvents is live today; six more products land on the same foundation over the next 12-18 months (LankaTemples, LankaBusiness, LankaHomes, LankaMart, LankaSeyla, LankaNivasa). The 5-layer topology exists so the marginal cost of launching the next product approaches zero re-architecture — and so products can be extracted into microservices when scale demands, without a rewrite. "Monolith-first, extract-when-needed" as an actual engineering reality, not a slogan. LankaTemples scaffolding shipped at commit `36d1fce2` (Consult #27 Q5 GREEN) as a live proof-of-concept that scaffolding is unblocked; scaffold is FROZEN per Tech Lead D-02 until founder ratifies first-slice implementation.

**Every commit in this Phase A.5 sprint serves that vision by closing named Wave 8.5 debt items or removing a legacy coupling that blocks a module boundary from being clean.** If a proposed action doesn't do one of those two things, it's out of scope. Wave 8.5.f interceptor wiring on LankaEvents+Identity+Communications isn't "wire an interceptor" — it's closing Consult #28 Risk R1 (LIVE production silent-domain-event-drop on the LankaEvents product DbContext today). Every remaining `_unitOfWork.CommitAsync(ct)` swapped to direct `_dbContext.SaveChangesAsync(ct)` closes a latent Wave 8.5.g write-loss surface (R2). Every promoted `LegacyPromotions/` file split into its domain folder closes Consult #17's transitional-bucket debt.

**Anti-pattern to reject with prejudice**: "just make it compile." A shortcut here (dumping DTOs somewhere expedient, promoting types into `LegacyPromotions/` without architect sign-off, letting a cycle stand because incremental builds pass) re-tangles exactly the boundaries Phase B viability depends on. **The architect gate is not bureaucracy; it is the load-bearing wall of Phase B viability.**

If you can't state (in your own words) what LankaConnect is, that we are actively mid-refactor, what phase we're in, and why the current sub-task serves that refactor — **STOP and read `docs/SESSION_PRIMER.md` (mandatory read order §0 below) before touching anything.**

---

# 🛑 SECTION 0: MANDATORY READ ORDER (BEFORE ANY TASK)

**Founder-mandated 2026-06-29. Non-negotiable. Skipping = work happens against stale context.**

Before taking ANY action — planning, implementation, architecture, refactoring, or documentation work — read in this order:

0. **[docs/SESSION_PRIMER.md](./docs/SESSION_PRIMER.md)** — **FRESH-SESSION FULL-CONTEXT BRIEFING**. ~10-minute prose narrative that grounds you in the ongoing modular-monolith refactor: what LankaConnect is, three-phase arc, months of Wave 1-6.5.e work already shipped, the 17 architect consults shaping decisions, current sprint position, immediate task on-ramp, self-test at the end. **If you cannot answer the self-test's 6 questions, do NOT proceed.**
1. **[docs/PLATFORM_MASTER_PLAN.md](./docs/PLATFORM_MASTER_PLAN.md)** — THE single source of truth for the platform; includes the **Agent Operating Protocol** that every agent must follow
2. **[docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](./docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md)** — authoritative architecture (5-layer model, D1-D10 decisions, ArchTest rules)
3. **[docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md](./docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md)** — current phase work plan (per `CURRENT_PHASE` in PLATFORM_MASTER_PLAN status header)
4. **[docs/AGENT_START_HERE.md](./docs/AGENT_START_HERE.md)** — human-readable onboarding companion (read once, return for FAQ + grep recipes)

**Hard rules**:

- ❌ **Never bypass architecture constraints** — the 5-layer model + D1-D10 decisions are not optional
- ❌ **Never change architecture without ADR + System Architect approval** — new ADRs go in `docs/architecture/decisions/`
- ❌ **Never add TODO items outside the hierarchy** — see TRACEABILITY_MATRIX.md; no orphan tasks
- ✅ **Always escalate planning changes to System Architect** — send focused consult via `SendMessage` to the architect persona
- ✅ **Planning, roadmap, architecture, and major documentation refactoring REQUIRE System Architect pairing** — non-negotiable

If documents conflict: STOP, request architect review. Do not pick a side.

---

# 🛑 SECTION 0.5: WAVE LABEL LEXICON (BINDING — DO NOT RENUMBER)

**Added 2026-07-06 after a session invented "Wave 6.5.g Application-layer compile debt cleanup" that collided with the sprint bible's Wave 6.5.g meaning. Wave labels below are BINDING; if a consult ruling reuses one for a new concept, translate back to a plan-conformant sub-slice label BEFORE writing to docs or commit messages.**

**Two-week bulk-move sprint labels** (`docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md`):

- **Wave 6.5.f** = Day 5 slot B → **LankaEvents handler migration to `IMultiContextUnitOfWork`** (~120 handlers)
- **Wave 6.5.g** = Day 5 slot C → **Payments un-skip** (11 handlers → integration events)
- **Wave 6.5.h** = Day 5 slot D → **Rule 5 un-skip** (14 services + 7 webhook handlers)

**Day 4 slot C sub-slice labels** (Consult #14 PASS B `IApplicationDbContext` teardown; architect called these "6.5.g.0..7" — translate to plan-conformant labels below):

- **4C.a** — preamble ash-sweep (delete dead types + dead-using sweep) ✅ CLOSED 2026-07-07
- **4C.b** — Business/Service/Review orphan cleanup (Consult #12 Option D follow-through) ✅ CLOSED 2026-07-07
- **4C.c** — create `CommunicationsDbContext` + relocate 3 email configs (BLOCKS 4C.f) ✅ CLOSED 2026-07-07
- **4C.d** — LankaEvents 10 DbSets → `LankaEventsDbContext` (Metro+Templates, SignUp, Registration sub-sub-slices) ✅ CLOSED 2026-07-08 at commit `2d46557b` (full production-code zero-error milestone)
- **4C.e** — `User` → `IdentityDbContext` (~50 sites → 6 files after enum) ✅ CLOSED 2026-07-08 as 4C.e.1 (skeleton) + 4C.e.2 (parity test) + 4C.e.3 (caller cutover + empty-Up migration)
- **4C.f** — Communications 3 DbSets → `CommunicationsDbContext` callers ✅ CLOSED 2026-07-08 (no-op — DbSets already excised in 4C.c; verified `grep -rn "context\.EmailMessages"` returns 0)
- **4C.g** — `ReferenceValue` → `AppDbContext` direct (5 sites → 1 caller after enum) ✅ CLOSED 2026-07-08
- **4C.h** — delete `IApplicationDbContext` + ArchTest forbidden-type rule + full `Run-Wave9` smoke — **BLOCKED on Wave 6.5.f** (~40+ `IApplicationDbContext` refs in LankaEvents.Application handlers migrate under Day 5 slot B; ~10 more in Communications.Application / Identity.Application / Media.Infra / LC.Infra / Host DI); closes once ALL injectors are 0.

**Hard rule**: NO new wave labels without ADR + architect approval. If a consult reuses a label for a new concept, translate to the plan-conformant sub-slice label above.

**Sub-slice granularity guardrail (architect ruling 2026-07-09):** 4C.d was over-granulated into `.i–.xiii` sub-sub-slices (13 commits). That is anti-pattern going forward. Cap each 4C.* sub-slice at ONE commit (skeleton + config move + parity test + caller cutover + migration all in a single commit when the scope fits). Sub-sub-slice ID-ing (`.1`, `.2`, `.3`) is permitted only when the sub-slice must physically split across days (architect approves at consult time).

---

# 🛑 SECTION 0.6: RECENT ARCHITECT RULINGS (as of 2026-07-16)

Rulings materially change behavior — refresh these at every session start.

**Consult #12 Option D (2026-07-06)** — `LankaConnect.Domain.Business` aggregate + all consumers deleted. LankaBusiness product will re-surface in Phase B. Any `Business`/`Service`/`Review` reference in new code is a bug.

**Consult #13 Q1 amendment (2026-07-06 — `[SetsRequiredMembers]` scope)** — `[SetsRequiredMembers]` is permitted on `LegacyBaseEntity` (2 ctors) + non-generic `AggregateRoot` (2 ctors) ONLY. Rationale: those are transitional-bridge ctors whose bodies already assign `Id = Guid.NewGuid()`; the attribute annotates existing truth for the C# 11 required-members analyzer. **Explicitly forbidden elsewhere** — do not add to `Entity<T>`, generic `AggregateRoot<T>`, or any application-layer type. Remove with `LegacyBaseEntity` post-Wave-6.5.

**Consult #13 Q2 Money-API operator form (2026-07-06)** — SharedKernel.Money exposes `+`/`*`/`-`/`>`/`<=` operators + `new Money(decimal, Currency)` constructor. Do NOT add legacy method aliases (`Money.Create`, `.Add`, `.Multiply`, `.IsGreaterThan`) to `SharedKernel.Money`. Rewrite callers to operator form. Same applies going forward.

**Consult #14 PASS B (2026-07-06 — `IApplicationDbContext` teardown)** — 44 consumers of `IApplicationDbContext` migrate to their respective module DbContexts per Consult #7 Delta multi-DbContext plan. Sequenced as 4C.a..h (see Section 0.5). New code MUST NOT inject `IApplicationDbContext` — inject the correct module DbContext (`LankaEventsDbContext`, `IdentityDbContext`, `CommunicationsDbContext`, `AppDbContext` for cross-cutting `ReferenceValue`).

**Consult #15 PASS C — Interface + DTO placement rule (permanent, 2026-07-06)** — **Interfaces + their DTO signatures live in `Module.Contracts`, never in `Module.Application`.** Any new interface introducing DTO records goes in Contracts. Existing violations (interface + DTOs in `.Application/Contracts/`) get relocated on any touch. This is the third instance of the same shape biting the project ([[feedback-roslyn-analyzer-recurrence-trigger]]); ArchTest rule follows post-sprint. Rule 5j config-relocation audit MANDATORY in commit message for any such move.

**Consult #16 (2026-07-08 — 4C.e User caller migration pattern)** — Option C mixed pattern for cycle-constrained callers. Cross-boundary READS route through the Contracts surface (`IIdentityQueries` etc.). Owned-writes stay on module DbContexts. Seeders (`UserSeeder`, `DbInitializer` cross-module orchestrators) physically MOVE — `UserSeeder` into `Identity.Infrastructure/Data/Seeders/`, `DbInitializer` into the host (`LankaConnect.API/Data/`) so it can PR every module without cycles. Host DI construction wires up both contexts. Landed at commit `8465d219`.

**Consult #17 (2026-07-09 — Wave 6.5.f cycle-break)** — LankaEvents.Infrastructure → LankaEvents.Application cycle blocked handler migration to `IMultiContextUnitOfWork`. Fix: **Option A — promote Application-declared interfaces + DTOs consumed by Infrastructure into `<Module>.Contracts/LegacyPromotions/`** (temporary bucket per Consult #17 refinement). Shipped as TWO commits per architect Q2 (one commit per module, single-concern per commit for revertability). Constraints:
  - Only INTERFACES + DTO records + static shim helpers move to LegacyPromotions. Implementation classes (repos, services) MUST stay in Infrastructure.
  - `IHostedService` background-service CLASSES stay in Infrastructure (no interface promotion needed).
  - Cycle-break commits ship ZERO runtime change — pure compile-time module-boundary reshape.
  - Post-cycle-break: DROP `<Module>.Infrastructure → <Module>.Application` PR; the reverse `<Module>.Application → <Module>.Infrastructure` PR is added in the FOLLOWING Wave 6.5.f handler-migration commit (mirror Forms' clean direction).
  - **Pre-flip grep evidence MANDATORY**: `grep -rn "using LankaConnect.{ModuleNs}.Application" src/{ModulePath}/Infrastructure/` must return zero. Paste result in commit body.
  - Rule 5j config-relocation audit STILL required in commit body (all moved files listed).
  - LankaEvents landed at commit `8c912ca1` (11 files promoted). Communications mirror commit pending.

**Day 10 debt (LegacyPromotions cleanup)** — `<Module>.Contracts/LegacyPromotions/` is a TEMPORARY BUCKET. Day 10 (2026-07-15) legacy-deletion pass splits each LegacyPromotions folder into domain-specific folders (`Contracts/Repositories/`, `Contracts/Services/`, `Contracts/DTOs/`) alongside the legacy csproj deletion. TRACEABILITY_MATRIX.md row pending. Do NOT forget.

**Consult #25 (2026-07-13 Day-7 attack order)** — direct-`_dbContext.SaveChangesAsync(ct)` pattern BLANKET-APPROVED (analog to Consult #19 Ignore blanket) as the go-forward pattern for single-context handlers. `_unitOfWork.CommitAsync(ct)` remains valid for AppDbContext-anchored handlers only. Un-skips of Payments (Wave 6.5.g) + Rule 5 (Wave 6.5.h) landed same-slot. Rule 5b consult-artifact obligation for direct-SaveChanges migrations is satisfied by this doc reference.

**Consult #26 (2026-07-14 Day-10 scope freeze)** — Sprint downscope ratified: 5 legacy csproj deletions reduced to 2/5 delivered (Domain + Shared physically gone; MetroAreaMappingProfile relocated); LankaConnect.Application (Dashboard cross-module query pair + Identity interface pair) + LankaConnect.Infrastructure + LankaConnect.API rename all deferred to Wave 8.5.a-refined / 8.5.b / 8.5.c. **Q3 Option i** ordered the JSONB Currency-object data normalization pass (nested `{"code":"USD",...}` → `"USD"` scalar) to clear Wave 8.5.j Events-list-endpoint failures — landed at commits `31e2ac41` + `ff02b13b` with residual money-flow-test cluster indicating a second root cause (see Consult #28 R3).

**Consult #27 (2026-07-14 Phase A close-out ratification)** — Canonical Phase-A definition-of-done authored (Q4): (1) solution builds against LankaConnect.API entry point with 0 errors; (2) Wave 9 API smoke ≥ pre-sprint baseline (182 pass); (3) ArchTest zero CI-blocking failures (skips permitted with Wave-tracked debt reference); (4) zero staging migration drift across all 7 module DbContexts; (5) frontend `web` builds against refactored backend. **All 5 conditions MET at head `f3033074`** (Wave 9 291/21/88, ArchTest 49/0/9, migration drift zero, `deploy-ui-staging.yml` run `29384577093` SUCCESS). Q5 Phase-B readiness = **NUANCED green** — scaffolding immediately unblocked, LankaTemples scaffold shipped at `36d1fce2`; cross-module write handlers gated on Wave 8.5.f + 8.5.h landing. Phase A closed 2026-07-15 with tag `phase-a-close`.

**Consult #28 (2026-07-16 Phase A completion review)** — Founder's 4 binding questions ruled: **Q1 SUBSTANTIALLY-DONE-WITH-DEBT**, **Q2 STABLE-WITH-KNOWN-RISK** (LankaEvents), **Q3 ADEQUATE-WITH-GAPS-NAMED** (test suite; 19.5% SKIPs inflates green rate — Wave 8.5-tracked SKIP audit owed), **Q4 GO-WITH-CONDITIONS** (Phase B). Named 5 risks R1-R5: R1 = Wave 8.5.f half-wire on LIVE LankaEventsDbContext dispatch (must-fix this week), R2 = ~90 unmigrated LankaEvents handlers latent write-loss surface, R3 = 5 money-flow-test residuals indicate second-root-cause JSON-column trap, R4 = 19.5% SKIP rate inflates green rate, R5 = doc drift (CLAUDE.md §-1 + §0.6 + PLATFORM_MASTER_PLAN header stale — this doc-refresh commit closes R5). Doc-drift also named: Consult #7 Delta §2.4 said "5 DbContexts" but reality on head = 7 — reconciliation in `docs/architecture/DBCONTEXT_OWNERSHIP_MATRIX.md` (authored 2026-07-16).

**Tech Lead 2-day sprint (2026-07-16 → 2026-07-17 EOD)** — Founder mandate 2026-07-16: complete Phase A.5 (all 12 Wave 8.5 items) + Wave 7 (Frontend Mirror) + Wave 8 prep (prod cutover runbooks) in a single 48-hour push under Tech Lead orchestration. 14 parallel agents spec'd in `docs/coordination/EXECUTION_PLAN.md`. Tech Lead-owned decisions logged in `docs/coordination/DECISIONS_LOG.md`; per-agent progress via `docs/coordination/agents/*.md`. Founder escalation triggers = product-scope input, architect-consult-required rule fire, Wave 5 gate fail, prod cutover unknowable risk (all else Tech Lead decides).

**Session handover snapshot (2026-07-19 Wave 5 Stabilization CLOSED)** — head commit `baa373aa` on `develop`. Phase A **STABILIZATION CLOSED** at Wave 5 verification pass: Wave 8.5 debt catalog delivered 10 of 12 items; Consult #28 R1-R5 all CLOSED or de-escalated; Wave 9 API smoke moved 291/21/88 → **348/6/35/389 (89.46% pass)** at Wave 5 fresh run 2026-07-19 (+57 pass, -15 fail, -53 skip, +16.71 pp); ArchTest **51/0/10/61** (+2 pass, 0 CI-blocking); Phase B FULL kick-off (scaffolding + first-slice + cross-module writes) UNBLOCKED. Evidence bundle: **`docs/sprint/PHASE_A_STABILIZATION_CLOSED.md`** (successor to PHASE_A_CLOSED.md). Wave 8.5.f interceptor complete (`dcd6c492`) — LIVE production silent-dispatch drop CLOSED (Consult #28 R1). Wave 8.5.g ~116 handlers direct-SaveChanges migrated in 9 commits `eaea551d`→`c66e1607` (Consult #28 R2 CLOSED H→L). Wave 8.5.h `IMultiContextUnitOfWork.CommitAsync(DbContext[])` retired per Tech Lead D-01. Wave 8.5.a Part 4 `LankaConnect.Application` csproj DELETED at `2f0f257d` (D-12 Option b DTO reshape). Wave 8.5.b Part 5 files relocated (5 commits); 250 legacy migrations permanent-defer under AppDbContext per Consult #26 Q4. Wave 8.5.d LegacyPromotions folder split (Media + Comm). Wave 8.5.i metro-area cross-module writes now go through `IIdentityMetroAreaJunctionRepository`. Wave 8.5.j JSON drift normalized + ADR-007 authored. Wave 8.5.k Businesses controller REMOVED per D-07. GAP-6 layer inversion (Products → SharedKernel.Contact + SharedKernel.Geo) shipped `839fec4a`+`d13e2b0b`+`ff5d4762`+`0eced7b5`. Founder briefing pack shipped: D2 review `eeea7d9d`, D3 readiness `331ca8dc`, D5 risk matrix `9e294b54`, D6 sequencing `8ee55176`. **Carry-forwards from Wave 5**: (1) Wave 8.5.c ApiRename (bounded, no Phase-B block per Consult #27 Q4.b) — queued behind founder R6 ratification; (2) Wave 8.5.e workflow-restore tail — deploy-staging.yml broken since 2026-07-18 due to `IEventRecommendationEngine.cs` refs 5 types deleted by GAP-1 Part A `302af044` (fix: delete unused interface OR reduce to primitive-parameter per D-13 Option A); (3) Wave 3 GAP-2/3/4/5 briefs authored, execution deferred to Phase A.5 v2 window. **Do NOT reopen decisions ratified by Consults #25-28 or Tech Lead D-01 through D-13** unless a new architect consult overrides.

---

# 🛑 SECTION 0.7: SPRINT-DAY DISCIPLINE DELTA (Days 2-6 vs Day 7+)

**Two-week bulk-move sprint (2026-07-06 → 2026-07-19) has a discipline bypass window per `docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md` §"What Breaks During Sprint" + §"Sprint Discipline Reminders".**

**Days 2-6 (2026-07-07 → 2026-07-11) — BYPASSED:**
- Rule 5j.4 handler-audit line: SUSPENDED (do NOT add `T-triggers:` / `S-class:` lines to commit bodies).
- PR-validation CI gate: BYPASSED via admin merge.
- Migration snapshot integrity: DEGRADED (Rule 5c staging-smoke deferred to Day 6 develop-merge).
- Unit tests: RED tolerated.
- develop: RED tolerated (bulk-move/integration is the sprint-active branch).

**Days 2-6 commit-body annotation (REQUIRED substitute)**:
```
Sprint-Day: <N> — <plan slot reference, e.g. "Day 4 slot C sub-slice 4C.e.3">

Discipline bypass: Rule 5j.4 SUSPENDED Days 2-6 per sprint bible.
```

**Rule 5j config-relocation audit** — STILL MANDATORY every commit that relocates a config/interface/DTO file, regardless of sprint day. Include old-path → new-path table + delta with fixes.

**Day 7+ (2026-07-12 onward) — DISCIPLINE BACK ON:**
- Rule 5j.4 T-triggers + S-class lines resume in every commit body.
- Rule 5c staging-smoke resumes as pre-merge gate.
- ArchTest gate resumes.
- PR-validation gate resumes.

**Fail-state trigger:** if `bulk-move/integration` not merged to `develop` by Day 6 EOD (2026-07-11 18:00 UTC) → sprint FAILS, invoke Consult #10 for Consult #8 12-16 wk plan transition.

---

# PART A: LANKACONNECT PROJECT RULES (MANDATORY)

## 🚨 SECTION 1: SENIOR ENGINEER MINDSET (ALWAYS ACTIVE)

**Role:** Think and act as a **Senior Software Engineer** at all times.

### Core Principles:
1. ✅ **Handle issues systematically** - No shortcuts, no quick patches
2. ✅ **Apply durable fixes** - Build for maintainability, not just immediate resolution
3. ✅ **Question everything** - If unsure about design/scope/impact, consult architect or ask user
4. ✅ **Reuse before create** - Search codebase for similar implementations before writing new code
5. ✅ **Never break existing functionality** - Especially UI components (very fragile)

---

## 🚨 SECTION 2: TEST-DRIVEN DEVELOPMENT (TDD) - MANDATORY

**ABSOLUTE REQUIREMENT: Write tests FIRST, then implementation.**

### TDD Process (Red-Green-Refactor):
1. **RED**: Write failing test for new feature
2. **GREEN**: Write minimal code to make test pass
3. **REFACTOR**: Clean up code while keeping tests green

### TDD Rules:
- ✅ **Zero tolerance for compilation errors** - Fix ALL errors before proceeding
- ✅ **Small, testable steps** - Iterate incrementally
- ✅ **90% test coverage minimum** - Measure with `dotnet test /p:CollectCoverage=true`
- ✅ **Tests must pass before commit** - Run `dotnet test` before every git commit

---

## 🚨 SECTION 3: UI/UX BEST PRACTICES (MANDATORY)

**CRITICAL: LankaConnect has established UI patterns. NEVER deviate without user approval.**

### UI Consistency Rules:
1. ✅ **Follow existing component patterns** - Check `/web/src/presentation/components/` before creating new components
2. ✅ **Use design system** - Refer to [UI_STYLE_GUIDE.md](./docs/UI_STYLE_GUIDE.md) for colors, spacing, typography
3. ✅ **Accessibility first** - All inputs must have labels, all interactive elements must be keyboard-accessible
4. ✅ **Mobile-first responsive design** - Test on mobile breakpoints (320px, 768px, 1024px)
5. ✅ **Loading states** - All async operations must show loading indicators
6. ✅ **Error boundaries** - All pages must have error boundaries for graceful failure

### UI Change Protocol:
**Before changing ANY UI component:**
1. Read the component file to understand current behavior
2. Search for ALL usages: `grep -r "ComponentName" web/src/`
3. Test changes in ALL contexts where component is used
4. Add unit tests for new props/behavior
5. Get user approval if changing visual appearance

---

## 🚨 SECTION 4: OBSERVABILITY & ERROR HANDLING (MANDATORY)

**CRITICAL: All code must be traceable and debuggable in production.**

### Logging Requirements:
```csharp
// ✅ CORRECT: Structured logging with context
_logger.LogInformation(
    "Creating order {OrderId} for user {UserId} with {ItemCount} items",
    orderId, userId, items.Count);

try
{
    var order = await _orderRepository.CreateAsync(orderData);
    _logger.LogInformation("Order {OrderId} created successfully", order.Id);
    return order;
}
catch (Exception ex)
{
    _logger.LogError(ex,
        "Failed to create order for user {UserId}. Items: {ItemCount}",
        userId, items.Count);
    throw;
}
```

### Try-Catch Requirements:
- ✅ **Wrap ALL external calls** - Database, HTTP, file I/O must have try-catch
- ✅ **Log before rethrowing** - Always log exception with context before `throw;`
- ✅ **Never swallow exceptions** - Empty catch blocks are FORBIDDEN
- ✅ **Use specific exceptions** - Catch specific types when possible

---

## 🚨 SECTION 5: DATABASE MIGRATIONS (EF CORE - MANDATORY)

**CRITICAL: ALL database changes MUST use EF Core migrations.**

### Deployment topology (founder-ruled 2026-06-07)

There is **no local Postgres / Docker**. Staging is the dev-validation environment
AND the pre-prod gate. Migrations ship by direct push to `develop` (which auto-deploys
to staging via `deploy-staging.yml`). PRs are reserved for production deploys (push
to `main`). The five rules below replace local dry-run.

### The five rules

1. **Idempotent SQL artifact in every migration commit message**. Paste the output
   of `dotnet ef migrations script --idempotent <prev> <new> --context <ContextName>`
   into the commit body. No SQL = commit is not ready to push.

2. **CI lint** fails the build if a migration's `.cs` file contains `DropTable` /
   `DropColumn` / `RenameTable` / `RenameColumn` UNLESS the migration's
   class-level XML doc comment starts with
   `/// SCHEMA-DESTRUCTIVE-APPROVED: <reason>`. Forces a deliberate human review
   beat for every destructive DDL operation.

3. **One concern per migration**. No mixing additive drift (AddColumn) with
   schema rename (RenameTable). If `dotnet ef migrations add` produces a mixed
   migration, **split it into two commits**.

4. **Azure Postgres PITR is the rollback mechanism** (7-day retention, default
   enabled). Restore procedure: `docs/operations/migration-rollback.md`.

5. **Explicit `ToTable("snake_case")` in entity configurations BEFORE generating
   any rename migration**. Without it, EF defaults to PascalCase pluralisation
   of the DbSet property name and silently corrupts table names in the
   generated `RenameTable` call.

### North-star guideline

> **"No schema migration ships to staging unless its idempotent SQL script has
> been read by a human, its destructive DDL has been explicitly labeled with
> `SCHEMA-DESTRUCTIVE-APPROVED`, and a pre-migration schema snapshot exists for
> rollback."**

### Migration Workflow (revised 2026-06-07):

```bash
# 1. Generate migration (ALWAYS name descriptively + use --context for module DbContexts)
dotnet ef migrations add AddMarketplaceProductsTable \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --context AppDbContext

# 2. READ THE GENERATED .cs FILE. Verify Up()/Down() match intent.
#    For destructive DDL, add SCHEMA-DESTRUCTIVE-APPROVED header.
#    For mixed concerns, abort and split into two migrations.

# 3. Generate the idempotent SQL artifact
dotnet ef migrations script --idempotent <prev> AddMarketplaceProductsTable \
    --context AppDbContext > /tmp/migration.sql

# 4. Read /tmp/migration.sql. Verify no PascalCase corruption,
#    no unintended drops, no mixed concerns.

# 5. Commit with the idempotent SQL in the body
git add <migration files only>
git commit -m "Add marketplace products migration

<one-paragraph rationale>

Idempotent SQL:
\`\`\`sql
<paste content of /tmp/migration.sql>
\`\`\`
"

# 6. Push to develop (auto-deploys to staging via deploy-staging.yml)
git push origin develop

# 7. Verify staging deploy + smoke-test the affected endpoint
# 8. If broken, use PITR to roll back per docs/operations/migration-rollback.md
```

### Migration Rules:
- ✅ **Never edit existing migrations** - Create new migration if changes needed
- ✅ **Hand-editing the JUST-generated migration to remove unintended sections IS allowed** — that's the human-review step replacing local dry-run. (Per MEMORY 6A.133, the anti-pattern is hand-CREATING a `.cs` file without an accompanying `.Designer.cs`; surgical edits to EF-generated files are normal.)
- ✅ **Use schema names** - All tables must specify schema: `modelBuilder.ToTable("products", "marketplace");` OR rely on `HasDefaultSchema("marketplace")`
- ✅ **Module DbContexts own their physical schema**. Cross-schema overrides (`ToTable("X", "events")`) are transitional pending Wave 4.9 per-module schema realignment.
- ✅ **Check for conflicts** - Pull latest from develop before creating migration

---

## 🚨 SECTION 6: AZURE STAGING DEPLOYMENT (MANDATORY)

**CRITICAL: LankaConnect deploys to Azure staging after EVERY change.**

### Deployment Workflow:

#### Backend Changes:
```bash
# 1-3: Make changes, write tests, run tests locally
dotnet test

# 4-5: Commit and push
git add . && git commit -m "feat(marketplace): Add product catalog API"
git push origin feature/marketplace-module

# 6: GitHub Actions runs deploy-staging.yml automatically

# 7: Test deployed API
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"12!@qwASzx","rememberMe":true,"ipAddress":"string"}'
```

### Post-Deployment Verification (MANDATORY):
- [ ] API endpoint returns expected response
- [ ] Database migrations applied successfully
- [ ] No errors in container logs
- [ ] Frontend page loads without errors

---

## 🚨 SECTION 7: DOCUMENTATION SYNCHRONIZATION (MANDATORY)

**Revised 2026-06-29 per founder ruling.** Retired the "3 PRIMARY tracking docs" pattern that produced documentation fragmentation. Replaced with the single-source-of-truth hierarchy.

### After EVERY implementation, update TWO documents:

1. **[docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md](./docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md)** — flip wave / sub-slice status (PLANNED → IN-PROGRESS → STAGING-VERIFIED `<date>` → CLOSED `<date>`). For non-Phase-A work, update the relevant phase plan.
2. **[docs/PROGRESS_TRACKER.md](./docs/PROGRESS_TRACKER.md)** — append narrative entry (what was done, why, commits, smoke evidence). PROGRESS_TRACKER is the append-only audit journal.

**Two docs, distinct purposes**:
- Phase plan = current status (what's true now)
- PROGRESS_TRACKER = historical journal (what happened when)

For new TODO items, also add a row to [docs/TRACEABILITY_MATRIX.md](./docs/TRACEABILITY_MATRIX.md) — Task → Sub-slice → Wave → Phase → Vision lineage. No orphan tasks.

`STREAMLINED_ACTION_PLAN.md` and `TASK_SYNCHRONIZATION_STRATEGY.md` are RETIRED — archived under `docs/archive/superseded/` for audit. Do NOT update them.

---

## 🚨 SECTION 8: STATUS REPORTING (MANDATORY - BE HONEST)

**CRITICAL: Never claim success without verification.**

### Status Checklist (Complete ALL before reporting success):
- [ ] Code committed and pushed
- [ ] Tests passing (90%+ coverage)
- [ ] Deployment succeeded
- [ ] Database migrations applied
- [ ] API tested with curl
- [ ] UI tested in browser
- [ ] Logs checked (no errors)
- [ ] Documentation updated

---

## 🚨 SECTION 9: MODULE DEVELOPMENT STANDARDS (MANDATORY)

**CRITICAL: All modules MUST follow Clean Architecture + DDD patterns.**

### Module Structure (Exact Pattern):
```
src/LankaConnect.[ModuleName]/
├── [ModuleName].Domain/        # Aggregates, Entities, ValueObjects
├── [ModuleName].Application/   # Commands, Queries, Handlers
├── [ModuleName].Infrastructure/ # Data, Repositories, Migrations
├── [ModuleName].API/           # Controllers, DTOs, Filters
└── [ModuleName].Tests/         # Domain, Application, Infra, API tests
```

### Module Boundaries (STRICT):
- ✅ **Module can reference:** `LankaConnect.Shared` only
- ❌ **Module CANNOT reference:** Other modules directly
- ❌ **No cross-module database queries**
- ❌ **No shared entities**

---

## 🚨 SECTION 10: UI STYLE GUIDE COMPLIANCE (MANDATORY)

**CRITICAL: Refer to [UI_STYLE_GUIDE.md](./docs/UI_STYLE_GUIDE.md) for ALL UI work.**

Before Building ANY UI Component:
1. Check if similar component exists
2. Read UI_STYLE_GUIDE.md for design tokens
3. Use existing components when possible
4. Get user approval if deviating from style guide

---

## 🚨 SECTION 11: GIT WORKFLOW FOR PARALLEL DEVELOPMENT

### Git Worktree Setup (Prevents Conflicts):
```bash
# Agent 1: Events module
git worktree add ../lc-events feature/events-module

# Agent 2: Marketplace module
git worktree add ../lc-marketplace feature/marketplace-module
```

### Before Merging to Develop:
1. Pull latest develop
2. Rebase feature branch
3. Resolve conflicts (if any)
4. Run ALL tests
5. Push and create PR

---

## 🚨 SECTION 12: PRE-COMPLETION CHECKLIST

**MANDATORY: Complete ALL items before reporting task complete.**

- [ ] Code follows Clean Architecture + DDD
- [ ] Tests written FIRST (TDD), 90%+ coverage
- [ ] All tests passing locally
- [ ] Code committed with descriptive message
- [ ] Deployed to Azure staging successfully
- [ ] API tested / UI tested
- [ ] Azure logs checked (no errors)
- [ ] All 3 PRIMARY docs updated
- [ ] Status report includes verification

---

# PART B: CLAUDE FLOW & SPARC METHODOLOGY

## 🚨 CRITICAL: CONCURRENT EXECUTION & FILE MANAGEMENT

**ABSOLUTE RULES**:
1. ALL operations MUST be concurrent/parallel in a single message
2. **NEVER save working files, text/mds and tests to the root folder**
3. ALWAYS organize files in appropriate subdirectories
4. **USE CLAUDE CODE'S TASK TOOL** for spawning agents concurrently, not just MCP

### ⚡ GOLDEN RULE: "1 MESSAGE = ALL RELATED OPERATIONS"

**MANDATORY PATTERNS:**
- **TodoWrite**: ALWAYS batch ALL todos in ONE call (5-10+ todos minimum)
- **File operations**: ALWAYS batch ALL reads/writes/edits in ONE message
- **Bash commands**: ALWAYS batch ALL terminal operations in ONE message
- **Memory operations**: ALWAYS batch ALL memory store/retrieve in ONE message

### 🎯 CRITICAL: Claude Code Task Tool for Agent Execution

**Claude Code's Task tool is the PRIMARY way to spawn agents:**
```javascript
// ✅ CORRECT: Use Claude Code's Task tool for parallel agent execution
[Single Message]:
  Task("Research agent", "Analyze requirements and patterns...", "researcher")
  Task("Coder agent", "Implement core features...", "coder")
  Task("Tester agent", "Create comprehensive tests...", "tester")
  Task("Reviewer agent", "Review code quality...", "reviewer")
```

**MCP tools are ONLY for coordination setup:**
- `mcp__claude-flow__swarm_init` - Initialize coordination topology
- `mcp__claude-flow__agent_spawn` - Define agent types for coordination
- `mcp__claude-flow__task_orchestrate` - Orchestrate high-level workflows

### 📁 File Organization Rules

**NEVER save to root folder. Use these directories:**
- `/src` - Source code files
- `/tests` - Test files
- `/docs` - Documentation and markdown files
- `/config` - Configuration files
- `/scripts` - Utility scripts

---

## 🚨 CRITICAL: REQUIREMENT DOCUMENTATION PROTOCOL (Phase 6A Prevention System)

**PROBLEM**: Phase 6A revealed requirements discussed in conversation but NEVER documented in PRIMARY tracking docs, causing missed implementations.

**SOLUTION**: Three-part prevention system to ensure requirement gaps are caught early.

### Part 1: Conversation History Review (ALWAYS DO FIRST)

**Before implementing ANY feature**:
1. ✅ Read conversation history looking for undocumented planning
2. ✅ Check if requirements were discussed but never written to tracking docs
3. ✅ Verify all user intent is captured in PRIMARY docs

**Red Flags to Look For**:
- "I discussed this before..." = Requirement in conversation history only
- "We talked about..." = Not documented in PRIMARY docs

### Part 2: Phase Number Management (CRITICAL)

**Before assigning ANY new phase number**:
1. ✅ Check [PHASE_6A_MASTER_INDEX.md](./docs/PHASE_6A_MASTER_INDEX.md) for next available number
2. ✅ Verify number not used in tracking docs
3. ✅ **Record assignment in master index BEFORE implementation starts**

### Part 3: Documentation Synchronization (BEFORE COMPLETION)

**Single Source of Truth** (founder-ruled 2026-06-29):

1. [PLATFORM_MASTER_PLAN.md](./docs/PLATFORM_MASTER_PLAN.md) - Master plan (read first, every time)
2. [MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md](./docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md) - Current phase plan (flip status here)
3. [PROGRESS_TRACKER.md](./docs/PROGRESS_TRACKER.md) - Append-only audit journal
4. [TRACEABILITY_MATRIX.md](./docs/TRACEABILITY_MATRIX.md) - Add row for any new TODO item

---

## Project Overview

This project is an **AI-powered listing application** built using:
- **Clean Architecture**: Domain-centered design with dependency inversion
- **Domain-Driven Design (DDD)**: Rich domain models with aggregates, value objects, domain services
- **Test-Driven Development (TDD)**: Red-Green-Refactor with 90% test coverage
- **SPARC Methodology**: Systematic development workflow with Claude-Flow orchestration

### Architectural Layers
```
src/
├── domain/          # Business logic, entities, value objects
├── application/     # Use cases, application services
├── infrastructure/  # Data access, external services
└── presentation/    # Controllers, UI, API endpoints
```

---

## SPARC Workflow Phases

1. **Specification** - Requirements analysis
2. **Pseudocode** - Algorithm design
3. **Architecture** - System design
4. **Refinement** - TDD implementation
5. **Completion** - Integration

---

## 🚀 Available Agents

### Core Development (Essential)
`coder`, `reviewer`, `tester`, `planner`, `researcher`, `backend-dev`, `system-architect`, `code-analyzer`, `api-docs`, `cicd-engineer`, `perf-analyzer`

### SPARC Methodology
`sparc-coord`, `sparc-coder`, `specification`, `pseudocode`, `architecture`, `refinement`

---

## 🎯 Claude Code vs MCP Tools

### Claude Code Handles ALL EXECUTION:
- **Task tool**: Spawn and run agents concurrently
- File operations (Read, Write, Edit, Glob, Grep)
- Code generation and programming
- Bash commands and system operations
- TodoWrite and task management
- Git operations, testing, debugging

### MCP Tools ONLY COORDINATE:
- Swarm initialization (topology setup)
- Agent type definitions (coordination patterns)
- Task orchestration (high-level planning)
- Memory management, performance tracking

**KEY**: MCP coordinates the strategy, Claude Code's Task tool executes with real agents.

---

## 📋 Agent Coordination Protocol

### Every Agent Spawned via Task Tool MUST:

**1️⃣ BEFORE Work:**
```bash
npx claude-flow@alpha hooks pre-task --description "[task]"
npx claude-flow@alpha hooks session-restore --session-id "swarm-[id]"
```

**2️⃣ DURING Work:**
```bash
npx claude-flow@alpha hooks post-edit --file "[file]" --memory-key "swarm/[agent]/[step]"
npx claude-flow@alpha hooks notify --message "[what was done]"
```

**3️⃣ AFTER Work:**
```bash
npx claude-flow@alpha hooks post-task --task-id "[task]"
npx claude-flow@alpha hooks session-end --export-metrics true
```

---

## 🎯 Concurrent Execution Examples

### ✅ CORRECT WORKFLOW: MCP Coordinates, Claude Code Executes

```javascript
// Single Message - Parallel Agent Execution
[Parallel Execution]:
  Task("Research agent", "Analyze API requirements. Check memory for prior decisions.", "researcher")
  Task("Coder agent", "Implement REST endpoints. Coordinate via hooks.", "coder")
  Task("Database agent", "Design database schema. Store decisions in memory.", "code-analyzer")
  Task("Tester agent", "Create comprehensive test suite with 90% coverage.", "tester")
  Task("Reviewer agent", "Review code quality and security. Document findings.", "reviewer")

  // Batch ALL todos in ONE call
  TodoWrite { todos: [
    {id: "1", content: "Research API patterns", status: "in_progress"},
    {id: "2", content: "Design database schema", status: "in_progress"},
    {id: "3", content: "Implement authentication", status: "pending"},
    {id: "4", content: "Build REST endpoints", status: "pending"},
    {id: "5", content: "Write unit tests", status: "pending"},
    {id: "6", content: "Integration tests", status: "pending"}
  ]}

  // Parallel file operations
  Write "app/src/server.ts"
  Write "app/tests/server.test.ts"
  Write "app/docs/API.md"
```

### ❌ WRONG (Multiple Messages):
```javascript
Message 1: Task("agent 1")
Message 2: TodoWrite { todos: [single todo] }
Message 3: Write "file.js"
// This breaks parallel coordination!
```

---

## Performance Benefits

- **84.8% SWE-Bench solve rate**
- **32.3% token reduction**
- **2.8-4.4x speed improvement**
- **27+ neural models**

---

## 🎯 PROJECT-SPECIFIC INFORMATION

### Tech Stack:
- **Backend**: .NET 8, C#, Clean Architecture, DDD, EF Core 8, PostgreSQL
- **Frontend**: Next.js 16, React 19, TypeScript, Zustand, TailwindCSS
- **Database**: PostgreSQL with schema separation (events, marketplace, business, forum)
- **Deployment**: Azure Container Apps (staging + production)
- **CI/CD**: GitHub Actions (deploy-staging.yml, deploy-ui-staging.yml)

### Azure Staging URLs:
- **Backend API**: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`
- **Test Credentials**: Email: `niroshhh@gmail.com`, Password: `12!@qwASzx`

---

## 📚 REFERENCE DOCUMENTS

**Mandatory read order is defined in SECTION 0 at the top of this file.** This section lists supporting references.

1. **[PLATFORM_MASTER_PLAN.md](./docs/PLATFORM_MASTER_PLAN.md)** - Master plan (SECTION 0 mandates reading this first)
2. **[ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](./docs/architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md)** - Authoritative architecture
3. **[MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md](./docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md)** - Current phase plan
4. **[AGENT_START_HERE.md](./docs/AGENT_START_HERE.md)** - Human-readable onboarding + FAQ + grep recipes
5. **[UI_STYLE_GUIDE.md](./docs/UI_STYLE_GUIDE.md)** - UI consistency rules
6. **[PROGRESS_TRACKER.md](./docs/PROGRESS_TRACKER.md)** - Append-only audit journal

`REVISED_MODULAR_MONOLITH_STRATEGY.md` is SUPERSEDED by ENTERPRISE_ARCHITECTURE_BLUEPRINT.md (replaced 2026-06-04; archived under `docs/archive/superseded/` in Commit 2).

---

## ❌ COMMON MISTAKES TO AVOID

1. ❌ **Skipping tests** - Tests are MANDATORY
2. ❌ **Committing without testing** - Always run tests first
3. ❌ **Breaking existing UI** - Test ALL usages
4. ❌ **Empty try-catch blocks** - Always log exceptions
5. ❌ **Cross-module dependencies** - Modules must be independent
6. ❌ **Skipping deployment verification** - Always test deployed API
7. ❌ **Claiming success without proof** - Verify EVERY checklist item

---

## ✅ SUCCESS CRITERIA

A task is ONLY complete when:
- ✅ Code follows all patterns defined in this file
- ✅ Tests written first (TDD) and passing (90%+ coverage)
- ✅ Deployed to Azure staging successfully
- ✅ Verified working in staging environment
- ✅ All documentation updated
- ✅ Checklist completed with evidence

---

## 🚨 SECTION 13: TESTING DISCIPLINE (MANDATORY — founder mandate 2026-06-07)

**CRITICAL**: "Done" no longer means "compiles + reads green on staging". Done means:
> **the testable unit has a unit test exercising the new behavior AND a staging API call
> that actually invokes that code path in runtime.**

Full ruling: [docs/architecture/TESTING_DISCIPLINE_RULING.md](docs/architecture/TESTING_DISCIPLINE_RULING.md).

### 13.1 — Unit Test Mandatory Triggers (T1 — T8)

Adding/modifying a unit test is MANDATORY in the same commit when any of these fire:

- **T1** New public method on a domain entity / aggregate / value object
- **T2** New or changed mutator touching `IAuditable` / domain events / state transitions
- **T3** New or changed Command / Query handler
- **T4** New or changed EF Core configuration (`ToTable`, `HasColumnName`, `Property`, `Ignore`, `ValueComparer`, `HasConversion`)
- **T5** New or changed REST endpoint signature
- **T6** New or changed DI registration / DbContext / interceptor registration
- **T7** Namespace move (no new test, but existing tests MUST compile + pass; post `dotnet test` evidence in commit message)
- **T8** EF Core migration add — migration-correctness test asserting snapshot delta matches intent

**Counter-triggers** (test NOT required): pure namespace alias, comment changes, .gitignore / .editorconfig, csproj reference move that compiler validates.

### 13.2 — Smoke Coverage Classes (S1 — S6)

After deploy, the commit's claimed smoke executes against staging:

- **S1** Read-only refactor → GET list + GET detail; assert 200 + non-empty
- **S2** Mutator refactor → CREATE → re-fetch → assert `createdAt` ≤60s old + `updatedAt == createdAt`; PATCH → re-fetch → assert `updatedAt > createdAt`
- **S3** EF config / Ignore / mapping change → POST → assert 201 → GET → assert fields present → inspect container logs for `42703` / `22P02` / `NpgsqlException` (any = FAIL)
- **S4** New endpoint → full lifecycle POST → GET → PATCH → DELETE + log inspection
- **S5** Schema migration → `\d <table>` probe pre + post + S2/S3 smoke on affected resource
- **S6** Module-DbContext touch → trigger module write path → probe `\d <schema>.<table>` confirms row written via new context

### 13.3 — Pre-Commit Checklist (enforced by `scripts/hooks/pre-push.ps1`)

```
[ ] 1. dotnet build LankaConnect.sln          → 0 Error(s)
[ ] 2. dotnet test                            → 0 failed
[ ] 3. T-trigger audit — list T-numbers fired + matching test file paths in commit body:
       Example:  T-triggers: T2 (Sponsor mutators), T4 (Sponsor EF config), T6 (DI)
                 Tests: tests/.../SponsorTests.cs, tests/.../SponsorConfigurationTests.cs
[ ] 4. S-class plan — list smoke scripts that will run post-deploy:
       Example:  S-class: S2 (mutator), S3 (log silence)
                 Smokes: scripts/smoke/Smoke-Mutator.ps1 -Resource sponsor -Mode Update
[ ] 5. Architect-consult flag — if T-triggers extend beyond current MASTER_TODO scope:
       STOP. Consult system-architect. Update plan. Then commit.
```

### 13.4 — Pre-Deploy Verification Checklist

```
[ ] 1. deploy-staging.yml: build + all DbContexts apply + container start OK
[ ] 2. scripts/smoke/Invoke-Login.ps1                  → 200 + bearer
[ ] 3. For each S-class in commit message: execute smoke + capture stdout
[ ] 4. scripts/smoke/Smoke-LogSilence.ps1              → no 42703/22P02/NpgsqlException
[ ] 5. For S5/S6: scripts/smoke/Smoke-Probe.ps1        → table/schema as expected
[ ] 6. Status report: deploy URL + smoke output + log silence + probe output
```

### 13.5 — Forcing Functions

1. **`scripts/hooks/pre-push.ps1`** — rejects pushes lacking `T-triggers:` / `S-class:` lines in commit messages. Bypass via `git push --no-verify` (logged to `docs/audit/test-debt-overrides.log`).
2. **PR-validation gate** — `.github/workflows/pr-validation.yml` greps for the same annotations on commits touching `src/`.
3. **Test-debt budget**: max **2 untested commits** in any rolling 24-hour window per branch. Hook hard-blocks the 3rd.
4. **Weekly audit**: `scripts/audit/Test-Debt-Report.ps1` runs Sunday EOD; posts one-paragraph summary to founder.

### 13.6 — Per-Wave MASTER_TODO Discipline (D.2)

Every behavior-touching wave gets `docs/MASTER_TODO_WAVE_<N>.md` with 4 mandatory checkboxes per Phase:

```markdown
- [ ] Migration written + applied + probe-verified
- [ ] Unit tests: T-triggers = ...; tests added in commit X
- [ ] API smoke: S-class = ...; smokes executed = ...
- [ ] Operator UAT: founder confirmed in browser on YYYY-MM-DD HH:MM UTC
- [ ] STAGING-VERIFIED flip at: <UTC>
```

**Status flips to STAGING-VERIFIED only when ALL FOUR boxes are ticked with concrete evidence.**

### 13.7 — Common ❌ Patterns to Avoid

1. ❌ "I'll write the test next commit" — FORBIDDEN. Same-commit or stop.
2. ❌ Read-only smoke for a mutator commit — S1 ≠ S2.
3. ❌ HTTP 200 alone counts as "verified" — only true if S1; mutator commits need re-fetch + assert.
4. ❌ Container logs unread after deploy — log silence is part of every smoke.
5. ❌ Skipping the operator UAT for render-surface changes — per `[[feedback-operator-uat-gate]]`.
6. ❌ Status report missing concrete evidence for one or more checklist boxes — incomplete; do not flip to STAGING-VERIFIED.

---

**Remember: This file is LAW. Follow it without exception. If something is unclear, ASK the user.**

**Last Updated**: 2026-07-09 EOD Sprint Day 4 — SESSION HANDOVER + PLATFORM VISION ANCHOR + FRESH-SESSION FULL-CONTEXT BRIEFING. Founder callout: earlier CLAUDE.md updates gave the shape of the vision but a fresh session STILL didn't grasp that we are actively mid-refactor after months of work. Two fixes: (1) SECTION -1 (Platform Vision Anchor) reworded to lead with "YOU ARE JOINING A MODULAR-MONOLITH REFACTOR IN PROGRESS" + "the refactor has been running for months, we are on Day 5 of a 2-week compressed final-push sprint"; (2) NEW `docs/SESSION_PRIMER.md` (~10-min prose briefing) added as read #0 in the mandatory read order — walks fresh session through the 3-phase arc, months-of-momentum leading to the sprint, 17-consult history shaping current work, sub-slice discipline, discipline-bypass window, immediate task on-ramp, and closes with a 6-question self-test. Companion memory pointer added at top of MEMORY.md. SECTION 0.6 handover snapshot + Day 5 slot A URGENT (sprint bible) unchanged.

**Previous 2026-07-09 update (earlier same day)**: SECTION 0.5 refreshed with 4C.a-g CLOSED status + 4C.h blocker note + sub-slice granularity guardrail. SECTION 0.6 gained Consult #16 (4C.e User caller Option C mixed pattern) + Consult #17 (Wave 6.5.f cycle-break via LegacyPromotions bucket). NEW SECTION 0.7 codifies Days 2-6 discipline bypass window.

**Previous updates**:
- 2026-07-06 — Added SECTION 0.5 (Wave label lexicon binding) + SECTION 0.6 (Consult #12/13/14/15).
- 2026-06-08 — Section 13 added per founder mandate (Testing Discipline Ruling).
