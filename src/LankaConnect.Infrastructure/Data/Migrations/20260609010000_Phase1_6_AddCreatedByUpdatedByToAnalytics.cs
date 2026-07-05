using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave4.9.2.6 Phase 1.6 (2026-06-09) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on
    /// <c>analytics.event_analytics</c>. Only IAuditable table in the
    /// analytics schema; <c>analytics.event_view_records</c> is a plain
    /// log/projection class with no IAuditable interface so it's
    /// intentionally excluded.
    /// </summary>
    public partial class Phase1_6_AddCreatedByUpdatedByToAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "created_by", schema: "analytics", table: "event_analytics", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "updated_by", schema: "analytics", table: "event_analytics", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "created_by", schema: "analytics", table: "event_analytics");
            migrationBuilder.DropColumn(name: "updated_by", schema: "analytics", table: "event_analytics");
        }
    }
}
