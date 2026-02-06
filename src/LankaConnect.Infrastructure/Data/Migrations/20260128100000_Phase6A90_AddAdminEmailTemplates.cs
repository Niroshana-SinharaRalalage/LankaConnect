using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.90: Add email templates for Admin User Management & Support/Feedback System.
    /// Templates added:
    /// - template-support-ticket-confirmation: Auto-confirmation for contact form submissions
    /// - template-support-ticket-reply: Reply notification to ticket submitter
    /// - template-account-locked-by-admin: Account locked notification
    /// - template-account-unlocked-by-admin: Account unlocked notification
    /// - template-account-activated-by-admin: Account activated notification
    /// - template-account-deactivated-by-admin: Account deactivated notification
    /// </summary>
    public partial class Phase6A90_AddAdminEmailTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Template 1: Support Ticket Confirmation
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
                    'template-support-ticket-confirmation',
                    'Phase 6A.90: Auto-confirmation email sent when contact form is submitted',
                    'We Received Your Message - Reference #{{ReferenceId}}',
                    'Hi {{Name}},

Thank you for contacting LankaConnect!

We have received your message and will respond as soon as possible. Please keep this reference number for your records:

Reference Number: {{ReferenceId}}
Subject: {{Subject}}

Your Message:
{{Message}}

Our team typically responds within 24-48 business hours. If your matter is urgent, please mention your reference number in any follow-up communication.

Best regards,
The LankaConnect Team

Questions? Contact us at {{SupportEmail}}

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .reference-box { background: #ecfdf5; border: 2px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 8px; text-align: center; }
        .reference-box .label { color: #065f46; font-size: 14px; margin-bottom: 8px; }
        .reference-box .number { color: #059669; font-size: 24px; font-weight: 700; font-family: monospace; }
        .message-box { background: #f9fafb; padding: 20px; margin: 20px 0; border-radius: 6px; border-left: 4px solid #10b981; }
        .message-box h3 { margin: 0 0 10px 0; color: #374151; font-size: 14px; }
        .message-box p { margin: 0; color: #4b5563; white-space: pre-wrap; }
        .info-text { color: #6b7280; font-size: 14px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✉️ Message Received</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{Name}}</strong>,</p>
            <p>Thank you for contacting LankaConnect! We have received your message and will respond as soon as possible.</p>

            <div class=""reference-box"">
                <div class=""label"">Your Reference Number</div>
                <div class=""number"">{{ReferenceId}}</div>
            </div>

            <p><strong>Subject:</strong> {{Subject}}</p>

            <div class=""message-box"">
                <h3>Your Message:</h3>
                <p>{{Message}}</p>
            </div>

            <p class=""info-text"">Our team typically responds within 24-48 business hours. If your matter is urgent, please mention your reference number in any follow-up communication.</p>

            <p>Best regards,<br><strong>The LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>Questions? Contact us at <a href=""mailto:{{SupportEmail}}"">{{SupportEmail}}</a></p>
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Support',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-support-ticket-confirmation'
                );
            ");

            // Template 2: Support Ticket Reply
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
                    'template-support-ticket-reply',
                    'Phase 6A.90: Email sent when admin replies to a support ticket',
                    'Re: {{Subject}} - Reference #{{ReferenceId}}',
                    'Hi {{Name}},

We have responded to your support request (Reference: {{ReferenceId}}).

Our Response:
{{ReplyContent}}

---
Original Subject: {{Subject}}

If you have any further questions, please reply to this email or contact us at {{SupportEmail}} with your reference number.

Best regards,
The LankaConnect Support Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .header .ref { display: inline-block; background: rgba(255,255,255,0.2); padding: 6px 14px; border-radius: 20px; margin-top: 10px; font-size: 14px; }
        .content { padding: 30px 20px; }
        .reply-box { background: #eff6ff; border-left: 4px solid #3b82f6; padding: 20px; margin: 20px 0; border-radius: 6px; }
        .reply-box h3 { margin: 0 0 12px 0; color: #1e40af; font-size: 14px; }
        .reply-box p { margin: 0; color: #1e3a8a; white-space: pre-wrap; }
        .original-subject { color: #6b7280; font-size: 13px; margin-top: 20px; padding-top: 15px; border-top: 1px solid #e5e7eb; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #3b82f6; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>💬 Support Response</h1>
            <div class=""ref"">Reference: {{ReferenceId}}</div>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{Name}}</strong>,</p>
            <p>We have responded to your support request.</p>

            <div class=""reply-box"">
                <h3>Our Response:</h3>
                <p>{{ReplyContent}}</p>
            </div>

            <p class=""original-subject""><strong>Original Subject:</strong> {{Subject}}</p>

            <p>If you have any further questions, please reply to this email or contact us with your reference number.</p>

            <p>Best regards,<br><strong>The LankaConnect Support Team</strong></p>
        </div>
        <div class=""footer"">
            <p>Questions? Contact us at <a href=""mailto:{{SupportEmail}}"">{{SupportEmail}}</a></p>
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Support',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-support-ticket-reply'
                );
            ");

            // Template 3: Account Locked by Admin
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
                    'template-account-locked-by-admin',
                    'Phase 6A.90: Email sent when admin locks a user account',
                    'Your LankaConnect Account Has Been Temporarily Locked',
                    'Hi {{UserName}},

Your LankaConnect account has been temporarily locked by an administrator.

Lock Details:
- Locked Until: {{LockUntil}}
- Reason: {{Reason}}

During this time, you will not be able to log in or access your account.

If you believe this was done in error or have questions, please contact us at {{SupportEmail}}.

LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .alert-box { background: #fef3c7; border: 1px solid #f59e0b; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .alert-box h3 { margin: 0 0 12px 0; color: #92400e; font-size: 16px; }
        .alert-box p { margin: 6px 0; color: #78350f; }
        .info-text { color: #6b7280; font-size: 14px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #f59e0b; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔒 Account Temporarily Locked</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>
            <p>Your LankaConnect account has been temporarily locked by an administrator.</p>

            <div class=""alert-box"">
                <h3>Lock Details</h3>
                <p><strong>Locked Until:</strong> {{LockUntil}}</p>
                <p><strong>Reason:</strong> {{Reason}}</p>
            </div>

            <p class=""info-text"">During this time, you will not be able to log in or access your account.</p>

            <p>If you believe this was done in error or have questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #f59e0b;"">{{SupportEmail}}</a>.</p>

            <p>LankaConnect Team</p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Account',
                    'Security',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-account-locked-by-admin'
                );
            ");

            // Template 4: Account Unlocked by Admin
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
                    'template-account-unlocked-by-admin',
                    'Phase 6A.90: Email sent when admin unlocks a user account',
                    'Your LankaConnect Account Has Been Unlocked',
                    'Hi {{UserName}},

Great news! Your LankaConnect account has been unlocked by an administrator.

You can now log in and access your account as usual.

Login here: {{LoginUrl}}

If you have any questions, please contact us at {{SupportEmail}}.

Best regards,
LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .success-box { background: #ecfdf5; border: 2px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 8px; text-align: center; }
        .success-box p { margin: 0; color: #065f46; font-size: 16px; }
        .cta-button { display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; text-decoration: none; padding: 14px 28px; border-radius: 6px; font-weight: 600; font-size: 16px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔓 Account Unlocked</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>

            <div class=""success-box"">
                <p>✅ Your LankaConnect account has been unlocked!</p>
            </div>

            <p>You can now log in and access your account as usual.</p>

            <div style=""text-align: center;"">
                <a href=""{{LoginUrl}}"" class=""cta-button"">Login to Your Account</a>
            </div>

            <p>If you have any questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #10b981;"">{{SupportEmail}}</a>.</p>

            <p>Best regards,<br><strong>LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Account',
                    'Security',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-account-unlocked-by-admin'
                );
            ");

            // Template 5: Account Activated by Admin
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
                    'template-account-activated-by-admin',
                    'Phase 6A.90: Email sent when admin activates a user account',
                    'Your LankaConnect Account Has Been Activated',
                    'Hi {{UserName}},

Great news! Your LankaConnect account has been activated by an administrator.

You can now log in and enjoy all the features of LankaConnect.

Login here: {{LoginUrl}}

If you have any questions, please contact us at {{SupportEmail}}.

Welcome back!
LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .success-box { background: #ecfdf5; border: 2px solid #10b981; padding: 20px; margin: 20px 0; border-radius: 8px; text-align: center; }
        .success-box p { margin: 0; color: #065f46; font-size: 16px; }
        .cta-button { display: inline-block; background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; text-decoration: none; padding: 14px 28px; border-radius: 6px; font-weight: 600; font-size: 16px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Account Activated</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>

            <div class=""success-box"">
                <p>✅ Your LankaConnect account has been activated!</p>
            </div>

            <p>You can now log in and enjoy all the features of LankaConnect.</p>

            <div style=""text-align: center;"">
                <a href=""{{LoginUrl}}"" class=""cta-button"">Login to Your Account</a>
            </div>

            <p>If you have any questions, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #10b981;"">{{SupportEmail}}</a>.</p>

            <p>Welcome back!<br><strong>LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Account',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-account-activated-by-admin'
                );
            ");

            // Template 6: Account Deactivated by Admin
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
                    'template-account-deactivated-by-admin',
                    'Phase 6A.90: Email sent when admin deactivates a user account',
                    'Your LankaConnect Account Has Been Deactivated',
                    'Hi {{UserName}},

Your LankaConnect account has been deactivated by an administrator.

You will no longer be able to log in or access your account until it is reactivated.

If you believe this was done in error or would like to request reactivation, please contact us at {{SupportEmail}}.

LankaConnect Team

© {{Year}} LankaConnect. All rights reserved.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; background-color: #f4f4f4; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #6b7280 0%, #4b5563 100%); color: white; padding: 30px 20px; text-align: center; }
        .header h1 { margin: 0; font-size: 24px; font-weight: 600; }
        .content { padding: 30px 20px; }
        .notice-box { background: #f3f4f6; border: 1px solid #9ca3af; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .notice-box p { margin: 0; color: #374151; }
        .info-text { color: #6b7280; font-size: 14px; margin: 20px 0; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #6b7280; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Account Deactivated</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>

            <div class=""notice-box"">
                <p>Your LankaConnect account has been deactivated by an administrator.</p>
            </div>

            <p class=""info-text"">You will no longer be able to log in or access your account until it is reactivated.</p>

            <p>If you believe this was done in error or would like to request reactivation, please contact us at <a href=""mailto:{{SupportEmail}}"" style=""color: #6b7280;"">{{SupportEmail}}</a>.</p>

            <p>LankaConnect Team</p>
        </div>
        <div class=""footer"">
            <p>© {{Year}} LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'Account',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-account-deactivated-by-admin'
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all Phase 6A.90 email templates
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-support-ticket-confirmation',
                    'template-support-ticket-reply',
                    'template-account-locked-by-admin',
                    'template-account-unlocked-by-admin',
                    'template-account-activated-by-admin',
                    'template-account-deactivated-by-admin'
                );
            ");
        }
    }
}
