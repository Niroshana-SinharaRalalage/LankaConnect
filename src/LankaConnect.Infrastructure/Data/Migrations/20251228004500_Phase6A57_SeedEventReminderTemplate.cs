using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A57_SeedEventReminderTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.57: Seed event-reminder email template into database
            // User requirement: Professional HTML layout matching other templates
            // Send 3 reminders: 7 days, 2 days, 1 day before event

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
                VALUES
                (
                    gen_random_uuid(),
                    'event-reminder',
                    'Event reminder sent 7 days, 2 days, and 1 day before event starts',
                    'Reminder: {{EventTitle}} {{ReminderTimeframe}}',
                    'Hi {{AttendeeName}},

This is a friendly reminder about your upcoming event!

EVENT DETAILS
-------------
Event: {{EventTitle}}
Date: {{EventStartDate}} at {{EventStartTime}}
Location: {{Location}}
Your Tickets: {{Quantity}}
{{ReminderMessage}}

We look forward to seeing you there!

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header {
            background: linear-gradient(135deg, #fb923c 0%, #f43f5e 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }
        .header h1 { margin: 0; font-size: 28px; }
        .content { padding: 30px 20px; background: #f9fafb; }
        .event-info {
            background: white;
            padding: 20px;
            margin: 20px 0;
            border-left: 4px solid #fb923c;
            border-radius: 4px;
        }
        .event-info h2 { margin-top: 0; color: #fb923c; }
        .event-info .detail-row { margin: 12px 0; }
        .event-info .detail-label { font-weight: 600; color: #666; }
        .reminder-message {
            background: #fef3c7;
            border: 2px solid #fbbf24;
            padding: 15px;
            margin: 20px 0;
            border-radius: 6px;
            text-align: center;
            font-size: 16px;
            font-weight: 600;
            color: #92400e;
        }
        .footer {
            text-align: center;
            padding: 20px;
            color: #666;
            font-size: 14px;
        }
        .button {
            display: inline-block;
            background: linear-gradient(135deg, #fb923c 0%, #f43f5e 100%);
            color: white;
            padding: 14px 28px;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            margin: 15px 0;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Event Reminder</h1>
        </div>
        <div class=""content"">
            <p style=""font-size: 16px;"">Hi {{AttendeeName}},</p>
            <p>This is a friendly reminder about your upcoming event!</p>

            <div class=""reminder-message"">
                {{ReminderMessage}}
            </div>

            <div class=""event-info"">
                <h2>Event Details</h2>
                <div class=""detail-row"">
                    <span class=""detail-label"">Event:</span> {{EventTitle}}
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Date:</span> {{EventStartDate}} at {{EventStartTime}}
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Location:</span> {{Location}}
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Your Tickets:</span> {{Quantity}}
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Starting In:</span> {{HoursUntilEvent}} hours
                </div>
            </div>

            <p style=""text-align: center;"">
                <a href=""{{EventDetailsUrl}}"" class=""button"">View Event Details</a>
            </p>

            <p>We look forward to seeing you there!</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
            <p>Questions? Reply to this email or contact support@lankaconnect.com</p>
        </div>
    </div>
</body>
</html>',
                    'EventReminder',
                    'Notification',
                    true,
                    NOW()
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.57: Remove event-reminder template
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'event-reminder';
            ");
        }
    }
}
