-- ================================================================
-- EMERGENCY FIX: Insert Missing 'event-details' Email Template
-- ================================================================
-- Issue: Event notification emails failing with "Template not found"
-- Root Cause: Migration ran but template not inserted in database
-- Fix: Manually insert the template
-- Safe to run: Idempotent (won't create duplicates)
-- ================================================================

BEGIN;

-- Check if template already exists
DO $$
DECLARE
    template_exists BOOLEAN;
BEGIN
    SELECT EXISTS(
        SELECT 1 FROM "EmailTemplates"
        WHERE "Name" = 'event-details'
    ) INTO template_exists;

    IF template_exists THEN
        RAISE NOTICE '✅ Template "event-details" already exists - skipping insert';
    ELSE
        RAISE NOTICE '🔧 Inserting missing "event-details" template...';

        INSERT INTO "EmailTemplates" (
            "Id",
            "Name",
            "Subject",
            "Body",
            "IsActive",
            "CreatedAt",
            "UpdatedAt"
        ) VALUES (
            gen_random_uuid(),
            'event-details',
            'Event Details: {{EventTitle}}',
            '<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Event Details</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #d32f2f; color: white; padding: 20px; text-align: center; }
        .content { background-color: #f9f9f9; padding: 20px; }
        .event-details { background-color: white; padding: 15px; margin: 15px 0; border-left: 4px solid #d32f2f; }
        .footer { text-align: center; padding: 20px; font-size: 12px; color: #666; }
        .button { display: inline-block; padding: 12px 24px; background-color: #d32f2f; color: white; text-decoration: none; border-radius: 4px; margin: 15px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>{{EventTitle}}</h1>
        </div>
        <div class="content">
            <p>Hello,</p>
            <p>{{Message}}</p>

            <div class="event-details">
                <h2>Event Information</h2>
                <p><strong>Event:</strong> {{EventTitle}}</p>
                <p><strong>Date & Time:</strong> {{EventDateTime}}</p>
                <p><strong>Location:</strong> {{EventLocation}}</p>
                <p><strong>Description:</strong> {{EventDescription}}</p>
            </div>

            <p style="text-align: center;">
                <a href="{{EventUrl}}" class="button">View Event Details</a>
            </p>
        </div>
        <div class="footer">
            <p>&copy; 2026 LankaConnect. All rights reserved.</p>
            <p>Sri Lankan Community Hub</p>
        </div>
    </div>
</body>
</html>',
            true,
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        );

        RAISE NOTICE '✅ Template "event-details" inserted successfully';
    END IF;
END $$;

COMMIT;

-- ================================================================
-- VERIFICATION QUERIES
-- ================================================================

-- 1. Verify template exists
SELECT
    "Id",
    "Name",
    "Subject",
    "IsActive",
    "CreatedAt"
FROM "EmailTemplates"
WHERE "Name" = 'event-details';

-- 2. Count all templates
SELECT COUNT(*) as total_templates FROM "EmailTemplates";

-- 3. List all template names
SELECT "Name", "IsActive" FROM "EmailTemplates" ORDER BY "Name";

-- ================================================================
-- Expected Output:
-- ✅ Should see 1 row with name = 'event-details'
-- ✅ IsActive should be 'true'
-- ================================================================
