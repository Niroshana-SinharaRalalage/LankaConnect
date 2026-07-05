using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.137B2: Add 3 refund email templates for donation, collection, and sponsor refunds.
    ///
    /// Templates:
    /// - template-donation-refund: Sent when a donation is refunded via Stripe webhook
    /// - template-collection-refund: Sent when a collection contribution is refunded via Stripe webhook
    /// - template-sponsor-refund: Sent when a money sponsor payment is refunded via Stripe webhook
    ///
    /// Common Placeholders:
    /// - {{UserName}} - Recipient display name
    /// - {{EventTitle}} - Event title
    /// - {{RefundedAt}} - Refund date/time
    /// - {{PaymentIntentId}} - Stripe payment reference
    /// - {{EventDetailsUrl}} - Link to event page
    /// - {{SupportEmail}} - Support contact email
    /// - {{Year}} - Current year for copyright
    ///
    /// Donation-specific: {{DonorName}}, {{DonorEmail}}, {{DonationAmount}}, {{Currency}}
    /// Collection-specific: {{ContributorName}}, {{ContributorEmail}}, {{ContributionAmount}}, {{Currency}}
    /// Sponsor-specific: {{SponsorName}}, {{SponsorEmail}}, {{SponsorOrganization}}, {{HasOrganization}}, {{Amount}}, {{Currency}}
    /// </summary>
    public partial class Phase6A137B2_AddRefundEmailTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Template 1: Donation Refund ──
            var donationRefundHtml = GetStandardTemplate(
                "Donation Refund Confirmation",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your donation to <strong>{{EventTitle}}</strong> has been refunded. The refund has been processed and should appear in your account within 5-10 business days.</p>

                <!-- REFUND DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fef2f2; padding: 24px; border-radius: 12px; border-left: 4px solid #dc2626;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; font-weight: 600; color: #991b1b; text-transform: uppercase; letter-spacing: 0.5px;"">Refund Details</p>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280; width: 140px;"">Refund Amount:</td>
                                    <td style=""padding: 6px 0; font-size: 20px; font-weight: 700; color: #dc2626;"">{{Currency}} ${{DonationAmount}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Event:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{EventTitle}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Refund Date:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{RefundedAt}}</td>
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
                    If you have any questions about your refund, please contact us at
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
                    'template-donation-refund',
                    'Phase 6A.137B2: Donation refund notification sent when Stripe processes a donation refund',
                    'Your Donation Refund - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Your donation to {{{{EventTitle}}}} has been refunded.

REFUND DETAILS
--------------
Refund Amount: {{{{Currency}}}} ${{{{DonationAmount}}}}
Event: {{{{EventTitle}}}}
Refund Date: {{{{RefundedAt}}}}
Reference: {{{{PaymentIntentId}}}}

The refund should appear in your account within 5-10 business days.

View event details: {{{{EventDetailsUrl}}}}

If you have any questions, contact us at {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(donationRefundHtml)}',
                    'DonationRefund',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-donation-refund'
                );
            ");

            // ── Template 2: Collection Refund ──
            var collectionRefundHtml = GetStandardTemplate(
                "Collection Refund Confirmation",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your contribution to the event fund for <strong>{{EventTitle}}</strong> has been refunded. The refund has been processed and should appear in your account within 5-10 business days.</p>

                <!-- REFUND DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f0fdf4; padding: 24px; border-radius: 12px; border-left: 4px solid #16a34a;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; font-weight: 600; color: #166534; text-transform: uppercase; letter-spacing: 0.5px;"">Refund Details</p>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280; width: 140px;"">Refund Amount:</td>
                                    <td style=""padding: 6px 0; font-size: 20px; font-weight: 700; color: #16a34a;"">{{Currency}} ${{ContributionAmount}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Event:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{EventTitle}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Refund Date:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{RefundedAt}}</td>
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
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #16a34a, #166534); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    If you have any questions about your refund, please contact us at
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
                    'template-collection-refund',
                    'Phase 6A.137B2: Collection refund notification sent when Stripe processes a collection refund',
                    'Your Event Fund Contribution Refund - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Your contribution to the event fund for {{{{EventTitle}}}} has been refunded.

REFUND DETAILS
--------------
Refund Amount: {{{{Currency}}}} ${{{{ContributionAmount}}}}
Event: {{{{EventTitle}}}}
Refund Date: {{{{RefundedAt}}}}
Reference: {{{{PaymentIntentId}}}}

The refund should appear in your account within 5-10 business days.

View event details: {{{{EventDetailsUrl}}}}

If you have any questions, contact us at {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(collectionRefundHtml)}',
                    'CollectionRefund',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-collection-refund'
                );
            ");

            // ── Template 3: Sponsor Refund ──
            var sponsorRefundHtml = GetStandardTemplate(
                "Sponsorship Refund Confirmation",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your monetary sponsorship for <strong>{{EventTitle}}</strong> has been refunded. The refund has been processed and should appear in your account within 5-10 business days.</p>

                <!-- REFUND DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fefce8; padding: 24px; border-radius: 12px; border-left: 4px solid #ca8a04;"">
                            <p style=""margin: 0 0 4px 0; font-size: 13px; font-weight: 600; color: #854d0e; text-transform: uppercase; letter-spacing: 0.5px;"">Refund Details</p>
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 12px;"">
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280; width: 140px;"">Refund Amount:</td>
                                    <td style=""padding: 6px 0; font-size: 20px; font-weight: 700; color: #ca8a04;"">{{Currency}} ${{Amount}}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Event:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827; font-weight: 500;"">{{EventTitle}}</td>
                                </tr>
                                {{#if HasOrganization}}
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Organization:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{SponsorOrganization}}</td>
                                </tr>
                                {{/if}}
                                <tr>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #6b7280;"">Refund Date:</td>
                                    <td style=""padding: 6px 0; font-size: 14px; color: #111827;"">{{RefundedAt}}</td>
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
                            <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ca8a04, #854d0e); color: #ffffff; padding: 14px 36px; border-radius: 8px; text-decoration: none; font-size: 15px; font-weight: 600;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                    If you have any questions about your refund, please contact us at
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
                    'template-sponsor-refund',
                    'Phase 6A.137B2: Sponsor refund notification sent when Stripe processes a money sponsor refund',
                    'Your Sponsorship Refund - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Your monetary sponsorship for {{{{EventTitle}}}} has been refunded.

REFUND DETAILS
--------------
Refund Amount: {{{{Currency}}}} ${{{{Amount}}}}
Event: {{{{EventTitle}}}}
Refund Date: {{{{RefundedAt}}}}
Reference: {{{{PaymentIntentId}}}}

The refund should appear in your account within 5-10 business days.

View event details: {{{{EventDetailsUrl}}}}

If you have any questions, contact us at {{{{SupportEmail}}}}.

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(sponsorRefundHtml)}',
                    'SponsorRefund',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-sponsor-refund'
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-donation-refund',
                    'template-collection-refund',
                    'template-sponsor-refund'
                );
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
