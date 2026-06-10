using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Media.Infrastructure.Migrations
{
    /// <summary>
    /// SCHEMA-DESTRUCTIVE-APPROVED: cross-schema rename of legacy aggregate tables
    /// events.photo_albums → media.photo_albums and events.album_photos → media.album_photos
    /// per W4.2 architect ruling (2026-06-06) deferred to Wave 4.9.3 (2026-06-09).
    /// Operation uses ALTER TABLE ... SET SCHEMA which is non-destructive at the row
    /// level (catalog-only, FK-preserving, index-preserving, sub-100ms apply) but is
    /// destructive at the namespace level — application code, snapshots, and any
    /// staging query references to events.photo_albums break atomically at apply time.
    /// FK probe (verified by architect via cross-migration source grep): zero external
    /// inbound FKs to either table. Internal FK album_photos → photo_albums is OID-
    /// tracked and survives SET SCHEMA without modification.
    /// Rollback: forward SET SCHEMA events via Down().
    /// </summary>
    public partial class Wave4_9_3_RenameMediaTablesToMediaSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "media");
            migrationBuilder.Sql("ALTER TABLE events.photo_albums SET SCHEMA media;");
            migrationBuilder.Sql("ALTER TABLE events.album_photos SET SCHEMA media;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE media.album_photos SET SCHEMA events;");
            migrationBuilder.Sql("ALTER TABLE media.photo_albums SET SCHEMA events;");
        }
    }
}
