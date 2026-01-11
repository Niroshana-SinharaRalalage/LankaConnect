using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6AX_MANUAL_AddEventOrganizerContactColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.X: Add Event Organizer Contact Details columns to events table
            migrationBuilder.AddColumn<bool>(
                name: "publish_organizer_contact",
                schema: "events",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "organizer_contact_name",
                schema: "events",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organizer_contact_phone",
                schema: "events",
                table: "events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organizer_contact_email",
                schema: "events",
                table: "events",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.X: Remove Event Organizer Contact Details columns
            migrationBuilder.DropColumn(
                name: "organizer_contact_email",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "organizer_contact_phone",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "organizer_contact_name",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "publish_organizer_contact",
                schema: "events",
                table: "events");
        }
    }
}
