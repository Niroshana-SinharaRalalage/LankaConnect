# Architect Ruling — Wave 6.5.f.5-hotfix Follow-up (Missing ConfigureSchemas in LankaEventsDbContext)

**Date**: 2026-07-04
**Participants**: Founder (Niroshana, executing agent), System Architect (Claude Opus 4.7 persona)
**Status**: BINDING — supersedes any sub-slice-level self-authorization taken since the earlier Option B ruling
**Related**: `docs/architect-consults/2026-07-04-wave-6-5-f-regression-ruling.md` (Option B ruling — successful mechanically, insufficient in scope); `docs/architect-consults/2026-07-02-wave-6-5-scope-shape.md` §5 hard-STOP #2 + §5 hard-STOP #7; Blueprint §7.4 (module DbContext ownership), §7.16 (LankaEventsDbContext extraction)

---

## 1. Diagnosis — the second, deeper defect

The Option B hotfix WAS correct as written. Every §3 acceptance criterion passed locally. But the ruling — and the 6.5.e work it built on — was measuring the wrong invariant.

**The invariant the ruling checked**: "every DbSet<T> in LankaEventsDbContext has a corresponding mapped IEntityType in the finalized model."

**The invariant that was ALSO required but NOT checked**: "every mapped IEntityType in LankaEventsDbContext resolves to the SAME physical `(schema, table_name)` pair that AppDbContext maps the same entity to, which is the same pair the physical Postgres schema uses."

The two contexts have divergent table-name resolution paths:

| Context | Table-name source | Result for `Event` | Result for `SignUpList` |
|---|---|---|---|
| `AppDbContext` | Explicit `.ToTable("events", "events")` in `ConfigureSchemas()` line 443 | `events.events` (correct) | `events.sign_up_lists` (correct) |
| `LankaEventsDbContext` | `HasDefaultSchema("events")` + DbSet<T> property name (no ToTable in EventConfiguration) | `events.Events` (WRONG — PascalCase) | `events.SignUpLists` (WRONG — PascalCase) |

`EventConfiguration.cs` has ZERO `.ToTable(...)` at builder level. Same for `SignUpListConfiguration.cs`, `SignUpItemConfiguration.cs`, `SignUpCommitmentConfiguration.cs`. EF Core 8 under `HasDefaultSchema("events")` names the table `<DbSet-property-name>` — for `public DbSet<Event> Events`, the table becomes `events.Events` (PascalCase preserved). Postgres, being case-sensitive on quoted identifiers, refuses the query.

**Why the parity test §3.4 passed anyway**: the parity test authored per §5.Q5 asserts `context.Model.FindEntityType(typeof(T)) != null` — mapping presence. It never inspects `GetSchema()` or `GetTableName()`. Both entities WERE mapped in the model. The test was correct at what it asserted; it just asserted the wrong thing.

**Why this went undetected in 6.5.e**: 6.5.e made LankaEventsDbContext DI-registered but write-side dormant — no repository queried through it. Wave 9 smoke exercised only AppDbContext-backed paths, and AppDbContext has explicit `.ToTable(...)` overrides in `ConfigureSchemas()` for every LankaEvents entity. The dual-mapping design of 6.5.e held at runtime purely because the failing context never served a query.

**Why the pre-hotfix state (46 failures) MASKED this defect**: the un-`Ignore<>` collision on `EventEmailGroupLink` / `EventBadge` fires at model-build time, which happens on FIRST query of any type through the context. The model-build exception threw BEFORE EF ever composed a SQL query against `events.Events`. The Option B hotfix cleared the model-build path — every query now composes cleanly — and immediately unmasked the underlying table-name defect on every read and write of the aggregate roots.

**This is not a regression the Option B hotfix introduced**. This defect has been latent in `LankaEventsDbContext` since it was extracted in Wave 6.5.e. The Option B hotfix made the failure visible sooner by removing the earlier crash surface; the founder's diagnosis of "made it worse" is measurement-view-only — the code is not worse, the failure mode is louder.

---

## 2. Scope-of-defect ruling — the defect is BROAD, not localized

**Divergences the founder's grep undercounted**:

| Entity | Config file | LankaEventsDbContext resolves to | Physical Postgres | Divergent? |
|---|---|---|---|---|
| `Event` | EventConfiguration.cs (no ToTable) | `events.Events` | `events.events` | YES |
| `SignUpList` | SignUpListConfiguration.cs (no ToTable) | `events.SignUpLists` | `events.sign_up_lists` | YES |
| `SignUpItem` | SignUpItemConfiguration.cs (no ToTable) | `events.SignUpItems` | `events.sign_up_items` | YES |
| `SignUpCommitment` | SignUpCommitmentConfiguration.cs (no ToTable) | `events.SignUpCommitments` | `events.sign_up_commitments` | YES |
| `Ticket` | TicketConfiguration.cs (PascalCase) | `events.Tickets` | `events.tickets` | YES |
| `EventBadge` | EventBadgeConfiguration.cs | `events.event_badges` | `badges.event_badges` | YES (SCHEMA) |
| `EventAnalytics` | EventAnalyticsConfiguration.cs | `events.event_analytics` | `analytics.event_analytics` | YES (SCHEMA) |
| `EventViewRecord` | EventViewRecordConfiguration.cs | `events.event_view_records` | `analytics.event_view_records` | YES (SCHEMA) |

**At least 8 concrete divergences** (5 table-name, 3 schema).

**Ruling on the scope question**: The defect is BROAD across LankaEventsDbContext. It is NOT localized to Event/Registration. The founder was correct to escalate before code-change.

---

## 3. Binding decision — Option E (config-embedded ToTable), executed as sub-slice 6.5.f.5-hotfix2

**Rejected**: Option D (add ConfigureSchemas mirror to LankaEventsDbContext) — perpetuates the "mapping intent in two places" trap.
**Rejected**: Option F (revert 6.5.f.5 + hotfix1) — re-arms the Ignore<> trap AND doesn't fix the underlying invariant hole.
**Adopted**: **Option E** — move the `.ToTable(schema, name)` mappings INTO each swept `IEntityTypeConfiguration` file, making every config self-contained and physical-schema-authoritative.

### 3.3 What Option E ships

1. `.ToTable("events", "events")` → `EventConfiguration.Configure()`.
2. `.ToTable("sign_up_lists", "events")` → `SignUpListConfiguration.Configure()`.
3. `.ToTable("sign_up_items", "events")` → `SignUpItemConfiguration.Configure()`.
4. `.ToTable("sign_up_commitments", "events")` → `SignUpCommitmentConfiguration.Configure()`.
5. `TicketConfiguration.cs`: `.ToTable("Tickets")` → `.ToTable("tickets", "events")`.
6. `EventBadgeConfiguration.cs`: `.ToTable("event_badges")` → `.ToTable("event_badges", "badges")`.
7. `EventAnalyticsConfiguration.cs`: `.ToTable("event_analytics")` → `.ToTable("event_analytics", "analytics")`.
8. `EventViewRecordConfiguration.cs`: `.ToTable("event_view_records")` → `.ToTable("event_view_records", "analytics")`.
9. **VERIFY** `EventEmailGroupLinkConfiguration.cs`: read migration; if physical schema is `events`, no change needed.
10. **VERIFY + ADD explicit `"events"` schema** to every other config that relies on `HasDefaultSchema` (per new Rule 5i).
11. **Update `AppDbContext.ConfigureSchemas()`** — remove every `.ToTable(...)` line for entities whose configs now carry the mapping. Do NOT remove: User (Identity), ForumTopic/Reply (Community), Business/Service/Review (Business), Communications module entities, StateTaxRate (AppDbContext-only).

**Justification**: Mapping intent lives with the entity → single source of truth. Closes ALL divergences at once (schema + table-name). Completes 6.5.e's unfinished config-relocation follow-through.

### 3.4 Extended parity test authored FIRST as red

The parity test authored per §5.Q5 is expanded IN THE SAME COMMIT to assert:
- `Model.FindEntityType(typeof(T)) != null` (mapping presence — already there)
- `Model.FindEntityType(typeof(T))!.GetSchema() == "<expected_schema>"` (schema-parity — NEW)
- `Model.FindEntityType(typeof(T))!.GetTableName() == "<expected_table>"` (table-name-parity — NEW)

The "expected" values are derived from `AppDbContext.Model.FindEntityType(typeof(T))` (the authoritative source of the physical schema mapping). TDD discipline: authored first as failing red test, THEN Option E fix applied, THEN test goes green.

---

## 4. Acceptance criteria (what green looks like on staging THIS time)

1. **Extended parity test authored FIRST as failing red**: `LankaEventsDbContextTableParityTests`. Must be in commit history as evidence of TDD discipline.
2. **Build**: `dotnet build LankaConnect.sln -c Release` clean.
3. **Full unit + integration test suite**: 375/375 or higher.
4. **Snapshot drift check** (CRITICAL): `dotnet ef migrations has-pending-model-changes` for BOTH contexts returns "No pending model changes." **If there IS drift**, HALT and re-consult with the drift diff attached. DO NOT auto-generate a migration.
5. **Local integration test smoke**: exercise `EventRepository.GetByIdAsync`, list query, `RegistrationRepository.GetByIdAsync`, plus one query touching each of Ticket / EventAnalytics / EventBadge.
6. **Staging smoke**: `Run-Wave9.ps1` full run returns to pre-Wave-6.5.f baseline. **Events must go from 62 → ≤ 2**.
7. **No baseline JSON change**: stays at 4 entries.

---

## 5. Sequencing

- **Commit N+1** (this branch): `Wave 6.5.f.5-hotfix2: embed physical (schema, table) in Products.LankaEvents configs + extend parity tests`. Ships after §4 all-green.
- **Commit N+2** (new branch): `Wave 6.5.f.7: DbContext model-build parity tests for all 6 module contexts`. Per prior §5.Q5, includes schema/table parity assertions. Estimated 0.5 sessions.
- **Commit N+3**: `Wave 6.5.f.4` (payments cluster cutover) — UNCHANGED sequencing, still gated on Rule 5c.

**hotfix1 commit is NOT amended and NOT reverted.** Preserves audit trail; two-commit narrative for reviewers.

---

## 6. Rulings on Q6, Q7, Q8

### Q6 — Extended parity tests: YES

**Rule 5e (revised)** — DbContext model-build parity test asserts THREE things: (1) mapping presence, (2) `.GetSchema() == expectedSchema`, (3) `.GetTableName() == expectedTableName`. Values derived from `AppDbContext.Model` (physical-schema authority) for shared entities.

### Q7 — Consult timing: root-cause-then-consult is correct, with 30-min soft cap

**Rule 5h (new)** — Hotfix-branch debug loops are 30-minute soft-capped:

> On any commit labeled `hotfix` on a Wave-blocking regression, the executing agent has 30 minutes of solo root-cause diagnosis time before the correct action is to open an architect consult with raw failure evidence attached, even without a hypothesis.

### Q8 — Rule 5c pre-merge staging-smoke: correctly applied

**Rule 5c (addendum)** — Staging-smoke result determines merge, not local test result:

> When a commit labeled per Rule 5c ships to staging and the smoke returns a FAILURE COUNT GREATER than the pre-branch baseline, that commit is DO-NOT-MERGE regardless of what local tests report.

### Two additional rules

**Rule 5i (new)** — Every `HasDefaultSchema` requires per-config explicit schema:

> Any DbContext that calls `modelBuilder.HasDefaultSchema("<X>")` imposes an obligation on every `IEntityTypeConfiguration<T>` swept into it: `.ToTable(...)` MUST include an explicit schema argument (two-arg form), even if it matches the default. Silent-latent trap otherwise.

**Rule 5j (new)** — Config-relocation commits require a physical-mapping audit:

> Any commit that physically relocates one or more `IEntityTypeConfiguration<T>` files must include an audit section in the commit message listing, for each moved file: (a) OLD context's mapping decisions, (b) NEW context's mapping decisions, (c) delta between them, with explicit `.ToTable(...)` fixes applied inside the moved config to make the two identical.

---

## 9. Ruling summary

Take **Option E**. Embed physical `(table, schema)` into each swept `IEntityTypeConfiguration<T>` file using two-arg `builder.ToTable("<snake_case>", "<schema>")`. Remove now-redundant explicit `.ToTable(...)` overrides for those entities from `AppDbContext.ConfigureSchemas()`. Fix three cross-schema divergences (EventBadge → badges, EventAnalytics/EventViewRecord → analytics). Fix four missing `.ToTable()` calls (Event, SignUpList, SignUpItem, SignUpCommitment). Fix PascalCase divergence on Ticket. Extend the parity test in the same commit to assert `.GetSchema()` and `.GetTableName()` — authored FIRST as red, verified GREEN after the fix.

The hotfix2 fix ships as a NEW commit on `wave-6-5-f-5-hotfix`. Gate merge on §4's seven acceptance criteria. If hotfix2's staging smoke does NOT return to pre-Wave-6.5.f baseline, halt and re-consult with residual `42P01` body attached.

Codify Rules 5h (30-min debug soft cap), 5i (HasDefaultSchema requires per-config explicit schema), 5j (config-relocation commits require physical-mapping audit). Extend Rule 5e to include schema/table-name assertions. Ratify Rule 5c's operation on hotfix1 as correct.
