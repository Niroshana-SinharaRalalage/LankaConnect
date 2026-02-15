-- Phase 6A.113: Export 14 Templates for "View Signup Forms" Button Addition
-- Purpose: Export event templates needing "View Signup Forms" button
-- Usage: Run against staging database, add button HTML, then import to production

-- Expected Output: 14 templates
SELECT
    name,
    subject,
    html_content
FROM communications.email_templates
WHERE name IN (
    'template-free-event-registration-confirmation',
    'template-paid-event-registration-confirmation-with-ticket',
    'template-event-registration-cancellation',
    'template-attendees-added-confirmation',
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-event-approval',
    'template-event-rejected',
    'template-event-postponed',
    'template-new-event-publication',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-signup-list-commitment-cancellation',
    'template-preliminary-registration-payment-pending'
)
ORDER BY name;

-- Button HTML Template:
-- Insert this after the "View Event Details" button section
-- {{#if HasSignupForms}}
--   <tr>
--     <td style="padding: 20px 0;">
--       <a href="{{SignupFormsUrl}}"
--          style="background-color: #10B981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: 600;">
--         View Signup Forms
--       </a>
--     </td>
--   </tr>
-- {{/if}}

-- Priority Levels:
-- HIGH: registration-confirmation, attendees-added, event-reminder, preliminary-registration
-- MEDIUM: registration-cancellation, event-cancellation, commitment-update, event-postponed, new-event-publication
-- LOW: event-approval, event-rejected, commitment-cancellation
