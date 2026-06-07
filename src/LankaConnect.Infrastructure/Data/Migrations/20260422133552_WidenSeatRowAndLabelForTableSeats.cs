using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class WidenSeatRowAndLabelForTableSeats : Migration
    {
        // Slice 5 Chunk 12: Seat.CreateAtTable stores the parent table's label in
        // seats.row (because the Row column is polymorphic between theater zones
        // — "A".."ZZ" — and banquet tables where row == tableLabel). The domain
        // allows table labels up to VenueTable.MaxLabelLength (50), but the DB
        // column was char(10) — any table label > 10 chars produced
        // Npgsql 22001 "value too long". Widen row → 50 (matches
        // VenueTable.MaxLabelLength) and label → 58 (50 + "-S{n}" suffix).
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "row",
                schema: "events",
                table: "seats",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "events",
                table: "seats",
                type: "character varying(58)",
                maxLength: 58,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "row",
                schema: "events",
                table: "seats",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "events",
                table: "seats",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(58)",
                oldMaxLength: 58);
        }
    }
}
