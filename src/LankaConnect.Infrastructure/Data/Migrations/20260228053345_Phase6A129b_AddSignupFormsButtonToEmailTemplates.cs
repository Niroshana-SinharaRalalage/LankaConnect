using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.129b: Add styled "View Signup Forms" button to email templates
    ///
    /// Root Cause: Phase6A113 migration used File.ReadAllText() to load template HTML from disk.
    /// This approach is fragile and may have failed silently, leaving database templates
    /// WITHOUT the {{#HasSignupForms}} conditional block. Even if it did apply, the button
    /// was only a simple text link — not a styled button matching the {{#HasSignUpLists}} design.
    ///
    /// Fix: Use inline SQL (not file-based) to reliably add a properly styled
    /// "View Signup Forms" button to ALL templates that have a "View Signup List" button.
    ///
    /// This migration is IDEMPOTENT:
    /// - Step 1 removes any existing simple HasSignupForms block (from Phase6A113)
    /// - Step 2 adds the styled version only if HasSignupForms is not present
    ///
    /// Templates affected: All templates containing {{#HasSignUpLists}} conditional block
    /// </summary>
    public partial class Phase6A129b_AddSignupFormsButtonToEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Remove any existing simple-style {{#HasSignupForms}} blocks
            // Phase6A113 may have added a simple <p> text link version that's visually
            // inconsistent with the styled {{#HasSignUpLists}} button.
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{\{#HasSignupForms\}\}[\s\S]*?\{\{/HasSignupForms\}\}',
                    ''
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%HasSignupForms%';
            ");

            // Step 2: Add properly styled signup forms button after {{/HasSignUpLists}}
            // Button styling matches the existing {{#HasSignUpLists}} button exactly:
            // - Table-based layout for email client compatibility
            // - MSO VML roundrect for Outlook
            // - Styled <a> tag for non-MSO clients
            // - Orange border (#ea580c), white background, rounded corners
            //
            // Only updates templates that have HasSignUpLists but NOT HasSignupForms
            // (idempotent — safe to re-run)
            var signupFormsButtonHtml = GetSignupFormsButtonHtml();
            var escapedHtml = signupFormsButtonHtml.Replace("'", "''");

            migrationBuilder.Sql(
                "UPDATE communications.email_templates " +
                "SET html_template = REPLACE(" +
                "    html_template, " +
                "    '{{/HasSignUpLists}}', " +
                "    '{{/HasSignUpLists}}" + escapedHtml + "'" +
                "), " +
                "updated_at = NOW() " +
                "WHERE html_template LIKE '%HasSignUpLists%' " +
                "AND html_template NOT LIKE '%HasSignupForms%';"
            );

            // Auto-generated reference_values timestamp updates
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5040));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5192));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(4939));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5105));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5137));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5516));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5072));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5004));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5454));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5416));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5357));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 28, 5, 33, 39, 149, DateTimeKind.Utc).AddTicks(5486));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the signup forms button block from all templates
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{\{#HasSignupForms\}\}[\s\S]*?\{\{/HasSignupForms\}\}',
                    ''
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%HasSignupForms%';
            ");

            // Auto-generated reference_values timestamp rollbacks
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8728));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8801));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8677));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8759));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8776));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8952));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8743));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8710));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8917));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8838));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8818));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 27, 5, 47, 58, 622, DateTimeKind.Utc).AddTicks(8937));
        }

        /// <summary>
        /// Returns the styled "View Signup Forms" button HTML block.
        /// Matches the {{#HasSignUpLists}} button design exactly.
        /// </summary>
        private static string GetSignupFormsButtonHtml()
        {
            return @"
{{#HasSignupForms}}
<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 8px 0 20px"">
<tr>
<td align=""center"" style=""padding: 8px 0 0"">
This event has signup forms available
</td>
</tr>
<tr>
<td align=""center"" style=""padding: 8px 0 0"">
<!--[if mso]>
<v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href=""{{SignupFormsUrl}}"" style=""height:48px;v-text-anchor:middle;width:260px;"" arcsize=""20%"" stroke=""true"" strokecolor=""#ea580c"" strokeweight=""2px"">
<v:fill type=""solid"" color=""#ffffff""/>
<w:anchorlock/>
<center style=""color:#ea580c;font-family:&quot;Segoe UI&quot;,Arial,sans-serif;font-size:15px;font-weight:bold;"">View Signup Forms</center>
</v:roundrect>
<![endif]-->
<!--[if !mso]><!-->
<a href=""{{SignupFormsUrl}}"" style=""display:inline-block;background:#ffffff;color:#ea580c;text-decoration:none;font-family:&quot;Segoe UI&quot;,Arial,sans-serif;font-weight:600;font-size:15px;padding:11px 44px;border-radius:10px;border:2px solid #ea580c;letter-spacing:0.2px;min-width:200px;text-align:center;"">View Signup Forms</a>
<!--<![endif]-->
</td>
</tr>
</table>
{{/HasSignupForms}}";
        }
    }
}
