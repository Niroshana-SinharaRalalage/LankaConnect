# Manual Database Fix Instructions - Phase 6A.41

## Connection Details
- **Host**: lankaconnect-db-staging.postgres.database.azure.com
- **Database**: LankaConnectDB
- **Username**: adminuser
- **Password**: 1qaz!QAZ

## Step 1: Verify Current State

Run this query to check if the template has NULL subject:

```sql
SELECT name, subject_template, is_active, created_at
FROM communications.email_templates
WHERE name = 'event-published';
```

**Expected Result**: `subject_template` column should show NULL or empty string

## Step 2: Apply Fix

Execute the complete fix script located at:
`c:\Work\LankaConnect\scripts\FixEventPublishedTemplateSubject_Phase6A41.sql`

Or run this simplified version:

```sql
-- Verify template exists
SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM communications.email_templates WHERE name = 'event-published')
        THEN 'Template exists - proceeding with update'
        ELSE 'ERROR: Template not found'
    END as verification;

-- Update the subject
UPDATE communications.email_templates
SET
    subject_template = 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}',
    updated_at = NOW()
WHERE name = 'event-published'
  AND (subject_template IS NULL OR subject_template = '');

-- Verify update
SELECT
    name,
    subject_template,
    CASE
        WHEN subject_template = 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}'
        THEN 'SUCCESS: Subject updated correctly'
        ELSE 'ERROR: Subject not updated'
    END as status,
    is_active,
    updated_at
FROM communications.email_templates
WHERE name = 'event-published';
```

## Step 3: Confirm Success

The final SELECT should show:
- `name`: event-published
- `subject_template`: New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}
- `status`: SUCCESS: Subject updated correctly
- `is_active`: true

## Step 4: After Database Fix - Notify Claude

Once you've run the SQL and confirmed success, tell Claude to proceed with API testing.
