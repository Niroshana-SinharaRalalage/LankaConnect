using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.133 Email: Fix 3 templates missing organizer contact support.
    ///
    /// 1. template-newsletter-notification: INSERT organizer contact block (was never added)
    /// 2. template-refund-requested: REPLACE unwrapped organizer card with standardized block
    /// 3. template-refund-completed: INSERT organizer contact block (was completely missing)
    ///
    /// Uses REGEXP_REPLACE with 'gs' flags (global + dot-matches-newline) for robust matching.
    /// All 3 templates get the same standardized organizer HTML card with:
    ///   - {{#if HasOrganizerContact}} conditional wrapper
    ///   - {{OrganizerContactHeader}} dynamic header
    ///   - {{{OrganizerContactsHtml}}} pre-formatted HTML table (triple-brace for unescaped)
    /// </summary>
    public partial class Phase6A133Email_FixRemainingOrganizerTemplates : Migration
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

            // ============================================================
            // Phase 6A.133 Email: Fix 3 templates missing organizer contacts
            // ============================================================

            // Standardized organizer contact HTML block (same format as Phase 6A.133 migration)
            var organizerBlock = @"{{#if HasOrganizerContact}}<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0 0;""><tr><td style=""background: #fefaf7; padding: 20px; border-radius: 8px; border: 1px solid #f3e4d5;""><p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 700; color: #9f1239; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>{{{OrganizerContactsHtml}}}</td></tr></table>{{/if}}";
            var sqlOrganizerBlock = organizerBlock.Replace("'", "''");

            // -------------------------------------------------------
            // 1. template-newsletter-notification: INSERT organizer block
            //    Anchor: Insert before <!-- DUAL CTA BUTTONS --> comment
            // -------------------------------------------------------
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- DUAL CTA BUTTONS -->',
                    '{sqlOrganizerBlock}<!-- DUAL CTA BUTTONS -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                  AND html_template LIKE '%DUAL CTA BUTTONS%'
                  AND html_template NOT LIKE '%HasOrganizerContact%';
            ");

            // -------------------------------------------------------
            // 2. template-refund-requested: REPLACE unwrapped organizer card
            //    The existing card has OrganizerContactName/Email/Phone but NO
            //    {{#HasOrganizerContact}} wrapper. Replace the entire section
            //    between <!-- ORGANIZER CONTACT CARD --> and <!-- CTA BUTTON -->
            //    with the standardized block.
            // -------------------------------------------------------
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<!--\s*ORGANIZER\s+CONTACT\s+CARD\s*-->.*?<!--\s*CTA\s+BUTTON\s*-->',
                    '{sqlOrganizerBlock}<!-- CTA BUTTON -->',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-refund-requested'
                  AND html_template LIKE '%OrganizerContactName%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");

            // -------------------------------------------------------
            // 3. template-refund-completed: INSERT organizer block
            //    Anchor: Insert before <!-- CTA BUTTON --> comment
            // -------------------------------------------------------
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- CTA BUTTON -->',
                    '{sqlOrganizerBlock}<!-- CTA BUTTON -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-refund-completed'
                  AND html_template LIKE '%CTA BUTTON%'
                  AND html_template NOT LIKE '%HasOrganizerContact%';
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
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7564));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7634));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7452));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7596));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7612));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7711));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7581));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7478));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7682));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7668));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7649));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 9, 1, 27, 17, 31, DateTimeKind.Utc).AddTicks(7697));
        }
    }
}
