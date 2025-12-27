using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A54_SeedNewEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.54: Seed 4 new email templates into communications.email_templates

            // Template 1: Member Email Verification
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
                VALUES
                (
                    gen_random_uuid(),
                    'member-email-verification',
                    'Email verification for new member signups with secure token',
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
                    'MemberEmailVerification',
                    'Authentication',
                    true,
                    NOW()
                );
            ");

            // Template 2: Signup Commitment Confirmation
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
                VALUES
                (
                    gen_random_uuid(),
                    'signup-commitment-confirmation',
                    'Confirmation email when user commits to bringing an item to an event',
                    'Item Commitment Confirmed for {{EventTitle}}',
                    'Hi {{UserName}},

Thank you for committing to bring items to {{EventTitle}}!

COMMITMENT DETAILS
------------------
Item: {{ItemDescription}}
Quantity: {{Quantity}}
Event Date: {{EventDateTime}}
Location: {{EventLocation}}

PICKUP/DELIVERY
---------------
{{PickupInstructions}}

You can view or modify your commitment in your event dashboard.

We appreciate your contribution!

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #10b981; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .commitment-box { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #10b981; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Commitment Confirmed!</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Thank you for committing to bring items to <strong>{{EventTitle}}</strong>!</p>
            <div class=""commitment-box"">
                <h3>Commitment Details</h3>
                <p><strong>Item:</strong> {{ItemDescription}}</p>
                <p><strong>Quantity:</strong> {{Quantity}}</p>
                <p><strong>Event Date:</strong> {{EventDateTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
            </div>
            <div class=""commitment-box"">
                <h3>Pickup/Delivery</h3>
                <p>{{PickupInstructions}}</p>
            </div>
            <p>You can view or modify your commitment in your event dashboard.</p>
            <p>We appreciate your contribution!</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'SignupCommitmentConfirmation',
                    'Notification',
                    true,
                    NOW()
                );
            ");

            // Template 3: Registration Cancellation Confirmation
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
                VALUES
                (
                    gen_random_uuid(),
                    'registration-cancellation',
                    'Confirmation email when user cancels event registration',
                    'Registration Cancelled - {{EventTitle}}',
                    'Hi {{UserName}},

Your registration for {{EventTitle}} has been cancelled.

CANCELLED REGISTRATION
-----------------------
Event: {{EventTitle}}
Date: {{EventDateTime}}
Location: {{EventLocation}}
Cancelled At: {{CancellationDateTime}}

REFUND INFORMATION
------------------
{{RefundDetails}}

CANCELLATION POLICY
-------------------
{{CancellationPolicy}}

If you cancelled by mistake, you can register again if spots are still available.

(c) 2025 LankaConnect
Questions? Reply to this email or visit our support page.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #ef4444; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .info-box { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #ef4444; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Registration Cancelled</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p>Your registration for <strong>{{EventTitle}}</strong> has been cancelled.</p>
            <div class=""info-box"">
                <h3>Cancelled Registration</h3>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Date:</strong> {{EventDateTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><strong>Cancelled At:</strong> {{CancellationDateTime}}</p>
            </div>
            <div class=""info-box"">
                <h3>Refund Information</h3>
                <p>{{RefundDetails}}</p>
            </div>
            <div class=""info-box"">
                <h3>Cancellation Policy</h3>
                <p>{{CancellationPolicy}}</p>
            </div>
            <p>If you cancelled by mistake, you can register again if spots are still available.</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'RegistrationCancellationConfirmation',
                    'Notification',
                    true,
                    NOW()
                );
            ");

            // Template 4: Organizer Custom Message to Attendees
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
                VALUES
                (
                    gen_random_uuid(),
                    'organizer-custom-message',
                    'Custom message from event organizer to attendees',
                    '{{Subject}}',
                    'Hi {{UserName}},

{{OrganizerName}} has sent you a message about {{EventTitle}}:

---
{{MessageContent}}
---

EVENT DETAILS
-------------
Date: {{EventDateTime}}
Location: {{EventLocation}}

You can view event details and respond in your event dashboard.

(c) 2025 LankaConnect
Questions? Reply to this email or contact the organizer.',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #8b5cf6; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .message-box { background: white; padding: 20px; margin: 15px 0; border-left: 4px solid #8b5cf6; border-radius: 4px; }
        .event-info { background: #f3f4f6; padding: 15px; margin: 15px 0; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Message from Event Organizer</h1>
        </div>
        <div class=""content"">
            <p>Hi {{UserName}},</p>
            <p><strong>{{OrganizerName}}</strong> has sent you a message about <strong>{{EventTitle}}</strong>:</p>
            <div class=""message-box"">
                {{MessageContent}}
            </div>
            <div class=""event-info"">
                <h3>Event Details</h3>
                <p><strong>Date:</strong> {{EventDateTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
            </div>
            <p>You can view event details and respond in your event dashboard.</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>',
                    'OrganizerCustomMessage',
                    'Business',
                    true,
                    NOW()
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.54: Remove seeded email templates
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'member-email-verification',
                    'signup-commitment-confirmation',
                    'registration-cancellation',
                    'organizer-custom-message'
                );
            ");
        }
    }
}
