using System;
using LankaConnect.Infrastructure.Data.Migrations.Resources;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 7F-A: UPDATE 3 lifecycle email templates with the Mode-B head-count card
    /// inserted at the {{#if HasOrganizerContact}} anchor (psycopg2-probed staging body
    /// + Phase 7E.4 chunk 1 anchor pattern):
    /// - <c>template-event-cancellation-notifications</c> (organiser cancels event broadcast)
    /// - <c>template-event-reminder</c> (cron-driven reminder)
    /// - <c>template-attendees-added-confirmation</c> (post-add-attendees confirmation)
    ///
    /// HTML loaded from embedded resources via <see cref="Phase7FATemplates.LoadHtml"/> so
    /// the migration is independent of disk layout (MEMORY 6A.129b: never <c>File.ReadAllText</c>).
    /// Each template's pre-update body is backed up to <c>communications.email_template_backups</c>
    /// (created in 7E.4 chunk 1) for rollback. UPDATE is parameterised through MigrationBuilder.Sql
    /// to avoid escape headaches on the multi-thousand-character HTML.
    /// </summary>
    public partial class Phase7F_A_FlexibleRegistrationLifecycleTemplates : Migration
    {
        private static readonly string[] TemplateNames = new[]
        {
            "template-event-cancellation-notifications",
            "template-event-reminder",
            "template-attendees-added-confirmation",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defensive: ensure backup table exists (it was created in Phase 7E.4 chunk 1, but
            // a fresh DB reaching this migration first wouldn't have it yet).
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

            foreach (var templateName in TemplateNames)
            {
                // 1. Back up current body before the UPDATE.
                migrationBuilder.Sql($@"
                    INSERT INTO communications.email_template_backups
                        (migration_tag, template_name, html_template_before)
                    SELECT 'Phase7F_A', name, html_template
                    FROM communications.email_templates
                    WHERE name = '{templateName}'
                    ON CONFLICT (migration_tag, template_name) DO NOTHING;
                ");

                // 2. Load v2 HTML and apply via parameterised UPDATE. Single-quote escape is
                // the only PostgreSQL escape needed since the connection is parameterless.
                var newHtml = Phase7FATemplates.LoadHtml(templateName);
                var escapedHtml = newHtml.Replace("'", "''");

                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{escapedHtml}'
                    WHERE name = '{templateName}';
                ");
            }

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1405));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1502));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1346));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1437));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1454));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1586));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1421));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1387));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1556));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1541));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1522));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1571));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore each template's pre-7F-A body from the backup table. Idempotent — if the
            // backup row is missing (e.g. someone wiped it), the UPDATE simply does nothing.
            foreach (var templateName in TemplateNames)
            {
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates t
                    SET html_template = b.html_template_before
                    FROM communications.email_template_backups b
                    WHERE t.name = '{templateName}'
                      AND b.migration_tag = 'Phase7F_A'
                      AND b.template_name = '{templateName}';
                ");
            }

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
    }
}
