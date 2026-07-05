using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.133 Email: Updates all email templates to support multiple organizer contacts.
    ///
    /// BEFORE: Templates render single organizer using {{OrganizerContactName}}, {{OrganizerContactEmail}}, {{OrganizerContactPhone}}
    /// AFTER: Templates render all organizer contacts using {{{OrganizerContactsHtml}}} (pre-formatted HTML from OrganizerContactHtmlBuilder)
    ///        and {{OrganizerContactHeader}} for dynamic "EVENT ORGANIZER(S)" header text.
    ///
    /// The entire {{#HasOrganizerContact}}...{{/HasOrganizerContact}} block is replaced with a standardized
    /// card layout that uses the pre-formatted HTML string containing all contacts.
    ///
    /// This migration uses REGEXP_REPLACE to handle whitespace variations across templates.
    /// </summary>
    public partial class Phase6A133Email_UpdateTemplatesMultiOrganizerContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Auto-generated reference data timestamp updates
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6521));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6687));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6461));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6640));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6658));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6787));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6618));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6497));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6751));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6733));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6708));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 23, 10, 25, 36, DateTimeKind.Utc).AddTicks(6768));

            // ============================================================
            // Phase 6A.133 Email: Update templates for multi-organizer contacts
            // ============================================================

            // The new organizer contact section uses:
            // - {{OrganizerContactHeader}} for dynamic "EVENT ORGANIZER" or "EVENT ORGANIZERS" text
            // - {{{OrganizerContactsHtml}}} (triple-brace) for pre-formatted HTML with all contacts
            // - Keeps {{#if HasOrganizerContact}} conditional to hide section when no contacts exist
            var newOrganizerSection = @"{{#if HasOrganizerContact}}" +
                @"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">" +
                @"<tr><td style=""background: #fefaf7; padding: 20px; border-radius: 8px; border: 1px solid #f3e4d5;"">" +
                @"<p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 700; color: #9f1239; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>" +
                @"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">" +
                @"{{{OrganizerContactsHtml}}}" +
                @"</table>" +
                @"</td></tr></table>" +
                @"{{/if}}";

            // Escape single quotes for SQL
            var sqlNewSection = newOrganizerSection.Replace("'", "''");

            // Replace legacy syntax: {{#HasOrganizerContact}}...{{/HasOrganizerContact}}
            // Uses 'gs' flags: g=global, s=dot matches newlines
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{{\{{#HasOrganizerContact\}}\}}.*?\{{\{{/HasOrganizerContact\}}\}}',
                    '{sqlNewSection}',
                    'gs'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%HasOrganizerContact%';
            ");

            // Also replace {{#if HasOrganizerContact}}...{{/if}} syntax if any templates use it
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{{\{{#if HasOrganizerContact\}}\}}.*?\{{\{{/if\}}\}}',
                    '{sqlNewSection}',
                    'gs'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%#if HasOrganizerContact%';
            ");

            // Also update text_template to use the new placeholders
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET text_template = REGEXP_REPLACE(
                    text_template,
                    '\{\{#HasOrganizerContact\}\}.*?\{\{/HasOrganizerContact\}\}',
                    '{{#if HasOrganizerContact}}' || E'\n' || '{{OrganizerContactHeader}}' || E'\n' || '{{{OrganizerContactsHtml}}}' || E'\n' || '{{/if}}',
                    'gs'
                ),
                updated_at = NOW()
                WHERE text_template LIKE '%HasOrganizerContact%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Auto-generated reference data timestamp rollbacks
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6635));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6700));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6578));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6668));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6684));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6845));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6652));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6616));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6816));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6798));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6728));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 7, 22, 19, 59, 807, DateTimeKind.Utc).AddTicks(6830));

            // Revert to single-contact section
            var oldOrganizerSection = @"{{#HasOrganizerContact}}" +
                @"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">" +
                @"<tr><td style=""background: #fefaf7; padding: 20px; border-radius: 8px; border: 1px solid #f3e4d5;"">" +
                @"<p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 700; color: #9f1239; text-transform: uppercase; letter-spacing: 0.5px;"">EVENT ORGANIZER</p>" +
                @"<p style=""margin: 0; font-size: 15px; color: #2d3748;""><strong>{{OrganizerContactName}}</strong></p>" +
                @"{{#if OrganizerContactEmail}}<p style=""margin: 4px 0 0 0; font-size: 14px;""><a href=""mailto:{{OrganizerContactEmail}}"" style=""color: #9f1239; text-decoration: none;"">{{OrganizerContactEmail}}</a></p>{{/if}}" +
                @"{{#if OrganizerContactPhone}}<p style=""margin: 4px 0 0 0; font-size: 14px; color: #4a5568;"">{{OrganizerContactPhone}}</p>{{/if}}" +
                @"</td></tr></table>" +
                @"{{/HasOrganizerContact}}";

            var sqlOldSection = oldOrganizerSection.Replace("'", "''");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{{\{{#if HasOrganizerContact\}}\}}.*?\{{\{{/if\}}\}}',
                    '{sqlOldSection}',
                    'gs'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%OrganizerContactsHtml%';
            ");
        }
    }
}
