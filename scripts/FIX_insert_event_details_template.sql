-- ================================================================
-- FIX: Insert Missing 'event-details' Email Template
-- ================================================================
-- Based on: 20260113020400_Phase6A61_AddEventDetailsTemplate.cs
-- Issue: Migration exists in code but template not in database
-- This script manually inserts the template from the migration
-- ================================================================

-- Check if template already exists
DO $$
DECLARE
    template_exists BOOLEAN;
BEGIN
    SELECT EXISTS(
        SELECT 1 FROM communications.email_templates
        WHERE name = 'event-details'
    ) INTO template_exists;

    IF template_exists THEN
        RAISE NOTICE '✅ Template "event-details" already exists - skipping insert';
    ELSE
        RAISE NOTICE '🔧 Inserting missing "event-details" template from migration...';

        -- Exact SQL from migration file
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
        VALUES
        (
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
        );

        RAISE NOTICE '✅ Template "event-details" inserted successfully';
    END IF;
END $$;

-- ================================================================
-- VERIFICATION QUERIES
-- ================================================================

-- Verify template exists
SELECT 
    name,
    description,
    subject_template,
    type,
    category,
    is_active,
    created_at
FROM communications.email_templates
WHERE name = 'event-details';

-- Count all templates
SELECT COUNT(*) as total_templates FROM communications.email_templates;

-- List all template names
SELECT name, category, is_active FROM communications.email_templates ORDER BY name;
