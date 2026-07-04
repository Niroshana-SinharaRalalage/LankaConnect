# Architect Ruling — Wave 6.5.f.5-hotfix2 Snapshot Drift (Third Consult, 2026-07-04)

**Date**: 2026-07-04
**Participants**: Founder (Niroshana, executing agent), System Architect (Claude Opus 4.7 persona)
**Status**: BINDING — supersedes Rule 5b's default disposition on snapshot rebaselines for this specific commit only
**Related**: `docs/architect-consults/2026-07-04-wave-6-5-f-hotfix-followup-ruling.md` (Option E ruling + §4.4 halt-and-consult rule); memory `[[feedback-empty-up-snapshot-rebaseline]]`; precedent migrations `Wave4_9_5_RebaselineAppDbContextSnapshotPostMediaFormsExtraction.cs`, `RebaselineEventEmailGroupJunction.cs`, `RebaselineNewsletterEmailGroupJunction.cs`

---

## 1. Diagnosis — Hypothesis A is CORRECT, but Hypothesis B is ALSO CORRECT for three specific tables

Hypothesis A is correct in the general case AND correctly identifies why the 14 RenameTable operations in the LankaEvents scratch migration would fail against physical Postgres. However, in the process of running down the drift signal I discovered a THIRD-order defect that neither the follow-up ruling nor hotfix2 caught: three tables in the moved configs map to a schema that does NOT match physical Postgres.

### 1.1 Physical Postgres ground truth (per migration files, cross-checked to `AppDbContextModelSnapshot.cs`)

| Table | Physical schema (per creation migration) | AppDbContext snapshot | LankaEventsDbContext snapshot (Wave 6.5.e recorded) | Current moved config (post-hotfix2) |
|---|---|---|---|---|
| `events` | `events` | `events` | `Events` (WRONG) | `events` (CORRECT) |
| `tickets` | `events` | `events` | `Tickets` (WRONG) | `events` (CORRECT) |
| `sign_up_lists` | `events` | `events` | `SignUpLists` (WRONG) | `events` (CORRECT) |
| `sign_up_items` | `events` | `events` | `SignUpItems` (WRONG) | `events` (CORRECT) |
| `sign_up_commitments` | `events` | `events` | `SignUpCommitments` (WRONG) | `events` (CORRECT) |
| `event_analytics` | `analytics` | `analytics` | `events` (WRONG schema) | `analytics` (CORRECT) |
| `event_view_records` | `analytics` | `analytics` | `events` (WRONG schema) | `analytics` (CORRECT) |
| `event_badges` | `badges` | `badges` | (Ignored in 6.5.e) | `badges` (CORRECT) |
| `event_email_groups` | **`public` (default)** | **`(string)null`** | (not present) | **`events` (WRONG — new divergence)** |
| `ticket_tiers` | **`public` (default)** | **`(string)null`** | **`events` (recorded WRONG in 6.5.e)** | **`events` (still WRONG)** |
| `TicketScanLogs` | **`public` (default)** | **`(string)null`** | **`events` (recorded WRONG in 6.5.e)** | **`events` (still WRONG)** |

The rows in **bold** are the newly identified Hypothesis B divergences.

### 1.2 Why the parity test is passing GREEN despite the three physical divergences

The test hard-codes expected `(schema, tableName)` values in the `SharedEntities()` MemberData. The founder transcribed values based on beliefs about physical schema, not what the snapshot actually recorded. `AppDbContextModelSnapshot.cs` at lines 3487, 4620, 4706 shows the actual authority: `(string)null` schema for all three. The test is divergence-blind by construction.

---

## 2. Ruling on R1: Hypothesis A is CORRECT — proceed with empty-Up rebaseline

Ship a single empty-`Up()`/empty-`Down()` migration on `LankaEventsDbContext` that regenerates `LankaEventsDbContextModelSnapshot.cs`. Do NOT execute any of the 148 operations from the scratch migration.

**Migration name**: `Wave6_5_f_5_hotfix2_RebaselineLankaEventsSnapshot`

**Deployment path**:
1. `dotnet ef migrations add Wave6_5_f_5_hotfix2_RebaselineLankaEventsSnapshot --context LankaEventsDbContext -o Migrations`
2. HAND-EMPTY the auto-generated Up()/Down() bodies. Keep the class-doc block.
3. Verify compiles: `dotnet build LankaConnect.sln -c Release`.
4. Re-run `has-pending-model-changes` → expected `No pending model changes.`

Do NOT hand-edit the `.Designer.cs`.

---

## 3. Ruling on R2: Hypothesis B is ALSO CORRECT — three physical-schema divergences

**Rule 5i.1 (new)**: When a config's entity's PHYSICAL table lives in the default connection schema AND the config is being swept into a DbContext with `HasDefaultSchema("<Y>")`, the two-arg `.ToTable(...)` form MUST use `(string?)null` as the schema argument.

**Corrected fix scope**:
1. `TicketTierConfiguration.cs`: `builder.ToTable("ticket_tiers", (string?)null);`
2. `TicketScanLogConfiguration.cs`: `builder.ToTable("TicketScanLogs", (string?)null);`
3. `EventEmailGroupLinkConfiguration.cs`: `builder.ToTable("event_email_groups", (string?)null);`

**Parity test fix (same commit)**: Update `LankaEventsDbContextTableParityTests.cs` MemberData with `null` schema for TicketTier + EventEmailGroupLink. ADD TicketScanLog. Update method signature to accept `string? expectedSchema`.

**TDD discipline**: authored FIRST as failing red, THEN config edits go green, THEN R1 rebaseline captures the corrected snapshot.

---

## 4. Ruling on R3: Three-commit sequencing

1. **Commit N (already exists)** `Wave 6.5.f.5-hotfix1: un-Ignore EventEmailGroupLink + EventBadge` — stays as-is. Not amended.
2. **Commit N+1 (in progress locally)** `Wave 6.5.f.5-hotfix2: embed physical (schema, table) in Products.LankaEvents configs + extend parity tests` — the config-relocation + Rule 5i sweep + Option E fixes + 30 parity-test cases.
3. **Commit N+2** `Wave 6.5.f.5-hotfix2b: Rule 5i.1 (public-schema physical divergence for ticket_tiers/TicketScanLogs/event_email_groups) + LankaEvents snapshot rebaseline`.
4. **Commit N+3** `Wave 6.5.f.5-hotfix2c: AppDbContext snapshot rebaseline` (IF the AppDbContext scratch drift is non-empty after N+2 lands).

Do NOT amend N+1. Two-commit narrative preserves audit trail.

---

## 5. Ruling on R4: AppDbContext drift — YES, exercise the scratch migration

Do NOT skip AppDbContext's drift. Same reasoning that produced §4.4 for LankaEventsDbContext applies identically.

**Sequence**: run the AppDbContext scratch AFTER commit N+2 lands (R2 config fixes). Categorize + attach in commit message per Rule 5j. If zero remaining operations, N+3 skipped.

**Predicted shape**: 3 RenameTable operations for `TicketScanLogs`, `ticket_tiers`, `event_email_groups` (schemas would fail against physical). After R2 fix, these DISAPPEAR. Possibly some UpdateData noise for reference_values seed timestamps.

If AppDbContext post-R2 scratch shows anything OTHER than the three predicted RenameTables + seed noise, HALT and re-consult with raw scratch diff attached.

---

## 6. Ruling on R5: Parity test evidence is INSUFFICIENT — three tightenings

**Rule 5e.2 (new)**: parity tests derive expected `(schema, tableName)` values from `AppDbContext.Model.FindEntityType(T)` via an InMemory AppDbContext instance, NOT from hand-transcribed constants.

**Refined test pattern**:
```csharp
[Theory]
[MemberData(nameof(SharedEntities))]  // Type-only list
public void SharedEntity_HasIdenticalSchemaAndTableName_AcrossBothDbContexts(Type entityType)
{
    using var appCtx = CreateAppDbContext();
    using var eventsCtx = CreateLankaEventsContext();

    var appMap = appCtx.Model.FindEntityType(entityType);
    var eventsMap = eventsCtx.Model.FindEntityType(entityType);

    appMap.Should().NotBeNull();
    eventsMap.Should().NotBeNull();

    eventsMap!.GetSchema().Should().Be(appMap!.GetSchema());
    eventsMap!.GetTableName().Should().Be(appMap!.GetTableName());
}
```

**Companion snapshot-parity test**: assert `AppDbContext.Model.FindEntityType(T).GetSchema()` against a curated list of expected values sourced from CREATION migrations. Only 12 entities. Catches "both DbContexts agree on a wrong value."

Delete the old hand-transcribed `SharedEntity_HasCorrectSchemaAndTableName_InLankaEventsDbContext` — divergence-blind by construction.

No staging-DB INFORMATION_SCHEMA required — three defense-in-depth layers (per-AppDbContext + curated snapshot + `has-pending-model-changes`) are sufficient.

---

## 7. Codified rule additions

- **Rule 5i.1** (extension of Rule 5i): physical-default-schema tables use `.ToTable("<name>", (string?)null)`.
- **Rule 5e.2** (extension of Rule 5e): parity tests derive expected values from `AppDbContext.Model`, not hand-transcribed constants.
- **Rule 5b clarification**: empty-Up snapshot rebaselines require architect consult. This ruling constitutes the consult for hotfix2b + hotfix2c. Subsequent rebaselines require fresh consult.
- **Rule 5h ratification**: 10 minutes on diagnosis before opening this consult — within 30-min soft cap.

---

## 8. Acceptance criteria (revised)

1. N+2 RED-first evidence for the 3 R2 config edits.
2. N+2 refined parity pattern per §6.1 + snapshot-parity companion per §6.2. Old hand-transcribed test deleted.
3. N+2 LankaEvents drift: `has-pending-model-changes` returns `Changes have been made` (rebaseline not yet applied — expected).
4. N+2 AppDbContext scratch categorized + attached in commit message per Rule 5j.
5. Build clean.
6. Full suite 405+/405+.
7. ArchTest 53/4/0.
8. N+3 (LankaEvents rebaseline): empty-Up migration ships. `has-pending-model-changes --context LankaEventsDbContext` returns `No pending model changes.`
9. N+4 (AppDbContext rebaseline, IF non-empty): same pattern.
10. Staging `Run-Wave9.ps1` returns Events failures ≤ 2. If HIGHER than 94 pre-hotfix2 result, halt + re-consult.

---

## 9. What NOT to do

- Do NOT execute the 148-line LankaEvents scratch migration against Postgres.
- Do NOT amend hotfix2 to fold in R2 fixes.
- Do NOT skip AppDbContext scratch exercise.
- Do NOT hand-edit `LankaEventsDbContextModelSnapshot.cs` directly.
- Do NOT extend Rule 5i.1 to any other config without first checking AppDbContext snapshot per §5.

---

## 10. Ruling summary

Hypothesis A correct AND Hypothesis B correct for 3 tables. Fix 3 configs to use `(string?)null` per Rule 5i.1 in commit N+2 (hotfix2b), with parity test refined per Rule 5e.2 (RED-first). Ship empty-Up LankaEvents snapshot rebaseline in same commit. Run AppDbContext scratch per §5 and ship empty-Up AppDbContext rebaseline as commit N+3 (hotfix2c) IF non-empty.

Time-since-last-broken-staging is generous — dev-validation only. Take the time to do this in three atomic commits.
