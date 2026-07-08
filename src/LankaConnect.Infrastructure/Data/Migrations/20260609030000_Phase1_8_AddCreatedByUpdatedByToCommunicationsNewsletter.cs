using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Wave4.9.2.8 Phase 1.8 (2026-06-09) — adds physical
    /// <c>created_by</c> and <c>updated_by</c> columns on the newsletter
    /// subset of the communications schema (3 tables, 6 columns):
    /// <c>newsletters</c>, <c>newsletter_email_history</c>,
    /// <c>newsletter_subscribers</c>.
    /// </summary>
    public partial class Phase1_8_AddCreatedByUpdatedByToCommunicationsNewsletter : Migration
    {
        private static readonly string[] Tables = new[]
        {
            "newsletters",
            "newsletter_email_history",
            "newsletter_subscribers",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.AddColumn<string>(name: "created_by", schema: "communications", table: table, type: "text", nullable: true);
                migrationBuilder.AddColumn<string>(name: "updated_by", schema: "communications", table: table, type: "text", nullable: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
            {
                migrationBuilder.DropColumn(name: "created_by", schema: "communications", table: table);
                migrationBuilder.DropColumn(name: "updated_by", schema: "communications", table: table);
            }
        }
    }
}
