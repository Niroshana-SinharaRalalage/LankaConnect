using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferredMetroAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_preferred_metro_areas",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metro_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferred_metro_areas", x => new { x.user_id, x.metro_area_id });
                    table.ForeignKey(
                        name: "fk_user_preferred_metro_areas_metro_area_id",
                        column: x => x.metro_area_id,
                        principalSchema: "events",
                        principalTable: "metro_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_preferred_metro_areas_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_preferred_metro_areas_metro_area_id",
                schema: "identity",
                table: "user_preferred_metro_areas",
                column: "metro_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_preferred_metro_areas_user_id",
                schema: "identity",
                table: "user_preferred_metro_areas",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_preferred_metro_areas",
                schema: "identity");
        }
    }
}
