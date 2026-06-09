using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave4.9.2.10a Phase 1.10a (2026-06-09) — Event aggregate proper.
    /// Adds physical <c>created_by</c> and <c>updated_by</c> columns on
    /// 10 events-schema tables: events, registrations, sponsors,
    /// sponsorship_packages, event_organizer_contacts, event_slug_aliases,
    /// event_templates, EventImages, EventVideos, metro_areas.
    /// 20 columns total.
    ///
    /// Note: EventImages / EventVideos are PascalCase legacy table names
    /// (Epic 2 Phase 2 imports). All others use snake_case.
    /// </summary>
    public partial class Phase1_10a_AddCreatedByUpdatedByToEventsCore : Migration
    {
        private static readonly string[] SnakeTables = new[]
        {
            "events",
            "registrations",
            "sponsors",
            "sponsorship_packages",
            "event_organizer_contacts",
            "event_slug_aliases",
            "event_templates",
            "metro_areas",
        };

        private static readonly string[] PascalTables = new[]
        {
            "EventImages",
            "EventVideos",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in SnakeTables)
            {
                migrationBuilder.AddColumn<string>(name: "created_by", schema: "events", table: table, type: "text", nullable: true);
                migrationBuilder.AddColumn<string>(name: "updated_by", schema: "events", table: table, type: "text", nullable: true);
            }
            foreach (var table in PascalTables)
            {
                migrationBuilder.AddColumn<string>(name: "created_by", schema: "events", table: table, type: "text", nullable: true);
                migrationBuilder.AddColumn<string>(name: "updated_by", schema: "events", table: table, type: "text", nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in SnakeTables)
            {
                migrationBuilder.DropColumn(name: "created_by", schema: "events", table: table);
                migrationBuilder.DropColumn(name: "updated_by", schema: "events", table: table);
            }
            foreach (var table in PascalTables)
            {
                migrationBuilder.DropColumn(name: "created_by", schema: "events", table: table);
                migrationBuilder.DropColumn(name: "updated_by", schema: "events", table: table);
            }
        }
    }
}
