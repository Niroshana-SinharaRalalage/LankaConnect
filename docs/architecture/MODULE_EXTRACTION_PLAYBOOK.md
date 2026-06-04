# Module Extraction Playbook

**Audience**: anyone extracting a new module into `src/Modules/<Name>/` during Phase A.
**Authority**: this document is the live playbook captured from the W3 Notifications extraction (2026-06-02 → 2026-06-04, commits `c82a4b40` → `ea0b0313`). Subsequent module extractions MUST follow this sequence; deviations require a documented amendment to the [Master TODO — Phase A](../MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md).

---

## Why Phase A exists (in one paragraph)

The platform composes **shared infrastructure modules** (Notifications, Communications, Media, Forms, Payments, Events, Identity — the 7 Phase A modules) and **product modules** that consume them. Today's product is **LankaEvents**. The Phase 2+ product roadmap adds **LankaSeyla / LankaMart / LankaNivasa** (Commerce storefronts per [ADR-002](ADR-002-tenancy-strategy.md)), **LankaBusiness** (Directory), **LankaHomes** (real-estate listings — Phase 2 product addition), **LankaTemples** (community / religious surface — Phase 2 product addition), and more. Each new product module composes the shared 7 with strict boundaries enforced by [LayeringRules.cs](../../tests/architecture/LankaConnect.ArchitectureTests/LayeringRules.cs). Microservice extraction post-Phase B is enabled by [BuildingBlocks.Contracts](../../src/BuildingBlocks/BuildingBlocks.Contracts/) (cross-module ABI), per-module DbContext, the outbox pattern, and replaceable [IIntegrationEventDispatcher](../../src/BuildingBlocks/BuildingBlocks.Contracts/IntegrationEvents/IIntegrationEventDispatcher.cs) (MediatR today → Service Bus later).

## The 10-step extraction sequence

Each step is its own commit. Smaller commits = better rollback granularity.

### Step 1 — Module skeleton (W3.1 pattern)

Create empty shells. NO code moves yet.

| File | Purpose |
|---|---|
| `src/Modules/<Name>/<Name>.Domain.csproj` | depends on `BuildingBlocks.Domain` only |
| `src/Modules/<Name>/<Name>.Contracts.csproj` | depends on `BuildingBlocks.Contracts` only (cross-module ABI) |
| `src/Modules/<Name>/<Name>.Application.csproj` | depends on Domain + Contracts + `BuildingBlocks.Application` + MediatR + FluentValidation |
| `src/Modules/<Name>/<Name>.Infrastructure.csproj` | depends on Application + Domain + Contracts + `BuildingBlocks.Infrastructure` + EF Core + Npgsql |
| `src/Modules/<Name>/<Name>.Api.csproj` | depends on Application + Domain + Contracts + Infrastructure + `BuildingBlocks.Web` + `Microsoft.AspNetCore.App` FrameworkReference |
| `tests/Modules/<Name>/<Name>.Domain.Tests.csproj` | mirror Domain |
| `tests/Modules/<Name>/<Name>.Application.Tests.csproj` | mirror Application |
| `tests/Modules/<Name>/<Name>.Infrastructure.Tests.csproj` | mirror Infrastructure + Testcontainers |
| `tests/Modules/<Name>/<Name>.Api.Tests.csproj` | mirror Api + `Microsoft.AspNetCore.Mvc.Testing` |

Each source project gets one `AssemblyMarker.cs` placeholder that anchors NetArchTest until real types arrive. Add all 9 to `LankaConnect.sln`.

Add 4 NetArchTest layering rules to [LayeringRules.cs](../../tests/architecture/LankaConnect.ArchitectureTests/LayeringRules.cs) (anchored on the AssemblyMarker for now):
- `Modules_<Name>_Domain_DoesNotDependOnLayeredMonolithOrOtherModules`
- `Modules_<Name>_Application_DoesNotDependOnInfraOrWebOrLayeredMonolith`
- `Modules_<Name>_Contracts_DependsOnlyOnBuildingBlocksContracts`
- `Modules_<Name>_Infrastructure_DoesNotDependOnApiOrWebOrLayeredMonolith`

Verify: `dotnet build` green; ArchTest count grew by 4.

### Step 2 — Move domain types (W3.2 pattern)

Move all `src/LankaConnect.Domain/<Name>/*.cs` files to `src/Modules/<Name>/<Name>.Domain/`. Update namespaces from `LankaConnect.Domain.<Name>` to `LankaConnect.Modules.<Name>.Domain`. Remove the AssemblyMarker placeholder; switch the corresponding ArchTest rule's anchor to a real type.

**Caller updates**: grep `using LankaConnect.Domain.<Name>` across `src/` and `tests/`, replace with `using LankaConnect.Modules.<Name>.Domain`. For Notifications this was ~11 sites. For Events (W7-W9.5) this will be hundreds — use sed.

Add a ProjectReference from `LankaConnect.Application.csproj` → `<Name>.Domain.csproj` so callers compile. Infrastructure + API + tests get it transitively.

**Transitional debt**: `<Name>.Domain.csproj` likely needs a temporary ProjectReference to `LankaConnect.Domain` for `BaseEntity` + `Result` + `IRepository<T>` + `IDomainEvent` (these primitives haven't elevated to `BuildingBlocks.Domain` yet). Document the edge in the csproj + relax the corresponding ArchTest rule to permit `LankaConnect.Domain` ONLY. Cut the edge in the pre-W12 [BuildingBlocks elevation pass](../../docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md).

Verify: full sln build green; ArchTest green; module-specific unit tests still pass.

### Step 3 — Define Contracts (W3.3 pattern)

Three types in `src/Modules/<Name>/<Name>.Contracts/`:

1. **`I<Name>Dispatcher`** — cross-module publish API for module authors. Takes CLR primitives only (no domain entities leak). Signature is `Task <DoVerb>Async(<primitives>, CancellationToken)`.
2. **`<Verb>IntegrationEventV1`** — `sealed record` deriving from `IntegrationEventBase` (from `BuildingBlocks.Contracts`) and implementing `IIntegrationEventV1`. Carries CLR primitives + a Contracts-local enum if needed. Other modules subscribe to this via MediatR + outbox.
3. **`<Kind>Kind`** (enum, if dispatcher takes a discriminator) — mirrors the Domain enum 1-for-1 by ordinal value but intentionally duplicated so the wire-format ABI decouples from internal domain evolution.

Add 5-10 contract-shape pinning tests to `tests/Modules/<Name>/<Name>.Application.Tests/Contracts/`. These verify: interface shape, parameter primitive-only check, integration event inheritance chain, kind-enum ordinal parity with the domain enum, default-init record values.

### Step 4 — Move Application + Infrastructure (W3.4 pattern)

This is the biggest single sub-step.

**Application moves**: every command + query + handler + DTO from `src/LankaConnect.Application/<Name>/` to `src/Modules/<Name>/<Name>.Application/`. Same folder structure (`Commands/<Op>/`, `Queries/<Op>/`, `DTOs/`). Namespaces flip from `LankaConnect.Application.<Name>` to `LankaConnect.Modules.<Name>.Application`.

**Infrastructure moves**: `<Name>Repository.cs` from `src/LankaConnect.Infrastructure/Data/Repositories/` to `src/Modules/<Name>/<Name>.Infrastructure/Repositories/`. The EF Core entity configuration (e.g. `NotificationConfiguration.cs`) **STAYS in legacy `LankaConnect.Infrastructure/Data/Configurations/`** to avoid an `Infrastructure ↔ <Name>.Infrastructure` cycle. The module Infrastructure consumes it via the existing legacy ProjectReference.

**NEW: `<Name>DbContext.cs`** in `<Name>.Infrastructure/Data/`. Derives from `DbContext` directly (BaseDbContext requires the entity to implement `IAuditable` / `ISoftDeletable` — defer that retrofit). Default schema = lowercase `<name>`. **CRITICAL**: explicitly `ToTable("<lowercase_table_name>", "<lowercase_schema>")` in `OnModelCreating`. EF convention generates PascalCase otherwise → snapshot drift vs production.

**NEW: `<Name>.Api/<Name>Module.cs`** (pulled FORWARD from Step 6 to break the registration cycle). Provides `Add<Name>Module(IServiceCollection, IConfiguration)` extension. Registers `<Name>DbContext` + module repositories. **Must also register MediatR handlers from the module assembly** — the host's outer `AddApplication(...)` only scans `LankaConnect.Application.dll`:

```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(SomeHandlerFromModule).Assembly));
```

**Host wiring**: `LankaConnect.API/Program.cs` calls `builder.Services.Add<Name>Module(builder.Configuration)` AFTER `AddInfrastructure(...)`. `LankaConnect.API.csproj` ProjectReferences `<Name>.Api.csproj`.

**Caller updates**: MediatR's existing assembly scan over `LankaConnect.Application` finds the moved handlers transitively (Application → `<Name>.Application` → `<Name>.Domain`). No controller / call-site code changes needed YET. Update only `using` directives that explicitly reference the old namespace.

**Transitional debt to document in csprojs**:
- `<Name>.Application` references `LankaConnect.Application` for `ICommand` / `ICommandHandler` / `ICurrentUserService` / `IUnitOfWork`
- `<Name>.Infrastructure` references `LankaConnect.Infrastructure` for `Repository<T>` base + `AppDbContext`

Both cut in the pre-W12 elevation pass.

Verify: full sln build green; ArchTest green; legacy `<Name>`-related unit tests in `LankaConnect.Application.Tests` still pass; new `<Name>.Application.Tests` 6+ Contracts-shape tests green.

### Step 5 — Empty-Up baseline migration (W3.5a pattern)

For modules whose entities ALREADY have physical tables in production:

1. Add `Microsoft.EntityFrameworkCore.Design` package to `<Name>.Infrastructure.csproj`.
2. Create `<Name>.Infrastructure/Data/<Name>DbContextDesignTimeFactory.cs` implementing `IDesignTimeDbContextFactory<<Name>DbContext>` with a placeholder Npgsql connection string (NEVER opened — used for design-time model inspection only). Required because `Program.cs` throws on empty conn string at design time.
3. Generate the baseline:
   ```bash
   dotnet ef migrations add Baseline_<Name> \
     --context <Name>DbContext \
     --project src/Modules/<Name>/<Name>.Infrastructure \
     --startup-project src/Modules/<Name>/<Name>.Infrastructure \
     --output-dir Migrations
   ```
4. **CRITICAL**: verify the companion `.Designer.cs` file exists AND contains `[Migration("<timestamp>_Baseline_<Name>")]` at line ~15. Per [MEMORY.md hand-create rule](../../../Users/Niroshana/.claude/projects/c--Work-LankaConnect/memory/MEMORY.md), without Designer.cs the `[Migration]` attribute is missing and `dotnet ef database update` silently ignores the migration.
5. Empty the generated `Up()` and `Down()` method bodies. Add class-level remarks documenting the manual deployment step:
   ```sql
   INSERT INTO <name>."__EFMigrationsHistory" (migration_id, product_version)
   VALUES ('<timestamp>_Baseline_<Name>', '8.0.19');
   ```
   (EF only writes the history row when it RUNS Up(), which is empty here → manual insertion needed per environment.)

For modules whose entities DON'T have physical tables yet (greenfield), skip the empty-Up — let EF generate the real `CreateTable(...)` calls and apply via `dotnet ef database update`.

Verify: build green; the new migration files are committed (Designer.cs is NOT optional — see MEMORY rule).

### Step 5b — Operational tables (W3.5b pattern)

Every module needs three per-schema operational tables:

| Table | Backed by | Purpose |
|---|---|---|
| `<schema>.idempotency_keys` | `IdempotencyKey` (BuildingBlocks.Infrastructure) | per-module idempotency store |
| `<schema>.outbox` | `OutboxMessage` (BuildingBlocks.Infrastructure) | per-module outbox for integration events |
| `<schema>.outbox_dead_letter` | `DeadLetterMessage` (BuildingBlocks.Infrastructure) | dead-letter for failed dispatches |

Configuration classes (`IdempotencyKeyConfiguration`, `OutboxMessageConfiguration`, `DeadLetterMessageConfiguration`) live in `BuildingBlocks.Infrastructure` and are reusable across all modules. The `<Name>DbContext` declares three `DbSet<>` properties and applies the three configurations in `OnModelCreating`.

Generate a SECOND migration `Add_<Name>OperationalTables` (NOT bundled with the baseline — keep the baseline a true history-only marker). Indexes:
- `idempotency_keys`: index on `ExpiresAt` for TTL sweep
- `outbox`: partial index on `OccurredAt` filtered `WHERE "ProcessedAt" IS NULL` (processor hot path)
- `outbox_dead_letter`: indexes on `OriginalOutboxId` (replay) + `DeadLetteredAt` (alert queries)

### Step 6 — Move controller (W3.6 pattern)

Move `<Name>Controller.cs` from `src/LankaConnect.API/Controllers/` to `src/Modules/<Name>/<Name>.Api/Controllers/`. Namespace flips to `LankaConnect.Modules.<Name>.Api.Controllers`.

**The controller MUST inherit `ControllerBase` directly**, NOT the legacy `LankaConnect.API.Controllers.BaseController<T>` — inheriting it closes a hard cycle `LankaConnect.API → <Name>.Api → LankaConnect.API`. Inline the ~30 LOC of helpers (`HandleResult`, `HandleResultUnit`, `BuildProblem`, `TryGetUserId`) at the bottom of the controller class. Future work elevates a reusable `ModuleControllerBase` into `BuildingBlocks.Web`.

**Add missing usings explicitly**: module project uses `Microsoft.NET.Sdk` (not `Microsoft.NET.Sdk.Web`), so implicit usings are minimal. Common addition: `using Microsoft.AspNetCore.Http;` for `StatusCodes`.

**Add `IFeatureManager` to the constructor** + log the flag value at the top of every endpoint method:
```csharp
var useNewModule = await _featureManager.IsEnabledAsync(FlagName);
_logger.LogInformation("... (UseNewModule={UseNewModule})", useNewModule);
```
End-to-end visibility for the W3.8 soak.

**Host wiring**: in `LankaConnect.API/Program.cs`, chain `.AddApplicationPart(typeof(<Name>Controller).Assembly)` onto the `AddControllers(...)` call so MVC discovers the controller from the referenced module assembly.

**Transitional edges added to `<Name>.Api.csproj`**:
- `Microsoft.FeatureManagement.AspNetCore` package
- `LankaConnect.Domain` ProjectReference (for `Result<T>` — cut alongside the elevation pass)
- `MediatR` package (for the `RegisterServicesFromAssemblies` call in `<Name>Module`)

### Step 7 — Feature flag registry (W3.7 pattern)

1. Add `Refactor.<Name>.UseNewModule: false` to `src/LankaConnect.API/appsettings.json` `FeatureManagement` section.
2. Add a registry row to [docs/feature-flags.md](../feature-flags.md) per the existing column structure. Sunset = current week + 4 (Refactor.* category convention).
3. Description states the legacy → new path swap the flag will eventually gate.

**Pragmatic note**: during W3 the flag had NO behavioral effect (new and legacy paths produce identical SQL). The flag's purpose was procedural + observation hook. **Real dual-path flags land starting W4** where modules genuinely diverge (Communications has Mode A/B/C templates; Payments has Stripe-vs-legacy path; etc.).

### Step 8 — Staging deploy + flag soak (W3.8 pattern)

Push triggers `deploy-staging.yml`. After deploy:

1. **Smoke flag-OFF (default)**:
   ```bash
   TOKEN=$(curl ... /api/Auth/login | jq -r .accessToken)
   curl -H "Authorization: Bearer $TOKEN" $STAGING/api/<Name>/<endpoint>
   ```
   Expected: HTTP 200, body matches W2.8 baseline shape.

2. **Verify controller registered + reached**:
   ```bash
   az containerapp logs show -n lankaconnect-api-staging -g lankaconnect-staging --tail 200 \
     | grep "EndpointMiddleware: Executing endpoint" | grep "<Name>.Api.Controllers"
   ```

3. **Verify flag pipeline**: log line `UseNewModule=False` appears for every endpoint hit.

4. **Run API baseline regression** to prove no structural drift:
   ```bash
   bash tests/api-baseline/run-baseline-regression.sh
   ```
   Expected: `OK — no breaking drift`.

5. **Flip flag ON** via Container App env var (dotted key works in Container Apps env-var names):
   ```bash
   az containerapp update --name lankaconnect-api-staging --resource-group lankaconnect-staging \
     --set-env-vars "FeatureManagement__Refactor.<Name>.UseNewModule=true"
   ```
   Wait for new revision to come up healthy.

6. **Smoke flag-ON**: same curl. Expected: HTTP 200, same body shape; container log shows `UseNewModule=True`.

7. **7-day soak**: monitor App Insights for error-rate deltas + p50/p95/p99 latency deltas vs the W2.6b baseline. Acceptance per master TODO: zero error rate increase, latency within 10%.

8. **After 7 days clean**: leave the flag default-false in appsettings (production canary uses Container Apps revision-based traffic split per ADR-004, NOT the flag percentage filter). The flag retires in the W7-equivalent cleanup PR.

### Step 9 — Update the playbook (W3.9 pattern)

This document. After each module's W3.9-equivalent step, scan this playbook for gaps and add what THAT module's extraction taught — pitfalls, version skews, surprising EF behavior, etc. The playbook is a living document; each module makes it sharper for the next.

### Step 10 — Update tracking docs (W3.10 pattern)

Update the three primary tracking docs per [CLAUDE.md §"Documentation Synchronization"](../../CLAUDE.md):
- [PROGRESS_TRACKER.md](../PROGRESS_TRACKER.md): "Latest" entry summarising the extraction
- [STREAMLINED_ACTION_PLAN.md](../STREAMLINED_ACTION_PLAN.md): Friday-of-week rollup
- [Master TODO — Phase A](../MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md): mark every sub-step `[x]` with verification evidence

Close the week on the dashboard at top of the master TODO.

---

## Module map (current + planned)

### Phase A shared infra modules (the 7 we extract during W3-W10)

| # | Module | Week | Status | Notes |
|---|---|---|---|---|
| 1 | Notifications | W3 | ✅ extracted (this playbook is its output) | lowest fan-in; sets the pattern |
| 2 | Communications | W4.1 | pending | Email + WhatsApp + Newsletter; 41 typed email-param classes move to Contracts |
| 3 | Media | W4.2 | pending | Photo Albums |
| 4 | Forms | W5 | pending | generalize ownership; Events handlers call via Contracts |
| 5 | Payments | W6 | pending | generic CheckoutSession abstraction + PaymentSettledIntegrationEventV1 |
| 6 | Events | W7-W9.5 | pending | the big one; 60+ domain files, 441 application files, 40+ handlers; absorbed 21 migrations during the May main→develop merges |
| 7 | Identity | W10 | pending | CANARY ORDER MATTERS — Identity ships LAST per architect verdict (flipping Identity first breaks everything) |

### Phase 2+ product modules (consume the shared 7)

| Product module | Composes shared modules | Status |
|---|---|---|
| **LankaEvents** | Notifications + Communications + Media + Forms + Payments + Events + Identity | live today; this is the revenue path |
| **LankaSeyla** (Commerce — clothing storefront) | Notifications + Communications + Media + Identity + Payments + new Commerce module | Phase 3 per [ADR-002](ADR-002-tenancy-strategy.md) |
| **LankaMart** (Commerce — grocery storefront) | same as LankaSeyla, different `storefront_id` | Phase 3 per [ADR-002](ADR-002-tenancy-strategy.md) |
| **LankaNivasa** (Commerce — home-goods storefront) | same as LankaSeyla, different `storefront_id` | Phase 3 per [ADR-002](ADR-002-tenancy-strategy.md) |
| **LankaBusiness** (Directory) | Notifications + Communications + Media + Identity + new Directory module | Phase 2+ |
| **LankaHomes** (real-estate listings) | Notifications + Communications + Media + Identity + Forms + new LankaHomes module | Phase 2+ — DOES NOT belong in Commerce module (different tenancy shape) |
| **LankaTemples** (community / religious surface) | Notifications + Communications + Media + Identity + Forms + new LankaTemples module | Phase 2+ — closer to Directory + content shape |

Adding a new Phase 2+ product module follows THIS playbook end-to-end — no Phase A change required. The shared foundation already supports them.

---

## Microservice readiness checklist (for Phase B+ extraction)

When a Phase A module is ready to become a microservice:

1. **Owns its DbContext + its own schema + its own migrations history** — yes per Step 4 + Step 5
2. **Owns its public ABI via Contracts** — yes per Step 3
3. **Dispatches integration events through outbox** — yes per Step 5b + the abstract `IIntegrationEventDispatcher`
4. **Has zero direct references to LankaConnect.{Application,Infrastructure,API,Shared}** — pending the BuildingBlocks elevation pass (Step 4's transitional debt)
5. **Can be hosted in its own `Host.<Name>` container** — pending W11+ host split work

When all 5 are checked, extraction = (a) lift the module's csprojs into a separate repo, (b) point its DbContext at its own Postgres, (c) swap the `IIntegrationEventDispatcher` registration from the AllInOne MediatR impl to the Service Bus impl.

---

## Common pitfalls (caught during W3)

| Pitfall | Symptom | Fix |
|---|---|---|
| `git add -A` sweeps untracked large files | Push rejected by GitHub pre-receive hook (>100MB) | Add the offending paths to `.gitignore` BEFORE staging |
| `EF convention generates PascalCase table name` | Snapshot drift vs lowercase production table | Explicit `ToTable("lowercase_name", "lowercase_schema")` in DbContext.OnModelCreating |
| `dotnet ef migrations add` fails: "appsettings.json not found" | Design-time tools try to boot Program.cs | Create `IDesignTimeDbContextFactory<>` with a placeholder conn string |
| `dotnet ef migrations add` fails: "InvalidConnectionString" inside Program.cs | Host's `AddInfrastructure` calls `new NpgsqlDataSourceBuilder(emptyString)` | Same — use `IDesignTimeDbContextFactory<>` (which doesn't boot the host) |
| `Module.Application → Module.Infrastructure → LankaConnect.Infrastructure → Module.Infrastructure` cycle on Repository<T> | Compile error: assembly reference cycle | Register module DbContext + repository inside `<Name>Module.Add<Name>Module` (Notifications.Api), NOT in LankaConnect.Infrastructure.DependencyInjection |
| `LankaConnect.API → Module.Api → LankaConnect.API` cycle on BaseController | Compile error: assembly reference cycle | Inherit ControllerBase directly + inline the 4 helpers (or elevate to `BuildingBlocks.Web.ModuleControllerBase` as a follow-up) |
| `Microsoft.NET.Sdk` implicit usings missing | Compile error: `StatusCodes` not found | Add `using Microsoft.AspNetCore.Http;` explicitly |
| `MediatR can't find module handler` | Runtime: `No service for type 'MediatR.IRequestHandler<...>'` | `<Name>Module.Add<Name>Module` must also call `AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(SomeHandler).Assembly))` — host's outer `AddApplication` only scans `LankaConnect.Application.dll` |
| `Container App --replace-env-vars` wipes manual env-var edits | Manual `set-env-vars` lost on next CI deploy | Add the env var binding (with `secretref:` for sensitive values) to `deploy-staging.yml` — see W2.6b commit `e9c508d0` |
| `appsettings.json FeatureManagement env-var override` syntax | Flag flip via `set-env-vars` doesn't take effect | Use dotted key: `FeatureManagement__Refactor.<Name>.UseNewModule=true` (double-underscore for the section separator, dots in the flag name) |

---

## References

- [Master TODO — Phase A](../MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md) — canonical 20-week schedule
- [ADR-002 — Tenancy Strategy](ADR-002-tenancy-strategy.md) — Commerce module row-level multi-tenant; explicitly names LankaSeyla / LankaMart / LankaNivasa
- [ADR-004 — Feature Flag Strategy](ADR-004-feature-flag-strategy.md) — `Refactor.*` flag conventions
- [feature-flags.md](../feature-flags.md) — live flag registry
- [LayeringRules.cs](../../tests/architecture/LankaConnect.ArchitectureTests/LayeringRules.cs) — NetArchTest enforcement of module boundaries
- W3 Notifications commits (this playbook's source): `c82a4b40` (W3.1), `b3498656` (W3.2), `030b2e38` (W3.3), `3d4fa987` (W3.4 + W3.5a), `bf3fa802` (W3.5b), `2673a319` (W3.6 + W3.7), `ea0b0313` (W3.8 MediatR fix)
