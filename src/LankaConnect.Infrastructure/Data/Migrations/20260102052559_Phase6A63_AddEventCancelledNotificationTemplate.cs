using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A63_AddEventCancelledNotificationTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.63: Add event-cancelled-notification email template
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""Id"", ""name"", ""type"", ""description"", ""subject_template"", ""html_template"", ""text_template"", ""created_at"", ""updated_at"", ""category"", ""is_active"")
                VALUES (
                    gen_random_uuid(),
                    'event-cancelled-notification',
                    'transactional',
                    'Event cancellation notification - Sent to all recipients (registrations, email groups, newsletter subscribers) when organizer cancels event',
                    'Event Cancelled: {{EventTitle}} - LankaConnect',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Event Cancelled - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Event Cancelled</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Dear LankaConnect Community,</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                We regret to inform you that the following event has been <strong>cancelled</strong>:
                            </p>

                            <!-- Event Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #fff8f5; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <h2 style=""margin: 0 0 15px 0; color: #8B1538; font-size: 20px;"">{{EventTitle}}</h2>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>📅 Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>📍 Location:</strong> {{EventLocation}}</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Cancellation Reason -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #fef2f2; border-left: 4px solid #DC2626; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #DC2626; font-size: 14px;"">Cancellation Reason:</p>
                                        <p style=""margin: 0; color: #666; font-size: 14px; line-height: 1.6;"">{{CancellationReason}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; color: #555; line-height: 1.6;"">
                                We apologize for any inconvenience this may cause. If you had registered for this event, your registration has been automatically cancelled.
                            </p>

                            <p style=""margin: 15px 0 0 0; color: #555; line-height: 1.6;"">
                                Thank you for your understanding.
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
                    'Dear LankaConnect Community,

We regret to inform you that the following event has been CANCELLED:

EVENT: {{EventTitle}}
DATE: {{EventStartDate}} at {{EventStartTime}}
LOCATION: {{EventLocation}}

CANCELLATION REASON:
{{CancellationReason}}

We apologize for any inconvenience this may cause. If you had registered for this event, your registration has been automatically cancelled.

Thank you for your understanding.

---
LankaConnect
Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.',
                    NOW(),
                    NOW(),
                    'Events',
                    true
                );
            ");

            migrationBuilder.DropCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3828));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3887));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3777));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3859));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3874));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3956));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3843));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3811));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3929));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3916));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3902));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 2, 5, 25, 57, 748, DateTimeKind.Utc).AddTicks(3942));

            migrationBuilder.AddCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations",
                sql: "(\r\n                    (\"UserId\" IS NOT NULL AND attendee_info IS NULL) OR\r\n                    (\"UserId\" IS NULL AND attendee_info IS NOT NULL) OR\r\n                    (attendees IS NOT NULL AND contact IS NOT NULL)\r\n                )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(242));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(350));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(157));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(297));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(324));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(483));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(268));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(215));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(433));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(406));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(377));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2025, 12, 31, 16, 0, 25, 704, DateTimeKind.Utc).AddTicks(458));

            migrationBuilder.AddCheckConstraint(
                name: "ck_registrations_valid_format",
                schema: "events",
                table: "registrations",
                sql: "(\n                    (\"UserId\" IS NOT NULL AND attendee_info IS NULL) OR\n                    (\"UserId\" IS NULL AND attendee_info IS NOT NULL) OR\n                    (attendees IS NOT NULL AND contact IS NOT NULL)\n                )");
        }
    }
}
