using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDualTicketPricingAndMultiAttendee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_registrations_user_xor_attendee",
                schema: "events",
                table: "registrations");

            migrationBuilder.AddColumn<string>(
                name: "attendees",
                schema: "events",
                table: "registrations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact",
                schema: "events",
                table: "registrations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "total_price_amount",
                schema: "events",
                table: "registrations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "total_price_currency",
                schema: "events",
                table: "registrations",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pricing",
                schema: "events",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations",
                sql: "(\n                    (user_id IS NOT NULL AND attendee_info IS NULL) OR\n                    (user_id IS NULL AND attendee_info IS NOT NULL) OR\n                    (attendees IS NOT NULL AND contact IS NOT NULL)\n                )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "attendees",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "contact",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "total_price_amount",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "total_price_currency",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "pricing",
                schema: "events",
                table: "events");

            migrationBuilder.AddCheckConstraint(
                name: "ck_registrations_user_xor_attendee",
                schema: "events",
                table: "registrations",
                sql: "(user_id IS NOT NULL AND attendee_info IS NULL) OR (user_id IS NULL AND attendee_info IS NOT NULL)");
        }
    }
}
