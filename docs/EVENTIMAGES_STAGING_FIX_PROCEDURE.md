# EventImages Staging Fix Procedure

## Overview
This document provides step-by-step instructions to fix the missing `EventImages` table in staging database.

**Problem**: `PostgresException: 42P01: relation "EventImages" does not exist`

**Root Cause**: Migration `20251103040053_AddEventImages` exists in old `Migrations/` folder but staging deployment only packaged files from `Data/Migrations/` folder.

**Solution**: Move migration to correct folder and apply to staging database.

---

## Quick Summary

| Step | Action | Duration | Risk |
|------|--------|----------|------|
| 1 | Pre-flight checks | 5 min | None |
| 2 | Move migration file to Data/Migrations/ | 2 min | Low |
| 3 | Verify migration compiles | 3 min | None |
| 4 | Generate SQL script | 5 min | None |
| 5 | Create database backup | 10 min | None |
| 6 | Apply migration to staging | 5 min | **Medium** |
| 7 | Verify table creation | 5 min | None |
| 8 | Test image upload | 5 min | Low |
| 9 | Deploy updated code | 15 min | Low |
| **Total** | | **55 min** | |

---

## Part 1: Pre-Flight Checks (5 minutes)

### 1.1 Verify Local Environment

```bash
# Navigate to project root
cd c:\Work\LankaConnect

# Check git status
git status

# Ensure on develop branch
git checkout develop
git pull origin develop

# Verify migration exists in OLD location
ls "src\LankaConnect.Infrastructure\Migrations\20251103040053_AddEventImages.cs"
# Expected: File exists

# Verify migration NOT in NEW location
ls "src\LankaConnect.Infrastructure\Data\Migrations\20251103040053_AddEventImages.cs" 2>$null
# Expected: File not found (PowerShell) or error (bash)
```

### 1.2 Verify Staging Database State

```bash
# Set staging connection string (replace with actual values)
$env:PGPASSWORD = "your-staging-password"
$STAGING_DB_HOST = "your-staging-server.postgres.database.azure.com"
$STAGING_DB_NAME = "lankaconnect"
$STAGING_DB_USER = "your-admin-user"

# Check if EventImages table exists
psql -h $STAGING_DB_HOST -U $STAGING_DB_USER -d $STAGING_DB_NAME -c "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'events' AND table_name = 'EventImages') AS table_exists;"
# Expected: table_exists = f (false)

# Check if migration is in history
psql -h $STAGING_DB_HOST -U $STAGING_DB_USER -d $STAGING_DB_NAME -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20251103040053_AddEventImages';"
# Expected: count = 0

# Check last applied migration
psql -h $STAGING_DB_HOST -U $STAGING_DB_USER -d $STAGING_DB_NAME -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"
# Note the latest migration ID
```

**Checkpoint**: If table exists or migration is in history, STOP and investigate. The issue may be different than expected.

---

## Part 2: Move Migration File (2 minutes)

### 2.1 Copy Migration Files

```bash
# Copy main migration file
cp "src\LankaConnect.Infrastructure\Migrations\20251103040053_AddEventImages.cs" `
   "src\LankaConnect.Infrastructure\Data\Migrations\20251103040053_AddEventImages.cs"

# Copy designer file
cp "src\LankaConnect.Infrastructure\Migrations\20251103040053_AddEventImages.Designer.cs" `
   "src\LankaConnect.Infrastructure\Data\Migrations\20251103040053_AddEventImages.Designer.cs"

# Verify copy succeeded
ls "src\LankaConnect.Infrastructure\Data\Migrations\20251103040053_AddEventImages*"
# Expected: 2 files (.cs and .Designer.cs)
```

### 2.2 Verify File Contents

```bash
# Check namespace is correct (should be LankaConnect.Infrastructure.Data.Migrations)
Get-Content "src\LankaConnect.Infrastructure\Data\Migrations\20251103040053_AddEventImages.cs" -Head 10 | Select-String "namespace"
# Expected: namespace LankaConnect.Infrastructure.Data.Migrations
```

---

## Part 3: Build Verification (3 minutes)

### 3.1 Compile Infrastructure Project

```bash
# Clean and rebuild
dotnet clean src/LankaConnect.Infrastructure
dotnet build src/LankaConnect.Infrastructure

# Check for errors
echo $LASTEXITCODE
# Expected: 0 (success)
```

### 3.2 List Migrations

```bash
# List all migrations detected by EF Core
dotnet ef migrations list `
    --project src/LankaConnect.Infrastructure `
    --startup-project src/LankaConnect.API

# Verify AddEventImages appears in the list
# Expected: Should see 20251103040053_AddEventImages
```

**Checkpoint**: If build fails or migration not listed, STOP and fix errors before proceeding.

---

## Part 4: Generate SQL Script (5 minutes)

### 4.1 Create Migration SQL

```bash
# Generate SQL for ONLY the AddEventImages migration
dotnet ef migrations script `
    20251102144315_AddEventCategoryAndTicketPrice `
    20251103040053_AddEventImages `
    --project src/LankaConnect.Infrastructure `
    --startup-project src/LankaConnect.API `
    --output apply_eventimages.sql `
    --idempotent

# Review generated SQL
Get-Content apply_eventimages.sql
```

### 4.2 Expected SQL Content

The generated SQL should contain:

```sql
-- Create EventImages table
CREATE TABLE events."EventImages" (
    "Id" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "ImageUrl" character varying(500) NOT NULL,
    "BlobName" character varying(255) NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "UploadedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_EventImages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventImages_events_EventId" FOREIGN KEY ("EventId")
        REFERENCES events.events ("Id") ON DELETE CASCADE
);

-- Create indexes
CREATE INDEX "IX_EventImages_EventId"
    ON events."EventImages" ("EventId");

CREATE UNIQUE INDEX "IX_EventImages_EventId_DisplayOrder"
    ON events."EventImages" ("EventId", "DisplayOrder");

-- Insert migration record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251103040053_AddEventImages', '8.0.x');
```

**Checkpoint**: If SQL looks incorrect or contains unexpected statements, STOP and review.

---

## Part 5: Database Backup (10 minutes)

### 5.1 Create Azure PostgreSQL Backup

**Option A: Azure Portal**
1. Navigate to Azure PostgreSQL Flexible Server
2. Go to "Backup and restore"
3. Click "Backup now"
4. Name: `pre-eventimages-migration-2025-12-03`
5. Wait for backup to complete (5-10 minutes)

**Option B: Azure CLI**
```bash
# Create backup
az postgres flexible-server backup create `
    --resource-group your-resource-group `
    --server-name your-staging-server `
    --backup-name pre-eventimages-migration-2025-12-03

# Verify backup exists
az postgres flexible-server backup list `
    --resource-group your-resource-group `
    --server-name your-staging-server `
    --query "[?name=='pre-eventimages-migration-2025-12-03']"
```

**Option C: pg_dump (If no Azure access)**
```bash
# Dump staging database
pg_dump -h $STAGING_DB_HOST `
        -U $STAGING_DB_USER `
        -d $STAGING_DB_NAME `
        -F c `
        -f "staging_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').dump"

# Verify dump file
ls *.dump -l
# Should see file with size > 0
```

**Checkpoint**: DO NOT PROCEED without confirmed backup.

---

## Part 6: Apply Migration to Staging (5 minutes)

### 6.1 Review SQL One More Time

```bash
# Open SQL file in editor
code apply_eventimages.sql

# Verify:
# - Creates EventImages table in events schema
# - Creates FK to events.events
# - Creates indexes
# - Inserts migration history record
# - No DROP or DELETE statements
```

### 6.2 Execute SQL on Staging

**Option A: psql Command Line**
```bash
# Apply migration
psql -h $STAGING_DB_HOST `
     -U $STAGING_DB_USER `
     -d $STAGING_DB_NAME `
     -f apply_eventimages.sql

# Check for errors in output
# Expected: CREATE TABLE, CREATE INDEX, INSERT (no errors)
```

**Option B: Azure Data Studio / pgAdmin**
1. Connect to staging database
2. Open new query window
3. Paste contents of `apply_eventimages.sql`
4. Execute (F5)
5. Verify "Commands completed successfully"

**Option C: EF Core CLI (If connection string available)**
```bash
# Set connection string
$env:ConnectionStrings__DefaultConnection = "Host=$STAGING_DB_HOST;Database=$STAGING_DB_NAME;Username=$STAGING_DB_USER;Password=***"

# Apply migration
dotnet ef database update 20251103040053_AddEventImages `
    --project src/LankaConnect.Infrastructure `
    --startup-project src/LankaConnect.API
```

---

## Part 7: Verification (5 minutes)

### 7.1 Verify Table Creation

```sql
-- Check table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
) AS table_exists;
-- Expected: true

-- Check table schema
SELECT column_name, data_type, character_maximum_length, is_nullable
FROM information_schema.columns
WHERE table_schema = 'events' AND table_name = 'EventImages'
ORDER BY ordinal_position;
-- Expected:
-- Id              | uuid          | NULL | NO
-- EventId         | uuid          | NULL | NO
-- ImageUrl        | varchar       | 500  | NO
-- BlobName        | varchar       | 255  | NO
-- DisplayOrder    | integer       | NULL | NO
-- UploadedAt      | timestamptz   | NULL | NO
```

### 7.2 Verify Indexes

```sql
-- Check indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'events' AND tablename = 'EventImages';
-- Expected: 3 indexes (PK + 2 created indexes)
```

### 7.3 Verify Foreign Key

```sql
-- Check FK constraint
SELECT
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'events'
  AND tc.table_name = 'EventImages';
-- Expected: FK_EventImages_events_EventId pointing to events.events(Id)
```

### 7.4 Verify Migration History

```sql
-- Check migration recorded
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';
-- Expected: 1 row with migration ID and EF Core version
```

**Checkpoint**: All verifications must pass before proceeding to testing.

---

## Part 8: Test Image Upload (5 minutes)

### 8.1 Prepare Test

**Prerequisites**:
- Valid event ID from staging database
- Valid JWT token for authenticated user
- Test image file (JPG/PNG, < 5MB)

```bash
# Get a test event ID
psql -h $STAGING_DB_HOST -U $STAGING_DB_USER -d $STAGING_DB_NAME -c "SELECT \"Id\", title FROM events.events ORDER BY \"CreatedAt\" DESC LIMIT 5;"
# Copy one event ID

# Set variables
$EVENT_ID = "paste-event-id-here"
$STAGING_API_URL = "https://your-staging-app.azurewebsites.net"
$JWT_TOKEN = "paste-token-here"
```

### 8.2 Execute Upload Test

**Option A: curl**
```bash
curl -X POST "$STAGING_API_URL/api/events/$EVENT_ID/images" `
     -H "Authorization: Bearer $JWT_TOKEN" `
     -H "Content-Type: multipart/form-data" `
     -F "file=@test_image.jpg" `
     -F "displayOrder=1" `
     -v

# Expected response:
# HTTP 200 OK
# {
#   "id": "guid",
#   "imageUrl": "https://...",
#   "displayOrder": 1
# }
```

**Option B: Postman**
1. Create new POST request
2. URL: `{{staging}}/api/events/{{eventId}}/images`
3. Headers: `Authorization: Bearer {{token}}`
4. Body: form-data
   - file: (select image file)
   - displayOrder: 1
5. Send
6. Verify 200 OK response

### 8.3 Verify Database Record

```sql
-- Check image record created
SELECT "Id", "EventId", "ImageUrl", "BlobName", "DisplayOrder", "UploadedAt"
FROM events."EventImages"
ORDER BY "UploadedAt" DESC
LIMIT 5;
-- Expected: New row with test image data
```

**Checkpoint**: If upload fails with 500 error, check application logs for details.

---

## Part 9: Deploy Updated Code (15 minutes)

### 9.1 Commit Changes

```bash
# Stage migration files
git add "src/LankaConnect.Infrastructure/Data/Migrations/20251103040053_AddEventImages.cs"
git add "src/LankaConnect.Infrastructure/Data/Migrations/20251103040053_AddEventImages.Designer.cs"

# Commit with descriptive message
git commit -m "fix: Move AddEventImages migration to Data/Migrations folder

- Fixes EventImages table missing in staging database
- Root cause: Migration was in old Migrations/ folder not deployed to staging
- Solution: Moved to Data/Migrations/ where all new migrations are created
- Applied manually to staging database via SQL script
- See docs/architecture/ADR-008-EventImages-Missing-Table-Root-Cause.md

Resolves: EventImages table 500 errors in staging"

# Push to develop
git push origin develop
```

### 9.2 Deploy to Staging

**Option A: Azure DevOps Pipeline**
```bash
# Trigger pipeline manually
az pipelines run --name "LankaConnect-Staging-Deploy" --branch develop
```

**Option B: GitHub Actions**
```bash
# Push triggers workflow
# OR manually trigger via Actions tab
```

**Option C: Azure App Service (Direct Deploy)**
```bash
# Build and publish
dotnet publish src/LankaConnect.API -c Release -o ./publish

# Deploy to Azure (using Azure CLI)
az webapp deployment source config-zip `
    --resource-group your-rg `
    --name your-staging-app `
    --src ./publish.zip
```

### 9.3 Verify Deployment

```bash
# Check app service logs
az webapp log tail --resource-group your-rg --name your-staging-app

# Verify app started successfully
curl "$STAGING_API_URL/health" -v
# Expected: HTTP 200 OK
```

---

## Part 10: Post-Deployment Validation (5 minutes)

### 10.1 Smoke Tests

```bash
# Test 1: Health check
curl "$STAGING_API_URL/health"
# Expected: {"status": "Healthy"}

# Test 2: Events list
curl "$STAGING_API_URL/api/events" -H "Authorization: Bearer $JWT_TOKEN"
# Expected: HTTP 200 with events array

# Test 3: Image upload (repeat from Part 8)
curl -X POST "$STAGING_API_URL/api/events/$EVENT_ID/images" `
     -H "Authorization: Bearer $JWT_TOKEN" `
     -F "file=@test_image_2.jpg" `
     -F "displayOrder=2"
# Expected: HTTP 200 OK
```

### 10.2 Monitor Application Insights

```bash
# Check for any new errors in last 10 minutes
az monitor app-insights query `
    --app your-app-insights `
    --analytics-query "exceptions | where timestamp > ago(10m) | order by timestamp desc"
# Expected: No results (or unrelated errors only)
```

---

## Part 11: Cleanup (Optional, Do Later)

### 11.1 Remove Old Migration Files

**WARNING**: Only do this AFTER all environments are updated and verified working.

```bash
# Create feature branch
git checkout -b cleanup/consolidate-migrations

# Remove old files
Remove-Item "src\LankaConnect.Infrastructure\Migrations\20251103040053_AddEventImages.cs"
Remove-Item "src\LankaConnect.Infrastructure\Migrations\20251103040053_AddEventImages.Designer.cs"

# Commit
git add -A
git commit -m "chore: Remove duplicate AddEventImages migration from old Migrations folder

All migrations now consolidated in Data/Migrations/"

# Create PR for review
git push origin cleanup/consolidate-migrations
```

### 11.2 Move ALL Remaining Migrations (Future Work)

See `docs/architecture/ADR-008-EventImages-Missing-Table-Root-Cause.md` for full migration consolidation plan.

---

## Rollback Procedure (If Issues Occur)

### If Migration Application Fails

**Step 1**: Stop any active migrations
```sql
-- Check for locks
SELECT * FROM pg_stat_activity
WHERE datname = 'lankaconnect' AND state = 'active';

-- Terminate if needed (replace PID)
SELECT pg_terminate_backend(PID);
```

**Step 2**: Restore from backup
```bash
# Using Azure CLI
az postgres flexible-server restore `
    --resource-group your-rg `
    --name your-staging-server `
    --source-server your-staging-server `
    --restore-point-in-time "2025-12-03T12:00:00Z" `
    --target-server your-staging-server-restored

# OR using pg_restore (if using pg_dump backup)
pg_restore -h $STAGING_DB_HOST -U $STAGING_DB_USER -d $STAGING_DB_NAME -c staging_backup_YYYYMMDD_HHMMSS.dump
```

**Step 3**: Remove failed migration from history (if it was inserted)
```sql
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';
```

### If Application Breaks After Deployment

**Step 1**: Rollback deployment
```bash
# Redeploy previous working version
git checkout develop~1  # Previous commit
dotnet publish src/LankaConnect.API -c Release -o ./publish_rollback
# Deploy ./publish_rollback
```

**Step 2**: Investigate logs
```bash
# Check application logs
az webapp log tail --resource-group your-rg --name your-staging-app

# Check Application Insights
az monitor app-insights query --app your-app-insights --analytics-query "exceptions | order by timestamp desc | take 10"
```

---

## Success Criteria

✅ Migration applied successfully (no SQL errors)
✅ EventImages table exists in staging database
✅ Table has correct schema (6 columns, 3 indexes, 1 FK)
✅ Migration recorded in __EFMigrationsHistory
✅ Image upload endpoint returns 200 OK
✅ Image data saved to database correctly
✅ Application logs show no migration-related errors
✅ Code changes committed and pushed to develop
✅ Staging deployment completed successfully

---

## Contacts

**If issues occur during this procedure, contact**:
- Database Admin: [contact info]
- DevOps Lead: [contact info]
- On-call Engineer: [contact info]

**Escalation**: If rollback is required, immediately notify team and document incident.

---

## Appendix A: Full SQL Script Template

```sql
-- ======================================
-- AddEventImages Migration
-- Generated: 2025-12-03
-- Target: Staging Database
-- ======================================

BEGIN;

-- Check prerequisites
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT FROM information_schema.tables
        WHERE table_schema = 'events' AND table_name = 'events'
    ) THEN
        RAISE EXCEPTION 'events.events table does not exist. Cannot create EventImages.';
    END IF;
END $$;

-- Create EventImages table
CREATE TABLE IF NOT EXISTS events."EventImages" (
    "Id" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "ImageUrl" character varying(500) NOT NULL,
    "BlobName" character varying(255) NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "UploadedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_EventImages" PRIMARY KEY ("Id")
);

-- Create foreign key
ALTER TABLE events."EventImages"
    ADD CONSTRAINT "FK_EventImages_events_EventId"
    FOREIGN KEY ("EventId")
    REFERENCES events.events ("Id")
    ON DELETE CASCADE;

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_EventImages_EventId"
    ON events."EventImages" ("EventId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_EventImages_EventId_DisplayOrder"
    ON events."EventImages" ("EventId", "DisplayOrder");

-- Insert migration history record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251103040053_AddEventImages', '8.0.10')
ON CONFLICT DO NOTHING;

COMMIT;

-- Verify
SELECT
    (SELECT COUNT(*) FROM events."EventImages") AS "EventImages_Count",
    (SELECT COUNT(*) FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251103040053_AddEventImages') AS "Migration_Recorded",
    (SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'events' AND table_name = 'EventImages')) AS "Table_Exists";
```

---

## Appendix B: Verification Queries

```sql
-- Full verification suite
DO $$
DECLARE
    table_exists boolean;
    migration_recorded boolean;
    fk_exists boolean;
    index_count integer;
BEGIN
    -- Check table
    SELECT EXISTS (
        SELECT FROM information_schema.tables
        WHERE table_schema = 'events' AND table_name = 'EventImages'
    ) INTO table_exists;

    -- Check migration history
    SELECT EXISTS (
        SELECT FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20251103040053_AddEventImages'
    ) INTO migration_recorded;

    -- Check FK
    SELECT EXISTS (
        SELECT FROM information_schema.table_constraints
        WHERE constraint_name = 'FK_EventImages_events_EventId'
          AND table_schema = 'events'
          AND table_name = 'EventImages'
    ) INTO fk_exists;

    -- Check indexes
    SELECT COUNT(*) INTO index_count
    FROM pg_indexes
    WHERE schemaname = 'events' AND tablename = 'EventImages';

    -- Report
    RAISE NOTICE 'Table exists: %', table_exists;
    RAISE NOTICE 'Migration recorded: %', migration_recorded;
    RAISE NOTICE 'FK exists: %', fk_exists;
    RAISE NOTICE 'Index count: % (expected: 3)', index_count;

    -- Assert
    IF NOT table_exists THEN
        RAISE EXCEPTION 'EventImages table does not exist';
    END IF;

    IF NOT migration_recorded THEN
        RAISE EXCEPTION 'Migration not recorded in history';
    END IF;

    IF NOT fk_exists THEN
        RAISE EXCEPTION 'Foreign key constraint missing';
    END IF;

    IF index_count != 3 THEN
        RAISE EXCEPTION 'Expected 3 indexes, found %', index_count;
    END IF;

    RAISE NOTICE 'All checks passed!';
END $$;
```
