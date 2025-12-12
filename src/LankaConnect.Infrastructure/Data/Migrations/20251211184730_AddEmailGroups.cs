using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "badges");

            migrationBuilder.CreateTable(
                name: "badges",
                schema: "badges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BlobName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Position = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "event_badges",
                schema: "badges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_badges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_badges_badges_BadgeId",
                        column: x => x.BadgeId,
                        principalSchema: "badges",
                        principalTable: "badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_event_badges_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Badges_DisplayOrder",
                schema: "badges",
                table: "badges",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Badges_IsActive",
                schema: "badges",
                table: "badges",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Badges_IsSystem",
                schema: "badges",
                table: "badges",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_Badges_Name",
                schema: "badges",
                table: "badges",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventBadges_BadgeId",
                schema: "badges",
                table: "event_badges",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_EventBadges_EventId",
                schema: "badges",
                table: "event_badges",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventBadges_EventId_BadgeId",
                schema: "badges",
                table: "event_badges",
                columns: new[] { "EventId", "BadgeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_badges",
                schema: "badges");

            migrationBuilder.DropTable(
                name: "badges",
                schema: "badges");
        }
    }
}
