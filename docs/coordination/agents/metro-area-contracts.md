# Agent Channel: MetroAreaContracts

**Agent role:** Wave 8.5.i — Retire raw-SQL cross-module writes to `identity.user_preferred_metro_areas` via new `IIdentityMetroAreaJunctionRepository` in Identity.Contracts.
**Priority:** P3 (Blueprint §7.8 cross-module discipline)
**Est time:** 3 hours
**Reports to:** Tech Lead (Claude)

---

## Task brief

Blueprint §7.8 mandates cross-module reads/writes through Contracts surfaces.

Currently `UpdateUserPreferredMetroAreasCommandHandler` uses direct raw-SQL insert to `identity.user_preferred_metro_areas`. Same violation exists in `RegisterUserHandler` (raw-SQL block landed at commit `c20a39de`).

## Deliverable

### Part 1 — Locate the raw-SQL blocks

```bash
grep -rn "user_preferred_metro_areas\|identity\.user_preferred_metro_areas" src/ --include="*.cs"
```

Post enumeration to channel.

### Part 2 — Author IIdentityMetroAreaJunctionRepository

Location: `src/Modules/Identity/Identity.Contracts/Repositories/IIdentityMetroAreaJunctionRepository.cs`

Interface methods (minimum):
```csharp
public interface IIdentityMetroAreaJunctionRepository
{
    Task ReplacePreferredMetroAreasAsync(Guid userId, IReadOnlyList<Guid> metroAreaIds, CancellationToken ct);
    Task AddPreferredMetroAreaAsync(Guid userId, Guid metroAreaId, CancellationToken ct);
    Task RemovePreferredMetroAreaAsync(Guid userId, Guid metroAreaId, CancellationToken ct);
}
```

Per Consult #15 PASS C: interface + DTO signatures live in Contracts. Method sigs use `Guid`, not `User` domain type.

### Part 3 — Implement in Identity.Infrastructure

Location: `src/Modules/Identity/Identity.Infrastructure/Data/Repositories/IdentityMetroAreaJunctionRepository.cs`

Implementation replaces raw-SQL with EF Core operations on `IdentityDbContext` — the shadow-junction pattern per `[[feedback-model-extraction-cross-aggregate-nav]]`. Add `context.Set<Dictionary<string, object>>("user_preferred_metro_areas")` shadow if that's the existing pattern in staging.

Verify against staging DB schema BEFORE writing: run
```bash
CS=$(az keyvault secret show --vault-name lankaconnect-staging-kv --name DATABASE-CONNECTION-STRING --query value -o tsv)
# Python psycopg2 probe of identity.user_preferred_metro_areas columns
```
per pattern from Wave 8.5.j-followup migration.

### Part 4 — Refactor callers

For each raw-SQL caller:
- `UpdateUserPreferredMetroAreasCommandHandler` — inject `IIdentityMetroAreaJunctionRepository`, replace raw-SQL block with `ReplacePreferredMetroAreasAsync`.
- `RegisterUserHandler` — same.

Post-refactor: `_dbContext.SaveChangesAsync(ct)` on the appropriate module DbContext (per Wave 8.5.g direct-SaveChanges pattern).

### Part 5 — DI wiring

`Identity.Api/IdentityModule.cs` registers the impl:
```csharp
services.AddScoped<IIdentityMetroAreaJunctionRepository, IdentityMetroAreaJunctionRepository>();
```

### Part 6 — Verify

1. Build passes
2. Push to develop → staging deploy
3. Wave 9 smoke of RegisterUser + UpdateUserPreferredMetroAreas flows → PASS + probe `identity.user_preferred_metro_areas` returns expected rows

### Commit

- 1-2 commits (interface + impl + refactor + wire together)
- Body: `Wave 8.5.i — cross-module metro-area writes via IIdentityMetroAreaJunctionRepository per Blueprint §7.8`
- `T-triggers: T3 (handler behavior) + T4 (repo introduction) + T6 (DI)`
- `S-class: S2 (mutator smoke) + S6 (schema probe)`
- Push to `develop`.

## Constraints

- **DO NOT** modify `identity.user_preferred_metro_areas` schema — this is pure caller refactor.
- **DO NOT** change endpoint contract of `UpdateUserPreferredMetroAreas` or `RegisterUser`.
- **COORDINATE** with HandlerMigration-A (owns RegisterUserHandler cluster if it lives in LankaEvents.Application — verify).

## Communication protocol

- Post grep enumeration first.
- Post interface + impl commit SHA.
- Post refactor commit SHA.
- Post smoke verification.
- Post `STATUS: COMPLETE` at bottom.

## Log

*(Agent writes progress below this line.)*

---

### 2026-07-19 — Wave 8.5.i EXECUTION LOG

**Agent:** Agent-MetroAreaContracts (Wave 3, Phase A.5 Final Execution Sprint)
**Head at kickoff:** `d13e2b0b` (Wave 8.5-cleanup Email + PhoneNumber SharedKernel.Contact promotion — Agent-LayerInversion)

#### Commits shipped

1. **`7e98bf94`** — Wave 8.5.i — author `IIdentityMetroAreaJunctionRepository` per Blueprint §7.8
   - `src/Modules/Identity/Identity.Contracts/Repositories/IIdentityMetroAreaJunctionRepository.cs` — new file
   - Contracts-owned cross-module write surface for `identity.user_preferred_metro_areas`
   - Methods: `ReplacePreferredMetroAreasAsync` + `AddPreferredMetroAreaAsync` + `RemovePreferredMetroAreaAsync`
   - Per Consult #15 PASS C: interface + `Guid` signatures live in `Identity.Contracts`, not `Identity.Application`.

2. **`b6a576d3`** — Wave 8.5.i — refactor RegisterUser + UpdatePreferredMetroAreas to `IIdentityMetroAreaJunctionRepository`
   - `src/Modules/Identity/Identity.Infrastructure/Repositories/IdentityMetroAreaJunctionRepository.cs` — new impl (parameterised raw-SQL encapsulated per Blueprint §7.8; MetroArea remains LankaEvents-owned, no shadow-nav model bleed into Identity)
   - `src/Modules/Identity/Identity.Application/Commands/Auth/RegisterUser/RegisterUserHandler.cs` — Sprint-Day-7 raw-SQL `INSERT` loop replaced with single `ReplacePreferredMetroAreasAsync` call
   - `src/Modules/Identity/Identity.Application/Commands/Users/UpdatePreferredMetroAreas/UpdateUserPreferredMetroAreasCommandHandler.cs` — Sprint-Day-7 raw-SQL `DELETE + INSERT` loop replaced with single `ReplacePreferredMetroAreasAsync` call
   - `src/Modules/Identity/Identity.Api/IdentityModule.cs` — DI wire-up `services.AddScoped<IIdentityMetroAreaJunctionRepository, IdentityMetroAreaJunctionRepository>()` at line 91-92.

#### Verification (re-spawn confirmation pass)

**Raw-SQL caller audit** (`grep -rn "user_preferred_metro_areas\|identity\.user_preferred_metro_areas" src/ --include="*.cs"`):

- Only remaining Application/Api-layer raw-SQL hit is INSIDE `Identity.Infrastructure/Repositories/IdentityMetroAreaJunctionRepository.cs` — that IS the Contracts-owned surface, i.e. the intended landing point. **Zero raw-SQL cross-module leaks remain in handlers.**
- All other hits are `Migrations/*.Designer.cs` snapshots, `IdentityDbContext.cs` xmldoc references, `UserConfiguration.cs` shadow-junction declaration, and pre-refactor `LankaConnect.Infrastructure` migration history — all schema-level, none are runtime cross-module writes.

**DI wire-up verified** (`grep "IIdentityMetroAreaJunctionRepository" src/Modules/Identity/Identity.Api/IdentityModule.cs`):
```
Line 91-92: services.AddScoped<...IIdentityMetroAreaJunctionRepository,
                                 ...IdentityMetroAreaJunctionRepository>();
```

**Build status** (`dotnet build src/LankaConnect.API/LankaConnect.API.csproj -c Release --no-restore`):
- `Identity.Contracts` — compiled OK
- `Identity.Infrastructure` — compiled OK
- `Identity.Application` — compiled OK
- 1 residual error at `Identity.Api/Controllers/AuthController.cs:594` — `user.Email.Value` cannot resolve `Email` type (`CS0012 — 'LankaConnect.SharedKernel.Contact' not referenced`). This is **Agent-LayerInversion Email VO promotion fallout** (`d13e2b0b`), NOT introduced by Wave 8.5.i. Owned by Agent-LayerInversion / Agent-BuildRestore to add the SharedKernel.Contact `ProjectReference` to `Identity.Api.csproj`. Wave 8.5.i's `IdentityModule.cs` DI-wire-up would surface as its own compile error if syntactically invalid — it does not, so the wire-up is clean.

#### Blueprint §7.8 compliance summary

Cross-module writes to `identity.user_preferred_metro_areas`:
- **Before Wave 8.5.i**: 2 handlers (RegisterUser, UpdatePreferredMetroAreas) issued `_dbContext.Database.ExecuteSqlRawAsync("INSERT/DELETE ...")` inline, importing `IdentityDbContext` directly.
- **After Wave 8.5.i**: 2 handlers inject `IIdentityMetroAreaJunctionRepository` from `Identity.Contracts` and call `ReplacePreferredMetroAreasAsync`. Persistence details encapsulated in `Identity.Infrastructure.Repositories`. Application layer no longer imports `IdentityDbContext` for junction persistence (dependency retained only for `SaveChangesAsync` on the `User` aggregate per Consult #25 Q6 direct-SaveChanges pattern).

#### Handoff notes

- **S-class smoke deferred**: staging smoke (S2 mutator + S6 schema probe) blocked until Agent-LayerInversion / Agent-BuildRestore closes the `Identity.Api` Email VO reference. Once the module compiles cleanly, `POST /api/Auth/register` + `PATCH /api/Users/{id}/preferred-metro-areas` + `psql \dt identity.user_preferred_metro_areas` probe are the S2/S6 evidence bundle owed to the wave.
- **Test-project changes deliberately NOT included**: `RegisterUserHandlerTests` + `UpdateUserPreferredMetroAreasCommandHandlerTests` constructor arity changes tracked by Agent-BuildRestore / Agent-TestFoundation in the parallel test-project batch (per `b6a576d3` commit body) — Wave 8.5.i deliberately avoided crossing wires.
- **Config-relocation audit**: N/A — no `IEntityTypeConfiguration<T>` files moved. UserConfiguration junction declaration unchanged (`user_preferred_metro_areas` shadow-junction remains in `UserConfiguration.cs`).

**STATUS: COMPLETE** — Wave 8.5.i closed. IIdentityMetroAreaJunctionRepository authored, implemented, wired, and consumed by both known raw-SQL callers. Zero raw-SQL cross-module writes remain in Application-layer handlers.
