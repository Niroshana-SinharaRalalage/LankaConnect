using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.97: Add "View Event Details" button to refund email templates.
    ///
    /// This migration adds a clickable button that navigates to the event details page,
    /// allowing users to view the event they were refunded for.
    ///
    /// Uses the {{EventDetailsUrl}} placeholder which is now populated by:
    /// - RefundRequestedEventHandler (via IEmailUrlHelper.BuildEventDetailsUrl)
    /// - RefundCompletedEventHandler (via IEmailUrlHelper.BuildEventDetailsUrl)
    /// </summary>
    public partial class Phase6A97_AddViewEventButtonToRefundTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Add "View Event Details" button to template-refund-requested
            // Inserted before the closing salutation
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,',
                    '<!-- View Event Details CTA Button - Phase 6A.97 -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,'
                ),
                text_template = REPLACE(
                    text_template,
                    'Best regards,',
                    'View Event Details: {{EventDetailsUrl}}

Best regards,'
                ),
                updated_at = NOW()
                WHERE name = 'template-refund-requested';
            ");

            // ============================================================
            // Add "View Event Details" button to template-refund-completed
            // Inserted before the closing salutation
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,',
                    '<!-- View Event Details CTA Button - Phase 6A.97 -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,'
                ),
                text_template = REPLACE(
                    text_template,
                    'Best regards,',
                    'View Event Details: {{EventDetailsUrl}}

Best regards,'
                ),
                updated_at = NOW()
                WHERE name = 'template-refund-completed';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the View Event Details button from template-refund-requested
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- View Event Details CTA Button - Phase 6A.97 -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,',
                    '<p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,'
                ),
                text_template = REPLACE(
                    text_template,
                    'View Event Details: {{EventDetailsUrl}}

Best regards,',
                    'Best regards,'
                ),
                updated_at = NOW()
                WHERE name = 'template-refund-requested';
            ");

            // Remove the View Event Details button from template-refund-completed
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- View Event Details CTA Button - Phase 6A.97 -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,',
                    '<p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,'
                ),
                text_template = REPLACE(
                    text_template,
                    'View Event Details: {{EventDetailsUrl}}

Best regards,',
                    'Best regards,'
                ),
                updated_at = NOW()
                WHERE name = 'template-refund-completed';
            ");
        }
    }
}
