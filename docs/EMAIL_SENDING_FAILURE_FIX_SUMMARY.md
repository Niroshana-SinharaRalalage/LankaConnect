# Email Sending Failure Fix Summary - Phase 6A.41

**Date**: 2025-12-24
**Status**: FIX READY - Database update required
**Priority**: HIGH

---

## Problem

Event publication emails fail with error:
```
Cannot access value of a failed result
```

**Working**: Registration confirmation emails (free events)
**Not Working**: Event publication notification emails

---

## Root Cause

The `event-published` email template has a **NULL `subject_template`** value in the database.

### Why Registration Works But Event Publication Fails

| Template | Subject in Database | Status |
|----------|-------------------|--------|
| `registration-confirmation` | `'Registration Confirmed for {{EventTitle}}'` | ✅ Working |
| `event-published` | `NULL` | ❌ Broken |

---

## What Was Already Done

**Phase 6A.41 Fix** (Commit 9306c99b - Deployed to staging 2025-12-24 14:39 UTC):
- Changed EF Core configuration from `OwnsOne` to `HasConversion`
- Added `EmailSubject.FromDatabase()` bypass method
- **Result**: Prevents materialization error, but template still has NULL subject

---

## What Needs to Be Done

### Fix the Database Template

**File**: `c:\Work\LankaConnect\scripts\FixEventPublishedTemplateSubject_Phase6A41.sql`

**What it does**:
```sql
UPDATE communications.email_templates
SET subject_template = 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}'
WHERE name = 'event-published'
  AND (subject_template IS NULL OR subject_template = '');
```

**How to run**:
```bash
# Connect to staging database and execute
az postgres flexible-server execute \
    --name lankaconnect-staging-db \
    --admin-user adminuser \
    --database-name lankaconnect \
    --file-path scripts/FixEventPublishedTemplateSubject_Phase6A41.sql
```

---

## Testing

**File**: `c:\Work\LankaConnect\scripts\test-event-publication-email.ps1`

**Run test**:
```powershell
.\scripts\test-event-publication-email.ps1 -Environment staging
```

**Expected result**:
- Event created and published successfully
- Logs show: `"Event notification emails completed for event {EventId}. Success: X, Failed: 0"`
- Email subject: `"New Event: [EventTitle] in [City], [State]"`

---

## Quick Start

### 1. Verify Current State
```sql
SELECT name, subject_template, is_active
FROM communications.email_templates
WHERE name = 'event-published';
```

**Expected**: `subject_template` is NULL or empty

### 2. Apply Fix
```bash
# Run fix script against staging database
scripts/FixEventPublishedTemplateSubject_Phase6A41.sql
```

### 3. Test
```powershell
# Test event publication email
.\scripts\test-event-publication-email.ps1
```

### 4. Verify Logs
```bash
# Check for successful email sending
az containerapp logs show \
    --name lankaconnect-staging \
    --resource-group lankaconnect-staging \
    --tail 100 | grep "event-published"
```

**Look for**: `"Template 'event-published' rendered from database successfully"`

---

## Files Created

1. **c:\Work\LankaConnect\docs\EMAIL_SENDING_FAILURE_COMPREHENSIVE_ANALYSIS.md**
   - Full root cause analysis with timeline, hypothesis, and detailed investigation

2. **c:\Work\LankaConnect\scripts\FixEventPublishedTemplateSubject_Phase6A41.sql**
   - SQL script to update template subject (safe, idempotent)

3. **c:\Work\LankaConnect\scripts\test-event-publication-email.ps1**
   - PowerShell script to test event publication email end-to-end

4. **c:\Work\LankaConnect\docs\EMAIL_SENDING_FAILURE_FIX_SUMMARY.md** (this file)
   - Quick reference for applying fix

---

## Success Criteria

✅ **Database**: Template has valid subject with placeholders
✅ **Code**: Phase 6A.41 EF Core fix deployed (already done)
✅ **Email**: Event publication emails sent successfully
✅ **Logs**: No "Cannot access value of a failed result" errors

---

## Rollback Plan

If issues arise after fix:

```sql
-- Revert template to NULL (NOT RECOMMENDED)
UPDATE communications.email_templates
SET subject_template = NULL
WHERE name = 'event-published';
```

**Better approach**: Fix any new issues rather than reverting, as NULL subject will cause same error.

---

## Related Documents

- **Full Analysis**: c:\Work\LankaConnect\docs\EMAIL_SENDING_FAILURE_COMPREHENSIVE_ANALYSIS.md
- **Original RCA**: c:\Work\LankaConnect\docs\EMAIL_SENDING_FAILURE_RCA.md
- **Phase 6A.41 Commit**: 9306c99b764bb29a1206bef9a0ddf2faaa585865
- **Migration**: src\LankaConnect.Infrastructure\Data\Migrations\20251221160725_SeedEventPublishedTemplate_Phase6A39.cs

---

**Next Action**: Execute database fix script and test event publication email.
