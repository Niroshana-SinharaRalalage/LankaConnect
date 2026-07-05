# Sprint Day 4 Status — Integration Merge Complete, Compile-Fix In Progress

**Executed today (Sat 2026-07-05):**

## What worked (Day 3+4 collapsed, saved a day)

1. **All 6 bulk-move branches merged into `bulk-move/integration` — 0 conflicts.**
2. **Phase 2 namespace rewrite**: 743 using-directive updates across 370 files.
3. **BB.Domain cleanup**: down from 16 → ~6 errors (near-clean).
4. **Push preserved on origin**: `bulk-move/integration @ 85567109` — all work protected.

## What we hit (larger than plan expected)

**Total compile errors after Phase 2 + BB.Domain cleanup: ~1,363.**

Per-project distribution (uniqued):

| csproj | Errors | Nature |
|---|---:|---|
| `Host.AllInOne` | **~2,530** raw (many dupes) | csproj is a PLACEHOLDER — `Microsoft.NET.Sdk` (not `.Web`), zero PackageReferences, zero ProjectReferences. Agents C+E moved ~40 files into it (BackgroundServices, Controllers, Services, Dashboard, Legacy DI). Every `using Microsoft.EntityFrameworkCore` / `LankaConnect.Modules.*` / `LankaConnect.Products.*` fails. |
| `LankaConnect.Shared.Tests` | 184 | Tests reference moved Email/WhatsApp types now in `Communications.Contracts`. |
| `Communications.Contracts` | 6 | Small cleanup. |
| `BuildingBlocks.Domain` | 6 | AggregateRoot `new` keyword nit + BusinessRule Error/string. |

Sprint plan expected "50-100 cross-module reference violations". We have ~1,300+. That's an order of magnitude larger.

## Root cause

Two independent contributors:

1. **Host.AllInOne was a placeholder csproj.** Manifest routed cross-cutting controllers + BackgroundServices + Legacy DI files there, but the csproj wasn't set up as a proper Web host. **~2,530 of the 2,726 raw errors originate here.**

2. **Namespace-map was too coarse.** 51 top-level rules covered feature-folder moves but not sub-namespace granularity. Phase 2 only rewrote 743 references; many more consumers still point at old namespaces.

## Options

### A. Fix Host.AllInOne csproj + extend namespace-map (~1-2 hours agent time)
1. Change `Host.AllInOne.csproj` to `Microsoft.NET.Sdk.Web` + add PackageReferences (EF Core, MediatR, AutoMapper, Npgsql, Swashbuckle) + ProjectReferences to all Modules + Products + BuildingBlocks + SharedKernel.
2. Extend `docs/sprint/namespace-map.txt` with granular sub-namespace rules based on `git log` per moved folder.
3. Re-run Phase 2. Re-measure. Iterate on residuals.

**Risk:** even with this, may still have hundreds of errors from moved-file interior references to deleted types (over-engineered Common/Database, Enterprise etc). Estimate 4-8 hours to reach clean build.

### B. Move Host.AllInOne files BACK to LankaConnect.API (30 min)
Files in `src/Hosts/Host.AllInOne/` (BackgroundServices, Controllers, Services, Dashboard, Legacy*) revert to `src/LankaConnect.API/`. Day 10 already planned to `git mv src/LankaConnect.API → src/Hosts/Host.AllInOne/` as a whole-project rename, which handles the csproj SDK conversion at that point.

**Trade-off:** doesn't fix the Communications.Contracts / BB.Domain / test errors, but knocks Host.AllInOne out of the picture — from ~2,530 errors to ~200-300. Much smaller residual.

### C. Stop-condition invoked (fallback to Consult #8 per-capability plan)
Sprint bible Day 6 EOD stop condition is "`bulk-move/integration` not merged to develop". We're not there — integration branch exists and merges are clean, just doesn't compile. Not stop-condition territory. But scale of remaining work suggests it's worth surfacing.

## Recommendation

**Option B.** Reverts a naive routing decision from the manifest (Host.AllInOne as target for cross-cutting host files was aspirational for Day 10). Reduces error surface by ~10x. Day 10 already handles the eventual `LankaConnect.API → Host.AllInOne` rename properly.

## Time spent today

Rough breakdown:
- Day 1 investigation (3 fixes): ~5h
- Day 2 firing agents A-E in parallel: ~1h
- Day 3+4 merge + Phase 2 + BB.Domain cleanup: ~2h
- **Total: ~8h focused engineering.**

## Sprint plan impact

- Day 2 completed ahead of schedule (Fri evening + Sat evening)
- Day 3+4 collapsed (saved one day)
- Day 4 compile-fix will likely bleed into Sun 2026-07-06 (originally Day 5)
- Net: still on-plan for 2-week window if Options A or B execute in 1-2 days

## Ask

Which option to pursue?
