# Phase 6A.47 Seed Data Failure - Quick Fix Commands

**CRITICAL ISSUE**: Migration applied but 0 rows seeded (expected 402 rows)

---

## 1️⃣ DIAGNOSE (1 minute)

```bash
# Connect to staging database and run diagnostic
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f c:/Work/LankaConnect/scripts/diagnose_seed_failure.sql
```

**Expected Output**: Section 10 shows "🔴 CRITICAL: No seed data"

---

## 2️⃣ CREATE HOTFIX MIGRATION (5 minutes)

```bash
cd c:\Work\LankaConnect

# Generate migration
dotnet ef migrations add Phase6A47_1_SeedReferenceData_Hotfix \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext

# IMPORTANT: Edit the generated file and add seed SQL
# See: scripts/HOTFIX_MIGRATION_INSTRUCTIONS.md (Step 2)
```

**Files to edit**:
- `src/LankaConnect.Infrastructure/Data/Migrations/[timestamp]_Phase6A47_1_SeedReferenceData_Hotfix.cs`

---

## 3️⃣ TEST LOCALLY (3 minutes)

```bash
# Apply migration
dotnet ef database update \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext

# Verify count (should be 402)
# Use SQL client or run:
dotnet run --project src/LankaConnect.Api
# Then test: GET /api/reference-data/email-statuses
```

---

## 4️⃣ DEPLOY (10 minutes)

```bash
# Commit
git add .
git commit -m "hotfix(phase-6a47): Add idempotent seed data migration for reference_values"
git push origin develop

# Monitor Azure DevOps pipeline
# Wait for Container App deployment
# Check logs for: "Applying migration '..._Phase6A47_1_SeedReferenceData_Hotfix'."
```

---

## 5️⃣ VERIFY (2 minutes)

```bash
# Check staging database
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -c "SELECT COUNT(*) FROM reference_data.reference_values;"

# Expected: 402

# Test API
curl https://lankaconnect-staging.azurewebsites.net/api/reference-data/email-statuses

# Expected: Array with 11 items
```

---

## ⚠️ ALTERNATIVE: MANUAL SQL SEED (If hotfix migration not feasible)

**Use this if you need IMMEDIATE fix without waiting for deployment**

### Step 1: Create seed SQL file

Create file: `c:/Work/LankaConnect/scripts/manual_seed.sql`

Copy this content:
```sql
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM reference_data.reference_values LIMIT 1) THEN
        -- EventCategory (8 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EventCategory', 'Religious', 0, 'Religious', 'Religious events', 1, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Cultural', 1, 'Cultural', 'Cultural events', 2, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Community', 2, 'Community', 'Community events', 3, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Educational', 3, 'Educational', 'Educational events', 4, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Social', 4, 'Social', 'Social events', 5, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Business', 5, 'Business', 'Business events', 6, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Charity', 6, 'Charity', 'Charity events', 7, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Entertainment', 7, 'Entertainment', 'Entertainment events', 8, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW());

        -- EventStatus (8 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'EventStatus', 'Draft', 0, 'Draft', 'Draft status', 1, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Published', 1, 'Published', 'Published status', 2, true, '{"allowsRegistration": true, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Active', 2, 'Active', 'Active status', 3, true, '{"allowsRegistration": true, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Postponed', 3, 'Postponed', 'Postponed status', 4, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Cancelled', 4, 'Cancelled', 'Cancelled status', 5, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Completed', 5, 'Completed', 'Completed status', 6, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Archived', 6, 'Archived', 'Archived status', 7, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'UnderReview', 7, 'Under Review', 'Under review status', 8, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW());

        -- UserRole (6 values)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'UserRole', 'GeneralUser', 1, 'General User', 'General user', 1, true,
                '{"canManageUsers": false, "canCreateEvents": false, "canModerateContent": false, "requiresSubscription": false, "canCreateBusinessProfile": false, "canCreatePosts": false, "monthlySubscriptionPrice": 0.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'BusinessOwner', 2, 'Business Owner', 'Business owner', 2, true,
                '{"canManageUsers": false, "canCreateEvents": false, "canModerateContent": false, "requiresSubscription": true, "canCreateBusinessProfile": true, "canCreatePosts": false, "monthlySubscriptionPrice": 10.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'EventOrganizer', 3, 'Event Organizer', 'Event organizer', 3, true,
                '{"canManageUsers": false, "canCreateEvents": true, "canModerateContent": false, "requiresSubscription": true, "canCreateBusinessProfile": false, "canCreatePosts": true, "monthlySubscriptionPrice": 10.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'EventOrganizerAndBusinessOwner', 4, 'Event Organizer + Business Owner', 'Combined role', 4, true,
                '{"canManageUsers": false, "canCreateEvents": true, "canModerateContent": false, "requiresSubscription": true, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 15.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'Admin', 5, 'Administrator', 'Administrator', 5, true,
                '{"canManageUsers": true, "canCreateEvents": true, "canModerateContent": true, "requiresSubscription": false, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 0.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'AdminManager', 6, 'Admin Manager', 'Admin manager', 6, true,
                '{"canManageUsers": true, "canCreateEvents": true, "canModerateContent": true, "requiresSubscription": false, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 0.00}'::jsonb, NOW(), NOW());

        -- COPY ALL REMAINING INSERT STATEMENTS FROM:
        -- src/LankaConnect.Infrastructure/Data/Migrations/20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs
        -- Lines 175-602 (Steps 5b-5g)
        -- This includes 38 more enum types with ~380 values

        RAISE NOTICE 'Seeded 402 reference values';
    END IF;
END $$;
```

### Step 2: Execute against staging

```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f c:/Work/LankaConnect/scripts/manual_seed.sql
```

### Step 3: Verify

```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -c "SELECT COUNT(*) FROM reference_data.reference_values;"
```

---

## 🔄 ROLLBACK (If something goes wrong)

```bash
# Revert migration
dotnet ef database update 20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext

# Delete hotfix migration files
rm src/LankaConnect.Infrastructure/Data/Migrations/*Phase6A47_1*

# Restore model snapshot
git checkout HEAD -- src/LankaConnect.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs
```

---

## 📋 VERIFICATION CHECKLIST

After fix is applied:

- [ ] Database has 402 rows: `SELECT COUNT(*) FROM reference_data.reference_values;`
- [ ] Database has 41 enum types: `SELECT COUNT(DISTINCT enum_type) FROM reference_data.reference_values;`
- [ ] No duplicates: `SELECT enum_type, code, COUNT(*) FROM reference_data.reference_values GROUP BY enum_type, code HAVING COUNT(*) > 1;` (should return 0 rows)
- [ ] API returns data: `GET /api/reference-data/email-statuses` (should return 11 items)
- [ ] No errors in logs
- [ ] Build status: 0 errors

---

## 📚 REFERENCE DOCUMENTS

- Full Analysis: `docs/PHASE_6A47_SEED_DATA_FAILURE_ANALYSIS.md`
- Detailed Instructions: `scripts/HOTFIX_MIGRATION_INSTRUCTIONS.md`
- Diagnostic Script: `scripts/diagnose_seed_failure.sql`
- Original Migration: `src/LankaConnect.Infrastructure/Data/Migrations/20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs`

---

**Total Time to Fix**: ~20 minutes (Option 1: Hotfix Migration) OR ~5 minutes (Option 2: Manual SQL)

**Recommended**: Use hotfix migration for long-term solution. Use manual SQL only if you need immediate fix.
