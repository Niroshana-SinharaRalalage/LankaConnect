using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Issue #39 Fix: Add "View Event Details" button to registration confirmation emails.
    /// 
    /// Root Cause: The previous fix (commit aa6a1fdb) added the EventDetailsUrl parameter
    /// to RegistrationEmailService but did NOT update the HTML templates to actually
    /// render the link. The URL was being passed but the template had no element to display it.
    /// 
    /// This migration adds a "View Event Details" CTA button to:
    /// - template-free-event-registration-confirmation
    /// - template-paid-event-registration-confirmation-with-ticket
    /// </summary>
    public partial class Issue39_AddEventDetailsLinkToRegistrationEmails : Migration
    {
        // The CTA button HTML to insert (using gradient style consistent with existing templates)
        private const string ViewEventDetailsButton = @"
                            <!-- Issue #39: View Event Details CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 28px 0; text-align: center;"">
                                <tr>
                                    <td style=""text-align: center;"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px; padding: 14px 28px; border-radius: 8px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>
";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update free event registration template
            // Insert the button BEFORE the HasOrganizerContact section
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{#HasOrganizerContact}}',
                    '" + ViewEventDetailsButton.Replace("'", "''") + @"
                            {{#HasOrganizerContact}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-free-event-registration-confirmation';
            ");

            // Update paid event registration template
            // First check what anchor point exists in this template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '{{#HasOrganizerContact}}',
                    '" + ViewEventDetailsButton.Replace("'", "''") + @"
                            {{#HasOrganizerContact}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
            ");

            // Also update text_template for paid event (it was missing EventDetailsUrl)
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET text_template = REGEXP_REPLACE(
                    text_template,
                    'We look forward to seeing you( at the event)?!',
                    E'View full event details: {{EventDetailsUrl}}\n\nWe look forward to seeing you!',
                    'gi'
                ),
                updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket'
                  AND text_template NOT LIKE '%EventDetailsUrl%';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the button from free event template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '" + ViewEventDetailsButton.Replace("'", "''") + @"
                            {{#HasOrganizerContact}}',
                    '{{#HasOrganizerContact}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-free-event-registration-confirmation';
            ");

            // Remove the button from paid event template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '" + ViewEventDetailsButton.Replace("'", "''") + @"
                            {{#HasOrganizerContact}}',
                    '{{#HasOrganizerContact}}'
                ),
                updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
            ");
        }
    }
}
