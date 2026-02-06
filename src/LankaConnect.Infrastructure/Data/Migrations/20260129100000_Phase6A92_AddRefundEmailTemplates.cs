using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.92: Add email templates for refund notifications.
    /// Templates added:
    /// - template-refund-requested: Sent when refund is initiated (event cancelled or user cancellation)
    /// - template-refund-completed: Sent when refund is processed by Stripe (webhook confirmation)
    /// </summary>
    public partial class Phase6A92_AddRefundEmailTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Template 1: Refund Requested
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
                    'template-refund-requested',
                    'Phase 6A.92: Refund requested notification - sent when refund is initiated',
                    'Your Refund for {{EventTitle}} is Being Processed',
                    'Hi {{UserName}},

We wanted to let you know that a refund for your registration to ""{{EventTitle}}"" is being processed.

REFUND DETAILS
Event: {{EventTitle}}
Event Date: {{EventDateTime}}
Refund Amount: ${{RefundAmount}}

WHAT HAPPENS NEXT
Your refund will be processed within 5-10 business days. The funds will be returned to your original payment method.

You will receive a confirmation email once the refund has been completed.

QUESTIONS?
If you have any questions about your refund, please contact the event organizer:

Organizer: {{OrganizerContactName}}
Email: {{OrganizerContactEmail}}
Phone: {{OrganizerContactPhone}}

Or reach out to our support team at {{SupportEmail}}.

Best regards,
The LankaConnect Team

---
This is an automated message from LankaConnect.
Please do not reply directly to this email.',
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
        .refund-box { background: #fffbeb; border: 2px solid #f59e0b; padding: 20px; margin: 20px 0; border-radius: 8px; }
        .refund-box .label { color: #92400e; font-size: 14px; margin-bottom: 4px; }
        .refund-box .amount { color: #b45309; font-size: 28px; font-weight: 700; }
        .detail-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e5e7eb; }
        .detail-row:last-child { border-bottom: none; }
        .detail-label { color: #6b7280; font-weight: 500; }
        .detail-value { color: #111827; font-weight: 600; }
        .timeline { background: #f9fafb; padding: 20px; margin: 20px 0; border-radius: 6px; }
        .timeline h3 { margin: 0 0 15px 0; color: #374151; font-size: 16px; }
        .timeline-item { display: flex; align-items: flex-start; margin-bottom: 12px; }
        .timeline-icon { width: 24px; height: 24px; background: #10b981; color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 12px; margin-right: 12px; flex-shrink: 0; }
        .timeline-icon.pending { background: #d1d5db; }
        .timeline-text { color: #4b5563; font-size: 14px; }
        .contact-box { background: #f3f4f6; padding: 20px; margin: 20px 0; border-radius: 6px; }
        .contact-box h3 { margin: 0 0 15px 0; color: #374151; font-size: 16px; }
        .contact-info { color: #4b5563; font-size: 14px; margin-bottom: 8px; }
        .contact-info strong { color: #111827; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #f59e0b; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Refund In Progress</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>
            <p>We wanted to let you know that a refund for your registration is being processed.</p>

            <div class=""refund-box"">
                <div class=""label"">Refund Amount</div>
                <div class=""amount"">${{RefundAmount}}</div>
            </div>

            <div style=""margin: 20px 0;"">
                <div class=""detail-row"">
                    <span class=""detail-label"">Event</span>
                    <span class=""detail-value"">{{EventTitle}}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Event Date</span>
                    <span class=""detail-value"">{{EventDateTime}}</span>
                </div>
            </div>

            <div class=""timeline"">
                <h3>What Happens Next</h3>
                <div class=""timeline-item"">
                    <div class=""timeline-icon"">&#10003;</div>
                    <div class=""timeline-text"">Refund initiated</div>
                </div>
                <div class=""timeline-item"">
                    <div class=""timeline-icon pending"">2</div>
                    <div class=""timeline-text"">Processing (5-10 business days)</div>
                </div>
                <div class=""timeline-item"">
                    <div class=""timeline-icon pending"">3</div>
                    <div class=""timeline-text"">Funds returned to original payment method</div>
                </div>
            </div>

            <div class=""contact-box"">
                <h3>Questions About Your Refund?</h3>
                <div class=""contact-info""><strong>Organizer:</strong> {{OrganizerContactName}}</div>
                <div class=""contact-info""><strong>Email:</strong> {{OrganizerContactEmail}}</div>
                <div class=""contact-info""><strong>Phone:</strong> {{OrganizerContactPhone}}</div>
            </div>

            <p>Best regards,<br><strong>The LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>Questions? Contact us at <a href=""mailto:{{SupportEmail}}"">{{SupportEmail}}</a></p>
            <p>This is an automated message. Please do not reply directly to this email.</p>
        </div>
    </div>
</body>
</html>',
                    'Registration',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-refund-requested'
                );
            ");

            // Template 2: Refund Completed
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
                    'template-refund-completed',
                    'Phase 6A.92: Refund completed confirmation - sent when Stripe confirms refund',
                    'Your Refund of ${{RefundAmount}} Has Been Processed',
                    'Hi {{UserName}},

Great news! Your refund has been successfully processed.

REFUND CONFIRMATION
Event: {{EventTitle}}
Refund Amount: ${{RefundAmount}}
Reference ID: {{StripeRefundId}}

The funds have been returned to your original payment method. Depending on your bank, it may take 3-5 additional business days to appear in your account.

If you have any questions, please contact our support team at {{SupportEmail}}.

Best regards,
The LankaConnect Team

---
This is an automated message from LankaConnect.
Please do not reply directly to this email.',
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
        .success-box .icon { font-size: 48px; margin-bottom: 10px; }
        .success-box .label { color: #065f46; font-size: 14px; margin-bottom: 4px; }
        .success-box .amount { color: #059669; font-size: 32px; font-weight: 700; }
        .detail-row { display: flex; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid #e5e7eb; }
        .detail-row:last-child { border-bottom: none; }
        .detail-label { color: #6b7280; font-weight: 500; }
        .detail-value { color: #111827; font-weight: 600; }
        .reference-id { font-family: monospace; background: #f3f4f6; padding: 4px 8px; border-radius: 4px; }
        .info-box { background: #eff6ff; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0; border-radius: 0 6px 6px 0; }
        .info-box p { margin: 0; color: #1e40af; font-size: 14px; }
        .footer { background: #f9fafb; padding: 20px; text-align: center; color: #6b7280; font-size: 12px; border-top: 1px solid #e5e7eb; }
        .footer a { color: #10b981; text-decoration: none; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Refund Complete</h1>
        </div>
        <div class=""content"">
            <p>Hi <strong>{{UserName}}</strong>,</p>
            <p>Great news! Your refund has been successfully processed.</p>

            <div class=""success-box"">
                <div class=""icon"">&#10003;</div>
                <div class=""label"">Refunded Amount</div>
                <div class=""amount"">${{RefundAmount}}</div>
            </div>

            <div style=""margin: 20px 0;"">
                <div class=""detail-row"">
                    <span class=""detail-label"">Event</span>
                    <span class=""detail-value"">{{EventTitle}}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Reference ID</span>
                    <span class=""detail-value reference-id"">{{StripeRefundId}}</span>
                </div>
            </div>

            <div class=""info-box"">
                <p><strong>Note:</strong> The funds have been returned to your original payment method. Depending on your bank, it may take 3-5 additional business days to appear in your account.</p>
            </div>

            <p>Best regards,<br><strong>The LankaConnect Team</strong></p>
        </div>
        <div class=""footer"">
            <p>Questions? Contact us at <a href=""mailto:{{SupportEmail}}"">{{SupportEmail}}</a></p>
            <p>This is an automated message. Please do not reply directly to this email.</p>
        </div>
    </div>
</body>
</html>',
                    'Registration',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-refund-completed'
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN ('template-refund-requested', 'template-refund-completed');
            ");
        }
    }
}
