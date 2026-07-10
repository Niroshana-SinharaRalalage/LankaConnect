# Consult #10 Ruling — Sprint Day 4 Compile-Fix Strategy

**Author:** system-architect (consult #10, 2026-07-05)
**Context:** Day 4 hit 1,363 compile errors on `bulk-move/integration @ d33c7016`. 93% in `Host.AllInOne` placeholder csproj.
**Prior context:** Consult #9 approved 2-week bulk-move sprint. Day 1 fixed 3 real bugs. Day 2 fired 6 parallel agents (clean merge Day 3+4). Wall at compile-fix.

## Ruling: Option B+ (B with scope extension)

Move every file currently under `src/Hosts/Host.AllInOne/` back to `src/LankaConnect.API/` **EXCEPT** — while doing the reversal, audit the ~40 files and forward-route the ~15-20 module-specific ones (Services, BackgroundServices) to their correct module Infrastructure in the same commit.

**Why B, not A:**
- A rebuilds a csproj that Day 10 was already going to build (via rename). Duplicated work.
- A doesn't solve the underlying problem — manifest miscategorized module-specific services as "cross-cutting."
- A's 4-8h estimate is optimistic (~49 ProjectReferences + package version drift + namespace map re-run + residual interior refs = full day).
- B is 30 min mechanical `git mv`, collapses error surface ~93%. Surfaces real residual (~200-300) as tractable.

## Two levels of manifest mistake

**Level 1 (correctable now):** Manifest AGENT E line "AdminController, HealthController, DashboardController → `Host.AllInOne/Controllers/`" contradicted manifest line 136 ("Program.cs STAYS in `LankaConnect.API/` Day 2, renamed on Day 10"). If Program stays, its controllers stay. Two rules inconsistent; agents obeyed earlier row.

**Level 2 (deeper):** `Host.AllInOne/Services/` and `BackgroundServices/` received files that are NOT cross-cutting:
- `AlbumImageService` → Media
- `RegistrationEmailService` → Events
- `RefundReconciliationBackgroundService` → Payments
- `EmailEncryptionService` → Communications
- `SeatHoldCleanupService` → Events

Manifest cell said "Audit — file-by-file"; that audit was skipped.

## Other blockers

**(a) `LankaConnect.Shared.Tests` — 184 errors:** Move project to `Communications.Contracts.Tests`, retarget ProjectReference, rewrite two namespace prefixes (`LankaConnect.Shared.Email` → `LankaConnect.Modules.Communications.Contracts.Email`, same for WhatsApp). **45-60 min mechanical.** Do NOT delete — they encode real contract behavior.

**(b) BB.Domain canonical state — four ratified decisions:**
1. `Result` and `Result<T>` live in one file each (`Result.cs`, `Result{T}.cs`), no duplicates, no partial. Keep richer legacy version, delete the other.
2. `IDomainEvent` = pure interface in `BuildingBlocks.Domain`, NO bridge back to legacy. Delete the legacy re-declaration causing the cycle.
3. `AggregateRoot<TId>` owns `DomainEvents` (DDD-correct home). Remove from `Entity<TId>`. `new` keyword is a smell — it hides a design conflict.
4. `BusinessRule` unifies on `Error` type (not `string`). Migrate any legacy `string` message-only BusinessRule.

**(c) Namespace-map coarseness:** Do NOT re-run Phase 2 as shotgun. Sequence:
1. B+ Host revert first (drops 1,363 → ~200-300)
2. Bucket residual with `grep CS0246`
3. Add targeted rules per missing type
4. Run Phase 2 only over affected files

## Execution sequence (~4-5h to green)

1. **B+ Host revert (60-90 min):** Each subfolder in `Host.AllInOne/` — decide (a) truly cross-cutting → `LankaConnect.API/`; (b) module-specific → target module Infrastructure/Api. `git mv` each. Leave `Host.AllInOne.csproj` as placeholder.
2. **BB.Domain finalize (30 min):** Ratify four decisions above. Delete duplicates. Rebuild.
3. **Shared.Tests relocation (60 min):** Move project + retarget + rewrite two prefixes.
4. **Measure (10 min):** `dotnet build ... | grep -c ": error"`. Target: <200.
5. **Targeted namespace-map extension (60-90 min):** Bucket residual, add rules, targeted Phase 2 on affected files.
6. **Commit each step separately.** Do not conflate.

Sprint stays on plan — Day 5 window.

## Consult discipline — 6 mechanical triggers (VERBATIM)

**I MUST consult system-architect BEFORE acting when any fire:**

1. **Manifest/plan conflict:** I discover two ratified rules that contradict each other.
2. **Scale mismatch:** measured cost/error/scope exceeds the plan's stated envelope by >2x.
3. **Deletion of a ratified type/file:** I am about to `git rm` / `Remove-Item` any file whose location was named in a manifest, consult, or ratified plan.
4. **`new` / `override` / `partial` / bridging cycles:** I am about to add any of these keywords to resolve a compile error rather than to express intent.
5. **csproj shape change:** I am about to modify SDK type, add >2 PackageReferences, or add >2 ProjectReferences in one edit.
6. **Cross-module boundary decision:** I am about to place a type in a module that isn't obviously its home, or route a "cross-cutting" file into a host/shared project.

**If any trigger fires, STOP, write a Consult #N brief with options A/B/C, and wait.**

Rule 2 alone would have caught Day 4 at ~300 errors instead of 1,363. Rule 4 would have caught the `new` keyword debate. Rule 6 would have caught the Host.AllInOne routing at the moment it was decided.

## Critical files for implementation

- `c:\Work\LankaConnect\src\Hosts\Host.AllInOne\Host.AllInOne.csproj`
- `c:\Work\LankaConnect\src\LankaConnect.API\LankaConnect.API.csproj`
- `c:\Work\LankaConnect\src\Hosts\Host.AllInOne\LegacyInfrastructureDependencyInjection.cs`
- `c:\Work\LankaConnect\src\Hosts\Host.AllInOne\LegacyApplicationDependencyInjection.cs`
- `c:\Work\LankaConnect\docs\sprint\bulk-move-manifest.md`
- `c:\Work\LankaConnect\docs\sprint\namespace-map.txt`
- `c:\Work\LankaConnect\src\BuildingBlocks\BuildingBlocks.Domain\` (Result, IDomainEvent, AggregateRoot, BusinessRule)
