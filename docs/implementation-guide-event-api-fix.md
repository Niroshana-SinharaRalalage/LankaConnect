# Implementation Guide: Event API Fix

**Date**: 2025-11-03
**Estimated Time**: 2 hours for emergency fix
**Risk Level**: Medium (staging only, no production data)

---

## Prerequisites

Before starting, verify you have:

1. Azure CLI installed and logged in: `az login`
2. Access to Azure PostgreSQL staging database
3. GitHub Actions workflow trigger permissions
4. Azure Data Studio or `psql` client installed
5. Access to staging container app logs

---

## Phase 1: Emergency Fix (Immediate - 2 hours)

### Step 1: Backup Current State (15 minutes)

**Objective**: Capture current database state for rollback if needed

```bash
# 1.1 Connect to staging database
az postgres flexible-server connect \
  --name lankaconnect-staging \
  --admin-user adminuser \
  --database-name lankaconnect

# 1.2 Export migration history
\copy (SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId") TO 'migration-history-backup.csv' WITH CSV HEADER;

# 1.3 Export Events schema (if any data exists)
\copy (SELECT * FROM events.events) TO 'events-backup.csv' WITH CSV HEADER;

# 1.4 Export Registrations schema (if any data exists)
\copy (SELECT * FROM events.registrations) TO 'registrations-backup.csv' WITH CSV HEADER;

# 1.5 Exit psql
\q
```

**Verification**:
- [ ] File `migration-history-backup.csv` created
- [ ] File `events-backup.csv` created (or error if table doesn't exist - OK)
- [ ] File `registrations-backup.csv` created (or error if table doesn't exist - OK)

---

### Step 2: Diagnose Current Schema (10 minutes)

**Objective**: Confirm the exact state of the Events table

```bash
# 2.1 Reconnect to staging database
az postgres flexible-server connect \
  --name lankaconnect-staging \
  --admin-user adminuser \
  --database-name lankaconnect

# 2.2 Check if Events table exists
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'events'
ORDER BY table_name;

# Expected output:
# table_name
# -----------
# events
# registrations
# (or neither if schema doesn't exist)

# 2.3 If Events table exists, check its columns
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

# 2.4 Check migration history for Events-related migrations
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Event%'
   OR "MigrationId" LIKE '%Registration%'
   OR "MigrationId" LIKE '%20251102%'
   OR "MigrationId" LIKE '%20251103%'
ORDER BY "MigrationId";

# 2.5 Exit psql
\q
```

**Document Findings**:

```
Events table exists: YES / NO
Status column exists: YES / NO (if table exists)
Status column name: "Status" (PascalCase) / "status" (snake_case) / N/A

Migration history includes:
[ ] 20250830150251_InitialCreate
[ ] 20251102000000_CreateEventsAndRegistrationsTables (SHOULD NOT BE HERE)
[ ] 20251102061243_AddEventLocationWithPostGIS
[ ] 20251102144315_AddEventCategoryAndTicketPrice
[ ] 20251103040053_AddEventImages
```

---

### Step 3: Execute Emergency Fix (20 minutes)

**Objective**: Drop and recreate Events schema to clean state

```bash
# 3.1 Reconnect to staging database
az postgres flexible-server connect \
  --name lankaconnect-staging \
  --admin-user adminuser \
  --database-name lankaconnect

# 3.2 Verify NO production data exists
SELECT COUNT(*) AS event_count FROM events.events;
-- If count > 0, STOP and escalate to product owner

SELECT COUNT(*) AS registration_count FROM events.registrations;
-- If count > 0, STOP and escalate to product owner

# 3.3 Drop Events schema (DESTRUCTIVE - NO UNDO)
DROP SCHEMA IF EXISTS events CASCADE;

# Expected output:
# NOTICE:  drop cascades to 2 other objects
# DETAIL:  drop cascades to table events.events
# drop cascades to table events.registrations
# DROP SCHEMA

# 3.4 Clean migration history
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
    '20251102000000_CreateEventsAndRegistrationsTables',
    '20251102061243_AddEventLocationWithPostGIS',
    '20251102144315_AddEventCategoryAndTicketPrice',
    '20251103040053_AddEventImages'
);

-- Expected output: DELETE 3 or DELETE 4

# 3.5 Verify clean state
SELECT "MigrationId"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Event%'
   OR "MigrationId" LIKE '%Registration%';

-- Expected output: (0 rows)

# 3.6 Verify schema dropped
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'events';

-- Expected output: (0 rows)

# 3.7 Exit psql
\q
```

**Verification**:
- [ ] Events schema dropped successfully
- [ ] 3-4 migration history entries deleted
- [ ] No Events-related tables remain
- [ ] No Events-related migrations in history

---

### Step 4: Trigger Redeployment (10 minutes)

**Objective**: Redeploy application to apply migrations from scratch

```bash
# 4.1 Trigger manual deployment via GitHub Actions
# Option A: Via GitHub UI
# Navigate to: https://github.com/YOUR_ORG/LankaConnect/actions/workflows/deploy-staging.yml
# Click "Run workflow" -> Select "develop" branch -> "Run workflow"

# Option B: Via GitHub CLI
gh workflow run deploy-staging.yml --ref develop

# 4.2 Monitor deployment progress
gh run watch

# Expected output:
# ✓ Checkout code
# ✓ Setup .NET 8.0.x
# ✓ Restore dependencies
# ✓ Build application
# ✓ Run unit tests
# ✓ Azure Login
# ✓ Login to Azure Container Registry
# ✓ Publish application
# ✓ Build Docker image
# ✓ Push Docker image
# ✓ Get Key Vault secrets
# ✓ Update Container App
# ✓ Wait for deployment
# ✓ Get Container App URL
# ✓ Smoke Test - Health Check
# ✓ Smoke Test - Entra Endpoint
# ✓ Deployment Summary
```

**Verification**:
- [ ] Deployment completed successfully (green checkmark)
- [ ] All tests passed
- [ ] Smoke tests passed

---

### Step 5: Monitor Container Startup (15 minutes)

**Objective**: Verify migrations apply successfully on container startup

```bash
# 5.1 Get container logs in real-time
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 \
  --follow

# 5.2 Watch for migration success messages
# Expected log sequence:
# [INF] Starting LankaConnect API
# [INF] Applying database migrations...
# [INF] Applied migration '20250830150251_InitialCreate'
# [INF] Applied migration '20250831125422_InitialMigration'
# ... (other migrations)
# [INF] Applied migration '20251102061243_AddEventLocationWithPostGIS'
# [INF] Applied migration '20251102144315_AddEventCategoryAndTicketPrice'
# [INF] Applied migration '20251103040053_AddEventImages'
# [INF] Database migrations applied successfully
# [INF] LankaConnect API started successfully

# 5.3 Watch for ERROR messages
# If you see "column status does not exist" - STOP, issue persists
# If you see "An error occurred while migrating the database" - STOP, new issue

# 5.4 Stop log stream when startup complete (Ctrl+C)
```

**Verification**:
- [ ] No migration errors in logs
- [ ] All Event migrations applied successfully
- [ ] Application started successfully
- [ ] No crash loop detected

---

### Step 6: Verify Database Schema (10 minutes)

**Objective**: Confirm Events table created with correct schema

```bash
# 6.1 Reconnect to staging database
az postgres flexible-server connect \
  --name lankaconnect-staging \
  --admin-user adminuser \
  --database-name lankaconnect

# 6.2 Verify Events table exists
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'events'
ORDER BY table_name;

-- Expected output:
-- table_name
-- -----------
-- event_images
-- events
-- registrations

# 6.3 Verify Events table columns
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
ORDER BY ordinal_position;

-- Expected output (18 columns total):
-- column_name                | data_type              | is_nullable | column_default
-- ----------------------------|------------------------|-------------|----------------
-- Id                         | uuid                   | NO          |
-- title                      | character varying(200) | NO          |
-- description                | character varying(2000)| NO          |
-- StartDate                  | timestamp with tz      | NO          |
-- EndDate                    | timestamp with tz      | NO          |
-- OrganizerId                | uuid                   | NO          |
-- Capacity                   | integer                | NO          |
-- Status                     | character varying(20)  | NO          | 'Draft'::varchar
-- CancellationReason         | character varying(500) | YES         |
-- CreatedAt                  | timestamp with tz      | NO          | NOW()
-- UpdatedAt                  | timestamp with tz      | YES         |
-- Category                   | character varying(20)  | NO          | 'Community'::varchar
-- ticket_price_amount        | numeric(18,2)          | YES         |
-- ticket_price_currency      | character varying(3)   | YES         |
-- has_location               | boolean                | YES         | true
-- address_street             | character varying(200) | YES         |
-- address_city               | character varying(100) | YES         |
-- address_state              | character varying(100) | YES         |
-- address_zip_code           | character varying(20)  | YES         |
-- address_country            | character varying(100) | YES         |
-- coordinates_latitude       | numeric(10,7)          | YES         |
-- coordinates_longitude      | numeric(10,7)          | YES         |
-- location                   | geography(Point,4326)  | YES         | (computed)

# 6.4 Verify Status column exists (critical check)
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
  AND column_name = 'Status';

-- Expected output:
-- column_name
-- ------------
-- Status

# 6.5 Verify migration history complete
SELECT "MigrationId"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Event%'
   OR "MigrationId" LIKE '%AddEventImages%'
   OR "MigrationId" LIKE '%AddEventCategory%'
   OR "MigrationId" LIKE '%AddEventLocation%'
ORDER BY "MigrationId";

-- Expected output:
-- MigrationId
-- -------------------------------------------
-- 20251102061243_AddEventLocationWithPostGIS
-- 20251102144315_AddEventCategoryAndTicketPrice
-- 20251103040053_AddEventImages

# 6.6 Exit psql
\q
```

**Verification**:
- [ ] Events table exists with 22 columns
- [ ] Status column exists (PascalCase)
- [ ] All location columns exist
- [ ] All migration history entries present

---

### Step 7: Verify API Endpoints (15 minutes)

**Objective**: Confirm Event APIs visible in Swagger and functional

```bash
# 7.1 Check health endpoint
curl -X GET \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health \
  -H "Accept: application/json" \
  | jq '.'

# Expected output:
# {
#   "Status": "Healthy",
#   "Checks": [
#     {
#       "Name": "PostgreSQL Database",
#       "Status": "Healthy",
#       "Duration": "00:00:00.123"
#     },
#     {
#       "Name": "Redis Cache",
#       "Status": "Healthy",
#       "Duration": "00:00:00.045"
#     },
#     {
#       "Name": "EF Core DbContext",
#       "Status": "Healthy",
#       "Duration": "00:00:00.234"
#     }
#   ],
#   "TotalDuration": "00:00:00.402"
# }

# 7.2 Check Swagger JSON for Event endpoints
curl -X GET \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json \
  | jq '.paths | keys | map(select(contains("event"))) | length'

# Expected output: 20

# 7.3 List all Event endpoint paths
curl -X GET \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json \
  | jq '.paths | keys | map(select(contains("event")))'

# Expected output (20 paths):
# [
#   "/api/events",                              # POST CreateEvent
#   "/api/events",                              # GET GetAllEvents
#   "/api/events/{id}",                         # GET GetEventById
#   "/api/events/{id}",                         # PUT UpdateEvent
#   "/api/events/{id}",                         # DELETE DeleteEvent
#   "/api/events/{id}/publish",                 # POST PublishEvent
#   "/api/events/{id}/cancel",                  # POST CancelEvent
#   "/api/events/{id}/postpone",                # POST PostponeEvent
#   "/api/events/{id}/archive",                 # POST ArchiveEvent
#   "/api/events/{id}/capacity",                # PUT UpdateEventCapacity
#   "/api/events/{id}/location",                # PUT UpdateEventLocation
#   "/api/events/organizer/{organizerId}",      # GET GetEventsByOrganizer
#   "/api/events/search",                       # GET SearchEvents
#   "/api/events/{id}/registrations",           # GET GetEventRegistrations
#   "/api/events/{id}/rsvp",                    # POST RsvpToEvent
#   "/api/events/{eventId}/rsvp/{userId}",      # PUT UpdateRsvp
#   "/api/events/{eventId}/rsvp/{userId}",      # DELETE CancelRsvp
#   "/api/events/{id}/images",                  # POST UploadEventImage
#   "/api/events/{id}/images/{imageId}",        # DELETE DeleteEventImage
#   "/api/events/{id}/images/reorder"           # PUT ReorderEventImages
# ]

# 7.4 Test Event creation (without auth)
curl -X POST \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Event",
    "description": "Testing Event API after fix",
    "startDate": "2025-12-01T18:00:00Z",
    "endDate": "2025-12-01T21:00:00Z",
    "capacity": 100,
    "category": "Community",
    "ticketPrice": {
      "amount": 0,
      "currency": "USD"
    }
  }'

# Expected output:
# 401 Unauthorized (authentication required)
# OR
# 400 Bad Request with validation errors

# 7.5 Verify Swagger UI accessible
# Open browser: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io
# Expected: Swagger UI loads with "LankaConnect API" title
# Expected: "Events" section visible with 20 endpoints
```

**Verification**:
- [ ] Health check returns Healthy
- [ ] Swagger JSON contains 20 Event endpoints
- [ ] Swagger UI shows Events section
- [ ] Event creation endpoint responds (401 or 400 expected without auth)

---

### Step 8: Integration Testing (20 minutes)

**Objective**: Verify Event CRUD operations work end-to-end

**Note**: This requires authentication. Skip if you don't have test user credentials.

```bash
# 8.1 Obtain JWT token (use your test user credentials)
TOKEN=$(curl -X POST \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@lankaconnect.com",
    "password": "YourTestPassword123!",
    "ipAddress": "127.0.0.1"
  }' \
  | jq -r '.token')

echo "Token: $TOKEN"

# 8.2 Create test event
EVENT_ID=$(curl -X POST \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Integration Test Event",
    "description": "Testing Event API after emergency fix",
    "startDate": "2025-12-15T18:00:00Z",
    "endDate": "2025-12-15T21:00:00Z",
    "capacity": 50,
    "category": "Community",
    "ticketPrice": {
      "amount": 10.00,
      "currency": "USD"
    },
    "location": {
      "address": {
        "street": "123 Main St",
        "city": "Los Angeles",
        "state": "CA",
        "zipCode": "90001",
        "country": "USA"
      },
      "coordinates": {
        "latitude": 34.0522,
        "longitude": -118.2437
      }
    }
  }' \
  | jq -r '.id')

echo "Created Event ID: $EVENT_ID"

# 8.3 Retrieve event
curl -X GET \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$EVENT_ID \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.'

# Expected output: Event details with all fields populated

# 8.4 Update event
curl -X PUT \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$EVENT_ID \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "UPDATED Integration Test Event",
    "description": "Updated description after emergency fix",
    "startDate": "2025-12-15T19:00:00Z",
    "endDate": "2025-12-15T22:00:00Z",
    "capacity": 75
  }' \
  | jq '.'

# Expected output: Updated event details

# 8.5 Publish event (change status from Draft to Published)
curl -X POST \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$EVENT_ID/publish \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.'

# Expected output: Event with status = "Published"

# 8.6 Search events
curl -X GET \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/search?query=Integration&city=Los+Angeles&radius=25" \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.'

# Expected output: Array containing our test event

# 8.7 Delete event
curl -X DELETE \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$EVENT_ID \
  -H "Authorization: Bearer $TOKEN"

# Expected output: 204 No Content
```

**Verification**:
- [ ] Event created successfully (returns event ID)
- [ ] Event retrieved successfully (all fields populated)
- [ ] Event updated successfully (changes persisted)
- [ ] Event published successfully (status changed to Published)
- [ ] Event search works (finds event by city/radius)
- [ ] Event deleted successfully (204 response)

---

### Step 9: Final Verification Checklist (10 minutes)

**Objective**: Confirm all success criteria met

```
Database:
- [ ] Events schema exists
- [ ] Events table has 22 columns
- [ ] Status column exists (PascalCase)
- [ ] All 3 Event migrations applied
- [ ] No orphaned migration history entries

Application:
- [ ] Container starts successfully (no crash loop)
- [ ] No migration errors in logs
- [ ] Health check returns Healthy
- [ ] Swagger UI loads correctly

API:
- [ ] Swagger shows 20 Event endpoints
- [ ] Event creation endpoint accessible (401 without auth)
- [ ] Event CRUD operations work with valid auth
- [ ] Event search/filter operations work
- [ ] PostGIS location queries work

Monitoring:
- [ ] Container logs show successful startup
- [ ] No error alerts in Application Insights
- [ ] Database connection count normal
- [ ] API response times < 500ms
```

---

## Phase 2: Code Fix (Next 4 hours)

### Step 10: Fix Migration Column Name Bug

**File**: `src/LankaConnect.Infrastructure/Migrations/20251102061243_AddEventLocationWithPostGIS.cs`

**Current Code** (Line 116-120):
```csharp
// Add composite index for common filtered queries
migrationBuilder.Sql(@"
    CREATE INDEX ix_events_status_city_startdate
    ON events.events (status, address_city, start_date)
    WHERE address_city IS NOT NULL;
");
```

**Corrected Code**:
```csharp
// Add composite index for common filtered queries
migrationBuilder.Sql(@"
    CREATE INDEX ix_events_status_city_startdate
    ON events.events (""Status"", address_city, ""StartDate"")
    WHERE address_city IS NOT NULL;
");
```

**Explanation**:
- PostgreSQL column names are case-insensitive UNLESS quoted
- InitialCreate migration created columns: `Status` (PascalCase), `StartDate` (PascalCase)
- Raw SQL must use quoted identifiers to match exact case: `"Status"`, `"StartDate"`

---

### Step 11: Standardize Naming Convention

**Objective**: Prevent future case-sensitivity bugs

**Option A**: Enforce snake_case globally (Recommended)

**File**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

Add this method:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    // Enforce PostgreSQL snake_case naming convention for all tables and columns
    configurationBuilder.Conventions.Add(_ => new AttributeToTableConvention());

    // Configure table name convention
    configurationBuilder.Properties<string>()
        .HaveMaxLength(256); // Default max length for strings

    base.ConfigureConventions(configurationBuilder);
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Apply snake_case naming convention to all entities
    foreach (var entity in modelBuilder.Model.GetEntityTypes())
    {
        // Convert table names to snake_case
        entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

        // Convert column names to snake_case
        foreach (var property in entity.GetProperties())
        {
            property.SetColumnName(property.GetColumnName().ToSnakeCase());
        }

        // Convert index names to snake_case
        foreach (var key in entity.GetKeys())
        {
            key.SetName(key.GetName()?.ToSnakeCase());
        }

        foreach (var index in entity.GetIndexes())
        {
            index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
        }
    }
}
```

**File**: `src/LankaConnect.Infrastructure/Extensions/StringExtensions.cs` (new file)

```csharp
namespace LankaConnect.Infrastructure.Extensions;

public static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        result.Append(char.ToLowerInvariant(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('_');
                result.Append(char.ToLowerInvariant(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
```

**Option B**: Keep PascalCase but quote all identifiers (Not recommended)

Update all raw SQL migrations to use double-quoted identifiers:
- `"Status"` instead of `status`
- `"StartDate"` instead of `start_date`
- etc.

---

### Step 12: Add Migration Validation to CI/CD

**File**: `.github/workflows/deploy-staging.yml`

Add this step after "Build application" (line 36):

```yaml
- name: Validate Database Migrations
  run: |
    echo "Validating EF Core migrations..."

    # Generate idempotent migration script
    dotnet ef migrations script \
      --project src/LankaConnect.Infrastructure \
      --startup-project src/LankaConnect.API \
      --idempotent \
      --output migration-script.sql

    echo "Migration script generated successfully"

    # Validate no unquoted case-sensitive column names
    echo "Checking for case-sensitive column references..."

    # Check for common column names that should be quoted
    if grep -iE '\b(status|start_date|end_date|created_at|updated_at)\b' migration-script.sql | grep -v '"'; then
      echo "ERROR: Unquoted case-sensitive column names detected"
      echo "PostgreSQL column names are case-sensitive. Use quoted identifiers."
      exit 1
    fi

    # Check for DROP statements (safety check)
    if grep -iE 'DROP (TABLE|SCHEMA|DATABASE)' migration-script.sql; then
      echo "WARNING: Destructive DROP statement detected in migration"
      echo "Please review migration carefully before deploying"
      # Uncomment to fail build on DROP statements
      # exit 1
    fi

    echo "Migration validation passed ✅"
```

---

### Step 13: Add Database Schema Health Check

**File**: `src/LankaConnect.API/HealthChecks/DatabaseSchemaHealthCheck.cs` (new file)

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LankaConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.API.HealthChecks;

public class DatabaseSchemaHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseSchemaHealthCheck> _logger;

    public DatabaseSchemaHealthCheck(AppDbContext context, ILogger<DatabaseSchemaHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify Events table has Status column
            var hasStatusColumn = await _context.Database.ExecuteSqlRawAsync(
                @"SELECT 1
                  FROM information_schema.columns
                  WHERE table_schema = 'events'
                    AND table_name = 'events'
                    AND column_name = 'Status'",
                cancellationToken);

            if (hasStatusColumn == 0)
            {
                _logger.LogError("Events table missing Status column - schema mismatch detected");
                return HealthCheckResult.Unhealthy(
                    "Events schema validation failed: Status column not found");
            }

            // Verify all required Event columns exist
            var requiredColumns = new[] {
                "Id", "title", "description", "StartDate", "EndDate",
                "OrganizerId", "Capacity", "Status", "CreatedAt"
            };

            foreach (var column in requiredColumns)
            {
                var exists = await _context.Database.ExecuteSqlRawAsync(
                    $@"SELECT 1
                       FROM information_schema.columns
                       WHERE table_schema = 'events'
                         AND table_name = 'events'
                         AND column_name = '{column}'",
                    cancellationToken);

                if (exists == 0)
                {
                    _logger.LogError($"Events table missing required column: {column}");
                    return HealthCheckResult.Unhealthy(
                        $"Events schema validation failed: {column} column not found");
                }
            }

            _logger.LogDebug("Database schema validation passed");
            return HealthCheckResult.Healthy("Events schema validated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database schema validation failed with exception");
            return HealthCheckResult.Unhealthy(
                "Events schema validation failed",
                ex);
        }
    }
}
```

**File**: `src/LankaConnect.API/Program.cs`

Add after existing health checks (line 146):

```csharp
.AddCheck<DatabaseSchemaHealthCheck>(
    name: "Database Schema Validation",
    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
    tags: new[] { "db", "schema", "ready" });
```

---

## Rollback Procedure

If something goes wrong during the emergency fix:

### Rollback Step 1: Restore Database Backup

```bash
# 1. Reconnect to staging database
az postgres flexible-server connect \
  --name lankaconnect-staging \
  --admin-user adminuser \
  --database-name lankaconnect

# 2. Restore migration history
\copy "__EFMigrationsHistory" FROM 'migration-history-backup.csv' WITH CSV HEADER;

# 3. Restore Events data (if backup exists)
CREATE SCHEMA IF NOT EXISTS events;
-- Recreate table structure from backup schema
\copy events.events FROM 'events-backup.csv' WITH CSV HEADER;

# 4. Exit
\q
```

### Rollback Step 2: Redeploy Previous Version

```bash
# Get previous successful deployment commit
PREVIOUS_COMMIT=$(gh run list \
  --workflow=deploy-staging.yml \
  --status=success \
  --limit=2 \
  --json headSha \
  --jq '.[1].headSha')

echo "Rolling back to commit: $PREVIOUS_COMMIT"

# Trigger deployment of previous commit
gh workflow run deploy-staging.yml --ref $PREVIOUS_COMMIT
```

---

## Success Criteria

Emergency fix is successful when:

1. **Database**:
   - Events schema exists
   - Events table has all required columns
   - Status column exists (correct case)
   - All migrations applied successfully

2. **Application**:
   - Container starts without errors
   - No migration errors in logs
   - Health check returns Healthy status

3. **API**:
   - Swagger UI shows 20 Event endpoints
   - Event CRUD operations work correctly
   - Location-based queries work

4. **Monitoring**:
   - No error alerts
   - Response times normal
   - Database connections stable

---

## Next Steps After Emergency Fix

1. Monitor staging for 24 hours for any issues
2. Implement Phase 2 code fixes (column name standardization)
3. Add migration validation to CI/CD pipeline
4. Implement database schema health check
5. Update team documentation with lessons learned
6. Schedule team training on migration best practices
7. Plan production deployment (ONLY after 48+ hours of stable staging)

---

## Support Contacts

- **Database Issues**: DBA Team (dba@lankaconnect.com)
- **DevOps Issues**: DevOps Team (devops@lankaconnect.com)
- **Application Issues**: Backend Team (backend@lankaconnect.com)
- **Emergency Escalation**: On-Call Engineer (oncall@lankaconnect.com)
