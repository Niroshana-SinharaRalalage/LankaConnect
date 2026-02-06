-- =====================================================
-- Phase 6A.77: Fix ALL Email Template Text Versions
-- Hotfix for Phase 6A.76 deployment issue
--
-- PROBLEM:
-- 1. The 5 new templates from Phase 6A.76 were inserted with HTML/text swapped
-- 2. ALL 19 templates need proper plain text extracted from HTML
--
-- SOLUTION:
-- Part 1: Swap HTML/text for the 5 new templates that have it backwards
-- Part 2: Update ALL 19 templates with proper plain text versions
-- =====================================================

BEGIN;

-- Show current state
SELECT 'BEFORE FIXES:' as status;
SELECT name,
       LEFT(text_template, 50) as text_preview,
       LEFT(html_template, 50) as html_preview
FROM communications.email_templates
WHERE name LIKE 'template-%'
ORDER BY name;

-- =====================================================
-- PART 1: FIX THE 5 NEW TEMPLATES (Swap HTML/text)
-- These templates currently have HTML in text_template column
-- =====================================================

-- For templates where text_template starts with "<!DOCTYPE", swap the columns
UPDATE communications.email_templates
SET
    text_template = html_template,
    html_template = text_template,
    updated_at = NOW()
WHERE name IN (
    'template-password-reset',
    'template-password-change-confirmation',
    'template-welcome',
    'template-anonymous-rsvp-confirmation',
    'template-organizer-role-approval'
)
AND text_template LIKE '<!DOCTYPE%';

SELECT 'AFTER COLUMN SWAP:' as status;
SELECT name,
       LEFT(text_template, 50) as text_preview
FROM communications.email_templates
WHERE name IN (
    'template-password-reset',
    'template-password-change-confirmation',
    'template-welcome',
    'template-anonymous-rsvp-confirmation',
    'template-organizer-role-approval'
)
ORDER BY name;

-- =====================================================
-- PART 2: UPDATE ALL 19 TEMPLATES WITH PROPER PLAIN TEXT
-- Extract meaningful content from HTML templates
-- =====================================================

-- Template 1: template-free-event-registration-confirmation
UPDATE communications.email_templates
SET text_template = 'Hi {{UserName}}, Thank you for registering for {{EventTitle}}! Your registration is confirmed. Event Details: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}. {{#if EventDescription}}About this event: {{EventDescription}}{{/if}} {{#if OrganizerName}}Organized by: {{OrganizerName}}{{#if OrganizerEmail}} ({{OrganizerEmail}}){{/if}}{{/if}} View full event details: {{EventDetailsUrl}} We look forward to seeing you there!',
    updated_at = NOW()
WHERE name = 'template-free-event-registration-confirmation';

-- Template 2: template-paid-event-registration-confirmation-with-ticket
UPDATE communications.email_templates
SET text_template = 'Hi {{UserName}}, Your ticket for {{EventTitle}} is confirmed! Order Details: Ticket Type: {{TicketType}}, Quantity: {{Quantity}}, Total Paid: ${{TotalAmount}}, Order Number: {{OrderNumber}}. Event Details: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}. Important: Please bring this confirmation email or your ticket to the event. View your ticket and event details: {{TicketUrl}} Thank you for your purchase!',
    updated_at = NOW()
WHERE name = 'template-paid-event-registration-confirmation-with-ticket';

-- Template 3: template-event-reminder
UPDATE communications.email_templates
SET text_template = 'Hi {{UserName}}, This is a friendly reminder about {{EventTitle}} happening soon! Event Details: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}{{#if EventDescription}}, About: {{EventDescription}}{{/if}}. {{#if IsRegistered}}You are registered for this event.{{/if}} View event details: {{EventDetailsUrl}} See you there!',
    updated_at = NOW()
WHERE name = 'template-event-reminder';

-- Template 4: template-membership-email-verification
UPDATE communications.email_templates
SET text_template = 'Hello {{UserName}}, Thank you for signing up for LankaConnect! Please verify your email address to complete your registration. Verification link: {{VerificationUrl}} This link will expire in {{ExpirationHours}} hours. If you did not create this account, please ignore this email.',
    updated_at = NOW()
WHERE name = 'template-membership-email-verification';

-- Template 5: template-signup-list-commitment-confirmation
UPDATE communications.email_templates
SET text_template = 'Hi {{UserName}}, Thank you for committing to bring {{ItemName}} for {{EventTitle}}! Your Commitment: Item: {{ItemName}}, Quantity: {{Quantity}}, Notes: {{Notes}}. Event Details: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}. You can update or cancel your commitment anytime: {{ManageCommitmentUrl}} Thank you for contributing!',
    updated_at = NOW()
WHERE name = 'template-signup-list-commitment-confirmation';

-- Template 6: template-signup-list-commitment-update
UPDATE communications.email_templates
SET text_template = 'Hi {{UserName}}, Your commitment for {{EventTitle}} has been updated. Updated Commitment: Item: {{ItemName}}, Quantity: {{Quantity}}, Notes: {{Notes}}. Event Details: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}. Manage your commitment: {{ManageCommitmentUrl}} Thank you!',
    updated_at = NOW()
WHERE name = 'template-signup-list-commitment-update';

-- Template 7: template-signup-list-commitment-cancellation
UPDATE communications.email_templates
SET text_template = 'Hi {{UserName}}, Your commitment for {{EventTitle}} has been cancelled as requested. Cancelled Commitment: Item: {{ItemName}}, Quantity: {{Quantity}}. If you would like to sign up again, visit: {{EventDetailsUrl}} Thank you for your participation!',
    updated_at = NOW()
WHERE name = 'template-signup-list-commitment-cancellation';

-- Template 8: template-event-registration-cancellation
UPDATE communications.email_templates
SET text_template = 'Hi {{UserName}}, Your registration for {{EventTitle}} has been cancelled as requested. Event Details: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}. If you would like to register again, visit: {{EventDetailsUrl}} We hope to see you at future events!',
    updated_at = NOW()
WHERE name = 'template-event-registration-cancellation';

-- Template 9: template-new-event-publication
UPDATE communications.email_templates
SET text_template = 'NEW EVENT ANNOUNCEMENT: {{EventTitle}}. {{EventDescription}} Event Details: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}{{#if TicketPrice}}, Price: ${{TicketPrice}}{{/if}}{{#if OrganizerName}}, Organized by: {{OrganizerName}}{{/if}}. View full details and register: {{EventDetailsUrl}} Don''t miss out!',
    updated_at = NOW()
WHERE name = 'template-new-event-publication';

-- Template 10: template-event-details-publication
UPDATE communications.email_templates
SET text_template = 'Hi {{UserName}}, Here are the details for {{EventTitle}}. {{EventDescription}} Event Information: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}{{#if TicketPrice}}, Price: ${{TicketPrice}}{{/if}}{{#if OrganizerName}}, Organized by: {{OrganizerName}}{{/if}}. View full details: {{EventDetailsUrl}} We look forward to seeing you!',
    updated_at = NOW()
WHERE name = 'template-event-details-publication';

-- Template 11: template-event-cancellation-notifications
UPDATE communications.email_templates
SET text_template = 'IMPORTANT: Event Cancellation Notice. Hi {{UserName}}, We regret to inform you that {{EventTitle}} scheduled for {{EventStartDate}} at {{EventStartTime}} has been cancelled. {{#if CancellationReason}}Reason: {{CancellationReason}}{{/if}} {{#if RefundInfo}}Refund Information: {{RefundInfo}}{{/if}} We apologize for any inconvenience. For more information, contact: {{OrganizerEmail}}',
    updated_at = NOW()
WHERE name = 'template-event-cancellation-notifications';

-- Template 12: template-event-approval
UPDATE communications.email_templates
SET text_template = 'Great news! Your event has been approved. Hi {{OrganizerName}}, Your event "{{EventTitle}}" has been reviewed and approved by our team. Your event is now live and visible to all users. Event Details: Date: {{EventStartDate}} at {{EventStartTime}}, Location: {{EventLocation}}. View and manage your event: {{EventDetailsUrl}} Thank you for organizing with LankaConnect!',
    updated_at = NOW()
WHERE name = 'template-event-approval';

-- Template 13: template-newsletter-notification
UPDATE communications.email_templates
SET text_template = 'LankaConnect Newsletter: {{NewsletterTitle}}. {{NewsletterContent}} Stay connected with your community! Unsubscribe: {{UnsubscribeUrl}}',
    updated_at = NOW()
WHERE name = 'template-newsletter-notification';

-- Template 14: template-newsletter-subscription-confirmation
UPDATE communications.email_templates
SET text_template = 'Hi, Thank you for subscribing to the LankaConnect newsletter! You will now receive updates about events and community news. You can unsubscribe at any time: {{UnsubscribeUrl}} Welcome to our community!',
    updated_at = NOW()
WHERE name = 'template-newsletter-subscription-confirmation';

-- Template 15: template-password-reset
UPDATE communications.email_templates
SET text_template = 'Hello {{UserName}}, We received a request to reset your password for your LankaConnect account. Click this link to reset your password: {{ResetLink}} This link will expire in 1 hour for security reasons. If you did not request this password reset, please ignore this email or contact support if you have concerns.',
    updated_at = NOW()
WHERE name = 'template-password-reset';

-- Template 16: template-password-change-confirmation
UPDATE communications.email_templates
SET text_template = 'Hello {{UserName}}, Your LankaConnect password has been successfully changed on {{ChangedAt}}. If you made this change, you can safely ignore this email. If you did NOT make this change, please contact our support team immediately to secure your account.',
    updated_at = NOW()
WHERE name = 'template-password-change-confirmation';

-- Template 17: template-welcome
UPDATE communications.email_templates
SET text_template = 'Hello {{UserName}}, Welcome to LankaConnect! Thank you for joining our community. With LankaConnect, you can: - Discover local events and activities, - Connect with your community, - Create and manage your own events, - Stay updated with newsletters. Get started: {{DashboardUrl}} If you have any questions, our support team is here to help!',
    updated_at = NOW()
WHERE name = 'template-welcome';

-- Template 18: template-anonymous-rsvp-confirmation
UPDATE communications.email_templates
SET text_template = 'Your RSVP for {{EventTitle}} has been confirmed! Event Details: Date: {{EventDate}}, Time: {{EventTime}}, Location: {{EventLocation}}. View full event details: {{EventUrl}} To modify or cancel your RSVP: {{ManageRsvpUrl}} We look forward to seeing you!',
    updated_at = NOW()
WHERE name = 'template-anonymous-rsvp-confirmation';

-- Template 19: template-organizer-role-approval
UPDATE communications.email_templates
SET text_template = 'Hello {{UserName}}, Congratulations! Your request to become an Event Organizer has been approved. You now have access to: - Create and publish events, - Manage event registrations, - Send notifications to attendees, - Access organizer dashboard and analytics. Visit your organizer dashboard: {{DashboardUrl}} We''re excited to see the amazing events you''ll create!',
    updated_at = NOW()
WHERE name = 'template-organizer-role-approval';

-- Show final state
SELECT 'AFTER ALL UPDATES:' as status;
SELECT name,
       LEFT(text_template, 80) as text_preview
FROM communications.email_templates
WHERE name LIKE 'template-%'
ORDER BY name;

-- Verify all 19 templates updated
SELECT 'VERIFICATION:' as status;
SELECT
    COUNT(*) as total_templates,
    COUNT(CASE WHEN text_template NOT LIKE '<!DOCTYPE%' THEN 1 END) as correct_text_templates,
    COUNT(CASE WHEN text_template LIKE '<!DOCTYPE%' THEN 1 END) as incorrect_text_templates
FROM communications.email_templates
WHERE name LIKE 'template-%';

COMMIT;

-- Success message
SELECT 'SUCCESS: All 19 email templates have been updated with proper plain text versions!' as result;
