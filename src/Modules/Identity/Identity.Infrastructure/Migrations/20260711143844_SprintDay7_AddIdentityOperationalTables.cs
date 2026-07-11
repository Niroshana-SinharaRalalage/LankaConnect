using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Modules.Identity.Infrastructure.Migrations
{
    /// <summary>
    /// Sprint-Day 7 (2026-07-11): Materialise identity.outbox + identity.outbox_dead_letter +
    /// identity.idempotency_keys — the 3 IdentityModule operational tables that the
    /// InitialIdentityContext (4C.e.3) migration LEFT OUT because its Up() body was blank
    /// ("empty-Up() rebaseline"). Its snapshot DECLARES them so <c>dotnet ef migrations add</c>
    /// doesn't detect a delta, but the physical DDL was never applied on staging.
    ///
    /// <para>
    /// Uses raw SQL with <c>IF NOT EXISTS</c> guards so this is safe both on staging (nothing
    /// exists) and on dev boxes (may have been created manually). The Users → users rename
    /// EF generated alongside this migration is DELETED — physical staging is already
    /// <c>identity.users</c> (created by AppDbContext migrations pre-4C.e). UserConfiguration.
    /// ToTable("users","identity") at 9958d128 aligns the model with physical; the snapshot
    /// delta was informational only.
    /// </para>
    /// </summary>
    public partial class SprintDay7_AddIdentityOperationalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE SCHEMA IF NOT EXISTS identity;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS identity.outbox (
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
                ON identity.outbox (""OccurredAt"")
                WHERE ""ProcessedAt"" IS NULL;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS identity.outbox_dead_letter (
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
                ON identity.outbox_dead_letter (""DeadLetteredAt"");");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_outbox_dead_letter_original_outbox_id
                ON identity.outbox_dead_letter (""OriginalOutboxId"");");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS identity.idempotency_keys (
                    ""Key"" uuid PRIMARY KEY,
                    ""SerializedResponse"" jsonb NOT NULL,
                    ""RecordedAt"" timestamp with time zone NOT NULL,
                    ""ExpiresAt"" timestamp with time zone NOT NULL
                );");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_idempotency_keys_expires_at
                ON identity.idempotency_keys (""ExpiresAt"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS identity.idempotency_keys;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS identity.outbox_dead_letter;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS identity.outbox;");
        }
    }
}
