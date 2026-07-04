# Consult #9 Ruling — 2-Week Bulk-Move Phase A Completion Plan

**Author:** system-architect (consult #9, 2026-07-04)
**Founder mandate:** 2 weeks. Bulk move. Fix-forward. No hedging.

## TL;DR

- **2 weeks feasible for backend structural completion** (legacy projects deleted, target folder layout live, staging back to green) IF Wave 7 + Wave 8 + Wave 4.9.1 cut, 6 parallel worktree agents Days 2-8, staging RED Days 2-6.
- **The 6-commit hotfix stack MUST merge to develop Day 1 morning or entire plan slips 24-48h.** Critical path.
- **Wave 7 + Wave 8 become Phase A.5** (backend-refactor-close ≠ Phase-A-close). Founder must ratify this scope redefinition.

Total burn: ~100 productive hours (10 hrs/day × 10 days) + 4 buffer days.

---

## Day 1 (Mon 2026-07-06): Consolidate Hotfix + Doc Surgery + Fan-Out Prep

- **Parallel agents:** 2
- **Agent A — Hotfix merge (solo):** rebase 6-commit `wave-6-5-f-5-hotfix` stack onto develop. Wave 9 smoke → confirm 182/0/79. Merge.
- **Agent B — MASTER_TODO surgery + Solution Rewrite Manifest:** delete Wave 4.9.1/7/8 rows, freeze Wave 4.9.2 fold-in list. Produce `bulk-move-manifest.md`: exact source→target path table for all ~1,391 files (409 App + 318 Dom + 664 Inf + ~50 API + ~50 Shared/root).
- **Merge gate:** hotfix stack green on develop. Manifest signed off.
- **Hours:** 10

**IF hotfix stack not green by 18:00 Day 1, STOP.**

---

## Day 2 (Tue 2026-07-07): The Bulk Move (Big-Bang Move Day)

- **Parallel agents:** 6, independent worktrees with pre-declared exclusive path sets.
- **Agent A — Domain move:** 318 `.cs` → `src/Modules/*/Domain/` or `src/Products/LankaEvents/LankaEvents.Domain/`. `git mv` batch. Namespace rewrite via one-shot script (built Day 1 night).
- **Agent B — Application move:** 409 `.cs` → target module Application projects.
- **Agent C — Infrastructure (part 1, non-EF):** ~200 service/repo/helper files → target module Infrastructure.
- **Agent D — Infrastructure (part 2, EF configs):** `Data/Configurations/*` → per-module `Data/Configurations/`. Migrations folder STAYS in LankaConnect.Infrastructure temporarily.
- **Agent E — API move:** ~50 controllers → target module `.Api` projects. Program.cs stays in LankaConnect.API (becomes Host.AllInOne Day 10).
- **Agent F — Shared + root move:** empty `LankaConnect.Shared/` + `LankaConnect/` root → SharedKernel/Contracts.
- **Merge gate: NONE. LEAVE BROKEN.** Push each to `bulk-move/agent-X` branch.
- **Hours:** 10

---

## Day 3 (Wed 2026-07-08): Compile Reconciliation

- **Parallel agents:** 6 (same worktrees). Fix namespace + `using` errors in bounded set.
- **Cross-agent dependency conflicts:** ~50-100 cross-module reference violations. Fix via Contracts elevation OR temporary `InternalsVisibleTo` (defer real fix to Day 7).
- **Merge gate:** Each worktree branch builds standalone.
- **Hours:** 10

---

## Day 4 (Thu 2026-07-09): Big Merge + Solution Reconciliation

- **Parallel agents:** 3
- **Agent A — Merge orchestrator:** merge 6 bulk-move branches → `bulk-move/integration` in dependency order (Domain → Contracts → Application → Infrastructure → Api → Host).
- **Agent B — csproj cleanup:** DELETE `LankaConnect.Domain/.Application/.Infrastructure/.Shared/LankaConnect` from solution. Remove ProjectReferences.
- **Agent C — DbContext consolidation:** 73 files with AppDbContext refs in moved Application handlers → rewrite to `IMultiContextUnitOfWork` + module DbContext. Fan into 3 sub-agents by handler family (Communications ~40, Businesses ~10, Analytics/Badges/Support/MetroAreas ~23).
- **Merge gate:** `bulk-move/integration` compiles. `dotnet test` NOT required to pass.
- **Hours:** 10

---

## Day 5 (Fri 2026-07-10): Migrations Repatriation + AppDbContext Death

- **Parallel agents:** 4
- **Agent A — Migration inventory:** split 506 AppDbContext migrations into per-module buckets. Cap history + route new ones per-module (don't re-parent old).
- **Agent B — Wave 6.5.f handler migration (LankaEvents):** ~120 handlers → `IMultiContextUnitOfWork` (123 files, 156 occurrences grep-confirmed).
- **Agent C — Wave 6.5.g Payments un-skip:** 11 handlers → integration events.
- **Agent D — Wave 6.5.h Rule 5 un-skip:** 14 services + 7 webhook handlers → integration events.
- **Merge gate:** Still on `bulk-move/integration`. Merge to develop Day 6.
- **Hours:** 10

---

## Day 6 (Sat 2026-07-11): Merge to develop + First Compile-Clean Baseline

- **Parallel agents:** 2
- **Agent A — Force merge `bulk-move/integration` → develop.** develop compiles for first time since Day 2. **Wave 9 smoke expected to fail 40-80/261.**
- **Agent B — Wave 4.6 (Identity.Contracts, 15-20 types) + Wave 4.7 (ICulturalCalendar DI).**
- **Merge gate:** develop compiles + `dotnet test` runs (pass rate irrelevant).
- **Founder sign-off required on develop merge.**
- **Hours:** 10

---

## Day 7 (Sun 2026-07-12): Wave 9 Smoke Regression Sprint (Round 1)

- **Parallel agents:** 5. Founder deploys develop to staging.
- **Agent A** — Events cluster (Wave 9.a, 117 endpoints)
- **Agent B** — Auth + Users (Wave 9.b)
- **Agent C** — Venue + AddOns + Seating (Wave 9.c)
- **Agent D** — Communications + Notifications (Wave 9.d)
- **Agent E** — Finance + Business + PhotoAlbums + long-tail (9.e + 9.f)
- **Fix-forward only.** Every fix is a straight commit to develop.
- **Target:** 100+/261 passing by EOD.
- **Hours:** 10

---

## Day 8 (Mon 2026-07-13): Wave 9 Smoke Regression Sprint (Round 2)

- **Parallel agents:** 5 (same clusters).
- **Target: 150+/261 passing.**
- Address DbContext-selection bugs (silent write-loss family). Rule 5j.4 audit script as CI gate on every commit.
- **Merge gate:** develop passes `dotnet build` + `dotnet test`.
- **Hours:** 10

---

## Day 9 (Tue 2026-07-14): Baseline Restoration

- **Parallel agents:** 4
- **Target: 182/0/79 baseline restored.**
- One agent per remaining smoke cluster. One agent runs ArchTest suite + un-skips Rule 5 + Rule 9b.
- **Merge gate:** 182/0/79 smoke + 57/0/0 ArchTest (up from 53/4/0 as 4 Skip-facts retire).
- **Hours:** 10

---

## Day 10 (Wed 2026-07-15): Legacy Project Deletion + Docs Update

- **Parallel agents:** 2
- **Agent A — csproj + directory deletion:**
  - `git rm -r src/LankaConnect.Domain/`
  - `git rm -r src/LankaConnect.Application/`
  - `git rm -r src/LankaConnect.Infrastructure/` (after moving `Data/Migrations/` into LankaEvents.Infrastructure with `Legacy_` prefix OR archive project)
  - `git rm -r src/LankaConnect.Shared/`
  - `git rm -r src/LankaConnect/`
  - Remove from `LankaConnect.sln`.
  - Rename `src/LankaConnect.API/` → `src/Hosts/Host.AllInOne/` (or leave as alias — founder rules).
- **Agent B — Blueprint + MASTER_TODO reconciliation.** Mark Phase A backend complete. Cut Wave 7/8 into new `PHASE_A_5_PLAN.md`.
- **Merge gate:** solution builds, 182/0/79 smoke green, 5 legacy csproj gone.
- **Hours:** 10

---

## Days 11-14 (Buffer — Thu 07-16 → Sun 07-19)

Likely consumption order:
1. **Day 11:** Additional regressions surfacing after 24h staging soak (~6h).
2. **Day 12:** 4 Skip-facts couldn't retire Day 9 due to hidden dependencies (~8h).
3. **Day 13:** Migration history reconciliation edge cases (~8h).
4. **Day 14:** Founder UAT + final sign-off.

If buffer not consumed: **SHIP.** Do not invent work.

---

## Q1-Q7 Direct Answers

**Q1: Feasible with 6-8 parallel agents? Arithmetic.**

**YES for backend structural.** NO for Wave 7 + Wave 8.

- 1,391 legacy files @ 250 files/agent-day × 6 agents = 1,500 file-move capacity. Fits Day 2.
- ~200 handler rewrites @ 8/agent-hour × 6 agents × 4h = 192. Fits Days 4-5.
- 156 smoke regressions @ 4/agent-hour × 5 agents × 20h (Days 7-8) = 400 capacity. 2.5× margin.
- Consult #8 = 520h remaining. Backend-only = 520 - 180 (W7) - 150 (W8) - 33 (W4.9.1) = **157h**. At 6-agent parallelism × 60% efficiency: 157 / (6 × 0.6) = **44h wall-clock ≈ 4.4 days pure work**. + coordination + regression + doc + buffer = **10 days. Fits.**

**Q2: Critical path — the ONE thing.**

**Day 1 hotfix stack merge.** 24h delay = 48-72h total slip because Day 2 parallel agents can't be re-baselined mid-flight. Secondary: Day 4 merge to `bulk-move/integration`.

**Q3: Discipline DROPPED entirely.**

- No unit tests added Days 2-5. Existing may break.
- No per-capability atomic extractions.
- No 7-day soak per slice. One soak Days 10-11.
- No ArchTest un-skip discipline until Day 9.
- Rule 5j.4 SUSPENDED Days 2-6, back ON Day 7.
- No code-review approvals Days 2-6 (founder + architect eyeball only).
- Wave 4.9.1 gap-fill: **DELETED not deferred.**
- Wave 4.9.2 physical columns: fold into Day 5 or defer to Phase B.

**Q4: Discipline KEPT non-negotiable.**

1. `git mv` for every file (blame/history preservation — ONLY discipline founder can't rebuild later).
2. Pre-declared exclusive path sets per agent.
3. Daily 18:00 founder sign-off.
4. Wave 9 smoke as objective "done" gate.
5. `develop` = only integration branch after Day 6.
6. Hotfix stack lands Day 1 morning.
7. Rule 5j.4 audit script exists Day 1, turns ON Day 7 for regression-fix commits.
8. LankaEvents runtime on staging by Day 10 EOD.

**Q5: STOP condition.**

Sprint FAILS if ANY:
1. **Day 1 EOD:** hotfix stack not on develop.
2. **Day 6 EOD:** `bulk-move/integration` not merged to develop. *(The big one.)*
3. **Day 9 EOD:** Wave 9 smoke below 100/261.
4. **Day 11 EOD:** staging soak reveals runtime write-loss on LankaEvents (systemic — unfixable in 3 days).

**Response:** archive `bulk-move/*` branches, revert develop to pre-Day-2 state (hotfix intact), transition to Consult #8 per-capability plan (12-16 weeks). ~10 days lost, option retained.

**Q6: Founder involvement per day.**

- Day 1: **4h** (approve manifest, sign hotfix merge, ratify W7/W8 cut).
- Days 2-5: **30 min/day** (18:00 sign-off ritual, no code touch).
- Day 6: **2h** (approve develop merge, deploy staging).
- Days 7-9: **1h/day** (triage smoke failures).
- Day 10: **3h** (final review, csproj deletions, sign off).
- Days 11-14: **2h/day** (UAT + sign-off).

**Total founder time: ~22 hours over 14 days.**

**Q7: Wave 7 + Wave 8 disposition.**

Both **CUT from 2-week window.** Become Phase A.5.

- **Wave 7 Frontend Mirror (~180h):** independent 4-6 week track after Day 10. Frontend team continues against current API surface (Host.AllInOne serves identically).
- **Wave 8 Production Cutover (~150h):** deferred. Prod runs off separate branch per founder. Cutover when founder decides.

On 2026-07-19, "Phase A complete" = backend structural refactor done. Production still runs old code.

---

## What We Break During Sprint (Explicit List)

1. **develop: RED Days 2-6.** Nobody merges anything else.
2. **Wave 9 smoke: RED Days 7-8.** Expect 60% fail rate after Day 6 merge.
3. **ArchTest: RED Days 4-8.** New Skip-facts may appear temporarily.
4. **Unit test suite: RED Days 2-5.**
5. **PR-validation CI gate: BYPASSED via admin merge Days 4-6.**
6. **Rule 5j.4: SUSPENDED Days 2-6.**
7. **Migration snapshot integrity: DEGRADED Days 5-10.** Locked Day 10.
8. **MASTER_TODO out of sync Days 2-9.** Reconciled Day 10.

**We do NOT break: production. Founder confirmed prod is separate branch.**

---

## Critical Path

```
Day 1 morning: hotfix stack lands
    ↓
Day 2: 6-agent bulk move
    ↓
Day 4: bulk-move/integration compiles
    ↓
Day 6 evening: develop merge + staging deploy
    ↓
Day 9: Wave 9 smoke back to 182/0/79
    ↓
Day 10: legacy csproj DELETED
    ↓
Phase A backend complete.
```

Single-node 24h slip on Days 1/4/6 = 48-72h total slip. Buffer 11-14 absorbs up to 96h. Beyond = Consult #8 territory.
