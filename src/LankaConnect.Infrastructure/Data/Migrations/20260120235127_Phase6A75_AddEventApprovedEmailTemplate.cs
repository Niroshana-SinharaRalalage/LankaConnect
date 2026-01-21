using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A75_AddEventApprovedEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.75: Add event-approved email template with LankaConnect branding
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'event-approved',
                    'Email sent to event organizers when their event is approved for publication',
                    'Great News! Your Event ""{{EventTitle}}"" Has Been Approved',
                    'Hi {{OrganizerName}},

Great news! Your event has been approved and is now published on LankaConnect.

EVENT DETAILS
-------------
Event: {{EventTitle}}
Date: {{EventStartDate}} at {{EventStartTime}}
Location: {{EventLocation}}
Approved On: {{ApprovedAt}}

WHAT''S NEXT?
- Your event is now visible to all users
- Attendees can start registering
- You can manage your event from your dashboard

VIEW YOUR EVENT
---------------
{{EventUrl}}

MANAGE YOUR EVENT
-----------------
{{EventManageUrl}}

Thank you for using LankaConnect to connect with the Sri Lankan community!

---
LankaConnect
Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Event Approved - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Event Approved!</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi {{OrganizerName}},</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                Great news! Your event has been <strong style=""color: #10b981;"">approved</strong> and is now published on LankaConnect.
                            </p>

                            <!-- Event Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #f0fdf4; border-left: 4px solid #10b981; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <h2 style=""margin: 0 0 15px 0; color: #065f46; font-size: 20px;"">{{EventTitle}}</h2>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Location:</strong> {{EventLocation}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Approved:</strong> {{ApprovedAt}}</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- What''s Next Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #1e40af; font-size: 14px;"">What''s Next?</p>
                                        <ul style=""margin: 0; padding-left: 20px; color: #666; font-size: 14px; line-height: 1.8;"">
                                            <li>Your event is now visible to all users</li>
                                            <li>Attendees can start registering</li>
                                            <li>You can manage your event from your dashboard</li>
                                        </ul>
                                    </td>
                                </tr>
                            </table>

                            <!-- CTA Buttons -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""background: #FF6600; border-radius: 6px; text-align: center;"">
                                                    <a href=""{{EventUrl}}"" style=""display: inline-block; padding: 14px 28px; color: white; text-decoration: none; font-weight: bold; font-size: 14px;"">View Event</a>
                                                </td>
                                                <td width=""15""></td>
                                                <td style=""background: #8B1538; border-radius: 6px; text-align: center;"">
                                                    <a href=""{{EventManageUrl}}"" style=""display: inline-block; padding: 14px 28px; color: white; text-decoration: none; font-weight: bold; font-size: 14px;"">Manage Event</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; color: #555; line-height: 1.6;"">
                                Thank you for using LankaConnect to connect with the Sri Lankan community!
                            </p>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
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
                    'transactional',
                    'System',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'event-approved'
                );
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(7828));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(7952));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(7699));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(7892));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(7923));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(8102));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(7859));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(7795));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(8043));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(8015));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(7984));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 23, 51, 23, 889, DateTimeKind.Utc).AddTicks(8073));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.75: Remove event-approved email template
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'event-approved';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7642));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7792));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7585));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7756));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7773));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7877));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7737));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7622));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7845));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7828));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7812));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 20, 21, 47, 23, 659, DateTimeKind.Utc).AddTicks(7862));
        }
    }
}
