-- Phase 6A.43: Update ticket-confirmation email template to use new variable format
-- This fixes the mismatch between code (EventDateTime) and database template (EventStartDate/EventStartTime)
--
-- Root Cause: Commit f45f08b4 updated PaymentCompletedEventHandler to send {{EventDateTime}}
-- but no migration was created to update the database template.
--
-- This script updates the existing ticket-confirmation template to match the code.
--
-- SAFE TO RUN: Updates existing template, does not delete or recreate

BEGIN;

-- Verify template exists before updating
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM communications.email_templates
        WHERE name = 'ticket-confirmation'
    ) THEN
        RAISE EXCEPTION 'Template ticket-confirmation not found. Run seed migration first.';
    END IF;
END $$;

-- Update ticket-confirmation template with new variables
UPDATE communications.email_templates
SET
    subject_template = 'Your Ticket for {{EventTitle}}',

    text_template = 'Hi {{UserName}},

Thank you for registering for {{EventTitle}}!

EVENT DETAILS
-------------
Date & Time: {{EventDateTime}}
Location: {{EventLocation}}
Registration Date: {{RegistrationDate}}

{{#HasAttendeeDetails}}
ATTENDEES
---------
{{Attendees}}
{{/HasAttendeeDetails}}

PAYMENT CONFIRMATION
--------------------
Amount Paid: {{AmountPaid}}
Payment ID: {{PaymentIntentId}}
Payment Date: {{PaymentDate}}

{{#HasTicket}}
YOUR TICKET
-----------
Ticket Code: {{TicketCode}}

Your ticket is attached to this email as a PDF. Please present it at the event entrance.
Valid Until: {{TicketExpiryDate}}
{{/HasTicket}}

{{#HasContactInfo}}
YOUR CONTACT INFORMATION
------------------------
Email: {{ContactEmail}}
{{#ContactPhone}}Phone: {{ContactPhone}}{{/ContactPhone}}
{{/HasContactInfo}}

We look forward to seeing you at the event!

LankaConnect
Sri Lankan Community Hub',

    html_template = '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif;
            line-height: 1.6;
            color: #1f2937;
            margin: 0;
            padding: 0;
            background-color: #f3f4f6;
        }
        .email-container {
            max-width: 850px;
            margin: 0 auto;
            background: #ffffff;
        }
        .header {
            background: linear-gradient(135deg, #f97316 0%, #f43f5e 50%, #10b981 100%);
            padding: 40px 30px;
            text-align: center;
        }
        .header h1 {
            color: #ffffff;
            font-size: 32px;
            font-weight: 700;
            margin: 0;
            text-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        {{#HasEventImage}}
        .event-image-container {
            width: 100%;
            max-height: 400px;
            overflow: hidden;
        }
        .event-image {
            width: 100%;
            height: auto;
            display: block;
        }
        {{/HasEventImage}}
        .content {
            padding: 40px 30px;
        }
        .greeting {
            font-size: 18px;
            color: #374151;
            margin-bottom: 20px;
        }
        .event-info {
            background: linear-gradient(135deg, #fef3c7 0%, #fce7f3 100%);
            border-left: 5px solid #f97316;
            padding: 25px;
            margin: 25px 0;
            border-radius: 0 8px 8px 0;
        }
        .event-info h2 {
            color: #dc2626;
            font-size: 22px;
            margin: 0 0 15px 0;
            font-weight: 600;
        }
        .info-row {
            display: flex;
            align-items: baseline;
            margin: 12px 0;
            font-size: 16px;
        }
        .info-label {
            font-weight: 600;
            color: #dc2626;
            min-width: 140px;
        }
        .info-value {
            color: #374151;
        }
        .attendee-section {
            background: #f0fdf4;
            border-left: 5px solid #10b981;
            padding: 25px;
            margin: 25px 0;
            border-radius: 0 8px 8px 0;
        }
        .attendee-section h2 {
            color: #059669;
            font-size: 22px;
            margin: 0 0 15px 0;
            font-weight: 600;
        }
        .payment-section {
            background: #eff6ff;
            border-left: 5px solid #3b82f6;
            padding: 25px;
            margin: 25px 0;
            border-radius: 0 8px 8px 0;
        }
        .payment-section h2 {
            color: #1d4ed8;
            font-size: 22px;
            margin: 0 0 15px 0;
            font-weight: 600;
        }
        .ticket-section {
            background: #faf5ff;
            border-left: 5px solid #a855f7;
            padding: 25px;
            margin: 25px 0;
            border-radius: 0 8px 8px 0;
        }
        .ticket-section h2 {
            color: #7c3aed;
            font-size: 22px;
            margin: 0 0 15px 0;
            font-weight: 600;
        }
        .ticket-code {
            background: #ffffff;
            border: 2px dashed #a855f7;
            padding: 15px;
            margin: 15px 0;
            text-align: center;
            font-family: ''Courier New'', monospace;
            font-size: 24px;
            font-weight: 700;
            color: #7c3aed;
            letter-spacing: 2px;
        }
        .contact-section {
            background: #fef2f2;
            border-left: 5px solid #ef4444;
            padding: 25px;
            margin: 25px 0;
            border-radius: 0 8px 8px 0;
        }
        .contact-section h2 {
            color: #dc2626;
            font-size: 22px;
            margin: 0 0 15px 0;
            font-weight: 600;
        }
        .footer {
            background: linear-gradient(135deg, #f97316 0%, #f43f5e 50%, #10b981 100%);
            padding: 30px;
            text-align: center;
        }
        .footer-brand {
            color: #ffffff;
            font-size: 24px;
            font-weight: 700;
            margin: 0 0 5px 0;
        }
        .footer-tagline {
            color: rgba(255, 255, 255, 0.95);
            font-size: 14px;
            margin: 0;
        }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="header">
            <h1>Registration Confirmed!</h1>
        </div>

        {{#HasEventImage}}
        <div class="event-image-container">
            <img src="{{EventImageUrl}}" alt="Event Image" class="event-image" />
        </div>
        {{/HasEventImage}}

        <div class="content">
            <p class="greeting">Hi {{UserName}},</p>
            <p>Thank you for registering for <strong>{{EventTitle}}</strong>! Your payment has been successfully processed.</p>

            <div class="event-info">
                <h2>Event Details</h2>
                <div class="info-row">
                    <span class="info-label">Date & Time:</span>
                    <span class="info-value">{{EventDateTime}}</span>
                </div>
                <div class="info-row">
                    <span class="info-label">Location:</span>
                    <span class="info-value">{{EventLocation}}</span>
                </div>
                <div class="info-row">
                    <span class="info-label">Registration Date:</span>
                    <span class="info-value">{{RegistrationDate}}</span>
                </div>
            </div>

            {{#HasAttendeeDetails}}
            <div class="attendee-section">
                <h2>Attendees</h2>
                {{Attendees}}
            </div>
            {{/HasAttendeeDetails}}

            <div class="payment-section">
                <h2>Payment Confirmation</h2>
                <div class="info-row">
                    <span class="info-label">Amount Paid:</span>
                    <span class="info-value">{{AmountPaid}}</span>
                </div>
                <div class="info-row">
                    <span class="info-label">Payment ID:</span>
                    <span class="info-value">{{PaymentIntentId}}</span>
                </div>
                <div class="info-row">
                    <span class="info-label">Payment Date:</span>
                    <span class="info-value">{{PaymentDate}}</span>
                </div>
            </div>

            {{#HasTicket}}
            <div class="ticket-section">
                <h2>Your Ticket</h2>
                <p>Your ticket is attached to this email as a PDF. Please present it at the event entrance.</p>
                <div class="ticket-code">{{TicketCode}}</div>
                <div class="info-row">
                    <span class="info-label">Valid Until:</span>
                    <span class="info-value">{{TicketExpiryDate}}</span>
                </div>
            </div>
            {{/HasTicket}}

            {{#HasContactInfo}}
            <div class="contact-section">
                <h2>Your Contact Information</h2>
                <div class="info-row">
                    <span class="info-label">Email:</span>
                    <span class="info-value">{{ContactEmail}}</span>
                </div>
                {{#ContactPhone}}
                <div class="info-row">
                    <span class="info-label">Phone:</span>
                    <span class="info-value">{{ContactPhone}}</span>
                </div>
                {{/ContactPhone}}
            </div>
            {{/HasContactInfo}}

            <p style="margin-top: 30px;">We look forward to seeing you at the event!</p>
        </div>

        <div class="footer">
            <p class="footer-brand">LankaConnect</p>
            <p class="footer-tagline">Sri Lankan Community Hub</p>
        </div>
    </div>
</body>
</html>',

    updated_at = NOW()

WHERE name = 'ticket-confirmation';

-- Verify update succeeded
DO $$
DECLARE
    updated_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO updated_count
    FROM communications.email_templates
    WHERE name = 'ticket-confirmation'
      AND html_template LIKE '%{{EventDateTime}}%'
      AND updated_at > NOW() - INTERVAL '1 minute';

    IF updated_count = 0 THEN
        RAISE EXCEPTION 'Template update failed. Template not updated.';
    ELSE
        RAISE NOTICE 'Template updated successfully. New variables: EventDateTime, HasAttendeeDetails, HasEventImage, HasTicket, HasContactInfo';
    END IF;
END $$;

COMMIT;

-- Verification query (run separately)
SELECT
    name,
    subject_template,
    CASE
        WHEN html_template LIKE '%{{EventDateTime}}%' THEN 'NEW FORMAT (FIXED)'
        WHEN html_template LIKE '%{{EventStartDate}}%' THEN 'OLD FORMAT (NEEDS FIX)'
        ELSE 'UNKNOWN'
    END as template_version,
    updated_at
FROM communications.email_templates
WHERE name = 'ticket-confirmation';
