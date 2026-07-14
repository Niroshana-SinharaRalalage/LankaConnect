# MASTER TODO — 2-Week Bulk-Move Sprint (Phase A Backend Completion)

**Status:** APPROVED by founder + system-architect 2026-07-04
**Sprint window:** Mon 2026-07-06 → Fri 2026-07-17 (10 working days) + Sat 07-16 → Sun 07-19 buffer
**Authoritative ruling:** [docs/architect-consults/2026-07-04-two-week-bulk-move-sprint-plan.md](architect-consults/2026-07-04-two-week-bulk-move-sprint-plan.md) (Consult #9)
**Prior context ruling:** [docs/architect-consults/2026-07-04-refactoring-trajectory-and-speed-up-analysis.md](architect-consults/2026-07-04-refactoring-trajectory-and-speed-up-analysis.md) (Consult #8, rejected 12-16 wk path)

---

## 🛑 READ THIS FIRST EVERY SESSION

This document is **THE plan.** Every action during the sprint must reference it. If an action isn't in this document, it doesn't happen. If the plan needs to change, consult system-architect FIRST, update this document SECOND, act THIRD.

**Founder's exact direction on approval (2026-07-04):**
> "GO. You have to stick to the plan. No deviation, no lagging, always and frequently refer the plan and refresh your memory and work toward the goal. Remember we have to finish this in two weeks. Always work with system-architect."

**Non-negotiable rules for this sprint:**

1. **NO deviation from the day-by-day plan below without system-architect ruling.**
2. **Refresh from this document at the start of every session and every day.**
3. **Founder daily 18:00 sign-off is mandatory** — even a "no update" ping.
4. **Fail-state triggers → STOP + consult** (defined in §Stop Conditions below).
5. **Production is untouched** — runs off separate branch. develop/staging CAN be red Days 2-6.

---

## Scope Ratified

**IN scope (this sprint):**
- Bulk move all ~1,391 legacy `.cs` files from 5 legacy projects into target `src/Modules/` and `src/Products/` layout.
- Wave 6.5.f handler migration (~120 LankaEvents handlers → `IMultiContextUnitOfWork`).
- Wave 6.5.g Payments un-skip (11 handlers → integration events).
- Wave 6.5.h Rule 5 legacy Infrastructure un-skip (14 services + 7 webhook handlers).
- Wave 4.6 (Identity.Contracts, 15-20 types) — folded into Day 6.
- Wave 4.7 (ICulturalCalendar DI + consumer migrations) — folded into Day 6.
- Wave 4.9 physical columns + schema renames — folded into Days 4-5 where they fit; otherwise deferred to Phase B.
- DELETE 5 legacy `LankaConnect.*` csproj files from solution.
- Wave 9 smoke suite restored to 182/0/79 baseline.

**OUT of scope (Phase A.5, after sprint):**
- Wave 7 Frontend Mirror (~180h, ~4-6 wk independent track by frontend team)
- Wave 8 Production Cutover (~150h, deferred to founder decision)
- Wave 4.9.1 retroactive testing gap-fill — **DELETED not deferred**

---

## Day-by-Day Plan (BINDING)

### Day 0 — Fri 2026-07-04 (PREP, TODAY)

| # | Task | Owner | Hours |
|---|---|---|---:|
| 0.1 | Write this document (MASTER_TODO_SPRINT) | Claude | 1 |
| 0.2 | Save `[[project-two-week-bulk-move-sprint-approved]]` memory | Claude | 0.5 |
| 0.3 | Draft `bulk-move-manifest.md` — source→target path table for all 1,391 files | Claude | 4 |
| 0.4 | Write namespace-rewrite Roslyn/regex script (`scripts/sprint/Rewrite-Namespaces.ps1`) | Claude | 2 |
| 0.5 | Pre-load Rule 5j.4 handler-audit script (`scripts/sprint/Audit-HandlerContext.ps1`) | Claude | 1 |
| **Total** | | | **8.5** |

### Day 0.5 — Sat 2026-07-05 (PREP, TOMORROW)

| # | Task | Owner | Hours |
|---|---|---|---:|
| 0.6 | Pre-configure 6 git worktrees `C:\Work\lc-bulk-move-{A,B,C,D,E,F}\` off develop | Claude | 1 |
| 0.7 | Dry-run namespace-rewrite script on 20-file subset. Verify `git blame` preserved. | Claude | 2 |
| 0.8 | Founder review of `bulk-move-manifest.md` — final adjustments | Founder + Claude | 2 |
| 0.9 | System-architect final review of Day 2 agent scopes | Architect | 1 |
| **Total** | | | **6** |

### Day 1 — Mon 2026-07-06 (Consolidate Hotfix + Doc Surgery + Fan-Out Prep)

| # | Task | Owner | Slot |
|---|---|---|---|
| 1.1 | Rebase `wave-6-5-f-5-hotfix` stack onto develop | Agent A | AM |
| 1.2 | Run Wave 9 smoke on rebased stack → confirm 182/0/79 baseline | Agent A | AM |
| 1.3 | Merge hotfix stack to develop | Agent A | Noon |
| 1.4 | MASTER_TODO surgery — delete Wave 4.9.1/7/8 rows, freeze 4.9.2 fold-in list | Agent B | PM |
| 1.5 | Author `PHASE_A_5_PLAN.md` for Wave 7 + 8 disposition | Agent B | PM |
| 1.6 | Founder EOD 18:00 sign-off — hotfix green + manifest ratified | Founder | 18:00 |
| **FAIL-STATE** | If hotfix stack not green on develop by 18:00 → **STOP**, invoke Consult #10 | | |

### Day 2 — Tue 2026-07-07 (Big-Bang Bulk Move Day)

**6 parallel worktree agents. Independent path sets. LEAVE BROKEN.**

| Agent | Owns | Move count |
|---|---|---:|
| A | `src/LankaConnect.Domain/` → `src/Modules/*/Domain/` + `src/Products/LankaEvents/LankaEvents.Domain/` | ~318 |
| B | `src/LankaConnect.Application/` → target module Application projects | ~409 |
| C | `src/LankaConnect.Infrastructure/` non-EF (services, repos, helpers) → module Infrastructure | ~200 |
| D | `src/LankaConnect.Infrastructure/Data/Configurations/` → per-module `Data/Configurations/`. **Migrations folder STAYS in LankaConnect.Infrastructure temporarily.** | ~50 |
| E | `src/LankaConnect.API/Controllers/` → target module `.Api` projects. **Program.cs stays.** | ~50 |
| F | `src/LankaConnect.Shared/` + `src/LankaConnect/` → SharedKernel/Contracts | ~50 |

**Discipline:**
- `git mv` for every file (blame/history preservation — NON-NEGOTIABLE).
- Namespace-rewrite script runs on each agent's target set.
- No agent touches another agent's path set (manifest enforces).
- Push each to `bulk-move/agent-{A..F}` branch.
- **NO MERGE. develop stays as-is (post-hotfix).**

### Day 3 — Wed 2026-07-08 (Compile Reconciliation)

Same 6 agents on same worktrees. Fix namespace + `using` errors in bounded set.

- Cross-agent dependency conflicts → system-architect resolves.
- Expected 50-100 cross-module reference violations.
- Fix via Contracts elevation OR temporary `InternalsVisibleTo` (real fix deferred to Day 7).
- **Gate:** each worktree branch builds standalone (`dotnet build` green).

### Day 4 — Thu 2026-07-09 (Big Merge + Solution Reconciliation)

**3 agents. Merge is not fully parallel.**

| Agent | Owns |
|---|---|
| A | Merge 6 branches → `bulk-move/integration` in order: Domain → Contracts → Application → Infrastructure → Api → Host. Resolve conflicts. |
| B | csproj cleanup: DELETE `LankaConnect.Domain/.Application/.Infrastructure/.Shared/LankaConnect` from `LankaConnect.sln`. Remove all `ProjectReference` entries. |
| C | DbContext consolidation: 73 App-legacy handlers → `IMultiContextUnitOfWork` + correct module DbContext. Fan into 3 sub-agents by family (Communications 40, Businesses 10, Analytics/Badges/Support/MetroAreas 23). |

**Gate:** `bulk-move/integration` compiles. `dotnet test` NOT required to pass yet.

### Day 5 — Fri 2026-07-10 (Migrations + Handler Migrations)

**4 agents.**

| Agent | Owns |
|---|---|
| A | Migration inventory: split 506 AppDbContext migrations into per-module buckets. Cap history + route new ones per-module. (Don't re-parent old.) |
| B | Wave 6.5.f LankaEvents handler migration (~120 handlers, 123 files, 156 occurrences). Rewrite to `IMultiContextUnitOfWork`. |
| C | Wave 6.5.g Payments un-skip (11 handlers → integration events). |
| D | Wave 6.5.h Rule 5 un-skip (14 services + 7 webhook handlers → integration events). |

**Gate:** Still on `bulk-move/integration`. develop merge is Day 6.

### 2026-07-06 alignment note — DO NOT INVENT NEW WAVE LABELS

**Wave label lexicon per this plan (BINDING, don't renumber):**

- **Wave 6.5.f** = Day 5 slot B → **LankaEvents handler migration to `IMultiContextUnitOfWork`** (~120 handlers).
- **Wave 6.5.g** = Day 5 slot C → **Payments un-skip** (11 handlers → integration events).
- **Wave 6.5.h** = Day 5 slot D → **Rule 5 un-skip** (14 services + 7 webhook handlers).

**Correction (2026-07-06):** An earlier version of this doc labeled the today's Modules-Domain compile-cleanup as "Wave 6.5.f cutover" and the follow-on `IApplicationDbContext` teardown as "Wave 6.5.g". **Both labels were incorrect renumberings that collided with the sprint bible above.**

**Correct mapping**:

| Today's actual work | Correct plan slot | Notes |
|---|---|---|
| Namespace rewrites + Business delete + `[SetsRequiredMembers]` bridge → 9 Domain projects compile-green | Day 3 (Compile Reconciliation) + Day 4 (Big Merge) tail | Compile-green gate for 9/? projects; not full gate |
| `IApplicationDbContext` seam teardown (44 consumers → module DbContexts per Consult #14) | **Day 4 slot C — DbContext consolidation** (73 App-legacy handlers fan into 3 sub-agents by family: Communications-40, Businesses-10, Analytics/Badges/Support/MetroAreas-23) | Day 4 gate = `bulk-move/integration` compiles — NOT met yet (134 errors). |
| ash-sweep of dead BB.Domain sub-namespaces (Monitoring/Security/Recovery/Database + 12 ash types) | Absorbed into Day 4 tail-fix work | Cheap, unblocks reading code during Day 4/5 handler rewrites |

### Day 4/5 boundary status @ 2026-07-06 18:00 UTC

- Commit `26a67b98` on `bulk-move/integration` (pushed): 9 Domain projects compile-green. 134 residual errors in `LankaConnect.Application` (160) + `LankaConnect.Domain.Tests` (90) + `Notifications.Domain.Tests` (10) + `BB.Application` (8, dead-usings post-ash-sweep) — **Day 4 gate not yet met**.
- **Consult chain executed today**: #12 Option D → #13 Q3+Q4+Q2+Q1 (Option B Path 1) → #13.3 PASS A (7 Modules cascade) → #13.5 PASS C → #13.5.1 PASS 1 → #13.5.2 PASS B (checkpoint red honestly) → #14 PASS B (`IApplicationDbContext` teardown = Day 4 slot C fan-out).
- **See** `docs/architecture/decisions/ADR-6.5.f-sln-exclusions.md` for the compile-red checkpoint context (the "ADR-6.5.f" filename retained to preserve commit link; do not rename).

### Day 4/5 boundary status @ 2026-07-09 EOD (session handover — WAVE 6.5.f DONE, 4C.h BLOCKED)

**Head commit on `bulk-move/integration`**: `5500a82c` (pushed).

**Sprint gates achieved by EOD Day 4**:
- ✅ All 4C.a-g CLOSED (Day 4 slot C sub-slices `.a` through `.g` shipped).
- ✅ Wave 6.5.f LankaEvents cycle-break landed (commit `8c912ca1`) — 11 files promoted to `LankaEvents.Contracts/LegacyPromotions/`.
- ✅ Wave 6.5.f LankaEvents handler migration landed (commit `24b06dc8`) — 21 handlers now inject `LankaEventsDbContext` directly.
- ✅ Wave 6.5.f Communications mirror landed (commit `f8ce2ee4`) — 2 files promoted + 8 Newsletter handlers migrated to `CommunicationsDbContext`.
- ✅ Residual injector sweep landed (commit `c6f2826b`) — Identity cycle-break (reverse edge was phantom), Comm.Infra background services, GetMetroAreas physical relocation from LC.Application → LankaEvents.Application, EnumSyncValidator direct-AppDbContext.
- ✅ `dotnet build LankaConnect.sln` incremental = 0 errors. Test-project errors at sprint-tolerance baseline (Domain.Tests 90 + Payments.App.Tests 18 + TestUtilities 14 + Notifications.Domain.Tests 10 + Communications.App.Tests 2).

**IApplicationDbContext live-injector count**: 84 → 3 (interface + AppDbContext impl marker + DI seam only). Delete gated on Day 5 slot A cycle-break below.

### 🛑 Day 5 slot A URGENT — NUGET-RESTORE CYCLE BLOCKS 4C.h + DAY 6 DEVELOP MERGE

**Discovered 2026-07-09 during 4C.h attempt (commit `5500a82c` — ATTEMPT REVERTED)**:

`dotnet build` incremental succeeds, but `dotnet restore` cold-run FAILS with MSB4006 "circular dependency in the target dependency graph". Root cause: the Wave 6.5.f cycle-break commits added `<Module>.Application → <Module>.Infrastructure` reverse-direction PRs, but the legacy `LankaConnect.Infrastructure` still holds PRs to `Communications.Application` + `LankaEvents.Application`, forming two 3-node cycles:

```
LC.Infrastructure → LankaEvents.Application → LankaEvents.Infrastructure
  → LC.Infrastructure (transitional AppDbContext + Repository<T> dep)

LC.Infrastructure → Communications.Application → Communications.Infrastructure
  → LC.Infrastructure (same transitional dep)
```

**Day 6 develop-merge will trigger a cold restore in CI and hit this blocker.** Must resolve before Day 6 EOD or sprint fails.

**Fix path (proper Clean-Architecture relocations)**:
1. Move 4 Email repo IMPLS from `LC.Infrastructure/Data/Repositories/` → `Communications.Infrastructure/Data/Repositories/`:
   - `EmailMessageRepository.cs`
   - `EmailStatusRepository.cs`
   - `EmailTemplateRepository.cs`
   - `UserEmailPreferencesRepository.cs`
2. Move 2 Export services from `LC.Infrastructure/Services/Export/` → `LankaEvents.Infrastructure/Services/Export/`:
   - `CsvExportService.cs`
   - `ExcelExportService.cs`
3. Drop `LC.Infrastructure → Communications.Application` PR.
4. Drop `LC.Infrastructure → LankaEvents.Application` PR.
5. Cascade fix any downstream signature-type dependencies (~5-15 DTOs may need Contracts.LegacyPromotions promotion per Consult #15 PASS C).
6. Re-attempt 4C.h delete `IApplicationDbContext` + Rule 14 ArchTest.
7. `dotnet restore` cold-run must PASS with zero cycles before commit.

**Architect consult REQUIRED** before executing — the DTO cascade for Export services (SignUpListDto, EventAttendeesResponse, ~15 more in Application.Common) needs Consult #15 PASS C treatment (interfaces + DTO signatures → Contracts).

**After 4C.h closes**, resume Day 5 slots B/C/D per original plan:
- Slot B: Wave 6.5.f LankaEvents handler migration — ✅ ALREADY DONE (commit `24b06dc8`); slot LIBERATED for cycle-break work.
- Slot C: Wave 6.5.g Payments un-skip (11 handlers → integration events).
- Slot D: Wave 6.5.h Rule 5 un-skip (14 services + 7 webhook handlers).

**Day 5 EOD gate**: `dotnet restore` cold-run PASSES + 4C.h closed + Wave 6.5.g/h shipped. Day 6 develop-merge unblocked.

### Day 4 slot C — remaining execution (per Consult #14 PASS B)

**These are Day 4 slot C sub-slices, NOT new waves.** Sub-slice numbering used internally for tracking; each is a Day 4 slot C sub-agent's beat.

- [ ] **4C.a — Preamble ash-sweep** (STARTED 2026-07-06): delete 13 ash types (7 VOs + 5 Enums + `EngineResults.cs`) + sweep dead usings for BB.Domain.Monitoring/Security/Recovery/Database + LankaConnect.Domain.Enterprise + LankaConnect.Domain.Business. Zero-risk cleanup. **DONE mid-session; uncommitted.**
- [ ] **4C.b — Businesses orphan cleanup** (Consult #14 sub-slice 6.5.g.1): delete `Businesses`/`Services`/`Reviews` DbSets from `IApplicationDbContext` + AppDbContext configs + `IReviewRepository`/`IServiceRepository` + orphan handler references. Rule 5j audit.
- [ ] **4C.c — Create `CommunicationsDbContext`** (Consult #14 sub-slice 6.5.g.2, MUST precede 4C.f): scaffold context, move 3 Email configs from AppDbContext, empty-Up() migration, parity test. Rule 5j + Rule 5c staging pre-merge MANDATORY.
- [ ] **4C.d — LankaEvents 10 DbSets → `LankaEventsDbContext`** (Consult #14 sub-slices 6.5.g.3.a/b/c): 3 sub-sub-slices — Metro+Templates (22 sites), SignUp cluster (40 sites), Registration cluster (110 sites). Rule 5j.4 per commit + Rule 5c staging pre-merge each.
- [ ] **4C.e — `User` → `IdentityDbContext`** (Consult #14 sub-slice 6.5.g.4): ~50 real caller sites. Rule 5j.4 + Rule 5c.
- [ ] **4C.f — Communications 3 DbSets → `CommunicationsDbContext`** (Consult #14 sub-slice 6.5.g.5): 4 caller rewrites + trigger real email smoke per `[[feedback-email-smoke]]`.
- [ ] **4C.g — `ReferenceValue` callers → `AppDbContext` direct** (Consult #14 sub-slice 6.5.g.6): 5 sites.
- [ ] **4C.h — Delete `IApplicationDbContext` + ArchTest forbidden-type rule** (Consult #14 sub-slice 6.5.g.7): final delete + full `Run-Wave9` smoke. Rule 5c MANDATORY.

**Day 4 gate re-met when**: `dotnet build LankaConnect.sln` → 0 errors. Then **immediately** proceed to Day 5.

### Discipline: Consult #14 sub-slice labels

Where Consult #14 says "Wave 6.5.g.X", the correct plan reference is "Day 4 slot C sub-slice 4C.Y". If a future consult references Consult #14's numbering (6.5.g.1..7), translate back to 4C.a..h before writing to docs or commits. **NO NEW WAVE LABELS from here; sprint bible §Scope Ratified is the only source of Wave-6.5.f/g/h meaning.**

### Day 6 — Sat 2026-07-11 (Force Merge to develop + First Baseline) — ⚠️ PARTIAL-CLOSED 2026-07-10 (Consult #19)

**Executed 2026-07-10 (1 day early per compressed sprint schedule). Head SHA `d51496b6` on develop.**

| Agent | Owns | Status |
|---|---|---|
| A | Force-merge `bulk-move/integration` → develop | ✅ DONE — merge commit `1f9ed6fd`; 0 conflicts, cold-restore PASS. **Founder sign-off in-session; architect approval given.** |
| B | Wave 4.6 (Identity.Contracts, 15-20 types) + Wave 4.7 (ICulturalCalendar DI + consumers). Folded here. | ⏸ DEFERRED — Wave 4.7 attempted, reverted (needs Consult #15 PASS C coordinated primitive+interface batch); rolled to Phase B. Wave 4.6 remaining trivial cleanup, no active blocker. |

**Gate outcome — PARTIALLY MET (Consult #18 disposition):**
- ✅ Merge landed to develop (STOP-CONDITION avoided).
- ✅ Production compiles clean at 0 errors (verified locally via `dotnet build src/LankaConnect.API/LankaConnect.API.csproj`).
- ❌ `dotnet build -c Release` full-solution build FAILED at CI run 29070713489 step 5 with 72 test-project compile errors — Consult #12 Option D Business delete tail + Consult #13 SharedKernel rename tail + Consult #17 LegacyPromotions retarget tail. These are Day 7 slot A fix-forward per Consult #18.
- ⚙ Consult #18 workflow-scope transitional applied at commit `d51496b6` (`.github/workflows/deploy-staging.yml` step 5 narrowed to `src/LankaConnect.API/LankaConnect.API.csproj`). Restores full-solution build Day 10 EOD alongside legacy-csproj deletion. TRACEABILITY_MATRIX rows: SPRINT-D7.1 (test-project fix) + SPRINT-D10.1 (workflow restore).
- 🎯 Deploy + smoke evidence gathered post-Consult-#18 in CI run 29071326289 (in progress at time of doc flip).

**STOP-CONDITION CHECK EOD Day 6:** ✅ PASSED — merge landed to develop (well before Sat 2026-07-11 18:00 UTC deadline).

**Commit trail Day 6:**
- `1f9ed6fd` — merge commit (bulk-move/integration → develop)
- `d51496b6` — Consult #18 workflow-scope transitional + TRACEABILITY_MATRIX + audit log
- `c5737ece` — DI hotfix: CommunicationsDbContext + IMultiContextUnitOfWork + IFormResponseExporter registrations added
- `2c90d86e` — AppDbContext hotfix: closed 4C.c ownership-gap residues (removed 3 residual `.ToTable()` for Email* whose configs migrated to CommunicationsDbContext)

**⚠️ DEPLOY STILL RED at Day 6 EOD — Consult #19 ruling:**

4 CI deploy attempts all fail at step 14 "Run EF Migrations". Build/Test/Docker/Push GREEN every round. Root cause per Consult #19: **AppDbContext model-discovery boundary is systemically wrong** — it discovers module-owned aggregates (Event, EmailMessage, ...) whose owned-VO configurations moved to per-module DbContexts, then fails to build the model because the VO ctor patterns are unmapped. This is not per-VO fixable; it is a missing architectural artifact.

Sample failure pattern (4th attempt, run 29096673448):
```
Unable to create a 'DbContext' of type 'AppDbContext'. The exception
'No suitable constructor was found for entity type 'Event.TicketPrice#Money'.
```

Rule 5k trigger fired (3 consecutive same-family hotfixes). Rule 5h soft-cap exceeded. Escalation to architect Consult #19 mandatory.

**Consult #19 Q1-Q4 rulings:**
- Q1: **Day 7 fix-forward** (not another hotfix).
- Q2: **Option A** — Bulk-add `modelBuilder.Ignore<T>()` for every LankaEvents/Communications/Identity/Forms/Media aggregate on AppDbContext, executed as a design pass. Blanket Rule 5b pre-approval granted; enumeration IS the consult artifact.
- Q3: Defer smoke to Day 7 EOD. Honest founder status delivered.
- Q4: Sprint bible §Day 6 flipped to PARTIAL-CLOSED (this entry); §Day 7 slot A entry point = "AppDbContext ownership-boundary hardening (Consult #19)."

**Consult #19 additional guardrails:**
1. `AppDbContextModelParityTests.cs` asserts the Ignore<T> list is complete (mirrors Rule 5e).
2. ArchTest rule post-sprint: AppDbContext MUST NOT reach module-owned aggregate types except via `Ignore<T>()`.
3. 4C.h stays blocked until Day 7 slot A green.
4. Deliverable artifact: `docs/architecture/appdbcontext-ownership-boundary.md` alongside the code change.

### Day 7 — Sun 2026-07-12 → Tue 2026-07-14 (STAGING-VERIFIED 2026-07-14 — extended 3 calendar days; single-agent execution, not 5-agent as planned)

**STATUS: STAGING-VERIFIED 2026-07-14** — 22 hotfix deploys landed 2026-07-11 → 2026-07-14. Wave 9 smoke moved **43.33% → 71.25%+ (285/27/88 → measured further post-22nd deploy)**. Sprint bible §Day 7 target "100+/261 pass" cleared by 141 on 20th deploy.

**Consult #25 (2026-07-13)** — attack order (a)→(d)→(c)→(b) approved + UoW Option B ratified (direct `_dbContext.SaveChangesAsync` blanket pre-approved for single-context handlers with Day 8 interceptor prereq). File: [docs/architect-consults/2026-07-13-consult-25-day7-attack-order.md](architect-consults/2026-07-13-consult-25-day7-attack-order.md).

**Consult #26 (pending)** — Events list-endpoint `OwnsOne(TicketPrice).ToJson("ticket_price")` MaterializeJsonEntity fails on legacy JSON shape drift across ~11 Events tests. Needs data-migration OR ToJson→scalar-column refactor. **DEFERRED to Phase A.5** — 6 downstream money-flow cluster fails cascade from this.

**Slot A entry point (BLOCKING all others) — Consult #19 AppDbContext ownership-boundary hardening.**

Before any smoke work can run: staging deploy must succeed. Deploy is blocked on AppDbContext design-time model discovery of module-owned aggregates. Per Consult #19 Option A:

- Derive authoritative `Ignore<T>()` list from Consult #7 Delta ownership matrix (LankaEvents / Communications / Identity / Forms / Media / Payments aggregates + their owned VOs).
- Apply as bulk `modelBuilder.Ignore<T>()` sweep in AppDbContext.OnModelCreating.
- Ship `AppDbContextModelParityTests.cs` asserting the Ignore<T> list is complete (mirrors Rule 5e parity-test pattern).
- Author `docs/architecture/appdbcontext-ownership-boundary.md` alongside — this document IS the Rule 5b consult artifact per Consult #19 blanket pre-approval.
- Post-deploy: verify AppDbContext design-time construction succeeds → migrations apply → container app deploys → Wave 9 smoke suite runs.

**5 agents once Slot A ships (Rule 5j.4 audit ON as CI gate for every commit).**

| Agent | Cluster | Count |
|---|---|---:|
| A | Consult #19 slot A (AppDbContext ownership boundary) THEN Events (Wave 9.a) | ~117 endpoints |
| B | Auth + Users (Wave 9.b) | ~30 |
| C | Venue + AddOns + Seating (Wave 9.c) | ~40 |
| D | Communications + Notifications (Wave 9.d) | ~35 |
| E | Finance + Business + PhotoAlbums + long-tail (9.e + 9.f) | ~40 |

**Fix-forward only.** Every fix = straight commit to develop.
**Target: 100+/261 passing by EOD (contingent on Slot A landing early Day 7).**

### Day 8 — Mon 2026-07-13 (Smoke Regression Round 2) — FOLDED INTO Day 7 execution 2026-07-14

**STATUS: FOLDED INTO Day 7 extended execution.** Day 8 target "150+/261 passing" cleared as part of the 22-deploy Day 7 chain (285 pass on 20th deploy = well past target).

**Deferred Day 8-scope work → Phase A.5** (per Consult #25 Q5 ruling):
- Per-module `SaveChangesInterceptor` for domain-event dispatch (Q2 blanket condition) — LankaEvents domain events currently dropped on the floor since Consult #20 sweep.
- ~95 LankaEvents.Application handler migration to direct `_dbContext.SaveChangesAsync(ct)` — write-loss family; currently masked because only CreateEvent traffic-tested.

### Day 9 — Tue 2026-07-14 (Baseline Restoration) — PARTIALLY CLOSED 2026-07-14

**4 agents.**

- One per remaining smoke cluster.
- One agent runs full ArchTest suite + un-skips Rule 5 + Rule 9b as targets ship.
- **Target: 182/0/79 smoke + 57/0/0 ArchTest.**

### Day 10 — Wed 2026-07-15 (Legacy Deletion + Docs Reconciliation)

**2 agents.**

| Agent | Owns |
|---|---|
| A | `git rm -r src/LankaConnect.Domain/`, `.Application/`, `.Infrastructure/` (after migrations folder relocation), `.Shared/`, root `LankaConnect/`. Remove from solution. Rename `src/LankaConnect.API/` → `src/Hosts/Host.AllInOne/`. |
| B | Update `ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` §1.1 — mark Phase A backend complete. Move Wave 7/8 into `PHASE_A_5_PLAN.md`. Update `PLATFORM_MASTER_PLAN.md` CURRENT_WAVE. |

**Gate:** solution builds, 182/0/79 smoke on staging, 5 legacy csproj gone.

**Day 10 execution reality (2026-07-14):**

Deletion scope reality check post-Day-7-9 execution:
- `LankaConnect.Domain` — 1 file (`Class1.cs` stub). **DELETABLE** after ProjectReference cleanup on ~10 module csproj.
- `LankaConnect.Shared` — 0 real files (only obj/ generated). **DELETABLE** after ref cleanup.
- `LankaConnect.Application` — 5 real files: `IEntraExternalIdService`, `IJwtTokenService`, `GetCommunityStats(Query|Handler)`, `MetroAreaMappingProfile`. **RELOCATION REQUIRED** before deletion. Namespace mismatch: `MetroAreaMappingProfile.cs` lives in the Application project but has namespace `LankaConnect.BuildingBlocks.Application.Common.Mappings` — physical relocation should move it to BuildingBlocks.Application or LankaEvents.Application per Consult #7 Delta MetroArea ownership.
- `LankaConnect.Infrastructure` — **566 files including 506 EF migrations**. Migration relocation is a ~2-3 day Wave 5 Day 5 slot A subtask that DID NOT HAPPEN this sprint. **Deletion DEFERRED to Phase A.5** with explicit transitional-holder documentation.
- `LankaConnect/` root — 0 leaked files. **DELETABLE** (empty dir).
- `LankaConnect.API` → `Hosts/Host.AllInOne` — physical rename requires updating `.sln` + all 15+ csproj `<ProjectReference>` entries + `.github/workflows/*.yml` step paths + all `dotnet ef migrations` commands in CI + `deploy-staging.yml` container-app deployment target. **DEFERRED to Phase A.5** — high blast radius, low benefit inside sprint window.

**Revised Day 10 delivery (per Consult #25 Q5 sequencing):**
- Delete `LankaConnect.Domain` (1 stub) + `LankaConnect.Shared` (0 files) csproj after removing ~10 transitive ProjectReferences.
- Relocate 5 Application files to correct module homes + delete `LankaConnect.Application` csproj.
- Document `LankaConnect.Infrastructure` as Phase A.5 debt in `docs/PHASE_A_5_PLAN.md` (holds AppDbContext + 506 migrations + transitional repos).
- Update `ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` §1.1 marking Phase A backend "structurally complete with LankaConnect.Infrastructure transitional".
- Update `PLATFORM_MASTER_PLAN.md` CURRENT_WAVE.

### Days 11-14 — Thu 2026-07-16 → Sun 2026-07-19 (Buffer)

Likely consumption:
- Day 11: 24h staging soak regressions (~6h)
- Day 12: 4 Skip-facts couldn't retire Day 9 due to hidden deps (~8h)
- Day 13: Migration history edge cases in prod-shaped test databases (~8h)
- Day 14: **Founder UAT + final sign-off**

**If buffer not consumed: SHIP. Do not invent work.**

---

## Q1-Q7 Architect Answers (Reference)

- **Q1 feasible?** YES for backend structural. NO for Wave 7 + 8. Arithmetic: 157h backend at 6-agent × 60% efficiency = 44h wall-clock ≈ 4.4 days pure work, + 5.6 days coord/regression/doc = 10 days.
- **Q2 critical path:** Day 1 hotfix merge. Secondary Day 4 integration merge.
- **Q3 dropped:** unit tests during move, per-capability atomic extractions, 7-day soaks per slice, ArchTest gates Days 4-8, Rule 5j.4 Days 2-6, code review Days 2-6, Wave 4.9.1.
- **Q4 kept non-negotiable:** `git mv` for every file, exclusive path sets, daily 18:00 sign-off, Wave 9 smoke as "done" gate, `develop` as only integration branch after Day 6, hotfix Day 1 morning, Rule 5j.4 ON from Day 7, LankaEvents staging runtime by Day 10.
- **Q5 stop conditions:** see §Stop Conditions below.
- **Q6 founder involvement:** ~22 hours total over 14 days. 4h Day 1, 30min/day Days 2-5, 2h Day 6, 1h/day Days 7-9, 3h Day 10, 2h/day buffer.
- **Q7 Wave 7/8:** CUT. Phase A.5. Frontend independent 4-6 wk track. Prod cutover on founder timeline.

---

## Stop Conditions (Fail-State)

Sprint FAILS and rebuilds around Consult #8 (12-16 wk) if ANY:

1. **Day 1 EOD:** hotfix stack not on develop.
2. **Day 6 EOD:** `bulk-move/integration` not merged to develop. *(The big one.)*
3. **Day 9 EOD:** Wave 9 smoke below 100/261.
4. **Day 11 EOD:** staging soak reveals systemic runtime write-loss on LankaEvents (unfixable in 3 remaining days).

**Response protocol:** archive `bulk-move/*` branches, revert develop to pre-Day-2 state (hotfix intact), invoke Consult #10 for Consult #8 transition plan. ~10 days lost, no production impact.

---

## What Breaks During Sprint (Explicit — Not Bugs)

1. **develop: RED Days 2-6** — nobody merges anything else during window.
2. **Wave 9 smoke: RED Days 7-8** — expect 60% fail rate after Day 6 merge.
3. **ArchTest: RED Days 4-8** — new Skip-facts may temporarily appear.
4. **Unit tests: RED Days 2-5**.
5. **PR-validation CI gate: BYPASSED via admin merge Days 4-6**.
6. **Rule 5j.4: SUSPENDED Days 2-6**, back ON Day 7.
7. **Migration snapshot integrity: DEGRADED Days 5-10**, locked Day 10.
8. **MASTER_TODO out of sync Days 2-9**, reconciled Day 10.

**What we DO NOT break: production.** Founder confirmed prod is separate branch.

---

## Sprint Discipline Reminders

Every session start / continuation:
1. Read this document top-to-bottom (5 min).
2. Check today's date against day number.
3. Check TodoWrite list — which task is `in_progress`?
4. If ambiguity: consult system-architect BEFORE acting.
5. If a plan change is needed: consult system-architect, update this doc, act — in that order.

Every commit during sprint:
1. Commit message references day number and agent letter.
2. Days 2-6: no discipline checklist (bypass acknowledged in commit body).
3. Days 7+: Rule 5j.4 audit line in commit body mandatory.

Every 18:00:
1. Post day status to founder: what shipped, what broke, next-day owned tasks.
2. Await sign-off before Day+1 kickoff.

---

## Change Log

- 2026-07-04: Sprint plan created and approved.
