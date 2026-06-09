using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave4.9.2.5 Phase 1.5 (2026-06-09) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on community
    /// schema: <c>community.topics</c> (ForumTopic) and
    /// <c>community.replies</c> (Reply). 4 nullable text columns total.
    /// </summary>
    public partial class Phase1_5_AddCreatedByUpdatedByToCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "topics", "replies" })
            {
                migrationBuilder.AddColumn<string>(name: "created_by", schema: "community", table: table, type: "text", nullable: true);
                migrationBuilder.AddColumn<string>(name: "updated_by", schema: "community", table: table, type: "text", nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "topics", "replies" })
            {
                migrationBuilder.DropColumn(name: "created_by", schema: "community", table: table);
                migrationBuilder.DropColumn(name: "updated_by", schema: "community", table: table);
            }
        }
    }
}
