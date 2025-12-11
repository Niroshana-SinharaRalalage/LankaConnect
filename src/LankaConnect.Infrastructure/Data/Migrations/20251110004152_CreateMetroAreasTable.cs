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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metro_areas",
                schema: "events");
        }
    }
}
