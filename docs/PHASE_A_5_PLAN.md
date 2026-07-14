# Phase A.5 Plan — Post-Sprint Continuation

**Status:** DRAFT — activated after 2026-07-19 (Phase A backend sprint completion)
**Author:** Claude / system-architect, 2026-07-04
**Founder ruling ratifying scope split (2026-07-04):**
> "GO. You have to stick to the plan. No deviation, no lagging..." (approval of Consult #9's Wave 7 + Wave 8 cut from the 2-week window)

## Purpose

Consult #9 (2026-07-04) proved that the 2-week bulk-move sprint is only mathematically feasible when Wave 7 (Frontend Mirror) and Wave 8 (Production Cutover) are removed from the window. Those two waves DO NOT disappear — they move to Phase A.5.

**On 2026-07-19, "Phase A complete" means backend structural refactor done.** Production still runs off the current pre-refactor branch. Frontend still points at Host.AllInOne serving the current API surface identically.

Phase A.5 exists to complete both waves in their proper scope after the sprint proves the backend refactor stable.

## Scope

### Wave 7 — Frontend Mirror (~180h, ~4-6 calendar weeks)

Turborepo workspace mirror of the backend modular structure. Independent frontend track — does NOT block Wave 8.

**Owner:** frontend team (Nirmal / whoever founder assigns). Claude assists as consulting engineer, not primary implementer.

**Slices:**
- 7.a Workspace scaffold (Turborepo + shared packages)
- 7.b Package migration by feature: events / marketplace / auth / admin / cultural / shared
- 7.c Build + config wiring
- 7.d UAT + frontend smoke

**Prerequisite:** Sprint Day 10 (Wed 2026-07-15) — Host.AllInOne serving stable API contract.

### Wave 8 — Production Cutover (~150h, ~5 calendar weeks)

Migrate production off the pre-refactor branch onto the modular-monolith branch. Blue-green cutover with rollback rehearsal.

**Owner:** DevOps + founder. Claude assists with runbook authoring.

**Slices:**
- 8.a Prod migration re-parenting audit (verify Wave 6.5.f migration history is clean vs prod)
- 8.b Blue-green environment provisioning
- 8.c Canary traffic split (10% → 50% → 100%)
- 8.d 24h prod soak + rollback rehearsal
- 8.e Legacy prod branch decommission

**Prerequisite:** Wave 7 complete OR founder ruling that frontend can continue against pre-Wave-7 API contract during cutover.

## Cadence

Phase A.5 is calendar-driven, not sprint-driven. Founder schedules Wave 7 kick-off after Sprint Day 14 (Sun 2026-07-19). Wave 8 kicks off after founder pre-cutover approval, typically 2 weeks post Wave 7 close.

## What Phase A.5 does NOT include

- Wave 4.9.1 retroactive testing gap-fill — **DELETED** in sprint MASTER_TODO surgery (Consult #9 L2)
- New feature work — Phase B territory
- Additional module extractions — Phase B territory

## Wave 8.5 — Sprint Deferred Debt (added 2026-07-14 during sprint Day 9-10)

The 2-week bulk-move sprint (2026-07-06 → 2026-07-19) delivered core structural refactor but deferred the following architectural debt to Phase A.5. All items surfaced during Days 4-10 execution and are documented with fix path so Phase A.5 can pick them up cleanly. **Est. ~2-3 weeks of Wave 8.5 work; scope-check with founder before Wave 7 kick-off.**

### 8.5.a — LankaConnect.Application csproj deletion (~4 hrs)

**5 files to relocate** before deleting the csproj:

| File | Current namespace | Suggested destination |
|---|---|---|
| `Common/Interfaces/IEntraExternalIdService.cs` | `LankaConnect.Application.Common.Interfaces` | `Identity.Contracts/Services/` (cross-module Identity auth port) |
| `Common/Interfaces/IJwtTokenService.cs` | `LankaConnect.Application.Common.Interfaces` | `Identity.Contracts/Services/` |
| `Dashboard/Queries/GetCommunityStats/GetCommunityStatsQuery.cs` | `LankaConnect.Application.Dashboard.Queries` | Consult architect — likely `Communications.Application/Queries/` OR new `Dashboard` capability module |
| `Dashboard/Queries/GetCommunityStats/GetCommunityStatsQueryHandler.cs` | (same as query) | Same as query |
| `MetroAreas/Mappings/MetroAreaMappingProfile.cs` | `LankaConnect.BuildingBlocks.Application.Common.Mappings` (namespace mismatch!) | `BuildingBlocks.Application/Common/Mappings/` (align physical to logical) OR `LankaEvents.Application/Mapping/` per MetroArea ownership |

Sprint hotfix `55ee3174` added an explicit `services.AddAutoMapper(typeof(MetroAreaMappingProfile).Assembly)` scan in `LegacyApplicationDependencyInjection.cs`. When the profile physically relocates, update this scan.

### 8.5.b — LankaConnect.Infrastructure csproj deletion (~2-3 days)

**566 files** currently in this transitional csproj:
- `Data/AppDbContext.cs` — cross-module ChangeTracker + reflection Ignore sweep (Consult #20). Owned by 20+ historical entities transitively; must be shrunk to nothing before deletion.
- `Data/Migrations/` — **506 EF migration files.** Physical history of Postgres schema evolution. Must relocate to per-module migration folders (per Consult #7 Delta multi-DbContext), preserving `__EFMigrationsHistory` continuity on staging + prod.
- `Data/Configurations/` — historical EntityTypeConfiguration files (some 4C.d-relocated but still-referenced from AppDbContext).
- `Data/Repositories/` — cross-module repos (Communications Email* — 4 files pending relocation per Day 5 slot A URGENT; Businesses/Services/Reviews orphan repos).
- `Data/Seeders/` — DbInitializer + BadgeSeeder + EnumSeeder + UserSeeder (post-Consult-#16 UserSeeder moved to Identity.Infrastructure).
- `Services/` — cross-cutting services still injecting AppDbContext (`RevenueCalculatorService`, `DatabaseSalesTaxService`, `TimeZoneLookupService`).
- `Templates/` — legacy email HTML/JSON templates (should move with Communications).
- `Security/` — historical JWT/password service impls.

**Phase A.5 plan for this csproj:**
1. Migration relocation task (~2 days): split 506 migrations into per-module buckets. Cap `__EFMigrationsHistory` entries in AppDbContext, route new migrations per-module. Do NOT re-parent old ones (would break prod).
2. AppDbContext dismantle: reflection Ignore sweep gets narrower as module-owned aggregates disappear from AppDbContext model.
3. Cross-cutting services relocate to `BuildingBlocks.Services/` or nearest module.
4. Templates relocate to `Communications.Infrastructure/Templates/`.
5. Delete csproj.

### 8.5.c — LankaConnect.API → Hosts/Host.AllInOne physical rename (~4 hrs)

Sprint-plan §Day 10 target. Deferred because:
- Physical rename touches `.sln` + 15+ csproj `<ProjectReference>` entries
- `.github/workflows/*.yml` step paths for `deploy-staging.yml`, `deploy-ui-staging.yml`, `pr-validation.yml`
- `dotnet ef migrations` CI commands (~4 subcommand paths)
- `docker-compose.yml` if present
- All docs cross-refs (dozens of files)

High blast radius, low functional benefit inside sprint window. Do as a single-shot rename with grep audit before pushing.

### 8.5.d — LegacyPromotions folder split (Consult #17 Day 10 debt, ~2 hrs)

`<Module>.Contracts/LegacyPromotions/` bucket per Consult #17 was TEMPORARY. Split into domain-specific folders (`Contracts/Repositories/`, `Contracts/Services/`, `Contracts/DTOs/`) as part of the legacy csproj deletion pass. Affected: `LankaEvents.Contracts/LegacyPromotions/` (11 files from Wave 6.5.f cycle-break), `Communications.Contracts/LegacyPromotions/` (2 files).

### 8.5.e — Test-project full-solution build restore (Consult #18 Day 10 debt, ~1 day)

Sprint bible §Day 6 landed Consult #18 workflow-scope transitional at commit `d51496b6`: `.github/workflows/deploy-staging.yml` step 5 narrowed to `src/LankaConnect.API/LankaConnect.API.csproj`. Restore full-solution build once test-project compile errors (72 files as of Day 6 EOD; some resolved during Day 7-10) are down to zero. TRACEABILITY_MATRIX row SPRINT-D10.1.

### 8.5.f — Domain-event dispatch per-module SaveChangesInterceptor (~4 hrs)

Consult #25 Q2 (2026-07-13) mandated as Day 8 prereq before handler direct-SaveChanges migration. Sprint deferred. Wire a `SaveChangesInterceptor` on each module DbContext (LankaEventsDbContext, IdentityDbContext, CommunicationsDbContext, MediaDbContext, FormsDbContext, NotificationsDbContext) that calls the existing `DomainEventDispatcher` after successful SaveChanges. Currently only `AppDbContext.CommitAsync` dispatches events — every write on a module DbContext (CreateEvent, RegisterUser, UpdateUserPreferredMetroAreas post-hotfix) silently drops raised domain events.

**Impact of current gap**: `MemberVerificationRequestedEvent` (fires on RegisterUser) is NOT dispatched → verification email not sent. `EventCreatedIntegrationEvent` (fires on CreateEvent) is NOT dispatched → downstream projections stale. Test discovery pending.

### 8.5.g — ~95 LankaEvents.Application handler `IUnitOfWork` migration (~1-2 days)

Consult #25 Q6 blanket-approved direct-SaveChanges migration for single-context handlers. Sprint fixed 3 handlers (CreateEvent, RegisterUser, UpdateUserPreferredMetroAreas) as forcing-function proofs. The remaining ~95 LankaEvents handlers still call `_unitOfWork.CommitAsync(ct)` which fires on AppDbContext and commits 0 changes for their module-owned aggregates. Silent write-loss until traffic exercises each handler.

**Prerequisite**: 8.5.f interceptor MUST land first, otherwise migration causes downstream dispatch regression.

### 8.5.h — Wave 6.5.a shared-connection `IMultiContextUnitOfWork` fix (~2-3 days)

Consult #25 Q2 documented: `UnitOfWork.CommitAsync(DbContext[])` throws "transaction not associated with the current connection" because AppDbContext + module DbContexts pull separate Npgsql pool connections. Proper fix: register a shared `NpgsqlConnection` per scope, hand it to each `AddDbContext<T>` via `UseConnection(...)`, then cross-context transaction enrollment works. Rule 5b + Rule 5c + Rule 5j.4 audit surface.

Alternative: retire `IMultiContextUnitOfWork.CommitAsync(DbContext[])` entirely — handlers do per-context `SaveChangesAsync`, cross-context atomicity handled via saga/compensation pattern. Architect decision at Wave 8.5.h kick-off.

### 8.5.i — Metro-area cross-module write via IMetroAreaRepository (~1 day)

Blueprint §7.8 mandates cross-module reads/writes through Contracts surfaces. Currently `UpdateUserPreferredMetroAreasCommandHandler` uses direct raw-SQL insert to `identity.user_preferred_metro_areas`. Scaffold `IIdentityMetroAreaJunctionRepository` in `Identity.Contracts/` + impl in `Identity.Infrastructure/` and route through it. Applies also to `RegisterUserHandler` raw-SQL block landed at `c20a39de`.

### 8.5.j — Events `OwnsOne(TicketPrice).ToJson("ticket_price")` data-shape drift (Consult #26, ~1-2 days)

Sprint discovered: `EventRepository.GetByOrganizerAsync` throws `Cannot get token type 'Number'/'Object' as string` from `JsonConvertedValueReaderWriter.FromJsonTyped` when reading historical Event rows. Root cause: pre-Consult-#23 JSON writes stored `ticket_price.currency` as a NESTED object; post-Consult-#23 writes it as an ISO 4217 string. EF Core 8's MaterializeJsonEntity path can't handle the shape drift.

**Options for Consult #26 (architect deliberation)**:
1. Data migration: `UPDATE events SET ticket_price = jsonb_set(...)` normalizing every legacy row.
2. Refactor: remove `.ToJson("ticket_price")` and store as separate scalar columns `ticket_price_amount` + `ticket_price_currency` (physical migration + config rewrite). Same for `Pricing` + `RevenueBreakdown` if they exhibit the same issue.
3. Custom Reader: author a shape-tolerant `JsonConvertedValueReaderWriter` variant that accepts both object and string forms for Currency. High-risk, EF-internals territory.

Cascades to 6 downstream money-flow test fails (Sponsors/SponsorshipPackages/Donations/Collections/AddOns/1 Events test) — sprint measured but not fixed.

### 8.5.k — Businesses module controller scaffold (product decision, ~1 day if greenlit)

Businesses aggregate was DELETED at Wave 6.5 per Consult #12 Option D (LankaBusiness product surfaces in Phase B). But Wave 9 smoke still expects a `BusinessesController` — 1 test fails at `POST /api/Businesses HTTP 404` (controller physically absent). Product decision: either scaffold a minimal controller for the 5 remaining GET endpoints (which ARE served — the WRITE endpoint is missing), OR mark the write test as SKIP in Wave 9 with reason "Businesses module parked pending Phase B / LankaBusiness product launch".

### 8.5.l — PhotoAlbums 1 fail body diagnostic (~30 min)

Wave 9 smoke reports 1 PhotoAlbums fail (`albums :: create album` POST 400) but body not captured in report. Patch smoke to log `$r.Body` on 400 responses, re-run to reveal specific error, then fix. Low priority.

## Change Log

- 2026-07-04: Created as part of sprint Day 1 doc surgery (pulled forward to Day 0.5).
- 2026-07-14: Wave 8.5 section added — captures all sprint-deferred debt (Days 4-10 discovery). 12 items 8.5.a through 8.5.l covering ~2-3 weeks of Phase A.5 work.
