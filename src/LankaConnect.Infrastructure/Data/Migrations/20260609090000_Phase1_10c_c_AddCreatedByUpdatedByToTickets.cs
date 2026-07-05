using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave4.9.2.10c.c Phase 1.10c.c (2026-06-09) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on
    /// <c>events.tickets</c>. AppDbContext-managed (TicketScanLog is
    /// not IAuditable so it's excluded from this rollout).
    /// </summary>
    public partial class Phase1_10c_c_AddCreatedByUpdatedByToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "created_by", schema: "events", table: "tickets", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "updated_by", schema: "events", table: "tickets", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "created_by", schema: "events", table: "tickets");
            migrationBuilder.DropColumn(name: "updated_by", schema: "events", table: "tickets");
        }
    }
}
