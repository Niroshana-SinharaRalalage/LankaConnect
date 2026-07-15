# Sprint Day 14 Retro — 2-Week Bulk-Move (2026-07-06 → 2026-07-15)

**Closed 4 calendar days early** (delivered 2026-07-15 vs planned 2026-07-19).

## What worked

1. **Consult-driven decision cadence.** 17 architect consults (#7 through #27) shipped ruled decisions in a single response each, no back-and-forth. Sprint would have stalled without this cadence — every mid-execution surprise (Consult #17 cycle-break, #18 workflow scope, #19 AppDbContext ownership, #25 attack order, #26 Day 10 scope freeze, #27 Phase A close-out) got a ruling within one round-trip.
2. **Bulk-move Day 2 6-worktree parallel move.** ~1,391 files migrated in a single day using isolated worktrees + namespace-rewrite script. Zero cross-agent conflicts. Big-bang worked because Days 3-4 caught the compile errors upstream of Day 6 merge.
3. **`git mv` discipline preserved blame** on every file. Post-sprint blame queries all still show original commit authors + history.
4. **Empty-Up() rebaseline pattern** (Consult #14 PASS B): let module DbContexts inherit historical schema without regenerating migrations. Scaled cleanly from 4C.c Communications through 4C.d LankaEvents 10 DbSets through 4C.e User → Identity.
5. **Wave 8.5 debt catalog authoring** (Day 10). Filing 12 items with exact fix paths + estimates gave founder honest visibility on partial delivery + made Phase A.5 kickoff turnkey.

## What surprised

1. **4C.d over-granulation into 13 sub-sub-slices.** Sprint bible SECTION 0.5 explicitly documented this as anti-pattern going forward: cap each sub-slice at ONE commit. Cost ~1 extra day of Day 4-5 overhead.
2. **Wave 6.5.a `IMultiContextUnitOfWork.CommitAsync(DbContext[])` broken at first use** (Day 7 16th deploy). Cross-connection transaction enrollment throws because AppDbContext + module DbContexts pull separate Npgsql pool connections. Consult #25 Q2 ruled Option B (direct SaveChanges) as sprint-compatible; proper shared-connection fix → Wave 8.5.h.
3. **4C.d.vi local BaseController extraction dropped `[ApiController]/[Route]/[Produces]` attributes** (Day 7 14th deploy). Silent regression that 404'd every module controller. Fix was one 3-attribute line per file × 4 files. Root cause: attribute copy-paste during cycle-break wasn't scoped in the sub-slice checklist.
4. **Wave 9 smoke measurement noise.** The suite expanded from 261 tests to 400 tests as fixture-dependent skips became runnable — made "pass count vs baseline" comparisons confusing. Consult #26 Q5 ratified the 291 pass on 400 suite as satisfying the 182 pass on 261 suite baseline.
5. **AutoMapper single-file miss cascading to 7+ smoke fails** (Day 7 18th deploy). `MetroAreaMappingProfile` invisible to entry-assembly-only `AddAutoMapper(assembly)` scan → GetMetroAreas 500 → every `Get-LcAnyMetroAreaId` smoke fixture failed → 7 AdminUsers + 1 Auth + downstream. One `services.AddAutoMapper(typeof(...).Assembly)` line cleared the cascade.
6. **Day 7 execution stretched across 3 calendar days.** Founder callout at 2026-07-14: pace expectation was 1 day / 1 sprint-day but reality was 22 hotfix deploys spanning 3 calendar days. Cause: each fix surfaced the next boundary bug. Discipline lesson: batch diagnosis + batch fix, don't one-deploy-per-fix.

## What to carry forward (already filed as Wave 8.5)

All 12 items live in `docs/PHASE_A_5_PLAN.md` §Wave 8.5:
- **8.5.a-refined** — LankaConnect.Application 4-file relocation (IEntraExternalIdService, IJwtTokenService, GetCommunityStats {Query,Handler}) + csproj delete. Blocked on User→Guid API reshape (Consult #15 PASS C).
- **8.5.b** — LankaConnect.Infrastructure 566-file dismantle + 506 EF migration relocation to per-module folders.
- **8.5.c** — LankaConnect.API → Hosts/Host.AllInOne physical rename (sln + CI + docs cross-refs).
- **8.5.d** — LegacyPromotions folder split into `Contracts/{Repositories,Services,DTOs}/` per Consult #17 Day 10 debt.
- **8.5.e** — Test-project full-solution build restore per Consult #18 (currently CI narrowed to `LankaConnect.API.csproj`).
- **8.5.f** — Per-module SaveChangesInterceptor for domain-event dispatch on LankaEventsDbContext + IdentityDbContext + CommunicationsDbContext. **BLOCKS all Phase-B cross-module writes.**
- **8.5.g** — ~95 LankaEvents.Application handler direct-SaveChanges migration (prereq: 8.5.f).
- **8.5.h** — Wave 6.5.a shared-connection `IMultiContextUnitOfWork` fix (register scoped `NpgsqlConnection`, hand to each `AddDbContext<T>`).
- **8.5.i** — Metro-area cross-module write via `IMetroAreaRepository` per Blueprint §7.8 (replaces raw-SQL insert in RegisterUserHandler + UpdateUserPreferredMetroAreasCommandHandler).
- **8.5.j** — Events `OwnsOne(TicketPrice).ToJson` data-shape drift. Consult #26 Q3 Option (i) data migration OR Option (ii) scalar-columns refactor.
- **8.5.k** — Businesses controller product decision (scaffold minimal OR mark Wave 9 SKIP with "LankaBusiness parked until Phase B").
- **8.5.l** — PhotoAlbums body-capture confirmed 2026-07-15: same Wave 8.5.f dispatch gap; folds into 8.5.f (no separate action).

## Metrics

- **Deploys:** 24 successful staging deploys + numerous docs commits across the sprint.
- **Consults:** 17 architect consults filed in `docs/architect-consults/`.
- **Test movement:** Wave 9 API smoke 43.33% → 72.75% (+29.42pp; +109 absolute pass count over baseline).
- **ArchTest:** 49 Passed / 0 Failed / 9 Skipped (all skips carry Wave 8.5 debt ref).
- **Migration drift:** zero across all 7 DbContexts.
- **Sprint bible §Stop Conditions:** 4 of 4 avoided.

## Discipline callouts for Phase A.5

- Keep Rule 5j.4 handler-audit line + Rule 5c staging-smoke pre-merge gate ACTIVE. Days 2-6 bypass window is closed; Wave 8.5 work runs under full discipline.
- Batch diagnosis before batch fix — the Day 7 22-deploy chain was a symptom of one-deploy-per-hotfix pattern.
- Every architect consult continues to file `docs/architect-consults/YYYY-MM-DD-consult-N-title.md` with Q1..QN structure.
