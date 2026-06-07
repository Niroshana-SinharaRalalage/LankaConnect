using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Forms.Infrastructure.Migrations
{
    /// <summary>
    /// Phase A W4.3 BASELINE migration for <see cref="LankaConnect.Modules.Forms.Infrastructure.Data.FormsDbContext"/>.
    /// </summary>
    /// <remarks>
    /// Mirrors the W4.2 Media baseline pattern. NEW operational tables
    /// (outbox / outbox_dead_letter / idempotency_keys in the <c>forms</c>
    /// schema) ship in this baseline. LEGACY aggregate tables
    /// (<c>events.event_forms</c> / <c>events.form_questions</c> /
    /// <c>events.form_responses</c> / <c>events.form_answers</c>) remain
    /// owned by the original AppDbContext migrations and are NOT recreated here.
    /// </remarks>
    public partial class Baseline_Forms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "forms");

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "forms",
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
                schema: "forms",
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
                schema: "forms",
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
                schema: "forms",
                table: "idempotency_keys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending_occurred_at",
                schema: "forms",
                table: "outbox",
                column: "OccurredAt",
                filter: "\"ProcessedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_dead_letter_dead_lettered_at",
                schema: "forms",
                table: "outbox_dead_letter",
                column: "DeadLetteredAt");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_dead_letter_original_outbox_id",
                schema: "forms",
                table: "outbox_dead_letter",
                column: "OriginalOutboxId");

            // events.event_forms / events.form_questions / events.form_responses /
            // events.form_answers + their indexes + FKs intentionally NOT created here —
            // owned by legacy AppDbContext migrations.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "idempotency_keys", schema: "forms");
            migrationBuilder.DropTable(name: "outbox", schema: "forms");
            migrationBuilder.DropTable(name: "outbox_dead_letter", schema: "forms");
        }
    }
}
