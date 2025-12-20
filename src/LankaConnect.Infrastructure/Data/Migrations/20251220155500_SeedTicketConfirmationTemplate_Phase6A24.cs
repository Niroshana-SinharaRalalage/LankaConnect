using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedTicketConfirmationTemplate_Phase6A24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.24: Seed ticket-confirmation email template into database
            // ROOT CAUSE: AzureEmailService loads templates from communications.email_templates table, not files
            // Templates must be in database for SendTemplatedEmailAsync() to work
            // This template is used by PaymentCompletedEventHandler for PAID event registrations

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
                    'ticket-confirmation',
                    'Paid event ticket confirmation email with payment details and PDF ticket attachment',
                    'Your Ticket for {{EventTitle}} - {{TicketCode}}',
                    'Hi {{UserName}},

Thank you for registering for {{EventTitle}}!

EVENT DETAILS
-------------
Date: {{EventStartDate}} at {{EventStartTime}}
Location: {{EventLocation}}
Attendees: {{AttendeeCount}}

PAYMENT CONFIRMATION
--------------------
Amount Paid: {{AmountPaid}}
Payment ID: {{PaymentIntentId}}
Payment Date: {{PaymentDate}}

YOUR TICKET
-----------
Ticket Code: {{TicketCode}}

Your ticket is attached to this email as a PDF. Please present it at the event entrance.
Valid Until: {{TicketExpiryDate}}

We look forward to seeing you at the event!

(c) 2025 LankaConnect
If you have questions, contact us at support@lankaconnect.com',
                    '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #2563eb; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { padding: 20px; background: #f9fafb; }
        .ticket-info { background: white; padding: 15px; margin: 15px 0; border-left: 4px solid #2563eb; border-radius: 4px; }
        .footer { text-align: center; padding: 20px; color: #666; font-size: 12px; }
        .code { background: #f3f4f6; padding: 4px 8px; border-radius: 4px; font-family: monospace; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Your Event Ticket</h1>
        </div>

        <div class=""content"">
            <p>Hi {{UserName}},</p>

            <p>Thank you for registering for <strong>{{EventTitle}}</strong>!</p>

            <div class=""ticket-info"">
                <h3>Event Details</h3>
                <p><strong>Date:</strong> {{EventStartDate}} at {{EventStartTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><strong>Attendees:</strong> {{AttendeeCount}}</p>
            </div>

            <div class=""ticket-info"">
                <h3>Payment Confirmation</h3>
                <p><strong>Amount Paid:</strong> {{AmountPaid}}</p>
                <p><strong>Payment ID:</strong> {{PaymentIntentId}}</p>
                <p><strong>Payment Date:</strong> {{PaymentDate}}</p>
            </div>

            <div class=""ticket-info"">
                <h3>Your Ticket</h3>
                <p><strong>Ticket Code:</strong> <span class=""code"">{{TicketCode}}</span></p>
                <p>Your ticket is attached to this email as a PDF. Please present it at the event entrance.</p>
                <p><strong>Valid Until:</strong> {{TicketExpiryDate}}</p>
            </div>

            <p>We look forward to seeing you at the event!</p>
        </div>

        <div class=""footer"">
            <p>&copy; 2025 LankaConnect. All rights reserved.</p>
            <p>If you have questions, contact us at support@lankaconnect.com</p>
        </div>
    </div>
</body>
</html>',
                    'Transactional',
                    'Event',
                    true,
                    NOW()
                )
                ON CONFLICT (""name"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the template if rolling back
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE ""name"" = 'ticket-confirmation';
            ");
        }
    }
}
