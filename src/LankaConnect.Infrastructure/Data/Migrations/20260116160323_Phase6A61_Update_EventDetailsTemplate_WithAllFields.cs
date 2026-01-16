using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.61+: Insert/Update event-details email template with ALL fields from event-published
    /// This ensures manual event notifications have the same rich template as automatic published notifications
    /// </summary>
    public partial class Phase6A61_Update_EventDetailsTemplate_WithAllFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.61+: Insert or update event-details template with rich HTML from event-published
            // Now includes: EventDescription, EventStartDate, EventStartTime, EventCity, EventState, EventUrl, IsFree, IsPaid, TicketPrice
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                (
                    ""id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at"",
                    ""updated_at""
                )
                VALUES
                (
                    gen_random_uuid(),
                    'event-details',
                    'Manual event notification template with rich HTML - includes all fields from event-published for consistency',
                    'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}',
                    'NEW EVENT ANNOUNCEMENT
======================

{{EventTitle}}

{{EventDescription}}

EVENT DETAILS
-------------
Date: {{EventStartDate}} at {{EventStartTime}}
Location: {{EventLocation}}
{{#IsFree}}Admission: FREE{{/IsFree}}
{{#IsPaid}}Ticket Price: {{TicketPrice}}{{/IsPaid}}

View full event details and register:
{{EventUrl}}

{{#HasOrganizerContact}}
Organizer: {{OrganizerName}}
{{#OrganizerEmail}}Email: {{OrganizerEmail}}{{/OrganizerEmail}}
{{#OrganizerPhone}}Phone: {{OrganizerPhone}}{{/OrganizerPhone}}
{{/HasOrganizerContact}}

{{#HasSignUpLists}}
View Sign-Up Lists: {{SignUpListsUrl}}
{{/HasSignUpLists}}

---
LankaConnect - Sri Lankan Community Hub
(c) 2026 LankaConnect',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Event Details</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
        <!-- Header with Sri Lankan gradient -->
        <tr>
            <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); color: white; padding: 30px 20px; text-align: center;"">
                <h1 style=""margin: 0; font-size: 24px; font-weight: 600;"">{{EventTitle}}</h1>
            </td>
        </tr>

        <!-- Content -->
        <tr>
            <td style=""padding: 30px 20px;"">
                <p style=""color: #475569; margin: 0 0 25px 0; line-height: 1.7;"">{{EventDescription}}</p>

                <!-- Event Details Box -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background: #f8fafc; padding: 20px; margin: 20px 0; border-left: 4px solid #FF6600; border-radius: 4px;"">
                    <tr>
                        <td>
                            <h3 style=""margin: 0 0 15px 0; font-size: 16px; color: #1e293b; font-weight: 600;"">Event Details</h3>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""5"">
                                <tr>
                                    <td style=""font-weight: 600; color: #475569; width: 120px; vertical-align: top;"">Date:</td>
                                    <td style=""color: #1e293b;"">{{EventStartDate}} at {{EventStartTime}}</td>
                                </tr>
                                <tr>
                                    <td style=""font-weight: 600; color: #475569; vertical-align: top;"">Location:</td>
                                    <td style=""color: #1e293b;"">{{EventLocation}}</td>
                                </tr>
                                <tr>
                                    <td style=""font-weight: 600; color: #475569; vertical-align: top;"">Admission:</td>
                                    <td style=""color: #1e293b;"">
                                        {{#IsFree}}<span style=""display: inline-block; padding: 6px 12px; border-radius: 4px; font-weight: 600; font-size: 14px; background-color: #10b981; color: white;"">FREE</span>{{/IsFree}}
                                        {{#IsPaid}}<span style=""display: inline-block; padding: 6px 12px; border-radius: 4px; font-weight: 600; font-size: 14px; background-color: #f59e0b; color: white;"">{{TicketPrice}}</span>{{/IsPaid}}
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <!-- CTA Button -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                    <tr>
                        <td style=""text-align: center; padding: 25px 0 15px 0;"">
                            <a href=""{{EventUrl}}"" style=""display: inline-block; background: #FF6600; color: white; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 16px;"">View Event &amp; Register</a>
                        </td>
                    </tr>
                </table>

                {{#HasSignUpLists}}
                <p style=""text-align: center; margin: 20px 0;"">
                    <a href=""{{SignUpListsUrl}}"" style=""color: #FF6600; text-decoration: underline;"">View Sign-Up Lists</a>
                </p>
                {{/HasSignUpLists}}

                {{#HasOrganizerContact}}
                <div style=""border-top: 1px solid #E5E7EB; padding-top: 20px; margin-top: 30px;"">
                    <p style=""color: #1F2937; margin: 8px 0;""><strong>Organizer:</strong> {{OrganizerName}}</p>
                    {{#OrganizerEmail}}<p style=""color: #4B5563; margin: 8px 0;"">📧 {{OrganizerEmail}}</p>{{/OrganizerEmail}}
                    {{#OrganizerPhone}}<p style=""color: #4B5563; margin: 8px 0;"">📱 {{OrganizerPhone}}</p>{{/OrganizerPhone}}
                </div>
                {{/HasOrganizerContact}}
            </td>
        </tr>

        <!-- Footer -->
        <tr>
            <td style=""text-align: center; padding: 25px 20px; background: #f8fafc; color: #64748b; font-size: 13px; line-height: 1.8;"">
                <p style=""margin: 0 0 8px 0;"">
                    You''re receiving this email because you''re part of the LankaConnect community in <strong>{{EventCity}}, {{EventState}}</strong>.
                </p>
                <p style=""margin: 0;"">
                    &copy; 2026 LankaConnect - Sri Lankan Community Hub
                </p>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'Transactional',
                    'Events',
                    true,
                    NOW(),
                    NOW()
                )
                ON CONFLICT (""name"") DO UPDATE SET
                    ""description"" = EXCLUDED.""description"",
                    ""subject_template"" = EXCLUDED.""subject_template"",
                    ""text_template"" = EXCLUDED.""text_template"",
                    ""html_template"" = EXCLUDED.""html_template"",
                    ""type"" = EXCLUDED.""type"",
                    ""category"" = EXCLUDED.""category"",
                    ""is_active"" = EXCLUDED.""is_active"",
                    ""updated_at"" = NOW();
            ");


            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1068));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1161));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1002));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1114));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1140));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1262));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1091));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1035));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1222));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1202));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1182));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1242));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.61+: Remove the template if rolling back
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE ""name"" = 'event-details';
            ");


            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4653));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4788));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4507));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4721));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4756));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4978));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4686));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4612));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4885));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4854));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4821));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 23, 52, 10, 24, DateTimeKind.Utc).AddTicks(4917));
        }
    }
}
