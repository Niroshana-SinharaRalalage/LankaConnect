using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.75: Add event-reminder email template to database.
    /// This template is used by EventReminderJob to send 7-day, 2-day, and 1-day reminders
    /// to event attendees before their registered events.
    ///
    /// ROOT CAUSE FIX: The original migration 20260114000000_SeedEventReminderTemplate.cs
    /// was missing its Designer.cs file, so EF Core never applied it to the database.
    /// This migration properly seeds the template with idempotent INSERT.
    /// </summary>
    public partial class Phase6A75_AddEventReminderTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.75: Seed the event-reminder email template required by EventReminderJob
            // Uses idempotent INSERT to prevent duplicates if template already exists
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
                    'event-reminder',
                    'Reminder email sent to attendees before an event (7 days, 2 days, 1 day)',
                    'Reminder: {{EventTitle}} is {{ReminderTimeframe}}!',
                    'Hi {{AttendeeName}},

This is a friendly reminder about your upcoming event!

EVENT DETAILS
--------------
Event: {{EventTitle}}
Date: {{EventStartDate}}
Time: {{EventStartTime}}
Location: {{Location}}
Your Tickets: {{Quantity}}

{{ReminderMessage}}

EVENT DETAILS
-------------
View your event details here: {{EventDetailsUrl}}

{{#HasOrganizerContact}}
ORGANIZER CONTACT
-----------------
Name: {{OrganizerContactName}}
Email: {{OrganizerContactEmail}}
Phone: {{OrganizerContactPhone}}
{{/HasOrganizerContact}}

We look forward to seeing you there!

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #f59e0b; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .event-box { background: white; padding: 20px; margin: 15px 0; border-left: 4px solid #f59e0b; border-radius: 4px; }
        .reminder-message { background: #fef3c7; padding: 15px; margin: 15px 0; border-radius: 4px; text-align: center; font-size: 16px; }
        .cta-button { display: inline-block; background: #f59e0b; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 10px 0; }
        .organizer-box { background: #f3f4f6; padding: 15px; margin: 15px 0; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Event Reminder</h1>
            <p style=""margin: 0; font-size: 18px;"">{{EventTitle}} is {{ReminderTimeframe}}!</p>
        </div>
        <div class=""content"">
            <p>Hi {{AttendeeName}},</p>
            <div class=""reminder-message"">
                <strong>{{ReminderMessage}}</strong>
            </div>
            <div class=""event-box"">
                <h3 style=""margin-top: 0;"">Event Details</h3>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Date:</strong> {{EventStartDate}}</p>
                <p><strong>Time:</strong> {{EventStartTime}}</p>
                <p><strong>Location:</strong> {{Location}}</p>
                <p><strong>Your Tickets:</strong> {{Quantity}}</p>
            </div>
            <p style=""text-align: center;"">
                <a href=""{{EventDetailsUrl}}"" class=""cta-button"">View Event Details</a>
            </p>
            {{#HasOrganizerContact}}
            <div class=""organizer-box"">
                <h3 style=""margin-top: 0;"">Organizer Contact</h3>
                <p><strong>Name:</strong> {{OrganizerContactName}}</p>
                <p><strong>Email:</strong> {{OrganizerContactEmail}}</p>
                <p><strong>Phone:</strong> {{OrganizerContactPhone}}</p>
            </div>
            {{/HasOrganizerContact}}
            <p>We look forward to seeing you there!</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'EventReminder',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates WHERE name = 'event-reminder'
                );
            ");

            // Update reference_values timestamps (auto-generated by EF Core)
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(506));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(618));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(340));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(568));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(593));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(743));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(539));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(404));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(693));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(669));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(645));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 21, 17, 30, 28, 879, DateTimeKind.Utc).AddTicks(718));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the event-reminder template
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'event-reminder';
            ");

            // Revert reference_values timestamps
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
    }
}
