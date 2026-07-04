# Day 2 Agent Briefs — Bulk-Move Execution

**Sprint Day:** 2 (Tue 2026-07-07)
**Sprint bible:** [MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md](../MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md)
**Manifest:** [bulk-move-manifest.md](bulk-move-manifest.md)

## Rules for ALL agents

1. **You run from your worktree only.** Never touch other agents' worktrees. Never merge.
2. **`git mv` for every file.** Never `rm + add`. Preserves blame history.
3. **DELETE decisions logged.** Every `git rm` writes a line to `docs/sprint/day-2-deletions.md` in your worktree: `<path> | <reason> | agent-<letter>`.
4. **NO test run, NO build attempt.** That's Day 3.
5. **Manifest is arbiter.** If a file has no rule, STOP and flag.
6. **Push each worktree at EOD.** No cross-branch coordination.

## Standard sequence (all agents)

```powershell
cd C:\Work\lc-bulk-move-<letter>\
git status                                                     # confirm clean develop-head
git pull origin develop                                        # catch up on any Day 1 late commits
.\scripts\sprint\Day2-BulkMove.ps1 -Agent <letter> -DryRun     # preview
.\scripts\sprint\Day2-BulkMove.ps1 -Agent <letter>             # execute
.\scripts\sprint\Rewrite-Namespaces.ps1 -Phase 1 -TargetRoot <target-root> -MapFile docs/sprint/namespace-map.txt
git status                                                     # verify only expected changes
git add -A
git commit -m "Day 2 agent-<letter> bulk-move: <N> files moved, <D> deleted. LEAVE BROKEN."
git push origin bulk-move/agent-<letter>
```

---

## Agent A — Domain Move

**Worktree:** `C:\Work\lc-bulk-move-A\`
**Branch:** `bulk-move/agent-A`
**Scope:** ~311 files from `src/LankaConnect.Domain/` + ~93 deletes (over-engineered dead code)

**Priority order:**
1. DELETE dead types (documented in manifest as ratified 2026-07-04)
2. MOVE survivors per manifest table (Analytics/Badges/Billing/Common/Communications/etc.)
3. Business STAYS -- handled Day 10 as `LankaConnect.Domain` → `LankaConnect.Domain.Legacy` rename

**Target roots (for namespace-rewrite Phase 1):**
- `src/Modules/Communications/Communications.Domain/`
- `src/Modules/CulturalIntelligence/CulturalIntelligence.Domain/`
- `src/Modules/Payments/Payments.Domain/`
- `src/Products/LankaEvents/LankaEvents.Domain/`
- `src/BuildingBlocks/BuildingBlocks.Domain/`
- `src/SharedKernel/SharedKernel.Cultural/`

Run Rewrite-Namespaces.ps1 Phase 1 against EACH target root.

---

## Agent B — Application Move

**Worktree:** `C:\Work\lc-bulk-move-B\`
**Branch:** `bulk-move/agent-B`
**Scope:** ~400 files from `src/LankaConnect.Application/`

**Priority order:**
1. Move all feature folders per manifest
2. Businesses STAYS -- handled Day 10 (Business KEEP-ALIVE)
3. DependencyInjection.cs + GlobalUsings.cs → `src/Hosts/Host.AllInOne/`

**Target roots:**
- `src/Modules/Communications/Communications.Application/`
- `src/Modules/CulturalIntelligence/CulturalIntelligence.Application/`
- `src/Modules/Identity/Identity.Application/`
- `src/Modules/Payments/Payments.Application/`
- `src/Products/LankaEvents/LankaEvents.Application/`
- `src/BuildingBlocks/BuildingBlocks.Application/`
- `src/SharedKernel/SharedKernel.Geo/`
- `src/SharedKernel/SharedKernel.Cultural/`
- `src/Hosts/Host.AllInOne/`

---

## Agent C — Infrastructure (Non-EF) Move

**Worktree:** `C:\Work\lc-bulk-move-C\`
**Branch:** `bulk-move/agent-C`
**Scope:** ~200 files from `src/LankaConnect.Infrastructure/` (excluding `Data/`)

**Priority order:**
1. DELETE dead types: `DisasterRecovery/`, `Monitoring/`, `Security/` (~6 files)
2. MOVE Email/WhatsApp/Templates → Communications
3. MOVE Storage → Media
4. MOVE Payments → Payments
5. MOVE Events → LankaEvents
6. MOVE Common/Helpers/Outbox/Database → BuildingBlocks
7. MOVE Services/BackgroundServices/DI → Host.AllInOne

---

## Agent D — Infrastructure EF Data Move

**Worktree:** `C:\Work\lc-bulk-move-D\`
**Branch:** `bulk-move/agent-D`
**Scope:** ~50 EF configurations from `src/LankaConnect.Infrastructure/Data/Configurations/`

**IMPORTANT — this agent needs architect co-op on Day 3:**
- Configurations are per-entity; each moves to its owning module.
- Audit map required Sat 2026-07-05 by system-architect (Consult #7 category matrix).
- Repositories folder needs same per-entity/per-repo split.
- **AppDbContext.cs + UnitOfWork.cs STAY Day 2** -- relocate Day 10.
- **Data/Migrations/ STAYS Day 2** -- Day 5 handles repatriation.
- **Data/Interceptors/ MOVES Day 2** to BuildingBlocks.Infrastructure.

Agent D is the SLOWEST agent because of per-file decisions.

---

## Agent E — API Controllers Move

**Worktree:** `C:\Work\lc-bulk-move-E\`
**Branch:** `bulk-move/agent-E`
**Scope:** 53 controller files from `src/LankaConnect.API/Controllers/`

**Controllers per target module (hardcoded in Day2-BulkMove.ps1 script):**
- LankaEvents (~13): AddOns, Analytics, Approvals, Badges, Collections, Donations, EventConfig, EventTemplates, Events, SeatingMetrics, Sponsors, SponsorshipPackages, VenueLayouts
- Identity (~3): AdminUsers, Auth, Users
- Communications (~11): AdminEmailTemplates, Contact, Email, EmailGroups, EmailMetrics, Newsletter, Newsletters, WhatsApp, WhatsAppAdmin, WhatsAppWebhook, AdminSupportTickets
- Payments (~2): Payments, RefundReconciliation
- Media (~2): PhotoAlbums, Content
- Host.AllInOne (~11): Admin, AdminRecovery, Configuration, Dashboard, Diagnostics, Health, MetroAreas, Public, ReferenceData, Test, Base
- Host.AllInOne/Legacy (~1): Businesses (Phase B)

**Program.cs + Startup + appsettings STAY Day 2.** Day 10 renames whole csproj.

---

## Agent F — Shared + Root Move

**Worktree:** `C:\Work\lc-bulk-move-F\`
**Branch:** `bulk-move/agent-F`
**Scope:** ~67 files from `src/LankaConnect.Shared/` + `src/LankaConnect/`

**Priority order:**
1. `LankaConnect.Shared/Email/` → `Modules/Communications/Communications.Contracts/Email/`
2. `LankaConnect.Shared/WhatsApp/` → `Modules/Communications/Communications.Contracts/WhatsApp/`
3. `LankaConnect/Application/` → `BuildingBlocks/BuildingBlocks.Application/RootLegacy/`
4. `LankaConnect/Domain/` → `BuildingBlocks/BuildingBlocks.Domain/RootLegacy/`

Smallest scope — this agent should finish first and can help peer-review other worktrees.

---

## End of Day 2 checkpoint

Each agent posts to founder EOD 18:00:
- Files moved: N
- Files deleted: D
- Files flagged (no manifest rule): F
- Push confirmation: bulk-move/agent-<letter> @ <sha>

Founder confirms all 6 branches pushed. Day 3 opens 06:00 Wed.
