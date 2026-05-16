using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.145 — adds per-sponsor image columns to the <c>events.sponsors</c> table.
    ///
    /// Both columns are nullable; the application-layer handler enforces atomic set/clear
    /// (handler always writes both or neither). The threshold gate
    /// (<c>SponsorConfiguration.MinAmountForSponsorImage</c>) determines who's eligible
    /// to upload; this migration just makes the storage available.
    /// </summary>
    public partial class Phase6A145_AddImageToSponsor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_blob_name",
                schema: "events",
                table: "sponsors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                schema: "events",
                table: "sponsors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_blob_name",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "image_url",
                schema: "events",
                table: "sponsors");
        }
    }
}
