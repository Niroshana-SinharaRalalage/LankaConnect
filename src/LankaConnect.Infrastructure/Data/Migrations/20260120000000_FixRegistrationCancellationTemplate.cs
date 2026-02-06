using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.62 Fix: Updates the registration-cancellation email template to use correct parameter names.
    ///
    /// Problem: The template inserted by Phase6A54 migration used wrong parameter names:
    ///   - Template had: {{EventDateTime}}, {{EventLocation}}, {{CancellationDateTime}}, {{RefundDetails}}, {{CancellationPolicy}}
    ///   - Handler sends: {{EventStartDate}}, {{EventStartTime}}, {{CancellationDate}}, {{UserName}}, {{EventTitle}}, {{Reason}}
    ///
    /// This mismatch caused the placeholders to not be replaced, resulting in emails not being sent correctly.
    ///
    /// Solution: Update the existing template to match what RegistrationCancelledEventHandler sends.
    /// </summary>
    public partial class FixRegistrationCancellationTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update the registration-cancellation template with correct parameter names
            // Parameters expected by RegistrationCancelledEventHandler:
            // - UserName: User's full name
            // - EventTitle: Event title
            // - EventStartDate: Formatted date (e.g., "January 20, 2026")
            // - EventStartTime: Formatted time (e.g., "3:00 PM")
            // - CancellationDate: Formatted datetime of cancellation
            // - Reason: Cancellation reason (currently hardcoded as "User cancelled registration")
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    ""subject_template"" = 'Registration Cancelled for {{EventTitle}}',
                    ""text_template"" = 'Hi {{UserName}},

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
                    ""html_template"" = '<!DOCTYPE html>
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
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Cancelled on:</strong> {{CancellationDate}}</p>
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
                    ""updated_at"" = NOW(),
                    ""description"" = 'Email sent when a user cancels their event registration (Fixed parameter names)'
                WHERE name = 'registration-cancellation';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to the original Phase6A54 template (with wrong parameters)
            // This is intentional - if we need to rollback, go back to original state
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    ""subject_template"" = 'Registration Cancelled - {{EventTitle}}',
                    ""text_template"" = 'Hi {{UserName}},

Your registration for {{EventTitle}} has been cancelled.

CANCELLED REGISTRATION
-----------------------
Event: {{EventTitle}}
Date: {{EventDateTime}}
Location: {{EventLocation}}
Cancelled At: {{CancellationDateTime}}

REFUND INFORMATION
------------------
{{RefundDetails}}

CANCELLATION POLICY
-------------------
{{CancellationPolicy}}

If you cancelled by mistake, you can register again if spots are still available.

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    ""html_template"" = '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #ef4444; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .info-box { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #ef4444; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Registration Cancelled</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Your registration for <strong>{{EventTitle}}</strong> has been cancelled.</p>
            <div class=""info-box"">
                <h3>Cancelled Registration</h3>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Date:</strong> {{EventDateTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><strong>Cancelled At:</strong> {{CancellationDateTime}}</p>
            </div>
            <div class=""info-box"">
                <h3>Refund Information</h3>
                <p>{{RefundDetails}}</p>
            </div>
            <div class=""info-box"">
                <h3>Cancellation Policy</h3>
                <p>{{CancellationPolicy}}</p>
            </div>
            <p>If you cancelled by mistake, you can register again if spots are still available.</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    ""updated_at"" = NOW(),
                    ""description"" = 'Confirmation email when user cancels event registration'
                WHERE name = 'registration-cancellation';
            ");
        }
    }
}