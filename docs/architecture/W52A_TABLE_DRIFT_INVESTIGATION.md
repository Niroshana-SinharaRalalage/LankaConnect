# Wave 5.2.a Schema-Drift Investigation Dossier

**Status**: 🛑 BLOCKED on founder decision. W5.2 sequence paused.
**Created**: 2026-06-28 03:05 UTC
**Last Updated**: 2026-06-28 03:05 UTC

---

## TL;DR for founder

`event_passes` and `pass_purchases` tables **do not exist anywhere in the staging database**, despite the four EF migrations that should have created them being recorded as applied in `__EFMigrationsHistory`. The drift was invisible until W5.2.a's `HasMany<EventPass>` fix (commit `9d9c2e78`) made EF generate real SQL against these tables for the first time, exposing the missing tables via HTTP 500 on `POST /api/Events/{id}/passes`.

**Three indistinguishable hypotheses** for how the tables disappeared. The architect's ruling is that the root-cause question is a **product question** ("do we still want the EventPass feature?") which only you can answer. The technical fix is upstream-blocked.

**Three questions for you** are listed at the bottom. Reading time: ~5 minutes.

---

## Timeline

| Time (UTC) | Event |
|---|---|
| 02:11 | W5.2.a commit `9d9c2e78` pushed: `EventConfiguration.HasMany(e => e.Passes).WithOne().HasForeignKey("EventId")` to fix Phase-6AX-era `AddPass` 0-changes-committed bug |
| 02:22 | Staging deploy succeeded (11m23s) |
| 02:35 | S2 full-lifecycle smoke: `POST /api/Events/{id}/passes` → **HTTP 500** |
| 02:36 | Container log: `Npgsql.PostgresException 42P01: relation "event_passes" does not exist` |
| 02:38 | Reproduced — failure is deterministic on every POST |
| 02:42 | Architect escalation: confirmed `HasMany` config is correct; failure is a deeper pre-existing drift |
| 02:55 | β-2 probe via Npgsql one-shot using Key Vault connection string: tables confirmed missing |
| 03:05 | Architect ruling: STOP, write dossier, wake founder, do not push destructive DDL |

---

## Evidence

### 1. EF migration history (staging `__EFMigrationsHistory`)

```
20251123072228_AddEventPassAndPassPurchaseEntities          (Nov 23)
20251123072848_AddEventPassAndPassPurchaseTables            (Nov 23)
20251123163612_AddSignUpListAndSignUpCommitmentTables       (Nov 23)
20251129201535_AddSignUpItemCategorySupport                 (Nov 29)
... (all 253 subsequent migrations through 2026-06-28)
20260628021141_Wave5_2a_AddPassHasManyConfiguration         (TODAY)
```

All 257 total migrations recorded as applied.

### 2. Actual table inventory in staging DB

Probed via Npgsql one-shot using `database-connection-string` from container app secrets. **Zero tables match `%pass%` or `%purchase%`** across any schema. `events` schema has 30+ tables (events, sign_up_*, registration_*, refund_*, tickets, seats, sponsors, venue_*, donations, collections, add_on_*, etc.) — but no `event_passes`, no `pass_purchases`.

Confirmed schemas in DB: `analytics, badges, business, communications, community, events, forms, hangfire, identity, media, notifications, payments, public, reference_data, support, users`. Searched all.

### 3. EF SQL signature

Container log inner exception (`Position: 13` in the SQL string) confirms the failing SQL is **unqualified `INSERT INTO event_passes (...)`** — no schema prefix. Matches the EF model snapshot: `b.ToTable("event_passes", (string)null)` (no schema = `public`).

### 4. Source-code archaeology

Inspected all four `pass`-related migration `.cs` files + grep'd every `Migrations/*.cs` for `DropTable` against `event_passes` / `pass_purchases`:

| Migration | Up()/Down() Body | Notes |
|---|---|---|
| `20251123072228_AddEventPassAndPassPurchaseEntities` | **Literally empty** (always was — first git commit) | Probably scaffolding stub later superseded |
| `20251123072848_AddEventPassAndPassPurchaseTables` | **Literally empty** (always was) | Same as above |
| `20251123163612_AddSignUpListAndSignUpCommitmentTables` | Up: CreateTable for `event_passes` + `pass_purchases` + `sign_up_lists` + `sign_up_commitments` (no schema = public). Down: matching DropTable. | First commit `f9b0b129` Nov 24 with this content. Never edited. |
| `20251129201535_AddSignUpItemCategorySupport` | Up starts with `DropTable(event_passes)` + `DropTable(pass_purchases)` then later `CreateTable(event_passes, ...)` + `CreateTable(pass_purchases, ...)` (no schema = public) | Drop-and-recreate pair |

**No later migration drops these tables.** Grep across every `Migrations/*.cs` for `DropTable.*event_passes` returns only the in-Down-of-163612 and in-Up-of-201535 occurrences above.

The other two tables created by migration `163612` (`sign_up_lists`, `sign_up_commitments`) **DO exist** in staging in the `events` schema. So 163612 partially succeeded (sign_up_* worked) but event_passes + pass_purchases didn't end up persisted.

### 5. Cross-validation of drafted recovery schema against current EventPassConfiguration

The architect's pre-push gate explicitly required this check. Result:

| Column | EventPassConfiguration (post-W5.1.a-α scalar refactor) | Drafted recovery migration | Match? |
|---|---|---|---|
| `name` | varchar(100) NOT NULL | varchar(100) NOT NULL | ✅ |
| `description` | varchar(500) NOT NULL | varchar(500) NOT NULL | ✅ |
| `price_amount` | `HasPrecision(18, 2)` NOT NULL → numeric(18,2) | numeric(18,2) NOT NULL | ✅ |
| `price_currency` | `HasMaxLength(3)` + `HasConversion<string>()` NOT NULL → varchar(3) | varchar(3) NOT NULL | ✅ |
| `total_quantity` | int NOT NULL | int NOT NULL | ✅ |
| `reserved_quantity` | int NOT NULL default 0 | int NOT NULL DEFAULT 0 | ✅ |
| `event_id` | shadow FK Guid NOT NULL | uuid NOT NULL FK → events.events(Id) ON DELETE CASCADE | ✅ |
| `created_at` / `updated_at` | timestamp tz | timestamp tz | ✅ |
| Schema | `builder.ToTable("event_passes")` no schema = public | public | ✅ |

PassPurchaseConfiguration cross-check: same. `total_price_amount` numeric(18,2), `total_price_currency` varchar(3), all match.

**Drafted schema is consistent with current EF model.** The precision-mismatch concern the architect flagged does not apply: my draft uses numeric(18,2) matching the current `HasPrecision(18, 2)` — NOT numeric(18,4) that BuildingBlocks Money convention defaults to (we're using the scalar-decomposed pattern, not the BB convention).

---

## Three indistinguishable hypotheses

| ID | Hypothesis | Evidence for | Evidence against |
|---|---|---|---|
| **Y-1** | Migration 201535's `DropTable(event_passes)` ran against a DB where the table didn't exist (because 163612's CreateTable had silently failed earlier), erroring the migration which was then somehow recorded as applied via hand recovery | Possible PostgreSQL DDL race or constraint conflict | EF generates plain `DROP TABLE` not `DROP TABLE IF EXISTS`, so error would have been visible in deploy logs; no record of such an error |
| **Y-2** | Manual `DROP TABLE event_passes; DROP TABLE pass_purchases;` executed directly against staging DB at some point, outside the migration system | Founder is the only operator with DB access; EventPass feature has no UI surface — the kind of table someone would drop intentionally during cleanup; no migration captures it because it lived only in DB state | No audit log accessible to confirm |
| **Y-3** | Azure PITR rollback that somehow affected only these tables | CLAUDE.md §5 rule 4 mentions PITR as the rollback mechanism | PITR is whole-DB, not selective — very unlikely; would have reset way more tables |

**Y-2 is the most plausible** — but I cannot confirm it without your input.

---

## What I want to do but am NOT doing tonight (architect-blocked)

### Drafted migration (NOT pushed)

```csharp
// SCHEMA-DESTRUCTIVE-APPROVED: net-new DDL to restore event_passes + pass_purchases
// tables missing from staging DB (root cause unclear; see this dossier).
public partial class Wave5_2a_fix_CreateMissingEventPassPassPurchaseTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent: CREATE TABLE IF NOT EXISTS via raw Sql() so this is safe
        // to apply even if the tables exist in some other environment.
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS public.event_passes (
                ""Id"" uuid NOT NULL,
                event_id uuid NOT NULL,
                name varchar(100) NOT NULL,
                description varchar(500) NOT NULL,
                price_amount numeric(18,2) NOT NULL,
                price_currency varchar(3) NOT NULL,
                total_quantity integer NOT NULL,
                reserved_quantity integer NOT NULL DEFAULT 0,
                created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                updated_at timestamp with time zone,
                CONSTRAINT ""PK_event_passes"" PRIMARY KEY (""Id""),
                CONSTRAINT ""FK_event_passes_events_event_id""
                    FOREIGN KEY (event_id) REFERENCES events.events(""Id"") ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_event_passes_event_id ON public.event_passes(event_id);
        ");
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS public.pass_purchases (
                ""Id"" uuid NOT NULL,
                user_id uuid NOT NULL,
                event_id uuid NOT NULL,
                event_pass_id uuid NOT NULL,
                quantity integer NOT NULL,
                total_price_amount numeric(18,2) NOT NULL,
                total_price_currency varchar(3) NOT NULL,
                status varchar(20) NOT NULL DEFAULT 'Pending',
                qr_code varchar(200) NOT NULL,
                confirmed_at timestamp with time zone,
                cancelled_at timestamp with time zone,
                created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                updated_at timestamp with time zone,
                CONSTRAINT ""PK_pass_purchases"" PRIMARY KEY (""Id"")
            );
            CREATE INDEX IF NOT EXISTS ix_pass_purchases_user_id ON public.pass_purchases(user_id);
            CREATE INDEX IF NOT EXISTS ix_pass_purchases_event_id ON public.pass_purchases(event_id);
            CREATE INDEX IF NOT EXISTS ix_pass_purchases_event_pass_id ON public.pass_purchases(event_pass_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_pass_purchases_qr_code ON public.pass_purchases(qr_code);
            CREATE INDEX IF NOT EXISTS ix_pass_purchases_event_user_status ON public.pass_purchases(event_id, user_id, status);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS public.pass_purchases;");
        migrationBuilder.Sql("DROP TABLE IF EXISTS public.event_passes;");
    }
}
```

**Why this is NOT shipped tonight:**

1. The schema is a guess validated against current source. If prod has the tables in a different schema or shape, `IF NOT EXISTS` only checks name — would create a second `public.event_passes` while data lives elsewhere.
2. Unsupervised destructive DDL at 03:00 UTC violates CLAUDE.md §5 + §13 gates.
3. The product question (do you still want the feature?) is upstream of the technical fix.

---

## Three questions for founder

### Q1 — Do you remember manually dropping these tables?

> Do you recall manually dropping `event_passes` and `pass_purchases` from staging at any point — possibly during Phase 6A cleanup, or when the EventPass feature was paused/abandoned? This is the most plausible Y-2 hypothesis. A simple "yes I dropped them on [date] because [reason]" resolves Y-1/Y-2/Y-3 instantly.

### Q2 — Is the EventPass / PassPurchase feature still in scope?

> Is the EventPass / PassPurchase feature still in scope for the LankaEvents product? Findings suggest it may have been abandoned mid-build:
> - All 146 events in staging have `passCount=0` (no operator has ever created a pass through any UI)
> - No public POST endpoint exists for PassPurchase in `LankaConnect.API/Controllers/` (grep confirmed: zero hits for `passes/purchase` or `purchase-pass`)
> - The handler `AddPassToEventCommandHandler` has been silently broken since Phase 6AX (0-changes-committed) and nobody noticed because nothing depended on the data
>
> If the feature is paused/abandoned, the correct fix is to **remove** the EF entities + handlers + tests, not recreate the tables. W5.2.b would carve forward without this dead code.
>
> If the feature is still in scope, we need to know which schema target (Q3).

### Q3 — If we keep the feature: which schema?

> If we ARE keeping the feature, do you want me to recreate the tables in:
> - **(a)** `public` schema (matching the original migrations 163612 + 201535), with a follow-up migration later to move them to `events`, OR
> - **(b)** `events` schema (matching the Wave-4 module-DbContext realignment direction), as a single forward-only operation
>
> Option (b) is cleaner long-term but requires updating `EventPassConfiguration.cs` + `PassPurchaseConfiguration.cs` to `builder.ToTable("event_passes", "events")` and regenerating a new migration with that schema. Option (a) ships faster and matches what other Event-family tables briefly were (before Wave 4.9 realignment).

---

## Current state of staging (as of 03:05 UTC)

- **Backend deploy**: green at `9d9c2e78`. Container app healthy. Serving traffic.
- **Frontend**: unaffected (no UI surface for EventPass).
- **Regression**: `POST /api/Events/{id}/passes` returns 500 (where previously returned 200 with silent 0-changes-committed). Zero user impact — no consumer of this endpoint exists in production.
- **All other endpoints**: unaffected. Log silence verified: zero `42703`/`22P02`/`NpgsqlException`/`InvalidOperationException` on any non-EventPass path.
- **Smoke checks for other Event endpoints**: green (`GET /api/Events/my-events?pageSize=10` returns 147 events; `GET /api/Events/5fbcea92-...` returns paid fixture with `ticketPriceAmount=18.0`, `ticketPriceCurrency="USD"`).

The loud 500 is the signal. The architect was deliberate about NOT reverting W5.2.a tonight — reverting would restore the silent failure mode (`passCount=0` despite POST → 200) and hide the urgency of Q2.

---

## Wave 5.2 sub-step status

| Sub-wave | Status |
|---|---|
| W5.2.a (HasMany fix) | ✅ Shipped (`9d9c2e78`). Exposed the schema drift. The fix itself is correct. |
| W5.2.a-fix (table recovery) | ⏸ BLOCKED on Q1/Q2/Q3 answers |
| W5.2.b (Commands carve-out, ~150 files) | ⏸ BLOCKED on Q2 answer (don't move dead code into Products) |
| W5.2.c (Queries carve-out, ~80 files) | ⏸ BLOCKED on Q2 |
| W5.2.d (BackgroundJobs + Services + Common stragglers) | ⏸ BLOCKED on Q2 |

---

## Recommended next steps after founder answers

**If Q2 = "abandoned":**
- Delete `EventPass.cs`, `PassPurchase.cs` from `Products/LankaEvents/LankaEvents.Domain/Entities/`
- Delete `EventPassConfiguration.cs`, `PassPurchaseConfiguration.cs`
- Delete `AddPassToEventCommandHandler` + DTO + request + tests
- Remove EventPass + PassPurchase from `IgnoreUnconfiguredEntities` whitelist + ApplyConfiguration list in `AppDbContext`
- Remove `HasMany(e => e.Passes)` from EventConfiguration
- Remove `_passes` collection + `Passes` property + `AddPass`/`RemovePass` from `Event.cs`
- Generate cleanup migration (empty-Up rebaseline since no DDL needed — tables don't exist anyway)
- Proceed to W5.2.b without the dead code

**If Q2 = "still in scope" + Q3 = (a) public schema:**
- Push the drafted `Wave5_2a_fix_CreateMissingEventPassPassPurchaseTables` migration as-is
- Wait for deploy + re-run S2 smoke
- Proceed to W5.2.b

**If Q2 = "still in scope" + Q3 = (b) events schema:**
- Update `EventPassConfiguration.cs`: `builder.ToTable("event_passes", "events")`
- Update `PassPurchaseConfiguration.cs`: `builder.ToTable("pass_purchases", "events")`
- Regenerate the recovery migration with `events` schema in the CREATE TABLE
- Push, deploy, S2 smoke, proceed to W5.2.b

---

## Architect ruling reference

Architect specifically ruled against three actions tonight:
1. ❌ Push the SCHEMA-DESTRUCTIVE-APPROVED migration
2. ❌ Probe prod DB at 03:00 UTC alone
3. ❌ Revert W5.2.a (would hide the signal)

Architect approved:
1. ✅ Write this dossier as a docs-only commit
2. ✅ Wait for founder to wake and answer Q1/Q2/Q3
3. ✅ Continue holding W5.2.b/c/d

Quote: *"Archaeology that produces three unfalsifiable hypotheses is the signal to escalate, not to pick the most plausible and ship a fix for it."*

---

## Honest summary for founder

W5.1 closed clean (you'd be proud). W5.2.a shipped and did its real job — surfacing a months-old silent bug. The fix that surfaced it is correct; the surfaced bug is upstream of W5.2 scope and is a product decision, not a schema decision. Holding for your call.

No unsupervised heroics, no destructive operations, no compromised prod access. Will pick up your answer immediately when you wake.
