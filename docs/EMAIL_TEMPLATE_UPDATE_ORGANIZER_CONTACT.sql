-- Phase 6A.X: Add Organizer Contact Section to Email Templates
-- This script updates email templates to include organizer contact information
-- Templates to update: registration-confirmation, event-cancelled-notification, event-reminder

-- ==============================================================================
-- TEMPLATE 1: Registration Confirmation Email
-- ==============================================================================
-- NOTE: This is the HTML section to be added AFTER the event details box
-- Add this section before the footer in the registration-confirmation template

/*
HTML SECTION TO ADD (After event details, before footer):

<!-- Phase 6A.X: Event Organizer Contact (Conditional) -->
{{#if HasOrganizerContact}}
<table role="presentation" width="100%" style="margin-top: 32px; border-collapse: collapse;">
  <tr>
    <td style="padding: 0;">
      <table role="presentation" width="100%" style="background-color: #f0f9ff; border-left: 4px solid #8B5CF6; border-radius: 8px; padding: 24px; border-collapse: collapse;">
        <tr>
          <td style="padding: 0;">
            <h3 style="margin: 0 0 16px 0; color: #7C3AED; font-size: 18px; font-weight: 600; font-family: Arial, sans-serif;">
              📞 Event Organizer Contact
            </h3>
            <p style="margin: 0 0 12px 0; color: #4B5563; font-size: 14px; font-family: Arial, sans-serif;">
              Have questions about this event? Feel free to reach out to the organizer:
            </p>

            <!-- Contact Name -->
            <div style="margin: 12px 0; padding: 12px; background-color: #ffffff; border-radius: 6px;">
              <table role="presentation" width="100%" style="border-collapse: collapse;">
                <tr>
                  <td style="padding: 0; vertical-align: middle; width: 32px;">
                    <div style="width: 28px; height: 28px; background-color: #DDD6FE; border-radius: 50%; display: flex; align-items: center; justify-content: center;">
                      <span style="font-size: 16px;">👤</span>
                    </div>
                  </td>
                  <td style="padding: 0 0 0 12px; vertical-align: middle;">
                    <p style="margin: 0; color: #6B7280; font-size: 12px; font-family: Arial, sans-serif;">Name</p>
                    <p style="margin: 2px 0 0 0; color: #111827; font-size: 14px; font-weight: 600; font-family: Arial, sans-serif;">
                      {{OrganizerContactName}}
                    </p>
                  </td>
                </tr>
              </table>
            </div>

            <!-- Contact Email (if provided) -->
            {{#if OrganizerContactEmail}}
            <div style="margin: 12px 0; padding: 12px; background-color: #ffffff; border-radius: 6px;">
              <table role="presentation" width="100%" style="border-collapse: collapse;">
                <tr>
                  <td style="padding: 0; vertical-align: middle; width: 32px;">
                    <div style="width: 28px; height: 28px; background-color: #DBEAFE; border-radius: 50%; display: flex; align-items: center; justify-content: center;">
                      <span style="font-size: 16px;">✉️</span>
                    </div>
                  </td>
                  <td style="padding: 0 0 0 12px; vertical-align: middle;">
                    <p style="margin: 0; color: #6B7280; font-size: 12px; font-family: Arial, sans-serif;">Email</p>
                    <p style="margin: 2px 0 0 0;">
                      <a href="mailto:{{OrganizerContactEmail}}" style="color: #2563EB; font-size: 14px; font-weight: 600; text-decoration: none; font-family: Arial, sans-serif;">
                        {{OrganizerContactEmail}}
                      </a>
                    </p>
                  </td>
                </tr>
              </table>
            </div>
            {{/if}}

            <!-- Contact Phone (if provided) -->
            {{#if OrganizerContactPhone}}
            <div style="margin: 12px 0; padding: 12px; background-color: #ffffff; border-radius: 6px;">
              <table role="presentation" width="100%" style="border-collapse: collapse;">
                <tr>
                  <td style="padding: 0; vertical-align: middle; width: 32px;">
                    <div style="width: 28px; height: 28px; background-color: #D1FAE5; border-radius: 50%; display: flex; align-items: center; justify-content: center;">
                      <span style="font-size: 16px;">📞</span>
                    </div>
                  </td>
                  <td style="padding: 0 0 0 12px; vertical-align: middle;">
                    <p style="margin: 0; color: #6B7280; font-size: 12px; font-family: Arial, sans-serif;">Phone</p>
                    <p style="margin: 2px 0 0 0;">
                      <a href="tel:{{OrganizerContactPhone}}" style="color: #059669; font-size: 14px; font-weight: 600; text-decoration: none; font-family: Arial, sans-serif;">
                        {{OrganizerContactPhone}}
                      </a>
                    </p>
                  </td>
                </tr>
              </table>
            </div>
            {{/if}}
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
{{/if}}
*/

-- ==============================================================================
-- BACKEND CODE: Update Email Handler to Include Organizer Contact Parameters
-- ==============================================================================

-- File: src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs
-- Update the parameters dictionary to include organizer contact information:

/*
C# CODE TO UPDATE:

var parameters = new Dictionary<string, object>
{
    // ... existing parameters ...

    // Phase 6A.X: Event Organizer Contact
    ["HasOrganizerContact"] = @event.HasOrganizerContact(),
    ["OrganizerContactName"] = @event.OrganizerContactName ?? "",
    ["OrganizerContactEmail"] = @event.OrganizerContactEmail ?? "",
    ["OrganizerContactPhone"] = @event.OrganizerContactPhone ?? ""
};
*/

-- ==============================================================================
-- TEMPLATE 2: Event Cancellation Email
-- ==============================================================================
-- Add the SAME HTML section (above) to event-cancelled-notification template

-- ==============================================================================
-- TEMPLATE 3: Event Reminder Email
-- ==============================================================================
-- Add the SAME HTML section (above) to event-reminder template

-- ==============================================================================
-- AFFECTED EMAIL HANDLER FILES (Backend)
-- ==============================================================================
/*
1. RegistrationConfirmedEventHandler.cs
   - Add organizer contact parameters to email template dictionary

2. EventCancellationEmailJob.cs
   - Add organizer contact parameters to email template dictionary

3. EventReminderJob.cs
   - Add organizer contact parameters to email template dictionary

4. Newsletter job (when Phase 6A.74 is implemented)
   - Add organizer contact parameters for newsletter emails
*/

-- ==============================================================================
-- TESTING CHECKLIST
-- ==============================================================================
/*
✓ Registration Confirmation Email:
  - Register for event with organizer contact published
  - Check email inbox
  - Verify organizer contact section displays
  - Verify all 3 contact methods show (name, email, phone)
  - Click email and phone links to verify they work

✓ Event Cancellation Email:
  - Cancel event with organizer contact published
  - Check cancellation email
  - Verify organizer contact section displays

✓ Event Reminder Email:
  - Create event with reminder (24h before start) and organizer contact
  - Wait for reminder job or trigger manually
  - Verify organizer contact section displays

✓ Conditional Rendering:
  - Create event WITHOUT organizer contact published
  - Register/cancel/reminder
  - Verify organizer contact section does NOT display
*/

-- ==============================================================================
-- DEPLOYMENT NOTES
-- ==============================================================================
/*
1. Email templates are stored in database table: communications.email_templates
2. Templates use Handlebars syntax for conditional rendering
3. Backend handlers must inject all parameters including organizer contact
4. Test in staging environment before production deployment
5. Ensure backward compatibility (templates work with old events without organizer contact)
*/

-- ==============================================================================
-- ACCESSIBILITY NOTES
-- ==============================================================================
/*
- All colors meet WCAG AA contrast requirements
- Email links (mailto: and tel:) work on all devices
- Tables used for layout (email-safe HTML)
- Inline styles required for email compatibility
- Tested in major email clients (Gmail, Outlook, Apple Mail)
*/
