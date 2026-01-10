using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A62_71_AddMissingEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.62: registration-cancellation template
            // Phase 6A.71: newsletter-confirmation template

            // Template 1: Registration Cancellation (Phase 6A.62)
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
                    'registration-cancellation',
                    'Email sent when a user cancels their event registration',
                    'Registration Cancelled for {{EventTitle}}',
                    'Hi {{UserName}},

This email confirms that your registration for the following event has been cancelled:

EVENT DETAILS
-------------
Event: {{EventTitle}}
Date: {{EventStartDate}} at {{EventStartTime}}
Cancelled on: {{CancellationDate}}

If you cancelled by mistake, you can register again if spaces are still available.

Thank you for using LankaConnect!

---
LankaConnect
Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Registration Cancelled - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Registration Cancelled</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi {{UserName}},</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                This email confirms that your registration for the following event has been <strong>cancelled</strong>:
                            </p>

                            <!-- Event Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #fff8f5; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <h2 style=""margin: 0 0 15px 0; color: #8B1538; font-size: 20px;"">{{EventTitle}}</h2>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>📅 Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>❌ Cancelled on:</strong> {{CancellationDate}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; color: #555; line-height: 1.6;"">
                                If you cancelled by mistake, you can register again if spaces are still available.
                            </p>

                            <p style=""margin: 15px 0 0 0; color: #555; line-height: 1.6;"">
                                Thank you for using LankaConnect!
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
                );
            ");

            // Template 2: Newsletter Confirmation (Phase 6A.71)
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
                    'newsletter-confirmation',
                    'Double opt-in confirmation email for newsletter subscriptions',
                    'Confirm Your Newsletter Subscription - LankaConnect',
                    'Hi,

Thank you for subscribing to the LankaConnect newsletter!

To confirm your subscription and start receiving updates about Sri Lankan community events in your area, please click the link below:

{{ConfirmationLink}}

SUBSCRIPTION DETAILS
--------------------
Email: {{Email}}
Metro Areas: {{MetroAreasText}}
{{#ReceiveAllLocations}}All Locations: Yes{{/ReceiveAllLocations}}

WHY CONFIRM?
This extra step ensures that we only send emails to people who genuinely want them. It protects your inbox and helps us maintain a high-quality mailing list.

If you didn''t subscribe to this newsletter, you can safely ignore this email.

---
LankaConnect
Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.

To unsubscribe: {{UnsubscribeLink}}',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Confirm Newsletter Subscription - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Confirm Your Subscription</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi,</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                Thank you for subscribing to the <strong>LankaConnect newsletter</strong>! To confirm your subscription and start receiving updates about Sri Lankan community events, please click the button below:
                            </p>

                            <!-- Confirmation Button -->
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px auto;"">
                                <tr>
                                    <td style=""background: #FF6600; border-radius: 6px; text-align: center;"">
                                        <a href=""{{ConfirmationLink}}"" style=""display: inline-block; padding: 14px 32px; color: white; text-decoration: none; font-weight: bold; font-size: 16px;"">Confirm Subscription</a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Subscription Details -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #f0fdf4; border-left: 4px solid #10b981; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #065f46; font-size: 14px;"">Your Subscription:</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Email:</strong> {{Email}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Metro Areas:</strong> {{MetroAreasText}}</p>
                                        {{#ReceiveAllLocations}}
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>All Locations:</strong> Yes</p>
                                        {{/ReceiveAllLocations}}
                                    </td>
                                </tr>
                            </table>

                            <!-- Why Confirm Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #1e40af; font-size: 14px;"">Why do I need to confirm?</p>
                                        <p style=""margin: 0; color: #666; font-size: 14px; line-height: 1.6;"">
                                            This extra step ensures we only send emails to people who genuinely want them. It protects your inbox and helps us maintain a high-quality mailing list.
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; color: #999; font-size: 13px; line-height: 1.6;"">
                                If you didn''t subscribe to this newsletter, you can safely ignore this email.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
                            <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 15px 0;"">Sri Lankan Community Hub</p>
                            <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0 0 10px 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                            <p style=""margin: 0;""><a href=""{{UnsubscribeLink}}"" style=""color: rgba(255,255,255,0.8); font-size: 11px; text-decoration: underline;"">Unsubscribe</a></p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'Marketing',
                    'Newsletter',
                    true,
                    NOW()
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN ('registration-cancellation', 'newsletter-confirmation');
            ");
        }
    }
}
