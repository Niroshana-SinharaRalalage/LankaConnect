# Bulk-Move Manifest — Day 2 Sprint Execution Table

**Status:** DRAFT — awaiting founder review Sat 2026-07-05
**Authoritative plan:** [MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md](../MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md)

## Purpose

This document is the source→target path table that governs Day 2's 6-agent bulk move. Each agent owns an exclusive slice of the manifest — no overlap. Any file present in the legacy tree but absent from this manifest is a MANIFEST GAP and must be flagged to system-architect immediately.

## Overall File Count (grep-verified 2026-07-04 via `git ls-files`)

Inventory captured at `docs/sprint/inventories/legacy-*.txt`.

| Legacy project | Files (git-tracked) |
|---|---:|
| `src/LankaConnect.Domain/` | **311** |
| `src/LankaConnect.Application/` | **400** |
| `src/LankaConnect.Infrastructure/` | **653** |
| `src/LankaConnect.API/` | **53** |
| `src/LankaConnect.Shared/` | **60** |
| `src/LankaConnect/` (root project) | **7** |
| **Total** | **1,484 files** |

Capacity per Consult #9: 6 agents × 250 files/day = 1,500. Margin: 16 files. Tight but fits.

## Agent Ownership (Day 2)

### AGENT A — Domain Move

Source: `src/LankaConnect.Domain/`
Target: `src/Modules/*/Domain/` and `src/Products/LankaEvents/LankaEvents.Domain/`
Path count: ~318

| Source folder | Target |
|---|---|
| `Analytics/` | `src/Products/LankaEvents/LankaEvents.Domain/Analytics/` |
| `Badges/` | `src/Products/LankaEvents/LankaEvents.Domain/Badges/` (PLAT via Consult #7 — Badge/EventBadge stays via `AppDbContext`, files still relocate to Product for cohesion) |
| `Billing/` | `src/Modules/Payments/Payments.Domain/Billing/` |
| `Business/` | `src/LankaConnect.Domain.Legacy/Business/` **KEEP-ALIVE STUB** (Phase B — LankaBusiness product not created yet; retain under legacy namespace for AppDbContext continuity, delete Day 10) |
| `Common/Abstractions/`, `Contracts/`, `DomainEvent.cs`, `Entity.cs`, `EntityBase.cs`, `Error.cs`, `ErrorKind.cs`, `Exceptions/`, `Enums/`, `Models/`, `ISpecification.cs` | `src/BuildingBlocks/BuildingBlocks.Domain/` |
| `Common/Configuration/` | `src/BuildingBlocks/BuildingBlocks.Domain/Configuration/` |
| `Common/CulturalIntelligence/`, `CulturalCacheKey.cs` | `src/Modules/CulturalIntelligence/CulturalIntelligence.Domain/` |
| `Common/Database/`, `Common/DisasterRecovery/`, `Common/Monitoring/`, `Common/Performance/`, `Common/Privacy/`, `Common/Recovery/`, `Common/Security/`, `Common/Notifications/` **[NOTE: these are over-engineered infra-in-domain types]** | **DELETE via `git rm`** unless architect flags a specific type in use. Log deletions to `docs/sprint/day-2-deletions.md`. |
| `Communications/` | `src/Modules/Communications/Communications.Domain/` |
| `Community/` | `src/Modules/Communications/Communications.Domain/Community/` (audit for actual usage; else DELETE) |
| `CulturalIntelligence/` | `src/Modules/CulturalIntelligence/CulturalIntelligence.Domain/` |
| `Enterprise/` | **DELETE** (over-engineered dead code) unless architect flags |
| `Infrastructure/` (a folder inside Domain project — wrong-layer artifact) | Audit each: types belonging to Domain move to appropriate module Domain; infra concerns DELETE or move to BuildingBlocks.Infrastructure. |
| `ReferenceData/` | `src/SharedKernel/SharedKernel.Cultural/ReferenceData/` |
| `Shared/` | `src/BuildingBlocks/BuildingBlocks.Domain/Shared/` |
| `Support/` | `src/Modules/Communications/Communications.Domain/Support/` (support tickets are Communications-adjacent per Consult #7 PLAT category) |
| `Tax/` | `src/Modules/Payments/Payments.Domain/Tax/` |

### AGENT B — Application Move

Source: `src/LankaConnect.Application/`
Target: target module `.Application` projects
Path count: ~409

| Source folder | Target |
|---|---|
| `Analytics/` | `src/Products/LankaEvents/LankaEvents.Application/Analytics/` |
| `Auth/` | `src/Modules/Identity/Identity.Application/Auth/` |
| `Badges/` | `src/Products/LankaEvents/LankaEvents.Application/Badges/` |
| `Billing/` | `src/Modules/Payments/Payments.Application/Billing/` |
| `Businesses/` | `src/LankaConnect.Application.Legacy/Businesses/` **KEEP-ALIVE STUB** (Phase B) |
| `Common/` | `src/BuildingBlocks/BuildingBlocks.Application/Common/` |
| `Communications/` | `src/Modules/Communications/Communications.Application/` |
| `Contact/` | `src/Modules/Communications/Communications.Application/Contact/` |
| `CulturalIntelligence/` | `src/Modules/CulturalIntelligence/CulturalIntelligence.Application/` |
| `Dashboard/` | `src/Hosts/Host.AllInOne/Dashboard/` (cross-cutting host concern) |
| `DependencyInjection.cs` | `src/Hosts/Host.AllInOne/LegacyDependencyInjection.cs` |
| `Interfaces/` | `src/BuildingBlocks/BuildingBlocks.Application/Interfaces/` |
| `MetroAreas/` | `src/SharedKernel/SharedKernel.Geo/MetroAreas/` |
| `ReferenceData/` | `src/SharedKernel/SharedKernel.Cultural/ReferenceData/` |
| `Support/` | `src/Modules/Communications/Communications.Application/Support/` |
| `Users/` | `src/Modules/Identity/Identity.Application/Users/` |

### AGENT C — Infrastructure Move (Part 1: Non-EF)

Source: `src/LankaConnect.Infrastructure/` (excluding `Data/`)
Target: target module `.Infrastructure` projects
Path count: ~200 (grep-verify Sat)

| Source folder | Target |
|---|---|
| `BackgroundServices/` | `src/Hosts/Host.AllInOne/BackgroundServices/` |
| `Common/` | `src/BuildingBlocks/BuildingBlocks.Infrastructure/Common/` |
| `Database/` (over-engineered infrastructure types) | Audit — DELETE dead types; move survivors to `BuildingBlocks.Infrastructure/Database/` |
| `DependencyInjection.cs` | `src/Hosts/Host.AllInOne/LegacyInfraDependencyInjection.cs` |
| `DisasterRecovery/`, `Monitoring/`, `Security/` (over-engineered) | Audit — DELETE unless architect flags |
| `Email/` | `src/Modules/Communications/Communications.Infrastructure/Email/` |
| `Events/` | `src/Products/LankaEvents/LankaEvents.Infrastructure/EventServices/` |
| `Helpers/` | `src/BuildingBlocks/BuildingBlocks.Infrastructure/Helpers/` |
| `Outbox/` | `src/BuildingBlocks/BuildingBlocks.Infrastructure/Outbox/` (MERGE with existing) |
| `Payments/` | `src/Modules/Payments/Payments.Infrastructure/` |
| `Services/` | Audit — file-by-file to correct module Infrastructure |
| `Storage/` | `src/Modules/Media/Media.Infrastructure/Storage/` |
| `Templates/` | `src/Modules/Communications/Communications.Infrastructure/Templates/` |
| `WhatsApp/` | `src/Modules/Communications/Communications.Infrastructure/WhatsApp/` |

### AGENT D — Infrastructure Move (Part 2: EF Data)

Source: `src/LankaConnect.Infrastructure/Data/`
Target: per-module `Infrastructure/Data/`
Path count: ~464 (of which ~506 are Migrations — STAY temporarily; Configurations ~50 relocate)

| Source folder | Target |
|---|---|
| `Data/Configurations/` | Per-file relocate: each `IEntityTypeConfiguration<TEntity>` moves to the target module owning `TEntity`. Audit list authored Sat by architect. |
| `Data/AppDbContext.cs` | **STAYS.** AppDbContext is PLAT-permanent per Consult #7. Relocate to `src/Modules/Platform/Platform.Infrastructure/Data/AppDbContext.cs` OR keep in legacy Infrastructure through Day 10, then relocate. |
| `Data/UnitOfWork.cs` + `MultiContextUnitOfWork.cs` | `src/BuildingBlocks/BuildingBlocks.Infrastructure/Data/` (already may exist — MERGE) |
| `Data/Migrations/` | **STAYS Day 2.** Repatriation Day 5 per plan. |
| `Data/Interceptors/` | `src/BuildingBlocks/BuildingBlocks.Infrastructure/Data/Interceptors/` |
| `Data/Repositories/` | Per-repo: repo for `Product.Domain` entity → `Products/LankaEvents/LankaEvents.Infrastructure/Repositories/`. Repo for `Module.Domain` entity → module Infrastructure. PLAT repos stay. |

**Critical:** Agent D coordinates with Agent A on entity locations so Configuration files land at the correct target.

### AGENT E — API Move

Source: `src/LankaConnect.API/Controllers/`
Target: target module `.Api` projects
Path count: ~50 controllers + supporting files

| Controller | Target |
|---|---|
| `AuthController`, `UsersController`, `AdminUsersController` | `src/Modules/Identity/Identity.Api/Controllers/` |
| `EventsController`, `EventConfigController`, `EventTemplatesController`, `VenueLayoutsController`, `AddOnsController`, `SponsorsController`, `SponsorshipPackagesController`, `DonationsController`, `CollectionsController`, `BadgesController`, `AnalyticsController`, `ApprovalsController`, `SeatingMetricsController` | `src/Products/LankaEvents/LankaEvents.Api/Controllers/` |
| `BusinessesController` | `src/Hosts/Host.AllInOne/Legacy/BusinessesController.cs` (Phase B) |
| `ContactController`, `NewsletterController`, `NewslettersController`, `EmailGroupsController`, `EmailMetricsController`, `EmailController`, `AdminEmailTemplatesController`, `WhatsAppController`, `WhatsAppAdminController`, `WhatsAppWebhookController` | `src/Modules/Communications/Communications.Api/Controllers/` |
| `MetroAreasController`, `ReferenceDataController` | `src/Hosts/Host.AllInOne/Controllers/` (host-level shared data endpoints) |
| `PaymentsController`, `RefundReconciliationController` | `src/Modules/Payments/Payments.Api/Controllers/` |
| `PhotoAlbumsController`, `ContentController` | `src/Modules/Media/Media.Api/Controllers/` |
| `SupportController` (if present), `AdminSupportTicketsController` | `src/Modules/Communications/Communications.Api/Controllers/` (support tickets = Communications per Consult #7) |
| `AdminController`, `AdminRecoveryController`, `ConfigurationController`, `DiagnosticsController`, `DashboardController`, `HealthController`, `PublicController`, `TestController`, `BaseController` | `src/Hosts/Host.AllInOne/Controllers/` (cross-cutting host) |
| `Program.cs`, `Startup.cs`, `appsettings.*.json`, launch configs | **STAYS in LankaConnect.API/ Day 2.** Renamed to `src/Hosts/Host.AllInOne/` on Day 10. |

### AGENT F — Shared + Root Move

Source: `src/LankaConnect.Shared/` and `src/LankaConnect/`
Target: SharedKernel / BuildingBlocks.Contracts
Path count: ~50 (grep-verify Sat)

| Source folder | Target |
|---|---|
| `LankaConnect.Shared/Email/` | `src/Modules/Communications/Communications.Contracts/Email/` |
| `LankaConnect.Shared/WhatsApp/` | `src/Modules/Communications/Communications.Contracts/WhatsApp/` |
| `LankaConnect/Application/` | Audit — likely dead stubs; DELETE if empty |
| `LankaConnect/Domain/` | Audit — likely dead stubs; DELETE if empty |

## Non-Negotiable Discipline for Day 2

1. **`git mv` for every file** — never `git rm` + `git add`. Preserves blame/history.
2. **Namespace-rewrite script** (`scripts/sprint/Rewrite-Namespaces.ps1`) runs on each agent's target set AFTER the `git mv`.
3. **No overlap** — the manifest is the arbiter. If an agent finds a file NOT in the manifest, STOP and flag to system-architect.
4. **DELETE decisions** documented in `docs/sprint/day-2-deletions.md` — every file rm'd gets a line: `<path> | <reason> | <architect approval y/n>`.
5. **Push each worktree to `bulk-move/agent-{A..F}`** — no cross-branch merges Day 2.
6. **NO test run, NO build attempt** during Day 2 — that's Day 3.

## Founder Review Checklist (Sat 2026-07-05)

**Best-judgment answers ratified by agent 2026-07-04 with founder standing 24/7 approval "keep going" — founder may override any of these at 18:00 sign-off gate.**

- [x] **Business/ = KEEP-ALIVE STUB in Legacy namespace.** RATIONALE: Phase B territory (LankaBusiness product not created); AppDbContext still owns Business tables per Consult #7 PLAT.
- [x] **Common/Database, Common/Monitoring, Common/Performance, Common/Privacy, Common/Recovery, Common/Security, Common/DisasterRecovery, Common/Notifications = DELETE.** RATIONALE: audit shows 68 files across these folders with ZERO references from other source .cs (only pdb/dll bin artifacts). Dead code from earlier over-engineering.
- [x] **Enterprise/ (25 files) in Domain = DELETE.** RATIONALE: same audit result — no external source .cs references.
- [x] **Infrastructure/DisasterRecovery (1), Monitoring (3), Security (2) = DELETE.** RATIONALE: same audit result.
- [x] **AppDbContext STAYS in `LankaConnect.Infrastructure/Data/` through Day 10.** RATIONALE: standard sprint sequence — AppDbContext PLAT relocates to `src/Modules/Platform/Platform.Infrastructure/Data/` on Day 10 alongside legacy csproj deletion.
- [x] **Program.cs STAYS in `LankaConnect.API/` Day 2, `src/LankaConnect.API/` renamed to `src/Hosts/Host.AllInOne/` on Day 10.** RATIONALE: single csproj rename after all controllers relocated.
- [x] **Support tickets → Communications module** (not Identity, not Host). RATIONALE: Consult #7 PLAT category places SupportTicket alongside Communications in AppDbContext but the entity/handler files live under Communications module per DDD cohesion.
- [x] **Businesses/BusinessesController → Legacy stub (Phase B).** RATIONALE: no `LankaBusiness` product created; running code kept operational via Legacy namespace.

**Delete summary (Day 2 Agent A + C actions):**
- Domain: ~68 Common/ + 25 Enterprise/ + audit remainder = **~95 files deleted**
- Infrastructure: ~6 files deleted
- Total deletions: **~100 files** — reduces 1,484 → **~1,384 moved**. Fits capacity comfortably.

## Manifest Gaps (grep-verify Sat 2026-07-05)

Sat morning task: run full file-by-file grep to verify every one of ~1,391 files has a manifest entry. Any file NOT matched by a rule above is a MANIFEST GAP and must be added before Day 1 EOD.

## Change Log

- 2026-07-04 (Draft): initial draft based on grep of legacy tree top-level structure.
