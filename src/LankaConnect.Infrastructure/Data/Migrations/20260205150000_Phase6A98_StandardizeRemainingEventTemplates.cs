using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.98: Standardize remaining 7 event templates that were missed by Phase6A96.
    ///
    /// Root Cause Analysis:
    /// Phase6A96 only called RemoveOldFooterText() for these templates instead of fully rebuilding them.
    /// This left them with the old 600px layout while other templates got the new 850px responsive layout.
    ///
    /// Templates Fixed:
    /// 1. template-new-event-publication
    /// 2. template-event-details-publication
    /// 3. template-event-cancellation-notifications
    /// 4. template-event-registration-cancellation
    /// 5. template-event-reminder
    /// 6. template-free-event-registration-confirmation
    /// 7. template-paid-event-registration-confirmation-with-ticket
    ///
    /// All templates now use:
    /// - 850px max-width (responsive)
    /// - Gradient header (orange → red → green)
    /// - Gradient footer with "LankaConnect" / "Sri Lankan Community Hub"
    /// - Consistent padding and styling
    /// </summary>
    public partial class Phase6A98_StandardizeRemainingEventTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Rebuild template-new-event-publication
            RebuildNewEventPublicationTemplate(migrationBuilder);

            // 2. Rebuild template-event-details-publication
            RebuildEventDetailsPublicationTemplate(migrationBuilder);

            // 3. Rebuild template-event-cancellation-notifications
            RebuildEventCancellationTemplate(migrationBuilder);

            // 4. Rebuild template-event-registration-cancellation
            RebuildRegistrationCancellationTemplate(migrationBuilder);

            // 5. Rebuild template-event-reminder
            RebuildEventReminderTemplate(migrationBuilder);

            // 6. Rebuild template-free-event-registration-confirmation
            RebuildFreeEventRegistrationTemplate(migrationBuilder);

            // 7. Rebuild template-paid-event-registration-confirmation-with-ticket
            RebuildPaidEventRegistrationTemplate(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration would restore old 600px templates
            // Not implemented as the new layout is preferred
            migrationBuilder.Sql(@"
                -- Phase 6A.98 Down: Templates cannot be automatically reverted.
                SELECT 'Phase 6A.98 rollback requires manual template restoration.' as warning;
            ");
        }

        #region Template Rebuild Methods

        private void RebuildNewEventPublicationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "New Event Published",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">A new event has been published that you might be interested in!</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #fef3c7, #fef9c3); padding: 24px; border-radius: 8px; border-left: 4px solid #f59e0b;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 20px; font-weight: 700; color: #92400e;"">{{EventTitle}}</p>
                                        <p style=""margin: 0; font-size: 14px; color: #78350f;"">{{EventDescription}}</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">📅 Date</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">📍 Location</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventLocation}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">🎟️ Price</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{TicketPrice}}</span>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = 'Hi {{{{UserName}}}},

A new event has been published that you might be interested in!

EVENT: {{{{EventTitle}}}}
{{{{EventDescription}}}}

DETAILS
Date: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}
Price: {{{{TicketPrice}}}}

View event details: {{{{EventDetailsUrl}}}}

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-new-event-publication';
            ");
        }

        private void RebuildEventDetailsPublicationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Event Details Updated",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">The event details have been updated. Here's the latest information:</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: #eff6ff; padding: 24px; border-radius: 8px; border-left: 4px solid #3b82f6;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 20px; font-weight: 700; color: #1e40af;"">{{EventTitle}}</p>
                                        <p style=""margin: 0; font-size: 14px; color: #1e3a8a;"">{{EventDescription}}</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">📅 Date</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">📍 Location</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventLocation}}</span>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Updated Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = 'Hi {{{{UserName}}}},

The event details have been updated. Here''s the latest information:

EVENT: {{{{EventTitle}}}}
{{{{EventDescription}}}}

DETAILS
Date: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}

View updated details: {{{{EventDetailsUrl}}}}

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-event-details-publication';
            ");
        }

        private void RebuildEventCancellationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Event Cancelled",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">We regret to inform you that the following event has been cancelled:</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: #fef2f2; padding: 24px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">❌</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 20px; font-weight: 700; color: #dc2626;"">{{EventTitle}}</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 14px; color: #991b1b;"">This event has been cancelled</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">Originally Scheduled</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">Location</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventLocation}}</span>
                                    </td>
                                </tr>
                            </table>

                            {{#if CancellationReason}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 6px;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Cancellation Reason:</p>
                                        <p style=""margin: 0; font-size: 14px; color: #4b5563;"">{{CancellationReason}}</p>
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            {{#if RefundStatus}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; border-left: 4px solid #10b981; padding: 15px; border-radius: 0 8px 8px 0;"">
                                        <p style=""margin: 0; color: #065f46; font-size: 14px;""><strong>Refund Status:</strong> {{RefundStatus}}</p>
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

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
                                We apologize for any inconvenience.<br><br>
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = 'Hi {{{{UserName}}}},

We regret to inform you that the following event has been cancelled:

EVENT: {{{{EventTitle}}}}
Originally Scheduled: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}

{{{{#if CancellationReason}}}}
Cancellation Reason: {{{{CancellationReason}}}}
{{{{/if}}}}

{{{{#if RefundStatus}}}}
Refund Status: {{{{RefundStatus}}}}
{{{{/if}}}}

{{{{#if HasOrganizerContact}}}}
Questions? Contact the organizer:
{{{{OrganizerContactName}}}} - {{{{OrganizerContactEmail}}}}
{{{{/if}}}}

We apologize for any inconvenience.

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-event-cancellation-notifications';
            ");
        }

        private void RebuildRegistrationCancellationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Registration Cancelled",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your registration for the following event has been cancelled:</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: #fef2f2; padding: 24px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">🎫</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 20px; font-weight: 700; color: #dc2626;"">{{EventTitle}}</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 14px; color: #991b1b;"">Registration Cancelled</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">Event Date</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">Location</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventLocation}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">Cancelled At</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{CancelledAt}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">Reason</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{CancellationReason}}</span>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; border-radius: 0 8px 8px 0;"">
                                        <p style=""margin: 0; color: #1e40af; font-size: 14px;""><strong>Refund Status:</strong> {{RefundStatus}}</p>
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

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = 'Hi {{{{UserName}}}},

Your registration for the following event has been cancelled:

EVENT: {{{{EventTitle}}}}

CANCELLATION DETAILS
Event Date: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}
Cancelled At: {{{{CancelledAt}}}}
Reason: {{{{CancellationReason}}}}

REFUND STATUS
{{{{RefundStatus}}}}

{{{{#if HasOrganizerContact}}}}
Questions? Contact the organizer:
{{{{OrganizerContactName}}}} - {{{{OrganizerContactEmail}}}}
{{{{/if}}}}

View event details: {{{{EventDetailsUrl}}}}

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-event-registration-cancellation';
            ");
        }

        private void RebuildEventReminderTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Event Reminder",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">This is a friendly reminder about your upcoming event!</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #fef3c7, #fef9c3); padding: 24px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">⏰</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 20px; font-weight: 700; color: #92400e;"">{{EventTitle}}</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 16px; color: #78350f;"">Coming up soon!</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">📅 Date & Time</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">📍 Location</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventLocation}}</span>
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

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                We look forward to seeing you there!<br><br>
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = 'Hi {{{{UserName}}}},

This is a friendly reminder about your upcoming event!

EVENT: {{{{EventTitle}}}}

DETAILS
Date & Time: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}

{{{{#if HasOrganizerContact}}}}
Questions? Contact the organizer:
{{{{OrganizerContactName}}}} - {{{{OrganizerContactEmail}}}}
{{{{/if}}}}

View event details: {{{{EventDetailsUrl}}}}

We look forward to seeing you there!

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-event-reminder';
            ");
        }

        private void RebuildFreeEventRegistrationTemplate(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.98: Uses parameters from FreeEventRegistrationEmailParams.ToDictionary():
            // UserName, EventTitle, EventStartDate, EventStartTime, EventDateTime, EventLocation,
            // EventDetailsUrl, SignUpListsUrl, RegistrationDate, HasAttendeeDetails, Attendees,
            // HasOrganizerContact, OrganizerContactName, OrganizerContactEmail, OrganizerContactPhone,
            // HasContactInfo, ContactEmail, ContactPhone, HasEventImage, EventImageUrl
            var html = GetStandardTemplate(
                "Registration Confirmed",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Thank you for registering! Your spot has been confirmed for:</p>

                            {{#if HasEventImage}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td>
                                        <img src=""{{EventImageUrl}}"" alt=""{{EventTitle}}"" style=""width: 100%; max-width: 100%; height: auto; border-radius: 8px;"" />
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; padding: 24px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">✓</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 20px; font-weight: 700; color: #059669;"">{{EventTitle}}</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 14px; color: #065f46;"">You're all set!</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">📅 Date & Time</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">📍 Location</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventLocation}}</span>
                                    </td>
                                </tr>
                            </table>

                            {{#if HasAttendeeDetails}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 6px;"">
                                        <p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 600; color: #374151;"">Registered Attendees:</p>
                                        {{{Attendees}}}
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            {{#if HasOrganizerContact}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #f3f4f6; padding: 20px; border-radius: 6px;"">
                                        <p style=""margin: 0 0 10px 0; font-size: 14px; font-weight: 600; color: #374151;"">Contact Information:</p>
                                        <p style=""margin: 0; font-size: 14px; color: #4b5563;""><strong>{{OrganizerContactName}}</strong> - <a href=""mailto:{{OrganizerContactEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{OrganizerContactEmail}}</a></p>
                                        {{#if OrganizerContactPhone}}<p style=""margin: 8px 0 0 0; font-size: 14px; color: #4b5563;"">Phone: {{OrganizerContactPhone}}</p>{{/if}}
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0 20px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(to right, #ea580c, #9f1239); color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 14px;"">View Event Details</a>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                We look forward to seeing you at the event!<br><br>
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = 'Hi {{{{UserName}}}},

Thank you for registering! Your spot has been confirmed for:

EVENT: {{{{EventTitle}}}}

DETAILS
Date & Time: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}

{{{{#if HasAttendeeDetails}}}}
Registered Attendees:
{{{{Attendees}}}}
{{{{/if}}}}

{{{{#if HasOrganizerContact}}}}
Contact Information:
{{{{OrganizerContactName}}}} - {{{{OrganizerContactEmail}}}}
{{{{#if OrganizerContactPhone}}}}Phone: {{{{OrganizerContactPhone}}}}{{{{/if}}}}
{{{{/if}}}}

View event details: {{{{EventDetailsUrl}}}}

We look forward to seeing you at the event!

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-free-event-registration-confirmation';
            ");
        }

        private void RebuildPaidEventRegistrationTemplate(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.98: Uses parameters from ResendTicketEmailCommandHandler and PaymentCompletedEventHandler:
            // UserName, EventTitle, EventDateTime, EventLocation, AmountPaid, PaymentIntentId, PaymentDate,
            // TicketCode, TicketExpiryDate, Attendees, HasAttendeeDetails, EventImageUrl, HasEventImage,
            // ContactEmail, ContactPhone, HasContactInfo
            var html = GetStandardTemplate(
                "Payment Confirmed",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Thank you for your purchase! Your ticket has been confirmed.</p>

                            {{#if HasEventImage}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td>
                                        <img src=""{{EventImageUrl}}"" alt=""{{EventTitle}}"" style=""width: 100%; max-width: 100%; height: auto; border-radius: 8px;"" />
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; padding: 24px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">🎟️</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 20px; font-weight: 700; color: #059669;"">{{EventTitle}}</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 14px; color: #065f46;"">Your ticket is confirmed!</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">📅 Date & Time</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">📍 Location</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventLocation}}</span>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 6px; text-align: center;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Ticket Code</p>
                                        <p style=""margin: 0; font-family: monospace; background: #e5e7eb; padding: 8px 16px; border-radius: 4px; font-size: 18px; font-weight: 700; display: inline-block;"">{{TicketCode}}</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 12px; color: #6b7280;"">Valid until {{TicketExpiryDate}}</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; border-left: 4px solid #10b981; padding: 15px; border-radius: 0 8px 8px 0;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #065f46;"">Payment Confirmation</p>
                                        <p style=""margin: 0; font-size: 14px; color: #047857;"">Amount Paid: <strong>{{AmountPaid}}</strong></p>
                                        <p style=""margin: 4px 0 0 0; font-size: 12px; color: #6b7280;"">Payment ID: {{PaymentIntentId}}</p>
                                        <p style=""margin: 4px 0 0 0; font-size: 12px; color: #6b7280;"">Date: {{PaymentDate}}</p>
                                    </td>
                                </tr>
                            </table>

                            {{#if HasAttendeeDetails}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 6px;"">
                                        <p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 600; color: #374151;"">Registered Attendees:</p>
                                        {{{Attendees}}}
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            {{#if HasContactInfo}}
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: #f3f4f6; padding: 20px; border-radius: 6px;"">
                                        <p style=""margin: 0 0 10px 0; font-size: 14px; font-weight: 600; color: #374151;"">Contact Information:</p>
                                        <p style=""margin: 0; font-size: 14px; color: #4b5563;"">Email: <a href=""mailto:{{ContactEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{ContactEmail}}</a></p>
                                        {{#if ContactPhone}}<p style=""margin: 8px 0 0 0; font-size: 14px; color: #4b5563;"">Phone: {{ContactPhone}}</p>{{/if}}
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                We look forward to seeing you at the event!<br><br>
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    text_template = 'Hi {{{{UserName}}}},

Thank you for your purchase! Your ticket has been confirmed.

EVENT: {{{{EventTitle}}}}

TICKET DETAILS
Date & Time: {{{{EventDateTime}}}}
Location: {{{{EventLocation}}}}

YOUR TICKET
Ticket Code: {{{{TicketCode}}}}
Valid Until: {{{{TicketExpiryDate}}}}

PAYMENT CONFIRMATION
Amount Paid: {{{{AmountPaid}}}}
Payment ID: {{{{PaymentIntentId}}}}
Payment Date: {{{{PaymentDate}}}}

{{{{#if HasAttendeeDetails}}}}
Registered Attendees:
{{{{Attendees}}}}
{{{{/if}}}}

{{{{#if HasContactInfo}}}}
Contact Information:
Email: {{{{ContactEmail}}}}
{{{{#if ContactPhone}}}}Phone: {{{{ContactPhone}}}}{{{{/if}}}}
{{{{/if}}}}

Your ticket is attached to this email as a PDF. Please present it at the event entrance.

We look forward to seeing you at the event!

Best regards,
The LankaConnect Team',
                    updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
            ");
        }

        #endregion

        #region Helper Methods

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

        #endregion
    }
}
