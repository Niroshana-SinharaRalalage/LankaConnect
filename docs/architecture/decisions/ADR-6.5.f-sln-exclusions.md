# ADR-6.5.f — Wave 6.5.f Cutover Checkpoint: Application-layer Compile Debt Carried into Wave 6.5.g

**Status**: Accepted (2026-07-06, Sprint Day 5)
**Author**: Niroshana (execution); System Architect (rulings)
**Consult chain**: Consult #12 → #13 → #13.3 → #13.5 → #13.5.1 → #13.5.2
**Scope**: `bulk-move/integration` branch, 2-week bulk-move sprint

## Context

Sprint Day 4 landed 1363 compile errors on `bulk-move/integration` after the Wave 6.5.f Modules-Domain carve-out + bulk namespace rewrites + Consult #12 Option D deletion of `LankaConnect.Domain.Business`. Sprint Day 5 execution reduced that to 0 errors across:

- `BuildingBlocks.Domain` (LegacyBaseEntity + non-generic AggregateRoot with `[SetsRequiredMembers]` per Consult #13 amendment)
- `Products/LankaEvents/LankaEvents.Domain`
- All 7 `Modules/*.Domain` projects (Communications, Payments, Identity, Forms, CulturalIntelligence, Notifications, Media)

**Residual 149 errors** remain in 5 non-Domain projects at the Wave 6.5.f cutover boundary:

| Project | Errors | Class |
|---|---:|---|
| `src/LankaConnect.Application` | 182 | dead-namespace usings (`BB.Domain.Monitoring/Security/Recovery/Database`), orphan `DbSet<Business>` after Consult #12 Option D deletion, `IApplicationDbContext` contract seam pending Consult #14 |
| `tests/LankaConnect.Domain.Tests` | 90 | test-shape drift (BB.Domain contract changes not yet propagated to tests) |
| `src/Modules/Media/Media.Application` | 14 | consumes `IApplicationDbContext` — same seam issue |
| `tests/Modules/Notifications/Notifications.Domain.Tests` | 10 | test-shape drift |
| `src/Modules/CulturalIntelligence/CulturalIntelligence.Application` | 2 | consumes `IApplicationDbContext` |

## Decision

**Checkpoint the Modules-Domain cutover as an honest red-build commit** on `bulk-move/integration` with the 149 residual errors documented. Defer cleanup to **Wave 6.5.g** under a fresh Consult #14 that will rule on the `IApplicationDbContext` contract seam.

## Alternatives considered

**Alt 1 — Path C (sln exclusion + `EnableDefaultCompileItems=false`)**: initially attempted per Consult #13.5 PASS C + #13.5.1 PASS 1. Excluded 2 test projects cleanly (nothing else PR-references them). Excluding 3 Application csprojs by emptying them cascaded 262 new errors into 5 downstream consumers (`Communications.Application` alone hit 198 CS0246 on `IApplicationDbContext`) because the seam is a contract, not a leaf. Path C reverted per Consult #13.5.2 PASS B: "Path C's premise — 'empty 3 leaf projects, contain the blast' — collapsed the moment the cascade hit `IApplicationDbContext` (a contract seam, not a leaf) … extending exclusion further is emptying the platform to fake green."

**Alt 2 — Path A (push through LankaConnect.Application cleanup solo)**: ~2-4 hrs mechanical + judgment calls on which `DbSet`s / methods survive, which move to module contexts, which delete. Blows past Rule 5h 30-min hotfix soft cap and mixes two concerns (Wave 6.5.f Domain cutover + Wave 6.5.g Application-layer cleanup) in one commit. Rejected per Consult #13.5 PASS C rationale.

## Consequences

**Positive**:
- Wave 6.5.f Modules-Domain cutover is a discrete, greppable atomic unit in git history.
- All 9 Domain projects compile clean (7 Modules + LankaEvents + BuildingBlocks) — the actual deliverable of Wave 6.5.f.
- `[SetsRequiredMembers]` transitional-bridge scope is well-bounded (LegacyBaseEntity + non-generic AggregateRoot only; Entity<T> untouched per Consult #12).
- Consult #14 gets a scoped input: 149 errors across 5 projects with clear seam (`IApplicationDbContext`).

**Negative**:
- Sprint bible compile-green invariant violated at commit time. This ADR is the explicit sanction; the Consult #13 cascade authorized it.
- Any bisect of `bulk-move/integration` between this commit and Wave 6.5.g cleanup lands on a red-build state. Bisecting agents should skip this range.
- Sprint pre-push hook may reject; will be pushed with `--no-verify` and logged to `docs/audit/test-debt-overrides.log` per Rule 5's hook-bypass audit protocol.

**Exit criteria (Wave 6.5.g)**:
1. Consult #14 rules on `IApplicationDbContext` disposition (delete / relocate to module contexts / retain as legacy seam).
2. Orphan `DbSet<Business>` + related methods deleted or relocated.
3. Dead-namespace usings (`BB.Domain.Monitoring/Security/Recovery/Database/Enterprise/ValueObjects`) audited: dead → deleted; live-but-moved → rewritten to their new SharedKernel locations.
4. Test projects updated to current contract shape (BB.Domain.Tests + Notifications.Domain.Tests).
5. `dotnet build LankaConnect.sln` → 0 errors.
6. `dotnet test` runs on all restored projects (failures OK per sprint tolerance; compile-green mandatory).

## Related

- Consult #12 Option D: Business aggregate deletion.
- Consult #13 Q3+Q4+Q2+Q1: LankaEvents.Domain fix chain.
- Consult #13 Q1 amendment: `[SetsRequiredMembers]` on LegacyBaseEntity ctors (transitional bridge scope).
- Consult #13.3 PASS A: mechanical extension of Q3/Q4/Q2 patterns to 7 Modules/*.Domain.
- Consult #13.5 PASS C → #13.5.1 PASS 1 → #13.5.2 PASS B: this ADR's ruling chain.
- Pending Consult #14: `IApplicationDbContext` seam disposition.

## Notes for reviewers

This ADR documents an operational compromise, not an architectural change. The 5-layer topology (BuildingBlocks → SharedKernel → Capabilities/Modules → Products → Hosts) is intact; D1-D10 stand; ADR-002 layering rules stand. The compile-red state is a Wave-boundary artifact, not a design decision.
