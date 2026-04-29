using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Slice 9.3 hard-delete orphaned venue layouts.
    ///
    /// <para>
    /// Slice 8 RC-2 (architect Rev 1): the from-preset → assign two-step flow could
    /// fail on its second step (tier-validation) but leave the layout already
    /// persisted with <c>events.venue_layouts.event_id = X</c>. Combined with
    /// <c>GetByEventIdAsync</c> filtering by <c>event_id</c> instead of joining via
    /// <c>events.events.venue_layout_id</c>, these orphans were visible to the UI
    /// as if assigned. Slice 9.3 fixes the read path; this migration deletes the
    /// existing orphan rows on staging (production has zero orphans — the buggy
    /// flow never shipped to prod).
    /// </para>
    ///
    /// <para>
    /// Definition of orphan: a row in <c>events.venue_layouts</c> where
    /// <c>event_id IS NOT NULL</c> AND no <c>events.events</c> row references its
    /// <c>id</c> via <c>venue_layout_id</c>. Templates (<c>event_id IS NULL</c>) are
    /// never orphans.
    /// </para>
    ///
    /// <para>
    /// Safety guards (per MEMORY 6A.122 silent-failure rule):
    /// <list type="bullet">
    /// <item>Pre-flight: count orphans, RAISE NOTICE.</item>
    /// <item>Pre-flight: ensure no live <c>seat_holds</c> reference orphan-layout
    /// seats — RAISE EXCEPTION if any (cascade safety; admin must investigate).</item>
    /// <item>Audit snapshot into a generic <c>events.deleted_layouts_audit</c> table
    /// before DELETE. Forensic trail; not enough to reconstruct (zones/seats are
    /// not snapshotted) but enough to identify what was lost.</item>
    /// <item>Post-condition: deletion count == orphan count, else RAISE EXCEPTION
    /// (transaction rolls back).</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Production safety: handles N=0 orphans cleanly. RAISE NOTICE logs zero,
    /// audit table created (empty), DELETE matches 0 rows, post-condition passes
    /// (0 == 0). Migration succeeds.
    /// </para>
    ///
    /// <para>
    /// <c>Down()</c> is a logged no-op — hard-delete is irreversible. The audit
    /// snapshot preserves the forensic trail. The reference_data timestamp
    /// updates (cosmetic EF Core scaffolding drift) are preserved in both
    /// directions to keep the model snapshot consistent with prior migrations.
    /// </para>
    /// </summary>
    public partial class Slice93HardDeleteOrphanLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─── Slice 9.3 hard-delete logic ─────────────────────────────────
            // Single PL/pgSQL DO block to keep all guards + audit + delete in one
            // atomic transaction. Errors abort the migration via RAISE EXCEPTION.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    v_orphan_count int;
                    v_hold_count int;
                    v_audit_count int;
                    v_deleted_count int;
                BEGIN
                    -- 1. Create the generic audit table (idempotent).
                    CREATE TABLE IF NOT EXISTS events.deleted_layouts_audit (
                        audit_id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                        layout_id             uuid NOT NULL,
                        layout_name           text,
                        event_id              uuid,
                        original_created_at   timestamptz,
                        zone_count            int,
                        seat_count            int,
                        deleted_at            timestamptz NOT NULL DEFAULT NOW(),
                        deleted_by_migration  text NOT NULL
                    );

                    -- 2. Count orphans (pre-flight).
                    SELECT COUNT(*) INTO v_orphan_count
                    FROM events.venue_layouts vl
                    WHERE vl.event_id IS NOT NULL
                      AND NOT EXISTS (
                          SELECT 1 FROM events.events e WHERE e.venue_layout_id = vl.""Id""
                      );

                    RAISE NOTICE '[Slice93] Found % orphan venue_layouts row(s) to delete', v_orphan_count;

                    IF v_orphan_count = 0 THEN
                        RAISE NOTICE '[Slice93] No orphans to delete. Migration completes as no-op (production-safe path).';
                        RETURN;
                    END IF;

                    -- 3. Pre-flight: ensure no live seat_holds reference orphan-layout seats.
                    --    If any exist, abort — cascade would silently destroy active holds.
                    SELECT COUNT(*) INTO v_hold_count
                    FROM events.seat_holds sh
                    WHERE sh.seat_id IN (
                        SELECT s.""Id""
                        FROM events.seats s
                        WHERE s.venue_zone_id IN (
                            SELECT z.""Id""
                            FROM events.venue_zones z
                            WHERE z.venue_layout_id IN (
                                SELECT vl.""Id"" FROM events.venue_layouts vl
                                WHERE vl.event_id IS NOT NULL
                                  AND NOT EXISTS (
                                      SELECT 1 FROM events.events e WHERE e.venue_layout_id = vl.""Id""
                                  )
                            )
                        )
                        OR s.venue_table_id IN (
                            SELECT t.""Id""
                            FROM events.venue_tables t
                            WHERE t.venue_layout_id IN (
                                SELECT vl.""Id"" FROM events.venue_layouts vl
                                WHERE vl.event_id IS NOT NULL
                                  AND NOT EXISTS (
                                      SELECT 1 FROM events.events e WHERE e.venue_layout_id = vl.""Id""
                                  )
                            )
                        )
                    );

                    IF v_hold_count > 0 THEN
                        RAISE EXCEPTION '[Slice93] % live seat_hold(s) reference orphan-layout seats. Aborting cascade-unsafe delete. Investigate before re-running.', v_hold_count;
                    END IF;

                    RAISE NOTICE '[Slice93] Pre-flight passed: 0 live seat_holds reference orphan-layout seats';

                    -- 4. Audit snapshot BEFORE delete.
                    INSERT INTO events.deleted_layouts_audit
                        (layout_id, layout_name, event_id, original_created_at, zone_count, seat_count, deleted_by_migration)
                    SELECT
                        vl.""Id"",
                        vl.name,
                        vl.event_id,
                        vl.created_at,
                        (SELECT COUNT(*) FROM events.venue_zones z WHERE z.venue_layout_id = vl.""Id""),
                        (
                            (SELECT COUNT(*)
                             FROM events.seats s
                             JOIN events.venue_zones z ON z.""Id"" = s.venue_zone_id
                             WHERE z.venue_layout_id = vl.""Id"")
                          + (SELECT COUNT(*)
                             FROM events.seats s
                             JOIN events.venue_tables t ON t.""Id"" = s.venue_table_id
                             WHERE t.venue_layout_id = vl.""Id"")
                        ),
                        'Slice93HardDeleteOrphanLayouts'
                    FROM events.venue_layouts vl
                    WHERE vl.event_id IS NOT NULL
                      AND NOT EXISTS (
                          SELECT 1 FROM events.events e WHERE e.venue_layout_id = vl.""Id""
                      );

                    GET DIAGNOSTICS v_audit_count = ROW_COUNT;

                    IF v_audit_count <> v_orphan_count THEN
                        RAISE EXCEPTION '[Slice93] Audit snapshot count (%) does not match orphan count (%). Aborting.', v_audit_count, v_orphan_count;
                    END IF;

                    RAISE NOTICE '[Slice93] Audit snapshot wrote % row(s) into events.deleted_layouts_audit', v_audit_count;

                    -- 5. Hard delete (cascades through zones → seats, tables → seats,
                    --    decorations, tier_assignments via FK ON DELETE CASCADE).
                    DELETE FROM events.venue_layouts vl
                    WHERE vl.event_id IS NOT NULL
                      AND NOT EXISTS (
                          SELECT 1 FROM events.events e WHERE e.venue_layout_id = vl.""Id""
                      );

                    GET DIAGNOSTICS v_deleted_count = ROW_COUNT;

                    -- 6. Post-condition guard (Phase 6A.122 silent-failure rule).
                    IF v_deleted_count <> v_orphan_count THEN
                        RAISE EXCEPTION '[Slice93] Deletion count (%) does not match orphan count (%). Rolling back.', v_deleted_count, v_orphan_count;
                    END IF;

                    RAISE NOTICE '[Slice93] Deleted % orphan venue_layouts row(s) (matched expected count). Audit snapshot preserved.', v_deleted_count;
                END $$;
            ");

            // ─── Cosmetic reference_data timestamp drift (EF Core scaffolding) ──
            // These UpdateData calls are scaffolded by `dotnet ef migrations add`
            // because the seeder uses DateTime.UtcNow for created_at on reference
            // values. Each migration shifts the snapshot by a few ticks. They are
            // idempotent and load-bearing only for keeping the model snapshot
            // (.Designer.cs / AppDbContextModelSnapshot.cs) consistent.

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(6989));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7050));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(6957));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7011));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7021));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7103));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7000));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(6977));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7085));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7075));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7060));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 29, 18, 55, 21, 516, DateTimeKind.Utc).AddTicks(7094));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Hard-delete is irreversible. The audit snapshot at events.deleted_layouts_audit
            // preserves the forensic trail (layout name, event id, zone/seat counts) but
            // not the full data — zones/seats/tier_assignments cannot be reconstructed.
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    RAISE NOTICE '[Slice93 Down] Hard-delete migration is not reversible. Forensic trail preserved at events.deleted_layouts_audit (deleted_by_migration = ''Slice93HardDeleteOrphanLayouts'').';
                END $$;
            ");

            // Revert reference_data timestamp drift.
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8735));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8812));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8685));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8770));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8785));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8892));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8753));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8717));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8862));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8846));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8827));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 20, 10, 34, 727, DateTimeKind.Utc).AddTicks(8878));
        }
    }
}
