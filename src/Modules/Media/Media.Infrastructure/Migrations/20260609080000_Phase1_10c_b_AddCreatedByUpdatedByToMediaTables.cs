using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LankaConnect.Modules.Media.Infrastructure.Migrations {
    /// <summary>
    /// Wave4.9.2.10c.b Phase 1.10c.b (2026-06-09) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on the 2 Media
    /// module tables (cross-schema overrides into <c>events</c>):
    /// <c>events.photo_albums</c>, <c>events.album_photos</c>.
    /// 4 columns total. Generated via MediaDbContext.
    /// </summary>
    public partial class Phase1_10c_b_AddCreatedByUpdatedByToMediaTables : Migration
    {
        private static readonly string[] Tables = new[]
        {
            "photo_albums",
            "album_photos",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<string>(name: "created_by", schema: "events", table: table, type: "text", nullable: true);
                migrationBuilder.AddColumn<string>(name: "updated_by", schema: "events", table: table, type: "text", nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.DropColumn(name: "created_by", schema: "events", table: table);
                migrationBuilder.DropColumn(name: "updated_by", schema: "events", table: table);
            }
        }
    }
}
