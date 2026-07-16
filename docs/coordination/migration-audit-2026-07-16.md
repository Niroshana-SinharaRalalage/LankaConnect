# Migration Audit — Wave 8.5.b (LankaConnect.Infrastructure Dismantle)

**Authored:** 2026-07-16 by Agent-CsprojDismantle-B (Tech Lead: Claude).
**Sprint:** Phase A Final Execution Sprint (Wave 8.5.b Phase 1).
**Head commit at audit time:** `aa8babbd` on branch `develop`.

---

## 1. Executive summary

There are **268 EF Core migrations** across the codebase, distributed across
the **7 operational DbContexts** documented in
[`docs/architecture/DBCONTEXT_OWNERSHIP_MATRIX.md`](../architecture/DBCONTEXT_OWNERSHIP_MATRIX.md).

- **250 migrations** (~93%) still live in `src/LankaConnect.Infrastructure/Data/Migrations/`
  under `AppDbContext` ownership.
- **18 migrations** (~7%) already live in per-module `Migrations/` folders and are
  correctly parented to their module DbContexts.

**Wave 8.5.b Part 4 disposition:** ZERO selective relocations of legacy AppDbContext
migrations. Rationale below (§4). All 250 AppDbContext migrations stay in
`src/LankaConnect.Infrastructure/Data/Migrations/` permanently.

**Constraints binding this recommendation:**

1. **Consult #26 Q4** — "Only migrations authored AFTER per-module DbContext
   creation belong in per-module folders. In practice most 506 migrations STAY
   where they are." (Actual count is 250 legacy migrations, not the 506 estimated
   at consult time. Same principle applies.)
2. **Parent (Tech Lead) explicit constraint 2026-07-16 EOD** — "Your Part 4
   selective relocation MUST NOT re-parent old migrations that already applied
   against `public.__EFMigrationsHistory`."
3. **Rule 5** (staging + prod migration history integrity) — re-parenting a
   migration means its recorded application in `__EFMigrationsHistory` would
   be orphaned; EF Core would think it was never applied and re-apply it (or
   fail on duplicate DDL).

---

## 2. Per-context inventory

Migration counts derived from a filesystem scan
(`find src -name "*.Designer.cs" -path "*/Migrations/*"`). Verified against
the docs `DBCONTEXT_OWNERSHIP_MATRIX §2` operational-context table.

Not verified with `dotnet ef migrations list` (would require solution-wide
build + PowerShell EF tools invocation for each of 7 contexts; each context's
per-module `Migrations/` folder is the authoritative source of what will be
applied on next deploy).

### 2.1 `AppDbContext` (250 migrations)

- **Physical folder:** `src/LankaConnect.Infrastructure/Data/Migrations/`
- **Files:** 250 `_*.cs` migration classes + 250 `_*.Designer.cs` snapshot classes +
  `AppDbContextModelSnapshot.cs` + `Resources/` embedded-template folder.
- **History table:** `public.__EFMigrationsHistory` (default schema).
- **Migration range:**
  - Oldest: `20250830150251_InitialCreate.cs`
  - Newest: `20260704163629_Wave6_5_f_5_hotfix2c_RebaselineAppDbContextSnapshot.cs`
- **Ownership assignment:** Category PLAT (per DBCONTEXT_OWNERSHIP_MATRIX §3) —
  entities like ReferenceValue, StateTaxRate, Badge, EventBadge, AdminAuditLog,
  SupportTicket, Stripe primitives, Newsletter/WhatsApp/Community families.
  Cross-referenced from every module; permanent AppDbContext ownership.
- **Recommendation:** **KEEP** all 250 in place.

### 2.2 `LankaEventsDbContext` (6 migrations)

- **Physical folder:** `src/Products/LankaEvents/LankaEvents.Infrastructure/Migrations/`
- **History table:** `events.__EFMigrationsHistory` (per per-module `HasDefaultSchema("events")`
  historical evolution — verified separate history table in DBCONTEXT_OWNERSHIP_MATRIX §2).
- **Migrations:**
  1. `20260704002856_Baseline_LankaEvents`
  2. `20260704013027_Wave6_5_e_AddLankaEventsOperationalTables`
  3. `20260704152036_Wave6_5_f_5_hotfix2b_RebaselineLankaEventsSnapshot`
  4. `20260705124736_SprintDay1_AddAuditColumnsToTicketTiersAndScanLogs`
  5. `20260715230000_Wave8_5_j_NormalizeTicketPriceCurrencyShape`
  6. `20260716130000_Wave8_5_k_NormalizeNumericCurrencyToIsoString`
- **Ownership assignment:** Category MOD (Product).
- **Recommendation:** already correct — no action.

### 2.3 `IdentityDbContext` (2 migrations)

- **Physical folder:** `src/Modules/Identity/Identity.Infrastructure/Migrations/`
- **History table:** `identity.__EFMigrationsHistory`.
- **Migrations:**
  1. `20260709010338_InitialIdentityContext`
  2. `20260711143844_SprintDay7_AddIdentityOperationalTables`
- **Ownership assignment:** Category MOD.
- **Recommendation:** already correct — no action.

### 2.4 `CommunicationsDbContext` (1 migration)

- **Physical folder:** `src/Modules/Communications/Communications.Infrastructure/Migrations/`
- **History table:** `communications.__EFMigrationsHistory`.
- **Migrations:**
  1. `20260711212134_InitialCommunicationsContext`
- **Ownership assignment:** Category MOD.
- **Recommendation:** already correct — no action.

### 2.5 `NotificationsDbContext` (2 migrations)

- **Physical folder:** `src/Modules/Notifications/Notifications.Infrastructure/Migrations/`
- **History table:** `notifications.__EFMigrationsHistory`.
- **Migrations:**
  1. `20260603221324_Baseline_Notifications`
  2. `20260604035116_Add_NotificationsOperationalTables`
- **Ownership assignment:** Category MOD.
- **Recommendation:** already correct — no action.

### 2.6 `MediaDbContext` (3 migrations)

- **Physical folder:** `src/Modules/Media/Media.Infrastructure/Migrations/`
- **History table:** `media.__EFMigrationsHistory`.
- **Migrations:**
  1. `20260607005618_Baseline_Media`
  2. `20260609080000_Phase1_10c_b_AddCreatedByUpdatedByToMediaTables`
  3. `20260610010000_Wave4_9_3_RenameMediaTablesToMediaSchema`
- **Ownership assignment:** Category MOD.
- **Recommendation:** already correct — no action.

### 2.7 `FormsDbContext` (4 migrations)

- **Physical folder:** `src/Modules/Forms/Forms.Infrastructure/Migrations/`
- **History table:** `forms.__EFMigrationsHistory`.
- **Migrations:**
  1. `20260607125345_Baseline_Forms`
  2. `20260609070000_Phase1_10c_a_AddCreatedByUpdatedByToFormsTables`
  3. `20260610020000_Wave4_9_4_RenameFormsTablesToFormsSchema`
  4. `20260611045647_Wave5_2c_AddOwnerEntityToForms`
- **Ownership assignment:** Category MOD.
- **Recommendation:** already correct — no action.

### 2.8 `LankaTemplesDbContext` (0 migrations — scaffold)

- Empty scaffold per Tech Lead D-02 freeze. No migrations authored yet.

---

## 3. Migration ownership matrix

| DbContext | Migrations | Physical folder | Applied against | History-table location |
|---|---:|---|---|---|
| `AppDbContext` | 250 | `src/LankaConnect.Infrastructure/Data/Migrations/` | staging + prod (initial deploy 2025-08-30 onward) | `public.__EFMigrationsHistory` |
| `LankaEventsDbContext` | 6 | `src/Products/LankaEvents/LankaEvents.Infrastructure/Migrations/` | staging (Wave 6.5.e onward, 2026-07-04) | `events.__EFMigrationsHistory` |
| `IdentityDbContext` | 2 | `src/Modules/Identity/Identity.Infrastructure/Migrations/` | staging (4C.e, 2026-07-08) | `identity.__EFMigrationsHistory` |
| `CommunicationsDbContext` | 1 | `src/Modules/Communications/Communications.Infrastructure/Migrations/` | staging (2026-07-11) | `communications.__EFMigrationsHistory` |
| `NotificationsDbContext` | 2 | `src/Modules/Notifications/Notifications.Infrastructure/Migrations/` | staging (Wave 4.0b, 2026-06-03) | `notifications.__EFMigrationsHistory` |
| `MediaDbContext` | 3 | `src/Modules/Media/Media.Infrastructure/Migrations/` | staging (Wave 4.2, 2026-06-07) | `media.__EFMigrationsHistory` |
| `FormsDbContext` | 4 | `src/Modules/Forms/Forms.Infrastructure/Migrations/` | staging (Wave 4.3, 2026-06-07) | `forms.__EFMigrationsHistory` |
| **Total** | **268** | — | — | — |

---

## 4. Part 4 relocation recommendation — KEEP-IN-PLACE for all 250 AppDbContext migrations

### 4.1 Rationale

- **Migration history integrity.** Each of the 250 AppDbContext migrations was
  registered in `public.__EFMigrationsHistory` at the moment it was applied
  (staging + prod). Every row records `MigrationId` + `ProductVersion`. When
  EF Core scans migrations at startup, it correlates registered migrations
  against candidate migrations by exact `MigrationId` match. Re-parenting a
  migration to a different DbContext changes its ownership record — the row
  still exists in `public.__EFMigrationsHistory` but the DbContext scanning at
  boot no longer sees the migration in its own `<schema>.__EFMigrationsHistory`
  table. EF's response depends on the model-snapshot state: silent no-op if
  snapshot matches, or attempt to re-apply the DDL (which fails on `CREATE TABLE`
  duplicates and unique-key violations).
- **Category assignment.** All entities materially represented in the 250
  legacy migrations are Category PLAT per DBCONTEXT_OWNERSHIP_MATRIX §3.
  Ownership is intentionally permanent on `AppDbContext` — moving the
  migrations would misrepresent the ownership matrix and imply relocation
  work that we've explicitly ruled out (Category PLAT never moves off
  AppDbContext).
- **Prod parity.** Staging + prod both have identical `public.__EFMigrationsHistory`
  contents. Any file relocation would require a coordinated `INSERT INTO
  <target-schema>.__EFMigrationsHistory` in both environments concurrent with
  the code deploy. That is a Phase B / operational-runbook concern, not a
  Phase A refactor concern.
- **Consult #7 Delta original ruling.** Category PLAT entities live on
  AppDbContext permanently to avoid `Ignore<T>()` + cross-context-FK-to-Ignored-
  principal complications. The migrations that establish those entities
  should co-locate with the DbContext that owns them.

### 4.2 What KEEP-IN-PLACE explicitly means for Wave 8.5.b tail (Phase 2 / Agent-CsprojDismantle-C)

- `src/LankaConnect.Infrastructure/Data/Migrations/` **does not move** in
  Phase 2. It stays where it is.
- `src/LankaConnect.Infrastructure/Data/AppDbContext.cs` + `DesignTimeDbContextFactory.cs`
  + `UnitOfWork.cs` **do not move** — they are the Category PLAT holders that
  the migrations reference.
- `src/LankaConnect.Infrastructure/Data/Configurations/`,
  `Data/Repositories/`, `Data/Converters/`, `Data/Seeders/` — evaluated file-by-file
  in Wave 8.5.b Part 5; nearly all remain Category PLAT and stay put (see
  Part 5 executed commits: `73c4ebe5` → `aa8babbd`).
- **Consequence for CsprojDismantle-C:** the csproj `LankaConnect.Infrastructure.csproj`
  **cannot be deleted** in this sprint window. There are 250 EF migrations
  physically anchored to this csproj (via SDK-style implicit include). Deleting
  the csproj would orphan them. The Tech Lead's D-04 fallback ("keep as empty
  compat holder") is the correct disposition — but the csproj is not empty; it
  still holds Category PLAT DbContext + configs + repos + migrations. Rename
  to something clearer if desired (e.g. `Platform.Infrastructure.csproj`
  hosting Category PLAT), but do not delete.

### 4.3 Post-sprint follow-up (out of scope for this sprint)

If a future wave decides to physically relocate the 250 legacy migrations to
per-module folders (retroactive re-parenting), the operational runbook must:

1. Draft the new folder layout + snapshot-file split (`AppDbContextModelSnapshot`
   → per-module snapshots).
2. Author idempotent SQL scripts that MOVE rows from
   `public.__EFMigrationsHistory` to the target `<schema>.__EFMigrationsHistory`
   tables, keyed on `MigrationId`.
3. Deploy the code change + the SQL cutover under a maintenance window.
4. Verify each module context's `EF migrations list` output against expected
   post-move state.

None of that is Phase A scope. Do not attempt it in this sprint.

---

## 5. Part 5 execution summary (non-migration file relocations)

Landed in the following commits on `develop` between `73c4ebe5` and `aa8babbd`:

| Commit | Description |
|---|---|
| `73c4ebe5` | Delete unreferenced `Class1.cs` stub |
| `275d6e42` | Relocate `Security/` (2 files) → `Identity.Infrastructure/Security/` |
| `9f53a243` | Relocate `Services/TimeZoneLookupService.cs` → `LankaEvents.Infrastructure/Services/` |
| `3337701c` | Relocate `Services/Validation/` (2 files) → `LankaConnect.API/Services/Validation/` |
| `aa8babbd` | Relocate `Templates/Email/` (9 files) → `Communications.Infrastructure/Templates/Email/` + csproj Content cutover |

All 5 commits carry Rule 5j config-relocation audit lines in their bodies +
`T-triggers:` / `S-class:` annotations per CLAUDE.md §13.

---

## 6. Handoff summary for CsprojDismantle-C (Wave 8.5.b Phase 2)

CsprojDismantle-C is the Wave 3 successor picking up Wave 8.5.b tail. Key
inputs for that agent:

### 6.1 LankaConnect.Infrastructure content shape post-Wave 8.5.b Part 5

Remaining content (post commit `aa8babbd`):
- `Data/AppDbContext.cs`, `Data/DesignTimeDbContextFactory.cs`, `Data/UnitOfWork.cs`
- `Data/Configurations/` (23 files — all Category PLAT: Newsletter/WhatsApp/Forum/
  Reply/AdminAudit/SupportTicket/Badge/StateTaxRate/ReferenceData)
- `Data/Configurations/ReferenceData/` (4 files — ReferenceValue configs)
- `Data/Converters/` (2 files — Money converters, cross-cutting)
- `Data/Migrations/` (~502 files: 250 pairs + snapshot + 1 orphan) + `Data/Migrations/Resources/` (18 embedded templates)
- `Data/Repositories/` (12 files + `Repository<T>` base) — Category PLAT repos
- `Data/Seeders/` (4 files: BadgeSeeder, EventSeeder, EventTemplateSeeder, MetroAreaSeeder)
- `LankaConnect.Infrastructure.csproj` + `LankaConnect.Infrastructure.csproj.lscache`

### 6.2 CsprojDismantle-C disposition recommendation

**Do NOT attempt csproj deletion in this sprint.**

Per §4.2 above, `LankaConnect.Infrastructure.csproj` remains the Category PLAT
holder. The path forward that satisfies both architect Consult #28 R5 ("Do NOT
delete this month; dismantle in-place") and founder "nothing deferred":

**Recommendation A — Keep csproj as-is, rebrand internally**
- Update `Description` in csproj to describe its current post-dismantle
  purpose: "Category PLAT DbContext holder (AppDbContext + cross-cutting
  entity configs + repos + 250 legacy migrations). Wave 8.5.b Phase 2 verified
  the remaining content is intentional — no further file relocation without
  ADR + architect approval per DBCONTEXT_OWNERSHIP_MATRIX §3."
- Leave `LankaConnect.Infrastructure.csproj` as the canonical Category PLAT
  Infrastructure csproj. This is the honest documentation of the modular-
  monolith reality: Category PLAT exists, needs a host csproj, this is it.
- **CsprojDismantle-C's ArchTest additions**: add an ArchTest rule enforcing
  that any new entity added to LC.Infra/Data/Configurations/ must be either
  ReferenceValue-family, Newsletter-family, WhatsApp-family, or explicitly
  ADR-blessed as Category PLAT.

**Recommendation B (optional) — Physical rename to `Platform.Infrastructure`**
- If founder wants cleaner semantics, physically rename the csproj + folder
  `LankaConnect.Infrastructure` → `Platform.Infrastructure` (mirroring the
  Wave 8.5.c `LankaConnect.API` → `Hosts/Host.AllInOne` rename that
  Agent-ApiRename executes in parallel).
- Requires: `git mv` folder + rename csproj + update all `ProjectReference`
  paths (grep-and-fix ~15 csprojs) + regenerate `.sln` file. Higher risk of
  cold-restore MSB4006 (same risk profile as Wave 6.5.f Day 5 slot A that we
  already survived).
- This is a cosmetic move; the underlying category (PLAT holder) is
  unchanged.

**Recommendation NOT to execute either B in this sprint** — the semantic
value is low, the cold-restore risk is nonzero, and it does not advance the
"nothing deferred" objective (founder's target is a working refactored
codebase, not a semantically pure csproj graph). Log Recommendation B in
`docs/PHASE_A_5_PLAN.md` post-sprint debt.

### 6.3 Cross-cutting csproj cleanup CsprojDismantle-C MAY execute

- Verify `LankaConnect.Infrastructure.csproj` no longer needs its
  `ProjectReference` to `LankaConnect.Application` once CsprojDismantle-A
  deletes that csproj. If A's Part 4 lands successfully, C removes the line
  4 `<ProjectReference Include="..\LankaConnect.Application\LankaConnect.Application.csproj" />`.

---

## 7. Change log

- **2026-07-16 23:28 UTC** — Audit initial version, authored by
  Agent-CsprojDismantle-B post Part 5 execution. Head commit `aa8babbd`.
