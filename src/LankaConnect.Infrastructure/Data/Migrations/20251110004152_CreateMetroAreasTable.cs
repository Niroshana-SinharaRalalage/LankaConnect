using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateMetroAreasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "metro_areas",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    center_latitude = table.Column<double>(type: "double precision", precision: 10, scale: 8, nullable: false),
                    center_longitude = table.Column<double>(type: "double precision", precision: 11, scale: 8, nullable: false),
                    radius_miles = table.Column<int>(type: "integer", nullable: false),
                    is_state_level_area = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metro_areas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_subscribers",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    metro_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    receive_all_locations = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    confirmation_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    confirmation_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unsubscribe_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unsubscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_subscribers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_metro_areas_is_active",
                schema: "events",
                table: "metro_areas",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_metro_areas_name",
                schema: "events",
                table: "metro_areas",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_metro_areas_state",
                schema: "events",
                table: "metro_areas",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "idx_newsletter_subscribers_active_confirmed",
                schema: "communications",
                table: "newsletter_subscribers",
                columns: new[] { "is_active", "is_confirmed" });

            migrationBuilder.CreateIndex(
                name: "idx_newsletter_subscribers_confirmation_token",
                schema: "communications",
                table: "newsletter_subscribers",
                column: "confirmation_token");

            migrationBuilder.CreateIndex(
                name: "idx_newsletter_subscribers_metro_area_id",
                schema: "communications",
                table: "newsletter_subscribers",
                column: "metro_area_id");

            migrationBuilder.CreateIndex(
                name: "idx_newsletter_subscribers_unsubscribe_token",
                schema: "communications",
                table: "newsletter_subscribers",
                column: "unsubscribe_token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metro_areas",
                schema: "events");

            migrationBuilder.DropTable(
                name: "newsletter_subscribers",
                schema: "communications");
        }
    }
}
