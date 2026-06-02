using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.162 — adds the optional brochure / flyer image slot on
    /// <c>events.sponsors</c>. Pure additive — 2 nullable text columns,
    /// no backfill needed. Both columns set atomically by the
    /// <c>Sponsor.SetBrochure</c> domain mutator.
    ///
    /// EF Core auto-scaffolded a long tail of <c>UpdateData</c> calls on
    /// <c>reference_data.reference_values.created_at</c> due to seed-data
    /// timestamp drift in the snapshot — those are unrelated to this
    /// migration's intent and have been removed manually so the migration
    /// is strictly additive: 2 AddColumn on Up, 2 DropColumn on Down.
    /// </summary>
    public partial class Phase6A162_AddSponsorBrochure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "brochure_blob_name",
                schema: "events",
                table: "sponsors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brochure_url",
                schema: "events",
                table: "sponsors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "brochure_blob_name",
                schema: "events",
                table: "sponsors");

            migrationBuilder.DropColumn(
                name: "brochure_url",
                schema: "events",
                table: "sponsors");
        }
    }
}
