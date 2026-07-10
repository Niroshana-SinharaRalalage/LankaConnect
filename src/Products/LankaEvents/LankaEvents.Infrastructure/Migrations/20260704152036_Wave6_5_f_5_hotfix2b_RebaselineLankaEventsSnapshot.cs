using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LankaConnect.Products.LankaEvents.Infrastructure.Migrations {
    /// <summary>
    /// Wave 6.5.f.5-hotfix2b snapshot rebaseline migration. Empty-Up/Down per
    /// [[feedback-empty-up-snapshot-rebaseline]] and the precedent
    /// <c>20260610142658_Wave4_9_5_RebaselineAppDbContextSnapshotPostMediaFormsExtraction</c>.
    ///
    /// Purpose: regenerate <c>LankaEventsDbContextModelSnapshot.cs</c> to match the
    /// corrected runtime model produced by hotfix2 + hotfix2b. The Wave 6.5.e snapshot
    /// recorded a BROKEN model shape (PascalCase table names like "Events", "Tickets",
    /// "SignUpLists"; cross-schema entities wrongly in "events" schema; junction tables
    /// wrongly in "events" instead of "public"). hotfix2 (commit f25003a1) fixed the
    /// runtime model via Rule 5i explicit two-arg ToTable in each swept EF config, and
    /// hotfix2b (this commit) removed the `HasDefaultSchema("events")` model-level
    /// annotation and let the three exception configs (TicketTier, TicketScanLog,
    /// EventEmailGroupLink) fall through to `null` schema (physical `public`).
    ///
    /// Why Up()/Down() are empty:
    ///   The scratch migration `dotnet ef migrations add` proposed had 148 operations:
    ///   14 RenameTable, 46 FK drops/re-adds, 10 PK drops/re-adds, 2 CreateTable
    ///   (event_badges → badges, event_email_groups → public). EVERY ONE of those
    ///   operations would FAIL against physical Postgres because the physical schema
    ///   has ALWAYS been correct — the recorded snapshot from Wave 6.5.e was the wrong
    ///   thing. Physical `events.events` already exists (RenameTable "Events" →
    ///   "events" would fail: source doesn't exist). Physical `badges.event_badges`
    ///   already exists (CreateTable would fail: already exists). Physical
    ///   `public.event_email_groups` already exists (CreateTable would fail).
    ///
    ///   So this migration ships an EMPTY Up()/Down() body — it advances the
    ///   __EFMigrationsHistory row so EF Core stops complaining "changes have been
    ///   made to the model since the last migration," and the auto-regenerated
    ///   <c>.Designer.cs</c> + <c>LankaEventsDbContextModelSnapshot.cs</c> capture
    ///   the CORRECTED model as the new snapshot baseline. Zero DDL runs. Physical
    ///   Postgres is unchanged.
    ///
    /// Ruling: docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2-snapshot-drift-ruling.md
    ///         docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2b-hasdefaultschema-override-ruling.md
    /// </summary>
    public partial class Wave6_5_f_5_hotfix2b_RebaselineLankaEventsSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Empty by design — see class doc. Snapshot regeneration only.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Empty by design — see class doc. Snapshot regeneration only.
        }
    }
}
