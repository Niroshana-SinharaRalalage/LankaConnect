using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    public partial class Phase7C_FixRebrandPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(8929));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(9022));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(8873));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(8967));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(8985));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(9120));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(8948));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(8909));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(9084));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(9065));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(9041));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 19, 6, 38, 368, DateTimeKind.Utc).AddTicks(9102));

            // =========================================================================
            // Phase 7C Fix: Correct rebrand patterns for multi-line HTML in DB
            //
            // Migration 2 (Phase7C_ReбрандAllTemplates) used patterns designed for
            // compact single-line CSS (e.g. "font-size: 20px; font-weight: 500;").
            // The DB stores multi-line deeply-indented CSS where "LankaConnect" brand
            // text is on its own indented line between ">" and "</p>".
            //
            // All passes are idempotent: WHERE clauses match only templates that still
            // need the change, so re-running is safe.
            // =========================================================================

            // Pass 1: Logo brand title text (LankaConnect → LankaEvents)
            // DB format: ">\n{spaces}LankaConnect\n{spaces}</p>"
            // Anchors to "color: #ffffff" inside the style attribute — unique to logo title.
            // [^""]* in C# verbatim = [^"]* in SQL = matches anything except a double-quote,
            // spanning newlines (POSIX character class negation includes \n).
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '(color: #ffffff;[^""]*""\s*>\s*)LankaConnect(\s*</p>)',
                    '\1LankaEvents\2',
                    'g')
                WHERE html_template LIKE '%LankaConnect%'
                  AND html_template LIKE '%color: #ffffff%';
            ");

            // Pass 2: Footer tagline text
            // Simple REPLACE — 'Sri Lankan Community Hub' is unique to the footer tagline.
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(html_template, 'Sri Lankan Community Hub', 'by LankaConnect')
                WHERE html_template LIKE '%Sri Lankan Community Hub%';
            ");

            // Pass 3: Logo title font-weight 500 → 700
            // DB stores CSS properties on separate lines:
            //   font-size: 20px;\n{spaces}font-weight: 500;
            // \s+ in POSIX regex matches newlines + spaces between the two properties.
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '(font-size: 20px;\s+font-weight: )500(;)',
                    '\1700\2',
                    'g')
                WHERE html_template LIKE '%font-size: 20px%'
                  AND html_template LIKE '%font-weight: 500%';
            ");

            // Pass 4: Footer copyright — &copy; named HTML entity
            // Migration 2 only handled &#169; and literal © — DB uses &copy;
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(html_template, '&copy; 2026 LankaConnect.', '&copy; {{Year}} LankaEvents.')
                WHERE html_template LIKE '%&copy; 2026 LankaConnect.%';
            ");

            // Pass 5: HTML <title> tag
            // DB format: "<title>Event Reminder - LankaConnect</title>" (not bare "<title>LankaConnect</title>")
            // Matching only the suffix 'LankaConnect</title>' handles all title variants.
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(html_template, 'LankaConnect</title>', 'LankaEvents</title>', 'g')
                WHERE html_template LIKE '%LankaConnect</title>%';
            ");

            // Pass 6: Footer sign-off (idempotent — may already be applied by Migration 2)
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(html_template, 'The LankaConnect Team', 'The LankaEvents Team')
                WHERE html_template LIKE '%The LankaConnect Team%';
            ");

            // Pass 7: Body text "LankaConnect community" → "LankaEvents community"
            // 4 templates have this in the body (event-cancellation, event-details-publication,
            // new-event-publication, newsletter-notification).
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(html_template, 'LankaConnect community', 'LankaEvents community')
                WHERE html_template LIKE '%LankaConnect community%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Email template data changes are intentional rebranding and are not reversed here.

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1427));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1483));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1367));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1456));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1470));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1550));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1442));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1411));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1524));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1511));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1498));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 42, 51, 591, DateTimeKind.Utc).AddTicks(1537));
        }
    }
}
