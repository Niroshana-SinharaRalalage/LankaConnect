using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A63Fix3_SwapTextHtmlTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.63 FIX 3: Fix text/HTML template swap in event-cancelled-notification
            // The previous migration (Phase6A63Fix) inserted templates with text and HTML swapped
            // This migration deletes the bad data and inserts with CORRECT column order:
            // text_template = plain text version, html_template = HTML version

            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'event-cancelled-notification';
            ");

            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates (""id"", ""name"", ""description"", ""subject_template"", ""text_template"", ""html_template"", ""category"", ""is_active"", ""created_at"", ""updated_at"")
                VALUES (
                    gen_random_uuid(),
                    'event-cancelled-notification',
                    'Event cancellation notification - Sent to all recipients (registrations, email groups, newsletter subscribers) when organizer cancels event',
                    'Event Cancelled: {{EventTitle}} - LankaConnect',
                    'EVENT CANCELLED - LankaConnect

Dear LankaConnect Community,

We regret to inform you that the following event has been CANCELLED:

EVENT: {{EventTitle}}
DATE: {{EventDate}}
LOCATION: {{EventLocation}}

REASON FOR CANCELLATION:
{{CancellationReason}}

We sincerely apologize for any inconvenience this may cause.

REFUND INFORMATION:
- If you paid for this event, you will receive a full refund within 5-7 business days
- Please check your email for a separate refund confirmation
- For questions, contact the organizer or our support team

Browse other upcoming events: {{DashboardUrl}}

Thank you for being part of the LankaConnect community!

---
LankaConnect - Connecting Sri Lankan Communities Worldwide
Unsubscribe from event notifications: {{UnsubscribeUrl}}',
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

                            <!-- Event Details Card -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background: #f9f9f9; border-left: 4px solid #8B1538; border-radius: 8px; padding: 20px; margin-bottom: 25px;"">
                                <tr>
                                    <td>
                                        <h2 style=""margin: 0 0 15px 0; font-size: 22px; color: #8B1538;"">{{EventTitle}}</h2>
                                        <p style=""margin: 0 0 10px 0; color: #666; font-size: 15px;"">
                                            <strong>Date:</strong> {{EventDate}}
                                        </p>
                                        <p style=""margin: 0 0 10px 0; color: #666; font-size: 15px;"">
                                            <strong>Location:</strong> {{EventLocation}}
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Cancellation Reason -->
                            <div style=""background: #fff4e5; border-left: 4px solid #FF6600; border-radius: 8px; padding: 20px; margin-bottom: 25px;"">
                                <h3 style=""margin: 0 0 10px 0; font-size: 16px; color: #FF6600; font-weight: bold;"">Reason for Cancellation:</h3>
                                <p style=""margin: 0; color: #555; line-height: 1.6; font-size: 14px;"">{{CancellationReason}}</p>
                            </div>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                We sincerely apologize for any inconvenience this may cause. If you had registered or planned to attend, please note:
                            </p>

                            <!-- Refund Notice -->
                            <ul style=""margin: 0 0 25px 20px; padding: 0; color: #555; line-height: 1.8;"">
                                <li>If you paid for this event, you will receive a <strong>full refund</strong> within 5-7 business days.</li>
                                <li>Please check your email for a separate refund confirmation.</li>
                                <li>For questions, contact the organizer or our support team.</li>
                            </ul>

                            <!-- Call-to-Action -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-bottom: 25px;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{DashboardUrl}}"" style=""display: inline-block; padding: 14px 32px; background: linear-gradient(135deg, #8B1538 0%, #FF6600 100%); color: #ffffff; text-decoration: none; font-weight: bold; border-radius: 6px; font-size: 16px; box-shadow: 0 2px 4px rgba(0,0,0,0.2);"">
                                            Browse Other Events
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 25px 0 0 0; color: #666; line-height: 1.6; font-size: 14px;"">
                                Thank you for being part of the LankaConnect community. We hope to see you at our upcoming events!
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style=""background: #f9f9f9; padding: 25px 30px; border-top: 1px solid #e0e0e0; text-align: center;"">
                            <p style=""margin: 0 0 10px 0; font-size: 14px; color: #999;"">
                                LankaConnect - Connecting Sri Lankan Communities Worldwide
                            </p>
                            <p style=""margin: 0; font-size: 12px; color: #aaa;"">
                                <a href=""{{UnsubscribeUrl}}"" style=""color: #8B1538; text-decoration: none;"">Unsubscribe</a> from event notifications
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'event',
                    true,
                    NOW(),
                    NOW()
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'event-cancelled-notification';
            ");
        }
    }
}
