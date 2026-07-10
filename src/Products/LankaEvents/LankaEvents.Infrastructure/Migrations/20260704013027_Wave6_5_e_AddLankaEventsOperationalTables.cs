using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace LankaConnect.Products.LankaEvents.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class Wave6_5_e_AddLankaEventsOperationalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "events",
                columns: table => new
                {
                    Key = table.Column<Guid>(type: "uuid", nullable: false),
                    SerializedResponse = table.Column<string>(type: "jsonb", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_dead_letter",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalOutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeadLetteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_dead_letter", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_expires_at",
                schema: "events",
                table: "idempotency_keys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending_occurred_at",
                schema: "events",
                table: "outbox",
                column: "OccurredAt",
                filter: "\"ProcessedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_dead_letter_dead_lettered_at",
                schema: "events",
                table: "outbox_dead_letter",
                column: "DeadLetteredAt");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_dead_letter_original_outbox_id",
                schema: "events",
                table: "outbox_dead_letter",
                column: "OriginalOutboxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "events");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "events");

            migrationBuilder.DropTable(
                name: "outbox_dead_letter",
                schema: "events");
        }
    }
}
