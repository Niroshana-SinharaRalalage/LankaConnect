using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitingListAndSocialSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "share_count",
                schema: "analytics",
                table: "event_analytics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "event_waiting_list",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_waiting_list", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_waiting_list_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_waiting_list_event_position",
                schema: "events",
                table: "event_waiting_list",
                columns: new[] { "EventId", "position" });

            migrationBuilder.CreateIndex(
                name: "ix_event_waiting_list_event_user",
                schema: "events",
                table: "event_waiting_list",
                columns: new[] { "EventId", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_waiting_list",
                schema: "events");

            migrationBuilder.DropColumn(
                name: "share_count",
                schema: "analytics",
                table: "event_analytics");
        }
    }
}
