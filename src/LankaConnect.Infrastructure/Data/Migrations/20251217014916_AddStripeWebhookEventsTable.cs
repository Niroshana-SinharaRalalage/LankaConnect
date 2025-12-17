using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeWebhookEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stripe_webhook_events",
                schema: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    processed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stripe_webhook_events_event_id",
                schema: "payments",
                table: "stripe_webhook_events",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stripe_webhook_events_event_type",
                schema: "payments",
                table: "stripe_webhook_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_stripe_webhook_events_processed",
                schema: "payments",
                table: "stripe_webhook_events",
                column: "processed");

            migrationBuilder.CreateIndex(
                name: "ix_stripe_webhook_events_processed_created_at",
                schema: "payments",
                table: "stripe_webhook_events",
                columns: new[] { "processed", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stripe_webhook_events",
                schema: "payments");
        }
    }
}
