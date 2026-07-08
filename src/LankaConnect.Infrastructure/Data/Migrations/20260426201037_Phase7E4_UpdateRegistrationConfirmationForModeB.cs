using System;
using LankaConnect.Infrastructure.Data.Migrations.Resources;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 7E.4 (chunk 1 of 5): UPDATE the <c>template-free-event-registration-confirmation</c>
    /// row in <c>communications.email_templates</c> with the v2 HTML — adds a Mode-B
    /// "Registered Attendees" card (HasHeadCount block) right after the existing Mode-A
    /// HasAttendeeDetails block. The new card renders Lead name + Total + demographic
    /// breakdown line + tier breakdown line for HeadCountOnly / HeadCountByAge /
    /// HeadCountByGender / HeadCountByAgeAndGender registrations.
    ///
    /// The HTML is loaded from an embedded resource via <see cref="Phase7E4Templates.LoadHtml"/>
    /// (per MEMORY 6A.129b — never <c>File.ReadAllText</c> in migrations: disk layout differs
    /// across local / CI / Docker). PostgreSQL parameterised UPDATE — no escape headaches.
    /// </summary>
    public partial class Phase7E4_UpdateRegistrationConfirmationForModeB : Migration
    {
        private const string TemplateName = "template-free-event-registration-confirmation";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backup current html_template into a recovery row before the UPDATE so we have
            // a self-contained rollback path (per MEMORY 7C.2 — the over-greedy REGEXP
            // recovery would have been faster with an in-DB backup). The backup row is
            // keyed by template name + a tag identifying this migration.
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS communications.email_template_backups (
                    backup_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
                    migration_tag text NOT NULL,
                    template_name text NOT NULL,
                    html_template_before text,
                    backed_up_at timestamptz NOT NULL DEFAULT NOW(),
                    UNIQUE (migration_tag, template_name)
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO communications.email_template_backups
                    (migration_tag, template_name, html_template_before)
                SELECT 'Phase7E4_chunk1', name, html_template
                FROM communications.email_templates
                WHERE name = 'template-free-event-registration-confirmation'
                ON CONFLICT (migration_tag, template_name) DO NOTHING;
            ");

            // Load the v2 HTML body and apply via parameterised UPDATE. Using a parameterised
            // SqlOperation (rather than string interpolation) avoids any single-quote escape
            // issues in the multi-thousand-character HTML content.
            var newHtml = Phase7E4Templates.LoadHtml(TemplateName);

            migrationBuilder.Sql(
                @"UPDATE communications.email_templates
                  SET html_template = '" + newHtml.Replace("'", "''") + @"'
                  WHERE name = '" + TemplateName + @"';");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the html_template from the backup row created in Up(). Idempotent —
            // if the backup row is missing (manual cleanup), the UPDATE simply touches no rows
            // and the migration record is removed cleanly.
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates t
                SET html_template = b.html_template_before
                FROM communications.email_template_backups b
                WHERE t.name = b.template_name
                  AND b.migration_tag = 'Phase7E4_chunk1'
                  AND t.name = 'template-free-event-registration-confirmation';
            ");

            // Remove our backup row to keep the table tidy (other migrations may share the table).
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_template_backups
                WHERE migration_tag = 'Phase7E4_chunk1'
                  AND template_name = 'template-free-event-registration-confirmation';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(3928));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(3989));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(3889));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(3954));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(3967));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(4064));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(3940));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(3914));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(4040));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(4027));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(4001));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 26, 1, 9, 18, 915, DateTimeKind.Utc).AddTicks(4052));
        }
    }
}
