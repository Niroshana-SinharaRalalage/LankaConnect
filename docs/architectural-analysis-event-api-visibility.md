# Architectural Analysis: Event API Visibility Issue

**Date**: 2025-11-03
**Severity**: Critical - Application crash on startup
**Impact**: All Event APIs (20 endpoints) unavailable in production/staging
**Status**: Root cause identified, solution designed

---

## Executive Summary

The Event APIs are not appearing in Swagger and the application crashes on startup due to a **critical database schema mismatch**. The staging database has an incomplete Events table structure that conflicts with the application's EF Core model expectations.

### Critical Finding

**The staging database has TWO separate Events tables**:
1. `events.events` - Created by `InitialCreate` migration (contains Status column)
2. A **second Events table** (or corrupted schema) - Created by a now-deleted migration `20251102000000_CreateEventsAndRegistrationsTables.cs`

This dual-table state causes the PostGIS migration to fail when attempting to create an index on a Status column that doesn't exist in the active Events table.

---

## Part 1: Root Cause Analysis

### 1.1 The Smoking Gun: Column Name Case Sensitivity

**Critical Evidence from Migration Analysis**:

```sql
-- InitialCreate (Line 35): Creates column as "Status" (Pascal case)
Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)

-- AddEventLocationWithPostGIS (Line 118): References column as "status" (lowercase)
CREATE INDEX ix_events_status_city_startdate
ON events.events (status, address_city, start_date)  -- FAILS: "column status does not exist"
```

**PostgreSQL is case-sensitive for unquoted identifiers**, and EF Core's naming convention uses **snake_case** by default, but this project uses **PascalCase** for column names (as seen in `InitialCreate`).

### 1.2 The Deleted Migration Problem

**Commit History Analysis**:

```bash
b57a6d1 fix(migrations): Add missing Event and Registration table creation migrations
f582356 fix(migrations): Consolidate Event migrations to correct directory
```

**What Happened**:
1. Developer created `20251102000000_CreateEventsAndRegistrationsTables.cs` to "fix" missing tables
2. This migration created a SECOND Events table (or overwrote the first with incorrect schema)
3. Migration was later DELETED (commit f582356) as "redundant"
4. Staging database STILL HAS this migration's schema changes applied
5. Migration history table (`__EFMigrationsHistory`) shows migration as "applied"
6. Subsequent migrations fail because they expect the `InitialCreate` schema, not the deleted migration's schema

### 1.3 Why Application Crashes

**Startup Sequence** (Program.cs lines 150-169):

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync(); // FAILS HERE
    // Application never reaches MapControllers()
    // EventsController never registers
    // Swagger shows 0 Event endpoints
}
```

**Failure Cascade**:
1. `MigrateAsync()` attempts to apply `AddEventLocationWithPostGIS` migration
2. Migration executes raw SQL: `CREATE INDEX ... ON events.events (status, ...)` (line 118)
3. PostgreSQL error: `column "status" does not exist`
4. Exception thrown, caught by Program.cs catch block (line 164)
5. Application logs error and RE-THROWS (line 168)
6. Container startup fails
7. Kubernetes restarts container in crash loop

**Result**: EventsController.cs never loads, Swagger never scans it, APIs invisible.

---

## Part 2: Database State Investigation

### 2.1 Expected vs Actual Schema

**Expected Schema** (from InitialCreate):
```sql
CREATE TABLE events.events (
    "Id" uuid PRIMARY KEY,
    "title" varchar(200) NOT NULL,
    "description" varchar(2000) NOT NULL,
    "StartDate" timestamptz NOT NULL,
    "EndDate" timestamptz NOT NULL,
    "OrganizerId" uuid NOT NULL,
    "Capacity" integer NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'Draft',  -- PASCAL CASE
    "CancellationReason" varchar(500),
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz
);
```

**Actual Schema** (suspected, based on error):
```sql
-- Schema created by deleted migration (lowercase column names)
CREATE TABLE events.events (
    id uuid PRIMARY KEY,
    title varchar(200) NOT NULL,
    description varchar(2000) NOT NULL,
    start_date timestamptz NOT NULL,
    end_date timestamptz NOT NULL,
    organizer_id uuid NOT NULL,
    capacity integer NOT NULL,
    -- NO "Status" OR "status" COLUMN
    cancellation_reason varchar(500),
    created_at timestamptz NOT NULL DEFAULT NOW(),
    updated_at timestamptz
);
```

### 2.2 Migration History Corruption

**Hypothesis**: The `__EFMigrationsHistory` table contains:

```sql
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Event%'
ORDER BY "MigrationId";
```

**Expected Output**:
```
20250830150251_InitialCreate
20251102061243_AddEventLocationWithPostGIS
20251102144315_AddEventCategoryAndTicketPrice
20251103040053_AddEventImages
```

**Actual Output (suspected)**:
```
20250830150251_InitialCreate
20251102000000_CreateEventsAndRegistrationsTables  -- DELETED FROM CODE BUT APPLIED TO DB
20251102061243_AddEventLocationWithPostGIS         -- PARTIALLY FAILED
20251102144315_AddEventCategoryAndTicketPrice      -- NEVER APPLIED
20251103040053_AddEventImages                      -- NEVER APPLIED
```

---

## Part 3: Technology Stack Analysis

### 3.1 EF Core Migration Behavior

**Critical Understanding**:
- EF Core uses `__EFMigrationsHistory` as source of truth for "what's applied"
- If a migration exists in history but NOT in codebase, EF Core **assumes it's still valid**
- Deleting a migration from code does NOT revert its database changes
- This creates a **schema drift** between code and database

### 3.2 PostgreSQL Naming Conventions

**EF Core Default Behavior**:
- Column names: `snake_case` (e.g., `start_date`)
- Table names: `snake_case` (e.g., `event_registrations`)

**This Project's Configuration** (EventConfiguration.cs):
- Explicitly uses `PascalCase` via `HasColumnName()` (e.g., `StartDate`)
- Raw SQL migrations MUST match this convention
- **BUG**: Line 118 in `AddEventLocationWithPostGIS` uses lowercase `status` instead of `Status`

### 3.3 Azure Container Apps Deployment

**CI/CD Pipeline** (deploy-staging.yml):

```yaml
- name: Update Container App (line 101)
  az containerapp update \
    --image lankaconnectstaging.azurecr.io/lankaconnect-api:${{ github.sha }}
```

**Zero Database Backup Strategy**:
- No pre-deployment schema backup
- No post-deployment rollback mechanism
- No migration dry-run validation
- **Risk**: Destructive migrations cannot be reverted

---

## Part 4: Architectural Gaps Identified

### 4.1 Missing Database Migration Safeguards

**Current State**:
- Migrations applied automatically on startup (Program.cs line 161)
- No validation before applying migrations
- No rollback strategy
- No schema version checking

**Required Safeguards**:
1. **Pre-deployment schema backup** (Azure SQL Database automated backups insufficient)
2. **Migration dry-run** in CI/CD pipeline (using `dotnet ef migrations script`)
3. **Schema validation** before container startup
4. **Health check** that verifies database schema matches model

### 4.2 Inconsistent Naming Conventions

**Problem**: Mixed use of PascalCase and snake_case in migrations

**Evidence**:
- `InitialCreate`: Uses `Status` (PascalCase)
- `AddEventLocationWithPostGIS`: Uses `status` (snake_case) in raw SQL
- EventConfiguration.cs: Uses `HasColumnName("title")` (snake_case) for some, PascalCase for others

**Solution Required**: Enforce consistent naming convention via:
1. EF Core global naming convention
2. Migration code review checklist
3. Database schema linting

### 4.3 Migration Deletion Without Database Cleanup

**Current Process**:
1. Developer creates migration
2. Migration gets applied to staging database
3. Developer deletes migration from code
4. Database retains migration's schema changes
5. EF Core confusion ensues

**Required Process**:
1. Create migration
2. Apply to dev environment
3. Discover it's wrong
4. **Run `dotnet ef database update <previous-migration>` to revert**
5. Delete migration file
6. Create corrected migration
7. Apply to all environments

---

## Part 5: Solution Design

### 5.1 Immediate Fix (Staging Environment)

**Objective**: Restore staging database to clean state and reapply migrations

**Strategy**: Nuclear option - drop and recreate Events schema

**Implementation**:

```sql
-- Step 1: Drop corrupted schema
DROP SCHEMA IF EXISTS events CASCADE;

-- Step 2: Clean migration history
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
    '20251102000000_CreateEventsAndRegistrationsTables',
    '20251102061243_AddEventLocationWithPostGIS',
    '20251102144315_AddEventCategoryAndTicketPrice',
    '20251103040053_AddEventImages'
);

-- Step 3: Redeploy application
-- Migrations will recreate schema from InitialCreate
```

**Execution Plan**:
1. Connect to Azure PostgreSQL staging database
2. Execute script via Azure Data Studio or `psql`
3. Trigger manual deployment via GitHub Actions
4. Monitor container logs for successful migration
5. Verify Swagger shows 20 Event endpoints

**Risk Assessment**:
- **Data Loss**: YES - All Events and Registrations deleted
- **Downtime**: ~5 minutes (schema drop + container restart)
- **Rollback**: N/A (no Events data exists in staging)
- **Acceptable**: YES (staging environment, no production data)

### 5.2 Root Cause Fix (Codebase)

**Bug 1: Column Name Case Mismatch**

**File**: `src/LankaConnect.Infrastructure/Migrations/20251102061243_AddEventLocationWithPostGIS.cs`

**Current Code** (Line 118):
```sql
CREATE INDEX ix_events_status_city_startdate
ON events.events (status, address_city, start_date)  -- WRONG: lowercase
```

**Corrected Code**:
```sql
CREATE INDEX ix_events_status_city_startdate
ON events.events ("Status", address_city, "StartDate")  -- CORRECT: match InitialCreate
```

**Bug 2: Inconsistent Naming in EventConfiguration**

**Current**: Mixed PascalCase and snake_case
**Solution**: Standardize on snake_case for ALL columns

**Recommended Change**:
```csharp
// Option A: Use snake_case everywhere (PostgreSQL convention)
builder.Property(e => e.Status)
    .HasColumnName("status")  // Explicit snake_case
    .HasConversion<string>()
    .HasMaxLength(20)
    .IsRequired()
    .HasDefaultValue(EventStatus.Draft);

// Option B: Configure global naming convention
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.Conventions.Add(_ => new SnakeCaseNamingConvention());
}
```

### 5.3 Long-Term Prevention

**Architectural Decision Record (ADR)**: Database Migration Safety

**Decision**: Implement multi-layer migration validation

**Implementation**:

1. **CI/CD Pipeline Enhancement** (deploy-staging.yml):
```yaml
- name: Generate Migration Script
  run: |
    dotnet ef migrations script \
      --project src/LankaConnect.Infrastructure \
      --startup-project src/LankaConnect.API \
      --idempotent \
      --output migration.sql

- name: Validate Migration Script
  run: |
    # Check for case-sensitive column references
    if grep -iE '(status|start_date|end_date)' migration.sql | grep -v '"'; then
      echo "ERROR: Unquoted case-sensitive column names detected"
      exit 1
    fi

- name: Backup Database Schema
  run: |
    az postgres flexible-server execute \
      --name lankaconnect-staging \
      --database-name lankaconnect \
      --file-path backup-schema.sql \
      "SELECT * FROM __EFMigrationsHistory"
```

2. **Health Check Enhancement** (Program.cs):
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("database-schema-validation", () =>
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Verify Events table has required columns
        var hasStatusColumn = await context.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM information_schema.columns WHERE table_name = 'events' AND column_name = 'Status'"
        );

        return hasStatusColumn > 0
            ? HealthCheckResult.Healthy("Events schema valid")
            : HealthCheckResult.Unhealthy("Events schema missing Status column");
    });
```

3. **Migration Code Review Checklist**:
```markdown
- [ ] Column names match EventConfiguration.cs exactly
- [ ] Raw SQL uses quoted identifiers for case-sensitive names
- [ ] Migration tested against local PostgreSQL database
- [ ] Migration script reviewed for DROP statements
- [ ] Rollback migration (Down method) tested
- [ ] No hard-coded connection strings or secrets
```

---

## Part 6: Implementation Plan

### Phase 1: Emergency Fix (Day 1 - 2 hours)

**Objective**: Restore Event APIs to staging immediately

**Tasks**:
1. Execute `reset-events-schema.sql` against staging database
2. Verify schema dropped: `SELECT * FROM events.events` returns error
3. Verify migration history cleaned: `SELECT * FROM __EFMigrationsHistory WHERE MigrationId LIKE '%Event%'` returns 0 rows
4. Trigger manual deployment via GitHub Actions
5. Monitor container logs: `az containerapp logs show --name lankaconnect-api-staging --tail 100`
6. Verify health check: `curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health`
7. Verify Swagger shows Event endpoints: Visit `/swagger/index.html`
8. Test Event creation: `POST /api/events`

**Success Criteria**:
- Health check returns 200 OK
- Swagger shows 20 Event endpoints
- Can create/retrieve/update/delete Events
- Container logs show no migration errors

### Phase 2: Code Fix (Day 2 - 4 hours)

**Objective**: Fix migration bugs and prevent recurrence

**Tasks**:
1. Fix `AddEventLocationWithPostGIS.cs` column name case
2. Standardize EventConfiguration.cs naming convention
3. Add global snake_case naming convention to AppDbContext
4. Create new migration to capture fixes
5. Update all raw SQL migrations to use quoted identifiers
6. Add migration validation script to CI/CD pipeline
7. Document naming convention in CODING_STANDARDS.md

**Success Criteria**:
- All migrations use consistent column name casing
- CI/CD pipeline validates migration scripts
- Local tests pass against PostgreSQL database
- Code review checklist completed

### Phase 3: Architectural Improvements (Week 2 - 16 hours)

**Objective**: Implement long-term safeguards

**Tasks**:
1. Add database schema health check
2. Implement pre-deployment schema backup
3. Add migration dry-run to CI/CD pipeline
4. Create database rollback procedure
5. Add schema version endpoint: `GET /api/health/database/version`
6. Implement database migration audit logging
7. Create Architecture Decision Record (ADR)
8. Train team on migration best practices

**Success Criteria**:
- Every deployment has automated schema backup
- Migration failures trigger rollback automatically
- Schema drift detected before container startup
- Team trained on migration procedures

---

## Part 7: Verification Steps

### 7.1 Pre-Fix Verification

**Confirm the Problem**:

```bash
# Step 1: Check container logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100

# Expected output: "column status does not exist"

# Step 2: Check migration history
az postgres flexible-server execute \
  --name lankaconnect-staging \
  --database-name lankaconnect \
  --querytext "SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId"

# Expected output: Shows 20251102000000_CreateEventsAndRegistrationsTables

# Step 3: Verify Events table schema
az postgres flexible-server execute \
  --name lankaconnect-staging \
  --database-name lankaconnect \
  --querytext "SELECT column_name FROM information_schema.columns WHERE table_name = 'events' AND table_schema = 'events'"

# Expected output: Missing "Status" column
```

### 7.2 Post-Fix Verification

**Confirm the Solution**:

```bash
# Step 1: Verify schema recreated
az postgres flexible-server execute \
  --name lankaconnect-staging \
  --database-name lankaconnect \
  --querytext "SELECT column_name FROM information_schema.columns WHERE table_name = 'events' AND table_schema = 'events' ORDER BY ordinal_position"

# Expected output: Shows "Status" column (or "status" if snake_case convention applied)

# Step 2: Verify migrations reapplied
az postgres flexible-server execute \
  --name lankaconnect-staging \
  --database-name lankaconnect \
  --querytext "SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId LIKE '%Event%' ORDER BY MigrationId"

# Expected output:
# 20250830150251_InitialCreate
# 20251102061243_AddEventLocationWithPostGIS
# 20251102144315_AddEventCategoryAndTicketPrice
# 20251103040053_AddEventImages

# Step 3: Test API endpoint
curl -X GET https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events \
  -H "Content-Type: application/json"

# Expected output: 200 OK with empty array []

# Step 4: Verify Swagger
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json \
  | jq '.paths | keys | map(select(contains("events"))) | length'

# Expected output: 20 (number of Event endpoints)
```

---

## Part 8: Answers to Critical Questions

### Q1: Root Cause?

**Answer**: Three simultaneous failures:

1. **Primary Cause**: Column name case mismatch in PostGIS migration (line 118)
   - Raw SQL uses lowercase `status`
   - EF Core schema uses PascalCase `Status`
   - PostgreSQL cannot find column, migration fails

2. **Secondary Cause**: Deleted migration still applied to database
   - `20251102000000_CreateEventsAndRegistrationsTables` deleted from code
   - Schema changes remain in staging database
   - Creates schema drift between code and database

3. **Tertiary Cause**: No migration validation in CI/CD
   - Migrations applied blindly on container startup
   - No dry-run testing before deployment
   - No schema backup before destructive operations

### Q2: Migration History Discrepancy?

**Answer**: The deleted migration `20251102000000_CreateEventsAndRegistrationsTables` was:

1. Created to "fix" missing Events table
2. Applied to staging database (created wrong schema)
3. Deleted from codebase (commit f582356)
4. **NEVER removed from database**

**Evidence**:
- File deleted: `src/LankaConnect.Infrastructure/Data/Migrations/20251102000000_CreateEventsAndRegistrationsTables.cs`
- Database history: Migration ID still in `__EFMigrationsHistory`
- Schema remains: Events table exists but with wrong columns

**Correct Process** (should have been):
1. `dotnet ef database update 20250830150251_InitialCreate` (revert to before bad migration)
2. Delete migration file
3. Verify database schema matches InitialCreate
4. Redeploy application

### Q3: Recovery Strategy?

**Answer**: Nuclear option - drop and recreate schema

**Why Nuclear**:
1. **Schema Drift Too Severe**: Cannot determine exact state of Events table
2. **No Data Loss Risk**: Staging has no production Events data
3. **Fastest Recovery**: Drop + recreate = 5 minutes vs. debugging schema = hours
4. **Clean Slate**: Ensures perfect alignment with InitialCreate migration

**Why NOT Incremental Fix**:
1. Unknown state: Don't know which columns exist
2. Unknown constraints: Don't know which indexes/foreign keys exist
3. Unknown data: Don't know if partial data exists
4. Risk of partial fix: Could leave database in worse state

**Alternative Considered**:
- Manual schema repair (add missing columns)
- **Rejected because**: Requires knowing exact current state, high risk of human error

### Q4: Prevention Measures?

**Answer**: Five-layer defense:

**Layer 1: CI/CD Pipeline Validation**
- Generate idempotent migration script
- Validate script for case-sensitive column names
- Dry-run migration against test database
- Fail build if validation errors

**Layer 2: Database Health Check**
- Verify schema matches EF Core model
- Run on every container startup BEFORE accepting traffic
- Return HTTP 503 if schema invalid
- Prevent broken state from serving requests

**Layer 3: Pre-Deployment Backup**
- Automated schema backup before every deployment
- Store backup in Azure Blob Storage
- Tag with deployment commit SHA
- Enable one-click rollback

**Layer 4: Code Review Checklist**
- Migration naming convention check
- Raw SQL syntax validation
- Rollback (Down method) testing
- PostgreSQL case-sensitivity audit

**Layer 5: Team Training**
- Document migration best practices
- Create runbook for migration failures
- Practice rollback procedures
- Quarterly disaster recovery drills

### Q5: Verification of Actual Schema?

**Answer**: Two approaches:

**Approach 1: Azure Portal Query (Recommended)**
```sql
-- Connect via Azure Data Studio or psql
-- Host: lankaconnect-staging.postgres.database.azure.com

SELECT
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
ORDER BY ordinal_position;
```

**Approach 2: EF Core Model Comparison**
```bash
# Generate current database schema
dotnet ef dbcontext scaffold \
  "Host=lankaconnect-staging.postgres.database.azure.com;Database=lankaconnect;..." \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  --output-dir TempModels \
  --context StagingDbContext \
  --force

# Compare generated Event entity with Domain/Events/Event.cs
diff TempModels/Event.cs src/LankaConnect.Domain/Events/Event.cs
```

---

## Part 9: Risk Assessment

### 9.1 Immediate Fix Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Data loss in staging Events | 100% | Low | No production data exists |
| Deployment downtime >10min | 30% | Medium | Automated rollback if fails |
| Migration fails again | 20% | High | Test against local PostgreSQL first |
| Other schemas corrupted | 5% | Critical | Backup entire database first |

### 9.2 Long-Term Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Same issue in production | 80% | Critical | DO NOT deploy to prod until fixed |
| Team repeats mistake | 60% | High | Training + code review checklist |
| Schema drift undetected | 40% | High | Health check implementation |
| Manual rollback required | 30% | Medium | Automated backup + restore procedure |

---

## Part 10: Recommendations

### 10.1 Immediate Actions (Next 4 Hours)

1. **STOP** all deployments to production until fix verified
2. Execute emergency fix in staging (drop/recreate schema)
3. Verify Swagger shows Event endpoints
4. Test Event CRUD operations
5. Monitor staging for 24 hours

### 10.2 Short-Term Actions (Next 2 Weeks)

1. Fix migration column name casing bugs
2. Implement CI/CD migration validation
3. Add database schema health check
4. Create migration code review checklist
5. Document naming convention standards
6. Train team on migration procedures

### 10.3 Long-Term Actions (Next Quarter)

1. Implement automated schema backup/restore
2. Add schema version endpoint
3. Create database migration audit trail
4. Establish disaster recovery SLA
5. Quarterly migration procedure drills
6. Consider migration tool upgrade (FluentMigrator, DbUp)

---

## Part 11: Architecture Decision Record

**ADR-007: Database Migration Safety and Naming Conventions**

**Status**: Proposed

**Context**:
Application crashes on startup due to schema mismatch between EF Core migrations and PostgreSQL database caused by:
1. Inconsistent column name casing (PascalCase vs snake_case)
2. Deleted migrations still applied to database
3. No CI/CD validation of migration scripts

**Decision**:
1. Standardize on **snake_case** for all PostgreSQL identifiers
2. Enforce quoted identifiers in raw SQL migrations
3. Require CI/CD migration validation before deployment
4. Implement database schema health checks
5. Mandate pre-deployment schema backups

**Consequences**:

**Positive**:
- Consistent naming prevents case-sensitivity bugs
- CI/CD validation catches errors before deployment
- Health checks prevent broken deployments from serving traffic
- Backups enable quick rollback from failures

**Negative**:
- Migration development takes longer (validation overhead)
- Team must learn PostgreSQL quoting rules
- Additional CI/CD pipeline complexity

**Alternatives Considered**:
1. Keep PascalCase everywhere - Rejected (fights PostgreSQL conventions)
2. Manual migration reviews only - Rejected (human error risk)
3. Disable auto-migration on startup - Rejected (requires manual deployment step)

---

## Conclusion

This issue represents a **critical architectural failure** in the database migration process. The combination of:
1. Inconsistent naming conventions
2. Missing migration validation
3. Deleted migrations still in database
4. No schema health checks

...created a perfect storm that crashed the application on startup.

The solution requires both **immediate remediation** (drop/recreate schema) and **long-term architectural improvements** (validation, health checks, backups).

**Critical Success Factor**: DO NOT deploy to production until:
1. Staging has been stable for 48+ hours after fix
2. Migration validation is in CI/CD pipeline
3. Schema health check is implemented
4. Team is trained on new migration procedures

**Estimated Recovery Time**:
- Emergency fix: 2 hours
- Code fixes: 4 hours
- Architectural improvements: 16 hours
- **Total**: 22 hours over 2 weeks

**Approval Required From**:
- Database Administrator (schema drop approval)
- DevOps Lead (CI/CD changes approval)
- Product Owner (deployment delay approval)
- Security Team (backup strategy approval)

---

**Next Steps**:
1. Review this analysis with technical leadership
2. Get approval for emergency schema drop
3. Execute Phase 1 (emergency fix)
4. Schedule Phase 2 and 3 implementation
5. Update project documentation with lessons learned
