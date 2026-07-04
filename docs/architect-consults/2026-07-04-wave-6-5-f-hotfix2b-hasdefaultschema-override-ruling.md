# Architect Ruling — Wave 6.5.f.5-hotfix2b HasDefaultSchema Override (Fourth Consult, 2026-07-04)

**Date**: 2026-07-04
**Participants**: Founder (Niroshana, executing agent), System Architect (Claude Opus 4.7 persona)
**Status**: BINDING — supersedes Rule 5i.1 mechanic (retracts `(string?)null` guidance, retains the intent)
**Related**: `docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2-snapshot-drift-ruling.md` (Third Consult — Rule 5i.1 origin); `docs/architect-consults/2026-07-04-wave-6-5-f-hotfix-followup-ruling.md` (Rule 5h halt-and-consult)

---

## 1. Diagnosis — you diagnosed correctly

`HasDefaultSchema("<X>")` is a model-level annotation applied at `ModelFinalizingConvention` time; it walks every entity whose `Schema` annotation is null and stamps `"<X>"` onto it. Both `.ToTable("<n>", (string?)null)` and `builder.Metadata.SetSchema(null)` inside `Configure()` set the annotation to null, which is exactly the state the convention overwrites. There is no per-entity `HasNoDefaultSchema()` primitive. That is a real EF Core 8 limitation.

Attempts 1, 2, 3 are all correct diagnostic probes of that limitation. Not stuck on a shallow bug — hit a genuine framework constraint.

---

## 2. Ruling on R1: Option G — remove `HasDefaultSchema("events")` from LankaEventsDbContext

**Why G is right, not just least-bad**:

1. **AppDbContext already proves the pattern.** `LankaConnect.Infrastructure.Data.AppDbContext.ConfigureSchemas` — NO `HasDefaultSchema`, instead per-entity `.ToTable("<name>", "<schema>")` for anything cross-schema, and public-schema entities fall through with schema=null. This is the codebase's own successful demonstration.

2. **Rule 5i's sweep (hotfix2) already did 95% of the work.** 22 of 22 non-exception configs now carry explicit two-arg `.ToTable("<name>", "<schema>")`. Removing `HasDefaultSchema` requires no additional work on those 22.

3. **The three exception configs become CORRECT with single-arg `.ToTable("<name>")` — no `SetSchema(null)` gymnastics needed.** Once `HasDefaultSchema` is gone, single-arg ToTable resolves to `null` schema in both DbContexts, matching physical Postgres, matching AppDbContextModelSnapshot ground truth.

4. **The parity-test intent (Third Consult §6) is preserved.** Cross-DbContext parity and snapshot parity both pass.

---

## 3. Ruling on R2: Migration risk, AppDbContext side, sub-slice

### 3.1 Operational tables — YES, need explicit `.ToTable("<name>", "events")`

Shared BuildingBlocks configs (`OutboxMessageConfiguration`, `DeadLetterMessageConfiguration`, `IdempotencyKeyConfiguration`) use single-arg `.ToTable("outbox")` etc., relying on the OWNING DbContext's `HasDefaultSchema`. If we remove `HasDefaultSchema` and the configs stay single-arg, they resolve to `null` and drift from physical `events.outbox`. That's a runtime break on the outbox producer.

**Cannot edit shared configs** — they're reused across modules (Forms/Members/etc. have their own `HasDefaultSchema`).

**Solution: thin adapter in LankaEventsDbContext.OnModelCreating**. After the three `ApplyConfiguration` calls, add explicit schema overrides on the entity types:

```csharp
modelBuilder.Entity<OutboxMessage>().ToTable("outbox", "events");
modelBuilder.Entity<DeadLetterMessage>().ToTable("outbox_dead_letter", "events");
modelBuilder.Entity<IdempotencyKey>().ToTable("idempotency_keys", "events");
```

Idiomatic (mirrors AppDbContext.ConfigureSchemas' inline pattern). Keeps shared configs shared.

### 3.2 AppDbContext side — NO changes required

AppDbContext already has no `HasDefaultSchema`. Zero-op.

### 3.3 Config file changes (final set for hotfix2b)

Revert these three back to single-arg AND drop the `SetSchema(null)` line:

- `TicketTierConfiguration.cs`: `builder.ToTable("ticket_tiers");`
- `TicketScanLogConfiguration.cs`: `builder.ToTable("TicketScanLogs");`
- `EventEmailGroupLinkConfiguration.cs`: `builder.ToTable("event_email_groups");`

Update Rule 5i.1 comments to reference this ruling (retracts `(string?)null` mechanic).

### 3.4 LankaEventsDbContext changes

- **Delete** `modelBuilder.HasDefaultSchema(SchemaName);`
- **Keep** `public const string SchemaName = "events";` — still useful for tests, DI, connection-string builders, and the three new operational-table stanzas.
- **Add** three explicit ToTable overrides for OutboxMessage / DeadLetterMessage / IdempotencyKey after the `ApplyConfiguration` calls.
- Update XML doc.

### 3.5 Sub-slice — ships as `hotfix2b` (NOT hotfix2b-alt)

Third Consult sequencing unchanged. This ruling revises hotfix2b's MECHANIC only. Working tree reset from the "3-config Rule 5i.1 mess" recommended.

### 3.6 Migration operational risk

**Zero physical migration.** No table moves. Model-shape rebaseline only. Snapshot Empty-Up per Third Consult §2.

---

## 4. Ruling on R3: Rule 5i.1 — RETRACT and REPLACE

**Rule 5i.1 (Third Consult)**: RETRACTED. The mechanic `.ToTable("<name>", (string?)null)` does not survive `HasDefaultSchema` finalization in EF Core 8.

**Rule 5i.1 (revised, this consult)**: A DbContext that owns entities living in the CONNECTION-DEFAULT schema (`public` in Postgres) MUST NOT declare `HasDefaultSchema("<X>")`. Cross-schema entities MUST use two-arg `.ToTable("<name>", "<schema>")` in their configs. The AppDbContext.ConfigureSchemas pattern (per-entity two-arg ToTable, no default) is the codebase convention. If a shared-config entity (e.g., outbox) needs a per-module schema pin, the OWNING DbContext applies an explicit `modelBuilder.Entity<T>().ToTable("<name>", "<schema>")` override immediately AFTER `ApplyConfiguration(new SharedConfig())`.

**Rule 5i (unchanged)**: still holds.
**Rule 5e.2 (Third Consult, unchanged)**: still holds — the parity tests CAUGHT this correctly.

---

## 5. Ruling on R4: Consult timing was appropriate

Textbook Rule 5h — 25 minutes with three clean diagnostic data points. No rule change.

---

## 6. Rule 5b clarification (implicit meta-note)

**Rule 5b scope clarification**: "model-shape decisions" means (a) `HasDefaultSchema` add/remove, (b) `ApplyConfigurationsFromAssembly` add/remove, (c) `Ignore<T>` add/remove, (d) `IEntityTypeConfiguration.Configure` changing `ToTable` schema. Does NOT mean: renaming a column (`HasColumnName`), tightening a constraint (`HasMaxLength`), adding a non-schema-changing index. Those are Rule 5j (drift-migration) territory, not Rule 5b (architect consult) territory.

---

## 7. Acceptance criteria (hotfix2b, revised)

1. `LankaEventsDbContext.cs`: `HasDefaultSchema` REMOVED. Three explicit ToTable stanzas ADDED. `SchemaName` const KEPT.
2. Three exception configs: single-arg `.ToTable("<name>")`. `SetSchema(null)` calls REMOVED. Rule 5i.1 comments updated.
3. Companion snapshot-parity test: GREEN.
4. Cross-DbContext parity test: GREEN.
5. `has-pending-model-changes --context LankaEventsDbContext`: `Changes have been made` (empty-Up rebaseline still pending).
6. `has-pending-model-changes --context AppDbContext`: 3 RenameTable operations disappear from scratch.
7. Build clean.
8. Full suite 405+/405+.
9. ArchTest 53/4/0.
10. Empty-Up LankaEvents rebaseline migration ships.
11. Staging `Run-Wave9.ps1` Events failures ≤ 2.

---

## 8. What NOT to do

- Do NOT keep `HasDefaultSchema("events")` and try to override per-entity.
- Do NOT hard-code `"events"` into shared BuildingBlocks outbox/idempotency configs.
- Do NOT commit hotfix2b-alt as a new sub-slice.
- Do NOT delete `LankaEventsDbContext.SchemaName`.
- Do NOT run the AppDbContext scratch until hotfix2b's config edits + snapshot rebaseline are applied.
- Do NOT re-open Rule 5i.1's `(string?)null` mechanic.

---

## 9. Codified rule updates

- **Rule 5i.1 (retracted, replaced)**: see §4.
- **Rule 5b clarification**: model-shape decisions are (a) `HasDefaultSchema`, (b) `ApplyConfigurationsFromAssembly` set, (c) `Ignore<T>` set, (d) schema in `ToTable`. Non-schema property tuning is Rule 5j.
- **Rule 5h ratification**: textbook.
- **New Rule 5i.2**: Shared IEntityTypeConfiguration classes (BuildingBlocks-owned, applied by multiple DbContexts) MUST use single-arg `ToTable`. The owning per-module DbContext MUST pin the schema explicitly with `modelBuilder.Entity<T>().ToTable(name, moduleSchema)` immediately after `ApplyConfiguration`.

---

## 10. Ruling summary

Option G: remove `HasDefaultSchema("events")` from LankaEventsDbContext. Revert three exception configs to single-arg. Add three explicit `modelBuilder.Entity<T>().ToTable(name, "events")` stanzas for outbox/deadletter/idempotency. Rule 5i.1 old form retracted; new form codified. Ship as hotfix2b; working tree reset recommended.
