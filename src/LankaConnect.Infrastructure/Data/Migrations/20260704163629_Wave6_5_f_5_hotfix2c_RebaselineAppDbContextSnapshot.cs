using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave 6.5.f.5-hotfix2c AppDbContext snapshot rebaseline migration. Empty-Up/Down
    /// per [[feedback-empty-up-snapshot-rebaseline]] and the precedent
    /// <c>20260610142658_Wave4_9_5_RebaselineAppDbContextSnapshotPostMediaFormsExtraction</c>.
    ///
    /// Purpose: regenerate <c>AppDbContextModelSnapshot.cs</c> to match the runtime model
    /// after hotfix2c's Part A (explicit EventBadge → Badge FK override restoring Restrict
    /// cascade behavior) took effect. Ships in the SAME COMMIT as Part A per fifth
    /// architect consult ruling §4 — splitting them creates a window where snapshot lies
    /// about runtime (Part B alone) or snapshot lies about physical (Part A alone).
    ///
    /// Why Up()/Down() are empty:
    ///   The scratch migration `dotnet ef migrations add` proposed had 28 operations:
    ///   2 DropCheckConstraint + 2 AddCheckConstraint (line-ending drift on
    ///   ck_registrations_valid_format — CRLF/LF cosmetic noise; Postgres tokenizes
    ///   whitespace during check-constraint compilation, so no physical behavior change)
    ///   and 24 UpdateData (seed-timestamp DateTime.UtcNow drift on
    ///   reference_data.reference_values.created_at — predicted noise per the third
    ///   architect consult §5). Zero FK operations remain post-Part-A (proving the
    ///   FK cascade fix took effect).
    ///
    ///   None of these operations should ever execute against physical Postgres:
    ///   - Physical `events.registrations` already has the equivalent check constraint;
    ///     dropping and re-adding with a whitespace-identical SQL body is churn.
    ///   - Physical `reference_data.reference_values` rows have real historical
    ///     created_at timestamps; updating them to a snapshot-generation-time value
    ///     would corrupt audit history.
    ///
    ///   Ship this migration with empty Up()/Down() bodies — advances the
    ///   __EFMigrationsHistory row so EF Core stops reporting "changes have been made
    ///   to the model," and the auto-regenerated <c>.Designer.cs</c> +
    ///   <c>AppDbContextModelSnapshot.cs</c> capture the corrected model (including
    ///   the restored Restrict FK) as the new snapshot baseline. Zero DDL runs. Physical
    ///   Postgres is unchanged. FK behavior on badges.event_badges stays Restrict.
    ///
    /// Ruling: docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2c-appdbcontext-drift-ruling.md
    /// </summary>
    public partial class Wave6_5_f_5_hotfix2c_RebaselineAppDbContextSnapshot : Migration
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
