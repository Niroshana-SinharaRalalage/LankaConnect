using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave4.9.2.10b Phase 1.10b (2026-06-09) — events schema signups +
    /// seats + venue subset (10 tables, 20 columns):
    /// sign_up_lists, sign_up_items, sign_up_commitments, seats,
    /// seat_holds, seat_reservations, venue_layouts, venue_zones,
    /// venue_tables, venue_decorations.
    /// </summary>
    public partial class Phase1_10b_AddCreatedByUpdatedByToEventsSignupsSeatsVenue : Migration
    {
        private static readonly string[] Tables = new[]
        {
            "sign_up_lists",
            "sign_up_items",
            "sign_up_commitments",
            "seats",
            "seat_holds",
            "seat_reservations",
            "venue_layouts",
            "venue_zones",
            "venue_tables",
            "venue_decorations",
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
