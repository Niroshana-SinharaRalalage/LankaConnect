using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.130: Add donation receipt email template.
    ///
    /// Template: template-donation-receipt
    /// Sent when a donation payment is completed via Stripe webhook.
    ///
    /// Placeholders:
    /// - {{UserName}} - Donor display name
    /// - {{DonorName}} - Donor name
    /// - {{DonorEmail}} - Donor email
    /// - {{EventTitle}} - Event title
    /// - {{DonationAmount}} - Formatted amount (e.g., "50.00")
    /// - {{DonationCurrency}} - Currency code (e.g., "USD")
    /// - {{PaymentIntentId}} - Stripe payment reference
    /// - {{PaymentDate}} - Date/time of payment
    /// - {{EventDetailsUrl}} - Link to event page
    /// - {{SupportEmail}} - Support contact email
    /// - {{Year}} - Current year for copyright
    /// </summary>
    public partial class Phase6A130_AddDonationReceiptEmailTemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var htmlTemplate = GetStandardTemplate(
                "Donation Receipt",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Thank you for your generous donation to <strong>{{EventTitle}}</strong>! Your support means a lot to the event organizer and community.</p>

                <!-- DONATION DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fef2f2; padding: 24px; border-radius: 12px; border-left: 4px solid #dc2626;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; font-weight: 600; color: #991b1b; text-transform: uppercase; letter-spacing: 0.5px;"">Donation Receipt</p>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280; width: 140px;"">Amount:</td>
                                    <td style=""padding: 6px 0; font-size: 20px; font-weight: 700; color: #111827;"">{{DonationCurrency}} ${{DonationAmount}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Event:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{EventTitle}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Date:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{PaymentDate}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Reference:</td>
                                    <td style=""padding: 6px 0; font-size: 13px; color: #6b7280; font-family: monospace;"">{{PaymentIntentId}}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <!-- VIEW EVENT LINK -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td align=""center"">
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #dc2626, #9f1239); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    If you have any questions about your donation, please contact us at
                    <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.
                </p>

                <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Best regards,<br>
                    <strong style=""color: #374151;"">The LankaConnect Team</strong>
                </p>");

            migrationBuilder.Sql($@"
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
                    'template-donation-receipt',
                    'Phase 6A.130: Donation receipt email sent after successful payment',
                    'Thank You for Your Donation - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Thank you for your generous donation to {{{{EventTitle}}}}!

DONATION RECEIPT
----------------
Amount: {{{{DonationCurrency}}}} ${{{{DonationAmount}}}}
Event: {{{{EventTitle}}}}
Date: {{{{PaymentDate}}}}
Reference: {{{{PaymentIntentId}}}}

View event details: {{{{EventDetailsUrl}}}}

If you have any questions, contact us at {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(htmlTemplate)}',
                    'DonationReceipt',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-donation-receipt'
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'template-donation-receipt';
            ");
        }

        private string GetStandardTemplate(string headerTitle, string contentHtml)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>LankaConnect</title>
</head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333333; margin: 0; padding: 0; background-color: #f3f4f6;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f3f4f6;"">
        <tr>
            <td align=""center"" style=""padding: 20px 10px;"">
                <!-- Main Container -->
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""width: 100%; max-width: 850px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);"">

                    <!-- Header Section - Gradient -->
                    <tr>
                        <td style=""padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 35px 30px; text-align: center; border-radius: 12px 12px 0 0;"">
                                        <span style=""font-size: 24px; font-weight: 500; color: #ffffff;"">{headerTitle}</span>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- BODY CONTENT -->
                    <tr>
                        <td style=""padding: 35px 40px;"">
                            {contentHtml}
                        </td>
                    </tr>

                    <!-- Footer Section - Gradient -->
                    <tr>
                        <td style=""padding: 0;"">
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); padding: 28px 30px; text-align: center; border-radius: 0 0 12px 12px;"">
                                        <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""text-align: center; padding-bottom: 4px;"">
                                                    <span style=""font-size: 24px; font-weight: 400; color: #ffffff; letter-spacing: 0.5px;"">LankaConnect</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""text-align: center;"">
                                                    <span style=""font-size: 13px; font-weight: 400; color: #ffffff; opacity: 0.9;"">Sri Lankan Community Hub</span>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string EscapeSql(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
