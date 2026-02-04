using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.87+ Fix: COMPLETE REBUILD of all signup commitment email templates.
    ///
    /// ROOT CAUSE: The original templates were created with placeholder names that don't match
    /// what TypedEmailParams provides. Phase 6A.96 only rebuilt the CANCELLATION template,
    /// leaving CONFIRMATION and UPDATE templates with broken placeholders.
    ///
    /// This migration:
    /// 1. Completely rebuilds template-signup-list-commitment-confirmation
    /// 2. Completely rebuilds template-signup-list-commitment-update
    /// 3. Ensures all placeholders match SignupCommitmentEmailParams.ToDictionary()
    ///
    /// Placeholders used (must match SignupCommitmentEmailParams):
    /// - UserName, EventTitle, ItemDescription, Quantity
    /// - EventDateTime (combined date/time with timezone)
    /// - EventLocation, EventDetailsUrl
    /// - HasSignUpLists, SignUpListsUrl
    /// - HasOrganizerContact, OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone
    /// - Year (for footer)
    /// </summary>
    public partial class Phase6A87Plus_RebuildSignupCommitmentTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rebuild CONFIRMATION template
            RebuildConfirmationTemplate(migrationBuilder);

            // Rebuild UPDATE template
            RebuildUpdateTemplate(migrationBuilder);

            // Rebuild CANCELLATION template (ensure it has all fields too)
            RebuildCancellationTemplate(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot automatically revert - would need old templates
            migrationBuilder.Sql(@"
                SELECT 'Phase 6A.87+ Down: Signup commitment templates cannot be automatically reverted.' as warning;
            ");
        }

        private void RebuildConfirmationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Sign-Up Confirmed!",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your sign-up commitment has been confirmed!</p>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #ecfdf5; padding: 20px; border-radius: 8px; border-left: 4px solid #059669;"">
                            <p style=""margin: 0 0 12px 0; font-size: 18px; font-weight: 600; color: #065f46;"">{{EventTitle}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>Item:</strong> {{ItemDescription}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>Quantity:</strong> {{Quantity}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>Date:</strong> {{EventDateTime}}</p>
                            <p style=""margin: 0; font-size: 14px; color: #374151;""><strong>Location:</strong> {{EventLocation}}</p>
                        </td>
                    </tr>
                </table>

                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                    <tr>
                        <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 14px 28px; border-radius: 8px;"">
                            <a href=""{{EventDetailsUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                {{#if HasSignUpLists}}
                <p style=""margin: 20px 0; font-size: 14px; color: #6b7280;"">
                    This event has sign-up lists. <a href=""{{SignUpListsUrl}}"" style=""color: #ea580c; text-decoration: underline;"">View Sign-Up Lists</a>
                </p>
                {{/if}}

                {{#if HasOrganizerContact}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f0f9ff; padding: 20px; border-radius: 8px; border-left: 4px solid #0284c7;"">
                            <p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 600; color: #0369a1;"">Event Organizer Contact Information</p>
                            <p style=""margin: 0 0 4px 0; font-size: 14px; color: #374151;""><strong>Name:</strong> {{OrganizerContactName}}</p>
                            <p style=""margin: 0 0 4px 0; font-size: 14px; color: #374151;""><strong>Email:</strong> {{OrganizerContactEmail}}</p>
                            {{#if OrganizerContactPhone}}<p style=""margin: 0; font-size: 14px; color: #374151;""><strong>Phone:</strong> {{OrganizerContactPhone}}</p>{{/if}}
                        </td>
                    </tr>
                </table>
                {{/if}}

                <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Best regards,<br>
                    <strong style=""color: #374151;"">The LankaConnect Team</strong>
                </p>");

            var text = @"Hi {{UserName}},

Your sign-up commitment has been confirmed!

Event: {{EventTitle}}
Item: {{ItemDescription}}
Quantity: {{Quantity}}
Date: {{EventDateTime}}
Location: {{EventLocation}}

View Event Details: {{EventDetailsUrl}}

{{#if HasSignUpLists}}
View Sign-Up Lists: {{SignUpListsUrl}}
{{/if}}

{{#if HasOrganizerContact}}
Event Organizer Contact:
Name: {{OrganizerContactName}}
Email: {{OrganizerContactEmail}}
{{#if OrganizerContactPhone}}Phone: {{OrganizerContactPhone}}{{/if}}
{{/if}}

Best regards,
The LankaConnect Team";

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = '{EscapeSql(text)}',
                    updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation';
            ");
        }

        private void RebuildUpdateTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Sign-Up Updated",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your sign-up commitment has been updated.</p>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fef3c7; padding: 20px; border-radius: 8px; border-left: 4px solid #d97706;"">
                            <p style=""margin: 0 0 12px 0; font-size: 18px; font-weight: 600; color: #92400e;"">{{EventTitle}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>Item:</strong> {{ItemDescription}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>New Quantity:</strong> {{Quantity}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>Date:</strong> {{EventDateTime}}</p>
                            <p style=""margin: 0; font-size: 14px; color: #374151;""><strong>Location:</strong> {{EventLocation}}</p>
                        </td>
                    </tr>
                </table>

                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                    <tr>
                        <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 14px 28px; border-radius: 8px;"">
                            <a href=""{{EventDetailsUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                {{#if HasSignUpLists}}
                <p style=""margin: 20px 0; font-size: 14px; color: #6b7280;"">
                    This event has sign-up lists. <a href=""{{SignUpListsUrl}}"" style=""color: #ea580c; text-decoration: underline;"">View Sign-Up Lists</a>
                </p>
                {{/if}}

                {{#if HasOrganizerContact}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f0f9ff; padding: 20px; border-radius: 8px; border-left: 4px solid #0284c7;"">
                            <p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 600; color: #0369a1;"">Event Organizer Contact Information</p>
                            <p style=""margin: 0 0 4px 0; font-size: 14px; color: #374151;""><strong>Name:</strong> {{OrganizerContactName}}</p>
                            <p style=""margin: 0 0 4px 0; font-size: 14px; color: #374151;""><strong>Email:</strong> {{OrganizerContactEmail}}</p>
                            {{#if OrganizerContactPhone}}<p style=""margin: 0; font-size: 14px; color: #374151;""><strong>Phone:</strong> {{OrganizerContactPhone}}</p>{{/if}}
                        </td>
                    </tr>
                </table>
                {{/if}}

                <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Best regards,<br>
                    <strong style=""color: #374151;"">The LankaConnect Team</strong>
                </p>");

            var text = @"Hi {{UserName}},

Your sign-up commitment has been updated.

Event: {{EventTitle}}
Item: {{ItemDescription}}
New Quantity: {{Quantity}}
Date: {{EventDateTime}}
Location: {{EventLocation}}

View Event Details: {{EventDetailsUrl}}

{{#if HasSignUpLists}}
View Sign-Up Lists: {{SignUpListsUrl}}
{{/if}}

{{#if HasOrganizerContact}}
Event Organizer Contact:
Name: {{OrganizerContactName}}
Email: {{OrganizerContactEmail}}
{{#if OrganizerContactPhone}}Phone: {{OrganizerContactPhone}}{{/if}}
{{/if}}

Best regards,
The LankaConnect Team";

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = '{EscapeSql(text)}',
                    updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update';
            ");
        }

        private void RebuildCancellationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Sign-Up Cancelled",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your sign-up commitment has been cancelled.</p>

                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fef2f2; padding: 20px; border-radius: 8px; border-left: 4px solid #dc2626;"">
                            <p style=""margin: 0 0 12px 0; font-size: 18px; font-weight: 600; color: #991b1b;"">{{EventTitle}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>Item:</strong> {{ItemDescription}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>Quantity:</strong> {{Quantity}}</p>
                            <p style=""margin: 0 0 8px 0; font-size: 14px; color: #374151;""><strong>Date:</strong> {{EventDateTime}}</p>
                            <p style=""margin: 0; font-size: 14px; color: #374151;""><strong>Location:</strong> {{EventLocation}}</p>
                        </td>
                    </tr>
                </table>

                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                    <tr>
                        <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 14px 28px; border-radius: 8px;"">
                            <a href=""{{EventDetailsUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;"">View Event Details</a>
                        </td>
                    </tr>
                </table>

                {{#if HasOrganizerContact}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f0f9ff; padding: 20px; border-radius: 8px; border-left: 4px solid #0284c7;"">
                            <p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 600; color: #0369a1;"">Event Organizer Contact Information</p>
                            <p style=""margin: 0 0 4px 0; font-size: 14px; color: #374151;""><strong>Name:</strong> {{OrganizerContactName}}</p>
                            <p style=""margin: 0 0 4px 0; font-size: 14px; color: #374151;""><strong>Email:</strong> {{OrganizerContactEmail}}</p>
                            {{#if OrganizerContactPhone}}<p style=""margin: 0; font-size: 14px; color: #374151;""><strong>Phone:</strong> {{OrganizerContactPhone}}</p>{{/if}}
                        </td>
                    </tr>
                </table>
                {{/if}}

                <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                    Best regards,<br>
                    <strong style=""color: #374151;"">The LankaConnect Team</strong>
                </p>");

            var text = @"Hi {{UserName}},

Your sign-up commitment has been cancelled.

Event: {{EventTitle}}
Item: {{ItemDescription}}
Quantity: {{Quantity}}
Date: {{EventDateTime}}
Location: {{EventLocation}}

View Event Details: {{EventDetailsUrl}}

{{#if HasOrganizerContact}}
Event Organizer Contact:
Name: {{OrganizerContactName}}
Email: {{OrganizerContactEmail}}
{{#if OrganizerContactPhone}}Phone: {{OrganizerContactPhone}}{{/if}}
{{/if}}

Best regards,
The LankaConnect Team";

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = '{EscapeSql(text)}',
                    updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-cancellation';
            ");
        }

        private static string GetStandardTemplate(string title, string body)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f3f4f6; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f3f4f6;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 600px; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <!-- Header with gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #ea580c 0%, #9f1239 50%, #065f46 100%); padding: 32px 40px; text-align: center;"">
                            <h1 style=""margin: 0; color: #ffffff; font-size: 28px; font-weight: 700;"">{title}</h1>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style=""padding: 40px;"">
                            {body}
                        </td>
                    </tr>

                    <!-- Footer with gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #ea580c 0%, #9f1239 50%, #065f46 100%); padding: 24px 40px; text-align: center;"">
                            <p style=""margin: 0 0 4px 0; color: #ffffff; font-size: 18px; font-weight: 700;"">LankaConnect</p>
                            <p style=""margin: 0; color: rgba(255,255,255,0.8); font-size: 14px;"">Sri Lankan Community Hub</p>
                            <p style=""margin: 16px 0 0 0; color: rgba(255,255,255,0.6); font-size: 12px;"">&copy; {{{{Year}}}} LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private static string EscapeSql(string value)
        {
            return value.Replace("'", "''");
        }
    }
}
