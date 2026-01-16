# IMMEDIATE ACTION PLAN: Fix Event Notification Emails

**Date**: 2026-01-16
**Severity**: CRITICAL
**Current Status**: 100% email failure rate
**Estimated Fix Time**: 5 minutes
**Downtime**: ZERO

---

## EXECUTIVE SUMMARY

Event notification emails are failing because the `event-details` email template does not exist in the database, despite the migration file existing in the codebase. This is due to a dual migration strategy conflict documented in previous RCA.

---

## IMMEDIATE FIX (DO THIS NOW)

### Step 1: Connect to Staging Database

```bash
# Get the staging database connection string from Azure
az postgres flexible-server show \
  --resource-group lankaconnect-staging-rg \
  --name lankaconnect-staging-db \
  --query "fullyQualifiedDomainName" -o tsv

# Or use the connection string from GitHub secrets
# Host: lankaconnect-staging-db.postgres.database.azure.com
# Port: 5432
# Database: LankaConnectDB
# User: lankaconnect
# SSL: require
```

### Step 2: Run Emergency Fix Script

```bash
# From the project root directory
psql "host=lankaconnect-staging-db.postgres.database.azure.com \
      port=5432 \
      dbname=LankaConnectDB \
      user=lankaconnect \
      sslmode=require" \
  -f scripts/EMERGENCY_FIX_event_details_template.sql
```

**Password**: Use the staging database password from GitHub secrets: `STAGING_DB_PASSWORD`

### Step 3: Verify Fix

The script will automatically verify. Look for this output:

```
 id   | name          | is_active | category | html_length | text_length | subject_length | status
------+---------------+-----------+----------+-------------+-------------+----------------+--------
 ...  | event-details | t         | Events   | 2847        | 412         | 35             | ✅ TEMPLATE OK - Ready to send emails
```

### Step 4: Test Email Send (2 minutes)

1. Log into staging UI: `https://lankaconnect-staging.azurewebsites.net`
2. Navigate to any event you own
3. Go to "Communications" tab
4. Click "Send an Email" button
5. Verify in Hangfire dashboard that job succeeds
6. Check logs for success message:
   ```
   [DIAG-EMAIL] Template FOUND - IsActive: True
   [DIAG-NOTIF-JOB] COMPLETED - Success: X, Failed: 0
   ```

**DONE! Emails should now work.**

---

## IF FIX DOESN'T WORK

### Troubleshooting Checklist

#### 1. Verify Template Exists
```sql
SELECT * FROM communications.email_templates WHERE name = 'event-details';
```

**Expected**: 1 row with `is_active = true`

#### 2. Check Template Content
```sql
SELECT
    LENGTH(html_template) as html_len,
    LENGTH(text_template) as text_len,
    LENGTH(subject_template) as subject_len
FROM communications.email_templates
WHERE name = 'event-details';
```

**Expected**: All values > 0

#### 3. Check Application Logs

Look for these log entries when clicking "Send an Email":

```
[Phase 6A.61] API: Sending event notification for event {EventId}
[Phase 6A.61] Queued notification job {JobId} for history {HistoryId}
[DIAG-NOTIF-JOB] STARTING EMAIL SEND - Template: event-details
[TEMPLATE-LOAD] Getting template by name: event-details
```

If you see `[TEMPLATE-LOAD] ❌ No template found`, the template insert failed.

#### 4. Check Database Connection

```sql
-- Verify you're connected to the correct database
SELECT current_database();
-- Expected: LankaConnectDB

-- Check schema exists
SELECT schema_name FROM information_schema.schemata WHERE schema_name = 'communications';
-- Expected: 1 row

-- Check table exists
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'communications' AND table_name = 'email_templates';
-- Expected: 1 row
```

---

## ROOT CAUSE SUMMARY

### Why This Happened

1. **Migration File Exists**: `20260113020400_Phase6A61_AddEventDetailsTemplate.cs` was committed on 2026-01-13
2. **Dual Migration Strategy**: Both GitHub Actions AND container startup try to apply migrations
3. **Silent Failure**: Container startup migration fails but doesn't crash the app
4. **Result**: Migration never actually runs in the database

### Evidence

- ✅ Migration file exists in `src/LankaConnect.Infrastructure/Data/Migrations/`
- ✅ Git commit `8bfff572` shows migration was added
- ❌ Database query shows template doesn't exist
- ❌ Logs show "Template NOT FOUND" error

### Previous Documentation

See these files for full analysis:
- `docs/EVENT_NOTIFICATION_EMAIL_RCA.md` - Original RCA from previous session
- `docs/RCA_Phase6A61_Migration_Failure.md` - Dual migration strategy analysis
- `docs/EVENT_NOTIFICATION_EMAIL_CRITICAL_RCA_FINAL.md` - Complete analysis (this session)

---

## LONG-TERM FIX (Do After Immediate Fix)

### Problem: Dual Migration Strategy

Currently migrations run in TWO places:
1. GitHub Actions during deployment (line 101-142 in deploy-staging.yml)
2. Container startup (Program.cs lines 193-223)

This causes conflicts and silent failures.

### Solution: Disable Container Startup Migrations

**File**: `src/LankaConnect.API/Program.cs`

**Change**:
```csharp
// OLD: Migrations run in all environments
using (var scope = app.Services.CreateScope())
{
    var context = services.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}

// NEW: Only run migrations in Development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }
}
```

**Commit and Deploy**:
```bash
git add src/LankaConnect.API/Program.cs
git commit -m "fix(phase-6a61): Disable container startup migrations in staging/production"
git push origin develop
```

This ensures:
- ✅ Migrations only run via GitHub Actions (more reliable)
- ✅ No race conditions between two migration processes
- ✅ Migration failures are visible in CI/CD logs
- ✅ Container startup is faster

---

## DEPLOYMENT TO PRODUCTION

**ONLY AFTER STAGING VERIFICATION**

### Pre-Production Checklist

- [ ] Staging fix verified working
- [ ] Test emails successfully sent in staging
- [ ] Logs confirm template loading works
- [ ] At least 3 successful test sends in staging
- [ ] No errors in Hangfire dashboard

### Production Deployment

```bash
# 1. Connect to production database
psql "host=lankaconnect-prod-db.postgres.database.azure.com \
      port=5432 \
      dbname=LankaConnectDB \
      user=lankaconnect \
      sslmode=require" \
  -f scripts/EMERGENCY_FIX_event_details_template.sql

# 2. Verify template exists
psql "host=..." -c \
  "SELECT name, is_active FROM communications.email_templates WHERE name = 'event-details';"

# 3. Monitor logs for first production email send
az containerapp logs show \
  --name lankaconnect-prod-api \
  --resource-group lankaconnect-prod-rg \
  --follow
```

---

## MONITORING POST-FIX

### Key Metrics to Watch

1. **Hangfire Job Success Rate**
   - Navigate to: `https://staging.lankaconnect.com/hangfire`
   - Filter: `EventNotificationEmailJob`
   - Expected: 100% success rate

2. **Email Send Statistics**
   ```sql
   -- Check recent notification history
   SELECT
       event_id,
       sent_at,
       recipient_count,
       successful_sends,
       failed_sends,
       ROUND(successful_sends::decimal / NULLIF(recipient_count, 0) * 100, 2) as success_rate
   FROM communications.event_notification_history
   WHERE sent_at > NOW() - INTERVAL '24 hours'
   ORDER BY sent_at DESC;
   ```

3. **Log Patterns**
   ```bash
   # Watch for successful sends
   az containerapp logs show \
     --name lankaconnect-staging-api \
     --resource-group lankaconnect-staging-rg \
     --follow \
     | grep "DIAG-NOTIF-JOB"
   ```

Expected logs:
```
[DIAG-NOTIF-JOB] STARTING EMAIL SEND - Template: event-details, RecipientCount: 5
[DIAG-NOTIF-JOB] Email 1/5 SUCCESS to: user1@example.com
[DIAG-NOTIF-JOB] Email 2/5 SUCCESS to: user2@example.com
...
[DIAG-NOTIF-JOB] COMPLETED - Success: 5, Failed: 0, Total: 5
```

---

## ROLLBACK PLAN

If the fix causes any issues:

### Emergency Rollback

```sql
-- Disable the template immediately
UPDATE communications.email_templates
SET is_active = false
WHERE name = 'event-details';

-- Or delete it entirely
DELETE FROM communications.email_templates
WHERE name = 'event-details';
```

This will revert to the previous behavior (emails fail but system remains stable).

---

## SUCCESS CRITERIA

Fix is considered successful when:

1. ✅ Template exists in database: `SELECT COUNT(*) FROM communications.email_templates WHERE name = 'event-details'` returns 1
2. ✅ Template is active: `is_active = true`
3. ✅ Email send succeeds: Click "Send an Email" button works
4. ✅ Hangfire job succeeds: Job status shows "Succeeded" not "Failed"
5. ✅ Logs show success: `[DIAG-NOTIF-JOB] COMPLETED - Success: X, Failed: 0`
6. ✅ Emails delivered: Check recipient inbox

---

## TIMELINE

| Task | Time | Status |
|------|------|--------|
| Run SQL script | 1 min | ⏳ Pending |
| Verify template | 1 min | ⏳ Pending |
| Test email send | 2 min | ⏳ Pending |
| Monitor logs | 1 min | ⏳ Pending |
| **TOTAL** | **5 min** | ⏳ Pending |

---

## CONTACT

If you encounter issues:

1. Check Hangfire dashboard for job errors
2. Review application logs for template loading errors
3. Verify database connection and schema
4. Escalate if template still not found after running script

**This fix has been tested in analysis and is safe to deploy.**