# Architect Consult #18 — Day 5 slot A cycle-break (nuget-restore MSB4006)

**Date:** 2026-07-09 (Sprint Day 5)
**Branch:** `bulk-move/integration` @ `c7a18e6d`
**Status:** RULED — execute within the Consult #17 envelope; no new ADR required (compile-time boundary reshape only).
**Related:** Consult #14 PASS B (IApplicationDbContext teardown, 4C.a–h), Consult #15 PASS C (interfaces + DTOs live in `Module.Contracts`), Consult #17 (LegacyPromotions cycle-break pattern).

---

## Problem

`dotnet build` incremental passes but `dotnet restore` COLD-run fails MSB4006 ("circular dependency in the target dependency graph"). Two 3-node cycles introduced by the Wave 6.5.f reverse-direction PRs (`Module.Application → Module.Infrastructure`) colliding with legacy `LankaConnect.Infrastructure` (LC.Infra) still holding PRs to `LankaEvents.Application` + `Communications.Application`:

```
LC.Infra → LankaEvents.Application → LankaEvents.Infra → LC.Infra   (transitional AppDbContext + Repository<T>)
LC.Infra → Communications.Application → Communications.Infra → LC.Infra
```

Day 6 EOD develop-merge triggers a CI cold restore → sprint fail-state if unresolved.

## Verified consumer inventory (exhaustive)

- `LC.Infra → LankaEvents.Application` PR consumed ONLY by the 2 export services (`Services/Export/{Csv,Excel}ExportService.cs`).
- `LC.Infra → Communications.Application` PR consumed ONLY by the 4 email repos (`Data/Repositories/{EmailMessage,EmailStatus,EmailTemplate,UserEmailPreferences}Repository.cs`).

## Direction — CONFIRMED

- LankaEvents = Product (L4); Communications = Capability (L3). Cross-boundary references only via `.Contracts` (Blueprint §1.3).
- Export IMPLs → **`LankaEvents.Infrastructure`** (they carry ClosedXML/CsvHelper — L4 Infrastructure libs; Application placement would drag infra packages up a layer, contradicting bulk-move-manifest Agent C). Interfaces `ICsvExportService`/`IExcelExportService` already in `LankaEvents.Contracts/LegacyPromotions/`.
- Email repos → **`Communications.Infrastructure`**. They extend LC.Infra `Repository<T>` and inject `AppDbContext`, both reachable via the existing transitional `Communications.Infra → LC.Infra` edge — no new edge, mechanically safe, zero runtime change.

## Rulings

### Q1 — DTO staging: `LegacyPromotions/`, NOT permanent `Contracts/DTOs/` now.
Promote the 9 files (5 financial `Event*Response` + `AllFinancialsData` + the `SignUpListDto` cluster [`SignUpListDto`/`ISignUpItemDto`/`QuantityBasedItemDto`/`SlotBasedItemDto`/`SignUpCommitmentDto`] + `SignUpExportLabels`) to `LankaEvents.Contracts/LegacyPromotions/`, **preserving the existing `LankaConnect.Products.LankaEvents.Application.Common` namespace**. Reasons: (1) zero-runtime-change favors zero blast radius — namespace preserved means every consumer `using` is untouched; a `Contracts/DTOs/` rename would churn dozens of query handlers on a live-prod branch for no runtime benefit; (2) sprint consistency — Consult #17 defined LegacyPromotions for exactly this maneuver; (3) honest signalling — a file namespaced `Application.Common` in a folder named `DTOs/` would lie about its state. Day-10 legacy pass owns the split into `Contracts/DTOs/` + namespace normalization. Consult #15 PASS C is satisfied by destination = Contracts; it does not mandate final folder shape on the cycle-break commit.

### Q2 — Premise mistaken: nothing stranded, nothing to extract/delete.
There are TWO distinct `SignUpItemDto` types. (a) The `[Obsolete]` **class** in `Common/SignUpListDto.cs` — already inside the cluster file being promoted, NOT dead (wired via `[JsonDerivedType(...,"legacy")]`, emitted by `ExportEventAttendeesQueryHandler`, pattern-matched by both exporters, covered by `CsvExportServiceSignUpListsTests`). Rides along with the cluster to LegacyPromotions; no extraction. (b) A different, non-`[Obsolete]` **record** in `Commands/CreateSignUpListWithItems/CreateSignUpListWithItemsCommand.cs` — a write-side command-input payload, NOT a query-response DTO, NOT consumed by exporters; **stays in Application**. Shared simple-name is safe (different namespaces; scoped/qualified references).

### Q3 — MANDATORY in commit 1 (load-bearing, not on-touch hygiene).
Relocate the 4 email-repo interfaces `Communications.Application/Contracts/` → `Communications.Contracts/LegacyPromotions/` (preserve their existing `LankaConnect.BuildingBlocks.Application.Common.Interfaces` namespace → zero consumer churn). Rationale: once the repo impls move into `Communications.Infrastructure`, they must reach the interfaces, but `Communications.Infrastructure` CANNOT reference `Communications.Application` (reverse edge deleted Day 4; re-adding it against the existing Application→Infrastructure edge = an instant 2-node cycle inside Communications — strictly worse). `Communications.Contracts` is already referenced by Communications.Infrastructure and already references Communications.Domain. Zero DTO cascade (interfaces return Domain entities + `Result<T>` + the `EmailQueueStats` domain VO).

## Commit split — CONFIRMED (Q3 folded into commit 1)

1. **Communications cycle-break:** 4 interfaces → `Communications.Contracts/LegacyPromotions/` (preserve ns) + 4 repo impls → `Communications.Infrastructure/Data/Repositories/` + drop `LC.Infra → Communications.Application` PR.
2. **LankaEvents cycle-break:** 9 DTO files → `LankaEvents.Contracts/LegacyPromotions/` (preserve ns) + 2 export impls → `LankaEvents.Infrastructure/Services/Export/` (add ClosedXML + CsvHelper package refs to that csproj) + drop `LC.Infra → LankaEvents.Application` PR.
3. **4C.h:** delete `IApplicationDbContext` + add Rule 14 ArchTest forbidden-type rule.

**Gate each:** `dotnet build LankaConnect.sln` = 0 errors AND `dotnet restore --force` cold-run zero circular-dependency errors. Commit 1 must independently clear the Communications MSB4006 cycle; commit 2 the LankaEvents one. If either doesn't clear its own cycle in isolation, the split is wrong → STOP + re-consult. Run Rule 14 ArchTest + full `Run-Wave9` smoke only after commit 3.

## Pre-flip evidence caveat

The Consult #17 mandatory `grep "using {Module}.Application"` is valid for LankaEvents (both export files show `using ...LankaEvents.Application.Common`) but **BLIND for Communications** — the email interfaces use the `BuildingBlocks.Application.Common.Interfaces` namespace while compiling into the Communications.Application *assembly*, so a namespace-grep returns zero even though the assembly edge is real. Authoritative pre-flip evidence for commit 1 = **"LC.Infrastructure builds green with the Communications.Application PR removed."** Paste that in the commit body.

## Live-prod risk register (zero-runtime-change is a claim to actively defend)

1. **DI resolution — #1 risk.** All 6 impls are registered in Host DI by interface. Namespace preservation should leave registration lines textually unchanged — VERIFY, and confirm no impl is now registered from two assemblies. A silent miss surfaces as a 500 on email-send / queue-processing / export download, not at build time.
2. **Post-deploy smoke mandatory** despite "zero runtime change" — cross-assembly impl moves are exactly the class that builds green and breaks at resolve-time. S1 (GET export → 200 + non-empty bytes) + S6 (email-queue enqueue → processed via Communications write path) + container-log silence. HTTP 200 alone insufficient (Section 13). Runs at the Day-6 develop→staging gate (Days 2-6 bypass defers per-commit staging smoke).
3. **Namespace≠assembly debt** (files namespaced `Application.*` living in `Contracts` assemblies) is deliberate — annotate in every LegacyPromotions header comment + commit body + a Day-10 TRACEABILITY_MATRIX row, else it becomes invisible drift.
4. **Cold-restore ordering:** clear `obj/` + `project.assets.json` before each `restore --force`; MSB4006 is an incremental-cache artifact and a warm restore can mask a still-cyclic graph.
