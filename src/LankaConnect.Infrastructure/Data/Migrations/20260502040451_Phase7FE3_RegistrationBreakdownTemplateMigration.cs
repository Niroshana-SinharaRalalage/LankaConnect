using System;
using LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 7F-E.3 (architect-approved 2026-05-01): UPDATE 5 email templates so they
    /// render the cross-surface <c>{{{RegistrationBreakdownHtml}}}</c> token instead of
    /// the legacy flat <c>HeadCountTotal</c> / <c>HeadCountBreakdownLine</c> /
    /// <c>TierBreakdownLine</c> trio.
    ///
    /// Templates touched:
    /// - 4 anchored templates have their inner <c>&lt;!-- attendee-block-7e --&gt;</c>
    ///   block replaced with the new structured token (free-confirmation, cancellation,
    ///   reminder, attendees-added).
    /// - 1 template (paid-with-ticket) gains a NEW anchor block — fixes the user-
    ///   reported gap where paid-event emails had no per-tier breakdown at all.
    ///
    /// HTML loaded from embedded resources via <see cref="Phase7FETemplates.LoadHtml"/>
    /// (memory 6A.129b). Each template's pre-update body is backed up to
    /// <c>communications.email_template_backups</c> for rollback.
    ///
    /// NB: this auto-generated migration also includes <c>UpdateData</c> calls on
    /// <c>reference_data.reference_values.created_at</c> — those are EF Core artefact
    /// noise from drift between snapshots. They're idempotent timestamp shuffles and
    /// cause no functional change.
    /// </summary>
    public partial class Phase7FE3_RegistrationBreakdownTemplateMigration : Migration
    {
        private static readonly string[] TemplateNames = new[]
        {
            "template-free-event-registration-confirmation",
            "template-event-cancellation-notifications",
            "template-event-reminder",
            "template-attendees-added-confirmation",
            "template-paid-event-registration-confirmation-with-ticket",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defensive: ensure backup table exists (created in Phase 7E.4 chunk 1; a
            // fresh DB reaching this migration first wouldn't have it yet).
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
                    SELECT 'Phase7F_E_3', name, html_template
                    FROM communications.email_templates
                    WHERE name = '{templateName}'
                    ON CONFLICT (migration_tag, template_name) DO NOTHING;
                ");

                // 2. Load v3 HTML from embedded resource and apply via parameterised UPDATE.
                var newHtml = Phase7FETemplates.LoadHtml(templateName);
                var escapedHtml = newHtml.Replace("'", "''");

                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = '{escapedHtml}'
                    WHERE name = '{templateName}';
                ");
            }

            // --- Auto-scaffolded reference_data drift below — preserved as-is ---
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6648));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6717));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6601));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6679));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6693));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6801));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6662));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6631));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6771));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6754));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6734));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 2, 4, 4, 48, 296, DateTimeKind.Utc).AddTicks(6786));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore each template's pre-7F-E.3 body from the backup table. Idempotent — if the
            // backup row is missing (e.g. it was manually deleted), the UPDATE simply does nothing.
            foreach (var templateName in TemplateNames)
            {
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates t
                    SET html_template = b.html_template_before
                    FROM communications.email_template_backups b
                    WHERE t.name = '{templateName}'
                      AND b.migration_tag = 'Phase7F_E_3'
                      AND b.template_name = '{templateName}';
                ");
            }

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1883));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1951));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1841));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1913));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1929));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(2032));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1899));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1867));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1997));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1984));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(1966));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 13, 39, 47, 100, DateTimeKind.Utc).AddTicks(2019));
        }
    }
}
