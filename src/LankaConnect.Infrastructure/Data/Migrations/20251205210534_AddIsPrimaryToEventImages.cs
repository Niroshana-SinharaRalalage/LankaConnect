using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPrimaryToEventImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.13: Add IsPrimary column to EventImages table
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                schema: "events",
                table: "EventImages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Data migration: Set first image (DisplayOrder = 1) as primary for each event
            migrationBuilder.Sql(@"
                UPDATE events.""EventImages""
                SET ""IsPrimary"" = true
                WHERE ""DisplayOrder"" = 1;
            ");

            // Add unique partial index to ensure only one primary image per event
            // This enforces the business rule at database level
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""IX_EventImages_EventId_IsPrimary_True""
                ON events.""EventImages""(""EventId"")
                WHERE ""IsPrimary"" = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the unique partial index first
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS events.""IX_EventImages_EventId_IsPrimary_True"";
            ");

            // Then drop the column
            migrationBuilder.DropColumn(
                name: "IsPrimary",
                schema: "events",
                table: "EventImages");
        }
    }
}
