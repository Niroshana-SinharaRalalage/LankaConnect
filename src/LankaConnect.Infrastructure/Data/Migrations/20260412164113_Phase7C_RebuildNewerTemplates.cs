using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 7C — Rebuild 8 newer email templates to match the production-standard Pattern B structure.
    ///
    /// Templates rebuilt (full HTML, not a patch):
    ///   template-donation-receipt, template-addon-purchase-receipt, template-collection-receipt,
    ///   template-sponsor-confirmation, template-donation-refund, template-collection-refund,
    ///   template-sponsor-refund, template-photo-album-published
    ///
    /// All 8 templates are updated to use:
    ///   - Warm beige background (#f3f0ec)
    ///   - 860px gradient outer wrapper with 6px accent stripe
    ///   - White header bar with label
    ///   - 720px inner content with gradient body (#fff→#fef9f5)
    ///   - Warm-tone details cards (#fefaf7, #f3e4d5 border)
    ///   - Production-standard CTA button (135deg gradient + box-shadow + VML fallback)
    ///   - Full footer: logo image + 95px text table (LankaEvents / by LankaConnect) + copyright
    ///   - Media queries for responsive design
    ///
    /// Down() is a no-op for data — restore html_template from DB snapshot if rollback needed.
    /// </summary>
    public partial class Phase7C_RebuildNewerTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Auto-generated reference_data timestamp updates (required by EF Core) ──
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1360));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1417));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1328));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1386));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1397));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1489));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1371));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1347));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1456));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1444));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1429));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 16, 41, 11, 180, DateTimeKind.Utc).AddTicks(1478));

            // ── Template rebuilds: Pattern B production-standard structure + LankaEvents branding ──

            migrationBuilder.Sql(BuildUpdateSql("template-donation-receipt",     "&#10003;&ensp;Donation Receipt",              DonationReceiptContent()));
            migrationBuilder.Sql(BuildUpdateSql("template-addon-purchase-receipt","&#10003;&ensp;Add-On Purchase Confirmed",      AddOnReceiptContent()));
            migrationBuilder.Sql(BuildUpdateSql("template-collection-receipt",   "&#10003;&ensp;Contribution Received",          CollectionReceiptContent()));
            migrationBuilder.Sql(BuildUpdateSql("template-sponsor-confirmation", "&#10003;&ensp;Sponsorship Confirmed",          SponsorConfirmationContent()));
            migrationBuilder.Sql(BuildUpdateSql("template-donation-refund",      "&#8617;&ensp;Donation Refund Confirmed",       DonationRefundContent()));
            migrationBuilder.Sql(BuildUpdateSql("template-collection-refund",    "&#8617;&ensp;Contribution Refund Confirmed",   CollectionRefundContent()));
            migrationBuilder.Sql(BuildUpdateSql("template-sponsor-refund",       "&#8617;&ensp;Sponsorship Refund Confirmed",    SponsorRefundContent()));
            migrationBuilder.Sql(BuildUpdateSql("template-photo-album-published","&#128247;&ensp;Photo Album Available",         PhotoAlbumContent()));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Auto-generated reference_data reversals ──
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1275));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1395));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1234));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1358));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1371));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1460));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1343));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1260));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1436));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1424));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1408));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 12, 2, 52, 27, 873, DateTimeKind.Utc).AddTicks(1448));

            // html_template data changes are not reversible without a DB snapshot.
            // To rollback: restore from a database backup taken before this migration was applied.
            migrationBuilder.Sql("SELECT 1; -- Phase7C_RebuildNewerTemplates Down(): restore html_template from DB snapshot if rollback is needed.");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // SQL builder
        // ─────────────────────────────────────────────────────────────────────────

        private static string BuildUpdateSql(string templateName, string headerLabel, string contentHtml)
        {
            var html = GetWrapper(headerLabel, contentHtml);
            return "UPDATE communications.email_templates " +
                   "SET html_template = $html_template$" + html + "$html_template$, " +
                   "updated_at = NOW() " +
                   "WHERE name = '" + templateName + "';";
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Production-standard wrapper (Pattern B)
        // ─────────────────────────────────────────────────────────────────────────

        private static string GetWrapper(string headerLabel, string contentHtml)
        {
            return @"<!doctype html>
<html xmlns=""http://www.w3.org/1999/xhtml"" lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"" />
    <title>LankaEvents</title>
    <style type=""text/css"">
        @media only screen and (max-width: 900px) {
            .email-container { width: 100% !important; max-width: 100% !important; }
            .email-content   { width: 100% !important; max-width: 100% !important; }
            .email-body-padding   { padding-left: 24px !important; padding-right: 24px !important; }
            .email-footer-padding { padding-left: 28px !important; padding-right: 28px !important; }
        }
        @media only screen and (max-width: 480px) {
            .email-body-padding   { padding-left: 18px !important; padding-right: 18px !important; }
            .email-footer-padding { padding-left: 20px !important; padding-right: 20px !important; }
            .email-card-padding   { padding-left: 16px !important; padding-right: 16px !important; }
            .email-card-header    { padding-left: 16px !important; padding-right: 16px !important; }
        }
    </style>
</head>
<body style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f3f0ec; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f3f0ec"">
        <tr>
            <td align=""center"" style=""padding: 32px 16px"">
                <table role=""presentation"" width=""860"" cellspacing=""0"" cellpadding=""0"" border=""0"" class=""email-container""
                       style=""max-width: 860px; width: 100%; background: linear-gradient(to right, #ea580c, #9f1239, #166534); border-radius: 16px; overflow: hidden; box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);"">

                    <!-- 6px gradient accent stripe -->
                    <tr><td style=""height: 6px; font-size: 0; line-height: 0"">&nbsp;</td></tr>

                    <!-- WHITE HEADER BAR -->
                    <tr>
                        <td style=""background: #ffffff"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 22px 40px 18px"">
                                        <span style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 15px; font-weight: 600; color: #9f1239; letter-spacing: 0.3px;"">HEADER_LABEL_PLACEHOLDER</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- BODY CONTENT -->
                    <tr>
                        <td align=""center"" style=""background: linear-gradient(180deg, #ffffff 0%, #fef9f5 100%)"">
                            <!--[if mso]><table role=""presentation"" width=""720"" align=""center"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td><![endif]-->
                            <table role=""presentation"" width=""720"" cellspacing=""0"" cellpadding=""0"" border=""0"" class=""email-content"" style=""max-width: 720px; width: 100%"">
                                <tr>
                                    <td class=""email-body-padding"" style=""padding: 32px 40px 24px"">
                                        CONTENT_HTML_PLACEHOLDER
                                    </td>
                                </tr>
                            </table>
                            <!--[if mso]></td></tr></table><![endif]-->
                        </td>
                    </tr>

                    <!-- FOOTER -->
                    <tr>
                        <td style=""padding: 0"">
                            <!--[if mso]>
                            <v:rect xmlns:v=""urn:schemas-microsoft-com:vml"" fill=""true"" stroke=""false"" style=""width:860px;"">
                            <v:fill type=""gradient"" color=""#ea580c"" color2=""#166534"" angle=""90""/>
                            <v:textbox inset=""0,20px,0,18px"" style=""mso-fit-shape-to-text:true"">
                            <![endif]-->
                            <div style=""background: linear-gradient(to right, #ea580c, #9f1239, #166534)"">
                                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""
                                       style=""background-image: url(&quot;data:image/svg+xml,%3Csvg width='40' height='40' viewBox='0 0 60 60' xmlns='http://www.w3.org/2000/svg'%3E%3Cg fill='none' fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='0.2'%3E%3Cpath d='M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E&quot;);"">
                                    <tr>
                                        <td align=""center"" class=""email-footer-padding"" style=""padding: 20px 40px 18px"">
                                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                                <tr>
                                                    <td align=""center"" style=""padding-bottom: 14px"">
                                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" align=""center"">
                                                            <tr>
                                                                <td style=""vertical-align: middle"">
                                                                    <!--[if !mso]><!-->
                                                                    <div style=""overflow: hidden; border-radius: 10px; font-size: 0; line-height: 0; max-height: 44px;"">
                                                                        <!--<![endif]-->
                                                                        <img src=""https://lankaconnectstrgaccount.blob.core.windows.net/assets/lankaconnect-logo.png""
                                                                             alt="""" width=""44"" height=""44""
                                                                             style=""width: 44px; height: 44px; border-radius: 10px; display: block;""
                                                                             onerror=""this.style.display='none'; this.parentElement.style.display='none'; this.parentElement.parentElement.style.display='none';"" />
                                                                        <!--[if !mso]><!-->
                                                                    </div>
                                                                    <!--<![endif]-->
                                                                </td>
                                                                <td style=""padding-left: 12px; vertical-align: middle;"">
                                                                    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""95"">
                                                                        <tr>
                                                                            <td width=""95"" style=""width: 95px"">
                                                                                <p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 20px; font-weight: 700; color: #ffffff; margin: 0; line-height: 1.15; letter-spacing: 1px; display: block;"">LankaEvents</p>
                                                                            </td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td width=""95"" style=""width: 95px; padding-top: 3px; text-align: right;"">
                                                                                <p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 9.5px; font-weight: 500; color: rgba(255, 255, 255, 0.85); letter-spacing: 0.9px; margin: 0; line-height: 1; display: block; text-align: right;"">by LankaConnect</p>
                                                                            </td>
                                                                        </tr>
                                                                    </table>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                                <tr>
                                                    <td align=""center"">
                                                        <p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 10px; color: rgba(255, 255, 255, 0.42); margin: 0;"">&#169; {{Year}} LankaEvents. All rights reserved.</p>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <!--[if mso]></v:textbox></v:rect><![endif]-->
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>"
            .Replace("HEADER_LABEL_PLACEHOLDER", headerLabel)
            .Replace("CONTENT_HTML_PLACEHOLDER", contentHtml);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Shared HTML fragments
        // ─────────────────────────────────────────────────────────────────────────

        private static string Greeting(string introText) =>
            @"<p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; margin: 0 0 5px; font-size: 16px; color: #333333; line-height: 1.7;"">Hi <strong style=""color: #ea580c; font-weight: 600"">{{UserName}}</strong>,</p>
<p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; margin: 0 0 20px; font-size: 15px; color: #555555; line-height: 1.7;"">" + introText + @"</p>";

        private static string CardOpen(string cardTitle) =>
            @"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 0 16px"">
    <tr>
        <td style=""background: #fefaf7; border: 1px solid #f3e4d5; border-radius: 12px; overflow: hidden;"">
            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                <tr>
                    <td class=""email-card-header"" style=""padding: 14px 22px 10px; border-bottom: 1px solid #f3e4d5;"">
                        <p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 12px; font-weight: 700; color: #9f1239; margin: 0;"">" + cardTitle + @"</p>
                    </td>
                </tr>
            </table>
            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                <tr>
                    <td class=""email-card-padding"" style=""padding: 16px 22px 18px"">
                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">";

        private static string CardClose() =>
            @"                        </table>
                    </td>
                </tr>
            </table>
        </td>
    </tr>
</table>";

        private static string Row(string label, string value, bool isAmount = false, bool isMono = false)
        {
            var valueStyle = isAmount
                ? @"font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 20px; font-weight: 700; color: #111827; margin: 0;"
                : isMono
                    ? @"font-family: monospace; font-size: 13px; color: #6b7280; margin: 0;"
                    : @"font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 14px; font-weight: 600; color: #111827; margin: 0;";
            return
                @"<tr><td style=""padding: 6px 0""><p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 10px; font-weight: 600; text-transform: uppercase; letter-spacing: 1.2px; color: #9ca3af; margin: 0 0 2px;"">" + label + @"</p><p style=""" + valueStyle + @""">" + value + @"</p></td></tr>";
        }

        private static string CtaButton(string url, string label) =>
            @"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 16px 0 8px"">
    <tr>
        <td align=""center"" style=""padding: 6px 0"">
            <!--[if mso]>
            <v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word""
                         href=""" + url + @""" style=""height: 48px; v-text-anchor: middle; width: 260px;"" arcsize=""20%"" stroke=""false"">
                <v:fill type=""gradient"" color=""#ea580c"" color2=""#9f1239"" angle=""135"" />
                <w:anchorlock />
                <center style=""color: #ffffff; font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 15px; font-weight: bold;"">" + label + @"</center>
            </v:roundrect>
            <![endif]-->
            <!--[if !mso]><!-->
            <a href=""" + url + @""" style=""display: inline-block; background: linear-gradient(135deg, #ea580c 0%, #9f1239 100%); color: #ffffff; text-decoration: none; font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-weight: 600; font-size: 15px; padding: 12px 44px; border-radius: 10px; box-shadow: 0 4px 15px rgba(255, 121, 0, 0.3); letter-spacing: 0.2px; min-width: 200px; text-align: center;"">" + label + @"</a>
            <!--<![endif]-->
        </td>
    </tr>
</table>";

        private static string SupportSignOff() =>
            @"<p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; margin: 16px 0 0; font-size: 14px; color: #6b7280;"">If you have any questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.</p>
<p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; margin: 24px 0 0; font-size: 14px; color: #6b7280;"">Best regards,<br /><strong style=""color: #374151;"">The LankaEvents Team</strong></p>";

        private static string SimpleSignOff() =>
            @"<p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; margin: 24px 0 0; font-size: 14px; color: #6b7280;"">Best regards,<br /><strong style=""color: #374151;"">The LankaEvents Team</strong></p>";

        // ─────────────────────────────────────────────────────────────────────────
        // Template content methods
        // ─────────────────────────────────────────────────────────────────────────

        private static string DonationReceiptContent() =>
            Greeting(@"Thank you for your generous donation to <strong style=""color: #9f1239; font-weight: 600"">{{EventTitle}}</strong>! Your support means a lot to the event organizer and community.") +
            CardOpen("&#128199;&ensp;Donation Receipt") +
            Row("Amount",     "{{DonationCurrency}} ${{DonationAmount}}", isAmount: true) +
            Row("Event",      "{{EventTitle}}") +
            Row("Date",       "{{PaymentDate}}") +
            Row("Reference",  "{{PaymentIntentId}}", isMono: true) +
            CardClose() +
            CtaButton("{{EventDetailsUrl}}", "View Event Details") +
            SupportSignOff();

        private static string AddOnReceiptContent() =>
            Greeting(@"Your add-on purchase for <strong style=""color: #9f1239; font-weight: 600"">{{EventTitle}}</strong> has been confirmed! Here are your purchase details.") +
            CardOpen("&#128199;&ensp;Purchase Receipt") +
            Row("Add-On",     "{{AddOnName}}") +
            Row("Quantity",   "{{Quantity}}") +
            Row("Unit Price", "{{Currency}} ${{UnitPrice}}") +
            Row("Total",      "{{Currency}} ${{TotalAmount}}", isAmount: true) +
            Row("Event",      "{{EventTitle}}") +
            Row("Date",       "{{PaymentDate}}") +
            Row("Reference",  "{{PaymentIntentId}}", isMono: true) +
            CardClose() +
            CtaButton("{{EventDetailsUrl}}", "View Event Details") +
            SupportSignOff();

        private static string CollectionReceiptContent() =>
            Greeting(@"Thank you for your contribution to <strong style=""color: #9f1239; font-weight: 600"">{{EventTitle}}</strong>! Your generosity helps make this event a success.") +
            CardOpen("&#128199;&ensp;Contribution Receipt") +
            Row("Amount",    "{{Currency}} ${{ContributionAmount}}", isAmount: true) +
            Row("Event",     "{{EventTitle}}") +
            Row("Date",      "{{PaymentDate}}") +
            Row("Reference", "{{PaymentIntentId}}", isMono: true) +
            CardClose() +
            CtaButton("{{EventDetailsUrl}}", "View Event Details") +
            SupportSignOff();

        private static string SponsorConfirmationContent() =>
            Greeting(@"Thank you for sponsoring <strong style=""color: #9f1239; font-weight: 600"">{{EventTitle}}</strong>! Your {{SponsorType}} support is greatly appreciated.") +
            @"{{#if HasOrganization}}<p style=""font-family: &quot;Segoe UI&quot;, Arial, sans-serif; font-size: 14px; color: #6b7280; margin: 0 0 16px;"">Organization: <strong style=""color: #374151;"">{{SponsorOrganization}}</strong></p>{{/if}}" +
            @"{{#if IsMonetarySponsor}}" +
            CardOpen("&#128199;&ensp;Sponsorship Receipt") +
            Row("Amount",    "{{Currency}} ${{Amount}}", isAmount: true) +
            Row("Event",     "{{EventTitle}}") +
            Row("Date",      "{{PaymentDate}}") +
            Row("Reference", "{{PaymentIntentId}}", isMono: true) +
            CardClose() +
            @"{{/if}}" +
            @"{{#if IsItemSponsor}}" +
            CardOpen("&#127873;&ensp;Item Sponsorship") +
            Row("Item",  "{{ItemName}}") +
            @"{{#if HasItemDescription}}" + Row("Description", "{{ItemDescription}}") + @"{{/if}}" +
            @"{{#if HasEstimatedValue}}" + Row("Est. Value", "${{EstimatedValue}}") + @"{{/if}}" +
            Row("Event",    "{{EventTitle}}") +
            Row("Recorded", "{{RecordedAt}}") +
            CardClose() +
            @"{{/if}}" +
            CtaButton("{{EventDetailsUrl}}", "View Event Details") +
            SupportSignOff();

        private static string DonationRefundContent() =>
            Greeting(@"Your donation to <strong style=""color: #9f1239; font-weight: 600"">{{EventTitle}}</strong> has been refunded. The refund should appear in your account within 5–10 business days.") +
            CardOpen("&#8617;&ensp;Refund Details") +
            Row("Refund Amount", "{{Currency}} ${{DonationAmount}}", isAmount: true) +
            Row("Event",         "{{EventTitle}}") +
            Row("Refund Date",   "{{RefundedAt}}") +
            Row("Reference",     "{{PaymentIntentId}}", isMono: true) +
            CardClose() +
            CtaButton("{{EventDetailsUrl}}", "View Event Details") +
            SupportSignOff();

        private static string CollectionRefundContent() =>
            Greeting(@"Your contribution to the event fund for <strong style=""color: #9f1239; font-weight: 600"">{{EventTitle}}</strong> has been refunded. The refund should appear within 5–10 business days.") +
            CardOpen("&#8617;&ensp;Refund Details") +
            Row("Refund Amount", "{{Currency}} ${{ContributionAmount}}", isAmount: true) +
            Row("Event",         "{{EventTitle}}") +
            Row("Refund Date",   "{{RefundedAt}}") +
            Row("Reference",     "{{PaymentIntentId}}", isMono: true) +
            CardClose() +
            CtaButton("{{EventDetailsUrl}}", "View Event Details") +
            SupportSignOff();

        private static string SponsorRefundContent() =>
            Greeting(@"Your monetary sponsorship for <strong style=""color: #9f1239; font-weight: 600"">{{EventTitle}}</strong> has been refunded. The refund should appear within 5–10 business days.") +
            CardOpen("&#8617;&ensp;Refund Details") +
            Row("Refund Amount", "{{Currency}} ${{Amount}}", isAmount: true) +
            Row("Event",         "{{EventTitle}}") +
            @"{{#if HasOrganization}}" + Row("Organization", "{{SponsorOrganization}}") + @"{{/if}}" +
            Row("Refund Date",   "{{RefundedAt}}") +
            Row("Reference",     "{{PaymentIntentId}}", isMono: true) +
            CardClose() +
            CtaButton("{{EventDetailsUrl}}", "View Event Details") +
            SupportSignOff();

        private static string PhotoAlbumContent() =>
            Greeting(@"The photo album for <strong style=""color: #9f1239; font-weight: 600"">{{EventTitle}}</strong> is now published! Browse the memories from this event.") +
            CardOpen("&#128247;&ensp;Album Details") +
            Row("Album", "{{AlbumName}}") +
            Row("Event", "{{EventTitle}}") +
            CardClose() +
            CardOpen("&#9888;&ensp;Access Notice") +
            Row("Available For", "{{PhotoExpiryDays}} days") +
            CardClose() +
            CtaButton("{{AlbumUrl}}", "View Photo Album") +
            SimpleSignOff();
    }
}
