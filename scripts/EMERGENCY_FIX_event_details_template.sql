-- ============================================================================
-- EMERGENCY FIX: Phase 6A.61 - Insert Missing event-details Template
-- ============================================================================
-- Issue: Event notification emails failing with "Email template 'event-details' not found"
-- Root Cause: Migration exists in code but not applied to database
-- Fix: Manually insert template into database
-- Impact: ZERO downtime, immediate fix
-- Date: 2026-01-16
-- ============================================================================

-- Check if template already exists (should return 0 rows if issue is present)
SELECT
    id,
    name,
    is_active,
    category,
    LENGTH(html_template) as html_length,
    LENGTH(text_template) as text_length,
    created_at
FROM communications.email_templates
WHERE name = 'event-details';

-- Insert the missing template (safe - uses WHERE NOT EXISTS to prevent duplicates)
INSERT INTO communications.email_templates
(
    "Id",
    "name",
    "description",
    "subject_template",
    "text_template",
    "html_template",
    "type",
    "category",
    "is_active",
    "created_at"
)
SELECT
    gen_random_uuid(),
    'event-details',
    'Manual event notification template sent by organizers to attendees with event details',
    '{{EventTitle}} - Event Details',
    'Dear Community Member,

Here are the details for {{EventTitle}}:

📅 Date & Time: {{EventDate}}
📍 Location: {{EventLocation}}
💰 Pricing: {{PricingDetails}}

View Event Details: {{EventDetailsUrl}}

{{#HasOrganizerContact}}
Organizer: {{OrganizerName}}
{{#OrganizerEmail}}📧 {{OrganizerEmail}}{{/OrganizerEmail}}
{{#OrganizerPhone}}📱 {{OrganizerPhone}}{{/OrganizerPhone}}
{{/HasOrganizerContact}}

LankaConnect - Sri Lankan Community Hub',
    '<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>{{EventTitle}}</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', sans-serif;">
  <div style="max-width: 600px; margin: 0 auto; background-color: #ffffff;">
    <!-- Header with Sri Lankan gradient -->
    <div style="background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px; text-align: center;">
      <h1 style="color: white; margin: 0; font-size: 28px; font-weight: bold;">{{EventTitle}}</h1>
    </div>

    <!-- Content -->
    <div style="padding: 30px;">
      <p style="color: #4B5563; margin-top: 0;">Dear Community Member,</p>
      <p style="color: #4B5563;">Here are the details for <strong>{{EventTitle}}</strong>:</p>

      <div style="background: #f5f5f5; padding: 20px; margin: 20px 0; border-radius: 8px;">
        <p style="color: #1F2937; margin: 8px 0;"><strong>📅 Date & Time:</strong> {{EventDate}}</p>
        <p style="color: #1F2937; margin: 8px 0;"><strong>📍 Location:</strong> {{EventLocation}}</p>
        {{#IsFreeEvent}}
        <p style="color: #1F2937; margin: 8px 0;"><strong>💰 Pricing:</strong> Free Event</p>
        {{/IsFreeEvent}}
        {{^IsFreeEvent}}
        <p style="color: #1F2937; margin: 8px 0;"><strong>💰 Pricing:</strong> {{PricingDetails}}</p>
        {{/IsFreeEvent}}
      </div>

      <p style="text-align: center; margin: 30px 0;">
        <a href="{{EventDetailsUrl}}" style="background: #FF6600; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;">View Event Details</a>
      </p>

      {{#HasSignUpLists}}
      <p style="text-align: center; margin: 20px 0;">
        <a href="{{SignUpListsUrl}}" style="color: #FF6600; text-decoration: underline;">View Sign-Up Lists</a>
      </p>
      {{/HasSignUpLists}}

      {{#HasOrganizerContact}}
      <div style="border-top: 1px solid #E5E7EB; padding-top: 20px; margin-top: 30px;">
        <p style="color: #1F2937; margin: 8px 0;"><strong>Organizer:</strong> {{OrganizerName}}</p>
        {{#OrganizerEmail}}<p style="color: #4B5563; margin: 8px 0;">📧 {{OrganizerEmail}}</p>{{/OrganizerEmail}}
        {{#OrganizerPhone}}<p style="color: #4B5563; margin: 8px 0;">📱 {{OrganizerPhone}}</p>{{/OrganizerPhone}}
      </div>
      {{/HasOrganizerContact}}
    </div>

    <!-- Footer -->
    <div style="background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 20px; text-align: center;">
      <p style="color: white; margin: 0; font-size: 14px;">LankaConnect - Sri Lankan Community Hub</p>
    </div>
  </div>
</body>
</html>',
    'Transactional',
    'Events',
    true,
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM communications.email_templates WHERE name = 'event-details'
);

-- Verify the template was inserted successfully
SELECT
    id,
    name,
    is_active,
    category,
    LENGTH(html_template) as html_length,
    LENGTH(text_template) as text_length,
    LENGTH(subject_template) as subject_length,
    created_at,
    CASE
        WHEN LENGTH(html_template) > 100 AND LENGTH(text_template) > 50 AND LENGTH(subject_template) > 5 AND is_active = true
        THEN '✅ TEMPLATE OK - Ready to send emails'
        ELSE '❌ TEMPLATE INCOMPLETE - Review template data'
    END as status
FROM communications.email_templates
WHERE name = 'event-details';

-- ============================================================================
-- DEPLOYMENT INSTRUCTIONS
-- ============================================================================
--
-- STAGING DATABASE:
-- psql "host=lankaconnect-staging-db.postgres.database.azure.com \
--       port=5432 \
--       dbname=LankaConnectDB \
--       user=lankaconnect \
--       sslmode=require" \
--   -f scripts/EMERGENCY_FIX_event_details_template.sql
--
-- PRODUCTION DATABASE (after staging verification):
-- psql "host=lankaconnect-prod-db.postgres.database.azure.com \
--       port=5432 \
--       dbname=LankaConnectDB \
--       user=lankaconnect \
--       sslmode=require" \
--   -f scripts/EMERGENCY_FIX_event_details_template.sql
--
-- ============================================================================
-- VERIFICATION STEPS AFTER DEPLOYMENT
-- ============================================================================
--
-- 1. Confirm template exists:
--    SELECT COUNT(*) FROM communications.email_templates WHERE name = 'event-details';
--    Expected: 1
--
-- 2. Test email send from UI:
--    - Login as event organizer
--    - Navigate to event communications tab
--    - Click "Send an Email" button
--    - Verify success in Hangfire dashboard
--
-- 3. Check logs for success:
--    [DIAG-EMAIL] Template FOUND - IsActive: True
--    [DIAG-NOTIF-JOB] Email X/Y SUCCESS
--    [DIAG-NOTIF-JOB] COMPLETED - Success: X, Failed: 0
--
-- ============================================================================