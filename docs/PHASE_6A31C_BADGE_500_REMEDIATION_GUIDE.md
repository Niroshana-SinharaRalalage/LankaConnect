# Phase 6A.31c: Badge 500 Error Remediation Guide

## Executive Summary

**Problem**: Badge API returns HTTP 500 Internal Server Error after deploying data-only migration 20251217205258_FixBadgeNullsDataOnly.

**Status**: Migration deployed successfully but error persists. Root cause UNKNOWN.

**This Guide Provides**: Step-by-step diagnostic workflow to identify and fix the actual root cause.

---

## Critical Analysis

### What We Know

1. **Migration Deployed**: GitHub Actions shows successful deployment at 21:12:47Z
2. **Migration Code**: Pure SQL UPDATE with COALESCE (no AlterColumn operations)
3. **API Status**: Still returns HTTP 500 on GET /api/badges
4. **Logs**: No Badge-specific exceptions found in Azure container logs (last 200 entries)
5. **Code Defense**: BadgeMappingExtensions.cs already has defensive null handling (Phase 6A.31b)

### What We Don't Know

1. **Did migration SQL execute?** - Deployment succeeded, but did UPDATE statement run?
2. **Do NULL values still exist?** - Our NULL hypothesis might be WRONG
3. **What's the actual exception?** - Can't find it in logs
4. **Is defensive code deployed?** - Code deployment vs migration deployment timing

---

## Diagnostic Workflow

### Step 1: Run Diagnostic Script

Execute the comprehensive diagnostic script that will check:
- Migration history in database
- NULL value detection across all 15 columns
- Sample badge data inspection
- API health check
- Badge endpoint testing with error capture
- Azure container logs analysis

```powershell
# From repository root
.\scripts\diagnose-badge-500-error.ps1 -Environment staging
```

**Prerequisites**:
- `.env.staging` file with `LANKACONNECT_CONNECTION_STRING` and `LANKACONNECT_API_BASE_URL`
- `psql` CLI tool installed (or manually run queries in Azure Portal)
- Azure CLI authenticated (`az login`)

### Step 2: Interpret Diagnostic Results

Follow the **Decision Tree** output at the end of the diagnostic script:

#### Scenario A: Migration NOT in __EFMigrationsHistory

**Meaning**: Migration was deployed but EF Core never recorded execution.

**Root Cause**: Migration failed silently during deployment.

**Solution**:
```powershell
# Apply migration manually with verbose logging
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --verbose
```

**Expected Outcome**: Migration executes successfully, NULL values get default values, API returns 200.

---

#### Scenario B: NULL Values Found in Database

**Meaning**: Migration recorded in history but UPDATE statement didn't execute.

**Root Cause**: SQL UPDATE failed or was skipped for unknown reason.

**Solution**:
```sql
-- Run manual SQL fix script
-- Location: scripts/fix-badge-nulls-manual.sql
-- Execute via Azure Query Editor or psql

-- Review BEFORE/AFTER counts
-- COMMIT only if validation shows ALL_VALID
```

**Expected Outcome**: All NULL values replaced with defaults, API returns 200.

---

#### Scenario C: NO NULL Values Found

**Meaning**: NULL hypothesis is **WRONG**. Something else is causing 500 error.

**Root Cause**: Unknown - requires actual exception message.

**Investigation Steps**:

1. **Check API Response Body** (from diagnostic script output)
   - Look for exception type and message
   - Check stack trace if available

2. **Capture Real-Time Logs** (while triggering error)
   ```bash
   # Terminal 1: Stream logs
   az containerapp logs show --name lankaconnect-staging --resource-group LankaConnect-rg --follow

   # Terminal 2: Trigger error
   curl https://lankaconnect-staging.azurewebsites.net/api/badges
   ```

3. **Review EF Core Configuration**
   - File: `src/LankaConnect.Infrastructure/Data/Configurations/BadgeConfiguration.cs`
   - Look for: `OwnsOne()` configurations for `ListingConfig`, `FeaturedConfig`, `DetailConfig`
   - Verify: All properties marked as `.IsRequired()` or have default values

4. **Review Domain Value Object**
   - File: `src/LankaConnect.Domain/Badges/BadgeLocationConfig.cs`
   - Check: Constructor validation logic
   - Possibility: Constructor throwing exception on edge case values (e.g., 0.0m causing validation failure)

5. **Test Locally Against Staging Database**
   ```json
   // appsettings.Development.json
   {
     "ConnectionStrings": {
       "DefaultConnection": "<staging-connection-string>"
     }
   }
   ```

   ```powershell
   # Run locally - gives better stack traces
   dotnet run --project src/LankaConnect.API

   # Test endpoint
   curl http://localhost:5000/api/badges
   ```

**Possible Alternative Root Causes**:
- **BadgeLocationConfig validation**: Constructor may reject default values (0.0, 1.0) as invalid
- **EF Core mapping issue**: Value object not hydrating correctly from database
- **Authorization failure**: User context not available during query
- **Database constraint violation**: Unrelated foreign key or check constraint
- **Deployment timing**: Defensive code not deployed yet (migration deployed first)

---

#### Scenario D: API Returns 200

**Meaning**: Issue was resolved by migration or defensive code deployment.

**Root Cause**: NULL values were the problem, now fixed.

**Verification**:
1. Test frontend Badge Management UI
2. Test Badge Assignment UI
3. Verify badge display on Events Listing, Featured Banner, Event Detail
4. Monitor for 24 hours to ensure stability

---

## Emergency Manual Fix (If All Else Fails)

If diagnostics don't reveal root cause and error persists:

### Option 1: Force Redeploy Application Code

Ensures defensive null handling code (Phase 6A.31b) is deployed:

```yaml
# .github/workflows/deploy-staging.yml
# Add environment variable to force rebuild
env:
  FORCE_REBUILD: "true"
  BUILD_VERSION: ${{ github.sha }}
```

```powershell
# Trigger deployment
git commit --allow-empty -m "fix: Force redeploy defensive Badge null handling"
git push origin develop
```

### Option 2: Add EF Core Interceptor for Null Safety

Create a query interceptor to catch null hydration issues:

```csharp
// src/LankaConnect.Infrastructure/Data/Interceptors/BadgeNullSafetyInterceptor.cs
public class BadgeNullSafetyInterceptor : IInterceptor
{
    public void Hydrating(object entity, HydratingEventData eventData)
    {
        if (entity is Badge badge)
        {
            // Force defaults if null
            badge.ListingConfig ??= BadgeLocationConfig.DefaultListing;
            badge.FeaturedConfig ??= BadgeLocationConfig.DefaultFeatured;
            badge.DetailConfig ??= BadgeLocationConfig.DefaultDetail;
        }
    }
}
```

### Option 3: Temporary API Fallback

Add try-catch in GetBadgesQueryHandler to return empty list on exception:

```csharp
// TEMPORARY - for emergency stabilization only
try
{
    var badges = await _badgeRepository.GetAllActiveAsync(cancellationToken);
    // ... existing logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Badge query failed - returning empty list as fallback");
    return Result<IReadOnlyList<BadgeDto>>.Success(new List<BadgeDto>());
}
```

---

## Post-Resolution Checklist

After resolving the 500 error:

- [ ] Confirm API returns HTTP 200 for GET /api/badges
- [ ] Verify badge count matches expected number
- [ ] Test Badge Management UI (Admin + EventOrganizer roles)
- [ ] Test Badge Assignment UI
- [ ] Verify badge display on all 3 locations (Listing, Featured, Detail)
- [ ] Monitor Azure Application Insights for recurring errors
- [ ] Document actual root cause in Phase 6A.31c summary
- [ ] Update migration strategy if needed
- [ ] Consider adding integration tests for Badge query scenarios

---

## Files Reference

### Diagnostic Scripts
- `scripts/diagnose-badge-500-error.ps1` - Automated diagnostic suite
- `scripts/fix-badge-nulls-manual.sql` - Manual SQL fix for NULL values

### Source Code
- `src/LankaConnect.Application/Badges/DTOs/BadgeMappingExtensions.cs` - Defensive null handling
- `src/LankaConnect.Application/Badges/Queries/GetBadges/GetBadgesQueryHandler.cs` - Query handler
- `src/LankaConnect.Domain/Badges/Badge.cs` - Domain entity
- `src/LankaConnect.Domain/Badges/BadgeLocationConfig.cs` - Value object
- `src/LankaConnect.Infrastructure/Data/Repositories/BadgeRepository.cs` - Repository
- `src/LankaConnect.Infrastructure/Data/Configurations/BadgeConfiguration.cs` - EF Core config

### Migrations
- `20251217175941_EnforceBadgeLocationConfigNotNull` - Schema-only migration (NOT NULL constraints)
- `20251217205258_FixBadgeNullsDataOnly` - Data-only migration (UPDATE with COALESCE)

---

## Decision Tree Summary

```
START: Badge API returns HTTP 500

├─ Migration NOT in __EFMigrationsHistory?
│  └─ YES → Run: dotnet ef database update
│
├─ NULL values found in database?
│  └─ YES → Run: scripts/fix-badge-nulls-manual.sql
│
├─ NO NULL values found?
│  └─ YES → Investigate exception from logs/API response
│            - Check EF Core configuration
│            - Check BadgeLocationConfig validation
│            - Test locally against staging DB
│            - Review deployment timing
│
└─ API returns 200?
   └─ YES → Verify frontend, monitor stability

END: Issue resolved
```

---

## Contact & Support

If this guide doesn't resolve the issue:

1. Capture FULL diagnostic script output
2. Capture FULL Azure container log output during error
3. Capture API response body with exception details
4. Review code deployment vs migration deployment timing
5. Consult Phase 6A Master Index for historical context

**Remember**: DO NOT create more migrations until root cause is confirmed!
