using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "event_analytics",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_views = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    unique_viewers = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    registration_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_analytics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_view_records",
                schema: "analytics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_view_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_analytics_event_id_unique",
                schema: "analytics",
                table: "event_analytics",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_analytics_last_viewed_at",
                schema: "analytics",
                table: "event_analytics",
                column: "last_viewed_at");

            migrationBuilder.CreateIndex(
                name: "ix_event_analytics_total_views",
                schema: "analytics",
                table: "event_analytics",
                column: "total_views");

            migrationBuilder.CreateIndex(
                name: "ix_event_view_records_dedup_ip",
                schema: "analytics",
                table: "event_view_records",
                columns: new[] { "event_id", "ip_address", "viewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_view_records_dedup_user",
                schema: "analytics",
                table: "event_view_records",
                columns: new[] { "event_id", "user_id", "viewed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_event_view_records_event_id",
                schema: "analytics",
                table: "event_view_records",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_view_records_ip_address",
                schema: "analytics",
                table: "event_view_records",
                column: "ip_address");

            migrationBuilder.CreateIndex(
                name: "ix_event_view_records_user_id",
                schema: "analytics",
                table: "event_view_records",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_view_records_viewed_at",
                schema: "analytics",
                table: "event_view_records",
                column: "viewed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_analytics",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "event_view_records",
                schema: "analytics");
        }
    }
}
