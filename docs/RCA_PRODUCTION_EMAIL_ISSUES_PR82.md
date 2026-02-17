# Root Cause Analysis: Production Email Issues After PR #82 Merge

**Date**: 2026-02-16
**Severity**: 🔴 **CRITICAL - Production Database Migration Failure**
**PR**: #82 (Phase 6A.116 & 6A.117 Email Fixes)
**Status**: 🚨 **ACTIVE INCIDENT - Multiple Issues**

---

## Executive Summary

PR #82 was successfully merged to `main` (commit `afe370c3`) containing Phase 6A.116 and 6A.117 email template fixes. However, production environment is experiencing **THREE DISTINCT ISSUES**:

1. **Database Migration Failure** - Migrations NOT applied to production database
2. **Data Mapping Bug** - Wrong quantities shown in signup update emails
3. **Performance Degradation** - Slow signup list operations

**Critical Finding**: Code is deployed, but database migrations were NOT executed, causing Issues #1. Issue #2 is unrelated to migrations. Issue #3 requires separate investigation.

---

## Issue #1: Database Migrations NOT Applied to Production

### Evidence

**Git History Shows Migrations Exist:**
```bash
Commit afe370c3: Merge PR #82 to main
├── d1468c37: Phase6A117_FixEmailTemplateTextAndLayout migration
└── 23f818ae: Phase6A116_FixEmailTemplateHtmlRendering migration
```

**User Screenshots Show:**
- Email footer still has "feel free to reply" text (should be removed by Phase6A117)
- Email formatting still broken (should be fixed by Phase6A116)

**Conclusion**: Migrations are IN code repository but NOT applied to production database.

---

### Root Cause Analysis - Issue #1

#### ✅ VERIFICATION: Deployment Workflow HAS Migration Step

**File**: `.github/workflows/deploy-production.yml` Lines 114-151

```yaml
- name: Run EF Core migrations
  run: |
    dotnet tool install -g dotnet-ef --version 8.0.0 2>/dev/null || dotnet tool update -g dotnet-ef --version 8.0.0

    DB_CONNECTION=$(az keyvault secret show \
      --vault-name ${{ env.KEY_VAULT_NAME }} \
      --name database-connection-string \
      --query value -o tsv)

    echo "Applying pending migrations to production database..."
    cd src/LankaConnect.API

    MAX_RETRIES=3
    RETRY_COUNT=0
    SUCCESS=false

    while [ $RETRY_COUNT -lt $MAX_RETRIES ] && [ "$SUCCESS" = false ]; do
      RETRY_COUNT=$((RETRY_COUNT + 1))
      echo "Migration attempt $RETRY_COUNT of $MAX_RETRIES..."

      if dotnet ef database update \
        --connection "$DB_CONNECTION" \
        --project ../LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj \
        --context AppDbContext \
        --verbose; then
        SUCCESS=true
        echo "✅ Migrations completed successfully"
      else
        if [ $RETRY_COUNT -lt $MAX_RETRIES ]; then
          DELAY=$((10 * (2 ** ($RETRY_COUNT - 1))))
          echo "⚠️  Migration failed. Retrying in ${DELAY}s..."
          sleep $DELAY
        else
          echo "❌ Migration failed after $MAX_RETRIES attempts"
          exit 1
        fi
      fi
    done
```

**Analysis**:
- ✅ Workflow DOES have migration step (Phase 3, Lines 114-151)
- ✅ Uses `dotnet ef database update` with retry logic
- ✅ Connects to production database via Key Vault
- ✅ Runs BEFORE container deployment (Phase 4)
- ✅ Has verbose logging enabled
- ✅ Has 3 retry attempts with exponential backoff

**Conclusion**: Workflow configuration is CORRECT. Migration step exists and is properly configured.

---

#### 🔍 HYPOTHESIS 1: Migration Discovery Failure

**Problem**: EF Core might not be discovering migrations at runtime.

**Evidence to Check**:
1. Migration files have correct namespace: `LankaConnect.Infrastructure.Data.Migrations` ✅
2. Migration class inherits from `Migration` ✅
3. Designer files exist (.Designer.cs) - **NEED TO VERIFY**
4. Designer files have `[Migration]` attribute - **NEED TO VERIFY**

**Migration File Structure (Phase6A116)**:
```csharp
namespace LankaConnect.Infrastructure.Data.Migrations
{
    public partial class Phase6A116_FixEmailTemplateHtmlRendering : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(html_template, '{{ResponseSummary}}', '{{{ResponseSummary}}}'),
                    updated_at = NOW()
                WHERE name IN (
                    'template-form-response-confirmation',
                    'template-form-response-update',
                    'template-form-response-cancellation',
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update'
                );
            ");
        }
```

**Migration File Structure (Phase6A117)**:
```csharp
namespace LankaConnect.Infrastructure.Data.Migrations
{
    public partial class Phase6A117_FixEmailTemplateTextAndLayout : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix Issue #10: Remove "feel free to reply" text
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(html_template, '<br />If you have questions, feel free to reply to this email.', ''),
                    updated_at = NOW()
                WHERE name IN (
                    'template-event-registration-cancellation',
                    'template-event-reminder',
                    'template-signup-list-commitment-update'
                )
                AND html_template LIKE '%If you have questions, feel free to reply to this email.%';
            ");

            // Fix Issues #11 & #12: Remove empty PICKUP/DELIVERY card
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(html_template, '<!-- PICKUP/DELIVERY CARD -->[\s\S]*?</table>[\s\S]*?<!--\[if mso\]>[\s\S]*?<!\[endif\]-->[\s\S]*?</td>[\s\S]*?</tr>', '', 'g'),
                    updated_at = NOW()
                WHERE name IN (
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update'
                )
                AND html_template LIKE '%PICKUP/DELIVERY CARD%';
            ");
        }
```

**Verification Required**:
- [ ] Check if `.Designer.cs` files exist for both migrations
- [ ] Check if `[Migration("20260216033407")]` attribute exists
- [ ] Check GitHub Actions logs for migration execution

---

#### 🔍 HYPOTHESIS 2: Migration Execution Failed Silently

**Problem**: Migration command ran but failed without stopping deployment.

**Evidence to Check**:
- GitHub Actions workflow logs for PR #82 merge
- Azure Container App logs during deployment
- Database `__EFMigrationsHistory` table

**Verification Required**:
```bash
# Check GitHub Actions logs
gh run list --workflow=deploy-production.yml --limit 5

# Check if migrations are in history table
psql $DB_CONNECTION -c "SELECT * FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '%Phase6A116%' OR \"MigrationId\" LIKE '%Phase6A117%';"
```

**Expected if migrations applied**:
```
MigrationId                                          | ProductVersion
-----------------------------------------------------|----------------
20260216033407_Phase6A116_FixEmailTemplateHtmlRendering | 8.0.0
20260216181052_Phase6A117_FixEmailTemplateTextAndLayout | 8.0.0
```

**If NOT found**: Migrations were never executed.

---

#### 🔍 HYPOTHESIS 3: Database Connection Issue

**Problem**: Workflow couldn't connect to production database during migration step.

**Evidence to Check**:
- Key Vault secret `database-connection-string` exists and is valid
- Network connectivity from GitHub Actions runner to Azure PostgreSQL
- PostgreSQL firewall rules allow GitHub Actions IPs

**Verification Required**:
```bash
# Validate Key Vault secret
az keyvault secret show --vault-name lankaconnect-prod-kv --name database-connection-string --query value -o tsv

# Check if secret is accessible
echo "Secret retrieved: $(az keyvault secret show --vault-name lankaconnect-prod-kv --name database-connection-string --query value -o tsv | cut -c 1-20)..."
```

---

#### 🔍 HYPOTHESIS 4: Wrong Database Context

**Problem**: Workflow might be targeting wrong context or database.

**Evidence to Check**:
- Workflow specifies `--context AppDbContext` ✅
- Connection string points to production database ✅

**Verification**: Already confirmed in workflow configuration.

---

### Recommended Investigation Steps - Issue #1

**Priority 1: Check GitHub Actions Logs**
```bash
# Get latest production deployment run
gh run list --workflow=deploy-production.yml --limit 1 --json databaseId,url,conclusion

# Download logs for migration step
gh run view <run-id> --log
```

**Priority 2: Check Database Migration History**
```sql
-- Connect to production database
SELECT * FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 10;

-- Check if Phase6A116/117 exist
SELECT * FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A116%' OR "MigrationId" LIKE '%Phase6A117%';
```

**Priority 3: Check Email Template State**
```sql
-- Verify if templates still have old format
SELECT
    name,
    CASE
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'OLD FORMAT (Bug)'
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'NEW FORMAT (Fixed)'
        ELSE 'No ResponseSummary'
    END as format_status,
    CASE
        WHEN html_template LIKE '%feel free to reply%' THEN 'HAS TEXT (Bug)'
        ELSE 'NO TEXT (Fixed)'
    END as reply_text_status
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-event-registration-cancellation',
    'template-event-reminder'
);
```

---

### Fix Plan - Issue #1

**Option A: Manual Migration Application (IMMEDIATE FIX)**

```bash
# 1. Connect to production database
az login
DB_CONNECTION=$(az keyvault secret show \
  --vault-name lankaconnect-prod-kv \
  --name database-connection-string \
  --query value -o tsv)

# 2. Apply migrations manually
cd src/LankaConnect.API
dotnet ef database update \
  --connection "$DB_CONNECTION" \
  --project ../LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj \
  --context AppDbContext \
  --verbose

# 3. Verify migrations applied
psql "$DB_CONNECTION" -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\" DESC LIMIT 5;"
```

**Option B: Re-trigger Deployment (AFTER ROOT CAUSE FOUND)**

1. Identify why migrations failed in original deployment
2. Fix underlying issue (network, permissions, etc.)
3. Re-run deployment workflow
4. Verify migrations applied

**Recommendation**: Use Option A for immediate fix, then investigate why original deployment failed.

---

## Issue #2: Wrong Numbers in Signup Update Emails

### Evidence

**User Screenshot Shows:**
```
PREVIOUS QUANTITY: 0
NEW QUANTITY: 0
```

**Expected Behavior:**
User changed quantity from 10 → 5 (visible in screenshot), email should show:
```
PREVIOUS QUANTITY: 10
NEW QUANTITY: 5
```

---

### Root Cause Analysis - Issue #2

#### 🔍 Data Flow Investigation

**Email Handler**: `CommitmentUpdatedEventHandler.cs`

**Line 54-56:**
```csharp
// Phase 6A.121: Support dual nullable fields (PhysicalQuantity or SlotsClaimed)
var oldQuantity = domainEvent.OldPhysicalQuantity ?? domainEvent.OldSlotsClaimed ?? 0;
var newQuantity = domainEvent.NewPhysicalQuantity ?? domainEvent.NewSlotsClaimed ?? 0;
```

**Analysis**:
- Handler reads `OldPhysicalQuantity` and `NewPhysicalQuantity` from `CommitmentUpdatedEvent`
- Falls back to `OldSlotsClaimed` and `NewSlotsClaimed`
- If BOTH are null, defaults to 0 ❌

**Line 91-98:**
```csharp
var emailParams = SignupCommitmentEmailParams.CreateUpdate(
    userId: user.Id,
    userName: user.FirstName,
    userEmail: user.Email.Value,
    eventId: @event.Id,
    eventTitle: @event.Title?.Value ?? "Untitled Event",
    signupItem: domainEvent.ItemDescription,
    quantity: newQuantity,  // ✅ Sets current quantity
    eventStartDate: @event.StartDate,
    timeZoneId: @event.TimeZoneId,
    eventLocation: @event.Location?.ToString() ?? "Location TBD",
    eventDetailsUrl: $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups"
);
```

**Problem Identified**:
- `CreateUpdate()` only sets `quantity` (new quantity)
- Email template needs BOTH `OldQuantity` and `NewQuantity`
- But `emailParams` is never populated with old/new quantity values!

**Missing Code**:
```csharp
// MISSING: Set old and new quantities for email template
emailParams.OldQuantity = oldQuantity;
emailParams.NewQuantity = newQuantity;
```

**Email Params Class**: `SignupCommitmentEmailParams.cs`

**Line 105-112:**
```csharp
/// <summary>
/// New quantity (for update template).
/// </summary>
public int NewQuantity { get; set; }

/// <summary>
/// Old/previous quantity (for update template).
/// </summary>
public int OldQuantity { get; set; }
```

**Line 220-261 (ToDictionary):**
```csharp
public Dictionary<string, object> ToDictionary()
{
    return new Dictionary<string, object>
    {
        // Core params
        { "UserName", UserName },
        { "EventTitle", EventTitle },
        { "ItemDescription", SignupItem },
        { "SignupItem", SignupItem },
        { "Quantity", Quantity },  // Current quantity
        { "EventDateTime", EmailDateTimeHelper.FormatDateTimeWithTz(EventStartDate, TimeZoneId) },
        { "EventLocation", EventLocation },
        { "EventDetailsUrl", EventDetailsUrl },
        { "CommitmentType", CommitmentType },
        { "PickupInstructions", PickupInstructions },

        // ... more params ...

        // Update template params
        { "EventDate", EmailDateTimeHelper.FormatEventDate(EventStartDate, TimeZoneId) },
        { "NewQuantity", NewQuantity },  // ✅ Sent to template
        { "OldQuantity", OldQuantity },  // ✅ Sent to template

        // ... more params ...
    };
}
```

**Analysis**:
- ✅ `SignupCommitmentEmailParams` HAS `OldQuantity` and `NewQuantity` properties
- ✅ `ToDictionary()` DOES send them to template
- ❌ `CommitmentUpdatedEventHandler` NEVER sets these properties!

---

### Root Cause - Issue #2

**BUG LOCATION**: `CommitmentUpdatedEventHandler.cs` Line 91-122

**ROOT CAUSE**: Missing property assignments after creating `emailParams`

**Expected Code** (Lines 91-122):
```csharp
var emailParams = SignupCommitmentEmailParams.CreateUpdate(
    userId: user.Id,
    userName: user.FirstName,
    userEmail: user.Email.Value,
    eventId: @event.Id,
    eventTitle: @event.Title?.Value ?? "Untitled Event",
    signupItem: domainEvent.ItemDescription,
    quantity: newQuantity,
    eventStartDate: @event.StartDate,
    timeZoneId: @event.TimeZoneId,
    eventLocation: @event.Location?.ToString() ?? "Location TBD",
    eventDetailsUrl: $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups"
);

// ❌ MISSING: Set old and new quantities for update template
emailParams.OldQuantity = oldQuantity;
emailParams.NewQuantity = newQuantity;

// Phase 6A.103: Add event image if available
var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
emailParams.WithEventImage(primaryImage?.ImageUrl ?? @event.Images.FirstOrDefault()?.ImageUrl ?? "");
```

**Impact**:
- `OldQuantity` and `NewQuantity` properties remain at default value (0)
- Email template receives `{"OldQuantity": 0, "NewQuantity": 0}`
- User sees "PREVIOUS QUANTITY: 0, NEW QUANTITY: 0" regardless of actual changes

---

### Fix Plan - Issue #2

**Single Line Addition Required**:

**File**: `src/LankaConnect.Application/Events/EventHandlers/CommitmentUpdatedEventHandler.cs`

**After Line 103** (after `emailParams` creation), add:
```csharp
// Set old and new quantities for update email template
emailParams.OldQuantity = oldQuantity;
emailParams.NewQuantity = newQuantity;
```

**Complete Fixed Code** (Lines 91-108):
```csharp
var emailParams = SignupCommitmentEmailParams.CreateUpdate(
    userId: user.Id,
    userName: user.FirstName,
    userEmail: user.Email.Value,
    eventId: @event.Id,
    eventTitle: @event.Title?.Value ?? "Untitled Event",
    signupItem: domainEvent.ItemDescription,
    quantity: newQuantity,
    eventStartDate: @event.StartDate,
    timeZoneId: @event.TimeZoneId,
    eventLocation: @event.Location?.ToString() ?? "Location TBD",
    eventDetailsUrl: $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups"
);

// Set old and new quantities for update email template
emailParams.OldQuantity = oldQuantity;
emailParams.NewQuantity = newQuantity;

// Phase 6A.103: Add event image if available
var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
emailParams.WithEventImage(primaryImage?.ImageUrl ?? @event.Images.FirstOrDefault()?.ImageUrl ?? "");
```

**Test Plan**:
1. Create unit test for `CommitmentUpdatedEventHandler`
2. Mock `CommitmentUpdatedEvent` with `OldPhysicalQuantity = 10`, `NewPhysicalQuantity = 5`
3. Verify email params have `OldQuantity = 10`, `NewQuantity = 5`
4. Test staging environment with actual signup list update
5. Verify email shows correct quantities

---

## Issue #3: Slow Signup List Operations

### Evidence

**User Report**: "Large performance issue with signup operations"

**Context**: This is a NEW issue not covered by Phase 6A.116/117

---

### Investigation Required - Issue #3

#### 🔍 Potential Causes

**Hypothesis 1: N+1 Query Problem**
- Handler might be loading related entities in a loop
- Check `CommitmentUpdatedEventHandler` for multiple repository calls

**Hypothesis 2: Database Index Missing**
- Recent schema changes might have removed indexes
- Check migrations for index operations

**Hypothesis 3: Event Handler Blocking**
- Email sending might be blocking transaction commit
- Check if fail-silent pattern is working correctly

**Hypothesis 4: Database Connection Pool Exhaustion**
- Multiple concurrent operations might exhaust pool
- Check Azure logs for connection timeouts

---

#### 🔍 Data Collection Needed

**Azure Container App Logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --tail 500 \
  --follow
```

**Database Performance Metrics**:
```sql
-- Check slow queries
SELECT
    query,
    calls,
    mean_exec_time,
    max_exec_time
FROM pg_stat_statements
WHERE query LIKE '%SignUpCommitment%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

**Application Insights**:
- Check request duration for `/api/events/{id}/signups` endpoints
- Check dependency duration for database calls
- Look for exceptions or timeouts

---

### Recommended Investigation Steps - Issue #3

**Priority 1: Check Application Logs**
```bash
# Look for slow operations
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --tail 1000 | grep -i "duration\|elapsed\|timeout"
```

**Priority 2: Analyze Handler Performance**
```csharp
// Check CommitmentUpdatedEventHandler.cs line 52
var stopwatch = Stopwatch.StartNew();
// ... handler code ...
stopwatch.Stop();
_logger.LogInformation("Duration={ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
```

**Priority 3: Database Query Analysis**
- Enable EF Core query logging
- Check for N+1 problems
- Verify indexes exist

---

## Summary and Priority

### Issue Priority

1. **Issue #2 (Data Bug)** - 🔴 **CRITICAL (P0)** - Users receiving incorrect data in emails
2. **Issue #1 (Migration Failure)** - 🟠 **HIGH (P1)** - UX issue but not breaking
3. **Issue #3 (Performance)** - 🟡 **MEDIUM (P2)** - Requires investigation to determine severity

### Classification

| Issue | Type | Root Cause | Fix Complexity |
|-------|------|------------|----------------|
| Issue #1 | Database Deployment | Migration not executed | Low (manual migration) |
| Issue #2 | Data Mapping Bug | Missing property assignment | Very Low (2 lines of code) |
| Issue #3 | Performance Issue | Unknown - requires investigation | Unknown |

### Immediate Action Items

**Issue #2 (HIGHEST PRIORITY)**:
- [ ] Add missing property assignments in `CommitmentUpdatedEventHandler.cs`
- [ ] Write unit test to prevent regression
- [ ] Deploy fix to staging
- [ ] Test with actual signup list update
- [ ] Deploy to production

**Issue #1 (IMMEDIATE FIX)**:
- [ ] Check GitHub Actions logs for migration failure
- [ ] Check production database `__EFMigrationsHistory` table
- [ ] Query email templates to verify current state
- [ ] Apply migrations manually if needed
- [ ] Investigate why original deployment failed

**Issue #3 (INVESTIGATION)**:
- [ ] Collect Azure logs for recent signup operations
- [ ] Analyze database query performance
- [ ] Check Application Insights for slow requests
- [ ] Determine if this is related to Phase 6A.121 changes

---

## Testing Strategy

### Issue #1 Verification

```sql
-- After applying migrations, verify:

-- 1. Check migration history
SELECT * FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A116%' OR "MigrationId" LIKE '%Phase6A117%';

-- 2. Verify HTML rendering fix (Phase6A116)
SELECT
    name,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'FIXED ✅'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'BROKEN ❌'
        ELSE 'N/A'
    END as html_status
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update'
);

-- 3. Verify "feel free to reply" removal (Phase6A117)
SELECT
    name,
    CASE
        WHEN html_template LIKE '%feel free to reply%' THEN 'BROKEN ❌'
        ELSE 'FIXED ✅'
    END as text_status
FROM communications.email_templates
WHERE name IN (
    'template-event-registration-cancellation',
    'template-event-reminder',
    'template-signup-list-commitment-update'
);

-- 4. Verify PICKUP/DELIVERY card removal (Phase6A117)
SELECT
    name,
    CASE
        WHEN html_template LIKE '%PICKUP/DELIVERY CARD%' THEN 'BROKEN ❌'
        ELSE 'FIXED ✅'
    END as layout_status
FROM communications.email_templates
WHERE name IN (
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update'
);
```

### Issue #2 Verification

```bash
# Test signup list update flow
curl -X PUT \
  'https://lankaconnect-api-prod-url/api/events/{eventId}/signups/{itemId}/commit' \
  -H 'Authorization: Bearer {token}' \
  -H 'Content-Type: application/json' \
  -d '{
    "quantity": 5,
    "notes": "Updated quantity"
  }'

# Check email received
# Verify shows:
# PREVIOUS QUANTITY: 10
# NEW QUANTITY: 5
```

---

## Lessons Learned

1. **Always verify migrations applied** - Add post-deployment check to workflow
2. **Test email data mapping** - Create integration tests for email handlers
3. **Monitor deployment logs** - Set up alerts for migration failures
4. **Performance regression testing** - Benchmark critical paths before deployment

---

## Next Steps

1. **Immediate**: Fix Issue #2 (data bug) - 2 lines of code
2. **High Priority**: Apply migrations manually for Issue #1
3. **Investigation**: Collect data for Issue #3 (performance)
4. **Long-term**: Add deployment verification script to CI/CD
5. **Documentation**: Update deployment runbook with migration verification steps

---

**Last Updated**: 2026-02-16
**Document Owner**: Senior Engineer / System Architect
**Related PRs**: #82
**Related Phases**: 6A.116, 6A.117
