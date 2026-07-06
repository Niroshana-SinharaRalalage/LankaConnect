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

### Day 6 — Sat 2026-07-11 (Force Merge to develop + First Baseline)

**2 agents.**

| Agent | Owns |
|---|---|
| A | Force-merge `bulk-move/integration` → develop. **develop compiles first time since Day 2.** Wave 9 smoke expected to fail 40-80/261 — do not panic. |
| B | Wave 4.6 (Identity.Contracts, 15-20 types) + Wave 4.7 (ICulturalCalendar DI + consumers). Folded here. |

**Gate:** develop compiles + `dotnet test` runs (pass rate irrelevant). Deploy to staging.
**Founder sign-off required @ noon or evening — approve develop merge.**

**STOP-CONDITION CHECK EOD Day 6:** If `bulk-move/integration` did NOT merge to develop by EOD → **SPRINT FAILED**, roll to Consult #8 plan.

### Day 7 — Sun 2026-07-12 (Smoke Regression Round 1)

**5 agents. Rule 5j.4 audit ON as CI gate for every commit.**

| Agent | Cluster | Count |
|---|---|---:|
| A | Events (Wave 9.a) | ~117 endpoints |
| B | Auth + Users (Wave 9.b) | ~30 |
| C | Venue + AddOns + Seating (Wave 9.c) | ~40 |
| D | Communications + Notifications (Wave 9.d) | ~35 |
| E | Finance + Business + PhotoAlbums + long-tail (9.e + 9.f) | ~40 |

**Fix-forward only.** Every fix = straight commit to develop.
**Target: 100+/261 passing by EOD.**

### Day 8 — Mon 2026-07-13 (Smoke Regression Round 2)

Same 5 agents, same clusters, continued ownership.

**Focus:** DbContext-selection bugs (silent write-loss family from Wave 6.5.f).
**Target: 150+/261 passing by EOD.**
**Gate:** develop passes `dotnet build` + `dotnet test` unit suite.

### Day 9 — Tue 2026-07-14 (Baseline Restoration)

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
