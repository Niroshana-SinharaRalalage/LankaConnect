using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.148.W4.D13 (G2 fix): Adds the dedicated <c>template-refund-withdrawn</c>
    /// email template. Fires once when an attendee uses the in-app Withdraw button on
    /// the pending-review status banner — confirms to them that the request was withdrawn,
    /// the registration is back to Confirmed, and no money moved. Closes the silent gap
    /// surfaced in Wave 4 plan G2 (RefundRequestWithdrawnEvent had zero subscribers).
    ///
    /// Idempotency: INSERT uses <c>WHERE NOT EXISTS</c> so re-running on an environment
    /// that already has the row is a no-op (same pattern as Phase6A148D7 + Phase6A137B2).
    ///
    /// The UpdateData calls below for reference_data.reference_values are snapshot-drift
    /// bookkeeping inserted by `dotnet ef migrations add` — same convention as prior
    /// migrations Phase6A148, Phase6A148b, Phase6A148D7, Phase6A151.
    /// </summary>
    public partial class Phase6A148W4D13_AddRefundWithdrawnTemplate : Migration
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
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1032));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1155));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(914));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1093));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1123));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1462));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1063));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(993));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1244));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1216));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1186));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 21, 24, 37, 938, DateTimeKind.Utc).AddTicks(1411));

            // ─────────────────────────────────────────────────────────────────
            // 6A.148.W4.D13: template-refund-withdrawn (new lifecycle stage)
            // ─────────────────────────────────────────────────────────────────

            var withdrawnHtml = GetStandardTemplate(
                "Refund Request Withdrawn",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">You have withdrawn your refund request for <strong>{{EventTitle}}</strong>. <strong>No money has been refunded</strong> — your original payment remains in place, and your registration is back to Confirmed.</p>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f3f4f6; padding: 24px; border-radius: 12px; border-left: 4px solid #6b7280;"">
                            <p style=""margin: 0 0 12px 0; font-size: 13px; font-weight: 600; color: #4b5563; text-transform: uppercase; letter-spacing: 0.5px;"">Withdrawn Items</p>
                            {{{LineItemsHtml}}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 16px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Total Withdrawn:</td>
                                    <td style=""padding: 6px 0; font-size: 18px; font-weight: 700; color: #4b5563; text-align: right;"">{{Currency}} ${{RequestedTotal}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Withdrawn At:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; text-align: right;"">{{WithdrawnAt}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    If you change your mind, you can submit a new refund request from the event page. Note that the organizer can still approve or decline any new request.
                </p>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #6b7280, #374151); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                {{#if HasOrganizerContact}}
                <p style=""margin: 28px 0 12px 0; font-size: 13px; font-weight: 600; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>
                {{{OrganizerContactsHtml}}}
                {{/if}}

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Questions? Contact <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.
                </p>");

            migrationBuilder.Sql($@"
                INSERT INTO communications.email_templates
                (
                    ""Id"", ""name"", ""description"",
                    ""subject_template"", ""text_template"", ""html_template"",
                    ""type"", ""category"", ""is_active"", ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-refund-withdrawn',
                    'Phase 6A.148.W4.D13: Sent once when an attendee withdraws their own pending refund request. Confirms the request was withdrawn, registration back to Confirmed, no money moved.',
                    'Refund Request Withdrawn — {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

You have withdrawn your refund request for {{{{EventTitle}}}}.

NO MONEY HAS BEEN REFUNDED — your original payment remains in place, and your registration is back to Confirmed.

WITHDRAWN ITEMS
---------------
(See itemized list in the HTML version)
Total Withdrawn: {{{{Currency}}}} ${{{{RequestedTotal}}}}
Withdrawn At: {{{{WithdrawnAt}}}}

If you change your mind, you can submit a new refund request from the event page.

View event details: {{{{EventDetailsUrl}}}}

Questions? Contact {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(withdrawnHtml)}',
                    'RefundWithdrawn',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-refund-withdrawn'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the template first so Down() leaves a clean slate
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'template-refund-withdrawn';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4075));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4210));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(3969));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4137));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4164));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4320));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4108));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4043));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4286));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4269));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4236));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 19, 18, 45, 14, 387, DateTimeKind.Utc).AddTicks(4302));
        }

        /// <summary>
        /// Standard 850px-wide email shell — same as Phase6A148D7 + Phase6A137B2 for visual
        /// consistency across all refund emails (only the header colour + title differ).
        /// </summary>
        private string GetStandardTemplate(string headerTitle, string contentHtml)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>LankaConnect</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f3f4f6;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f3f4f6;"">
        <tr>
            <td align=""center"" style=""padding: 20px 10px;"">
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width: 100%; max-width: 850px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 35px 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                                        <span style=""font-size: 24px; font-weight: 500; color: #ffffff;"">{headerTitle}</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 35px 40px;"">
                            {contentHtml}
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 28px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""text-align: center; padding-bottom: 4px;"">
                                                    <span style=""font-size: 24px; font-weight: 400; color: #ffffff; letter-spacing: 0.5px;"">LankaConnect</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""text-align: center;"">
                                                    <span style=""font-size: 13px; font-weight: 400; color: #ffffff; opacity: 0.9;"">Sri Lankan Community Hub</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string EscapeSql(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
