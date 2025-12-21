using Microsoft.EntityFrameworkCore.Migrations;

namespace LankaConnect.Infrastructure.Data.Migrations;

/// <summary>
/// Phase 6A.39: Seed event-published email template into database.
/// ROOT CAUSE FIX: EventPublishedEventHandler was using IEmailTemplateService (filesystem-based)
/// instead of IEmailService (database-based), causing template rendering failures.
/// This migration adds the template to the database so it can be used with AzureEmailService.
/// </summary>
public partial class SeedEventPublishedTemplate_Phase6A39 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Phase 6A.39: Seed event-published email template
        // Uses {{#variable}}...{{/variable}} syntax for conditionals (supported by AzureEmailService)
        // NOT {{#if variable}}...{{else}}...{{/if}} (Handlebars syntax - not supported)
        migrationBuilder.Sql(@"
            INSERT INTO communications.email_templates
            (
                ""id"",
                ""name"",
                ""description"",
                ""subject_template"",
                ""text_template"",
                ""html_template"",
                ""category"",
                ""is_active"",
                ""created_at"",
                ""updated_at""
            )
            VALUES
            (
                gen_random_uuid(),
                'event-published',
                'Email notification sent to subscribers when a new event is published',
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

---
This email was sent because you subscribed to event notifications for {{EventCity}}, {{EventState}}.

(c) 2025 LankaConnect
If you have questions, contact us at info@lankaconnect.com',
                '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>New Event Announcement</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
        <!-- Header -->
        <tr>
            <td style=""background: linear-gradient(135deg, #FF6600 0%, #8B1538 100%); color: white; padding: 30px 20px; text-align: center;"">
                <h1 style=""margin: 0; font-size: 24px; font-weight: 600;"">New Event Announcement</h1>
            </td>
        </tr>

        <!-- Content -->
        <tr>
            <td style=""padding: 30px 20px;"">
                <h2 style=""font-size: 22px; font-weight: 700; color: #1e293b; margin: 0 0 15px 0;"">{{EventTitle}}</h2>

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

                <p style=""margin-top: 25px; color: #64748b; font-size: 14px; text-align: center;"">
                    Don''t miss out on this exciting event! Click the button above to view full details and secure your spot.
                </p>
            </td>
        </tr>

        <!-- Footer -->
        <tr>
            <td style=""text-align: center; padding: 25px 20px; background: #f8fafc; color: #64748b; font-size: 13px; line-height: 1.8;"">
                <p style=""margin: 0 0 8px 0;"">
                    You''re receiving this email because you subscribed to event notifications for <strong>{{EventCity}}, {{EventState}}</strong>.
                </p>
                <p style=""margin: 0;"">
                    &copy; 2025 LankaConnect | <a href=""mailto:info@lankaconnect.com"" style=""color: #FF6600; text-decoration: none;"">Contact Us</a>
                </p>
            </td>
        </tr>
    </table>
</body>
</html>',
                'Event',
                true,
                NOW(),
                NOW()
            )
            ON CONFLICT (""name"") DO UPDATE SET
                ""description"" = EXCLUDED.""description"",
                ""subject_template"" = EXCLUDED.""subject_template"",
                ""text_template"" = EXCLUDED.""text_template"",
                ""html_template"" = EXCLUDED.""html_template"",
                ""updated_at"" = NOW();
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove the template if rolling back
        migrationBuilder.Sql(@"
            DELETE FROM communications.email_templates
            WHERE ""name"" = 'event-published';
        ");
    }
}
