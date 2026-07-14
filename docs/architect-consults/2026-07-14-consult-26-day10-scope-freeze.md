# Consult #26 — Day 10 Scope Freeze + Application Relocation (2026-07-14)

## Context

Sprint calendar Day 9 EOD (2026-07-14). Wave 9 API smoke `291/21/88/72.75%` on develop head `774a4c54`. Day 10 execution planning + scope decisions needed for the last day of active sprint before Days 11-14 buffer + founder UAT.

## Questions posed

- **Q1** — Item A relocation destinations (5 files in `LankaConnect.Application`)
- **Q2** — Item C `LankaConnect.API` → `Hosts/Host.AllInOne` rename disposition
- **Q3** — Events `OwnsOne(TicketPrice).ToJson` data-drift (Wave 8.5.j) — sprint or Phase A.5?
- **Q4** — ArchTest re-run + Skip-fact retirement scope for Day 12
- **Q5** — Sprint bible §Day 10 gate downscope ratification

## Architect rulings

### Q1 — Item A relocation destinations

1. `IEntraExternalIdService.cs` → `Identity.Contracts/Services/` — Consult #15 PASS C: interfaces live in Contracts; this is a cross-boundary Identity service surface.
2. `IJwtTokenService.cs` → `Identity.Contracts/Services/` — same rationale.
3. `GetCommunityStatsQuery.cs` → **defer to Wave 8.5.a-refined** — cross-module aggregation query; no clean owner (touches Events + Users + Communications). Needs Dashboard capability decision or read-model host; rushing it violates §-1 anti-pattern.
4. `GetCommunityStatsQueryHandler.cs` → **defer to Wave 8.5.a-refined** (same).
5. `MetroAreaMappingProfile.cs` → `LankaEvents.Application/Mapping/` — align physical + namespace to actual MetroArea ownership (4C.d closure). Rename namespace to `LankaConnect.LankaEvents.Application.Mapping`. Update `LegacyApplicationDependencyInjection.cs` scan.

Ship tomorrow with 3/5 files; csproj deletion **blocked** until Dashboard query relocation resolves in 8.5.a-refined. Rename `LankaConnect.Application` → `LankaConnect.Application.LegacyResidual` in the interim to signal terminal state.

### Q2 — Item C API rename: PARK in Wave 8.5.c

30-50 file edits across `.sln` + csprojs + 4 CI workflows + `dotnet ef` command paths on Day 10 EOD is a CI-breakage risk with 5 days of runway. A broken `deploy-staging.yml` mid-sprint = compounding blocker. Rename is nomenclature-only; zero runtime/architecture value delta. Phase A.5 gets a dedicated day with proper before/after CI dry-run. Downscope §Day 10 gate accordingly.

### Q3 — Wave 8.5.j: Option (i) data migration THIS sprint

17 tests unlocked in ~4 hours vs. 2-3 days for Option (ii); Option (iii) masks drift. Execute Day 11 (Thu 07-16): PL/pgSQL migration walking `events` table, normalizing `ticket_price` JSONB shape to canonical `{amount, currency}`. Idempotent SQL in commit body per Rule 5. Pair with `SaveChangesInterceptor` guard added Day 12 to prevent recurrence (2-hour ADD, not a scope explosion). Option (ii) scalar-columns refactor stays in Phase A.5 as the real fix; data migration buys sprint close.

### Q4 — ArchTest: baseline run TODAY (Day 10), un-skip pass Day 12

Two separate operations. Baseline re-run tonight after Item A lands — captures a number for the sprint retro without touching Skip-facts. Un-skip pass Day 12 as planned: Rules 5 + 9b Skip-facts retire cleanly only after Wave 8.5.j drift lands (Day 11), because a chunk of the skips exist because the underlying tests are flaky against the drifted data. Running Day 10 without that context produces noisy diffs and wasted re-work.

### Q5 — Ratify partial delivery — new §Day 10 gate

- ✅ Solution builds
- ✅ Smoke ≥ 250 pass (currently 291, was 182 target — 60% overshoot)
- ✅ 2/5 legacy csproj gone (`LankaConnect.Domain`, `LankaConnect.Shared`)
- ✅ Wave 8.5.a-refined + 8.5.b + 8.5.c carryover documented with dated commitments

Mandating 5/5 forces rushed 566-file Item B (Infrastructure deletion) = guaranteed regression on the last 5 sprint days. Sprint success = boundary purity + test-suite health; both achieved. Wave 8.5 is a legitimate Phase A.5 continuation, not a failure mode.

## Sprint bible §Day 10 status flip

`Day 10 (2026-07-15): PARTIAL DELIVERED — 2/5 csproj gone + Item A 3/5 files landed (Identity.Contracts x2, LankaEvents.Application x1); Dashboard query pair deferred → Wave 8.5.a-refined; Item C API-rename parked → Wave 8.5.c; Wave 8.5.j data-migration groundwork Day 11; ArchTest baseline captured; smoke 291/21/88 sustained on develop head 774a4c54.`

## Execution outcome retro (added 2026-07-14 EOD)

**Q1 execution downscope**: Ruling said "Identity.Contracts" for the 2 interfaces, but Identity.Contracts is layered to only reference `BuildingBlocks.Contracts` (no `Identity.Domain` reference). `IJwtTokenService.GenerateAccessTokenAsync(User user)` requires the `User` domain entity — moving to Contracts would need a canonical refactor to `Guid userId` or `UserSummaryDto`. Attempted move to `Identity.Application/Common/Interfaces/` instead (which does reference Identity.Domain). Compile failed: `Identity.Infrastructure/Security/EntraExternalIdService.cs` + `JwtTokenService.cs` couldn't find the interfaces because `Identity.Infrastructure` doesn't reference `Identity.Application`. Adding that reference creates a hard cycle with the existing `Identity.Application → Identity.Infrastructure` edge (Wave 6.5.f Consult #17 LegacyPromotions transitional).

**Revised Day 10 delivery**: only `MetroAreaMappingProfile` relocated (1 of 5 files). The 2 interface relocations reverted; deferred to Wave 8.5.a-refined properly per the layering-constraint discovery.

**Sprint bible §Day 10 status (final)**: `2/5 csproj gone (Domain + Shared) + 1/5 Application files relocated (MetroAreaMappingProfile → LankaEvents.Application/Mapping/); interfaces + Dashboard query pair → Wave 8.5.a-refined; API-rename → Wave 8.5.c; ratified per Q5.`
