using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.108: Add 3 email templates for form response notifications.
    ///
    /// Templates:
    /// 1. template-form-response-confirmation - Sent when user submits a form response
    /// 2. template-form-response-update - Sent when user updates their response
    /// 3. template-form-response-cancellation - Sent when user deletes their response
    ///
    /// Pattern: Mirrors signup list commitment templates (Phase 6A.96 standard)
    /// - Gradient header/footer (orange → red → green)
    /// - Conditional sections for event image, organizer contact, form description
    /// - Response summary (max 5 Q&A pairs)
    /// - Edit link with access token for anonymous users
    /// </summary>
    public partial class Phase6A108_AddFormResponseEmailTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Template 1: Form Response Confirmation
            var confirmationHtml = GetStandardTemplate(
                "Form Response Received",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Thank you for filling out <strong>{{FormTitle}}</strong> for <strong>{{EventTitle}}</strong>!</p>

                <!-- FORM DESCRIPTION (conditional) -->
                {{#HasFormDescription}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                    <tr>
                        <td style=""background: #eff6ff; padding: 16px; border-radius: 8px; border-left: 4px solid #3b82f6;"">
                            <p style=""margin: 0; font-size: 14px; color: #1e40af; font-style: italic;"">{{FormDescription}}</p>
                        </td>
                    </tr>
                </table>
                {{/HasFormDescription}}

                <!-- RESPONSE SUMMARY -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #ea580c;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Your Responses</p>
                            <p style=""margin: 0; font-size: 14px; color: #6b7280; white-space: pre-wrap;"">{{ResponseSummary}}</p>
                        </td>
                    </tr>
                </table>

                <!-- EVENT DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #166534;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Event Details</p>
                            <p style=""margin: 0; font-size: 16px; color: #111827; font-weight: 500;"">{{EventTitle}}</p>
                            <p style=""margin: 8px 0 0 0; font-size: 14px; color: #6b7280;""><strong>Date:</strong> {{EventDateTime}}</p>
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;""><strong>Location:</strong> {{EventLocation}}</p>
                        </td>
                    </tr>
                </table>

                <!-- RESPONSE DEADLINE (conditional) -->
                {{#HasResponseDeadline}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                    <tr>
                        <td style=""background: #fef3c7; padding: 16px; border-radius: 8px; border-left: 4px solid #f59e0b; text-align: center;"">
                            <p style=""margin: 0; font-size: 14px; color: #92400e;""><strong>Response Deadline:</strong> {{ResponseDeadline}}</p>
                        </td>
                    </tr>
                </table>
                {{/HasResponseDeadline}}

                <!-- EDIT BUTTON -->
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 14px 28px; border-radius: 8px; text-align: center;"">
                            <a href=""{{EditFormUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px; display: block;"">Edit Your Response</a>
                        </td>
                    </tr>
                </table>

                <!-- ORGANIZER CONTACT (conditional) -->
                {{#HasOrganizerContact}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #6366f1;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Organizer Contact</p>
                            <p style=""margin: 0; font-size: 14px; color: #6b7280;""><strong>{{OrganizerContactName}}</strong></p>
                            {{#OrganizerContactEmail}}
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;"">Email: <a href=""mailto:{{OrganizerContactEmail}}"" style=""color: #4f46e5;"">{{OrganizerContactEmail}}</a></p>
                            {{/OrganizerContactEmail}}
                            {{#OrganizerContactPhone}}
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;"">Phone: {{OrganizerContactPhone}}</p>
                            {{/OrganizerContactPhone}}
                        </td>
                    </tr>
                </table>
                {{/HasOrganizerContact}}

                <p style=""margin: 20px 0; font-size: 14px; color: #6b7280;"">
                    <a href=""{{EventDetailsUrl}}"" style=""color: #ea580c; text-decoration: none; font-weight: 500;"">View Event Details →</a>
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
                    'template-form-response-confirmation',
                    'Phase 6A.108: Confirmation email when user submits a form response',
                    'Form Response Received - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Thank you for filling out {{{{FormTitle}}}} for {{{{EventTitle}}}}!

YOUR RESPONSES
--------------
{{{{ResponseSummary}}}}

EVENT DETAILS
-------------
Event: {{{{EventTitle}}}}
Date: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}

You can edit your response at any time using this link:
{{{{EditFormUrl}}}}

View event details: {{{{EventDetailsUrl}}}}

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(confirmationHtml)}',
                    'FormResponseConfirmation',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-form-response-confirmation'
                );
            ");

            // Template 2: Form Response Update
            var updateHtml = GetStandardTemplate(
                "Form Response Updated",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your response to <strong>{{FormTitle}}</strong> for <strong>{{EventTitle}}</strong> has been updated.</p>

                <!-- FORM DESCRIPTION (conditional) -->
                {{#HasFormDescription}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                    <tr>
                        <td style=""background: #eff6ff; padding: 16px; border-radius: 8px; border-left: 4px solid #3b82f6;"">
                            <p style=""margin: 0; font-size: 14px; color: #1e40af; font-style: italic;"">{{FormDescription}}</p>
                        </td>
                    </tr>
                </table>
                {{/HasFormDescription}}

                <!-- UPDATED RESPONSE SUMMARY -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #ecfdf5; padding: 20px; border-radius: 8px; border-left: 4px solid #059669;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #065f46;"">Updated Responses</p>
                            <p style=""margin: 0; font-size: 14px; color: #047857; white-space: pre-wrap;"">{{ResponseSummary}}</p>
                            <p style=""margin: 12px 0 0 0; font-size: 12px; color: #6b7280;"">Updated: {{UpdatedAt}}</p>
                        </td>
                    </tr>
                </table>

                <!-- EVENT DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #166534;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Event Details</p>
                            <p style=""margin: 0; font-size: 16px; color: #111827; font-weight: 500;"">{{EventTitle}}</p>
                            <p style=""margin: 8px 0 0 0; font-size: 14px; color: #6b7280;""><strong>Date:</strong> {{EventDateTime}}</p>
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;""><strong>Location:</strong> {{EventLocation}}</p>
                        </td>
                    </tr>
                </table>

                <!-- RESPONSE DEADLINE (conditional) -->
                {{#HasResponseDeadline}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                    <tr>
                        <td style=""background: #fef3c7; padding: 16px; border-radius: 8px; border-left: 4px solid #f59e0b; text-align: center;"">
                            <p style=""margin: 0; font-size: 14px; color: #92400e;""><strong>Response Deadline:</strong> {{ResponseDeadline}}</p>
                        </td>
                    </tr>
                </table>
                {{/HasResponseDeadline}}

                <!-- EDIT AGAIN BUTTON -->
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 14px 28px; border-radius: 8px; text-align: center;"">
                            <a href=""{{EditFormUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px; display: block;"">Edit Again</a>
                        </td>
                    </tr>
                </table>

                <!-- ORGANIZER CONTACT (conditional) -->
                {{#HasOrganizerContact}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #6366f1;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Organizer Contact</p>
                            <p style=""margin: 0; font-size: 14px; color: #6b7280;""><strong>{{OrganizerContactName}}</strong></p>
                            {{#OrganizerContactEmail}}
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;"">Email: <a href=""mailto:{{OrganizerContactEmail}}"" style=""color: #4f46e5;"">{{OrganizerContactEmail}}</a></p>
                            {{/OrganizerContactEmail}}
                            {{#OrganizerContactPhone}}
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;"">Phone: {{OrganizerContactPhone}}</p>
                            {{/OrganizerContactPhone}}
                        </td>
                    </tr>
                </table>
                {{/HasOrganizerContact}}

                <p style=""margin: 20px 0; font-size: 14px; color: #6b7280;"">
                    <a href=""{{EventDetailsUrl}}"" style=""color: #ea580c; text-decoration: none; font-weight: 500;"">View Event Details →</a>
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
                    'template-form-response-update',
                    'Phase 6A.108: Notification email when user updates their form response',
                    'Form Response Updated - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Your response to {{{{FormTitle}}}} for {{{{EventTitle}}}} has been updated.

UPDATED RESPONSES
-----------------
{{{{ResponseSummary}}}}
Updated: {{{{UpdatedAt}}}}

EVENT DETAILS
-------------
Event: {{{{EventTitle}}}}
Date: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}

You can edit your response again at:
{{{{EditFormUrl}}}}

View event details: {{{{EventDetailsUrl}}}}

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(updateHtml)}',
                    'FormResponseUpdate',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-form-response-update'
                );
            ");

            // Template 3: Form Response Cancellation
            var cancellationHtml = GetStandardTemplate(
                "Form Response Cancelled",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your response to <strong>{{FormTitle}}</strong> for <strong>{{EventTitle}}</strong> has been cancelled.</p>

                <!-- CANCELLATION NOTICE -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #fef2f2; padding: 20px; border-radius: 8px; border-left: 4px solid #dc2626; text-align: center;"">
                            <span style=""font-size: 48px;"">✓</span>
                            <p style=""margin: 10px 0 0 0; font-size: 18px; font-weight: 600; color: #991b1b;"">Response Cancelled</p>
                            <p style=""margin: 8px 0 0 0; font-size: 14px; color: #6b7280;"">Cancelled: {{CancelledAt}}</p>
                        </td>
                    </tr>
                </table>

                <!-- EVENT DETAILS -->
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #166534;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Event Details</p>
                            <p style=""margin: 0; font-size: 16px; color: #111827; font-weight: 500;"">{{EventTitle}}</p>
                            <p style=""margin: 8px 0 0 0; font-size: 14px; color: #6b7280;""><strong>Date:</strong> {{EventDateTime}}</p>
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;""><strong>Location:</strong> {{EventLocation}}</p>
                        </td>
                    </tr>
                </table>

                <!-- FORM DESCRIPTION (conditional) -->
                {{#HasFormDescription}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                    <tr>
                        <td style=""background: #eff6ff; padding: 16px; border-radius: 8px; border-left: 4px solid #3b82f6;"">
                            <p style=""margin: 0; font-size: 14px; color: #1e40af; font-style: italic;"">{{FormDescription}}</p>
                        </td>
                    </tr>
                </table>
                {{/HasFormDescription}}

                <p style=""margin: 30px 0 20px 0; font-size: 14px; color: #6b7280;"">If you cancelled by mistake, you can fill out the form again by visiting the event page.</p>

                <!-- ORGANIZER CONTACT (conditional) -->
                {{#HasOrganizerContact}}
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                    <tr>
                        <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #6366f1;"">
                            <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Organizer Contact</p>
                            <p style=""margin: 0; font-size: 14px; color: #6b7280;""><strong>{{OrganizerContactName}}</strong></p>
                            {{#OrganizerContactEmail}}
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;"">Email: <a href=""mailto:{{OrganizerContactEmail}}"" style=""color: #4f46e5;"">{{OrganizerContactEmail}}</a></p>
                            {{/OrganizerContactEmail}}
                            {{#OrganizerContactPhone}}
                            <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;"">Phone: {{OrganizerContactPhone}}</p>
                            {{/OrganizerContactPhone}}
                        </td>
                    </tr>
                </table>
                {{/HasOrganizerContact}}

                <p style=""margin: 20px 0; font-size: 14px; color: #6b7280;"">
                    <a href=""{{EventDetailsUrl}}"" style=""color: #ea580c; text-decoration: none; font-weight: 500;"">View Event Details →</a>
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
                    'template-form-response-cancellation',
                    'Phase 6A.108: Notification email when user deletes/cancels their form response',
                    'Form Response Cancelled - {{{{EventTitle}}}}',
                    'Hi {{{{UserName}}}},

Your response to {{{{FormTitle}}}} for {{{{EventTitle}}}} has been cancelled.

RESPONSE CANCELLED
------------------
Your form response has been removed.
Cancelled: {{{{CancelledAt}}}}

EVENT DETAILS
-------------
Event: {{{{EventTitle}}}}
Date: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}

If you cancelled by mistake, you can fill out the form again by visiting the event page:
{{{{EventDetailsUrl}}}}

Best regards,
The LankaConnect Team

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{EscapeSql(cancellationHtml)}',
                    'FormResponseCancellation',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-form-response-cancellation'
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Phase 6A.108 email templates
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-form-response-confirmation',
                    'template-form-response-update',
                    'template-form-response-cancellation'
                );
            ");
        }

        /// <summary>
        /// Creates a standard email template with consistent header and footer.
        /// Pattern from Phase 6A.96: Gradient header/footer (orange → red → green).
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

                    <!-- EVENT IMAGE (conditional + graceful fallback) -->
                    {{{{#HasEventImage}}}}
                    <!--[if !mso]><!-->
                    <tr>
                        <td style=""font-size: 0; line-height: 0"">
                            <!--<![endif]-->
                            <img
                                src=""{{{{EventImageUrl}}}}""
                                alt=""{{{{EventTitle}}}}""
                                width=""860""
                                style=""width: 100%; max-height: 300px; object-fit: cover; display: block""
                                onerror=""
                                    this.style.display = 'none';
                                    this.parentElement.style.height = '0';
                                    this.parentElement.style.overflow = 'hidden';
                                ""
                            />
                            <!--[if !mso]><!-->
                        </td>
                    </tr>
                    <!--<![endif]-->
                    {{{{/HasEventImage}}}}

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

        /// <summary>
        /// Escapes single quotes for SQL string literals.
        /// </summary>
        private string EscapeSql(string input)
        {
            return input.Replace("'", "''");
        }
    }
}
