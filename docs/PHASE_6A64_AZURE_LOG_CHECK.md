# Phase 6A.64 - Azure Container Log Diagnostic Guide

**Status**: API still failing after successful migration and logging deployment
**Last Deployment**: Commit `1244f569` (completed successfully)
**Issue**: Newsletter subscription returns `SUBSCRIPTION_FAILED` (400 Bad Request)

---

## 🔍 Critical Next Step: Check Azure Container Logs

The enhanced logging I added will show **exactly** where the failure occurs. You need to check Azure Container App logs.

### Option 1: Azure Portal (Easiest)

1. Go to: [Azure Portal](https://portal.azure.com)
2. Navigate to: Resource Groups → `lankaconnect-staging`
3. Click: Container Apps → `lankaconnect-api-staging`
4. Go to: "Monitoring" → "Log stream" or "Logs"
5. Filter for: `Phase 6A.64`
6. Make a test API call and watch logs in real-time

### Option 2: Azure CLI

```bash
# Stream logs in real-time
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow \
  --tail 100

# Filter for Phase 6A.64 entries
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow \
  | grep "Phase 6A.64"
```

### Option 3: Application Insights (If Configured)

```kusto
traces
| where timestamp > ago(1h)
| where message contains "Phase 6A.64"
| project timestamp, message, severityLevel
| order by timestamp desc
```

---

## 📋 What to Look For in Logs

When you make this API call:
```bash
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Newsletter/subscribe" \
  -H "Content-Type: application/json" \
  -d '{"email":"azure-log-test@example.com","metroAreaIds":["39111111-1111-1111-1111-111111111001"],"receiveAllLocations":false}'
```

You should see these log entries (in order):

### Expected Log Flow

```
1. [Phase 6A.64] Newsletter subscription START - Email: azure-log-test@example.com, MetroAreaCount: 1, ReceiveAll: False
   ↓
2. [Phase 6A.64] Email validation PASSED - Email: azure-log-test@example.com
   ↓
3. [Phase 6A.64] Existing subscriber check - Email: azure-log-test@example.com, Found: False
   ↓
4. [Phase 6A.64] Creating NEW subscriber - Email: azure-log-test@example.com
   ↓
5. [Phase 6A.64] Creating new subscriber - Email: azure-log-test@example.com, MetroAreaIds: [39111111-1111-1111-1111-111111111001], ReceiveAll: False
   ↓
6. [Phase 6A.64] Domain entity created - Email: azure-log-test@example.com, SubscriberId: {guid}, MetroAreaCount: 1
   ↓
7. [Phase 6A.64] Added subscriber to repository - Email: azure-log-test@example.com, SubscriberId: {guid}
   ↓
8. [Phase 6A.64] Committing changes to database - Email: azure-log-test@example.com
   ↓
9. [Phase 6A.64] Database commit SUCCESSFUL - Email: azure-log-test@example.com, SubscriberId: {guid}
```

### If It Fails - What to Look For

**Scenario A: Exception Thrown**
```
[Phase 6A.64] EXCEPTION during newsletter subscription - Email: ..., ExceptionType: {type}, Message: {message}, StackTrace: ...
[Phase 6A.64] INNER EXCEPTION - Type: {type}, Message: {message}
```

**Scenario B: Domain Validation Failed**
```
[Phase 6A.64] Create FAILED - Email: ..., Error: {validation error}
```

**Scenario C: Stops at Specific Step**
- If logs stop at "Creating new subscriber" → Issue in domain entity
- If logs stop at "Committing changes" → Database/EF issue
- If logs stop at "Added subscriber to repository" → Unit of work issue

---

## 🎯 Common Issues and Solutions

### Issue 1: No Logs Appear At All
**Cause**: Controller/API layer failing before reaching command handler
**Check**: Look for controller-level exceptions or routing errors

### Issue 2: Stops at "Committing changes"
**Cause**: EF Core can't save to junction table
**Possible Reasons**:
- Junction table doesn't exist (migration didn't run)
- EF Core configuration issue with many-to-many relationship
- Foreign key constraint violation

**Verify**:
```sql
-- Check if junction table exists
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'communications'
AND table_name = 'newsletter_subscriber_metro_areas';

-- Check if old column still exists
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'communications'
AND table_name = 'newsletter_subscribers'
AND column_name = 'metro_area_id';
```

### Issue 3: Exception with EF Core
**Common Exception Types**:
- `DbUpdateException` → Database constraint violation
- `InvalidOperationException` → EF Core configuration issue
- `NullReferenceException` → Missing navigation property

---

## 🚀 Action Items for You

**Immediate** (5 minutes):
1. Open Azure Portal → Container App Logs
2. Start log streaming
3. Make test API call
4. **Copy all `[Phase 6A.64]` log entries** and send them to me

**What I Need**:
```
Full log output showing:
- Where the flow starts: [Phase 6A.64] Newsletter subscription START
- Where it stops or fails
- Any exception messages
- Full stack trace if exception occurs
```

**With these logs, I can**:
- Identify exact failure point (validation/domain/repository/database)
- See the actual error message and exception type
- Provide targeted fix immediately

---

## 📊 Deployment Verification Checklist

Before checking logs, verify deployment is correct:

### 1. Container App Running Latest Code
```bash
# Check container app revision
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "[0].{name:name, active:properties.active, createdTime:properties.createdTime}"
```

**Expected**: Latest revision from commit `1244f569`

### 2. Migration Actually Ran
```sql
-- Check migration history
SELECT migration_id, product_version
FROM public."__EFMigrationsHistory"
ORDER BY migration_id DESC
LIMIT 5;
```

**Expected**: Contains `20260107183000_Phase6A64_AddNewsletterSubscriberMetroAreasJunctionTable`

### 3. Junction Table Exists
```sql
-- Verify table structure
\d communications.newsletter_subscriber_metro_areas

-- Expected columns:
-- subscriber_id | uuid
-- metro_area_id | uuid
-- created_at | timestamp with time zone
```

---

## 📝 Log Template for Reporting

Please provide logs in this format:

```
===== TEST ATTEMPT 1 =====
Timestamp: YYYY-MM-DD HH:MM:SS UTC
Request: POST /api/Newsletter/subscribe
Body: {"email":"test@example.com","metroAreaIds":[...],"receiveAllLocations":false}

LOGS:
[Copy all Phase 6A.64 log entries here]

RESULT: HTTP 400 - SUBSCRIPTION_FAILED
===========================
```

---

## ⚠️ If You Can't Access Logs

If you cannot access Azure logs directly, please provide:

1. **Database query results**:
   ```sql
   -- Does junction table exist?
   SELECT * FROM information_schema.tables
   WHERE table_name = 'newsletter_subscriber_metro_areas';

   -- Does old column still exist?
   SELECT * FROM information_schema.columns
   WHERE table_name = 'newsletter_subscribers'
   AND column_name = 'metro_area_id';
   ```

2. **Migration history**:
   ```sql
   SELECT * FROM public."__EFMigrationsHistory"
   WHERE migration_id LIKE '%Phase6A64%';
   ```

3. **Container App Info**:
   - Which revision is currently active?
   - When was it deployed?
   - Is it from commit `1244f569`?

---

**Next Step**: Please check Azure logs and provide the `[Phase 6A.64]` log entries. This will tell us exactly what's failing.
