# Phase 6A.64 Testing Guide - Newsletter Junction Table

**Date**: 2026-01-08
**Status**: Ready for Testing
**Commits**:
- Part 1 (Background Jobs): `34c7523a`
- Part 2 (Junction Table): `0244ab2e`
- Hangfire Dashboard Access: `fe62ee40`

---

## 🎯 Testing Objectives

Verify that Phase 6A.64 (both parts) work together to:
1. ✅ Store multiple metro areas for newsletter subscribers (junction table)
2. ✅ Send event cancellation emails to newsletter subscribers without timeout (background jobs)
3. ✅ Find newsletter subscribers by state correctly (query fix)

---

## 📋 Pre-Testing Verification

### 1. Check Azure Deployment Status

**Azure Portal Steps**:
1. Navigate to: Azure Portal → LankaConnect-rg → lankaconnect-api
2. Go to "Revisions and replicas" tab
3. Verify latest revision is **Active** (should include commit `fe62ee40` or later)
4. Check "Console" tab → Logs for migration messages:
   ```
   [INFO] Applying database migrations...
   [INFO] Database migrations applied successfully
   ```

### 2. Verify Database Migration

Run this SQL query in your Azure PostgreSQL database:

```sql
-- Step 1: Verify junction table exists
SELECT
    table_name,
    table_schema
FROM information_schema.tables
WHERE table_schema = 'communications'
AND table_name = 'newsletter_subscriber_metro_areas';

-- Expected: 1 row with table_name = 'newsletter_subscriber_metro_areas'

-- Step 2: Verify junction table structure
SELECT
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'communications'
AND table_name = 'newsletter_subscriber_metro_areas'
ORDER BY ordinal_position;

-- Expected: 3 columns
-- subscriber_id | uuid | NO
-- metro_area_id | uuid | NO
-- created_at | timestamp with time zone | NO

-- Step 3: Verify old column was dropped
SELECT
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'communications'
AND table_name = 'newsletter_subscribers'
AND column_name = 'metro_area_id';

-- Expected: 0 rows (column should be deleted)

-- Step 4: Check indexes exist
SELECT
    indexname,
    tablename
FROM pg_indexes
WHERE schemaname = 'communications'
AND tablename = 'newsletter_subscriber_metro_areas';

-- Expected: 3 indexes
-- pk_newsletter_subscriber_metro_areas (PRIMARY KEY)
-- ix_newsletter_subscriber_metro_areas_metro_area_id
-- ix_newsletter_subscriber_metro_areas_subscriber_id
```

**✅ If all queries return expected results, migration is successful. Proceed to testing.**

**❌ If any query fails, deployment is incomplete. Wait 5 minutes and retry.**

---

## 🧪 Testing Scenarios

### Test 1: Newsletter Subscription with Multiple Metro Areas (UI TEST - YOU)

**Objective**: Verify UI can submit multiple metro areas and they're all stored in junction table.

**Test Data**:
- Email: `varunipw@gmail.com`
- State: Ohio (should select all 5 metro areas: Akron, Cincinnati, Cleveland, Columbus, Toledo)

**Steps**:
1. Navigate to newsletter subscription page: https://your-staging-url.com/newsletter/subscribe
2. Delete existing subscription for `varunipw@gmail.com` (if exists)
3. Fill out form:
   - Email: `varunipw@gmail.com`
   - Check **"Ohio"** checkbox
   - Verify all 5 Ohio metro areas are shown as selected in UI
   - Click **"Subscribe"**
4. Check browser console for:
   - ✅ HTTP 200 OK response
   - ✅ No `SUBSCRIPTION_FAILED` error
   - ✅ Confirmation message displayed

**Database Verification** (Run this SQL):
```sql
-- Verify subscriber was created
SELECT
    id,
    email,
    receive_all_locations,
    is_active,
    is_confirmed,
    created_at
FROM communications.newsletter_subscribers
WHERE email = 'varunipw@gmail.com';

-- Expected: 1 row with is_active = true, is_confirmed = false

-- Verify ALL 5 metro areas were stored in junction table
SELECT
    ns.email,
    ma.name as metro_area_name,
    ma.state,
    nsma.created_at
FROM communications.newsletter_subscribers ns
JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
JOIN events.metro_areas ma ON nsma.metro_area_id = ma.id
WHERE ns.email = 'varunipw@gmail.com'
ORDER BY ma.name;

-- Expected: 5 rows
-- varunipw@gmail.com | Akron | OH
-- varunipw@gmail.com | Cincinnati | OH
-- varunipw@gmail.com | Cleveland | OH
-- varunipw@gmail.com | Columbus | OH
-- varunipw@gmail.com | Toledo | OH
```

**✅ Pass Criteria**:
- UI shows success message
- Database has 1 newsletter subscriber row
- Junction table has **5 rows** (one for each Ohio metro area)

**❌ Fail Criteria**:
- UI shows error message or `SUBSCRIPTION_FAILED`
- Junction table has 0 rows or less than 5 rows
- Database shows old `metro_area_id` column still exists

---

### Test 2: API Newsletter Subscription (API TEST - CLAUDE)

**Objective**: Verify API accepts `metroAreaIds` array and stores all in junction table.

**Test Data**:
- Email: `claude-test@example.com`
- Metro Area IDs: All 5 Ohio metro areas

**API Call**:
```bash
POST /api/proxy/newsletter/subscribe
Content-Type: application/json

{
  "email": "claude-test@example.com",
  "metroAreaIds": [
    "39111111-1111-1111-1111-111111111001",  # Akron
    "39111111-1111-1111-1111-111111111002",  # Cincinnati
    "39111111-1111-1111-1111-111111111003",  # Cleveland
    "39111111-1111-1111-1111-111111111004",  # Columbus
    "39111111-1111-1111-1111-111111111005"   # Toledo
  ],
  "receiveAllLocations": false
}
```

**Expected Response**: HTTP 200 OK
```json
{
  "success": true,
  "data": {
    "id": "<guid>",
    "email": "claude-test@example.com",
    "metroAreaIds": ["39111111-...", "39111111-...", ...],
    "receiveAllLocations": false,
    "isActive": true,
    "isConfirmed": false,
    "confirmationToken": "<token>",
    "confirmationSentAt": "<timestamp>"
  }
}
```

**Database Verification**:
```sql
SELECT
    ns.email,
    COUNT(nsma.metro_area_id) as metro_count,
    STRING_AGG(ma.name, ', ' ORDER BY ma.name) as metro_areas
FROM communications.newsletter_subscribers ns
JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
JOIN events.metro_areas ma ON nsma.metro_area_id = ma.id
WHERE ns.email = 'claude-test@example.com'
GROUP BY ns.email;

-- Expected: 1 row with metro_count = 5
```

---

### Test 3: Event Cancellation Email to Newsletter Subscribers (INTEGRATION TEST - YOU)

**Objective**: Verify newsletter subscribers receive cancellation emails for events in subscribed states.

**Prerequisites**:
- Test 1 completed successfully (`varunipw@gmail.com` subscribed to all Ohio metro areas)
- Subscriber confirmed (run SQL update if needed):
  ```sql
  UPDATE communications.newsletter_subscribers
  SET is_confirmed = true, confirmed_at = NOW()
  WHERE email = 'varunipw@gmail.com';
  ```

**Test Data**:
- Event ID: `13c4b999-b9f4-4a54-abe2-2d36192ac36b` (Aurora, Ohio - Akron metro area)
- Expected Recipients:
  1. `niroshhh@gmail.com` (event registrant)
  2. `niroshanaks@gmail.com` (event registrant)
  3. `varunipw@gmail.com` (newsletter subscriber) **← THE FIX**

**Steps**:
1. Navigate to event management page
2. Find event: Aurora, Ohio (Event ID: `13c4b999-b9f4-4a54-abe2-2d36192ac36b`)
3. Click **"Cancel Event"**
4. Enter cancellation reason: "Testing Phase 6A.64 newsletter subscriber junction table fix"
5. Confirm cancellation

**API Response Verification**:
- ✅ API responds in **< 2 seconds** (background job fix)
- ✅ HTTP 200 OK with success message
- ✅ No timeout errors

**Hangfire Dashboard Verification**:
1. Navigate to Hangfire Dashboard: https://your-staging-url.com/hangfire
2. Go to "Jobs" → "Succeeded"
3. Find job: `EventCancellationEmailJob`
4. Click job to view details
5. Check execution logs for:
   ```
   [Phase 6A.64] EventCancellationEmailJob STARTED - Event 13c4b999
   [Phase 6A.64] Retrieved 2 confirmed registrations
   [Phase 6A.64] Getting confirmed subscribers for ALL metro areas in state Ohio
   [Phase 6A.64] Found 5 metro areas in state OH
   [Phase 6A.64] Retrieved 1 confirmed subscribers for state Ohio
   [Phase 6A.64] Sending cancellation emails to 3 unique recipients
   [Phase 6A.64] Sent cancellation email to niroshhh@gmail.com in ...ms
   [Phase 6A.64] Sent cancellation email to niroshanaks@gmail.com in ...ms
   [Phase 6A.64] Sent cancellation email to varunipw@gmail.com in ...ms
   [Phase 6A.64] EventCancellationEmailJob COMPLETED
   ```

**Email Verification**:
Check inbox for all 3 emails:
- ✅ `niroshhh@gmail.com` received cancellation email
- ✅ `niroshanaks@gmail.com` received cancellation email
- ✅ `varunipw@gmail.com` received cancellation email **← PRIMARY FIX VALIDATION**

**Database Verification**:
```sql
-- Verify newsletter subscriber was found by state query
SELECT
    ns.email,
    ns.is_confirmed,
    COUNT(nsma.metro_area_id) as subscribed_metro_count,
    STRING_AGG(ma.name, ', ' ORDER BY ma.name) as subscribed_metros
FROM communications.newsletter_subscribers ns
JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
JOIN events.metro_areas ma ON nsma.metro_area_id = ma.id
WHERE ns.email = 'varunipw@gmail.com'
GROUP BY ns.id, ns.email, ns.is_confirmed;

-- Expected:
-- varunipw@gmail.com | true | 5 | Akron, Cincinnati, Cleveland, Columbus, Toledo
```

**✅ Pass Criteria**:
- API responds quickly (< 2s)
- Hangfire job shows all 3 emails sent
- All 3 recipients received cancellation emails
- Logs show "Retrieved 1 confirmed subscribers for state Ohio"

**❌ Fail Criteria**:
- API timeout (> 5s)
- Only 2 emails sent (newsletter subscriber missing)
- Hangfire logs show "Retrieved 0 confirmed subscribers for state Ohio"
- `varunipw@gmail.com` did not receive email

---

### Test 4: State-Level Newsletter Subscriber Query (API TEST - CLAUDE)

**Objective**: Verify repository query finds subscribers with ANY metro area in a state.

**Prerequisites**: Test 1 completed (varunipw@gmail.com subscribed to all Ohio metro areas)

**Test Scenarios**:

#### Scenario A: Query for Ohio subscribers
**Expected**: Should find `varunipw@gmail.com` (subscribed to all 5 Ohio metro areas)

#### Scenario B: Query for California subscribers
**Expected**: Should NOT find `varunipw@gmail.com` (not subscribed to any CA metro areas)

**Database Query to Test**:
```sql
-- This mimics what GetConfirmedSubscribersByStateAsync does

-- Step 1: Get all Ohio metro area IDs
WITH ohio_metros AS (
    SELECT id, name, state
    FROM events.metro_areas
    WHERE state = 'OH'
)
-- Step 2: Find subscribers with ANY Ohio metro area
SELECT DISTINCT
    ns.email,
    ns.is_confirmed,
    ns.is_active,
    COUNT(DISTINCT nsma.metro_area_id) as matched_metro_count
FROM communications.newsletter_subscribers ns
JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
JOIN ohio_metros om ON nsma.metro_area_id = om.id
WHERE ns.is_active = true AND ns.is_confirmed = true
GROUP BY ns.id, ns.email, ns.is_confirmed, ns.is_active;

-- Expected: 1 row
-- varunipw@gmail.com | true | true | 5
```

---

## 📊 Test Results Checklist

### Database Migration
- [ ] Junction table `newsletter_subscriber_metro_areas` exists
- [ ] Junction table has 3 columns: `subscriber_id`, `metro_area_id`, `created_at`
- [ ] Junction table has 3 indexes (PK + 2 FK indexes)
- [ ] Old column `metro_area_id` dropped from `newsletter_subscribers`

### UI Testing (You)
- [ ] Newsletter subscription form accepts email and state selection
- [ ] Selecting "Ohio" shows all 5 metro areas selected
- [ ] Form submission succeeds (no `SUBSCRIPTION_FAILED` error)
- [ ] Database has 5 junction table rows for subscriber
- [ ] Event cancellation completes in < 2 seconds
- [ ] All 3 recipients receive cancellation emails

### API Testing (Claude)
- [ ] POST `/api/proxy/newsletter/subscribe` accepts `metroAreaIds` array
- [ ] API returns success response with all metro area IDs
- [ ] Database stores all metro areas in junction table
- [ ] State query finds subscribers correctly

### Integration Testing (You + Claude)
- [ ] Background job queues instantly (< 1s API response)
- [ ] Hangfire dashboard shows job execution
- [ ] Logs show `[Phase 6A.64]` entries
- [ ] Newsletter subscribers receive event cancellation emails
- [ ] Repository query uses junction table JOIN

---

## 🐛 Troubleshooting

### Issue: `SUBSCRIPTION_FAILED` error in UI
**Cause**: Migration hasn't run yet, junction table doesn't exist
**Fix**:
1. Check Azure Container App logs for migration messages
2. Verify junction table exists with SQL query
3. Wait 5-10 minutes for deployment to complete
4. Try subscription again

### Issue: Only 1 metro area stored instead of 5
**Cause**: Old code still running (migration not applied)
**Fix**:
1. Check git commit: Should be `fe62ee40` or later
2. Verify migration files don't have `.skip` extension
3. Force redeploy: Push empty commit to trigger deployment

### Issue: Newsletter subscriber not receiving cancellation email
**Cause**: Subscriber not confirmed OR junction table empty
**Fix**:
1. Check `is_confirmed = true` for subscriber
2. Run SQL to confirm subscriber:
   ```sql
   UPDATE communications.newsletter_subscribers
   SET is_confirmed = true, confirmed_at = NOW()
   WHERE email = 'varunipw@gmail.com';
   ```
3. Verify junction table has 5 rows for subscriber
4. Cancel event again

### Issue: API timeout on event cancellation
**Cause**: Background job fix not deployed
**Fix**: Verify commit `34c7523a` is in deployment history

---

## 📝 Test Report Template

After completing all tests, document results:

```
# Phase 6A.64 Test Report

Date: [Date]
Tester: [Your Name]
Environment: Azure Staging

## Test Results Summary
- Database Migration: ✅ PASS / ❌ FAIL
- UI Newsletter Subscription: ✅ PASS / ❌ FAIL
- API Newsletter Subscription: ✅ PASS / ❌ FAIL
- Event Cancellation (Integration): ✅ PASS / ❌ FAIL

## Issues Found
[List any issues encountered]

## Screenshots
[Attach screenshots of successful tests]

## Next Steps
[What should be done next]
```

---

## ✅ Success Criteria

**Phase 6A.64 is FULLY VERIFIED when**:
1. ✅ Junction table exists in database
2. ✅ UI can subscribe with multiple metro areas (all stored)
3. ✅ API accepts `metroAreaIds` array
4. ✅ Event cancellation completes in < 2 seconds
5. ✅ Newsletter subscriber receives cancellation email
6. ✅ Hangfire dashboard shows successful job execution
7. ✅ Logs show `[Phase 6A.64]` prefix for all operations

---

**Ready to Test!** Start with database verification, then proceed to UI testing. I'll handle API testing in parallel.
