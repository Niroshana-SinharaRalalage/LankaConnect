# Phase 6A.34 Verification - Quick Start Guide

## What Happened?

You registered for an event, but we can't find the POST request logs in Azure. This guide helps you verify if Phase 6A.34 fix is working correctly.

## TL;DR - What To Do Right Now

### Step 1: Check When Registration Happened

**Run this SQL query** against your Azure PostgreSQL database:

```sql
SELECT
    r."Id",
    r."CreatedAt",
    CASE
        WHEN r."CreatedAt" < '2025-12-18 04:38:00+00' THEN 'BEFORE Phase 6A.34 (explains missing logs)'
        ELSE 'AFTER Phase 6A.34 (should have logs - PROBLEM!)'
    END AS "Status"
FROM "Registrations" r
WHERE r."EventId" = 'c1f182a9-c957-4a78-a0b2-085917a88900'
ORDER BY r."CreatedAt" DESC
LIMIT 5;
```

### Step 2: Test Fresh Registration

**Setup**:
```bash
cd c:\Work\LankaConnect\scripts

# Set your authentication token
export AUTH_TOKEN="Bearer YOUR_TOKEN_HERE"
```

**Run test**:
```bash
bash test-phase6a34-fix.sh
```

### Step 3: Check Logs

**While test is running**, in another terminal:

```bash
az containerapp logs show \
  --name lankaconnect \
  --resource-group DefaultResourceGroup-EUS \
  --follow \
  --tail 100
```

**Look for these log lines** (in order):
1. `POST /api/events/c1f182a9.../rsvp`
2. `Processing RsvpToEventCommand`
3. `[Phase 6A.24] Found 1 domain events`
4. `RegistrationConfirmedEvent`
5. `Queuing confirmation email`

---

## Most Likely Scenario

**Registration happened BEFORE Phase 6A.34 deployment (04:38 UTC).**

This means:
- ✅ Your registration is valid and saved
- ✅ Phase 6A.34 was deployed successfully
- ✅ POST logs rolled out of the 300-line log window
- ❌ You didn't receive confirmation email (because it was before fix)

**Action**: Run the test script to verify the fix works now.

---

## Files Created

### Scripts
- `verify-registration-timestamp.sh` - Instructions for checking registration time
- `query-registration-timestamp.sql` - SQL queries for database investigation
- `test-phase6a34-fix.sh` - Comprehensive test script for Phase 6A.34

### Documentation
- `PHASE_6A_34_VERIFICATION_PLAN.md` - Complete verification plan with technical details
- `README_VERIFICATION.md` - This quick start guide

---

## Expected Test Results

### If Fix Is Working ✅

**Terminal output**:
```
STEP 3: Register for Event (POST /rsvp)
HTTP Status: 200
✓ Registration successful

STEP 4: Verify Registration (GET)
HTTP Status: 200
✓ Registration verified in database
```

**Azure logs**:
```
[14:30:15] POST /api/events/c1f182a9.../rsvp responded 200
[14:30:15] Processing RsvpToEventCommand for event c1f182a9...
[14:30:15] [Phase 6A.24] Found 1 domain events to dispatch
[14:30:15] Dispatching domain event: RegistrationConfirmedEvent
[14:30:15] Handling RegistrationConfirmedEvent for user abc123
[14:30:15] Queuing confirmation email for registration def456
[14:30:15] OutboxMessage created with Status=Pending
```

### If Fix Is NOT Working ❌

**Symptoms**:
- Registration succeeds (200 OK)
- GET confirms registration exists
- But NO domain event logs
- No confirmation email queued

**Action**: Contact development team - Phase 6A.34 needs additional investigation.

---

## Troubleshooting

### Can't Connect to Azure CLI

```bash
# Login to Azure
az login

# Verify you can see the container app
az containerapp list --resource-group DefaultResourceGroup-EUS
```

### Can't Connect to Database

1. Go to Azure Portal
2. Navigate to your PostgreSQL database
3. Use the Query Editor in the portal
4. Paste and run the SQL query from Step 1

### Test Script Permission Denied

```bash
# Make script executable
chmod +x test-phase6a34-fix.sh

# Or run with bash explicitly
bash test-phase6a34-fix.sh
```

### No AUTH_TOKEN

1. Login to your web app
2. Open browser DevTools (F12)
3. Go to Application → Local Storage
4. Find your auth token
5. Export it: `export AUTH_TOKEN="Bearer <token>"`

---

## Next Steps After Verification

### If Test Passes ✅

1. Mark Phase 6A.34 as "Verified" in documentation
2. Monitor production for confirmation email delivery
3. Consider adding UTC timestamps to all logs
4. Update Azure log retention policy (currently ~300 lines)

### If Test Fails ❌

1. Create Phase 6A.35 for logging enhancements
2. Add explicit logging to RsvpToEventCommandHandler
3. Add explicit logging to AppDbContext.CommitAsync
4. Investigate why DetectChanges() isn't working
5. Check if domain events are being cleared prematurely

---

## Questions?

Check the full verification plan: `docs/PHASE_6A_34_VERIFICATION_PLAN.md`

This document contains:
- Detailed scenario analysis
- Complete test procedures
- Architecture diagrams
- Decision matrix
- Success criteria

---

## Summary

**Your Action Items**:
1. ⏱️ Run SQL query to check registration timestamp (2 minutes)
2. 🧪 Run test script with fresh registration (5 minutes)
3. 📋 Monitor Azure logs during test (2 minutes)
4. ✅ Verify expected log sequence appears
5. 📝 Update Phase 6A.34 status based on results

**Total Time**: ~10 minutes

**Expected Outcome**: Confirmation that Phase 6A.34 fix is working correctly, and your original registration happened before the fix was deployed (which explains the missing logs).
