# Wave 4.9 — Comprehensive Schema Realignment Plan

**Author**: LankaConnect system-architect (deep enterprise review)
**Date**: 2026-06-07
**Status**: Awaiting founder approval on the 4 blocking decisions (see Execution Gate at end)

> This document is the comprehensive plan that replaces the tactical Phase 1/2/3 sequencing
> the developer-mode session was attempting. It supersedes any prior tactical notes.

---

## Section 1 — State Inventory (probe-verified, not summaries)

### 1.1 DbContext + snapshot landscape

| DbContext | Snapshot LOC | Modules.* leaks | Status |
|---|---|---|---|
| AppDbContext (`src/LankaConnect.Infrastructure/Data/AppDbContext.cs`, 702 LOC) | 9,555 (at `Migrations/AppDbContextModelSnapshot.cs`) | **6 leaked types** | DRIFTED |
| NotificationsDbContext | ~330 | 0 | CLEAN; **not yet applied to staging** |
| MediaDbContext | ~310 | 0 | CLEAN; **never deployed** |
| FormsDbContext | ~460 | 0 | CLEAN; **never deployed** |

**Verdict**: Module-side state is pristine. All drift is concentrated in the legacy AppDbContext snapshot.

### 1.2 Two-migration-directories mystery — RESOLVED

- `src/LankaConnect.Infrastructure/Migrations/` (33 historical migrations 2025-08 to 2026-04, plus the active 9,555-LOC `AppDbContextModelSnapshot.cs`)
- `src/LankaConnect.Infrastructure/Data/Migrations/` (207 migrations from 2026-04-21 onward, no snapshot)

Both compile because EF discovers via `[Migration("...")]` attributes, not filesystem. **But** the next `dotnet ef migrations add` will emit a new `AppDbContextModelSnapshot.cs` into `Data/Migrations/` — causing a **duplicate-class compile error** with the old snapshot.

**Required action**: Phase 0.5 consolidation BEFORE any other migration touches AppDbContext.

### 1.3 IAuditable inventory

- 63 entities derive from `LegacyBaseEntity` (IAuditable).
- The snapshot has 0 `CreatedBy` properties — **proves it was generated before W3D entity migration**.
- Next migration add will emit ~252 AddColumn statements (63 entities × 4 columns).
- A few tables (Sponsor from Phase 6A.151) ALREADY have `created_by`/`updated_by` from ad-hoc Phase 6A migrations → need `IF NOT EXISTS` wrap.

### 1.4 The 6 leaked types in AppDbContextModelSnapshot

```
LankaConnect.Modules.Media.Domain.Entities.AlbumPhoto         (lines 2449, 7202)
LankaConnect.Modules.Forms.Domain.EventForm                   (lines 2591, 9473)
LankaConnect.Modules.Forms.Domain.Entities.FormAnswer         (lines 2831, 7261)
LankaConnect.Modules.Forms.Domain.Entities.FormQuestion       (lines 2888, 7270)
LankaConnect.Modules.Forms.Domain.Entities.FormResponse       (lines 2947, 7261-9478)
LankaConnect.Domain.Events.PhotoAlbum                         (line 4634, 9543)  — DUAL leak (old + new ns)
LankaConnect.Domain.Notifications.Notification                (line 5242)        — DUAL leak (old + new ns)
```

### 1.5 Cross-context relationships — all soft GUIDs, ZERO DB-level FKs

| Reference | Type | DB-level FK? |
|---|---|---|
| Media.PhotoAlbum.EventId → events.events.id | `Property` only | NO |
| Forms.EventForm.EventId → events.events.id | `Property` only | NO |
| Notifications.Notification.UserId → users.users.id | `Property` only | NO |

**Implication**: `ALTER TABLE ... SET SCHEMA` per-module is single-transaction safe. Intra-module FKs (AlbumPhoto.AlbumId → PhotoAlbum.Id) move automatically with the rename.

### 1.6 Migration history vs deployed staging

**Critical finding**: `deploy-staging.yml` line 127 only runs `dotnet ef database update --context AppDbContext`. Module DbContexts (NotificationsDbContext, MediaDbContext, FormsDbContext) are **NEVER applied**. So:

- `notifications.outbox`, `notifications.outbox_dead_letter`, `notifications.idempotency_keys` do NOT exist on staging.
- `media.outbox`, etc. do NOT exist on staging.
- `forms.outbox`, etc. do NOT exist on staging.

Data flow works because the entities use `events.photo_albums` (cross-schema override) and `notifications.notifications` (created by legacy AppDbContext migration in 2025-11). The outbox tables exist nowhere — latent landmine for Wave 6.5.

### 1.7 Schemas physically deployed today

Confirmed: `analytics`, `badges`, `business`, `communications`, `community`, `events`, `identity`, `notifications`, `payments`, `reference_data`, `support`, `users`, `public`.

**NOT deployed**: `media`, `forms`.

---

## Section 2 — Identified Risks (ranked by blast radius)

| # | Risk | Trigger | Blast | Recovery | Prevented by |
|---|---|---|---|---|---|
| 1 | Destructive Drop* lurking for moved entities | Next `migrations add` | 7 DropTable + ~15 DropForeignKey on live data; PITR-only recovery | PITR | Phase 3 snapshot purge |
| 2 | Two-snapshot collision | Next `migrations add` | Compile failure OR EF nondeterminism; halts staging deploys | git revert | Phase 0.5 directory consolidation |
| 3 | 252 AddColumn mixed with destructive Drop* | Phase 1 migration generated against current snapshot | Multi-concern; partial application = stuck state | PITR + manual `__EFMigrationsHistory` cleanup | Phase 3 BEFORE Phase 1 |
| 4 | Module migrations never applied | Today, every deploy | Outbox tables absent; Wave 6.5 will fail | Apply manually | Phase 0.7 — CI extension |
| 5 | CI single-context blind-spot | Today | New module migrations never apply silently | Same as #4 | Phase 0.7 |
| 6 | Cross-context UoW eventual consistency | Existing self-saving repository pattern | Phantom domain events on rollback | Cleanup script | Wave 6.5 outbox cutover |
| 7 | Per-Phase-6A ad-hoc audit columns | Phase 1 IAuditable migration | AddColumn fails on column-already-exists | Edit to `IF NOT EXISTS` | Pre-flight `psql \d <table>` probe |
| 8-12 | (lower-ranked: multi-schema migrations, hand-edit Designer risk, PITR retention edge cases, lock-window during ALTER, EnsureSchema ordering) | Various | | | Various — see full plan |

---

## Section 3 — Architect-Approved Phase Sequence

Reordered per architect deep-dive analysis:

```
Phase 0   ✓ DONE (CI lint + ArchTest skipped pending Phase 3) — commits 083c0e4f, 603fed50
Phase 0.5 — Merge two migration directories (source-only, ZERO DB risk)
Phase 0.7 — Extend deploy-staging.yml to apply module DbContexts
Phase 3   — AppDbContext snapshot purge via "ghost migration" pattern (Up() empty)
Phase 1   — IAuditable AddColumn pass, split into 10 per-schema groups (1.1 through 1.10)
Phase 2   — Per-module schema rename (Media → Forms; Communications + Identity deferred to Wave 8)
```

**Rationale for ordering**: Phase 3 cleans the snapshot so Phase 1's `migrations add` produces purely additive output (instead of mixed Drop+AddColumn). Phase 2 follows Phase 1.1–1.8 because moving Media+Forms tables AFTER they have public-schema audit columns is the more reviewable path; per-module IAuditable (1.9, 1.10) then runs after the rename, in the new schemas.

---

## Section 4 — Detailed Phase Specifications

See the full plan body for:

### Phase 0.5 — Consolidate two migration directories
- `git mv src/LankaConnect.Infrastructure/Migrations/*.cs src/LankaConnect.Infrastructure/Data/Migrations/`
- `git mv src/LankaConnect.Infrastructure/Migrations/AppDbContextModelSnapshot.cs src/LankaConnect.Infrastructure/Data/Migrations/`
- Build + ArchTest verify
- ~30 min wall-clock; zero DB impact

### Phase 0.7 — Extend CI to apply module DbContexts
- Edit `.github/workflows/deploy-staging.yml` line 127 to apply 3 additional contexts (`MediaDbContext`, `FormsDbContext`, `NotificationsDbContext`)
- Each adds ~10 sec to deploy
- First deploy will create: `notifications.outbox`, `notifications.outbox_dead_letter`, `notifications.idempotency_keys`, `media.*` operational tables, `forms.*` operational tables, `media.__EFMigrationsHistory`, `forms.__EFMigrationsHistory`
- **Critical**: NO entity table relocation yet (the Baseline migrations are no-op for entity tables; only operational tables get created)

### Phase 3 — Ghost migration: snapshot purge
- Run `dotnet ef migrations add Phase3_PurgeLeakedModuleTypesFromSnapshot --context AppDbContext`
- EF auto-emits DropTable for the 7 leaked types (PhotoAlbum, AlbumPhoto, EventForm + 3 form-children, Notification dual-leak)
- **Manually empty the Up()/Down() bodies** — keep them as comment-only
- The auto-rewritten snapshot (now free of leaks) stays as-is
- Add `/// SCHEMA-DESTRUCTIVE-APPROVED: snapshot-only correction` header
- CI passes (empty Up() = no destructive DDL pattern matches the lint regex)
- Migration row records in `__EFMigrationsHistory` on apply, but no SQL runs
- Live tables untouched; snapshot is now truth
- ~95 min wall-clock

### Phase 1 — IAuditable AddColumn pass, per-schema (10 groups)
- Group 1.1 Identity (8 tables) — 32 AddColumn
- Group 1.2 Events core (12 tables) — 48 AddColumn
- Group 1.3 Registrations & seating (15 tables) — 60 AddColumn (largest)
- Group 1.4 Financials (10 tables) — 40 AddColumn (Sponsor has pre-existing columns; wrap IF NOT EXISTS)
- Group 1.5 Communications (8 tables) — 32 AddColumn
- Group 1.6 Sign-ups (5 tables) — 20 AddColumn
- Group 1.7 Cultural reference data (2 tables) — 8 AddColumn
- Group 1.8 Admin & support (2 tables) — 8 AddColumn
- Group 1.9 Photo albums (MediaDbContext, AFTER Phase 2) — 8 AddColumn
- Group 1.10 Forms (FormsDbContext, AFTER Phase 2) — 16 AddColumn

**Each group ships as its own commit + own deploy + own staging soak (~45 min per group, ~12.5 hr total spread over 2-3 working days).**

### Phase 2 — Per-module schema rename
- **Media** (~90 min): `ALTER TABLE public.photo_albums SET SCHEMA media` + `ALTER TABLE public.album_photos SET SCHEMA media`
- **Forms** (~120 min): 4 tables to `forms.*` schema
- **Communications** (Wave 8): 8 tables, highest write traffic, requires Q4 Option B (30-sec scale-to-zero)
- **Identity** (Wave 8 ONLY): never run on staging in isolation; bundle with production cutover

Wrap each `ALTER TABLE ... SET SCHEMA` in `DO $$ IF EXISTS ... END$$` idempotent block. Down() reverses to `public`. Use `SCHEMA-DESTRUCTIVE-APPROVED:` header.

---

## Section 5 — Architect-Level Guardrails

### 5.1 New CI / ArchTest rules per phase

- **Phase 0.7**: extend migration-lint to fail if a new module migration lacks a corresponding CI apply step in `deploy-staging.yml`
- **Phase 3**: extend migration-lint to flag empty-Up() bodies without `SCHEMA-DESTRUCTIVE-APPROVED` header
- **Phase 1**: new ArchTest — every IAuditable-derived entity in AppDbContext must have all 4 audit columns configured
- **Phase 2**: extend migration-lint regex to detect raw `ALTER TABLE ... SET SCHEMA` in `Sql(...)` blocks
- **Phase 2**: new ArchTest — no cross-context FK declarations (codifies Q3)

### 5.2 Pre-flight scripts (run before each migration)

Concrete `psql` queries to capture row counts, pre-existing audit columns, FK inventory. Full templates in plan body.

### 5.3 Smoke-test commands per phase (post-deploy)

Authentication step + per-phase verification curl commands + per-phase `psql` row-count comparisons. Full templates in plan body.

### 5.4 Production cutover differences (Wave 8)

PITR retention extends to 35 days; traffic-weight shift required (5% → 25% → 50% → 75% → 100% over a working day); off-peak window mandatory (Saturday 02:00 UTC); founder + on-call approval gate; post-mortem required within 24 hr of each phase; cross-surface smoke matrix; operator UAT browser smoke before traffic shift past 25%.

**Implication**: Wave 8 requires its own `docs/PHASE_8_PRODUCTION_CUTOVER.md`. The plan in this document is **staging-only**.

---

## Section 6 — Founder Decisions Required (Execution Gate)

| Q | Question | Recommended | Status to start Phase 0.5 |
|---|---|---|---|
| Q1 | Consolidate two migration directories? | **YES (Option A)** | 🚩 BLOCKING |
| Q2 | Schema names `media`/`forms`/`communications`/`identity` lowercase snake_case? | **YES** | 🟢 CAN-DEFAULT |
| Q3 | No DB-level cross-context FKs (codify as ArchTest)? | **YES** | 🟢 CAN-DEFAULT |
| Q4 | Acceptable staging downtime per migration? | **Option A (no app pause) for everything except Phase 2 Comm/Identity (Option B 30-sec scale-to-zero)** | 🚩 BLOCKING for Phase 2 Comm+Identity only |
| Q5 | IAuditable backfill on existing rows? | **NULL** | 🟢 CAN-DEFAULT |
| Q6 | Migration discipline header text format? | **`SCHEMA-DESTRUCTIVE-APPROVED:` on XML doc** | 🟢 CAN-DEFAULT |
| Q7 | Module-context CI deploy step? | **Option A (sequential in deploy-staging.yml)** | 🚩 BLOCKING for Phase 0.7 |
| Q8 | Phase ordering 0.5→0.7→3→1→2? | **YES (architect-reaffirmed)** | 🚩 BLOCKING |
| Q9 | Phase 1 IAuditable per-schema (10 migrations)? | **YES (Option B)** | 🚩 BLOCKING for Phase 1 |
| Q10 | Restore-point UTC capture mechanism? | **Inline commit message** | 🟢 CAN-DEFAULT |

**Phase 3 additional gate**: founder must explicitly accept the "ghost migration" pattern (empty Up()/Down() on a recorded migration row) as architecturally acceptable.

**Minimum subset to unblock Phase 0.5**: Q1, Q8 — both blocking, recommended **YES + YES**.

---

## Section 7 — Relevant File Paths

- `src/LankaConnect.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` — the 9555-LOC stale snapshot (§1.4 leaked types; Phase 0.5 moves; Phase 3 purges)
- `src/LankaConnect.Infrastructure/Migrations/` — old migrations directory (Phase 0.5 dissolves)
- `src/LankaConnect.Infrastructure/Data/Migrations/` — newer migrations directory (canonical home after Phase 0.5)
- `src/LankaConnect.Infrastructure/Data/AppDbContext.cs:504-509` — `configuredEntityTypes` allow-list + `modelBuilder.Ignore(type)` fallback (makes Phase 3 snapshot purge work)
- `src/LankaConnect.Infrastructure/DependencyInjection.cs:81` — `MigrationsAssembly("LankaConnect.Infrastructure")` (proves folder-merge is runtime-neutral)
- `src/Modules/Notifications/Notifications.Infrastructure/Data/NotificationsDbContext.cs` — `__EFMigrationsHistory` scoped to `notifications` schema
- `src/Modules/Media/Media.Infrastructure/Data/MediaDbContext.cs` — `SchemaName = "media"` constant
- `src/Modules/Forms/Forms.Infrastructure/Data/FormsDbContext.cs` — `SchemaName = "forms"` constant
- `.github/workflows/deploy-staging.yml:127` — single AppDbContext apply (Phase 0.7 extends)
- `.github/workflows/pr-validation.yml:78-165` — Phase 0 migration-lint (Section 5.1 guardrails extend)
- `docs/operations/migration-rollback.md` — PITR rollback runbook (Phase 0 already shipped)

---

**END OF PLAN** — Awaiting founder approval on the 4 blocking Qs (Q1, Q7, Q8, Q9 + the Phase 3 ghost-migration concept).
