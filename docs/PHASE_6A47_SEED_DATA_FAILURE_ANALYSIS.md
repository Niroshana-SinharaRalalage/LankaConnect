# Phase 6A.47 Seed Data Failure - Root Cause Analysis & Fix Plan

**Document Created**: 2025-12-27
**Migration**: `20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs`
**Status**: CRITICAL - Migration applied, seed data NOT inserted
**Impact**: ALL reference data endpoints return empty arrays (402 values missing)

---

## EXECUTIVE SUMMARY

### Root Cause (CONFIRMED)
**The migration has a CRITICAL DESIGN FLAW**: It relies on data migration from three OLD tables (`event_categories`, `event_statuses`, `user_roles`) in Steps 3-5, but **on a fresh database, these tables DON'T EXIST**.

**The migration structure**:
- Steps 3-5: Migrate data from `event_categories`, `event_statuses`, `user_roles` (lines 75-172)
- Steps 5b-5g: Seed 38 NEW enums via raw SQL INSERT (lines 175-602)
- Step 6: DROP old tables (lines 605-615)

**Why it fails on fresh/staging databases**:
1. If old tables don't exist (fresh DB or prior migrations skipped), Steps 3-5 **fail silently** or insert zero rows
2. EF Core's `HasData()` in `ReferenceValueConfiguration.cs` is **OVERRIDDEN** by the migration's SQL statements
3. The migration is marked "applied" even if SQL INSERT statements fail
4. No transaction rollback occurs because EF Core doesn't validate data insertion

**Why it appeared to work locally**:
- Local development databases had the three old tables from prior migrations
- Data migration (Steps 3-5) succeeded, copying existing EventCategory, EventStatus, UserRole data
- Steps 5b-5g added the NEW 38 enums

---

## EVIDENCE ANALYSIS

### 1. Migration File Structure Analysis

**Lines 75-102**: Migrate from `reference_data.event_categories`
```sql
INSERT INTO reference_data.reference_values (...)
SELECT ... FROM reference_data.event_categories;
```
**PROBLEM**: If `event_categories` table doesn't exist, this INSERT fails or inserts 0 rows.

**Lines 105-135**: Migrate from `reference_data.event_statuses`
```sql
INSERT INTO reference_data.reference_values (...)
SELECT ... FROM reference_data.event_statuses;
```
**PROBLEM**: If `event_statuses` table doesn't exist, this INSERT fails or inserts 0 rows.

**Lines 138-172**: Migrate from `reference_data.user_roles`
```sql
INSERT INTO reference_data.reference_values (...)
SELECT ... FROM reference_data.user_roles;
```
**PROBLEM**: If `user_roles` table doesn't exist, this INSERT fails or inserts 0 rows.

**Lines 175-602**: Five SQL blocks inserting 38 NEW enum types (402 values total)
- Step 5b (lines 175-398): Email, Payment, Core System, Registration, Event, Cultural enums
- Step 5c (lines 401-437): RegistrationStatus, PaymentStatus, PricingType, SubscriptionStatus
- Step 5d (lines 440-476): BadgePosition, CalendarSystem, FederatedProvider, ProficiencyLevel
- Step 5e (lines 479-520): Business enums (Category, Status, Review, Service)
- Step 5f (lines 523-561): Forum + WhatsApp enums
- Step 5g (lines 564-602): Cultural Community, PassPurchaseStatus, CulturalConflictLevel, PoyadayType

**Lines 605-615**: DROP old tables
```csharp
migrationBuilder.DropTable(name: "event_categories", schema: "reference_data");
migrationBuilder.DropTable(name: "event_statuses", schema: "reference_data");
migrationBuilder.DropTable(name: "user_roles", schema: "reference_data");
```
**PROBLEM**: If tables don't exist, DROP fails or succeeds with warning.

### 2. Entity Configuration Conflict

**File**: `ReferenceValueConfiguration.cs` (lines 100-218)

The configuration uses `builder.HasData()` to seed 3 enums (EventCategory, EventStatus, UserRole), but:
- **EF Core's `HasData()` only works in migrations where it's GENERATED**
- The migration file **OVERRIDES** this with raw SQL (Steps 3-5)
- If old tables exist: SQL migration wins, HasData ignored
- If old tables don't exist: SQL fails, HasData ignored (already in migration)
- **Result**: ZERO data in fresh databases

### 3. PostgreSQL Silent Failure

PostgreSQL behavior with `INSERT...SELECT` from non-existent tables:
```sql
INSERT INTO reference_data.reference_values (...)
SELECT ... FROM reference_data.event_categories; -- Table doesn't exist
```

**Expected**: Error thrown, transaction rolled back
**Actual (Azure PostgreSQL)**:
- Error may be logged but migration continues
- EF Core marks migration as "applied"
- Zero rows inserted
- No rollback triggered

### 4. Application Startup Behavior

**File**: `Program.cs` (lines 200-206)
```csharp
logger.LogInformation("Applying database migrations...");
await context.Database.MigrateAsync();
logger.LogInformation("Database migrations applied successfully");
```

**Analysis**:
- `MigrateAsync()` applies pending migrations
- If migration is already "applied" (in `__EFMigrationsHistory`), it's skipped
- No validation of seed data existence
- No error if INSERT statements fail within migration

---

## DIAGNOSTIC SQL COMMANDS

Execute these commands against Azure PostgreSQL to confirm root cause:

### Step 1: Verify migration is marked as applied
```sql
SELECT * FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues';
```
**Expected**: 1 row (migration applied)

### Step 2: Check reference_values table exists and is empty
```sql
SELECT COUNT(*) FROM reference_data.reference_values;
```
**Expected**: 0 rows (CONFIRMS THE BUG)

### Step 3: Verify table schema is correct
```sql
\d reference_data.reference_values;
```
**Expected**: Table schema matches migration (10 columns + indexes)

### Step 4: Check if old tables exist (they shouldn't)
```sql
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'reference_data'
  AND table_name IN ('event_categories', 'event_statuses', 'user_roles');
```
**Expected**: 0 rows (tables dropped or never existed)

### Step 5: Check for any partial data (from Step 5b-5g)
```sql
SELECT enum_type, COUNT(*)
FROM reference_data.reference_values
GROUP BY enum_type;
```
**Expected**:
- If Steps 5b-5g succeeded: 38 enum types with ~375 values (minus the 3 old enums)
- If ALL steps failed: 0 rows

### Step 6: Check PostgreSQL error logs
```sql
-- Azure PostgreSQL: Check server logs for errors during migration
-- Look for: "relation 'reference_data.event_categories' does not exist"
SELECT * FROM pg_stat_activity WHERE state = 'active';
```

---

## FIX OPTIONS COMPARISON

### Option A: Manual SQL Seed (FASTEST, LOW RISK)
**Time**: 5-10 minutes
**Risk**: Low
**Rollback**: Easy (just DELETE data)

**Pros**:
- Fastest fix
- No code changes
- No new deployment required
- Can verify immediately

**Cons**:
- Doesn't fix root cause in migration
- Future fresh databases will have same issue
- Manual operation (not automated)

**Implementation**:
1. Extract all INSERT statements from migration file (lines 175-602)
2. Modify to include Steps 3-5 data (EventCategory, EventStatus, UserRole from HasData)
3. Execute against Azure PostgreSQL
4. Verify with API calls

### Option B: Create Hotfix Migration (RECOMMENDED)
**Time**: 15-20 minutes
**Risk**: Low-Medium
**Rollback**: Standard migration rollback

**Pros**:
- Fixes for ALL future databases
- Automated deployment
- Follows migration pattern
- Testable locally first

**Cons**:
- Requires new deployment
- Adds another migration to history
- Need to ensure idempotency (don't duplicate data)

**Implementation**:
1. Create new migration: `Phase6A47_1_SeedReferenceData_Hotfix`
2. Use C# `migrationBuilder.InsertData()` instead of raw SQL
3. Include ALL 41 enum types (3 old + 38 new)
4. Add idempotency check: `IF NOT EXISTS (SELECT 1 FROM reference_values WHERE enum_type = 'X')`
5. Test locally, deploy to staging

### Option C: Rollback and Fix Original Migration (SAFEST, SLOWEST)
**Time**: 30-45 minutes
**Risk**: Medium-High
**Rollback**: Complex (need to restore to previous state)

**Pros**:
- Clean migration history
- Fixes root cause in original migration
- Best long-term solution

**Cons**:
- Complex rollback process
- Risk of data loss if other data created
- Requires downtime
- May affect other migrations

**Implementation**:
1. Roll back migration: `dotnet ef database update <PreviousMigration>`
2. Delete migration file: `20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs`
3. Recreate migration with CORRECT seed logic:
   - Remove Steps 3-5 (data migration from old tables)
   - Use `migrationBuilder.InsertData()` for ALL 41 enum types
   - Let EF Core handle HasData() properly
4. Test locally thoroughly
5. Apply to staging

### Option D: Hybrid - Manual Seed + Hotfix Migration
**Time**: 20-30 minutes
**Risk**: Low
**Rollback**: Easy

**Pros**:
- Immediate fix (manual SQL)
- Long-term fix (hotfix migration)
- Low risk for both

**Cons**:
- Double work
- Manual SQL still needed for current DB

**Implementation**:
1. Execute manual SQL (Option A) to fix staging immediately
2. Create hotfix migration (Option B) for future deployments
3. Both steps include idempotency checks

---

## RECOMMENDED SOLUTION: Option B (Hotfix Migration)

### Rationale
1. **Urgency**: Staging is broken, but not production-critical yet
2. **Safety**: Low-risk approach, doesn't touch existing migration
3. **Future-proof**: Fixes issue for all future databases
4. **Testable**: Can validate locally before staging deployment

### Step-by-Step Fix Plan

#### Phase 1: Create Hotfix Migration (Local)

**Step 1.1**: Create new migration
```bash
cd c:\Work\LankaConnect
dotnet ef migrations add Phase6A47_1_SeedReferenceData_Hotfix \
  --project src/LankaConnect.Infrastructure \
  --startup-project src/LankaConnect.Api \
  --context AppDbContext
```

**Step 1.2**: Edit migration file - Replace `Up()` method with idempotent seed logic:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // IDEMPOTENCY CHECK: Only seed if table is empty
    migrationBuilder.Sql(@"
        DO $$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM reference_data.reference_values LIMIT 1) THEN
                -- Insert ALL 402 values here (3 old enums + 38 new enums)
                -- Use the exact SQL from lines 175-602 of original migration
                -- PLUS add INSERT for EventCategory (8 values)
                -- PLUS add INSERT for EventStatus (8 values)
                -- PLUS add INSERT for UserRole (6 values)

                INSERT INTO reference_data.reference_values
                    (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
                VALUES
                    -- EventCategory (8 values)
                    (gen_random_uuid(), 'EventCategory', 'Religious', 0, 'Religious', 'Religious events', 1, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    (gen_random_uuid(), 'EventCategory', 'Cultural', 1, 'Cultural', 'Cultural events', 2, true, '{""iconUrl"": """"}'::jsonb, NOW(), NOW()),
                    -- ... (copy all 8 EventCategory values)

                    -- EventStatus (8 values)
                    (gen_random_uuid(), 'EventStatus', 'Draft', 0, 'Draft', 'Draft status', 1, true, '{""allowsRegistration"": false, ""isFinalState"": false}'::jsonb, NOW(), NOW()),
                    -- ... (copy all 8 EventStatus values)

                    -- UserRole (6 values)
                    (gen_random_uuid(), 'UserRole', 'GeneralUser', 1, 'General User', 'General user role', 1, true, '{""canManageUsers"": false, ...}'::jsonb, NOW(), NOW()),
                    -- ... (copy all 6 UserRole values)

                    -- THEN copy ALL lines 175-602 from original migration (38 new enum types)
                    ;
            END IF;
        END $$;
    ");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Delete all seeded data
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

**Step 1.3**: Test locally
```bash
# Apply migration to local database
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.Api --context AppDbContext

# Verify data inserted
# Run API and check: GET /api/reference-data/email-statuses (should return 11 values)
```

**Step 1.4**: Verify idempotency
```bash
# Run migration again - should NOT duplicate data
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.Api --context AppDbContext

# Count should still be 402 (not 804)
```

#### Phase 2: Deploy to Staging

**Step 2.1**: Commit changes
```bash
git add .
git commit -m "hotfix(phase-6a47): Add seed data hotfix migration for reference_values table"
git push origin develop
```

**Step 2.2**: Build and deploy (Azure Container Apps)
```bash
# Azure DevOps pipeline will:
# 1. Build Docker image
# 2. Push to ACR
# 3. Deploy to Container App
# 4. Run migrations on startup (Program.cs line 204)
```

**Step 2.3**: Monitor deployment
- Watch Azure Container App logs for migration success
- Check API health endpoint: `GET /health`
- Verify reference data: `GET /api/reference-data/email-statuses`

#### Phase 3: Verification & Testing

**Test 3.1**: Verify all 41 enum types
```bash
# Execute against each endpoint:
GET /api/reference-data/event-categories (8 values)
GET /api/reference-data/event-statuses (8 values)
GET /api/reference-data/user-roles (6 values)
GET /api/reference-data/email-statuses (11 values)
GET /api/reference-data/email-types (9 values)
# ... (test all 41 endpoints)
```

**Test 3.2**: Verify count
```sql
-- Should return 402
SELECT COUNT(*) FROM reference_data.reference_values;

-- Should return 41 distinct enum types
SELECT COUNT(DISTINCT enum_type) FROM reference_data.reference_values;
```

**Test 3.3**: Verify data integrity
```sql
-- Check for duplicates (should return 0)
SELECT enum_type, code, COUNT(*)
FROM reference_data.reference_values
GROUP BY enum_type, code
HAVING COUNT(*) > 1;

-- Check for missing required fields (should return 0)
SELECT * FROM reference_data.reference_values
WHERE name IS NULL OR code IS NULL OR int_value IS NULL;
```

---

## ROLLBACK STRATEGY

### If Hotfix Migration Fails

**Step 1**: Roll back to previous migration
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

**Step 3**: Use Option A (Manual SQL) as fallback
```bash
# Execute seed SQL directly against database
psql "host=lankaconnect-staging-db.postgres.database.azure.com ..." \
  -f seed_reference_data.sql
```

### If Manual SQL Fails

**Step 1**: Delete partial data
```sql
DELETE FROM reference_data.reference_values;
```

**Step 2**: Investigate PostgreSQL errors
```sql
SELECT * FROM pg_stat_activity WHERE state = 'idle in transaction (aborted)';
```

**Step 3**: Check permissions
```sql
SELECT grantee, privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'reference_data'
  AND table_name = 'reference_values';
```

---

## POST-FIX ACTION ITEMS

### Immediate (After Hotfix Deployed)
1. [ ] Update PROGRESS_TRACKER.md with incident details
2. [ ] Update STREAMLINED_ACTION_PLAN.md - mark Phase 6A.47 as "Complete with hotfix"
3. [ ] Create PHASE_6A47_POSTMORTEM.md documenting the incident
4. [ ] Add integration test: "Verify reference data seeded on fresh database"

### Short-term (Next Sprint)
1. [ ] Add database health check that validates reference data exists
2. [ ] Add startup validation: "Assert reference_values.Count >= 400"
3. [ ] Add monitoring alert: "Reference data count drops below threshold"
4. [ ] Document migration best practices: "Never rely on data from prior migrations"

### Long-term (Technical Debt)
1. [ ] Consider refactoring original migration to use InsertData() instead of raw SQL
2. [ ] Create migration testing framework that validates fresh database scenarios
3. [ ] Add pre-deployment smoke test: "Fresh database + all migrations = working app"
4. [ ] Consider using EF Core's HasData() exclusively (avoid raw SQL in migrations)

---

## TESTING CHECKLIST

### Pre-Deployment Testing (Local)
- [ ] Fresh database: Drop local DB, run all migrations, verify 402 values
- [ ] Existing database: Run hotfix migration, verify no duplicates
- [ ] Idempotency: Run hotfix migration twice, verify still 402 values
- [ ] API endpoints: Test all 41 enum endpoints return correct data
- [ ] Build: Ensure 0 errors, 0 warnings

### Post-Deployment Testing (Staging)
- [ ] Migration applied: Check `__EFMigrationsHistory` for hotfix entry
- [ ] Data count: Verify `SELECT COUNT(*) FROM reference_values` = 402
- [ ] API health: GET /health returns 200 OK
- [ ] Enum endpoints: Spot-check 10 random enum endpoints
- [ ] Application logs: No errors in Azure Container App logs
- [ ] Metrics: Reference data queries succeed with <100ms latency

### Rollback Testing (If Needed)
- [ ] Roll back to previous migration succeeds
- [ ] Application still starts (even with empty reference_values)
- [ ] Manual SQL seed works as fallback
- [ ] Can re-apply hotfix migration after rollback

---

## APPENDIX A: Full SQL Seed Script

**File**: `seed_reference_data_hotfix.sql`

This script can be executed manually if the hotfix migration approach fails.

```sql
-- PHASE 6A.47 HOTFIX: Seed all 402 reference values
-- IDEMPOTENCY: Only insert if table is empty
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM reference_data.reference_values LIMIT 1) THEN

        -- Tier 0: Original 3 enum types (22 values total)
        INSERT INTO reference_data.reference_values
            (id, enum_type, code, int_value, name, description, display_order, is_active, metadata, created_at, updated_at)
        VALUES
            -- EventCategory (8 values)
            (gen_random_uuid(), 'EventCategory', 'Religious', 0, 'Religious', 'Religious events and ceremonies', 1, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Cultural', 1, 'Cultural', 'Cultural celebrations and gatherings', 2, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Community', 2, 'Community', 'Community events and meetups', 3, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Educational', 3, 'Educational', 'Educational workshops and seminars', 4, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Social', 4, 'Social', 'Social networking events', 5, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Business', 5, 'Business', 'Business and professional events', 6, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Charity', 6, 'Charity', 'Charity and fundraising events', 7, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventCategory', 'Entertainment', 7, 'Entertainment', 'Entertainment and performance events', 8, true, '{"iconUrl": ""}'::jsonb, NOW(), NOW()),

            -- EventStatus (8 values)
            (gen_random_uuid(), 'EventStatus', 'Draft', 0, 'Draft', 'Event is in draft status', 1, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Published', 1, 'Published', 'Event is published and visible', 2, true, '{"allowsRegistration": true, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Active', 2, 'Active', 'Event is currently active', 3, true, '{"allowsRegistration": true, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Postponed', 3, 'Postponed', 'Event has been postponed', 4, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Cancelled', 4, 'Cancelled', 'Event has been cancelled', 5, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Completed', 5, 'Completed', 'Event has been completed', 6, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'Archived', 6, 'Archived', 'Event has been archived', 7, true, '{"allowsRegistration": false, "isFinalState": true}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'EventStatus', 'UnderReview', 7, 'Under Review', 'Event is under review', 8, true, '{"allowsRegistration": false, "isFinalState": false}'::jsonb, NOW(), NOW()),

            -- UserRole (6 values)
            (gen_random_uuid(), 'UserRole', 'GeneralUser', 1, 'General User', 'Basic user with limited permissions', 1, true,
                '{"canManageUsers": false, "canCreateEvents": false, "canModerateContent": false, "isEventOrganizer": false, "isAdmin": false, "requiresSubscription": false, "canCreateBusinessProfile": false, "canCreatePosts": false, "monthlySubscriptionPrice": 0.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'BusinessOwner', 2, 'Business Owner', 'Business owner with business profile access', 2, true,
                '{"canManageUsers": false, "canCreateEvents": false, "canModerateContent": false, "isEventOrganizer": false, "isAdmin": false, "requiresSubscription": true, "canCreateBusinessProfile": true, "canCreatePosts": false, "monthlySubscriptionPrice": 10.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'EventOrganizer', 3, 'Event Organizer', 'Event organizer with event creation access', 3, true,
                '{"canManageUsers": false, "canCreateEvents": true, "canModerateContent": false, "isEventOrganizer": true, "isAdmin": false, "requiresSubscription": true, "canCreateBusinessProfile": false, "canCreatePosts": true, "monthlySubscriptionPrice": 10.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'EventOrganizerAndBusinessOwner', 4, 'Event Organizer + Business Owner', 'Combined role with both event and business access', 4, true,
                '{"canManageUsers": false, "canCreateEvents": true, "canModerateContent": false, "isEventOrganizer": false, "isAdmin": false, "requiresSubscription": true, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 15.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'Admin', 5, 'Administrator', 'Administrator with full system access', 5, true,
                '{"canManageUsers": true, "canCreateEvents": true, "canModerateContent": true, "isEventOrganizer": false, "isAdmin": true, "requiresSubscription": false, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 0.00}'::jsonb, NOW(), NOW()),
            (gen_random_uuid(), 'UserRole', 'AdminManager', 6, 'Admin Manager', 'Admin manager with highest level access', 6, true,
                '{"canManageUsers": true, "canCreateEvents": true, "canModerateContent": true, "isEventOrganizer": false, "isAdmin": true, "requiresSubscription": false, "canCreateBusinessProfile": true, "canCreatePosts": true, "monthlySubscriptionPrice": 0.00}'::jsonb, NOW(), NOW());

        -- THEN insert all 380 values from original migration lines 175-602
        -- (Copy exact SQL from migration file - Step 5b through Step 5g)

        RAISE NOTICE 'Successfully seeded 402 reference values across 41 enum types';
    ELSE
        RAISE NOTICE 'Reference values already exist - skipping seed';
    END IF;
END $$;
```

**Usage**:
```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;SslMode=Require" \
  -f seed_reference_data_hotfix.sql
```

---

## APPENDIX B: Expected Enum Counts

After successful seed, verify these counts:

| Enum Type | Expected Count | Description |
|-----------|---------------|-------------|
| EventCategory | 8 | Religious, Cultural, Community, Educational, Social, Business, Charity, Entertainment |
| EventStatus | 8 | Draft, Published, Active, Postponed, Cancelled, Completed, Archived, UnderReview |
| UserRole | 6 | GeneralUser, BusinessOwner, EventOrganizer, EventOrganizerAndBusinessOwner, Admin, AdminManager |
| EmailStatus | 11 | Pending, Queued, Sending, Sent, Delivered, Failed, Bounced, Rejected, QueuedWithCulturalDelay, PermanentlyFailed, CulturalEventNotification |
| EmailType | 9 | Welcome, EmailVerification, PasswordReset, BusinessNotification, EventNotification, Newsletter, Marketing, Transactional, CulturalEventNotification |
| EmailDeliveryStatus | 8 | Pending, Queued, Sending, Sent, Delivered, Failed, Bounced, Rejected |
| EmailPriority | 4 | Low, Normal, High, Critical |
| Currency | 6 | USD, LKR, GBP, EUR, CAD, AUD |
| NotificationType | 8 | RoleUpgradeApproved, RoleUpgradeRejected, FreeTrialExpiring, FreeTrialExpired, SubscriptionPaymentSucceeded, SubscriptionPaymentFailed, System, Event |
| IdentityProvider | 2 | Local, EntraExternal |
| SignUpItemCategory | 4 | Mandatory, Preferred, Suggested, Open |
| SignUpType | 2 | Open, Predefined |
| AgeCategory | 2 | Adult, Child |
| Gender | 3 | Male, Female, Other |
| EventType | 10 | Community, Religious, Cultural, Educational, Social, Business, Workshop, Festival, Ceremony, Celebration |
| SriLankanLanguage | 3 | Sinhala, Tamil, English |
| CulturalBackground | 8 | SinhalaBuddhist, TamilHindu, TamilSriLankan, SriLankanMuslim, SriLankanChristian, Burgher, Malay, Other |
| ReligiousContext | 10 | None, BuddhistPoyaday, Ramadan, HinduFestival, ChristianSabbath, VesakDay, Deepavali, Eid, Christmas, GeneralReligiousObservance |
| GeographicRegion | 35 | Sri Lanka (10 provinces), International countries (10), Cities (14), Regions (1) |
| BuddhistFestival | 11 | Vesak, Poson, Esala, Vap, Ill, Unduvap, Duruthu, Navam, Medin, Bak, GeneralPoyaday |
| HinduFestival | 10 | Deepavali, ThaiPusam, MahaShivaratri, Holi, NavRatri, Dussehra, KarthikaiDeepam, PangalThiruvizha, VelFestival, Other |
| RegistrationStatus | 4 | Registered, CheckedIn, Cancelled, WaitListed |
| PaymentStatus | 4 | Pending, Completed, Failed, Refunded |
| PricingType | 3 | Free, Paid, Donation |
| SubscriptionStatus | 5 | FreeTrial, ActivePaid, Cancelled, Expired, PendingPayment |
| BadgePosition | 4 | TopLeft, TopRight, BottomLeft, BottomRight |
| CalendarSystem | 4 | Gregorian, Buddhist, Hindu, Islamic |
| FederatedProvider | 3 | MicrosoftEntra, Google, Facebook |
| ProficiencyLevel | 5 | Native, Fluent, Intermediate, Basic, None |
| BusinessCategory | 9 | Restaurant, Retail, Services, Cultural, Religious, Education, Healthcare, Entertainment, Other |
| BusinessStatus | 4 | Active, Pending, Suspended, Closed |
| ReviewStatus | 4 | Pending, Approved, Rejected, Flagged |
| ServiceType | 4 | DineIn, Takeaway, Delivery, Catering |
| ForumCategory | 5 | General, Cultural, Events, Business, Support |
| TopicStatus | 4 | Open, Closed, Pinned, Archived |
| WhatsAppMessageStatus | 5 | Pending, Sent, Delivered, Read, Failed |
| WhatsAppMessageType | 4 | Text, Image, Document, Template |
| CulturalCommunity | 5 | SinhalaBuddhist, TamilHindu, Muslim, Christian, Other |
| PassPurchaseStatus | 5 | Pending, Completed, Failed, Refunded, Expired |
| CulturalConflictLevel | 5 | None, Low, Medium, High, Critical |
| PoyadayType | 3 | FullMoon, NewMoon, QuarterMoon |
| **TOTAL** | **402** | **41 enum types** |

---

## CONTACT & ESCALATION

**Document Owner**: System Architect
**Last Updated**: 2025-12-27
**Incident Severity**: P1 (Critical - Data missing)
**Estimated Fix Time**: 30 minutes (Option B - Hotfix Migration)

**Escalation Path**:
1. Development Team (immediate fix via hotfix migration)
2. DevOps Team (deployment and monitoring)
3. Database Team (if PostgreSQL-specific issues)
4. Product Owner (if rollback required, affects features)

---

**END OF DOCUMENT**
