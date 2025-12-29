using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.53 Fix #3: Revert to correct email template layout (NO logo)
    /// User confirmed that event registration emails use plain footer without logo image.
    /// This reverts the template to match the working event registration layout.
    /// </summary>
    public partial class Phase6A53Fix3_RevertToCorrectTemplateNoLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Revert member-email-verification template to correct layout (no logo, matches event registration emails)
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    html_template = '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verify Your Email - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient (Clean header - matches event registration) -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Welcome to LankaConnect!</h1>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi <span style=""color: #FF6600; font-weight: bold;"">{{UserName}}</span>,</p>
                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">Thank you for joining LankaConnect, the Sri Lankan Community Hub! To complete your registration and activate your account, please verify your email address.</p>

                            <!-- Verification Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{VerificationUrl}}"" style=""display: inline-block; background: linear-gradient(135deg, #8B1538 0%, #FF6600 100%); color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 6px rgba(139, 21, 56, 0.3);"">
                                            Verify Email Address
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Important Info Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: #fff8f5; padding: 20px; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0;"">
                                        <p style=""margin: 0 0 10px 0; font-size: 14px; color: #666;""><strong style=""color: #8B1538;"">⏰ This link expires in {{ExpirationHours}} hours</strong></p>
                                        <p style=""margin: 0; font-size: 14px; color: #666;"">For your security, please verify your email soon.</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; font-size: 14px; color: #999; line-height: 1.6;"">If you didn''t create this account, please ignore this email. No further action is required.</p>
                        </td>
                    </tr>
                    <!-- Footer with Brand Gradient (Clean footer - NO decorative stars, NO logo) -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
                            <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;"">Sri Lankan Community Hub</p>
                            <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    description = 'Email verification for new member signups with secure token - Phase 6A.53 (Correct layout, no logo)',
                    updated_at = NOW()
                WHERE name = 'member-email-verification';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1351));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1512));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1245));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1419));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1454));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1712));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1382));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1313));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1628));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1594));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1548));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 23, 17, 37, 491, DateTimeKind.Utc).AddTicks(1658));
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
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9384));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9630));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9136));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9514));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9572));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9923));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9448));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9312));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9807));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9748));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9689));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 21, 8, 17, 475, DateTimeKind.Utc).AddTicks(9865));
        }
    }
}
