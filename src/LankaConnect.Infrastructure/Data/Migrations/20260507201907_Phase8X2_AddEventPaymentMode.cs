using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 8X.2 — Add EventPaymentMode + ExternalRegistration columns to events.events.
    ///
    /// Schema additions:
    ///   - payment_mode smallint NOT NULL DEFAULT 0  (Free=0, OnPlatformPaid=1, ExternalPaid=2)
    ///   - external_registration_url varchar(2048) NULL
    ///   - external_registration_instructions text NULL
    ///   - external_registration_vendor_name varchar(100) NULL
    ///   - ix_events_payment_mode (B-tree index on payment_mode)
    ///
    /// Backfill: paid rows get payment_mode = 1; free rows stay at the DB default 0.
    /// A post-backfill RAISE EXCEPTION assertion fails the migration if any row with
    /// IsFreeEvent=false is left at payment_mode=0 (Phase 6A.122 silent-UPDATE-success
    /// lesson — REPLACE/UPDATE can match 0 rows and still record the migration as
    /// applied).
    ///
    /// Down() drops the index then the columns. NOTE: Down() DISCARDS DATA stored in
    /// the new columns. For ExternalPaid events that used the new columns, Down() will
    /// permanently lose external_registration_* values. Forward-only rollback is the
    /// architect-approved default for Phase 8X (deploy code-only rollback before
    /// considering schema rollback). Phase 8Y will introduce an archive-then-drop
    /// pattern when the legacy IsFreeEvent column is dropped.
    /// </summary>
    public partial class Phase8X2_AddEventPaymentMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────────
            //  1. Add the four new columns
            // ─────────────────────────────────────────────────────────────────

            migrationBuilder.AddColumn<short>(
                name: "payment_mode",
                schema: "events",
                table: "events",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "external_registration_url",
                schema: "events",
                table: "events",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_registration_instructions",
                schema: "events",
                table: "events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_registration_vendor_name",
                schema: "events",
                table: "events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // ─────────────────────────────────────────────────────────────────
            //  2. Backfill paid events to payment_mode = 1 (OnPlatformPaid).
            //     Free events keep the DB default 0 (no UPDATE needed).
            // ─────────────────────────────────────────────────────────────────

            migrationBuilder.Sql(@"
                UPDATE events.events
                   SET payment_mode = 1
                 WHERE ""IsFreeEvent"" = false;
            ");

            // ─────────────────────────────────────────────────────────────────
            //  3. Index on payment_mode (supports the optional Phase 8X.6.5
            //     `?paymentMode=` filter and any cohort analytics).
            // ─────────────────────────────────────────────────────────────────

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_events_payment_mode
                    ON events.events (payment_mode);
            ");

            // ─────────────────────────────────────────────────────────────────
            //  4. Post-backfill assertion (Phase 6A.122 lesson).
            //     RAISE EXCEPTION if any paid row was left at payment_mode = 0.
            //     This fails the migration loudly rather than recording success
            //     with silently-bad data.
            // ─────────────────────────────────────────────────────────────────

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                          FROM events.events
                         WHERE ""IsFreeEvent"" = false
                           AND payment_mode = 0
                    ) THEN
                        RAISE EXCEPTION 'Phase 8X.2 backfill failed: paid events with payment_mode=0 still exist';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS events.ix_events_payment_mode;");

            migrationBuilder.DropColumn(
                name: "external_registration_vendor_name",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "external_registration_instructions",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "external_registration_url",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "payment_mode",
                schema: "events",
                table: "events");
        }
    }
}
