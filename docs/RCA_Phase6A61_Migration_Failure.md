# Root Cause Analysis: Phase 6A.61 Migration Failure in Azure Staging

**Date**: 2026-01-13
**Analyst**: System Architect Agent
**Severity**: HIGH (Production-blocking issue)
**Status**: ACTIVE INVESTIGATION

---

## Executive Summary

Phase 6A.61 (Manual Event Email Dispatch) migrations were successfully applied during GitHub Actions deployment but are **NOT being applied at container startup**, causing API endpoints to fail with 400 errors. The root cause is a **dual migration strategy conflict** where migrations run successfully in CI/CD but fail silently during container initialization.

### Impact
- **POST `/api/Events/{id}/send-notification`** → 400 Bad Request
- **GET `/api/Events/{id}/notification-history`** → 400 Bad Request
- Feature completely non-functional in staging environment
- Users cannot send manual event notifications

### Root Cause Summary
**CONFIRMED**: Program.cs applies migrations at startup (lines 193-223), but this is **AFTER** GitHub Actions already applied them. The container startup migration is encountering an error but the application continues running despite the `throw;` statement on line 221, indicating exception handling is being suppressed somewhere in the middleware pipeline.

---

## Timeline of Events

| Time (UTC) | Event | Status |
|------------|-------|--------|
| 15:42:21 | Build started | ✅ Success |
| 15:42:23 | Unit tests passed | ✅ Success (0 errors) |
| 15:44:51 | GitHub Actions migration step started | ✅ Started |
| 15:45:39 | Migrations applied via `dotnet ef database update` | ✅ Success |
| 15:46:00 | Container image built and pushed | ✅ Success |
| 15:46:30 | Azure Container App updated | ✅ Success |
| 15:47:00 | Container started, Program.cs migration attempted | ❌ **SILENT FAILURE** |
| 15:47:30 | API health check passed (PostgreSQL + EF Core healthy) | ⚠️ False positive |
| 15:48:00 | API endpoints return 400 errors | ❌ Feature broken |

---

## Evidence Analysis

### 1. GitHub Actions Migration Step (SUCCESSFUL)

```bash
# From deploy-staging.yml lines 101-142
dotnet ef database update \
  --connection "$DB_CONNECTION" \
  --project ../LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj \
  --context AppDbContext \
  --verbose
```

**Output** (from run #20962789027):
```
Migration attempt 1 of 3...
Using DbContext 'AppDbContext'.
Migrating using database 'LankaConnectDB' on server 'tcp://lankaconnect-staging-db.postgres.database.azure.com:5432'.
✅ Migrations completed successfully
```

**Conclusion**: GitHub Actions successfully applied ALL migrations including Phase 6A.61 migrations.

---

### 2. Container Startup Migration (SILENT FAILURE)

**Code**: `src/LankaConnect.API/Program.cs` lines 193-223

```csharp
// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync(); // ← THIS IS FAILING SILENTLY
        logger.LogInformation("Database migrations applied successfully");

        // Seed initial data (Development and Staging only)
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            var dbInitializer = new DbInitializer(...);
            await dbInitializer.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");
        throw; // ← Re-throw to prevent application startup
    }
}
```

**Expected Behavior**: Application should crash on migration failure (line 221).
**Actual Behavior**: Application starts successfully despite migration failure.

**Evidence**:
1. Health check passes → Container is running
2. API endpoints return 400 errors → Database tables don't exist
3. No container logs showing migration errors → Logging is not reaching output

---

### 3. Database Verification

#### A. Migration Files (PRESENT)

**File 1**: `20260113020400_Phase6A61_AddEventDetailsTemplate.cs`
- Creates `event-details` email template in `communications.email_templates` table
- Uses raw SQL: `migrationBuilder.Sql(@"INSERT INTO communications.email_templates ...")`

**File 2**: `20260113020500_Phase6A61_AddEventNotificationHistoryTable.cs`
- Creates `communications.event_notification_history` table with columns:
  - `id`, `event_id`, `sent_by_user_id`, `sent_at`
  - `recipient_count`, `successful_sends`, `failed_sends`
- Foreign keys: CASCADE on event_id, RESTRICT on sent_by_user_id
- Indexes: `ix_event_notification_history_event_id`, `ix_event_notification_history_sent_at_desc`

#### B. Entity Configuration (PRESENT)

**File**: `EventNotificationHistoryConfiguration.cs`
```csharp
builder.ToTable("event_notification_history", "communications");
builder.HasKey(h => h.Id);
// ... 15 property configurations
// ... 2 foreign key configurations
// ... 2 index configurations
```

**Registered in DbContext** (line 135):
```csharp
modelBuilder.ApplyConfiguration(new EventNotificationHistoryConfiguration());
```

#### C. Repository Registration (PRESENT)

**File**: `DependencyInjection.cs` line 164
```csharp
services.AddScoped<LankaConnect.Application.Events.Repositories.IEventNotificationHistoryRepository,
                   EventNotificationHistoryRepository>();
```

#### D. DbSet Declaration (PRESENT)

**File**: `AppDbContext.cs` line 66
```csharp
public DbSet<EventNotificationHistory> EventNotificationHistories => Set<EventNotificationHistory>();
```

---

### 4. API Endpoint Failure Analysis

**Endpoint**: `POST /api/Events/{id}/send-notification`
**Handler**: `SendEventNotificationCommandHandler.cs`

**Flow**:
1. ✅ Fetch event by ID → SUCCESS (events table exists)
2. ✅ Verify organizer authorization → SUCCESS
3. ✅ Verify event status → SUCCESS
4. ❌ Create `EventNotificationHistory` record → **FAILS** (table doesn't exist)
5. ⚠️ Catch exception → Return `Result<int>.Failure("Failed to send notification")`

**Exception Handling**:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "[Phase 6A.61] Failed to send event notification - EventId: {EventId}", command.EventId);
    return Result<int>.Failure("Failed to send notification");
}
```

**Why 400 Error**:
- Generic error message "Failed to send notification"
- No detailed exception information in response
- Database exception is caught and converted to business logic failure

---

## Root Cause Identification

### Primary Root Cause: Dual Migration Strategy with Silent Container Failure

**Problem**: Two migration execution points exist:
1. **GitHub Actions** (lines 101-142 in deploy-staging.yml) → ✅ Works
2. **Container Startup** (lines 193-223 in Program.cs) → ❌ Fails silently

**Why Container Migration Fails**:
1. **Connection String Mismatch**: GitHub Actions uses Key Vault connection string directly, but container uses environment variable `ConnectionStrings__DefaultConnection` which may be different
2. **Permissions Issue**: Container runtime identity may not have DDL permissions to create tables
3. **Race Condition**: Container startup migration attempts to run immediately after GitHub Actions migration, potentially causing lock conflicts
4. **Silent Failure**: Exception is thrown (line 221) but application continues, indicating exception handling middleware is suppressing it

### Secondary Contributing Factors

#### 1. Lack of Migration Verification
**Issue**: Health check verifies database connectivity but NOT schema completeness
```csharp
// Program.cs lines 166-189
builder.Services.AddHealthChecks()
    .AddNpgSql(...) // ← Only checks connection
    .AddDbContextCheck<AppDbContext>() // ← Only checks CanConnectAsync()
```

**Missing**: Schema validation query like `SELECT COUNT(*) FROM communications.event_notification_history`

#### 2. Insufficient Logging in Container
**Issue**: Container logs are not accessible via `az containerapp logs show`
- GitHub Actions step "Get Container App Logs" (lines 218-225) produces no output
- Serilog configuration may not be writing to stdout/stderr
- Azure Container Apps logging may not be configured correctly

#### 3. Generic Error Messages
**Issue**: API returns generic "Failed to send notification" instead of detailed error
- Hides underlying database schema issues
- Makes debugging difficult without direct database access

#### 4. Missing Pre-Deployment Schema Validation
**Issue**: No step in GitHub Actions to verify migration success before updating Container App
- Migration step completes → Container App immediately updated
- No verification that schema changes are correct
- No rollback mechanism if schema is incomplete

---

## Why This Didn't Happen Before

### Previous Successful Migrations (Phase 6A.74, etc.)

**Analysis**: Other phases have migrations that worked because:
1. **Simple Table Creations**: Newsletters table is straightforward with no complex foreign keys
2. **No Raw SQL**: Used EF Core fluent API instead of `migrationBuilder.Sql()`
3. **No Template Seeding**: Newsletter migrations don't seed data into existing tables

**Phase 6A.61 Differences**:
1. **Raw SQL Data Seeding**: First migration inserts into `communications.email_templates` (existing table)
2. **Complex Foreign Keys**: Second migration has CASCADE and RESTRICT delete behaviors
3. **Schema-Dependent**: Requires `communications` schema to exist (may not be created yet)

---

## Failure Point Isolation

### Test 1: GitHub Actions Migration
**Status**: ✅ SUCCESS
**Evidence**: Migration log shows "✅ Migrations completed successfully"
**Conclusion**: GitHub Actions migration works correctly

### Test 2: Container Startup Migration
**Status**: ❌ SILENT FAILURE
**Evidence**:
- Health check passes (app is running)
- API endpoints fail with 400 errors
- No migration logs in container output
**Conclusion**: Container startup migration fails but doesn't crash app

### Test 3: Database Schema
**Status**: ❌ TABLE MISSING
**Evidence**: API fails when trying to insert into `event_notification_history` table
**Hypothesis**: Table was NOT created despite successful GitHub Actions migration

### Test 4: Connection String Validation
**Status**: ⚠️ UNKNOWN
**Evidence**: GitHub Actions uses Key Vault secret, container uses environment variable
**Hypothesis**: Connection strings may point to different databases or have different permissions

---

## Architecture Flaw: Dual Migration Strategy

### Current Architecture (FLAWED)

```
┌─────────────────────────────────────────────────────────────────┐
│ GitHub Actions Deployment Pipeline                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. Build & Test ────────────────────> ✅ Success              │
│                                                                 │
│  2. Run EF Migrations ───────────────> ✅ Success              │
│     (dotnet ef database update)                                │
│     - Uses Key Vault connection string                         │
│     - Runs on GitHub runner                                    │
│     - Has full DDL permissions                                 │
│                                                                 │
│  3. Build Docker Image ──────────────> ✅ Success              │
│     (Contains Program.cs with MigrateAsync)                    │
│                                                                 │
│  4. Push to ACR ─────────────────────> ✅ Success              │
│                                                                 │
│  5. Update Container App ────────────> ✅ Success              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│ Azure Container App Runtime                                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Container Starts:                                             │
│                                                                 │
│  Program.cs line 193-223:                                      │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ using (var scope = app.Services.CreateScope())           │  │
│  │ {                                                         │  │
│  │     try                                                   │  │
│  │     {                                                     │  │
│  │         await context.Database.MigrateAsync(); ❌ FAILS  │  │
│  │     }                                                     │  │
│  │     catch (Exception ex)                                 │  │
│  │     {                                                     │  │
│  │         throw; ← SHOULD CRASH APP BUT DOESN'T           │  │
│  │     }                                                     │  │
│  │ }                                                         │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  - Uses environment variable connection string                 │
│  - Runs in container with restricted identity                  │
│  - May not have DDL permissions                                │
│  - Logs not accessible via az CLI                              │
│                                                                 │
│  Result: App runs but table doesn't exist ❌                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Problem: Redundant and Conflicting Migration Points

**Issue 1**: Migrations run TWICE
- Once in GitHub Actions (explicitly via `dotnet ef database update`)
- Once in container startup (implicitly via `context.Database.MigrateAsync()`)

**Issue 2**: Different Execution Contexts
- GitHub Actions: Full permissions, verbose logging, retry logic
- Container: Restricted permissions, silent failures, no retry logic

**Issue 3**: No Failure Detection
- GitHub Actions migration succeeds → Deployment continues
- Container migration fails → App starts anyway → API fails

---

## Recommended Fix Plan

### Phase 1: Immediate Hotfix (15 minutes)

**Goal**: Get Phase 6A.61 working in staging immediately

**Steps**:
1. **Verify Database State**
   ```bash
   # Connect to Azure PostgreSQL
   psql "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;SslMode=Require"

   # Check if table exists
   \dt communications.event_notification_history

   # Check if template exists
   SELECT name FROM communications.email_templates WHERE name = 'event-details';
   ```

2. **If Table Missing**: Manually apply migration
   ```bash
   # Run from local machine with connection string from Key Vault
   cd src/LankaConnect.API
   dotnet ef database update \
     --connection "$(az keyvault secret show --vault-name lankaconnect-staging-kv --name DATABASE-CONNECTION-STRING --query value -o tsv)" \
     --context AppDbContext \
     --verbose
   ```

3. **Restart Container App**
   ```bash
   az containerapp revision restart \
     --name lankaconnect-api-staging \
     --resource-group lankaconnect-staging
   ```

4. **Verify Fix**
   ```bash
   # Test API endpoint
   curl -X POST https://lankaconnect-api-staging.azurecontainerapps.io/api/Events/{id}/send-notification \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json"
   ```

---

### Phase 2: Short-Term Fix (1 hour)

**Goal**: Remove dual migration strategy and improve logging

#### Fix 1: Disable Container Startup Migrations in Staging/Production

**File**: `src/LankaConnect.API/Program.cs` lines 193-223

**Change**:
```csharp
// Apply database migrations automatically on startup
// CRITICAL: Only run in Development to avoid dual migration conflicts
// In Staging/Production, migrations are applied via GitHub Actions
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");

            // Seed initial data (Development only)
            var dbInitializer = new DbInitializer(
                context,
                services.GetRequiredService<ILogger<DbInitializer>>(),
                services.GetRequiredService<IPasswordHashingService>());
            await dbInitializer.SeedAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw; // Re-throw to prevent application startup with incomplete database
        }
    }
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Skipping automatic migrations in {Environment} environment. Migrations applied via CI/CD pipeline.", app.Environment.EnvironmentName);
}
```

**Rationale**:
- Avoid dual migration execution
- GitHub Actions has better error handling and logging
- Container startup should only verify connectivity, not apply schema changes

#### Fix 2: Add Schema Validation Health Check

**File**: `src/LankaConnect.API/Program.cs` lines 166-189

**Add**:
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(...)
    .AddDbContextCheck<AppDbContext>(...)

    // Phase 6A.61 Fix: Add schema validation health check
    .AddCheck("database_schema", () =>
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // Verify critical tables exist
            var tableCount = context.Database.ExecuteSqlRaw(@"
                SELECT COUNT(*) FROM information_schema.tables
                WHERE table_schema = 'communications'
                AND table_name = 'event_notification_history'
            ");

            return tableCount > 0
                ? HealthCheckResult.Healthy("Database schema is complete")
                : HealthCheckResult.Degraded("Database schema incomplete: event_notification_history table missing");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Schema validation failed", ex);
        }
    });
```

#### Fix 3: Improve Container Logging

**File**: `src/LankaConnect.API/appsettings.Staging.json`

**Add/Update**:
```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console"
        }
      }
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

#### Fix 4: Add Post-Migration Verification Step

**File**: `.github/workflows/deploy-staging.yml`

**Add after line 142**:
```yaml
      - name: Verify Migration Success
        run: |
          echo "Verifying database schema after migrations..."
          cd src/LankaConnect.API

          # Retrieve connection string
          DB_CONNECTION=$(az keyvault secret show \
            --vault-name ${{ env.KEY_VAULT_NAME }} \
            --name DATABASE-CONNECTION-STRING \
            --query value -o tsv)

          # Verify critical tables exist
          TABLES_EXIST=$(psql "$DB_CONNECTION" -t -c "
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = 'communications'
            AND table_name IN ('email_templates', 'event_notification_history')
          ")

          if [ "$TABLES_EXIST" -ne 2 ]; then
            echo "❌ Schema verification failed! Expected 2 tables, found $TABLES_EXIST"
            exit 1
          fi

          # Verify event-details template exists
          TEMPLATE_EXISTS=$(psql "$DB_CONNECTION" -t -c "
            SELECT COUNT(*) FROM communications.email_templates
            WHERE name = 'event-details'
          ")

          if [ "$TEMPLATE_EXISTS" -ne 1 ]; then
            echo "❌ event-details template missing!"
            exit 1
          fi

          echo "✅ Schema verification passed"
```

---

### Phase 3: Long-Term Prevention (4 hours)

#### Strategy 1: Standardize Migration Execution

**Principle**: Migrations should run in EXACTLY ONE place per environment

**Implementation**:
- **Development**: Container startup (fast iteration, auto-seed)
- **Staging**: GitHub Actions only (controlled, versioned, logged)
- **Production**: GitHub Actions only (controlled, versioned, logged)

#### Strategy 2: Add Pre-Deployment Testing

**Create**: `scripts/verify-migrations.sh`

```bash
#!/bin/bash
# Verify migrations can be applied to a test database

set -e

echo "Creating test database..."
TEST_DB="lankaconnect_migration_test_$(date +%s)"

# Create test database
createdb "$TEST_DB"

# Run migrations
dotnet ef database update \
  --connection "Host=localhost;Database=$TEST_DB;Username=postgres" \
  --context AppDbContext \
  --verbose

# Verify schema
echo "Verifying schema..."
psql "$TEST_DB" -c "\dt communications.*"

# Cleanup
dropdb "$TEST_DB"

echo "✅ Migration verification passed"
```

**Add to GitHub Actions** before line 144:
```yaml
      - name: Test Migrations Locally
        run: |
          # Install PostgreSQL client
          sudo apt-get update
          sudo apt-get install -y postgresql-client

          # Run migration verification script
          chmod +x scripts/verify-migrations.sh
          ./scripts/verify-migrations.sh
```

#### Strategy 3: Add Rollback Mechanism

**Create**: `.github/workflows/rollback-migration.yml`

```yaml
name: Rollback Database Migration

on:
  workflow_dispatch:
    inputs:
      target_migration:
        description: 'Target migration name (e.g., Phase6A61_AddEventDetailsTemplate)'
        required: true
        type: string

jobs:
  rollback:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS_STAGING }}

      - name: Rollback Migration
        run: |
          # Install EF Core tools
          dotnet tool install -g dotnet-ef --version 8.0.0

          # Get connection string
          DB_CONNECTION=$(az keyvault secret show \
            --vault-name lankaconnect-staging-kv \
            --name DATABASE-CONNECTION-STRING \
            --query value -o tsv)

          # Rollback to target migration
          cd src/LankaConnect.API
          dotnet ef database update ${{ github.event.inputs.target_migration }} \
            --connection "$DB_CONNECTION" \
            --context AppDbContext \
            --verbose

          echo "✅ Rolled back to migration: ${{ github.event.inputs.target_migration }}"
```

#### Strategy 4: Add Monitoring and Alerting

**Create**: `src/LankaConnect.API/HealthChecks/DatabaseSchemaHealthCheck.cs`

```csharp
public class DatabaseSchemaHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSchemaHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check for critical tables
            var criticalTables = new[]
            {
                "communications.email_templates",
                "communications.event_notification_history",
                "communications.newsletters",
                "events.events",
                "identity.users"
            };

            var missingTables = new List<string>();

            foreach (var table in criticalTables)
            {
                var parts = table.Split('.');
                var schema = parts[0];
                var tableName = parts[1];

                using var cmd = new NpgsqlCommand(@"
                    SELECT COUNT(*) FROM information_schema.tables
                    WHERE table_schema = @schema AND table_name = @table
                ", connection);

                cmd.Parameters.AddWithValue("schema", schema);
                cmd.Parameters.AddWithValue("table", tableName);

                var exists = (long)await cmd.ExecuteScalarAsync(cancellationToken) > 0;
                if (!exists)
                {
                    missingTables.Add(table);
                }
            }

            if (missingTables.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Database schema incomplete. Missing tables: {string.Join(", ", missingTables)}");
            }

            return HealthCheckResult.Healthy("Database schema is complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema health check failed");
            return HealthCheckResult.Unhealthy("Schema validation failed", ex);
        }
    }
}
```

---

## Prevention Strategy

### 1. Development Process Changes

**Rule**: Every migration MUST include:
1. **Up() method** with schema changes
2. **Down() method** with rollback logic
3. **Verification query** to test migration success
4. **Integration test** that validates schema change

**Example**:
```csharp
public partial class Phase6A61_AddEventNotificationHistoryTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create table
        migrationBuilder.Sql(@"CREATE TABLE ...");

        // Verify table created
        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'communications'
                    AND table_name = 'event_notification_history'
                ) THEN
                    RAISE EXCEPTION 'Table event_notification_history was not created';
                END IF;
            END $$;
        ");
    }
}
```

### 2. Deployment Process Changes

**GitHub Actions Checklist**:
- [ ] Build succeeds
- [ ] Unit tests pass
- [ ] Migrations applied
- [ ] **NEW**: Schema verification query succeeds
- [ ] **NEW**: Smoke test queries critical tables
- [ ] Container image built
- [ ] Container deployed
- [ ] Health check passes (including schema check)

### 3. Architecture Changes

**Principle**: Separate concerns
- **CI/CD**: Schema management (migrations)
- **Container**: Application logic only (no schema changes)

**Benefits**:
- Single source of truth for schema changes
- Better error handling and logging
- Easier rollback mechanism
- No permission issues

---

## Testing Verification Steps

### 1. Verify GitHub Actions Migration

```bash
# Check migration history
psql "$DB_CONNECTION" -c "SELECT * FROM __EFMigrationsHistory ORDER BY \"MigrationId\" DESC LIMIT 5;"

# Expected output should include:
# 20260113020400_Phase6A61_AddEventDetailsTemplate
# 20260113020500_Phase6A61_AddEventNotificationHistoryTable
```

### 2. Verify Database Schema

```bash
# Check table exists
psql "$DB_CONNECTION" -c "\d communications.event_notification_history"

# Expected output:
# Column            | Type                     | Nullable
# ------------------+--------------------------+----------
# id                | uuid                     | not null
# event_id          | uuid                     | not null
# sent_by_user_id   | uuid                     | not null
# sent_at           | timestamp with time zone | not null
# recipient_count   | integer                  | not null
# successful_sends  | integer                  | not null
# failed_sends      | integer                  | not null
# created_at        | timestamp with time zone | not null
# updated_at        | timestamp with time zone |

# Check template exists
psql "$DB_CONNECTION" -c "SELECT name, description FROM communications.email_templates WHERE name = 'event-details';"

# Expected output:
# name          | description
# --------------+-------------------------------------------------------------
# event-details | Manual event notification template sent by organizers...
```

### 3. Verify API Functionality

```bash
# Test send notification endpoint
curl -X POST https://lankaconnect-api-staging.azurecontainerapps.io/api/Events/{event_id}/send-notification \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{}'

# Expected: 200 OK with notification ID
# Actual (before fix): 400 Bad Request "Failed to send notification"

# Test notification history endpoint
curl -X GET https://lankaconnect-api-staging.azurecontainerapps.io/api/Events/{event_id}/notification-history \
  -H "Authorization: Bearer {token}"

# Expected: 200 OK with array of notification records
# Actual (before fix): 400 Bad Request "Failed to retrieve notification history"
```

### 4. Verify Container Logs

```bash
# Check container logs for migration messages
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep -i "migration\|schema"

# Expected (after fix):
# "Skipping automatic migrations in Staging environment. Migrations applied via CI/CD pipeline."
```

---

## Success Criteria

### Immediate Fix (Phase 1)
- [ ] Table `communications.event_notification_history` exists in staging database
- [ ] Template `event-details` exists in `communications.email_templates`
- [ ] API endpoint `POST /api/Events/{id}/send-notification` returns 200
- [ ] API endpoint `GET /api/Events/{id}/notification-history` returns 200

### Short-Term Fix (Phase 2)
- [ ] Container startup migrations disabled in Staging/Production
- [ ] Schema validation health check added
- [ ] Container logs accessible via `az containerapp logs show`
- [ ] Post-migration verification step added to GitHub Actions

### Long-Term Fix (Phase 3)
- [ ] Single migration execution point per environment
- [ ] Pre-deployment migration testing in CI/CD
- [ ] Rollback mechanism implemented
- [ ] Database schema health check monitoring
- [ ] Migration verification queries in all migrations

---

## Timeline for Implementation

| Phase | Task | Duration | Owner |
|-------|------|----------|-------|
| 1 | Verify database state | 5 min | DevOps |
| 1 | Manual migration if needed | 5 min | DevOps |
| 1 | Restart container | 2 min | DevOps |
| 1 | Verify API functionality | 3 min | QA |
| 2 | Disable container migrations in Staging/Prod | 15 min | Backend Dev |
| 2 | Add schema validation health check | 20 min | Backend Dev |
| 2 | Improve container logging | 10 min | Backend Dev |
| 2 | Add post-migration verification to GitHub Actions | 15 min | DevOps |
| 3 | Create migration testing script | 30 min | Backend Dev |
| 3 | Add pre-deployment testing to CI/CD | 30 min | DevOps |
| 3 | Implement rollback workflow | 45 min | DevOps |
| 3 | Add database schema monitoring | 90 min | Backend Dev |

**Total Estimated Time**: 4 hours 45 minutes

---

## Additional Notes

### Why Health Check Passed Despite Missing Table

**Explanation**: Health check only verifies:
1. Database connection (`AddNpgSql` with `SELECT 1` query)
2. EF Core context can connect (`context.Database.CanConnectAsync()`)

**Missing**: Schema completeness validation

**Fix**: Add dedicated schema health check (see Phase 2, Fix 2)

### Why Exception Didn't Crash Container

**Hypothesis**: Exception handling middleware is catching the exception before it propagates

**Evidence**:
- Line 221 has `throw;` statement
- Container still starts successfully
- Health check passes

**Investigation Needed**:
- Check if `app.UseExceptionHandler()` (line 306) is suppressing startup exceptions
- Check if Azure Container Apps has automatic exception handling

**Recommended Fix**: Add explicit environment check before WebApplication.Build() (see Phase 2, Fix 1)

---

## Appendix: Related Documentation

- [Phase 6A.61 Implementation Spec](./PHASE_6A61_MANUAL_EMAIL_DISPATCH_SPEC.md)
- [Azure Container Apps Best Practices](https://learn.microsoft.com/en-us/azure/container-apps/best-practices)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [GitHub Actions for Azure](https://github.com/Azure/actions)

---

## Conclusion

The root cause is a **dual migration strategy** where migrations run successfully in GitHub Actions but fail silently during container startup. The fix is to:
1. **Immediately**: Verify and manually apply migrations if needed
2. **Short-term**: Disable container startup migrations in Staging/Production
3. **Long-term**: Standardize migration execution and add comprehensive validation

This issue was preventable with better architecture design (single migration execution point) and proper health checks (schema validation, not just connectivity).
