# Quick Fix Guide - Critical Issues

**Last Updated**: 2025-12-03
**Estimated Time**: 2 hours
**Risk Level**: Medium (requires staging database changes)

## TL;DR

**Problem**: Three critical issues blocking production:
1. Two migration folders causing inconsistency
2. EF Core error preventing new migrations
3. EventImages table missing in staging

**Solution**: Consolidate migrations, fix EF Core config, apply missing migrations

---

## Quick Commands (Copy-Paste Ready)

### 1. Fix EF Core Configuration (5 minutes)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\EventConfiguration.cs`

**Find** (around line 69-79):
```csharp
// Configure TicketPrice as owned Money value object (Epic 2 Phase 2 - legacy single pricing)
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.Property(m => m.Amount)
        .HasColumnName("ticket_price_amount")
        .HasPrecision(18, 2);

    money.Property(m => m.Currency)
        .HasColumnName("ticket_price_currency")
        .HasConversion<string>()
        .HasMaxLength(3);
});
```

**Replace with**:
```csharp
// Configure TicketPrice as JSONB for consistency with Pricing
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.ToJson("ticket_price");
});
```

**Find** (around line 82-87):
```csharp
// TEMPORARILY DISABLED TO FIX EVENTIMAGES MIGRATION ISSUE
// builder.OwnsOne(e => e.Pricing, pricing =>
// {
//     pricing.ToJson("pricing");
// });
```

**Replace with**:
```csharp
// Session 21: Configure Pricing as JSONB for dual ticket pricing (adult/child)
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");  // Money objects auto-serialized
});
```

### 2. Create Migration for TicketPrice Conversion (5 minutes)

```powershell
cd c:\Work\LankaConnect
dotnet ef migrations add ConvertTicketPriceToJson `
    --project src\LankaConnect.Infrastructure `
    --startup-project src\LankaConnect.API `
    --output-dir Data\Migrations
```

**Verify migration created**:
```powershell
ls src\LankaConnect.Infrastructure\Data\Migrations\*ConvertTicketPriceToJson*
```

### 3. Test in Development (10 minutes)

```powershell
# Build to check for errors
dotnet build src\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj

# Apply migration
dotnet ef database update `
    --project src\LankaConnect.Infrastructure `
    --startup-project src\LankaConnect.API

# Run tests
dotnet test tests\LankaConnect.Infrastructure.Tests\LankaConnect.Infrastructure.Tests.csproj
```

### 4. Consolidate Migrations (10 minutes)

```powershell
cd c:\Work\LankaConnect

# Move all migrations from old folder to new folder
$sourceFolder = "src\LankaConnect.Infrastructure\Migrations"
$destFolder = "src\LankaConnect.Infrastructure\Data\Migrations"

# Only move files that don't already exist in destination
Get-ChildItem -Path $sourceFolder -Filter "*.cs" | ForEach-Object {
    $destPath = Join-Path $destFolder $_.Name
    if (-not (Test-Path $destPath)) {
        Copy-Item -Path $_.FullName -Destination $destFolder -Force
        Write-Host "Copied: $($_.Name)"
    } else {
        Write-Host "Skipped (exists): $($_.Name)"
    }
}

# Verify count
Write-Host "Old folder count: $(Get-ChildItem -Path $sourceFolder -Filter '*.cs' | Measure-Object).Count"
Write-Host "New folder count: $(Get-ChildItem -Path $destFolder -Filter '*.cs' | Measure-Object).Count"
```

**IMPORTANT**: Do NOT delete old folder yet - verify first!

### 5. Verify Consolidated Migrations (5 minutes)

```powershell
# List all migrations
dotnet ef migrations list `
    --project src\LankaConnect.Infrastructure `
    --startup-project src\LankaConnect.API

# Build to ensure no errors
dotnet build src\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj
```

**Expected Output**: Should show all migrations from August 30 to present, including:
- `20251103040053_AddEventImages`
- `20251203XXXXXX_ConvertTicketPriceToJson` (new)

### 6. Apply Missing Migrations to Staging (20 minutes)

**Option A: Generate SQL Script** (Recommended for production)
```powershell
# Generate SQL for AddEventImages migration
dotnet ef migrations script 20251102144315_AddEventCategoryAndTicketPrice 20251103040053_AddEventImages `
    --project src\LankaConnect.Infrastructure `
    --startup-project src\LankaConnect.API `
    --output scripts\apply_eventimages_staging.sql `
    --idempotent
```

**Review script** before applying to staging:
```powershell
notepad scripts\apply_eventimages_staging.sql
```

**Apply to staging** (via SSH or database client):
```bash
# SSH to staging server
ssh your-staging-server

# Apply SQL script
psql -U postgres -d lankaconnect_staging -f apply_eventimages_staging.sql

# Verify table exists
psql -U postgres -d lankaconnect_staging -c "
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
) AS table_exists;
"
```

**Option B: Direct Migration** (If you have direct access)
```bash
# SSH to staging server
cd /app/LankaConnect

# Apply migrations
dotnet ef database update \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --connection "Host=localhost;Database=lankaconnect_staging;Username=postgres;Password=xxx"
```

### 7. Verify Staging (10 minutes)

```bash
# Check migration history
psql -U postgres -d lankaconnect_staging -c "
SELECT \"MigrationId\"
FROM \"__EFMigrationsHistory\"
WHERE \"MigrationId\" IN ('20251103040053_AddEventImages', 'ConvertTicketPriceToJson')
ORDER BY \"MigrationId\";
"

# Check EventImages table
psql -U postgres -d lankaconnect_staging -c "
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'events' AND table_name = 'EventImages'
ORDER BY ordinal_position;
"

# Check events table columns
psql -U postgres -d lankaconnect_staging -c "
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'events' AND table_name = 'events'
  AND column_name IN ('ticket_price', 'pricing', 'ticket_price_amount', 'ticket_price_currency')
ORDER BY ordinal_position;
"
```

**Expected Results**:
- AddEventImages migration in history: ✅
- EventImages table exists with 6 columns: ✅
- events.ticket_price (jsonb): ✅
- events.pricing (jsonb): ✅
- ticket_price_amount and ticket_price_currency: ❌ (should NOT exist after migration)

### 8. Test Event Image Upload (5 minutes)

```bash
# Test image upload endpoint
curl -X POST https://staging.yourdomain.com/api/events/{eventId}/images \
  -H "Authorization: Bearer {your-staging-token}" \
  -F "image=@test_image.jpg"
```

**Expected**: 200 OK with image URL in response

### 9. Commit Changes (5 minutes)

```powershell
cd c:\Work\LankaConnect

# Stage changes
git add src\LankaConnect.Infrastructure\Data\Configurations\EventConfiguration.cs
git add src\LankaConnect.Infrastructure\Data\Migrations\*ConvertTicketPriceToJson*
git add docs\architecture\

# Commit
git commit -m "fix(ef-core): Consolidate migrations and fix Money ToJson configuration

Critical fixes for production deployment:

1. EF Core Configuration:
   - Convert Event.TicketPrice to ToJson (JSONB storage)
   - Enable Event.Pricing ToJson configuration
   - Fixes shared-type conflict for Money value object

2. Migration Consolidation:
   - All migrations now in Data/Migrations/ folder
   - Removed duplicate migrations from old Migrations/ folder
   - Ensures consistent migration state across environments

3. Database Schema:
   - Created migration to convert ticket_price columns to JSONB
   - Maintains backward compatibility with data migration
   - EventImages table migration now visible in all environments

Root Cause:
- Money value object used in two ownership contexts (direct and JSON)
- EF Core couldn't determine which mapping to use
- Migration folder split caused staging deployment issues

Impact:
- Fixes 'Unable to determine the owner' EF Core error
- Enables EventImages table creation in staging
- Unblocks dual pricing feature (adult/child tickets)

Verification:
- ✅ 0 compilation errors
- ✅ All migrations consolidated
- ✅ EventConfiguration uses ToJson for both TicketPrice and Pricing
- ✅ Data migration script preserves existing ticket prices

Ref: docs/architecture/CRITICAL_ISSUES_ANALYSIS.md
Ref: docs/architecture/MIGRATION_FOLDER_ARCHITECTURE.md

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

### 10. Delete Old Migration Folder (Optional - After Verification)

**Only do this AFTER staging deployment is successful!**

```powershell
cd c:\Work\LankaConnect

# Final verification - should show 0 files or only Designer.cs files
Get-ChildItem -Path src\LankaConnect.Infrastructure\Migrations -Filter "*.cs" | Where-Object {
    -not $_.Name.EndsWith(".Designer.cs")
}

# If output is empty or only .Designer.cs files, safe to delete
# Remove-Item -Path src\LankaConnect.Infrastructure\Migrations -Recurse -Force
```

---

## Troubleshooting

### Issue: "Build failed with compilation errors"

**Check**:
```powershell
dotnet build src\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj --no-incremental
```

**Common Causes**:
1. ToJson() syntax error in EventConfiguration.cs
2. Missing closing braces
3. Namespace mismatch in migration files

**Fix**: Review EventConfiguration.cs lines 69-87 for syntax errors

---

### Issue: "Migration already exists"

**Error**: `The migration '20251203_ConvertTicketPriceToJson' already exists.`

**Fix**: Remove the existing migration and recreate
```powershell
dotnet ef migrations remove `
    --project src\LankaConnect.Infrastructure `
    --startup-project src\LankaConnect.API

# Then create again
dotnet ef migrations add ConvertTicketPriceToJson `
    --project src\LankaConnect.Infrastructure `
    --startup-project src\LankaConnect.API `
    --output-dir Data\Migrations
```

---

### Issue: "EventImages table already exists in staging"

**Check**:
```sql
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
);
```

**If true**: Table exists but migration history is missing

**Fix**: Add migration to history manually
```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251103040053_AddEventImages', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
```

---

### Issue: "Data loss error on TicketPrice conversion"

**Error**: `Column 'ticket_price_amount' cannot be dropped because it contains data`

**Fix**: The migration should include data migration SQL. Verify the generated migration:
```csharp
migrationBuilder.Sql(@"
    UPDATE events.events
    SET ticket_price = json_build_object(
        'Amount', ticket_price_amount,
        'Currency', ticket_price_currency
    )::jsonb
    WHERE ticket_price_amount IS NOT NULL;
");
```

If missing, add manually before dropping columns.

---

### Issue: "ToJson not recognized"

**Error**: `'EntityTypeBuilder<Money>' does not contain a definition for 'ToJson'`

**Check EF Core version**:
```powershell
dotnet list src\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj package | Select-String "EntityFrameworkCore"
```

**Requirement**: EF Core 7.0 or higher

**Fix**: Update EF Core if needed
```powershell
dotnet add src\LankaConnect.Infrastructure package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add src\LankaConnect.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
```

---

## Verification Checklist

Use this checklist to verify each step:

### Pre-Deployment
- [ ] EventConfiguration.cs uses `ToJson("ticket_price")` for TicketPrice
- [ ] EventConfiguration.cs uses `ToJson("pricing")` for Pricing
- [ ] No `OwnsOne` inside `ToJson` blocks
- [ ] `ConvertTicketPriceToJson` migration created
- [ ] Migration includes data migration SQL (converts columns to JSONB)
- [ ] All migrations in `Data/Migrations/` folder
- [ ] Build succeeds with 0 errors
- [ ] Tests pass

### Staging Deployment
- [ ] Database backup created
- [ ] `AddEventImages` migration applied (table exists)
- [ ] `ConvertTicketPriceToJson` migration applied
- [ ] Migration history shows both migrations
- [ ] Event image upload returns 200 OK
- [ ] Old events still display ticket prices correctly
- [ ] New events with Pricing create successfully
- [ ] No errors in application logs

### Production Deployment (After Staging Success)
- [ ] All staging checks passed
- [ ] Production database backup created
- [ ] Deployment plan approved
- [ ] Migrations applied in correct order
- [ ] Application restarted
- [ ] Health checks passing
- [ ] Event features working (create, update, image upload)

---

## Success Criteria

**You know it's fixed when**:

1. ✅ `dotnet build` succeeds with 0 errors
2. ✅ `dotnet ef migrations list` shows AddEventImages migration
3. ✅ Staging database has EventImages table
4. ✅ Event image upload returns 200 OK (not 500)
5. ✅ Event creation with dual pricing works
6. ✅ Old events still display correctly (backward compatibility)

---

## Rollback Plan

**If something goes wrong in staging**:

1. **Restore database**:
   ```bash
   pg_restore -U postgres -d lankaconnect_staging -c backup_YYYYMMDD.dump
   ```

2. **Revert code changes**:
   ```powershell
   git checkout HEAD~1 src\LankaConnect.Infrastructure\Data\Configurations\EventConfiguration.cs
   ```

3. **Restart application**:
   ```bash
   sudo systemctl restart lankaconnect-api
   ```

4. **Investigate** using full analysis document

---

## Next Steps After Fix

1. Update documentation:
   - [ ] Add ADR for migration consolidation
   - [ ] Update PROGRESS_TRACKER.md
   - [ ] Document in STREAMLINED_ACTION_PLAN.md

2. Add monitoring:
   - [ ] Migration count alert (dev vs staging)
   - [ ] EventImages table existence check
   - [ ] Image upload success rate metric

3. Prevent recurrence:
   - [ ] Add pre-deployment migration consistency check
   - [ ] Document EF Core ToJson pattern for team
   - [ ] Create CI/CD check for migration folder structure

---

## Related Documents

- **Full Analysis**: [CRITICAL_ISSUES_ANALYSIS.md](./CRITICAL_ISSUES_ANALYSIS.md)
- **Visual Guide**: [MIGRATION_FOLDER_ARCHITECTURE.md](./MIGRATION_FOLDER_ARCHITECTURE.md)
- **ADR**: [ADR-007-Consolidate-Migrations.md](./adr/ADR-007-Consolidate-Migrations.md)

## Questions?

**For technical questions**: Check the full analysis document
**For emergency issues**: Rollback and investigate
**For architectural decisions**: Consult ADR-007
