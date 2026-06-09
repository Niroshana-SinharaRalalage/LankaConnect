using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave4.9.2.3 Phase 1.3 (2026-06-08) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on the badges
    /// schema group:
    /// <list type="bullet">
    ///   <item><c>badges.badges</c> (the Badge aggregate)</item>
    ///   <item><c>badges.event_badges</c> (the EventBadge aggregate)</item>
    /// </list>
    /// Purely additive (4 nullable text columns total).
    ///
    /// Same template as Phase 1.1 / 1.2: hand-authored .cs/.Designer.cs +
    /// surgical 16-line snapshot edit (8 per entity). NO
    /// <c>dotnet ef migrations add</c>.
    /// </summary>
    public partial class Phase1_3_AddCreatedByUpdatedByToBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "created_by",
                schema: "badges",
                table: "badges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                schema: "badges",
                table: "badges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                schema: "badges",
                table: "event_badges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                schema: "badges",
                table: "event_badges",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "created_by", schema: "badges", table: "badges");
            migrationBuilder.DropColumn(name: "updated_by", schema: "badges", table: "badges");
            migrationBuilder.DropColumn(name: "created_by", schema: "badges", table: "event_badges");
            migrationBuilder.DropColumn(name: "updated_by", schema: "badges", table: "event_badges");
        }
    }
}
