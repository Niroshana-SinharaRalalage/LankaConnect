-- Phase 6A.55: Detection Query for Corrupt Attendee JSONB Records
-- Purpose: Identify registrations with null age_category values in attendees JSONB
-- Run this on Azure Staging database to assess scope before implementing fix

-- ====================================================================================
-- Query 1: Quick Count - Total Registrations with Null AgeCategory
-- ====================================================================================

SELECT COUNT(DISTINCT id) as registrations_with_null_age_category
FROM registrations
WHERE attendees IS NOT NULL
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(attendees) elem
      WHERE elem->>'age_category' IS NULL
         OR elem->>'ageCategory' IS NULL
         OR elem->>'age_category' = 'null'
  );

-- ====================================================================================
-- Query 2: Detailed Breakdown - Registrations and Attendee Counts
-- ====================================================================================

SELECT
    r.id as registration_id,
    r.event_id,
    r.status,
    r.created_at,
    jsonb_array_length(r.attendees) as total_attendees,
    (
        SELECT COUNT(*)
        FROM jsonb_array_elements(r.attendees) elem
        WHERE elem->>'age_category' IS NULL
           OR elem->>'ageCategory' IS NULL
           OR elem->>'age_category' = 'null'
    ) as null_age_category_count,
    r.attendees as attendees_jsonb
FROM registrations r
WHERE r.attendees IS NOT NULL
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
         OR elem->>'ageCategory' IS NULL
         OR elem->>'age_category' = 'null'
  )
ORDER BY r.created_at DESC
LIMIT 50;

-- ====================================================================================
-- Query 3: Event Impact Analysis - Which Events Are Affected?
-- ====================================================================================

SELECT
    e.id as event_id,
    e.title,
    e.start_date,
    COUNT(DISTINCT r.id) as affected_registrations,
    SUM(jsonb_array_length(r.attendees)) as total_attendees
FROM events e
JOIN registrations r ON r.event_id = e.id
WHERE r.attendees IS NOT NULL
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(r.attendees) elem
      WHERE elem->>'age_category' IS NULL
         OR elem->>'ageCategory' IS NULL
         OR elem->>'age_category' = 'null'
  )
GROUP BY e.id, e.title, e.start_date
ORDER BY affected_registrations DESC;

-- ====================================================================================
-- Query 4: Sample Corrupt Records - First 10 for Inspection
-- ====================================================================================

SELECT
    id as registration_id,
    event_id,
    user_id,
    status,
    created_at,
    attendees
FROM registrations
WHERE attendees IS NOT NULL
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(attendees) elem
      WHERE elem->>'age_category' IS NULL
         OR elem->>'ageCategory' IS NULL
         OR elem->>'age_category' = 'null'
  )
ORDER BY created_at DESC
LIMIT 10;

-- ====================================================================================
-- Query 5: JSONB Structure Analysis - Check for Field Name Variations
-- ====================================================================================

-- Check if age_category or ageCategory is used
SELECT
    'age_category' as field_name,
    COUNT(*) as count
FROM registrations
WHERE attendees IS NOT NULL
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(attendees) elem
      WHERE elem ? 'age_category'
  )

UNION ALL

SELECT
    'ageCategory' as field_name,
    COUNT(*) as count
FROM registrations
WHERE attendees IS NOT NULL
  AND EXISTS (
      SELECT 1
      FROM jsonb_array_elements(attendees) elem
      WHERE elem ? 'ageCategory'
  );

-- ====================================================================================
-- Expected Results & Interpretation
-- ====================================================================================

/*
Query 1: Should return a number (e.g., 15 registrations)
  - If 0: No corrupt data exists (bug may have been fixed differently)
  - If > 0: Need to proceed with Phase 6A.55 fix

Query 2: Shows detailed breakdown
  - registration_id: Use this to test API endpoints
  - null_age_category_count: How many attendees per registration are affected
  - attendees_jsonb: Inspect actual JSONB structure

Query 3: Event-level impact
  - Helps prioritize which events to test after fix
  - Shows organizer-facing impact (can't view attendees for these events)

Query 4: Sample records for testing
  - Use these registration_ids to test API endpoints
  - Verify fix works with actual corrupt data

Query 5: Field name analysis
  - Should show which naming convention is used
  - Important for cleanup migration (age_category vs ageCategory)
*/

-- ====================================================================================
-- Next Steps Based on Results
-- ====================================================================================

/*
IF Query 1 returns 0:
  - Bug may have been fixed by Phase 6A.47 differently
  - Run API tests to confirm
  - May only need monitoring (Phase 5)

IF Query 1 returns > 0:
  - Proceed with Phase 2: Defensive code changes
  - Proceed with Phase 3: Database cleanup migration
  - Use sample records from Query 4 for testing
*/
