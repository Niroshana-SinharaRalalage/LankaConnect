using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A63Fix6_UpdateCancellationFooterWithLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.63 FIX 6: Update cancellation email footer to match registration email footer with logo
            // Replace plain text footer with branded footer containing logo image, matching registration template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '                    <!-- Footer -->
                    <tr>
                        <td style=""background: #f9f9f9; padding: 25px 30px; border-top: 1px solid #e0e0e0; text-align: center;"">
                            <p style=""margin: 0 0 10px 0; font-size: 14px; color: #999;"">
                                LankaConnect - Connecting Sri Lankan Communities Worldwide
                            </p>
                            <p style=""margin: 0; font-size: 12px; color: #aaa;"">
                                <a href=""{{UnsubscribeUrl}}"" style=""color: #8B1538; text-decoration: none;"">Unsubscribe</a> from event notifications
                            </p>
                        </td>
                    </tr>',
                    '                    <!-- Footer with gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <!-- Top decoration -->
                                <tr>
                                    <td align=""center"" style=""padding: 15px 0 10px 0; font-size: 12px; letter-spacing: 6px; color: rgba(255,255,255,0.2);"">
                                        ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦ ✦
                                    </td>
                                </tr>
                                <!-- Logo -->
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0;"">
                                        <img src=""https://lankaconnectstrgaccount.blob.core.windows.net/assets/lankaconnect-logo.png"" alt=""LankaConnect"" width=""70"" height=""70"" style=""width: 70px; height: 70px; border-radius: 50%; background: white; padding: 5px; display: block;"">
                                    </td>
                                </tr>
                                <!-- Brand name -->
                                <tr>
                                    <td align=""center"" style=""padding: 8px 0 0 0;"">
                                        <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0;"">LankaConnect</p>
                                    </td>
                                </tr>
                                <!-- Tagline -->
                                <tr>
                                    <td align=""center"" style=""padding: 5px 0;"">
                                        <p style=""color: rgba(255,255,255,0.9); font-size: 13px; margin: 0;"">Sri Lankan Community Hub</p>
                                    </td>
                                </tr>
                                <!-- Copyright -->
                                <tr>
                                    <td align=""center"" style=""padding: 15px 0 20px 0;"">
                                        <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>'
                )
                WHERE name = 'event-cancelled-notification';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5701));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5834));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5530));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5768));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5801));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(6000));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5735));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5631));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5934));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5903));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5869));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 5, 10, 473, DateTimeKind.Utc).AddTicks(5967));
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
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8484));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8650));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8372));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8567));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8610));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8864));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8525));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8439));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8786));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8748));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8690));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 3, 8, 48, 315, DateTimeKind.Utc).AddTicks(8825));
        }
    }
}
