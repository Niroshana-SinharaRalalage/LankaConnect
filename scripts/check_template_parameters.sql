-- Phase 6A.80 Part 3: Template Parameter Investigation Script
-- Purpose: Identify parameter mismatches between handlers and email templates
-- Author: Phase 6A.80 Part 3 RCA
-- Date: 2026-01-24

-- =====================================================
-- PART 1: Check template-new-event-publication template content
-- =====================================================

SELECT
    name,
    subject_template,
    -- Extract organizer-related parameters from HTML
    CASE
        WHEN html_template LIKE '%{{OrganizerContactName}}%' THEN 'Uses {{OrganizerContactName}}'
        WHEN html_template LIKE '%{{OrganizerName}}%' THEN 'Uses {{OrganizerName}}'
        ELSE 'No organizer name parameter'
    END as organizer_name_param,
    CASE
        WHEN html_template LIKE '%{{HasOrganizerContact}}%' THEN 'Uses {{HasOrganizerContact}}'
        WHEN html_template LIKE '%{{OrganizerEmail}}%' THEN 'Uses {{OrganizerEmail}}'
        ELSE 'No organizer contact parameter'
    END as organizer_contact_param,
    -- Extract all Handlebars parameters
    (
        SELECT string_agg(DISTINCT matches[1], ', ')
        FROM regexp_matches(html_template, '{{([A-Za-z]+)}}', 'g') AS matches
    ) as all_html_parameters,
    (
        SELECT string_agg(DISTINCT matches[1], ', ')
        FROM regexp_matches(text_template, '{{([A-Za-z]+)}}', 'g') AS matches
    ) as all_text_parameters
FROM communications.email_templates
WHERE name IN ('template-new-event-publication', 'template-event-details-publication');

-- =====================================================
-- PART 2: Get full HTML template for manual inspection
-- =====================================================

SELECT
    name,
    html_template
FROM communications.email_templates
WHERE name = 'template-new-event-publication';

-- =====================================================
-- PART 3: Get full text template for manual inspection
-- =====================================================

SELECT
    name,
    text_template
FROM communications.email_templates
WHERE name = 'template-new-event-publication';

-- =====================================================
-- PART 4: Check template-event-details-publication
-- =====================================================

SELECT
    name,
    subject_template,
    html_template,
    text_template
FROM communications.email_templates
WHERE name = 'template-event-details-publication';

-- =====================================================
-- PART 5: Find recent emails sent with these templates
-- =====================================================

SELECT
    em."Id" as email_id,
    em.to_emails,
    em.template_name,
    em.status,
    em."CreatedAt" as queued_at,
    em.sent_at,
    -- Check for literal Handlebars in rendered content
    CASE
        WHEN em.html_body LIKE '%{{OrganizerContactName}}%' THEN 'FAIL - Has {{OrganizerContactName}}'
        WHEN em.html_body LIKE '%{{HasOrganizerContact}}%' THEN 'FAIL - Has {{HasOrganizerContact}}'
        WHEN em.html_body LIKE '%{{OrganizerName}}%' THEN 'FAIL - Has {{OrganizerName}}'
        ELSE 'OK - No literal Handlebars'
    END as organizer_rendering_check,
    -- Extract template_data to see what parameters were sent
    (em.template_data::json->>'HasOrganizerContact')::text as param_has_organizer,
    (em.template_data::json->>'OrganizerName')::text as param_organizer_name,
    (em.template_data::json->>'OrganizerContactName')::text as param_organizer_contact_name,
    (em.template_data::json->>'EventTitle')::text as param_event_title
FROM communications.email_messages em
WHERE em.template_name IN ('template-new-event-publication', 'template-event-details-publication')
    AND em."CreatedAt" >= NOW() - INTERVAL '7 days'
ORDER BY em."CreatedAt" DESC
LIMIT 10;

-- =====================================================
-- EXPECTED RESULTS
-- =====================================================

-- Part 1: Should show which parameter names templates use (OrganizerName vs OrganizerContactName)
-- Part 2-4: Full template content for manual inspection
-- Part 5: Shows parameter mismatch - if template uses {{OrganizerContactName}} but handler sends OrganizerName, they won't match

-- =====================================================
-- TROUBLESHOOTING
-- =====================================================

-- If PART 5 shows literal Handlebars in emails:
-- 1. Check PART 1 results to see which parameter names the template expects
-- 2. Compare against handler code (EventNotificationEmailJob.cs line 338, EventPublishedEventHandler.cs line 96-110)
-- 3. If mismatch found, update handler code to match template expectations
-- 4. Run migration to update template content if template is wrong
