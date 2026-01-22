using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.76: Comprehensive Email Template Migration
    ///
    /// This migration performs TWO operations:
    /// 1. RENAMES all 14 existing email templates to use the new 'template-*' naming convention
    /// 2. CREATES 5 missing email templates that were referenced in code but didn't exist in database
    ///
    /// Templates Renamed (14):
    /// - registration-confirmation → template-free-event-registration-confirmation
    /// - event-published → template-new-event-publication
    /// - member-email-verification → template-membership-email-verification
    /// - event-cancelled-notification → template-event-cancellation-notifications
    /// - registration-cancellation → template-event-registration-cancellation
    /// - newsletter-confirmation → template-newsletter-subscription-confirmation
    /// - newsletter → template-newsletter-notification
    /// - event-details → template-event-details-publication
    /// - signup-commitment-confirmation → template-signup-list-commitment-confirmation
    /// - signup-commitment-updated → template-signup-list-commitment-update
    /// - signup-commitment-cancelled → template-signup-list-commitment-cancellation
    /// - event-approved → template-event-approval
    /// - event-reminder → template-event-reminder
    /// - ticket-confirmation → template-paid-event-registration-confirmation-with-ticket
    ///
    /// Templates Created (5):
    /// - template-password-reset
    /// - template-password-change-confirmation
    /// - template-welcome
    /// - template-anonymous-rsvp-confirmation
    /// - template-organizer-role-approval
    ///
    /// IMPORTANT: Code must be deployed at the same time as this migration runs.
    /// </summary>
    public partial class Phase6A76_RenameAndAddEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // PART 1: RENAME ALL 14 EXISTING TEMPLATES
            // =====================================================

            migrationBuilder.Sql(@"
                -- Rename registration-confirmation to template-free-event-registration-confirmation
                UPDATE communications.email_templates
                SET name = 'template-free-event-registration-confirmation', updated_at = NOW()
                WHERE name = 'registration-confirmation';

                -- Rename event-published to template-new-event-publication
                UPDATE communications.email_templates
                SET name = 'template-new-event-publication', updated_at = NOW()
                WHERE name = 'event-published';

                -- Rename member-email-verification to template-membership-email-verification
                UPDATE communications.email_templates
                SET name = 'template-membership-email-verification', updated_at = NOW()
                WHERE name = 'member-email-verification';

                -- Rename event-cancelled-notification to template-event-cancellation-notifications
                UPDATE communications.email_templates
                SET name = 'template-event-cancellation-notifications', updated_at = NOW()
                WHERE name = 'event-cancelled-notification';

                -- Rename registration-cancellation to template-event-registration-cancellation
                UPDATE communications.email_templates
                SET name = 'template-event-registration-cancellation', updated_at = NOW()
                WHERE name = 'registration-cancellation';

                -- Rename newsletter-confirmation to template-newsletter-subscription-confirmation
                UPDATE communications.email_templates
                SET name = 'template-newsletter-subscription-confirmation', updated_at = NOW()
                WHERE name = 'newsletter-confirmation';

                -- Rename newsletter to template-newsletter-notification
                UPDATE communications.email_templates
                SET name = 'template-newsletter-notification', updated_at = NOW()
                WHERE name = 'newsletter';

                -- Rename event-details to template-event-details-publication
                UPDATE communications.email_templates
                SET name = 'template-event-details-publication', updated_at = NOW()
                WHERE name = 'event-details';

                -- Rename signup-commitment-confirmation to template-signup-list-commitment-confirmation
                UPDATE communications.email_templates
                SET name = 'template-signup-list-commitment-confirmation', updated_at = NOW()
                WHERE name = 'signup-commitment-confirmation';

                -- Rename signup-commitment-updated to template-signup-list-commitment-update
                UPDATE communications.email_templates
                SET name = 'template-signup-list-commitment-update', updated_at = NOW()
                WHERE name = 'signup-commitment-updated';

                -- Rename signup-commitment-cancelled to template-signup-list-commitment-cancellation
                UPDATE communications.email_templates
                SET name = 'template-signup-list-commitment-cancellation', updated_at = NOW()
                WHERE name = 'signup-commitment-cancelled';

                -- Rename event-approved to template-event-approval
                UPDATE communications.email_templates
                SET name = 'template-event-approval', updated_at = NOW()
                WHERE name = 'event-approved';

                -- Rename event-reminder to template-event-reminder
                UPDATE communications.email_templates
                SET name = 'template-event-reminder', updated_at = NOW()
                WHERE name = 'event-reminder';

                -- Rename ticket-confirmation to template-paid-event-registration-confirmation-with-ticket
                UPDATE communications.email_templates
                SET name = 'template-paid-event-registration-confirmation-with-ticket', updated_at = NOW()
                WHERE name = 'ticket-confirmation';
            ");

            // =====================================================
            // PART 2: CREATE 5 MISSING TEMPLATES
            // =====================================================

            // 2.1 Create template-password-reset
            migrationBuilder.Sql(@"
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
                    'template-password-reset',
                    'Email sent when user requests a password reset with a reset link',
                    'Reset Your LankaConnect Password',
                    'Hi {{UserName}},

We received a request to reset your password for your LankaConnect account.

RESET YOUR PASSWORD
-------------------
Click the link below to reset your password:
{{ResetLink}}

This link will expire on: {{ExpiresAt}}

If you did not request a password reset, please ignore this email or contact support if you have concerns.

SECURITY TIP
------------
- Never share your password or reset link with anyone
- LankaConnect will never ask for your password via email or phone
- If you suspect unauthorized access, contact us immediately

---
LankaConnect
Sri Lankan Community Hub
{{SupportEmail}}
© 2025 LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset Password - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Password Reset Request</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi {{UserName}},</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                We received a request to reset your password for your LankaConnect account.
                            </p>

                            <!-- CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""background: #FF6600; border-radius: 6px; text-align: center;"">
                                                    <a href=""{{ResetLink}}"" style=""display: inline-block; padding: 14px 28px; color: white; text-decoration: none; font-weight: bold; font-size: 14px;"">Reset Password</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Expiry Warning -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #fef3c7; border-left: 4px solid #f59e0b; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 15px 20px;"">
                                        <p style=""margin: 0; color: #92400e; font-size: 14px;"">
                                            <strong>Link expires:</strong> {{ExpiresAt}}
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0; color: #666; font-size: 14px; line-height: 1.6;"">
                                If you did not request a password reset, please ignore this email or contact support if you have concerns.
                            </p>

                            <!-- Security Tip Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #1e40af; font-size: 14px;"">Security Tip</p>
                                        <ul style=""margin: 0; padding-left: 20px; color: #666; font-size: 13px; line-height: 1.8;"">
                                            <li>Never share your password or reset link with anyone</li>
                                            <li>LankaConnect will never ask for your password via email</li>
                                        </ul>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
                            <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;"">Sri Lankan Community Hub</p>
                            <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'transactional',
                    'Security',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-password-reset'
                );
            ");

            // 2.2 Create template-password-change-confirmation
            migrationBuilder.Sql(@"
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
                    'template-password-change-confirmation',
                    'Email sent after user successfully resets their password',
                    'Your LankaConnect Password Has Been Changed',
                    'Hi {{UserName}},

This is a confirmation that your LankaConnect password has been successfully changed.

CHANGE DETAILS
--------------
Account: {{UserEmail}}
Changed On: {{ChangeDate}}

If you made this change, no further action is needed.

If you DID NOT make this change, your account may have been compromised. Please:
1. Click here to reset your password immediately: {{LoginUrl}}
2. Contact our support team at {{SupportEmail}}

SECURITY RECOMMENDATIONS
------------------------
- Use a strong, unique password
- Enable two-factor authentication if available
- Never share your login credentials

---
LankaConnect
Sri Lankan Community Hub
{{SupportEmail}}
© 2025 LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Changed - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Password Changed Successfully</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi {{UserName}},</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                This is a confirmation that your LankaConnect password has been <strong style=""color: #10b981;"">successfully changed</strong>.
                            </p>

                            <!-- Change Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #f0fdf4; border-left: 4px solid #10b981; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <h2 style=""margin: 0 0 15px 0; color: #065f46; font-size: 18px;"">Change Details</h2>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Account:</strong> {{UserEmail}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Changed On:</strong> {{ChangeDate}}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0; color: #555; line-height: 1.6;"">
                                If you made this change, no further action is needed.
                            </p>

                            <!-- Warning Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #fef2f2; border-left: 4px solid #ef4444; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #991b1b; font-size: 14px;"">Did not make this change?</p>
                                        <p style=""margin: 0; color: #666; font-size: 13px; line-height: 1.6;"">
                                            Your account may have been compromised. Please reset your password immediately and contact our support team at {{SupportEmail}}.
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <!-- CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""background: #FF6600; border-radius: 6px; text-align: center;"">
                                                    <a href=""{{LoginUrl}}"" style=""display: inline-block; padding: 14px 28px; color: white; text-decoration: none; font-weight: bold; font-size: 14px;"">Login to Your Account</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
                            <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;"">Sri Lankan Community Hub</p>
                            <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'transactional',
                    'Security',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-password-change-confirmation'
                );
            ");

            // 2.3 Create template-welcome (sent AFTER email verification)
            migrationBuilder.Sql(@"
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
                    'template-welcome',
                    'Welcome email sent after user successfully verifies their email address (different from verification email)',
                    'Welcome to LankaConnect!',
                    'Hi {{UserName}},

Welcome to LankaConnect! Your email has been verified and your account is now fully active.

WHAT YOU CAN DO NOW
-------------------
- Discover and register for community events
- Connect with the Sri Lankan community
- Get updates on upcoming events and activities
- Create your member profile

GET STARTED
-----------
Login to your account and start exploring:
{{LoginUrl}}

We are excited to have you as part of our community!

---
LankaConnect
Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Welcome to LankaConnect!</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi {{UserName}},</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                Welcome to <strong style=""color: #8B1538;"">LankaConnect</strong>! Your email has been verified and your account is now <strong style=""color: #10b981;"">fully active</strong>.
                            </p>

                            <!-- What You Can Do Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #1e40af; font-size: 14px;"">What You Can Do Now</p>
                                        <ul style=""margin: 0; padding-left: 20px; color: #666; font-size: 14px; line-height: 1.8;"">
                                            <li>Discover and register for community events</li>
                                            <li>Connect with the Sri Lankan community</li>
                                            <li>Get updates on upcoming events and activities</li>
                                            <li>Create your member profile</li>
                                        </ul>
                                    </td>
                                </tr>
                            </table>

                            <!-- CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""background: #FF6600; border-radius: 6px; text-align: center;"">
                                                    <a href=""{{LoginUrl}}"" style=""display: inline-block; padding: 14px 28px; color: white; text-decoration: none; font-weight: bold; font-size: 14px;"">Start Exploring</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; color: #555; line-height: 1.6;"">
                                We are excited to have you as part of our community!
                            </p>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
                            <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;"">Sri Lankan Community Hub</p>
                            <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'transactional',
                    'System',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-welcome'
                );
            ");

            // 2.4 Create template-anonymous-rsvp-confirmation
            migrationBuilder.Sql(@"
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
                    'template-anonymous-rsvp-confirmation',
                    'RSVP confirmation email for anonymous (non-registered) event attendees',
                    'Your RSVP Confirmation: {{EventTitle}}',
                    'Hi {{UserName}},

Thank you for your RSVP! Your registration has been confirmed.

EVENT DETAILS
-------------
Event: {{EventTitle}}
Date: {{EventStartDate}}
Time: {{EventStartTime}}
Location: {{EventLocation}}

REGISTRATION DETAILS
--------------------
Number of Attendees: {{Quantity}}
Registration Date: {{RegistrationDate}}

{{#if HasAttendeeDetails}}
ATTENDEES
---------
{{#each Attendees}}
- {{Name}} ({{AgeCategory}})
{{/each}}
{{/if}}

{{#if HasContactInfo}}
CONTACT INFORMATION
-------------------
Email: {{ContactEmail}}
{{#if ContactPhone}}Phone: {{ContactPhone}}{{/if}}
{{/if}}

Please save this email for your records. We look forward to seeing you at the event!

---
LankaConnect
Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>RSVP Confirmation - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">RSVP Confirmed!</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi {{UserName}},</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                Thank you for your RSVP! Your registration has been <strong style=""color: #10b981;"">confirmed</strong>.
                            </p>

                            <!-- Event Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #f0fdf4; border-left: 4px solid #10b981; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <h2 style=""margin: 0 0 15px 0; color: #065f46; font-size: 18px;"">Event Details</h2>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Event:</strong> {{EventTitle}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Date:</strong> {{EventStartDate}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Time:</strong> {{EventStartTime}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Location:</strong> {{EventLocation}}</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Registration Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #1e40af; font-size: 14px;"">Registration Details</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Number of Attendees:</strong> {{Quantity}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Registration Date:</strong> {{RegistrationDate}}</p>
                                    </td>
                                </tr>
                            </table>

                            {{#if HasAttendeeDetails}}
                            <!-- Attendees List -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #faf5ff; border-left: 4px solid #a855f7; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #7e22ce; font-size: 14px;"">Attendees</p>
                                        {{#each Attendees}}
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;"">- {{Name}} ({{AgeCategory}})</p>
                                        {{/each}}
                                    </td>
                                </tr>
                            </table>
                            {{/if}}

                            <p style=""margin: 20px 0 0 0; color: #555; line-height: 1.6;"">
                                Please save this email for your records. We look forward to seeing you at the event!
                            </p>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
                            <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;"">Sri Lankan Community Hub</p>
                            <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'transactional',
                    'Events',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-anonymous-rsvp-confirmation'
                );
            ");

            // 2.5 Create template-organizer-role-approval
            migrationBuilder.Sql(@"
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
                    'template-organizer-role-approval',
                    'Email sent to users when their EventOrganizer role upgrade request is approved',
                    'Welcome to LankaConnect Event Organizers!',
                    'Hi {{UserName}},

Great news! Your request to become an Event Organizer on LankaConnect has been approved.

YOUR NEW ROLE
-------------
Role: Event Organizer
Approved On: {{ApprovedAt}}
Trial Period: 6-month free trial

WHAT YOU CAN DO NOW
-------------------
- Create and publish events for the Sri Lankan community
- Manage event registrations and attendees
- Access event analytics and insights
- Create sign-up lists for volunteer coordination
- Send event updates to registered attendees

GET STARTED
-----------
Visit your dashboard to create your first event:
{{DashboardUrl}}

Need help getting started? Check out our organizer guide or contact support.

Thank you for joining the LankaConnect community of organizers!

---
LankaConnect
Sri Lankan Community Hub
© 2025 LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome Event Organizer - LankaConnect</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
        <tr>
            <td align=""center"" style=""padding: 40px 20px;"">
                <table role=""presentation"" width=""650"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 650px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">
                    <!-- Header with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: bold; color: white;"">Welcome, Event Organizer!</h1>
                        </td>
                    </tr>

                    <!-- Content -->
                    <tr>
                        <td style=""padding: 40px 30px; background: #ffffff;"">
                            <p style=""font-size: 16px; margin: 0 0 20px 0; color: #333;"">Hi {{UserName}},</p>

                            <p style=""margin: 0 0 25px 0; color: #555; line-height: 1.6;"">
                                Great news! Your request to become an <strong style=""color: #8B1538;"">Event Organizer</strong> on LankaConnect has been <strong style=""color: #10b981;"">approved</strong>.
                            </p>

                            <!-- Role Details Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #f0fdf4; border-left: 4px solid #10b981; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <h2 style=""margin: 0 0 15px 0; color: #065f46; font-size: 18px;"">Your New Role</h2>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Role:</strong> Event Organizer</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Approved:</strong> {{ApprovedAt}}</p>
                                        <p style=""margin: 5px 0; color: #666; font-size: 14px;""><strong>Trial:</strong> 6-month free trial included</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- What You Can Do Box -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 0 8px 8px 0;"">
                                <tr>
                                    <td style=""padding: 20px;"">
                                        <p style=""margin: 0 0 10px 0; font-weight: bold; color: #1e40af; font-size: 14px;"">What You Can Do Now</p>
                                        <ul style=""margin: 0; padding-left: 20px; color: #666; font-size: 14px; line-height: 1.8;"">
                                            <li>Create and publish events for the Sri Lankan community</li>
                                            <li>Manage event registrations and attendees</li>
                                            <li>Access event analytics and insights</li>
                                            <li>Create sign-up lists for volunteer coordination</li>
                                            <li>Send event updates to registered attendees</li>
                                        </ul>
                                    </td>
                                </tr>
                            </table>

                            <!-- CTA Button -->
                            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""background: #FF6600; border-radius: 6px; text-align: center;"">
                                                    <a href=""{{DashboardUrl}}"" style=""display: inline-block; padding: 14px 28px; color: white; text-decoration: none; font-weight: bold; font-size: 14px;"">Go to Dashboard</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 20px 0 0 0; color: #555; line-height: 1.6;"">
                                Thank you for joining the LankaConnect community of organizers. We can''t wait to see the amazing events you''ll create!
                            </p>
                        </td>
                    </tr>

                    <!-- Footer with Brand Gradient -->
                    <tr>
                        <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
                            <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
                            <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;"">Sri Lankan Community Hub</p>
                            <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">&copy; 2025 LankaConnect. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
                    'transactional',
                    'System',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-organizer-role-approval'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =====================================================
            // PART 1: REVERT ALL 14 TEMPLATE RENAMES
            // =====================================================

            migrationBuilder.Sql(@"
                -- Revert template-free-event-registration-confirmation to registration-confirmation
                UPDATE communications.email_templates
                SET name = 'registration-confirmation', updated_at = NOW()
                WHERE name = 'template-free-event-registration-confirmation';

                -- Revert template-new-event-publication to event-published
                UPDATE communications.email_templates
                SET name = 'event-published', updated_at = NOW()
                WHERE name = 'template-new-event-publication';

                -- Revert template-membership-email-verification to member-email-verification
                UPDATE communications.email_templates
                SET name = 'member-email-verification', updated_at = NOW()
                WHERE name = 'template-membership-email-verification';

                -- Revert template-event-cancellation-notifications to event-cancelled-notification
                UPDATE communications.email_templates
                SET name = 'event-cancelled-notification', updated_at = NOW()
                WHERE name = 'template-event-cancellation-notifications';

                -- Revert template-event-registration-cancellation to registration-cancellation
                UPDATE communications.email_templates
                SET name = 'registration-cancellation', updated_at = NOW()
                WHERE name = 'template-event-registration-cancellation';

                -- Revert template-newsletter-subscription-confirmation to newsletter-confirmation
                UPDATE communications.email_templates
                SET name = 'newsletter-confirmation', updated_at = NOW()
                WHERE name = 'template-newsletter-subscription-confirmation';

                -- Revert template-newsletter-notification to newsletter
                UPDATE communications.email_templates
                SET name = 'newsletter', updated_at = NOW()
                WHERE name = 'template-newsletter-notification';

                -- Revert template-event-details-publication to event-details
                UPDATE communications.email_templates
                SET name = 'event-details', updated_at = NOW()
                WHERE name = 'template-event-details-publication';

                -- Revert template-signup-list-commitment-confirmation to signup-commitment-confirmation
                UPDATE communications.email_templates
                SET name = 'signup-commitment-confirmation', updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation';

                -- Revert template-signup-list-commitment-update to signup-commitment-updated
                UPDATE communications.email_templates
                SET name = 'signup-commitment-updated', updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update';

                -- Revert template-signup-list-commitment-cancellation to signup-commitment-cancelled
                UPDATE communications.email_templates
                SET name = 'signup-commitment-cancelled', updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-cancellation';

                -- Revert template-event-approval to event-approved
                UPDATE communications.email_templates
                SET name = 'event-approved', updated_at = NOW()
                WHERE name = 'template-event-approval';

                -- Revert template-event-reminder to event-reminder
                UPDATE communications.email_templates
                SET name = 'event-reminder', updated_at = NOW()
                WHERE name = 'template-event-reminder';

                -- Revert template-paid-event-registration-confirmation-with-ticket to ticket-confirmation
                UPDATE communications.email_templates
                SET name = 'ticket-confirmation', updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
            ");

            // =====================================================
            // PART 2: REMOVE THE 5 NEW TEMPLATES
            // =====================================================

            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-password-reset',
                    'template-password-change-confirmation',
                    'template-welcome',
                    'template-anonymous-rsvp-confirmation',
                    'template-organizer-role-approval'
                );
            ");
        }
    }
}
