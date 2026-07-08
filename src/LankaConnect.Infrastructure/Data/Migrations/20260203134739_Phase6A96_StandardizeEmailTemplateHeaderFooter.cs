using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.96: Standardize all email templates with consistent header and footer.
    ///
    /// Changes:
    /// 1. Apply gradient header (orange to red to green) to all templates
    /// 2. Apply gradient footer with "LankaConnect" / "Sri Lankan Community Hub" to all templates
    /// 3. Remove old footer text: "This email was sent by LankaConnect..."
    /// 4. Make templates responsive with full-width design
    /// </summary>
    public partial class Phase6A96_StandardizeEmailTemplateHeaderFooter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase A: Remove old footer text from templates that have correct header/footer
            // These templates already have the gradient but contain the old text
            RemoveOldFooterText(migrationBuilder);

            // Phase B: Rebuild templates that need full restructure
            RebuildOrganizerCustomEmail(migrationBuilder);
            RebuildAccountActivatedTemplate(migrationBuilder);
            RebuildAccountDeactivatedTemplate(migrationBuilder);
            RebuildAccountLockedTemplate(migrationBuilder);
            RebuildAccountUnlockedTemplate(migrationBuilder);
            RebuildAttendeesAddedTemplate(migrationBuilder);
            RebuildEventApprovalTemplate(migrationBuilder);
            RebuildNewsletterSubscriptionConfirmationTemplate(migrationBuilder);
            RebuildPreliminaryRegistrationTemplate(migrationBuilder);
            RebuildRefundCompletedTemplate(migrationBuilder);
            RebuildRefundRequestedTemplate(migrationBuilder);
            RebuildSignupCancellationTemplate(migrationBuilder);
            RebuildSupportTicketConfirmationTemplate(migrationBuilder);
            RebuildSupportTicketReplyTemplate(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down migration is complex for template changes - log a warning
            // In practice, we would need to store the old templates to restore them
            migrationBuilder.Sql(@"
                -- Phase 6A.96 Down: Templates cannot be fully reverted automatically.
                -- The old templates would need to be manually restored from backup.
                SELECT 'Phase 6A.96 rollback: Email template changes cannot be automatically reverted.' as warning;
            ");
        }

        private void RemoveOldFooterText(MigrationBuilder migrationBuilder)
        {
            // Remove "This email was sent by LankaConnect..." text from templates
            var templatesToFix = new[]
            {
                "template-event-cancellation-notifications",
                "template-event-details-publication",
                "template-event-registration-cancellation",
                "template-event-reminder",
                "template-free-event-registration-confirmation",
                "template-new-event-publication",
                "template-paid-event-registration-confirmation-with-ticket",
                "template-signup-list-commitment-confirmation",
                "template-signup-list-commitment-update"
            };

            foreach (var templateName in templatesToFix)
            {
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REPLACE(
                        html_template,
                        '<p style=""margin: 10px 0 0 0; font-size: 12px; color: #9ca3af;"">This email was sent by LankaConnect. If you have any questions, please contact your event organizer.</p>',
                        ''
                    ),
                    updated_at = NOW()
                    WHERE name = '{templateName}';
                ");
            }
        }

        private void RebuildOrganizerCustomEmail(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "{{Subject}}",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">{{Content}}</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #ea580c;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Event Details</p>
                                        <p style=""margin: 0; font-size: 14px; color: #6b7280;"">{{EventTitle}}</p>
                                        <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;"">{{EventDateTime}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">{{OrganizerName}}</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = 'OrganizerCustomEmail';
            ");
        }

        private void RebuildAccountActivatedTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Account Activated",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Great news! Your LankaConnect account has been activated by an administrator.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; padding: 20px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">✓</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 18px; font-weight: 600; color: #059669;"">Account Active</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">You can now access all features of your account. Log in to get started!</p>

                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 14px 28px; border-radius: 8px;"">
                                        <a href=""{{LoginUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;"">Log In Now</a>
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
                    updated_at = NOW()
                WHERE name = 'template-account-activated-by-admin';
            ");
        }

        private void RebuildAccountDeactivatedTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Account Deactivated",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your LankaConnect account has been deactivated by an administrator.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #fef2f2; padding: 20px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">⚠</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 18px; font-weight: 600; color: #dc2626;"">Account Deactivated</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">If you believe this was done in error or have questions, please contact our support team.</p>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = 'template-account-deactivated-by-admin';
            ");
        }

        private void RebuildAccountLockedTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Account Locked",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your LankaConnect account has been temporarily locked by an administrator.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #fef3c7; padding: 20px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">🔒</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 18px; font-weight: 600; color: #b45309;"">Account Locked</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">If you have questions about this action, please contact our support team at <a href=""mailto:{{SupportEmail}}"" style=""color: #ea580c; text-decoration: none;"">{{SupportEmail}}</a>.</p>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = 'template-account-locked-by-admin';
            ");
        }

        private void RebuildAccountUnlockedTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Account Unlocked",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Great news! Your LankaConnect account has been unlocked.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; padding: 20px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">🔓</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 18px; font-weight: 600; color: #059669;"">Account Unlocked</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">You can now log in and access your account normally.</p>

                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 14px 28px; border-radius: 8px;"">
                                        <a href=""{{LoginUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;"">Log In Now</a>
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
                    updated_at = NOW()
                WHERE name = 'template-account-unlocked-by-admin';
            ");
        }

        private void RebuildAttendeesAddedTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Attendees Added",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your additional attendees have been successfully added to your registration.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #ea580c;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Event Details</p>
                                        <p style=""margin: 0; font-size: 16px; color: #111827; font-weight: 500;"">{{EventTitle}}</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 14px; color: #6b7280;"">{{EventDateTime}}</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">New Attendees Added</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{NewAttendeesCount}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0; border-bottom: 1px solid #e5e7eb;"">
                                        <span style=""color: #6b7280;"">Total Attendees</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{TotalAttendeesCount}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">Amount Charged</span>
                                        <span style=""float: right; font-weight: 600; color: #059669;"">${{AmountCharged}}</span>
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
                    updated_at = NOW()
                WHERE name = 'template-attendees-added-confirmation';
            ");
        }

        private void RebuildEventApprovalTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Event Approved",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{OrganizerName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Great news! Your event has been approved and is now published on LankaConnect.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; padding: 20px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">✓</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 18px; font-weight: 600; color: #059669;"">Event Approved!</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #ea580c;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Event Details</p>
                                        <p style=""margin: 0; font-size: 16px; color: #111827; font-weight: 500;"">{{EventTitle}}</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 14px; color: #6b7280;"">{{EventDateTime}}</p>
                                        <p style=""margin: 4px 0 0 0; font-size: 14px; color: #6b7280;"">{{EventLocation}}</p>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0;"">
                                <tr>
                                    <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 14px 28px; border-radius: 8px;"">
                                        <a href=""{{EventUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;"">View Your Event</a>
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
                    updated_at = NOW()
                WHERE name = 'template-event-approval';
            ");
        }

        private void RebuildNewsletterSubscriptionConfirmationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Newsletter Subscription",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi there,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Please confirm your subscription to the LankaConnect newsletter.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0; text-align: center;"">
                                <tr>
                                    <td>
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 auto;"">
                                            <tr>
                                                <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 16px 32px; border-radius: 8px;"">
                                                    <a href=""{{ConfirmationUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;"">Confirm Subscription</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0; font-size: 14px; color: #6b7280;"">If you didn't request this subscription, you can safely ignore this email.</p>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = 'template-newsletter-subscription-confirmation';
            ");
        }

        private void RebuildPreliminaryRegistrationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "{{EventTitle}}",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your registration is reserved! Please complete your payment to confirm your spot.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #fef3c7; padding: 20px; border-radius: 8px; text-align: center;"">
                                        <span style=""font-size: 48px;"">⏳</span>
                                        <p style=""margin: 10px 0 0 0; font-size: 18px; font-weight: 600; color: #b45309;"">Payment Pending</p>
                                        <p style=""margin: 8px 0 0 0; font-size: 14px; color: #92400e;"">Expires: {{ExpirationTime}}</p>
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
                                        <span style=""color: #6b7280;"">Date</span>
                                        <span style=""float: right; font-weight: 600; color: #111827;"">{{EventDateTime}}</span>
                                    </td>
                                </tr>
                                <tr>
                                    <td style=""padding: 12px 0;"">
                                        <span style=""color: #6b7280;"">Amount Due</span>
                                        <span style=""float: right; font-weight: 600; color: #059669;"">${{TotalAmount}}</span>
                                    </td>
                                </tr>
                            </table>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0; text-align: center;"">
                                <tr>
                                    <td>
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 auto;"">
                                            <tr>
                                                <td style=""background: linear-gradient(to right, #ea580c 0%, #9f1239 100%); padding: 16px 32px; border-radius: 8px;"">
                                                    <a href=""{{PaymentUrl}}"" style=""color: #ffffff; text-decoration: none; font-weight: 600; font-size: 16px;"">Complete Payment</a>
                                                </td>
                                            </tr>
                                        </table>
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
                    updated_at = NOW()
                WHERE name = 'template-preliminary-registration-payment-pending';
            ");
        }

        private void RebuildRefundCompletedTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
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

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = 'template-refund-completed';
            ");
        }

        private void RebuildRefundRequestedTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
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

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Questions? Contact the organizer:<br>
                                <strong>{{OrganizerContactName}}</strong> - {{OrganizerContactEmail}}
                            </p>

                            <p style=""margin: 20px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = 'template-refund-requested';
            ");
        }

        private void RebuildSignupCancellationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Sign-Up Cancelled",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Your sign-up commitment has been cancelled.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #ea580c;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Event</p>
                                        <p style=""margin: 0; font-size: 16px; color: #111827; font-weight: 500;"">{{EventTitle}}</p>
                                        <p style=""margin: 12px 0 0 0; font-size: 14px; font-weight: 600; color: #374151;"">Item</p>
                                        <p style=""margin: 0; font-size: 14px; color: #6b7280;"">{{ItemDescription}}</p>
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
                    updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-cancellation';
            ");
        }

        private void RebuildSupportTicketConfirmationTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Support Request Received",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Thank you for contacting us. We have received your support request and will get back to you as soon as possible.</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #f9fafb; padding: 20px; border-radius: 8px; border-left: 4px solid #ea580c;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #374151;"">Your Message</p>
                                        <p style=""margin: 0; font-size: 14px; color: #6b7280; white-space: pre-wrap;"">{{Message}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0; font-size: 14px; color: #6b7280;"">Our team typically responds within 24-48 hours.</p>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Support Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = 'template-support-ticket-confirmation';
            ");
        }

        private void RebuildSupportTicketReplyTemplate(MigrationBuilder migrationBuilder)
        {
            var html = GetStandardTemplate(
                "Support Reply",
                @"<p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">Hi <strong>{{UserName}}</strong>,</p>

                            <p style=""margin: 0 0 20px 0; font-size: 16px; color: #374151;"">We have responded to your support request:</p>

                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td style=""background: #ecfdf5; padding: 20px; border-radius: 8px; border-left: 4px solid #059669;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #065f46;"">Our Response</p>
                                        <p style=""margin: 0; font-size: 14px; color: #374151; white-space: pre-wrap;"">{{ReplyMessage}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0; font-size: 14px; color: #6b7280;"">If you have any follow-up questions, please reply to this email.</p>

                            <p style=""margin: 30px 0 0 0; font-size: 14px; color: #6b7280;"">
                                Best regards,<br>
                                <strong style=""color: #374151;"">The LankaConnect Support Team</strong>
                            </p>");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = '{EscapeSql(html)}',
                    updated_at = NOW()
                WHERE name = 'template-support-ticket-reply';
            ");
        }

        /// <summary>
        /// Creates a standard email template with consistent header and footer.
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
