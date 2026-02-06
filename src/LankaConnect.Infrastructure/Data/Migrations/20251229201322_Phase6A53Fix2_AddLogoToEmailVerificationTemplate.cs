using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A53Fix2_AddLogoToEmailVerificationTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.53 Fix #2: Add logo to email verification template footer
            // Matching the registration-confirmation template layout from Phase 6A.34
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
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background: #ffffff; border-radius: 12px;"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 10px 0 5px 0; font-size: 14px; letter-spacing: 8px; color: rgba(255,255,255,0.25);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 15px 25px; color: white;"">
                                        <h1 style=""margin: 0; font-size: 28px; font-weight: bold;"">Welcome to LankaConnect!</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0 10px 0; font-size: 14px; letter-spacing: 8px; color: rgba(255,255,255,0.25);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <p>Hi <span style=""color: #FF6600; font-weight: bold;"">{{UserName}}</span>,</p>
                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">Thank you for joining LankaConnect, the Sri Lankan Community Hub! To complete your registration and activate your account, please verify your email address.</p>
                            <!-- Verification Button -->
                            <div style=""text-align: center; margin: 30px 0;"">
                                <a href=""{{VerificationUrl}}"" style=""display: inline-block; background: linear-gradient(135deg, #8B1538 0%, #FF6600 100%); color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 8px rgba(0,0,0,0.2);"">
                                    Verify Email Address
                                </a>
                            </div>
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
                    <!-- Footer with Brand Gradient and Logo -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td align=""center"" style=""padding: 15px 0 10px 0; font-size: 12px; letter-spacing: 6px; color: rgba(255,255,255,0.2);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0;"">
                                        <img src=""https://lankaconnectstrgaccount.blob.core.windows.net/assets/lankaconnect-logo.png"" alt=""LankaConnect"" width=""70"" height=""70"" style=""width: 70px; height: 70px; border-radius: 50%; background: white; padding: 5px; display: block;"">
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 8px 0;"">
                                        <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0;"">LankaConnect</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0;"">
                                        <p style=""color: rgba(255,255,255,0.9); font-size: 13px; margin: 0;"">Sri Lankan Community Hub</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td align=""center"" style=""padding: 15px 0 20px 0;"">
                                        <p style=""color: rgba(255,255,255,0.7); font-size: 11px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
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
</html>',
                    description = 'Email verification for new member signups with secure token - Phase 6A.53 (Enhanced layout with logo)',
                    updated_at = NOW()
                WHERE name = 'member-email-verification';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("030ea891-4551-d7a4-a8dc-7b6aae04a1a3"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5835));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("09e43bc5-e08a-d3d7-ad60-8038af9b6d29"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5918));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5322));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("25c65b18-5a29-02bf-ccc7-8abdc92bcf36"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(6282));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5480));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5220));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("39052dc5-732b-c91e-5d2f-2712825ffd67"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(6429));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5407));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5439));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("51dd102d-1284-794a-2ce1-c94a668c123f"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5691));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6246f6e6-122b-8a19-e19d-cdd59ef66b49"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(6007));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5360));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("7f3aed08-471c-9621-ae43-077e05a24f53"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(6102));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5272));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("996e43b0-1aaa-4958-a81e-8fe5a7dade49"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5867));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9a393a97-dce0-972e-266a-c7d0c87e6fbe"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(6148));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c028c08b-c7dc-333e-a756-2e0e0f49da15"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5658));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5527));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f16f3460-bdd0-053d-481c-1947ad5c6f77"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5979));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f54f2798-7e29-ae36-27fa-332729049b7a"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(6387));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f5a75980-4ea7-8af7-fab6-293538bca4fd"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(5947));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f7fb53b7-7def-8e63-6fa5-8beeb39b8fff"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 20, 13, 17, 770, DateTimeKind.Utc).AddTicks(6342));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("030ea891-4551-d7a4-a8dc-7b6aae04a1a3"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9609));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("09e43bc5-e08a-d3d7-ad60-8038af9b6d29"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9637));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9431));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("25c65b18-5a29-02bf-ccc7-8abdc92bcf36"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9787));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9491));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9385));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("39052dc5-732b-c91e-5d2f-2712825ffd67"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9860));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9465));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9477));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("51dd102d-1284-794a-2ce1-c94a668c123f"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9595));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6246f6e6-122b-8a19-e19d-cdd59ef66b49"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9684));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9447));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("7f3aed08-471c-9621-ae43-077e05a24f53"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9730));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9415));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("996e43b0-1aaa-4958-a81e-8fe5a7dade49"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9623));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9a393a97-dce0-972e-266a-c7d0c87e6fbe"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9762));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c028c08b-c7dc-333e-a756-2e0e0f49da15"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9579));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9513));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f16f3460-bdd0-053d-481c-1947ad5c6f77"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9670));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f54f2798-7e29-ae36-27fa-332729049b7a"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9837));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f5a75980-4ea7-8af7-fab6-293538bca4fd"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9651));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("f7fb53b7-7def-8e63-6fa5-8beeb39b8fff"),
                column: "created_at",
                value: new DateTime(2025, 12, 29, 18, 34, 37, 328, DateTimeKind.Utc).AddTicks(9814));
        }
    }
}
