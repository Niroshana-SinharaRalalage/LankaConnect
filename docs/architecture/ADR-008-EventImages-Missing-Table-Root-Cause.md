# ADR-008: EventImages Table Missing in Staging - Root Cause Analysis

## Status
ANALYSIS COMPLETE - Solution Documented

## Date
2025-12-03

## Context

### Symptom
Production staging environment throwing 500 errors on image upload:
```
PostgresException: 42P01: relation "EventImages" does not exist
```

### Initial Investigation Findings
1. Migration file `20251103040053_AddEventImages.cs` exists in codebase
2. Running `dotnet ef database update` locally with staging connection says "database is already up to date"
3. Table `events."EventImages"` does not exist in staging PostgreSQL database
4. Application code expects the table to exist (EventImageConfiguration is registered)

### The Paradox
How can EF Core claim the database is "up to date" when a critical table is missing?

## Root Cause Analysis

### Discovery: Dual Migration Folder Architecture

The codebase has **TWO migration folders**:

```
src/LankaConnect.Infrastructure/
├── Migrations/                           ← OLD LOCATION (17 migrations)
│   ├── 20250830150251_InitialCreate.cs
│   ├── 20250831125422_InitialMigration.cs
│   ├── ...
│   └── 20251103040053_AddEventImages.cs  ← CRITICAL FILE (Namespace: LankaConnect.Infrastructure.Data.Migrations)
│
└── Data/
    └── Migrations/                       ← NEW LOCATION (27 migrations)
        ├── 20251109152709_AddNewsletterSubscribers.cs
        ├── 20251110004152_CreateMetroAreasTable.cs
        ├── ...
        └── 20251203162215_AddPricingJsonbColumn.cs
```

### Key Facts

1. **Namespace Consistency**: ALL migration files use `namespace LankaConnect.Infrastructure.Data.Migrations` regardless of physical location
2. **EF Core Configuration**: `npgsqlOptions.MigrationsAssembly("LankaConnect.Infrastructure")` (no subfolder specified)
3. **Reflection-Based Discovery**: EF Core finds migrations via reflection by scanning the assembly for classes implementing `Migration`
4. **Build/Publish Behavior**: Deployment process may only package files from one folder

### Timeline of Events

**Nov 3, 2025 (Commit f582356)**: "Consolidate Event migrations to correct directory"
- Moved `20251103040053_AddEventImages.cs` from `Migrations/` to `Data/Migrations/`
- This file was the LAST migration created in the old location
- Subsequent migrations were created in `Data/Migrations/` only

**Nov 9+**: New migrations generated in `Data/Migrations/` only

**Dec 3**: Issue discovered - EventImages table missing in staging

### Why "Database is up to date" Was a Lie

When running `dotnet ef database update` locally:

1. **EF Core scans the assembly** and finds migrations in BOTH folders via reflection
2. **Checks `__EFMigrationsHistory`** table in database
3. **Sees that all migrations from `Data/Migrations/` are applied**
4. **Incorrectly assumes** `AddEventImages` is also applied (because it's in the assembly)
5. **Reports "up to date"** even though the table doesn't exist

The issue: **EF Core's migration tracking only uses migration IDs, not file locations**

### Why Staging Deployment Failed

During deployment to staging Azure App Service:

**Hypothesis 1: Partial File Copy**
```bash
# If deployment process uses specific folder patterns:
COPY src/LankaConnect.Infrastructure/Data/Migrations/*.cs /app/Migrations/
# Result: Only NEW migrations deployed, OLD migrations (including AddEventImages) NOT copied
```

**Hypothesis 2: .csproj Compilation Configuration**
```xml
<ItemGroup>
  <Compile Include="Data\Migrations\**\*.cs" />
  <!-- Old Migrations/ folder might be excluded -->
</ItemGroup>
```

**Hypothesis 3: Migration Deployment Script**
```bash
# If using explicit migration list:
dotnet ef migrations add ...
dotnet ef database update --migration <latest-migration-in-Data-Migrations>
# Result: Skips AddEventImages because it's not in the expected folder
```

### Database State Verification

**Expected State** (Development):
```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';
-- Result: 1 row (migration applied)
```

**Actual State** (Staging):
```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';
-- Result: 0 rows (migration NOT applied)

SELECT tablename FROM pg_tables
WHERE schemaname = 'events' AND tablename = 'EventImages';
-- Result: 0 rows (table does NOT exist)
```

## Decision: Three-Part Remediation Strategy

### Part 1: Move Missing Migration to Correct Folder

**Action**: Copy `20251103040053_AddEventImages.cs` to `Data/Migrations/`

**Rationale**:
- All NEW migrations are being created in `Data/Migrations/`
- Ensures all migrations are in ONE consistent location
- Prevents future deployment issues

**Implementation**:
```bash
# Copy file to new location
cp "src/LankaConnect.Infrastructure/Migrations/20251103040053_AddEventImages.cs" \
   "src/LankaConnect.Infrastructure/Data/Migrations/20251103040053_AddEventImages.cs"

# Copy designer file as well
cp "src/LankaConnect.Infrastructure/Migrations/20251103040053_AddEventImages.Designer.cs" \
   "src/LankaConnect.Infrastructure/Data/Migrations/20251103040053_AddEventImages.Designer.cs"
```

### Part 2: Apply Missing Migration to Staging Database

**Option A: Use EF Core CLI** (Preferred)
```bash
# Set connection string for staging
$env:ConnectionStrings__DefaultConnection = "Host=your-staging-db;Database=lankaconnect;..."

# Apply migrations up to AddEventImages
dotnet ef database update 20251103040053_AddEventImages \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API
```

**Option B: Generate SQL Script and Apply Manually**
```bash
# Generate SQL for just the AddEventImages migration
dotnet ef migrations script 20251102144315_AddEventCategoryAndTicketPrice 20251103040053_AddEventImages \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --output apply_eventimages.sql

# Review the SQL
cat apply_eventimages.sql

# Apply to staging database
psql -h your-staging-db -U postgres -d lankaconnect -f apply_eventimages.sql
```

**Option C: Deploy and Let App Startup Migrate** (If using auto-migration)
```csharp
// In Program.cs startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // This will apply missing migrations
}
```

### Part 3: Consolidate ALL Migrations to Single Folder

**Action**: Move remaining migrations from old `Migrations/` folder to `Data/Migrations/`

**Files to Move** (16 migrations):
```
20250830150251_InitialCreate
20250831125422_InitialMigration
20250904194650_AddCommunicationsTables
20251028184528_AddEntraExternalIdSupport
20251031125825_AddUserProfilePhoto
20251031131720_AddUserLocation
20251031194253_AddUserCulturalInterestsAndLanguages
20251101194703_CreateUserCulturalInterestsAndLanguagesTables
20251102000439_AddExternalLoginsSupport
20251102061243_AddEventLocationWithPostGIS
20251102144315_AddEventCategoryAndTicketPrice
20251104004732_AddEventVideos
20251104060300_AddEventAnalytics
20251104184035_AddFullTextSearchSupport
20251104195443_AddWaitingListAndSocialSharing
20251102000000_CreateEventsAndRegistrationsTables
```

**Post-Move Actions**:
1. Delete old `Migrations/` folder
2. Update documentation to reference single migration location
3. Verify all migrations still compile
4. Run `dotnet ef migrations list` to confirm all migrations are found

## Verification Steps

### Pre-Deployment Verification
```bash
# 1. Verify all migrations in Data/Migrations/
ls -la src/LankaConnect.Infrastructure/Data/Migrations/*.cs | wc -l
# Expected: 44 files (22 migrations × 2 files each)

# 2. Verify no migrations in old location
ls src/LankaConnect.Infrastructure/Migrations/*.cs 2>/dev/null || echo "Empty (good!)"

# 3. Verify all migrations compile
dotnet build src/LankaConnect.Infrastructure

# 4. List all migrations
dotnet ef migrations list --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
```

### Post-Deployment Verification (Staging)
```sql
-- 1. Verify EventImages table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
) AS "HasEventImagesTable";
-- Expected: true

-- 2. Verify migration is recorded in history
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';
-- Expected: 1 row

-- 3. Verify table schema
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'events' AND table_name = 'EventImages'
ORDER BY ordinal_position;
-- Expected: Id, EventId, ImageUrl, BlobName, DisplayOrder, UploadedAt

-- 4. Test image upload
-- POST /api/events/{eventId}/images
-- Expected: 200 OK with image metadata
```

## Rollback Plan

If migration fails in staging:

**Step 1: Restore Database Backup**
```bash
# Azure Database for PostgreSQL
az postgres flexible-server backup restore \
    --resource-group your-rg \
    --name your-server \
    --backup-retention-days 7 \
    --time "2025-12-03T00:00:00Z"
```

**Step 2: Remove Failed Migration from History**
```sql
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';
```

**Step 3: Revert Code Changes**
```bash
git revert HEAD
git push origin develop
```

**Step 4: Investigate and Re-Plan**
- Review migration script for errors
- Check database locks or permissions
- Test in non-production environment first

## Lessons Learned

### Why This Happened

1. **No Explicit Migration Folder Configuration**: EF Core was configured with assembly name only, not specific folder
2. **Namespace Independence**: Migration namespace didn't enforce physical location
3. **Manual File Movement**: Migrations were moved manually (commit f582356) without updating deployment scripts
4. **Insufficient Integration Testing**: Staging deployment process wasn't tested after folder restructure
5. **Migration Discovery Ambiguity**: EF Core's reflection-based discovery hid the folder inconsistency

### Prevention Measures for Future

1. **Explicit Migration Folder Configuration**
```csharp
// DependencyInjection.cs
npgsqlOptions.MigrationsAssembly("LankaConnect.Infrastructure");
npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
// TODO: Add MigrationsFolder option if supported
```

2. **CI/CD Pipeline Checks**
```yaml
# .github/workflows/deploy-staging.yml
- name: Verify Migration Folder Consistency
  run: |
    MIGRATION_COUNT=$(find src/LankaConnect.Infrastructure/Data/Migrations -name "*.cs" -type f | wc -l)
    if [ $MIGRATION_COUNT -eq 0 ]; then
      echo "ERROR: No migrations found in Data/Migrations/"
      exit 1
    fi
```

3. **Pre-Deployment Migration Verification**
```bash
# Run before each deployment
dotnet ef migrations list --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
dotnet ef migrations script --idempotent --output verify_migrations.sql
```

4. **Database Health Checks**
```csharp
// HealthChecks/MigrationHealthCheck.cs
public class MigrationHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context)
    {
        var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            return HealthCheckResult.Degraded($"Pending migrations: {string.Join(", ", pendingMigrations)}");
        }
        return HealthCheckResult.Healthy("All migrations applied");
    }
}
```

5. **Documentation Standard**
- Document migration folder in CONTRIBUTING.md
- Add migration creation guide with explicit folder paths
- Require architecture approval for folder structure changes

## Related Documents

- [MIGRATION_FOLDER_ARCHITECTURE.md](./MIGRATION_FOLDER_ARCHITECTURE.md) - Detailed folder structure analysis
- [CRITICAL_ISSUES_ANALYSIS.md](./CRITICAL_ISSUES_ANALYSIS.md) - Full system analysis
- [EventMediaUploadArchitecture.md](./EventMediaUploadArchitecture.md) - EventImages feature documentation
- [ADR-007-Consolidate-Migrations.md](./adr/ADR-007-Consolidate-Migrations.md) - Migration consolidation decision

## Consequences

### Positive
- Single source of truth for migrations (Data/Migrations/)
- Consistent deployment behavior across environments
- Easier to track migration history
- Reduced risk of missing migrations in future deployments

### Negative
- Git history shows migrations in two locations (historical artifact)
- Requires careful git bisect when debugging historical migration issues
- One-time manual fix required for staging environment

### Neutral
- No impact on application functionality after fix is applied
- No breaking changes to API or database schema
- Transparent to end users

## Implementation Checklist

- [x] Root cause identified (dual migration folders)
- [ ] Copy AddEventImages migration to Data/Migrations/
- [ ] Apply AddEventImages migration to staging database
- [ ] Verify EventImages table exists in staging
- [ ] Test image upload in staging
- [ ] Move all remaining migrations to Data/Migrations/
- [ ] Delete old Migrations/ folder
- [ ] Update CI/CD pipeline with migration checks
- [ ] Document migration creation process
- [ ] Add migration health check to application
- [ ] Verify all environments have consistent migration state

## Notes

This issue demonstrates the importance of:
1. Explicit configuration over convention (specify migration folder explicitly)
2. Integration testing of deployment processes
3. Database state verification in staging before production
4. Version control of infrastructure as code (migrations)
5. Automated checks for deployment consistency

The dual folder structure was likely created during a refactoring effort (commit f582356) to organize infrastructure code, but the deployment process wasn't updated accordingly. This is a common issue when restructuring code without updating all dependent systems.
