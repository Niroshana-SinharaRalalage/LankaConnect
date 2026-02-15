# Root Cause Analysis: Issue #71 Email Template Deployment Failures

**Date**: 2026-02-14
**Phase**: 6A.110
**Issue Type**: Database Migration + Dependency Injection
**Severity**: High (Production Deployment Blocked)
**Status**: ✅ **RESOLVED**

---

## Executive Summary

GitHub Issue #71 requested removal of "reply to this email" text from all email templates since the sender is DoNotReply@lankaconnect.app. The implementation required 3 deployment attempts due to cascading issues:

1. **Deployment 1 Failed**: DI lifetime mismatch in EnumSyncValidator (Issue #78 validator)
2. **Deployment 2 Failed**: Migration used wrong database column names
3. **Deployment 3 Succeeded**: All issues resolved

---

## Problem Statement

### Original Issue #71
- **Reporter**: User feedback
- **Symptom**: Email templates contain "feel free to reply to this email" text
- **Actual Sender**: DoNotReply@lankaconnect.app (cannot receive replies)
- **Impact**: Misleading user experience, users may attempt to reply and never get response
- **Expected Fix**: Replace with support contact (support@lankaconnect.app)

---

## Implementation Timeline

### Deployment Attempt 1 (Run ID 22018984484) - FAILED
**Date**: 2026-02-14 14:23:57
**Duration**: 10m20s
**Status**: ❌ **FAILED** at "Run EF Migrations"

**Changes Committed**:
- Created `20260214142053_Phase6A110_RemoveReplyToEmailFromDoNotReplyTemplates.cs`
- SQL REPLACE operations to update email templates
- Used column names `text_body` and `html_body`

**Failure Reason**: DI Lifetime Mismatch
```
System.InvalidOperationException: Cannot consume scoped service 'LankaConnect.Application.Common.Interfaces.IApplicationDbContext' from singleton 'Microsoft.Extensions.Hosting.IHostedService'.
```

**Root Cause**: `EnumSyncValidator` (created for Issue #78) was registered as `IHostedService` (singleton lifetime) but directly injected `IApplicationDbContext` (scoped lifetime). EF Core requires DbContext to be scoped to prevent thread-safety issues and ensure proper disposal.

---

### Deployment Attempt 2 (Run ID 22020204649) - FAILED
**Date**: 2026-02-14 15:57:09
**Duration**: 10m34s
**Status**: ❌ **FAILED** at "Run EF Migrations"

**Changes Committed**:
- Fixed `EnumSyncValidator.cs` to inject `IServiceProvider` instead of `IApplicationDbContext`
- Created service scope in `StartAsync()` to resolve scoped service:
  ```csharp
  using var scope = _serviceProvider.CreateScope();
  var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
  await ValidateEventCategoryAsync(dbContext, cancellationToken);
  ```

**Failure Reason**: Wrong Database Column Names
```
Npgsql.PostgresException (0x80004005): 42703: column "text_body" does not exist
```

**Root Cause**: Migration SQL used `text_body` and `html_body` column names, but actual database schema uses `text_template` and `html_template`.

**Investigation**:
1. Checked `EmailTemplate.cs` entity - properties are `TextTemplate` and `HtmlTemplate`
2. Checked `EmailTemplateConfiguration.cs` - columns mapped with:
   ```csharp
   builder.Property(e => e.TextTemplate)
       .HasColumnName("text_template")
       .HasColumnType("text");

   builder.Property(e => e.HtmlTemplate)
       .HasColumnName("html_template")
       .HasColumnType("text");
   ```
3. Conclusion: Migration used incorrect column names (assumed `_body` suffix instead of actual `_template` suffix)

---

### Deployment Attempt 3 (Run ID 22020397578) - ✅ SUCCESS
**Date**: 2026-02-14 16:11:26
**Duration**: 8m47s
**Status**: ✅ **SUCCESS**

**Changes Committed**:
- Fixed migration to use correct column names:
  - `text_body` → `text_template`
  - `html_body` → `html_template`

**All Steps Passed**:
- ✅ Set up job
- ✅ Build application
- ✅ Run unit tests
- ✅ Azure Login
- ✅ Build Docker image
- ✅ Push Docker image
- ✅ **Run EF Migrations** ← Critical step that previously failed
- ✅ Verify Database Schema
- ✅ Update Container App
- ✅ Smoke Test - Health Check
- ✅ Smoke Test - Entra Endpoint
- ✅ Deployment Summary

---

## Technical Details

### Issue 1: DI Lifetime Mismatch

**Incorrect Pattern**:
```csharp
public class EnumSyncValidator : IHostedService  // Singleton
{
    private readonly IApplicationDbContext _dbContext;  // Scoped - WRONG!

    public EnumSyncValidator(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;  // Lifetime violation!
    }
}
```

**Correct Pattern** (Follows Microsoft Best Practices):
```csharp
public class EnumSyncValidator : IHostedService  // Singleton
{
    private readonly IServiceProvider _serviceProvider;  // ✓ Singleton can inject IServiceProvider

    public EnumSyncValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Create scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        await ValidateEventCategoryAsync(dbContext, cancellationToken);

        // Scope disposes DbContext automatically
    }
}
```

**Why This Matters**:
- `IHostedService` runs for the lifetime of the application (singleton)
- `DbContext` must be scoped because:
  - Each request should get its own instance
  - DbContext is not thread-safe
  - DbContext must be disposed after each operation to release connections
- `IServiceProvider.CreateScope()` creates a new dependency injection scope
- Scoped services can be resolved within a created scope
- Scope disposal automatically disposes all scoped services (including DbContext)

---

### Issue 2: Incorrect Column Names

**Database Schema** (from `EmailTemplateConfiguration.cs`):
```csharp
public void Configure(EntityTypeBuilder<EmailTemplate> builder)
{
    builder.ToTable("email_templates", "communications");

    builder.Property(e => e.TextTemplate)
        .HasColumnName("text_template")  // ← Actual column name
        .HasColumnType("text")
        .IsRequired();

    builder.Property(e => e.HtmlTemplate)
        .HasColumnName("html_template")  // ← Actual column name
        .HasColumnType("text");
}
```

**Incorrect Migration SQL** (Deployment 1 & 2):
```sql
UPDATE communications.email_templates
SET text_body = REPLACE(text_body, ...)  -- ❌ Wrong column name
WHERE text_body LIKE '%reply to this email%';

UPDATE communications.email_templates
SET html_body = REPLACE(html_body, ...)  -- ❌ Wrong column name
WHERE html_body LIKE '%reply to this email%';
```

**Correct Migration SQL** (Deployment 3):
```sql
UPDATE communications.email_templates
SET text_template = REPLACE(text_template, ...)  -- ✓ Correct column name
WHERE text_template LIKE '%reply to this email%';

UPDATE communications.email_templates
SET html_template = REPLACE(html_template, ...)  -- ✓ Correct column name
WHERE html_template LIKE '%reply to this email%';
```

**Text Replacements Performed**:
1. `'Questions? Reply to this email or visit our support page.'` → `'Questions? Contact us at support@lankaconnect.app or visit our support page.'`
2. `'If you have any follow-up questions, please reply to this email.'` → `'If you have any questions, please contact us at support@lankaconnect.app.'`
3. `'please reply to this email'` → `'please contact us at support@lankaconnect.app'`
4. `'Reply to this email'` → `'Contact us at support@lankaconnect.app'`

**HTML Template Replacements**:
- Same replacements but with HTML anchor tags for support email:
  ```html
  <a href="mailto:support@lankaconnect.app" style="color: #FF7900;">support@lankaconnect.app</a>
  ```

---

## Files Modified

### 1. EnumSyncValidator.cs (Issue #78 DI Fix)
**File**: `src/LankaConnect.Infrastructure/Services/Validation/EnumSyncValidator.cs`

**Changes**:
- Line 1: Added `using Microsoft.Extensions.DependencyInjection;`
- Line 17-18: Changed from `IApplicationDbContext _dbContext` to `IServiceProvider _serviceProvider`
- Line 20-25: Updated constructor parameter
- Line 34-36: Added scope creation in `StartAsync()`
- Line 56: Updated method signature to accept `IApplicationDbContext` parameter

**Commit**: `36c7a20b` - "fix(validation): Fix DI lifetime issue in EnumSyncValidator (Phase 6A.110)"

### 2. Phase6A110 Migration (Issue #71 Column Name Fix)
**File**: `src/LankaConnect.Infrastructure/Data/Migrations/20260214142053_Phase6A110_RemoveReplyToEmailFromDoNotReplyTemplates.cs`

**Changes**:
- All instances of `text_body` → `text_template` (20 replacements)
- All instances of `html_body` → `html_template` (20 replacements)

**Commit**: `e2d5cca8` - "fix(migration): Use correct column names (text_template/html_template) in Phase6A110 (Issue #71)"

---

## Lessons Learned

### 1. Always Verify Database Schema Before Writing Migrations

**Problem**: Assumed column names without checking actual schema.

**Prevention**:
- Always check `EntityTypeConfiguration` classes before writing raw SQL migrations
- Use EF Core model snapshot as reference
- Consider querying staging database to verify schema if unsure

**Better Approach**:
```bash
# Check configuration
cat src/LankaConnect.Infrastructure/Data/Configurations/EmailTemplateConfiguration.cs | grep HasColumnName

# Or query database
psql -d lankaconnect -c "\d communications.email_templates"
```

### 2. Respect Dependency Injection Lifetime Rules

**Problem**: Singleton service cannot directly inject scoped service.

**Key Principles**:
- Singleton → can inject → Singleton, IServiceProvider
- Scoped → can inject → Scoped, Singleton, IServiceProvider
- Transient → can inject → Any

**Pattern for Background Services**:
```csharp
// ✓ Correct: Singleton injects IServiceProvider, creates scope for scoped services
public class MyBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MyBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();

        // Use dbContext...

        // Scope disposes dbContext automatically when using block exits
    }
}
```

### 3. Test Migrations Locally Before Deploying

**Problem**: Migration failures discovered during Azure deployment.

**Prevention**:
```bash
# Always test locally first
dotnet ef database update --project src/LankaConnect.Infrastructure --context AppDbContext

# Verify migration applied
psql -d lankaconnect -c "SELECT * FROM __EFMigrationsHistory ORDER BY migration_id DESC LIMIT 5;"

# Check affected data
psql -d lankaconnect -c "SELECT name, text_template FROM communications.email_templates WHERE text_template LIKE '%support@lankaconnect%' LIMIT 3;"
```

### 4. Use Nested REPLACE Carefully

**Pattern Used**:
```sql
SET column = REPLACE(
    REPLACE(
        REPLACE(
            REPLACE(column, 'pattern1', 'replacement1'),
            'pattern2', 'replacement2'
        ),
        'pattern3', 'replacement3'
    ),
    'pattern4', 'replacement4'
)
```

**Considerations**:
- Nested REPLACE executes inner-to-outer
- Order matters if patterns overlap
- Test with sample data first
- Consider using REGEXP_REPLACE for complex patterns

### 5. Cascading Fixes Create Risk

**Timeline**:
- Fix Issue #78 → Introduced DI bug
- Fix DI bug → Discovered migration bug
- Fix migration bug → Success (3rd attempt)

**Better Approach**:
- Write comprehensive unit tests for new validators
- Test migrations in local environment before committing
- Use integration tests that exercise full startup process
- Deploy validators and migrations separately when possible

---

## Verification Checklist

After Deployment 3 Success:

- [x] All GitHub Actions steps passed
- [x] EF Migrations completed successfully
- [x] Container app updated and running
- [x] Health check passed
- [x] Smoke tests passed
- [ ] Manual verification of email template content (pending - requires API query or database access)
- [ ] Send test email to verify no "reply to this email" text appears
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update STREAMLINED_ACTION_PLAN.md
- [ ] Close GitHub Issue #71

---

## Related Issues

- **GitHub Issue #71**: Email templates say "reply to this email" but sender is DoNotReply@lankaconnect.app
- **GitHub Issue #78**: Festival filter error (caused EnumSyncValidator creation which introduced DI bug)
- **Phase 6A.109**: EventCategory enum sync fix
- **Phase 6A.110**: Email template "reply to this email" text removal

---

## Deployment Summary

| Attempt | Run ID | Status | Failure Point | Root Cause | Fix |
|---------|--------|--------|---------------|------------|-----|
| 1 | 22018984484 | ❌ Failed | Run EF Migrations | DI lifetime mismatch in EnumSyncValidator | Inject IServiceProvider, create scope |
| 2 | 22020204649 | ❌ Failed | Run EF Migrations | Column "text_body" does not exist | Use correct names (text_template/html_template) |
| 3 | 22020397578 | ✅ Success | - | - | All issues resolved |

**Total Time to Resolution**: ~2 hours
**Commits**: 3 (1 migration + 2 fixes)
**Deployments**: 3 attempts
**Lines Changed**: ~50 lines total

---

## Conclusion

This RCA demonstrates the importance of:
1. Verifying database schema before writing migrations
2. Understanding .NET dependency injection lifetimes
3. Testing migrations locally before deploying
4. Systematic debugging when deployments fail
5. Comprehensive logging to quickly identify failures

The cascading nature of the fixes (Issue #78 validator → DI bug → migration bug) highlights the need for thorough testing at each step, especially when adding infrastructure components like startup validators.

---

**RCA Prepared By**: Claude Sonnet 4.5
**Date**: 2026-02-14
**Deployment URLs**:
- Failed Deployment 1: https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions/runs/22018984484
- Failed Deployment 2: https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions/runs/22020204649
- Successful Deployment 3: https://github.com/Niroshana-SinharaRalalage/LankaConnect/actions/runs/22020397578
