# Phase 6A.47 Hotfix Migration - Step-by-Step Instructions

**Date**: 2025-12-27
**Issue**: Migration applied but seed data NOT inserted (0 rows instead of 402)
**Solution**: Create hotfix migration with idempotent seed logic

---

## PREREQUISITE: Run Diagnostic Script First

**BEFORE creating the hotfix**, execute the diagnostic script to confirm the issue:

```bash
# Connect to staging database
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require"

# Execute diagnostic script
\i c:/Work/LankaConnect/scripts/diagnose_seed_failure.sql

# Review Section 10 (Summary Report)
# Expected output: "🔴 CRITICAL: No seed data - Migration Steps 5b-5g failed completely"
```

If the diagnostic confirms 0 rows in `reference_data.reference_values`, proceed with hotfix.

---

## STEP 1: Create Hotfix Migration (Local Development)

### 1.1 Generate Migration File

```bash
cd c:\Work\LankaConnect

# Create hotfix migration
dotnet ef migrations add Phase6A47_1_SeedReferenceData_Hotfix \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext \
  --output-dir Data/Migrations
```

**Expected output**:
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

**Generated files**:
- `src/LankaConnect.Infrastructure/Data/Migrations/20251227XXXXXX_Phase6A47_1_SeedReferenceData_Hotfix.cs`
- `src/LankaConnect.Infrastructure/Data/Migrations/20251227XXXXXX_Phase6A47_1_SeedReferenceData_Hotfix.Designer.cs`
- `src/LankaConnect.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs` (updated)

### 1.2 Verify Migration Files Created

```bash
ls src/LankaConnect.Infrastructure/Data/Migrations/*Phase6A47_1*
```

---

## STEP 2: Edit Migration File with Seed Logic

### 2.1 Open Migration File

Open: `src/LankaConnect.Infrastructure/Data/Migrations/20251227XXXXXX_Phase6A47_1_SeedReferenceData_Hotfix.cs`

### 2.2 Replace Up() Method

The generated `Up()` method will be empty. Replace it with this idempotent seed logic:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // IDEMPOTENCY: Only seed if table is completely empty
    // This prevents duplicate data if migration runs multiple times
    migrationBuilder.Sql(@"
        DO $$
        DECLARE
            row_count INTEGER;
        BEGIN
            -- Check current row count
            SELECT COUNT(*) INTO row_count FROM reference_data.reference_values;

            -- Only seed if table is empty
            IF row_count = 0 THEN
                RAISE NOTICE 'Table is empty (% rows). Starting seed...', row_count;

                -- ========================================
                -- TIER 0: Original 3 Enum Types (22 values)
                -- ========================================

                -- EventCategory (8 values)
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'EventCategory', 'Religious', 0, 'Religious', 'Religious events and ceremonies', 1, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventCategory', 'Cultural', 1, 'Cultural', 'Cultural celebrations and gatherings', 2, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventCategory', 'Community', 2, 'Community', 'Community events and meetups', 3, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventCategory', 'Educational', 3, 'Educational', 'Educational workshops and seminars', 4, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventCategory', 'Social', 4, 'Social', 'Social networking events', 5, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventCategory', 'Business', 5, 'Business', 'Business and professional events', 6, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventCategory', 'Charity', 6, 'Charity', 'Charity and fundraising events', 7, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventCategory', 'Entertainment', 7, 'Entertainment', 'Entertainment and performance events', 8, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW());

                -- EventStatus (8 values)
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'EventStatus', 'Draft', 0, 'Draft', 'Event is in draft status', 1, true, '{""allowsRegistration"": false, ""isFinalState"": false}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventStatus', 'Published', 1, 'Published', 'Event is published and visible', 2, true, '{""allowsRegistration"": true, ""isFinalState"": false}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventStatus', 'Active', 2, 'Active', 'Event is currently active', 3, true, '{""allowsRegistration"": true, ""isFinalState"": false}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventStatus', 'Postponed', 3, 'Postponed', 'Event has been postponed', 4, true, '{""allowsRegistration"": false, ""isFinalState"": false}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventStatus', 'Cancelled', 4, 'Cancelled', 'Event has been cancelled', 5, true, '{""allowsRegistration"": false, ""isFinalState"": true}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventStatus', 'Completed', 5, 'Completed', 'Event has been completed', 6, true, '{""allowsRegistration"": false, ""isFinalState"": true}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventStatus', 'Archived', 6, 'Archived', 'Event has been archived', 7, true, '{""allowsRegistration"": false, ""isFinalState"": true}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventStatus', 'UnderReview', 7, 'Under Review', 'Event is under review', 8, true, '{""allowsRegistration"": false, ""isFinalState"": false}'::jsonb, NOW(), NOW());

                -- UserRole (6 values)
                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    (gen_random_uuid(), 'UserRole', 'GeneralUser', 1, 'General User', 'Basic user with limited permissions', 1, true,
                        '{""canManageUsers"": false, ""canCreateEvents"": false, ""canModerateContent"": false, ""isEventOrganizer"": false, ""isAdmin"": false, ""requiresSubscription"": false, ""canCreateBusinessProfile"": false, ""canCreatePosts"": false, ""monthlySubscriptionPrice"": 0.00}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'UserRole', 'BusinessOwner', 2, 'Business Owner', 'Business owner with business profile access', 2, true,
                        '{""canManageUsers"": false, ""canCreateEvents"": false, ""canModerateContent"": false, ""isEventOrganizer"": false, ""isAdmin"": false, ""requiresSubscription"": true, ""canCreateBusinessProfile"": true, ""canCreatePosts"": false, ""monthlySubscriptionPrice"": 10.00}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'UserRole', 'EventOrganizer', 3, 'Event Organizer', 'Event organizer with event creation access', 3, true,
                        '{""canManageUsers"": false, ""canCreateEvents"": true, ""canModerateContent"": false, ""isEventOrganizer"": true, ""isAdmin"": false, ""requiresSubscription"": true, ""canCreateBusinessProfile"": false, ""canCreatePosts"": true, ""monthlySubscriptionPrice"": 10.00}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'UserRole', 'EventOrganizerAndBusinessOwner', 4, 'Event Organizer + Business Owner', 'Combined role with both event and business access', 4, true,
                        '{""canManageUsers"": false, ""canCreateEvents"": true, ""canModerateContent"": false, ""isEventOrganizer"": false, ""isAdmin"": false, ""requiresSubscription"": true, ""canCreateBusinessProfile"": true, ""canCreatePosts"": true, ""monthlySubscriptionPrice"": 15.00}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'UserRole', 'Admin', 5, 'Administrator', 'Administrator with full system access', 5, true,
                        '{""canManageUsers"": true, ""canCreateEvents"": true, ""canModerateContent"": true, ""isEventOrganizer"": false, ""isAdmin"": true, ""requiresSubscription"": false, ""canCreateBusinessProfile"": true, ""canCreatePosts"": true, ""monthlySubscriptionPrice"": 0.00}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'UserRole', 'AdminManager', 6, 'Admin Manager', 'Admin manager with highest level access', 6, true,
                        '{""canManageUsers"": true, ""canCreateEvents"": true, ""canModerateContent"": true, ""isEventOrganizer"": false, ""isAdmin"": true, ""requiresSubscription"": false, ""canCreateBusinessProfile"": true, ""canCreatePosts"": true, ""monthlySubscriptionPrice"": 0.00}'::jsonb, NOW(), NOW());

                -- ========================================
                -- COPY ALL SQL FROM ORIGINAL MIGRATION
                -- Lines 175-602 (Steps 5b-5g)
                -- ========================================
                -- [PASTE THE REMAINING 380 INSERT STATEMENTS HERE]
                -- See: c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs
                -- Lines 175-398: Email, Payment, Core System, Registration, Event, Cultural enums
                -- Lines 401-437: RegistrationStatus, PaymentStatus, PricingType, SubscriptionStatus
                -- Lines 440-476: BadgePosition, CalendarSystem, FederatedProvider, ProficiencyLevel
                -- Lines 479-520: Business enums
                -- Lines 523-561: Forum + WhatsApp enums
                -- Lines 564-602: Cultural Community, PassPurchaseStatus, CulturalConflictLevel, PoyadayType

                RAISE NOTICE 'Successfully seeded 402 reference values across 41 enum types';
            ELSE
                RAISE NOTICE 'Table already has % rows. Skipping seed to prevent duplicates.', row_count;
            END IF;
        END $$;
    ");
}
```

### 2.3 Add Down() Method

Replace the `Down()` method with delete logic:

```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    // Delete all seeded reference data
    migrationBuilder.Sql(@"
        DELETE FROM reference_data.reference_values
        WHERE enum_type IN (
            'EventCategory', 'EventStatus', 'UserRole',
            'EmailStatus', 'EmailType', 'EmailDeliveryStatus', 'EmailPriority',
            'Currency', 'NotificationType', 'IdentityProvider',
            'SignUpItemCategory', 'SignUpType', 'AgeCategory', 'Gender',
            'EventType', 'SriLankanLanguage', 'CulturalBackground', 'ReligiousContext',
            'GeographicRegion', 'BuddhistFestival', 'HinduFestival',
            'RegistrationStatus', 'PaymentStatus', 'PricingType', 'SubscriptionStatus',
            'BadgePosition', 'CalendarSystem', 'FederatedProvider', 'ProficiencyLevel',
            'BusinessCategory', 'BusinessStatus', 'ReviewStatus', 'ServiceType',
            'ForumCategory', 'TopicStatus', 'WhatsAppMessageStatus', 'WhatsAppMessageType',
            'CulturalCommunity', 'PassPurchaseStatus', 'CulturalConflictLevel', 'PoyadayType'
        );
    ");
}
```

### 2.4 Copy Remaining INSERT Statements

**CRITICAL**: You must copy ALL INSERT statements from lines 175-602 of the original migration file:
- Open: `src/LankaConnect.Infrastructure/Data/Migrations/20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs`
- Copy lines 175-398 (Step 5b - Email, Payment, Core System enums)
- Copy lines 401-437 (Step 5c - Registration/Payment/Pricing/Subscription)
- Copy lines 440-476 (Step 5d - Badge/Calendar/Federated/Proficiency)
- Copy lines 479-520 (Step 5e - Business enums)
- Copy lines 523-561 (Step 5f - Forum + WhatsApp)
- Copy lines 564-602 (Step 5g - Cultural Community + Conflict)

Paste ALL of these INSERT statements after the UserRole INSERT in the hotfix migration.

---

## STEP 3: Test Locally

### 3.1 Build Project

```bash
dotnet build src/LankaConnect.Infrastructure
```

**Expected**: Build succeeded with 0 errors, 0 warnings

### 3.2 Apply Migration to Local Database

```bash
dotnet ef database update \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext
```

**Expected output**:
```
Build started...
Build succeeded.
Applying migration '20251227XXXXXX_Phase6A47_1_SeedReferenceData_Hotfix'.
Done.
```

### 3.3 Verify Seed Data Locally

```bash
# Option 1: Use SQL query
psql -h localhost -U postgres -d LankaConnectDB -c "SELECT COUNT(*) FROM reference_data.reference_values;"

# Option 2: Use dotnet ef
dotnet ef dbcontext script \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext \
  | grep "reference_values"

# Option 3: Run API and test endpoint
dotnet run --project src/LankaConnect.Api
# Navigate to: https://localhost:7001/api/reference-data/email-statuses
# Expected: JSON array with 11 items
```

**Expected counts**:
- Total rows: 402
- Distinct enum_type: 41

### 3.4 Test Idempotency

Run the migration again to ensure it doesn't duplicate data:

```bash
# Run migration again
dotnet ef database update \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext

# Verify count is STILL 402 (not 804)
psql -h localhost -U postgres -d LankaConnectDB -c "SELECT COUNT(*) FROM reference_data.reference_values;"
```

**Expected**: 402 rows (NO duplicates)

---

## STEP 4: Commit and Push

### 4.1 Git Status

```bash
git status
```

**Expected changes**:
```
M  src/LankaConnect.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs
A  src/LankaConnect.Infrastructure/Data/Migrations/20251227XXXXXX_Phase6A47_1_SeedReferenceData_Hotfix.cs
A  src/LankaConnect.Infrastructure/Data/Migrations/20251227XXXXXX_Phase6A47_1_SeedReferenceData_Hotfix.Designer.cs
```

### 4.2 Commit Changes

```bash
git add src/LankaConnect.Infrastructure/Data/Migrations/
git commit -m "hotfix(phase-6a47): Add idempotent seed data migration for reference_values

PROBLEM:
- Migration 20251227034100_Phase6A47 applied but seed data NOT inserted
- Steps 3-5 failed because old tables (event_categories, event_statuses, user_roles) don't exist on fresh databases
- Steps 5b-5g (38 new enum types) succeeded but missing 3 old enum types
- Result: 0 rows instead of 402 rows in reference_values table

ROOT CAUSE:
- Migration relied on data migration from old tables that may not exist
- No idempotency check for fresh database scenario
- Silent failure in PostgreSQL with INSERT...SELECT from non-existent tables

SOLUTION:
- New migration Phase6A47.1 with idempotent seed logic
- Checks if table is empty before seeding (prevents duplicates)
- Includes ALL 41 enum types (3 old + 38 new) = 402 values total
- Uses gen_random_uuid() for IDs
- Wrapped in DO $$ block for safety

TESTING:
- ✅ Local: Applied to fresh database → 402 rows inserted
- ✅ Idempotency: Ran twice → still 402 rows (no duplicates)
- ✅ API: All 41 enum endpoints return correct data
- ✅ Build: 0 errors, 0 warnings

DEPLOYMENT:
- Ready for staging deployment
- Safe to run on existing databases (idempotent)
- Rollback: dotnet ef database update Phase6A47_Refactor_To_Unified_ReferenceValues

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
```

### 4.3 Push to Remote

```bash
git push origin develop
```

---

## STEP 5: Deploy to Staging

### 5.1 Monitor Azure DevOps Pipeline

1. Navigate to: Azure DevOps → Pipelines
2. Watch for build triggered by commit
3. Verify build succeeds
4. Verify deployment to Container App succeeds

### 5.2 Monitor Container App Startup

```bash
# View logs in Azure Portal
# Container Apps → lankaconnect-staging → Logs

# Look for migration log:
# "Applying database migrations..."
# "Applying migration '20251227XXXXXX_Phase6A47_1_SeedReferenceData_Hotfix'."
# "Database migrations applied successfully"
```

### 5.3 Verify Seed Data in Staging

**Option 1: Direct database query**
```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" \
  -c "SELECT COUNT(*) FROM reference_data.reference_values;"
```

**Expected**: 402 rows

**Option 2: API health check**
```bash
curl https://lankaconnect-staging.azurewebsites.net/api/reference-data/email-statuses
```

**Expected**: JSON array with 11 EmailStatus values

### 5.4 Run Full Diagnostic Script

```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" \
  -f c:/Work/LankaConnect/scripts/diagnose_seed_failure.sql
```

**Expected Summary Report**:
```
🟢 SUCCESS: All seed data present
NO ACTION NEEDED: System is healthy
```

---

## STEP 6: Verification Checklist

### Database Verification
- [ ] Migration appears in `__EFMigrationsHistory` table
- [ ] `reference_values` table has exactly 402 rows
- [ ] All 41 distinct `enum_type` values present
- [ ] No duplicate `enum_type + int_value` combinations
- [ ] No duplicate `enum_type + code` combinations
- [ ] No NULL values in required fields (`enum_type`, `code`, `int_value`, `name`)

### API Verification
Test sample endpoints:
- [ ] GET `/api/reference-data/event-categories` → 8 values
- [ ] GET `/api/reference-data/event-statuses` → 8 values
- [ ] GET `/api/reference-data/user-roles` → 6 values
- [ ] GET `/api/reference-data/email-statuses` → 11 values
- [ ] GET `/api/reference-data/currency` → 6 values
- [ ] GET `/api/reference-data/geographic-regions` → 35 values

### Application Health
- [ ] Container App shows "Healthy" status
- [ ] No errors in application logs
- [ ] API responds to /health endpoint
- [ ] Build status: 0 errors, 0 warnings

---

## STEP 7: Update Documentation

### 7.1 Update Progress Tracker

Add to `docs/PROGRESS_TRACKER.md`:
```markdown
### 2025-12-27: Phase 6A.47 Hotfix - Seed Data Fix
- **ISSUE**: Original migration applied but seed data NOT inserted (0/402 rows)
- **ROOT CAUSE**: Migration relied on old tables that don't exist on fresh databases
- **FIX**: Created Phase6A47.1 hotfix migration with idempotent seed logic
- **RESULT**: All 402 reference values now seeded correctly across 41 enum types
- **COMMIT**: [commit-hash]
- **STATUS**: ✅ Complete
```

### 7.2 Update Streamlined Action Plan

Update `docs/STREAMLINED_ACTION_PLAN.md`:
```markdown
## Phase 6A.47: Refactor to Unified Reference Data Architecture
- **STATUS**: ✅ Complete (with hotfix)
- **DELIVERABLES**:
  - ✅ Unified `reference_values` table
  - ✅ 41 enum types seeded (402 total values)
  - ✅ Hotfix migration for fresh database scenario
- **FILES**:
  - Migration: `20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs`
  - Hotfix: `20251227XXXXXX_Phase6A47_1_SeedReferenceData_Hotfix.cs`
  - Analysis: `docs/PHASE_6A47_SEED_DATA_FAILURE_ANALYSIS.md`
```

### 7.3 Update Master Index

Add to `docs/PHASE_6A_MASTER_INDEX.md`:
```markdown
### Phase 6A.47.1: Hotfix - Seed Data Migration
- **Created**: 2025-12-27
- **Status**: Complete
- **Description**: Hotfix for seed data failure in Phase 6A.47 original migration
- **Deliverables**: Idempotent migration seeding all 402 reference values
```

---

## ROLLBACK PROCEDURE (If Hotfix Fails)

### Emergency Rollback Steps

**Step 1**: Revert to previous migration
```bash
dotnet ef database update 20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext
```

**Step 2**: Delete hotfix migration files
```bash
rm src/LankaConnect.Infrastructure/Data/Migrations/*Phase6A47_1_SeedReferenceData_Hotfix*
```

**Step 3**: Restore model snapshot from git
```bash
git checkout HEAD -- src/LankaConnect.Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs
```

**Step 4**: Manual SQL seed as fallback
```bash
# Execute seed SQL directly (see APPENDIX A in analysis document)
psql "host=lankaconnect-staging-db.postgres.database.azure.com..." \
  -f scripts/seed_reference_data_hotfix.sql
```

---

## CONTACT & SUPPORT

**Document Owner**: System Architect
**Created**: 2025-12-27
**Incident**: Phase 6A.47 Seed Data Failure (P1 - Critical)

**Reference Documents**:
- Analysis: `docs/PHASE_6A47_SEED_DATA_FAILURE_ANALYSIS.md`
- Diagnostic Script: `scripts/diagnose_seed_failure.sql`
- Original Migration: `src/LankaConnect.Infrastructure/Data/Migrations/20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs`

---

**END OF INSTRUCTIONS**
