using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Communications.Infrastructure.Migrations
{
    /// <summary>
    /// Sprint-Day 7 (2026-07-11): CommunicationsDbContext initial migration under the
    /// "empty-Up rebaseline" pattern — Communications physical tables (email_messages,
    /// email_templates, user_email_preferences) were historically materialised by
    /// AppDbContext migrations before the 4C.c relocation (Consult #14 PASS B) moved
    /// ownership to CommunicationsDbContext. This migration establishes the snapshot as
    /// the new baseline WITHOUT re-creating those pre-existing tables.
    ///
    /// <para>
    /// Additionally materialises the 3 per-module operational tables (outbox,
    /// outbox_dead_letter, idempotency_keys) that the OutboxProcessor&lt;CommunicationsDbContext&gt;
    /// needs — same pattern as SprintDay7_AddIdentityOperationalTables (9f7b1578) for Identity.
    /// All DDL uses IF NOT EXISTS so this is idempotent both on staging (base tables exist,
    /// op tables missing) and on dev boxes (may have anything).
    /// </para>
    /// </summary>
    public partial class InitialCommunicationsContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE SCHEMA IF NOT EXISTS communications;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS communications.outbox (
                    ""Id"" uuid PRIMARY KEY,
                    ""EventType"" character varying(512) NOT NULL,
                    ""Payload"" jsonb NOT NULL,
                    ""OccurredAt"" timestamp with time zone NOT NULL,
                    ""ProcessedAt"" timestamp with time zone NULL,
                    ""RetryCount"" integer NOT NULL DEFAULT 0,
                    ""LastError"" character varying(2000) NULL
                );");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_outbox_pending_occurred_at
                ON communications.outbox (""OccurredAt"")
                WHERE ""ProcessedAt"" IS NULL;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS communications.outbox_dead_letter (
                    ""Id"" uuid PRIMARY KEY,
                    ""OriginalOutboxId"" uuid NOT NULL,
                    ""EventType"" character varying(512) NOT NULL,
                    ""Payload"" jsonb NOT NULL,
                    ""OccurredAt"" timestamp with time zone NOT NULL,
                    ""DeadLetteredAt"" timestamp with time zone NOT NULL,
                    ""RetryCount"" integer NOT NULL,
                    ""LastError"" character varying(2000) NULL
                );");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_outbox_dead_letter_dead_lettered_at
                ON communications.outbox_dead_letter (""DeadLetteredAt"");");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_outbox_dead_letter_original_outbox_id
                ON communications.outbox_dead_letter (""OriginalOutboxId"");");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS communications.idempotency_keys (
                    ""Key"" uuid PRIMARY KEY,
                    ""SerializedResponse"" jsonb NOT NULL,
                    ""RecordedAt"" timestamp with time zone NOT NULL,
                    ""ExpiresAt"" timestamp with time zone NOT NULL
                );");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_idempotency_keys_expires_at
                ON communications.idempotency_keys (""ExpiresAt"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS communications.idempotency_keys;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS communications.outbox_dead_letter;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS communications.outbox;");
        }
    }
}
