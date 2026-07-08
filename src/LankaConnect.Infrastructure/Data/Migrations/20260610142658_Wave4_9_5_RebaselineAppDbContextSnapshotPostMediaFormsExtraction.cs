using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// SCHEMA-DESTRUCTIVE-APPROVED: Wave 4.9.5 snapshot rebaseline only.
    ///
    /// This migration is INTENTIONALLY EMPTY at the SQL level. Its sole purpose
    /// is to regenerate <c>AppDbContextModelSnapshot.cs</c> so it no longer
    /// contains the 7 stale entity blocks that were physically migrated out of
    /// AppDbContext's ownership in earlier waves:
    ///   - LankaConnect.Modules.Media.Domain.PhotoAlbum -> MediaDbContext
    ///     (W4.2 extraction 2026-06-06), table renamed
    ///     events.photo_albums -> media.photo_albums by Wave 4.9.3 (e502ef41).
    ///   - LankaConnect.Modules.Media.Domain.Entities.AlbumPhoto -> same as
    ///     PhotoAlbum (events.album_photos -> media.album_photos).
    ///   - LankaConnect.Modules.Forms.Domain.EventForm + FormQuestion +
    ///     FormResponse + FormAnswer -> FormsDbContext (W4.3 extraction
    ///     2026-06-06), tables renamed events.event_forms -> forms.event_forms
    ///     etc. by Wave 4.9.4 (8a293082).
    ///   - LankaConnect.Domain.Notifications.Notification -> NotificationsDbContext
    ///     (W3.5 extraction 2026-06-05). The legacy
    ///     LankaConnect.Domain.Notifications namespace was deleted; the
    ///     snapshot retained a stale type-name reference.
    ///
    /// EF Core's diff engine generated DropTable + CreateTable operations for
    /// those 7 tables because they were in the old snapshot but not in
    /// AppDbContext's current model. The DropTable operations have been removed
    /// from Up()/Down() because the underlying tables are now owned by Media /
    /// Forms / Notifications module DbContexts -- dropping them here would
    /// destroy live data on staging. The CreateTable operations would put the
    /// tables back in their pre-Wave-4.9.3/4.9.4 locations (events.*), undoing
    /// the schema renames that landed in commits e502ef41 + 8a293082.
    ///
    /// Per CLAUDE.md Section 5: "Hand-editing the JUST-generated migration to
    /// remove unintended sections IS allowed -- that's the human-review step
    /// replacing local dry-run. The anti-pattern is hand-CREATING a .cs file
    /// without an accompanying .Designer.cs; surgical edits to EF-generated
    /// files are normal." This migration is the canonical example of that
    /// human-review beat: EF wrote the destructive SQL, we accept the snapshot
    /// regeneration (the actual heal), and we delete the SQL.
    ///
    /// Idempotent SQL after this migration: a single
    /// INSERT INTO __EFMigrationsHistory row. No DDL.
    /// </summary>
    public partial class Wave4_9_5_RebaselineAppDbContextSnapshotPostMediaFormsExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty -- see class-level comment.
            // Snapshot regeneration is the sole net effect of this migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty -- these tables are not AppDbContext's to recreate.
        }
    }
}
