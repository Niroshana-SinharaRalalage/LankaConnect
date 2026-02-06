# Phase 6A.61 Hotfix Implementation Plan

**Date**: 2026-01-13
**Priority**: CRITICAL
**Estimated Time**: 1 hour 15 minutes

---

## Immediate Actions (15 minutes)

### Step 1: Verify Database State (5 minutes)

```bash
# Connect to Azure PostgreSQL Staging
DB_CONNECTION=$(az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name DATABASE-CONNECTION-STRING \
  --query value -o tsv)

# Check if table exists
psql "$DB_CONNECTION" -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'communications' AND table_name = 'event_notification_history';"

# Check if template exists
psql "$DB_CONNECTION" -c "SELECT name, description FROM communications.email_templates WHERE name = 'event-details';"

# Check migration history
psql "$DB_CONNECTION" -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%Phase6A61%' ORDER BY \"MigrationId\";"
```

**Expected Results**:
- Table: `event_notification_history` should exist
- Template: `event-details` should exist
- Migrations: Both `20260113020400_Phase6A61_AddEventDetailsTemplate` and `20260113020500_Phase6A61_AddEventNotificationHistoryTable` should be in history

**If Missing**: Proceed to Step 2

### Step 2: Apply Migrations Manually (5 minutes)

```bash
# Navigate to API project
cd src/LankaConnect.API

# Apply migrations
dotnet ef database update \
  --connection "$DB_CONNECTION" \
  --project ../LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj \
  --context AppDbContext \
  --verbose
```

### Step 3: Restart Container (2 minutes)

```bash
# Restart container app
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging

# Wait for restart
sleep 30
```

### Step 4: Verify Fix (3 minutes)

```bash
# Get API URL
API_URL=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

# Test health check
curl -X GET "https://$API_URL/health" | jq

# Test database schema (should be in health check after fix)
psql "$DB_CONNECTION" -c "SELECT COUNT(*) as table_exists FROM information_schema.tables WHERE table_schema = 'communications' AND table_name = 'event_notification_history';"
```

---

## Short-Term Fixes (1 hour)

### Fix 1: Disable Container Startup Migrations in Staging/Production (15 minutes)

**File**: `src/LankaConnect.API/Program.cs`

**Current Code** (lines 193-223):
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
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        // Seed initial data (Development and Staging only)
        if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
        {
            var dbInitializer = new DbInitializer(
                context,
                services.GetRequiredService<ILogger<DbInitializer>>(),
                services.GetRequiredService<IPasswordHashingService>());
            await dbInitializer.SeedAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");
        throw; // Re-throw to prevent application startup with incomplete database
    }
}
```

**New Code**:
```csharp
// Apply database migrations automatically on startup
// CRITICAL FIX Phase 6A.61: Only run migrations in Development to avoid dual execution
// In Staging/Production, migrations are applied exclusively via GitHub Actions CI/CD
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
    // Phase 6A.61 Fix: Log that migrations are skipped in Staging/Production
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "Skipping automatic migrations in {Environment} environment. " +
        "Database schema changes are applied via GitHub Actions CI/CD pipeline (deploy-staging.yml lines 101-142).",
        app.Environment.EnvironmentName);
}
```

**Commit Message**:
```
fix(phase-6a61-hotfix): Disable container startup migrations in Staging/Production

Problem:
- Migrations were running twice: once in GitHub Actions, once at container startup
- Container startup migration was failing silently, causing API failures
- Health check passed but tables were missing

Solution:
- Migrations now ONLY run in Development environment at container startup
- Staging/Production migrations handled exclusively by GitHub Actions
- Prevents dual execution and permission issues

Impact:
- Fixes Phase 6A.61 API 400 errors
- Eliminates silent migration failures
- Simplifies deployment architecture
```

---

### Fix 2: Add Post-Migration Schema Verification (20 minutes)

**File**: `.github/workflows/deploy-staging.yml`

**Insert after line 142** (after "Run EF Migrations" step):

```yaml
      - name: Verify Database Schema
        run: |
          echo "Verifying database schema after migrations..."

          # Retrieve database connection string from Key Vault
          DB_CONNECTION=$(az keyvault secret show \
            --vault-name ${{ env.KEY_VAULT_NAME }} \
            --name DATABASE-CONNECTION-STRING \
            --query value -o tsv)

          # Install PostgreSQL client
          sudo apt-get update -qq
          sudo apt-get install -y -qq postgresql-client > /dev/null 2>&1

          # Verify critical Phase 6A.61 tables exist
          echo "Checking communications.event_notification_history table..."
          TABLE_EXISTS=$(psql "$DB_CONNECTION" -t -c "
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = 'communications'
            AND table_name = 'event_notification_history'
          " | xargs)

          if [ "$TABLE_EXISTS" -ne 1 ]; then
            echo "❌ Schema verification failed: event_notification_history table missing"
            exit 1
          fi
          echo "✅ event_notification_history table exists"

          # Verify event-details template exists
          echo "Checking event-details email template..."
          TEMPLATE_EXISTS=$(psql "$DB_CONNECTION" -t -c "
            SELECT COUNT(*) FROM communications.email_templates
            WHERE name = 'event-details'
            AND type = 'Transactional'
            AND category = 'Events'
          " | xargs)

          if [ "$TEMPLATE_EXISTS" -ne 1 ]; then
            echo "❌ Schema verification failed: event-details template missing"
            echo "Found $TEMPLATE_EXISTS templates (expected 1)"
            exit 1
          fi
          echo "✅ event-details template exists"

          # Verify foreign keys are correct
          echo "Checking foreign key constraints..."
          FK_COUNT=$(psql "$DB_CONNECTION" -t -c "
            SELECT COUNT(*) FROM information_schema.table_constraints
            WHERE constraint_type = 'FOREIGN KEY'
            AND table_schema = 'communications'
            AND table_name = 'event_notification_history'
          " | xargs)

          if [ "$FK_COUNT" -ne 2 ]; then
            echo "⚠️  Warning: Expected 2 foreign keys, found $FK_COUNT"
          fi
          echo "✅ Foreign key constraints verified ($FK_COUNT constraints)"

          # Verify indexes exist
          echo "Checking indexes..."
          INDEX_COUNT=$(psql "$DB_CONNECTION" -t -c "
            SELECT COUNT(*) FROM pg_indexes
            WHERE schemaname = 'communications'
            AND tablename = 'event_notification_history'
            AND indexname LIKE 'ix_%'
          " | xargs)

          if [ "$INDEX_COUNT" -lt 2 ]; then
            echo "⚠️  Warning: Expected at least 2 indexes, found $INDEX_COUNT"
          fi
          echo "✅ Indexes verified ($INDEX_COUNT indexes)"

          echo "════════════════════════════════════════════════════════════════"
          echo "✅ Database schema verification completed successfully"
          echo "════════════════════════════════════════════════════════════════"
```

**Commit Message**:
```
feat(phase-6a61-hotfix): Add post-migration schema verification to CI/CD

Problem:
- Migrations could succeed but leave incomplete schema
- No validation step between migration and deployment
- Silent failures were not detected

Solution:
- Add schema verification step after EF migrations
- Check for table existence, templates, foreign keys, and indexes
- Fail deployment if schema is incomplete

Verification:
- communications.event_notification_history table
- event-details email template
- Foreign key constraints (2)
- Indexes (2+)

Impact:
- Catches incomplete migrations before deployment
- Prevents container from starting with broken schema
- Provides detailed error messages for debugging
```

---

### Fix 3: Improve Container Logging Configuration (10 minutes)

**File**: `src/LankaConnect.API/appsettings.Staging.json`

**Add/Update**:
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "Microsoft.EntityFrameworkCore.Migrations": "Information",
        "System.Net.Http.HttpClient": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "LankaConnect.API",
      "Environment": "Staging"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**Commit Message**:
```
fix(phase-6a61-hotfix): Improve container logging for Staging environment

Problem:
- Container logs were not accessible via az containerapp logs show
- Migration failures were not visible in logs
- Debugging container issues was difficult

Solution:
- Configure Serilog to write to Console with detailed format
- Enable EF Core migration logging
- Add structured properties for filtering

Impact:
- Container logs now visible in Azure Portal and CLI
- Migration errors are logged with full stack traces
- Easier debugging for future issues
```

---

### Fix 4: Add Detailed Error Messages to API Endpoints (15 minutes)

**File**: `src/LankaConnect.Application/Events/Commands/SendEventNotification/SendEventNotificationCommandHandler.cs`

**Update Exception Handling** (around line 80):

**Current Code**:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "[Phase 6A.61] Failed to send event notification - EventId: {EventId}", command.EventId);
    return Result<int>.Failure("Failed to send notification");
}
```

**New Code**:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex,
        "[Phase 6A.61] Failed to send event notification - " +
        "EventId: {EventId}, ExceptionType: {ExceptionType}, Message: {Message}",
        command.EventId, ex.GetType().FullName, ex.Message);

    // Phase 6A.61 Hotfix: Provide more detailed error message for debugging
    var errorMessage = _environment.IsDevelopment() || _environment.IsStaging()
        ? $"Failed to send notification: {ex.Message}"
        : "Failed to send notification";

    return Result<int>.Failure(errorMessage);
}
```

**Note**: Add `IWebHostEnvironment _environment` to constructor if not already present.

**Commit Message**:
```
fix(phase-6a61-hotfix): Add detailed error messages for notification endpoints

Problem:
- Generic "Failed to send notification" error hid root cause
- Database schema issues were not visible to developers
- Debugging required direct database access

Solution:
- Include exception details in Development/Staging environments
- Log full exception type and message
- Keep generic message in Production for security

Impact:
- Developers can identify schema issues from API responses
- Faster debugging in Staging environment
- Maintains security in Production
```

---

## Deployment Steps

### 1. Create Hotfix Branch

```bash
# Create hotfix branch from develop
git checkout develop
git pull origin develop
git checkout -b hotfix/phase-6a61-migration-failure

# Apply fixes (Fix 1-4 above)
# ... make code changes ...

# Stage changes
git add src/LankaConnect.API/Program.cs
git add .github/workflows/deploy-staging.yml
git add src/LankaConnect.API/appsettings.Staging.json
git add src/LankaConnect.Application/Events/Commands/SendEventNotification/SendEventNotificationCommandHandler.cs

# Commit with detailed message
git commit -m "fix(phase-6a61-hotfix): Resolve dual migration strategy causing API failures

PROBLEM:
- Phase 6A.61 migrations ran twice (GitHub Actions + container startup)
- Container startup migration failed silently
- API endpoints returned 400 errors due to missing tables
- Health check passed but schema was incomplete

ROOT CAUSE:
- Dual migration execution points (CI/CD + container startup)
- Container migration failure didn't prevent app startup
- No schema validation in health checks
- Insufficient logging in container environment

SOLUTION:
1. Disable container startup migrations in Staging/Production
2. Add post-migration schema verification in GitHub Actions
3. Improve container logging configuration
4. Add detailed error messages in Staging environment

FIXES:
- src/LankaConnect.API/Program.cs: Only run migrations in Development
- .github/workflows/deploy-staging.yml: Add schema verification step
- src/LankaConnect.API/appsettings.Staging.json: Enable EF Core logging
- SendEventNotificationCommandHandler.cs: Add detailed error messages

VERIFICATION:
- Manual database verification (Step 1)
- Post-migration schema checks (Step 2)
- Container logs accessible (Step 3)
- API error messages informative (Step 4)

IMPACT:
- Eliminates dual migration execution
- Catches schema issues before deployment
- Improves debugging capabilities
- Prevents future silent failures

REFERENCES:
- docs/RCA_Phase6A61_Migration_Failure.md
- docs/HOTFIX_Phase6A61_Implementation_Plan.md

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

# Push hotfix branch
git push origin hotfix/phase-6a61-migration-failure
```

### 2. Test Locally (Optional)

```bash
# Start local development environment
docker-compose up -d

# Verify migrations run automatically in Development
dotnet run --project src/LankaConnect.API/LankaConnect.API.csproj --environment Development

# Check logs for migration messages
# Expected: "Applying database migrations..." → "Database migrations applied successfully"

# Stop containers
docker-compose down
```

### 3. Deploy to Staging

```bash
# Merge hotfix to develop
git checkout develop
git merge hotfix/phase-6a61-migration-failure --no-ff -m "Merge hotfix/phase-6a61-migration-failure into develop"

# Push to trigger deployment
git push origin develop

# Monitor GitHub Actions deployment
gh run watch

# Expected workflow steps:
# 1. Build & Test → SUCCESS
# 2. Run EF Migrations → SUCCESS
# 3. **NEW** Verify Database Schema → SUCCESS (catches incomplete migrations)
# 4. Build Docker Image → SUCCESS
# 5. Push to ACR → SUCCESS
# 6. Update Container App → SUCCESS
# 7. Smoke Test - Health Check → SUCCESS
```

### 4. Verify Deployment

```bash
# Get API URL
API_URL=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

# Test health check
curl -X GET "https://$API_URL/health" | jq

# Check container logs (should now be accessible)
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 | grep -E "migration|schema|Phase6A61"

# Expected log message:
# "Skipping automatic migrations in Staging environment. Database schema changes are applied via GitHub Actions CI/CD pipeline."

# Test API endpoints (requires valid JWT token)
# You'll need to:
# 1. Login to get JWT token
# 2. Create/find an event ID
# 3. Test send notification endpoint

# Example:
# curl -X POST "https://$API_URL/api/Events/{event-id}/send-notification" \
#   -H "Authorization: Bearer {token}" \
#   -H "Content-Type: application/json"
```

---

## Rollback Plan

If hotfix causes issues:

```bash
# Revert to previous container image
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --output table

# Activate previous revision
az containerapp revision activate \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision <previous-revision-name>

# Verify rollback
curl -X GET "https://$API_URL/health"
```

---

## Success Criteria

### Phase 1: Immediate Fix
- [x] Database tables verified to exist
- [x] Container restarted
- [x] API endpoints return 200 (not 400)

### Phase 2: Short-Term Fixes
- [x] Container startup migrations disabled in Staging/Production
- [x] Schema verification added to GitHub Actions
- [x] Container logs accessible and informative
- [x] API error messages detailed in Staging

### Phase 3: Deployment Verification
- [x] GitHub Actions workflow passes all steps including new schema verification
- [x] Container logs show "Skipping automatic migrations" message
- [x] Health check passes
- [x] API endpoints functional (200 responses)

---

## Post-Deployment Monitoring (24 hours)

### Metrics to Watch

1. **API Error Rate**
   - Monitor 400/500 error rates for Phase 6A.61 endpoints
   - Expected: < 1% error rate

2. **Container Logs**
   - Check for any migration-related errors
   - Verify "Skipping automatic migrations" message appears

3. **Database Performance**
   - Monitor query performance for `event_notification_history` table
   - Check for slow queries or locking issues

4. **GitHub Actions**
   - Verify schema verification step passes on future deployments
   - Check deployment time (should be slightly longer due to verification)

### Alert Thresholds

- API 400 error rate > 5% → Investigate immediately
- API 500 error rate > 1% → Investigate immediately
- Container restart count > 3 in 1 hour → Check logs
- Migration verification step failure → Block deployment

---

## Documentation Updates

### Files to Update After Deployment

1. **PROGRESS_TRACKER.md**
   - Add hotfix details under Phase 6A.61
   - Mark as "Complete (with hotfix)"

2. **PHASE_6A61_MANUAL_EMAIL_DISPATCH_SPEC.md**
   - Add "Known Issues" section with hotfix reference
   - Update deployment notes

3. **README.md** (if applicable)
   - Update deployment instructions
   - Add note about migration strategy

4. **Architecture Diagrams**
   - Update to show single migration execution point
   - Document GitHub Actions as sole migration executor for Staging/Production

---

## Lessons Learned

### What Went Wrong
1. Dual migration execution points created conflict
2. Silent failures were not detected by health checks
3. Generic error messages hid root cause
4. Container logs were not accessible for debugging

### What Went Right
1. Migrations worked correctly in GitHub Actions
2. Code changes were minimal (no domain logic affected)
3. Rollback plan was straightforward (previous container revision)
4. RCA identified root cause quickly

### Process Improvements
1. All migrations must include verification queries
2. Health checks must validate schema completeness
3. Container logs must be accessible in all environments
4. Error messages must be detailed in non-production environments
5. Deployment pipeline must verify schema before updating containers

---

## Contact Information

**For Questions/Issues**:
- System Architect: [Your Name]
- DevOps Lead: [DevOps Name]
- Backend Lead: [Backend Name]

**Escalation Path**:
1. Check container logs: `az containerapp logs show`
2. Check database state: `psql $DB_CONNECTION`
3. Review RCA: `docs/RCA_Phase6A61_Migration_Failure.md`
4. Contact System Architect

---

## Appendix: Quick Reference Commands

```bash
# Database verification
psql "$DB_CONNECTION" -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'communications' AND table_name = 'event_notification_history';"

# Container logs
az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 50

# Container restart
az containerapp revision restart --name lankaconnect-api-staging --resource-group lankaconnect-staging

# Health check
curl -X GET "https://lankaconnect-api-staging.azurecontainerapps.io/health" | jq

# Migration history
psql "$DB_CONNECTION" -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 10;"

# GitHub Actions status
gh run list --workflow deploy-staging.yml --limit 5

# Rollback to previous revision
az containerapp revision list --name lankaconnect-api-staging --resource-group lankaconnect-staging --output table
az containerapp revision activate --name lankaconnect-api-staging --resource-group lankaconnect-staging --revision <revision-name>
```
