using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.133 Email: Fix organizer contact placement in 2 templates.
    ///
    /// 1. template-newsletter-notification: MOVE organizer block from inside Event Details card
    ///    to before <!-- CLOSING --> (as a separate section, matching other templates).
    /// 2. template-event-reminder: REMOVE old/broken organizer block, INSERT standardized block
    ///    before <!-- CLOSING -->.
    ///
    /// Also fixes EventRepository.GetWithRegistrationsAsync to include OrganizerContacts
    /// (manual reminder trigger was missing this Include).
    /// </summary>
    public partial class Phase6A133Email_FixTemplateOrganizerPlacement : Migration
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
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1669));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1777));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1625));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1715));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1731));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1856));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1697));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1653));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1829));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1815));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1793));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 10, 1, 59, 51, 848, DateTimeKind.Utc).AddTicks(1842));

            // ============================================================
            // Phase 6A.133 Email: Fix organizer block placement in 2 templates
            // ============================================================

            // Standardized organizer contact HTML block
            var organizerBlock = @"{{#if HasOrganizerContact}}<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0 0;""><tr><td style=""background: #fefaf7; padding: 20px; border-radius: 8px; border: 1px solid #f3e4d5;""><p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 700; color: #9f1239; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>{{{OrganizerContactsHtml}}}</td></tr></table>{{/if}}";
            var sqlOrganizerBlock = organizerBlock.Replace("'", "''");

            // -------------------------------------------------------
            // 1. template-newsletter-notification:
            //    REMOVE organizer block from inside Event Details card
            //    (was incorrectly placed before <!-- DUAL CTA BUTTONS -->)
            //    then RE-INSERT before <!-- CLOSING --> as a separate section
            // -------------------------------------------------------

            // Step 1a: Remove the incorrectly-placed organizer block
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{{\{{#if HasOrganizerContact\}}\}}.*?\{{\{{/if\}}\}}\s*<!-- DUAL CTA BUTTONS -->',
                    '<!-- DUAL CTA BUTTONS -->',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                  AND html_template LIKE '%HasOrganizerContact%DUAL CTA BUTTONS%';
            ");

            // Step 1b: Insert organizer block before <!-- CLOSING -->
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- CLOSING -->',
                    '{sqlOrganizerBlock}<!-- CLOSING -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                  AND html_template LIKE '%CLOSING%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");

            // -------------------------------------------------------
            // 2. template-event-reminder:
            //    REMOVE any existing organizer block (old or new format)
            //    then INSERT standardized block before <!-- CLOSING -->
            // -------------------------------------------------------

            // Step 2a: Remove old-style organizer block with ORGANIZER CONTACT CARD comment
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<!--\s*ORGANIZER\s+CONTACT\s+CARD\s*-->.*?(\{\{/HasOrganizerContact\}\}|\{\{/if\}\})',
                    '',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-reminder'
                  AND html_template LIKE '%ORGANIZER CONTACT%';
            ");

            // Step 2b: Remove any remaining {{#if HasOrganizerContact}}...{{/if}} block
            //          (in case 20260307 migration already replaced it to new format)
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{\{#if HasOrganizerContact\}\}.*?\{\{/if\}\}',
                    '',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-reminder'
                  AND html_template LIKE '%HasOrganizerContact%';
            ");

            // Step 2c: Insert standardized organizer block before <!-- CLOSING -->
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- CLOSING -->',
                    '{sqlOrganizerBlock}<!-- CLOSING -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-reminder'
                  AND html_template LIKE '%CLOSING%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(7817));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(8065));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(7639));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(7919));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(7967));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(8513));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(7867));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(7757));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(8392));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(8174));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(8113));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 19, 38, 11, 409, DateTimeKind.Utc).AddTicks(8466));
        }
    }
}
