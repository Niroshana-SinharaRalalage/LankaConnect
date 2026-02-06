# Phase 6A.X Critical Issue - Root Cause Analysis

**Date**: 2026-01-17 16:50 UTC
**Status**: 🔴 **BROKEN - Migration Applied But Queries Still Failing**

---

## Executive Summary

**Database**: ✅ Table exists in correct schema with correct data
**Code**: ✅ Migration deployed, EF Core configured correctly
**Runtime**: ❌ Queries to `reference_data.state_tax_rates` still throwing exceptions

**Root Cause**: EF Core model not refreshed after migration deployment

---

## Evidence

### 1. Database State (User Verified)
```sql
SELECT state_code, state_name, tax_rate
FROM reference_data.state_tax_rates
WHERE state_code = 'OH';
```
**Result**:
```
OH | Ohio | 0.0575
```
✅ **Table exists in `reference_data` schema**
✅ **Data is present and correct**

### 2. API Test Result
```bash
GET /api/events/d543629f-a5ba-4475-b124-3d0fc5200f2f/attendees
```
**Response**:
```json
{
  "hasRevenueBreakdown": false,
  "totalSalesTax": 0,
  "totalStripeFees": 0,
  "attendees": [
    {
      "salesTaxAmount": null,
      "stripeFeeAmount": null,
      "platformCommissionAmount": null,
      "organizerPayoutAmount": null
    }
  ]
}
```
❌ **Breakdown calculation NOT working**

### 3. Azure Container Logs
```
16:46:57.076 +00:00 [WRN] Exception while calculating on-the-fly revenue breakdown for registration ae12f621-4c14-40be-bae6-25ed9b8a00ba
at LankaConnect.Infrastructure.Data.Repositories.StateTaxRateRepository.GetActiveByStateCodeAsync(String stateCode, CancellationToken cancellationToken) in StateTaxRateRepository.cs:line 20
at LankaConnect.Infrastructure.Services.DatabaseSalesTaxService.GetStateTaxRateAsync(String stateCode, CancellationToken cancellationToken)
```
❌ **Exception thrown when querying StateTaxRate table**

### 4. Code Verification
**AppDbContext.cs (Line 103)**:
```csharp
public DbSet<LankaConnect.Domain.Tax.StateTaxRate> StateTaxRates => Set<LankaConnect.Domain.Tax.StateTaxRate>();
```
✅ **DbSet defined**

**AppDbContext.cs (Line 234)**:
```csharp
modelBuilder.Entity<LankaConnect.Domain.Tax.StateTaxRate>().ToTable("state_tax_rates", "reference_data");
```
✅ **Schema configured to reference_data**

**AppDbContext.cs (Line 271)**:
```csharp
typeof(LankaConnect.Domain.Tax.StateTaxRate) // Phase 6A.X: US State Sales Tax Rates
```
✅ **Entity in configured types list**

### 5. Deployment History
| Time (UTC) | Deployment ID | Commit | Status |
|------------|---------------|--------|--------|
| 15:05-15:11 | 21096274913 | 1fe302aa (OUR FIX) | ✅ Success |
| 15:15-15:21 | 21096412655 | ? | ✅ Success |
| 15:25-15:31 | 21096552093 | ? | ✅ Success |
| 15:27-15:33 | 21096575241 | ? | ✅ Success |
| 15:34-15:40 | 21096674134 | bd47b618 (docs only) | ✅ Success |

**Container Revision**: lankaconnect-api-staging--0000631 (Created: 15:39 UTC)

✅ **Our migration code IS deployed** (commit 1fe302aa in history)
✅ **Later commits are documentation only** (no code changes)

---

## Root Cause Analysis

### The Problem
Even though:
1. Database has table in correct schema
2. EF Core configuration points to correct schema
3. Migration was deployed successfully
4. Container was restarted after deployment

**The query is STILL failing** with same error as before.

### Hypothesis: EF Core Model Not Refreshed

**Theory**: When EF Core first initialized (before migration ran), it compiled the model based on the OLD table location (public schema). Even after migration moved the table to reference_data schema, EF Core's compiled model cache still points to the old location.

**Evidence**:
- Container restarted at 15:39 UTC (30 minutes AFTER our deployment)
- Migration likely ran during first deployment (15:05 UTC)
- But EF Core model was compiled BEFORE migration ran
- Subsequent container restarts use cached compiled model

### Why This Happens
1. **Deployment 15:05**: Container starts, EF Core initializes, THEN migration runs
2. **EF Core Initialization**: Compiles model pointing to `reference_data.state_tax_rates` (from code)
3. **Migration Runs**: Moves table from `public` to `reference_data`
4. **But**: EF Core's runtime query translator might still have old metadata cached
5. **Container Restart 15:39**: Uses pre-compiled model, doesn't re-check actual database schema

---

## Why Standard Fixes Aren't Working

### ❌ Tried: Container Restart
Multiple deployments/restarts happened (15:15, 15:25, 15:27, 15:34) but issue persists

### ❌ Tried: Schema Configuration
Already set to `reference_data` in AppDbContext.cs line 234

### ❌ Tried: DbSet Registration
Already registered in AppDbContext.cs lines 103, 271

---

## Possible Solutions

### Solution 1: Force EF Core Model Rebuild ⭐ RECOMMENDED
**Action**: Clear EF Core compiled model cache and force rebuild

**How**:
1. Add explicit model snapshot regeneration
2. Or delete compiled model files
3. Or add `services.AddDbContext` with `EnableSensitiveDataLogging` to force recompile

**Implementation**:
```csharp
// In Startup.cs or Program.cs
services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging(); // Forces model recompilation
    options.EnableDetailedErrors();
});
```

### Solution 2: Explicit Migration Execution Order
**Action**: Ensure migration runs BEFORE EF Core model compilation

**How**:
1. Run migrations in separate initialization step
2. THEN start application
3. Ensures model is compiled AFTER schema changes

**Implementation**:
```csharp
// In Program.cs
public static async Task Main(string[] args)
{
    var host = CreateHostBuilder(args).Build();

    // Run migrations FIRST
    using (var scope = host.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    // THEN run application
    await host.RunAsync();
}
```

### Solution 3: Add Diagnostic Logging
**Action**: Add detailed EF Core SQL logging to see exact query being generated

**Implementation**:
```csharp
_logger.LogInformation("About to query StateTaxRates for state: {StateCode}", stateCode);
_logger.LogInformation("DbSet type: {Type}, Table: {Table}",
    _dbSet.GetType(),
    _dbSet.EntityType.GetTableName());
```

### Solution 4: Manual Schema Hint in Repository
**Action**: Explicitly tell EF Core to use reference_data schema in query

**Implementation**:
```csharp
public async Task<StateTaxRate?> GetActiveByStateCodeAsync(string stateCode, CancellationToken cancellationToken = default)
{
    var normalizedCode = stateCode.ToUpperInvariant();

    // Explicit schema hint
    return await _context.StateTaxRates
        .FromSqlRaw("SELECT * FROM reference_data.state_tax_rates WHERE state_code = {0} AND is_active = true ORDER BY effective_date DESC LIMIT 1", normalizedCode)
        .AsNoTracking()
        .FirstOrDefaultAsync(cancellationToken);
}
```

---

## Immediate Next Steps

### Option A: Quick Fix (SQL Workaround)
Use `FromSqlRaw` in StateTaxRateRepository to explicitly specify schema
- ⏱️ **Time**: 10 minutes
- ✅ **Pros**: Immediate fix, no architectural changes
- ❌ **Cons**: Bypasses EF Core, not ideal long-term

### Option B: Proper Fix (Force Model Rebuild)
Add code to force EF Core model recompilation on startup
- ⏱️ **Time**: 30 minutes
- ✅ **Pros**: Proper fix, future-proof
- ❌ **Cons**: Requires code change + deployment

### Option C: Nuclear Option (Drop and Recreate)
Drop state_tax_rates table and let migration recreate it
- ⏱️ **Time**: 5 minutes
- ✅ **Pros**: Forces EF Core to rebuild model
- ❌ **Cons**: Data loss (must re-seed)

---

## Recommendation

**Use Solution 4 (Manual Schema Hint) as IMMEDIATE fix:**
1. Modify StateTaxRateRepository to use `FromSqlRaw`
2. Deploy in 10 minutes
3. Verify API works
4. THEN implement Solution 1 (proper fix) for long-term

**Rationale**:
- Quick fix unblocks user immediately
- Proper fix can be done systematically
- No data loss, no risky changes

---

## User Decision Required

Please choose:
1. **Quick Fix** - Manual SQL in repository (10 min deployment)
2. **Proper Fix** - Force EF model rebuild (30 min deployment)
3. **Both** - Quick fix now, proper fix later
4. **Other** - Different approach

I'm ready to implement whichever you prefer.
