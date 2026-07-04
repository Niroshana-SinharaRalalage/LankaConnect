# Architect Ruling — Wave 6.5.f.5-hotfix2c AppDbContext Drift (Fifth Consult, 2026-07-04)

**Date**: 2026-07-04
**Participants**: Founder (Niroshana, executing agent), System Architect (Claude Opus 4.7 persona)
**Status**: BINDING — supersedes Wave 6.5.f regression ruling §2.2 mechanic on `HasOne(eb => eb.Badge)` deletion; retains its intent
**Related**: `docs/architect-consults/2026-07-04-wave-6-5-f-regression-ruling.md` §2.2; `docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2-snapshot-drift-ruling.md` §5; `docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2b-hasdefaultschema-override-ruling.md`

---

## 1. Diagnosis

### 1.1 Item #1 (`FK_event_badges_badges_BadgeId` cascade change) — CONFIRMED LOAD-BEARING

Hotfix1 §2.2 deleted the explicit `HasOne(eb => eb.Badge)...OnDelete(Restrict)` block from `EventBadgeConfiguration.cs`. EF Core 8's `RelationshipDiscoveryConvention` still walks `EventBadge`'s public properties, still finds `Badge? Badge`, still sees `Badge` mapped in AppDbContext's model, and infers a FK. Because `builder.Property(eb => eb.BadgeId).IsRequired()` remains, the inferred FK is required — EF's default cascade for required FKs is `Cascade`.

Physical Postgres has `Restrict` since creation migration `20251211184730_AddEmailGroups.cs` line 61. AppDbContext runtime model now says `Cascade`. Real semantic drift. Third-consult §5 halt fired correctly.

### 1.2 Item #2 (`ck_registrations_valid_format` line-ending drift) — cosmetic

Runtime = `\r\n` (CRLF from Windows file), old snapshot = `\n` (LF). Postgres tokenizes whitespace during check-constraint compilation — semantically identical. No behavior change.

### 1.3 Third-order finding — `GetEventBadgesQueryHandler` latent broken caller

`src/LankaConnect.Application/Badges/Queries/GetEventBadges/GetEventBadgesQueryHandler.cs` lines 30-41 dereferences `eb.Badge` after hotfix1 made that navigation unmapped in `LankaEventsDbContext`. `.Where(eb => eb.Badge != null)` filters out every row → endpoint returns empty. Zero test coverage. Silent-empty-response bug (F30a shape). **NOT in hotfix2c scope** — separate hotfix2d.

---

## 2. Ruling on Q2 — Option Q2c

### Rejected: Q2b (config-side FK)
Would force `Badge` into whichever DbContext runs the config. `LankaEventsDbContext.OnModelCreating` `Ignore<Badge>()`s — collision.

### Rejected: Q2a (accept Cascade)
`DeleteBadgeCommandHandler.Handle` has NO pre-check iterating EventBadges. Deleting a Badge under Cascade would silently destroy assignment history platform-wide. Original comment `"Don't cascade delete badges when event badge is deleted"` was authoritative domain intent.

### Adopted: Q2c

Add explicit FK configuration in `AppDbContext.OnModelCreating` AFTER `ApplyConfigurationsFromAssembly`:

```csharp
modelBuilder.Entity<EventBadge>()
    .HasOne(eb => eb.Badge)
    .WithMany()
    .HasForeignKey(eb => eb.BadgeId)
    .OnDelete(DeleteBehavior.Restrict);
```

Rule 5i.2 pattern: shared config generic, owning DbContext pins module-specific behavior. AppDbContext already references Badge + EventBadge — no new cross-module reference.

---

## 3. Ruling on Q3 — line-ending is cosmetic, capture-as-is

Ship hotfix2c capturing current `\r\n`. Do NOT normalize `RegistrationConfiguration.cs` to LF in this hotfix.

Codified deferral: log a Rule 5j follow-up for housekeeping wave — audit all `IEntityTypeConfiguration<T>` files for verbatim SQL string literals with embedded newlines; normalize via `.gitattributes eol=lf` or refactor to single-line concatenation.

---

## 4. Ruling on Q1 — atomic two-part commit

Third-consult §4's "pure empty-Up rebaseline" is now insufficient — the FK drift is a runtime defect, not snapshot-only.

**Hotfix2c ships as ONE commit** with both:

- **Part A**: The Q2c FK stanza in `AppDbContext.OnModelCreating`.
- **Part B**: Empty-Up `Wave6_5_f_5_hotfix2c_RebaselineAppDbContextSnapshot` migration. Hand-empty `Up()` / `Down()`. Regenerated snapshot preserves `OnDelete(Restrict)`.

Splitting them creates a window where either snapshot lies about runtime, or snapshot lies about physical. Both are Rule 5b-class traps.

Commit title: `Wave 6.5.f.5-hotfix2c: restore EventBadge→Badge Restrict FK + AppDbContext snapshot rebaseline`.

### Post-hotfix2c verification

1. Build 0 errors.
2. `has-pending-model-changes --context AppDbContext` → "No pending model changes."
3. `has-pending-model-changes --context LankaEventsDbContext` → "No pending model changes." (sentinel).
4. Infrastructure.Tests ≥ 418.
5. ArchTest 53/4/0.
6. **New scratch verification**: `dotnet ef migrations add ScratchAppDbCtx --context AppDbContext -o <scratchpad>` produces ONLY seed-timestamp `UpdateData`. If ANY FK op appears, Part A didn't take. `git checkout --` scratch afterward.
7. Staging Run-Wave9.ps1 Events ≤ 2.

---

## 5. Ruling on Q4 — Rule 5j extensions

**Rule 5j.2 (new)**: When ANY commit deletes/modifies a `HasOne`/`HasMany`/`WithOne`/`WithMany`/`HasForeignKey` block, the commit MUST:
1. Identify every DbContext mapping the affected entity.
2. For each, determine whether the CLR navigation still has a MAPPED PRINCIPAL. If yes, EF infers convention FK — audit `OnDelete`.
3. Categorize physical DB's current FK behavior (grep creation migration + subsequent alters).
4. If convention-inferred differs from physical, restore via DbContext-level override (Rule 5i.2 pattern).

Record audit in commit body. Retroactive application NOT required (do not rewrite hotfix1's commit).

**Rule 5j.3 (new)**: `HasOne`/`HasMany` deletions require Application-layer caller grep for navigation-property dereferences. Repair or acknowledge each hit. This is what would have caught `GetEventBadgesQueryHandler` at hotfix1 time.

---

## 6. Second-order defect — `GetEventBadgesQueryHandler` — separate hotfix2d

Ships immediately AFTER hotfix2c merges. Scope:
- Hydrate badge DTOs via `IBadgeRepository.GetByIdsAsync(...)` at application layer.
- Remove `.Where(eb => eb.Badge != null)` filter + `eb.Badge!.ToBadgeDto()` dereference.
- Author unit test constructing Event with two EventBadges, mocks both repositories, asserts DTOs contain hydrated Badge data.
- Rule 5c staging-smoke candidate: deploy on own branch, `Run-Wave9.ps1 -Controllers Badges`, verify empty-response failure count returns to 0.

---

## 7. Codified rule additions

- **Rule 5j.2**: per-DbContext convention-FK audit + physical-DB behavior audit on config-block deletion.
- **Rule 5j.3**: Application-layer caller grep on navigation removal.
- **Rule 5i.2 (unchanged)**: shared config generic, owning DbContext pins.
- **Rule 5b clarification**: `modelBuilder.Entity<T>().HasOne/HasMany/OnDelete(...)` added to `OnModelCreating` for physical-DB-matching restoration is IN-scope for Rule 5b consult. This ruling constitutes that consult.
- **Rule 5h ratification**: 5-min categorization + immediate consult per §5 halt trigger. Textbook.

---

## 8. Acceptance criteria (hotfix2c)

Per §4.4 above, plus commit body includes raw pre-fix scratch categorization + explicit Rule 5j.2 audit result.

---

## 9. What NOT to do

- NOT pure empty-Up rebaseline (freezes Cascade into snapshot).
- NOT physical migration to change FK cascade.
- NOT put FK stanza in `EventBadgeConfiguration.cs` (Q2b — collision).
- NOT split hotfix2c into two commits.
- NOT touch `RegistrationConfiguration.cs` line endings.
- NOT fix `GetEventBadgesQueryHandler` in hotfix2c.
- NOT amend hotfix1 or prior commits.
- NOT retroactively add Rule 5j.2 audit to hotfix1 body.

---

## 10. Ruling summary

Ship hotfix2c as ATOMIC commit combining (Part A) explicit `modelBuilder.Entity<EventBadge>().HasOne(eb => eb.Badge).WithMany().HasForeignKey(...).OnDelete(Restrict)` in `AppDbContext.OnModelCreating` restoring `Restrict` via Rule 5i.2, plus (Part B) empty-Up `AppDbContext` snapshot rebaseline capturing corrected runtime model. Closes FK drift without physical DDL. Codify Rules 5j.2 + 5j.3. `GetEventBadgesQueryHandler` broken-caller bug is hotfix2d immediately after.
