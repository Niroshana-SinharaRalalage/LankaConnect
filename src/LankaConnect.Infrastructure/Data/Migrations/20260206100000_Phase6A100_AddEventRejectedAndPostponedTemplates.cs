using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.100: Add email templates for event rejection and postponement notifications.
    /// These templates replace inline HTML generation in EventRejectedEventHandler and EventPostponedEventHandler.
    ///
    /// Templates added:
    /// - template-event-rejected: Notification to organizer when event is rejected by admin
    /// - template-event-postponed: Notification to attendees when event is postponed
    /// </summary>
    public partial class Phase6A100_AddEventRejectedAndPostponedTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Template 1: Event Rejected - sent to organizer when their event is rejected
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
                    'template-event-rejected',
                    'Phase 6A.100: Email sent to organizer when their event submission is rejected and requires changes',
                    'Event Requires Changes: {{EventTitle}}',
                    'Dear {{OrganizerName}},

Your event submission has been reviewed and requires some changes before it can be approved.

Event Details:
- Event: {{EventTitle}}
- Scheduled Date: {{EventStartDate}} at {{EventStartTime}}
- Reviewed: {{RejectedAt}}

Feedback from our team:
{{Reason}}

Please update your event and resubmit for approval.

If you have any questions, please contact us at {{SupportEmail}}.

Best regards,
The LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .event-details { background: #fef3c7; border: 1px solid #f59e0b; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .event-details h3 { margin: 0 0 12px 0; color: #92400e; font-size: 16px; }
        .event-details p { margin: 6px 0; color: #78350f; }
        .feedback-box { background: #fff7ed; border-left: 4px solid #f59e0b; padding: 20px; margin: 20px 0; border-radius: 6px; }
        .feedback-box h3 { margin: 0 0 12px 0; color: #9a3412; font-size: 14px; }
        .feedback-box p { margin: 0; color: #7c2d12; white-space: pre-wrap; }
        .action-text { color: #374151; font-size: 14px; margin: 20px 0; padding: 15px; background: #f3f4f6; border-radius: 6px; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #f59e0b; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>⚠️ Event Requires Changes</h1>
        </div>
        <div class=""content"">
            <p>Dear <strong>{{OrganizerName}}</strong>,</p>
            <p>Your event submission has been reviewed and requires some changes before it can be approved.</p>

            <div class=""event-details"">
                <h3>Event Details</h3>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Scheduled Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                <p><strong>Reviewed:</strong> {{RejectedAt}}</p>
            </div>

            <div class=""feedback-box"">
                <h3>Feedback from our team:</h3>
                <p>{{Reason}}</p>
            </div>

            <p class=""action-text"">📝 Please update your event based on the feedback above and resubmit for approval.</p>

            <p>If you have any questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #f59e0b;"">{{SupportEmail}}</a>.</p>

            <p>Best regards,<br><strong>The LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Event',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-event-rejected'
                );
            ");

            // Template 2: Event Postponed - sent to attendees when event is postponed
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
                    'template-event-postponed',
                    'Phase 6A.100: Email sent to attendees when an event they registered for is postponed',
                    'Event Postponed: {{EventTitle}}',
                    'Dear {{UserName}},

We would like to inform you that the following event has been postponed:

Event Details:
- Event: {{EventTitle}}
- Original Date: {{OriginalStartDate}} at {{OriginalStartTime}}
- Postponed On: {{PostponedAt}}

Reason for Postponement:
{{Reason}}

What happens next:
- Your registration remains active
- We will notify you once a new date has been confirmed
- If you need to cancel your registration, you may do so at any time

We apologize for any inconvenience this may cause. Thank you for your understanding.

If you have any questions, please contact us at {{SupportEmail}}.

Best regards,
The LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .event-details { background: #f5f3ff; border: 1px solid #8b5cf6; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .event-details h3 { margin: 0 0 12px 0; color: #5b21b6; font-size: 16px; }
        .event-details p { margin: 6px 0; color: #4c1d95; }
        .reason-box { background: #faf5ff; border-left: 4px solid #8b5cf6; padding: 20px; margin: 20px 0; border-radius: 6px; }
        .reason-box h3 { margin: 0 0 12px 0; color: #6b21a8; font-size: 14px; }
        .reason-box p { margin: 0; color: #581c87; white-space: pre-wrap; }
        .info-box { background: #ecfdf5; border: 1px solid #10b981; padding: 15px; margin: 20px 0; border-radius: 6px; }
        .info-box h4 { margin: 0 0 10px 0; color: #065f46; font-size: 14px; }
        .info-box ul { margin: 0; padding-left: 20px; color: #047857; }
        .info-box li { margin: 4px 0; }
        .apology-text { color: #6b7280; font-size: 14px; margin: 20px 0; font-style: italic; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #8b5cf6; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>📅 Event Postponed</h1>
        </div>
        <div class=""content"">
            <p>Dear <strong>{{UserName}}</strong>,</p>
            <p>We would like to inform you that the following event has been postponed:</p>

            <div class=""event-details"">
                <h3>Event Details</h3>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Original Date:</strong> {{OriginalStartDate}} at {{OriginalStartTime}}</p>
                <p><strong>Postponed On:</strong> {{PostponedAt}}</p>
            </div>

            <div class=""reason-box"">
                <h3>Reason for Postponement:</h3>
                <p>{{Reason}}</p>
            </div>

            <div class=""info-box"">
                <h4>✅ What happens next:</h4>
                <ul>
                    <li>Your registration remains active</li>
                    <li>We will notify you once a new date has been confirmed</li>
                    <li>If you need to cancel your registration, you may do so at any time</li>
                </ul>
            </div>

            <p class=""apology-text"">We apologize for any inconvenience this may cause. Thank you for your understanding.</p>

            <p>If you have any questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #8b5cf6;"">{{SupportEmail}}</a>.</p>

            <p>Best regards,<br><strong>The LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Event',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-event-postponed'
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Phase 6A.100 email templates
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-event-rejected',
                    'template-event-postponed'
                );
            ");
        }
    }
}
