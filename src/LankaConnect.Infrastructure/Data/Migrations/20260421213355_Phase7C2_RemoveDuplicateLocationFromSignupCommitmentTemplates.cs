using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    public partial class Phase7C2_RemoveDuplicateLocationFromSignupCommitmentTemplates : Migration
    {
        // Phase 7C.2 — Bug A fix: the COMMITMENT DETAILS card in the five signup/volunteer
        // commitment email templates duplicates Event Date + Location rows that already appear
        // in the EVENT DETAILS card further down. The duplication leaks the GPS-coordinate
        // suffix produced by EventLocation.ToString(). This migration surgically strips ONLY
        // the commitment-card duplicates; the EVENT DETAILS card location row is rewritten
        // separately in the next migration (Phase7C2_RewriteEventLocationInSignupCommitmentTemplates).
        //
        // The regex anchors on the "Event Date" label (unique to the commitment card — the
        // event-details card uses "Date &amp; Time") then consumes up to the {{EventLocation}}
        // placeholder's closing </tr>, covering both sibling rows in one pass. REGEXP_REPLACE
        // (not REPLACE) handles the multi-line whitespace variations between seed migrations.
        // See Phase 6A.117/6A.122 memory entry on silent REPLACE failures.
        private const string RemoveDuplicateLocationRegex =
            @"<tr>[\s\S]*?Event Date[\s\S]*?</tr>\s*<tr>[\s\S]*?Location[\s\S]*?\{\{EventLocation\}\}[\s\S]*?</tr>";

        private static readonly string[] TargetTemplates = new[]
        {
            "template-signup-list-commitment-confirmation",
            "template-signup-list-commitment-update",
            "template-signup-list-commitment-cancellation",
            "template-volunteer-commitment-confirmation",
            "template-volunteer-commitment-cancellation",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var name in TargetTemplates)
            {
                // Row-count assertion guards against silent failures: if the regex no longer
                // matches (template already corrected, or renamed), FAIL LOUD rather than
                // record the migration as applied with zero effect.
                migrationBuilder.Sql($@"
DO $migration$
DECLARE
    affected INT;
BEGIN
    UPDATE communications.email_templates
    SET html_template = REGEXP_REPLACE(
        html_template,
        '{RemoveDuplicateLocationRegex}',
        '',
        'g'
    ),
    updated_at = NOW()
    WHERE name = '{name}';

    GET DIAGNOSTICS affected = ROW_COUNT;
    IF affected = 0 THEN
        RAISE EXCEPTION 'Phase 7C.2 remove-duplicate-location migration: template ""{name}"" not found or regex did not match. Aborting.';
    END IF;
END
$migration$;
");
            }

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1205));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1347));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1122));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1268));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1296));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1548));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1237));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1174));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1492));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1461));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1423));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 21, 33, 44, 476, DateTimeKind.Utc).AddTicks(1521));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 7C.2 — the commitment-card duplicate rows are NOT re-inserted on Down().
            // Their original HTML carried inline styles that evolved across multiple seed
            // migrations with non-deterministic whitespace, so a faithful restore cannot be
            // expressed as a single SQL string. Rolling back this migration is intentionally
            // a no-op for the template bodies; if a rollback is ever needed, re-run the
            // seed migration for the affected template names.
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5890));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6056));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5772));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5960));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5992));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6241));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5924));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(5851));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6175));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6137));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6090));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 21, 19, 22, 16, 380, DateTimeKind.Utc).AddTicks(6207));
        }
    }
}
