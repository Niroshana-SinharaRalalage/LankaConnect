using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedEventReminderTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Production Fix: Seed the event-reminder email template required by EventReminderJob
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'event-reminder';
            ");
        }
    }
}