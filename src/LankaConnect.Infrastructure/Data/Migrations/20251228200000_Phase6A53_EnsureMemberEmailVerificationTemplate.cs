using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A53_EnsureMemberEmailVerificationTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.53: Ensure member-email-verification template exists
            // Uses ON CONFLICT to handle case where template already exists

            migrationBuilder.Sql(@"
                -- First, delete any existing template with this name (to avoid conflicts)
                DELETE FROM communications.email_templates WHERE name = 'member-email-verification';

                -- Now insert the template with correct enum value
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
                VALUES
                (
                    gen_random_uuid(),
                    'member-email-verification',
                    'Email verification for new member signups with secure token - Phase 6A.53',
                    'Verify Your Email Address - LankaConnect',
                    'Hi {{UserName}},

Welcome to LankaConnect!

To complete your registration, please verify your email address by clicking the link below:

{{VerificationUrl}}

This link will expire in {{ExpirationHours}} hours.

If you didn''t create this account, please ignore this email.

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #2563eb; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .verify-button { display: inline-block; background: #2563eb; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; margin: 20px 0; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to LankaConnect!</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Thank you for signing up! To complete your registration, please verify your email address:</p>
            <p style=""text-align: center;"">
                <a href=""{{VerificationUrl}}"" class=""verify-button"">Verify Email Address</a>
            </p>
            <p style=""color: #666; font-size: 14px;"">This link will expire in {{ExpirationHours}} hours.</p>
            <p style=""color: #666; font-size: 14px;"">If you didn''t create this account, please ignore this email.</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    10,  -- EmailType.MemberEmailVerification = 10
                    'Authentication',
                    true,
                    NOW()
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.53: Remove member-email-verification template
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'member-email-verification';
            ");
        }
    }
}
