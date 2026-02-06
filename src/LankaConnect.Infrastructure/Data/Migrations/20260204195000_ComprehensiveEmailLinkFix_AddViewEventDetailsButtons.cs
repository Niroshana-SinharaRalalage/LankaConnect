using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Comprehensive Email Link Fix: Add "View Event Details" and "View Sign-Up Lists" buttons
    /// to all event-related email templates that are missing them.
    ///
    /// Issue: Many event-related templates have handlers passing EventDetailsUrl but the HTML
    /// templates don't have elements to render the links.
    ///
    /// Templates Fixed (11 total):
    /// HIGH PRIORITY (Signup Commitment):
    /// 1. template-signup-list-commitment-confirmation
    /// 2. template-signup-list-commitment-update
    /// 3. template-signup-list-commitment-cancellation
    ///
    /// MEDIUM PRIORITY (Event-Related):
    /// 4. template-attendees-added-confirmation
    /// 5. template-event-registration-cancellation
    /// 6. template-event-approval
    /// 7. template-event-details-publication
    /// 8. template-new-event-publication
    /// 9. template-preliminary-registration-payment-pending
    ///
    /// LOW PRIORITY (Refund):
    /// 10. template-refund-completed
    /// 11. template-refund-requested
    /// </summary>
    public partial class ComprehensiveEmailLinkFix_AddViewEventDetailsButtons : Migration
    {
        // Standard CTA button HTML - View Event Details
        private const string ViewEventDetailsButton = @"
                            <!-- View Event Details CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 28px 0; text-align: center;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px; padding: 14px 28px; border-radius: 8px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>
";

        // Standard CTA button HTML - View Sign-Up Lists (anchor link)
        private const string ViewSignUpListsButton = @"
                            <!-- View Sign-Up Lists CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 0 28px 0; text-align: center;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{{EventDetailsUrl}}#sign-ups"" style=""display: inline-block; background: #166534; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 14px; padding: 12px 24px; border-radius: 8px;"">View Sign-Up Lists</a>
                                    </td>
                                </tr>
                            </table>
";

        // Combined buttons for templates that need both
        private const string BothButtons = @"
                            <!-- View Event Details CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 28px 0; text-align: center;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px; padding: 14px 28px; border-radius: 8px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>
                            <!-- View Sign-Up Lists CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 0 28px 0; text-align: center;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{{EventDetailsUrl}}#sign-ups"" style=""display: inline-block; background: #166534; color: #ffffff; text-decoration: none; font-weight: 600; font-size: 14px; padding: 12px 24px; border-radius: 8px;"">View Sign-Up Lists</a>
                                    </td>
                                </tr>
                            </table>
";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // HIGH PRIORITY: Signup Commitment Templates (need both buttons)
            // ============================================================

            // 1. template-signup-list-commitment-confirmation
            // Insert buttons before {{#HasOrganizerContact}} or before footer
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{BothButtons.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // 2. template-signup-list-commitment-update
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{BothButtons.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // 3. template-signup-list-commitment-cancellation
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{BothButtons.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-cancellation'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // ============================================================
            // MEDIUM PRIORITY: Other Event-Related Templates
            // ============================================================

            // 4. template-attendees-added-confirmation
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{ViewEventDetailsButton.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-attendees-added-confirmation'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // 5. template-event-registration-cancellation
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{ViewEventDetailsButton.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-registration-cancellation'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // 6. template-event-approval (with sign-up lists since organizers might set them up)
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{BothButtons.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-approval'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // 7. template-event-details-publication (with sign-up lists)
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{BothButtons.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-details-publication'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // 8. template-new-event-publication (with sign-up lists)
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{BothButtons.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-new-event-publication'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // 9. template-preliminary-registration-payment-pending
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{ViewEventDetailsButton.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-preliminary-registration-payment-pending'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // ============================================================
            // LOW PRIORITY: Refund Templates
            // ============================================================

            // 10. template-refund-completed
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{ViewEventDetailsButton.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-refund-completed'
                  AND html_template NOT LIKE '%View Event Details%';
            ");

            // 11. template-refund-requested
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{{{#HasOrganizerContact}}}}',
                    '{ViewEventDetailsButton.Replace("'", "''")}
                            {{{{#HasOrganizerContact}}}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-refund-requested'
                  AND html_template NOT LIKE '%View Event Details%';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the buttons by replacing them with nothing
            // This is a best-effort rollback

            var templatesToFix = new[]
            {
                "template-signup-list-commitment-confirmation",
                "template-signup-list-commitment-update",
                "template-signup-list-commitment-cancellation",
                "template-attendees-added-confirmation",
                "template-event-registration-cancellation",
                "template-event-approval",
                "template-event-details-publication",
                "template-new-event-publication",
                "template-preliminary-registration-payment-pending",
                "template-refund-completed",
                "template-refund-requested"
            };

            foreach (var template in templatesToFix)
            {
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REGEXP_REPLACE(
                        html_template,
                        '<!-- View Event Details CTA Button -->.*?</table>\s*',
                        '',
                        'gs'
                    ),
                    updated_at = NOW()
                    WHERE name = '{template}';
                ");

                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REGEXP_REPLACE(
                        html_template,
                        '<!-- View Sign-Up Lists CTA Button -->.*?</table>\s*',
                        '',
                        'gs'
                    ),
                    updated_at = NOW()
                    WHERE name = '{template}';
                ");
            }
        }
    }
}
