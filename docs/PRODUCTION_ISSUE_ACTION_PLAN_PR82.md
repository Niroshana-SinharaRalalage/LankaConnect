# Production Issue Action Plan - PR #82

**Date**: 2026-02-16
**Status**: 🚨 **ACTIVE INCIDENT**
**Severity**: 🔴 **CRITICAL**

---

## Quick Summary

PR #82 merged to production with 3 distinct issues:

| Issue | Severity | Fix Complexity | ETA |
|-------|----------|----------------|-----|
| **Issue #2: Wrong quantities in emails** | 🔴 P0 | Very Low (2 lines) | 30 min |
| **Issue #1: Migrations not applied** | 🟠 P1 | Low (manual migration) | 1-2 hours |
| **Issue #3: Performance degradation** | 🟡 P2 | Unknown (investigation) | TBD |

---

## Issue #2: Wrong Quantities in Signup Update Emails (HIGHEST PRIORITY)

### Problem
Users updating signup commitments receive emails showing:
```
PREVIOUS QUANTITY: 0
NEW QUANTITY: 0
```

Instead of actual values (e.g., 10 → 5).

### Root Cause
`CommitmentUpdatedEventHandler.cs` creates email params but **never sets** `OldQuantity` and `NewQuantity` properties.

### Fix (2 Lines of Code)

**File**: `src/LankaConnect.Application/Events/EventHandlers/CommitmentUpdatedEventHandler.cs`

**After line 103**, add:
```csharp
// Set old and new quantities for update email template
emailParams.OldQuantity = oldQuantity;
emailParams.NewQuantity = newQuantity;
```

### Action Steps

```bash
# 1. Create fix branch
git checkout develop
git pull origin develop
git checkout -b fix/phase6a122-signup-email-quantities

# 2. Apply fix (edit file manually or use Edit tool)
# Add 2 lines after line 103 in CommitmentUpdatedEventHandler.cs

# 3. Build and test
dotnet build src/LankaConnect.API
dotnet test tests/LankaConnect.Application.Tests

# 4. Commit and push
git add src/LankaConnect.Application/Events/EventHandlers/CommitmentUpdatedEventHandler.cs
git commit -m "fix(email): Phase 6A.122 - Fix wrong quantities in signup update emails

CRITICAL BUG: CommitmentUpdatedEventHandler creates email params but never
sets OldQuantity and NewQuantity properties, causing emails to show 0/0.

Fix: Add 2 lines to set emailParams.OldQuantity and emailParams.NewQuantity
from calculated oldQuantity and newQuantity variables.

Issue: Users receive incorrect data in signup commitment update emails
Impact: Confusing UX - users see PREVIOUS: 0, NEW: 0 instead of actual values
Root Cause: Missing property assignments after email params creation

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

git push origin fix/phase6a122-signup-email-quantities

# 5. Create PR
gh pr create --title "Phase 6A.122: Fix wrong quantities in signup update emails" \
  --body "## Critical Bug Fix

**Issue**: Users updating signup commitments receive emails with PREVIOUS: 0, NEW: 0

**Root Cause**: CommitmentUpdatedEventHandler never sets OldQuantity/NewQuantity

**Fix**: Add 2 lines to set email params properties

**Testing**: Manual test + unit test

**Priority**: P0 - Incorrect data shown to users"

# 6. Merge to develop, then main
# 7. Deploy to staging, test, deploy to production
```

---

## Issue #1: Database Migrations NOT Applied to Production

### Problem
Email templates in production database still have old format:
- Using `{{ResponseSummary}}` instead of `{{{ResponseSummary}}}` (broken line breaks)
- Still showing "feel free to reply to this email" text

### Root Cause
**UNKNOWN** - Workflow HAS migration step, but migrations were not applied.

### Investigation Required

**Step 1: Check GitHub Actions Logs**
```bash
# Get latest production deployment run
gh run list --workflow=deploy-production.yml --limit 1

# View logs
gh run view <run-id> --log | grep -i "migration\|error"
```

**Step 2: Check Database State**
```powershell
# Run verification script
.\scripts\verify_production_migrations_phase6a116_117.ps1
```

**Step 3: Check Template State**
```sql
SELECT
    name,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'FIXED'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'BROKEN'
        ELSE 'N/A'
    END as status
FROM communications.email_templates
WHERE name LIKE '%form-response%' OR name LIKE '%signup-list-commitment%';
```

### Immediate Fix (Manual Migration)

```powershell
# Apply migrations manually
.\scripts\apply_phase6a116_117_migrations.ps1

# Verify success
.\scripts\verify_production_migrations_phase6a116_117.ps1
```

### Long-term Fix

1. Investigate why deployment workflow migration step failed
2. Add deployment verification step to CI/CD
3. Add post-deployment health check for migrations

---

## Issue #3: Performance Degradation in Signup Operations

### Problem
User reports "large performance issue with signup operations"

### Investigation Required

**Step 1: Collect Azure Logs**
```bash
az containerapp logs show \
  --name lankaconnect-api-prod \
  --resource-group lankaconnect-prod \
  --tail 1000 \
  --follow | grep -i "signup\|commitment\|duration"
```

**Step 2: Check Database Queries**
```sql
-- Enable query logging
ALTER SYSTEM SET log_min_duration_statement = 1000; -- Log queries > 1s

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

**Step 3: Check Application Insights**
- Request duration for `/api/events/{id}/signups` endpoints
- Dependency duration for database calls
- Look for timeouts or exceptions

### Potential Causes

1. **N+1 Query Problem** - Handler loading related entities in loop
2. **Missing Database Index** - Recent schema changes removed indexes
3. **Event Handler Blocking** - Email sending blocking transaction
4. **Connection Pool Exhaustion** - Concurrent operations exhausting pool

---

## Priority Timeline

### Immediate (0-2 hours)

1. ✅ **[DONE]** Create comprehensive RCA document
2. ✅ **[DONE]** Create verification scripts
3. ✅ **[DONE]** Create manual migration scripts
4. ⏳ **Fix Issue #2** - Apply 2-line code fix
5. ⏳ **Investigate Issue #1** - Check deployment logs

### Short-term (2-8 hours)

1. Deploy Issue #2 fix to staging
2. Test Issue #2 fix with actual signup updates
3. Apply Issue #1 migrations manually if needed
4. Deploy Issue #2 fix to production
5. Verify Issue #1 migrations applied

### Medium-term (1-2 days)

1. Investigate Issue #1 root cause (why deployment failed)
2. Collect data for Issue #3 (performance)
3. Add deployment verification to CI/CD
4. Create unit tests for Issue #2

### Long-term (1 week)

1. Fix Issue #1 deployment workflow (if broken)
2. Fix Issue #3 performance issue (after investigation)
3. Add regression tests for all 3 issues
4. Update deployment runbook

---

## Success Criteria

### Issue #2 (Data Bug)
- [ ] Code fix applied and deployed
- [ ] Signup update email shows correct old/new quantities
- [ ] Unit test added to prevent regression
- [ ] No errors in Azure logs

### Issue #1 (Migration Failure)
- [ ] Migrations exist in `__EFMigrationsHistory` table
- [ ] Templates use `{{{ResponseSummary}}}` (triple braces)
- [ ] "Feel free to reply" text removed from 3 templates
- [ ] Empty PICKUP/DELIVERY card removed from 2 templates
- [ ] Test emails show correct formatting

### Issue #3 (Performance)
- [ ] Root cause identified
- [ ] Fix implemented and tested
- [ ] Performance benchmarks show improvement
- [ ] No regression in other areas

---

## Communication Plan

### Stakeholders to Notify

1. **User who reported issues** - Update on progress every 2 hours
2. **Product Owner** - Status update after each fix deployed
3. **Development Team** - Share RCA and lessons learned

### Status Updates

**Template**:
```
Production Issue Update - PR #82

Status: [INVESTIGATING | IN PROGRESS | TESTING | RESOLVED]

Issue #2 (Wrong Quantities): [status]
Issue #1 (Migrations): [status]
Issue #3 (Performance): [status]

Next Steps:
- [action 1]
- [action 2]

ETA for Resolution: [estimate]
```

---

## Files Created

1. **RCA Document**: `docs/RCA_PRODUCTION_EMAIL_ISSUES_PR82.md`
2. **Verification Script**: `scripts/verify_production_migrations_phase6a116_117.ps1`
3. **Migration Script**: `scripts/apply_phase6a116_117_migrations.ps1`
4. **Action Plan**: `docs/PRODUCTION_ISSUE_ACTION_PLAN_PR82.md` (this file)

---

## Next Steps (IMMEDIATE)

**Right now (next 30 minutes)**:
1. Apply Issue #2 code fix
2. Run verification script for Issue #1
3. Check GitHub Actions logs for deployment

**Then (next 2 hours)**:
1. Test Issue #2 fix in staging
2. Apply Issue #1 migrations manually if needed
3. Begin Issue #3 investigation

**Monitor**:
- Azure logs for errors
- User feedback on email issues
- Performance metrics for signup operations

---

**Last Updated**: 2026-02-16
**Document Owner**: Senior Engineer
**Related Documents**: RCA_PRODUCTION_EMAIL_ISSUES_PR82.md
