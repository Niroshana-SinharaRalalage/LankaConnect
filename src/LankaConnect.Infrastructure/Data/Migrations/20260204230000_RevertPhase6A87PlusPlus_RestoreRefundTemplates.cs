using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Revert Phase6A87++ and restore refund templates to Phase6A96 standardized styling.
    ///
    /// Root Cause: Phase6A87++ completely replaced refund templates, breaking the
    /// standardized header/footer established by Phase6A96.
    ///
    /// This migration restores the Phase6A96 templates using GetStandardTemplate() pattern.
    /// Parameter fixes (ReferenceId, EventDateTime, HasOrganizerContact) will be applied
    /// in a separate surgical migration.
    /// </summary>
    public partial class RevertPhase6A87PlusPlus_RestoreRefundTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Restore template-refund-requested to Phase6A96 standardized version
            var refundRequestedHtml = GetStandardTemplate(
                "Refund In Progress",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">We wanted to let you know that a refund for your registration is being processed.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #fffbeb; border: 2px solid #f59e0b; padding: 24px; border-radius: 8px; text-align: center;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; color: #92400e;"">Refund Amount</p>
                                        <p style=""margin: 0; font-size: 32px; font-weight: 700; color: #b45309;"">${{RefundAmount}}</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">Event</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventTitle}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">Event Date</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0; background: #f9fafb; padding: 20px; border-radius: 6px;"">
                                <tr>
                                    <td>
                                        <p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 600; color: #374151;"">What Happens Next</p>
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; color: #4b5563;"">✓ Refund initiated</p>
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; color: #4b5563;"">○ Processing (5-10 business days)</p>
                                        <p style=""margin: 0; font-size: 14px; color: #4b5563;"">○ Funds returned to original payment method</p>
                                    </td>
                                </tr>
                            </table>

                            {{#if HasOrganizerContact}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #f3f4f6; padding: 20px; border-radius: 6px;"">
                                        <p style=""margin: 0 0 10px 0; font-size: 14px; font-weight: 600; color: #374151;"">Questions? Contact the organizer:</p>
                                        <p style=""margin: 0; font-size: 14px; color: #4b5563;""><strong>{{OrganizerContactName}}</strong> - <a href=""mailto:{{OrganizerContactEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{OrganizerContactEmail}}</a></p>
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(refundRequestedHtml)}',
                    text_template = 'Hi {{{{UserName}}}},

We wanted to let you know that a refund for your registration to ""{{{{EventTitle}}}}"" is being processed.

REFUND DETAILS
Event: {{{{EventTitle}}}}
Event Date: {{{{EventDateTime}}}}
Refund Amount: ${{{{RefundAmount}}}}

WHAT HAPPENS NEXT
Your refund will be processed within 5-10 business days. The funds will be returned to your original payment method.

You will receive a confirmation email once the refund has been completed.

{{{{#if HasOrganizerContact}}}}
QUESTIONS?
Contact the event organizer:
{{{{OrganizerContactName}}}} - {{{{OrganizerContactEmail}}}}
{{{{/if}}}}

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-refund-requested';
            ");

            // Restore template-refund-completed to Phase6A96 standardized version
            var refundCompletedHtml = GetStandardTemplate(
                "Refund Complete",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Great news! Your refund has been successfully processed.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; padding: 24px; border-radius: 8px; text-align: center;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; color: #065f46;"">Refunded Amount</p>
                                        <p style=""margin: 0; font-size: 36px; font-weight: 700; color: #059669;"">${{RefundAmount}}</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">Event</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventTitle}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">Event Date</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">Reference ID</span>
                                        <span style=""float: right; font-family: monospace; background: #f3f4f6; padding: 4px 8px; border-radius: 4px; font-size: 12px;"">{{StripeRefundId}}</span>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; border-radius: 0 8px 8px 0;"">
                                        <p style=""margin: 0; color: #1e40af; font-size: 14px;""><strong>Note:</strong> Depending on your bank, it may take 3-5 additional business days for the funds to appear in your account.</p>
                                    </td>
                                </tr>
                            </table>

                            {{#if HasOrganizerContact}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #f3f4f6; padding: 20px; border-radius: 6px;"">
                                        <p style=""margin: 0 0 10px 0; font-size: 14px; font-weight: 600; color: #374151;"">Questions? Contact the organizer:</p>
                                        <p style=""margin: 0; font-size: 14px; color: #4b5563;""><strong>{{OrganizerContactName}}</strong> - <a href=""mailto:{{OrganizerContactEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{OrganizerContactEmail}}</a></p>
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(refundCompletedHtml)}',
                    text_template = 'Hi {{{{UserName}}}},

Great news! Your refund has been successfully processed.

REFUND CONFIRMATION
Event: {{{{EventTitle}}}}
Event Date: {{{{EventDateTime}}}}
Refund Amount: ${{{{RefundAmount}}}}
Reference ID: {{{{StripeRefundId}}}}

The funds have been returned to your original payment method. Depending on your bank, it may take 3-5 additional business days to appear in your account.

{{{{#if HasOrganizerContact}}}}
QUESTIONS?
Contact the event organizer:
{{{{OrganizerContactName}}}} - {{{{OrganizerContactEmail}}}}
{{{{/if}}}}

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-refund-completed';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down would restore Phase6A87++ templates, but those were broken
            // so we don't implement a full rollback
        }

        /// <summary>
        /// Creates a standard email template with consistent header and footer.
        /// Copied from Phase6A96 migration for consistency.
        /// </summary>
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
                <!-- Main Container - Responsive -->
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

                    <!-- Main Content Area -->
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

        /// <summary>
        /// Escapes single quotes for SQL string literals.
        /// </summary>
        private string EscapeSql(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
