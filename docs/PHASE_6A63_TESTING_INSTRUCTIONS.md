# Phase 6A.63: Event Cancellation Email - Testing Instructions

**Date**: 2026-01-02
**Deployment Status**: ✅ **DEPLOYED TO STAGING** (Run ID: 20660160985, completed 14:43:09 UTC)
**API Base URL**: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`

---

## Deployment Verification

### ✅ Completed Steps
1. ✅ Code committed: c6c6da60
2. ✅ Pushed to develop branch
3. ✅ Staging deployment triggered
4. ✅ Deployment completed successfully (5m 51s)
5. ✅ API health check: Responding (200 OK)

### 🔄 Verification Needed
- [ ] Database migration applied (Phase6A63_AddEventCancelledNotificationTemplate)
- [ ] Email template exists in database (event-cancelled-notification)
- [ ] Event cancellation sends to ALL recipients (registrations + email groups + newsletter subscribers)

---

## What Changed in Phase 6A.63

### CRITICAL BUG FIXED
**Before**: Event cancellation emails ONLY sent to confirmed registrations
**After**: Event cancellation emails sent to ALL recipients:
1. ✅ Confirmed registrations
2. ✅ Email group recipients (NEW - was missing!)
3. ✅ Newsletter subscribers in event region (NEW - was missing!)

### Example Scenario That Was Broken
- Event with 0 registrations + 5 email group recipients
- **Old behavior**: 0 emails sent (early exit, bug!)
- **New behavior**: 5 emails sent to email group recipients

---

## Testing Approach

You have **2 options** for testing:

### Option 1: Test via UI (RECOMMENDED)
This is the most realistic test since it exercises the entire flow.

**Steps**:
1. Login to staging UI: http://localhost:3000 (pointing to staging API)
2. Navigate to Dashboard
3. Create/publish event with email groups and location
4. Cancel event with cancellation reason
5. Check if emails were sent

**Pros**: Tests complete user flow
**Cons**: Requires UI setup and event creation

### Option 2: Test via API (FASTER)
Direct API testing to verify backend behavior.

**Steps**:
1. Get auth token
2. Create event via API
3. Add email groups via API
4. Publish event via API
5. Cancel event via API
6. Check logs for email sending

**Pros**: Faster, more controlled
**Cons**: Doesn't test UI integration

---

## API Testing Instructions (Option 2)

### Prerequisites
You'll need:
- Valid auth token (JWT)
- Event organizer role
- Email group IDs or newsletter subscribers in database

### Step 1: Verify Migration Applied

**Check Database** (via Azure portal or connection string):
```sql
SELECT * FROM communications.email_templates
WHERE name = 'event-cancelled-notification';
```

**Expected Result**: 1 row with:
- `name`: `event-cancelled-notification`
- `type`: `transactional`
- `category`: `Events`
- `is_active`: `true`
- `html_template`: Contains orange/rose gradient branding
- `subject_template`: `Event Cancelled: {{EventTitle}} - LankaConnect`

### Step 2: Get Auth Token

**Endpoint**: `POST /api/Auth/login`

```bash
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "your-organizer-email@example.com",
    "password": "your-password"
  }'
```

**Save the token** from response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": { ... }
}
```

### Step 3: Create Test Event

**Endpoint**: `POST /api/Events`

**IMPORTANT**: Include email groups AND location for comprehensive testing

```bash
TOKEN="your-token-here"

curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Phase 6A.63 Test Event - Cancellation Emails",
    "description": "Testing event cancellation email consolidation (registrations + email groups + newsletter subscribers)",
    "startDate": "2026-01-15T18:00:00Z",
    "endDate": "2026-01-15T21:00:00Z",
    "location": {
      "name": "Test Venue",
      "address": {
        "street": "123 Test Street",
        "city": "Orlando",
        "state": "Florida",
        "zipCode": "32801"
      }
    },
    "category": "Social",
    "ticketPrice": 0,
    "maxAttendees": 50,
    "emailGroupIds": ["<email-group-id-1>", "<email-group-id-2>"]
  }'
```

**Save the event ID** from response:
```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "title": "Phase 6A.63 Test Event - Cancellation Emails",
  "status": "Draft",
  ...
}
```

### Step 4: Publish Event

**Endpoint**: `POST /api/Events/{id}/publish`

```bash
EVENT_ID="your-event-id-here"

curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$EVENT_ID/publish" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"
```

**This triggers**: EventPublishedEventHandler → sends emails to email groups + newsletter subscribers

**Expected Result**:
```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "status": "Published",
  ...
}
```

### Step 5: (Optional) Add Registrations

**Endpoint**: `POST /api/Events/{id}/register`

```bash
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$EVENT_ID/register" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": "'"$EVENT_ID"'",
    "attendees": [
      {
        "firstName": "Test",
        "lastName": "User",
        "email": "testuser1@example.com",
        "phoneNumber": "+1234567890"
      }
    ]
  }'
```

**Repeat** for 1-2 more registrations if testing consolidation.

### Step 6: Cancel Event (THE KEY TEST!)

**Endpoint**: `POST /api/Events/{id}/cancel`

```bash
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$EVENT_ID/cancel" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Testing Phase 6A.63 - event cancellation email consolidation (registrations + email groups + newsletter subscribers)"
  }'
```

**Expected Result**:
```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "status": "Cancelled",
  "cancellationReason": "Testing Phase 6A.63 - event cancellation email consolidation...",
  ...
}
```

### Step 7: Verify Email Sending in Logs

**Check Azure Container Logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow false \
  --tail 500 | grep "\[Phase 6A.63\]"
```

**Expected Log Entries**:
```
[Phase 6A.63] EventCancelledEventHandler INVOKED - Event {EventId}, Cancelled At {CancelledAt}
[Phase 6A.63] Found {Count} confirmed registrations for Event {EventId}
[Phase 6A.63] Resolved {Count} notification recipients for Event {EventId}. Breakdown: EmailGroups={X}, Metro={Y}, State={Z}, AllLocations={W}
[Phase 6A.63] Sending cancellation emails to {TotalCount} unique recipients for Event {EventId}. Breakdown: Registrations={A}, EmailGroups={B}, Newsletter={C}
[Phase 6A.63] Event cancellation emails completed for event {EventId}. Success: {SuccessCount}, Failed: {FailCount}
```

### Step 8: Verify Email Template Rendering

**Check Sent Emails** (if you have access to test email accounts):

**Expected Email Content**:
- ✅ Subject: `Event Cancelled: Phase 6A.63 Test Event - Cancellation Emails - LankaConnect`
- ✅ Header: Orange/rose gradient (#8B1538 → #FF6600 → #2d5016)
- ✅ Event details box: Orange accent border (#FF6600)
- ✅ Cancellation reason box: Red accent border (#DC2626)
- ✅ Footer: Orange/rose gradient
- ✅ All template variables replaced correctly

---

## Test Scenarios

### Scenario 1: Registrations Only ✅
- Event with 2 confirmed registrations
- No email groups, no newsletter subscribers
- **Expected**: 2 emails sent

**Verification**:
```
[Phase 6A.63] Sending cancellation emails to 2 unique recipients
Breakdown: Registrations=2, EmailGroups=0, Newsletter=0
[Phase 6A.63] Event cancellation emails completed. Success: 2, Failed: 0
```

### Scenario 2: Email Groups Only ⚠️ CRITICAL TEST
- Event with 0 registrations
- 1 email group with 5 addresses
- No newsletter subscribers
- **Expected**: 5 emails sent (previously sent ZERO - bug!)

**Verification**:
```
[Phase 6A.63] Found 0 confirmed registrations
[Phase 6A.63] Resolved 5 notification recipients. Breakdown: EmailGroups=5, Metro=0, State=0, AllLocations=0
[Phase 6A.63] Sending cancellation emails to 5 unique recipients
Breakdown: Registrations=0, EmailGroups=5, Newsletter=0
[Phase 6A.63] Event cancellation emails completed. Success: 5, Failed: 0
```

**This scenario PROVES the bug is fixed!**

### Scenario 3: Newsletter Subscribers Only ⚠️ CRITICAL TEST
- Event in Orlando, Florida (metro area with subscribers)
- 0 registrations, no email groups
- **Expected**: X emails sent to metro area subscribers (previously sent ZERO - bug!)

**Verification**:
```
[Phase 6A.63] Found 0 confirmed registrations
[Phase 6A.63] Resolved X notification recipients. Breakdown: EmailGroups=0, Metro=X, State=Y, AllLocations=Z
[Phase 6A.63] Sending cancellation emails to X unique recipients
Breakdown: Registrations=0, EmailGroups=0, Newsletter=X
[Phase 6A.63] Event cancellation emails completed. Success: X, Failed: 0
```

### Scenario 4: Full Consolidation (All Types) ✅
- Event with:
  - 2 confirmed registrations
  - 1 email group with 5 addresses
  - Location with 3 newsletter subscribers
  - 1 duplicate email between registration and email group
- **Expected**: 9 unique emails (2+5+3-1 duplicate)

**Verification**:
```
[Phase 6A.63] Found 2 confirmed registrations
[Phase 6A.63] Resolved 8 notification recipients. Breakdown: EmailGroups=5, Metro=3, State=0, AllLocations=0
[Phase 6A.63] Sending cancellation emails to 9 unique recipients (deduplication worked!)
Breakdown: Registrations=2, EmailGroups=5, Newsletter=3
[Phase 6A.63] Event cancellation emails completed. Success: 9, Failed: 0
```

### Scenario 5: Zero Recipients ✅
- Event with no registrations, no email groups, no newsletter subscribers
- **Expected**: 0 emails sent, early exit with log message

**Verification**:
```
[Phase 6A.63] Found 0 confirmed registrations
[Phase 6A.63] Resolved 0 notification recipients. Breakdown: EmailGroups=0, Metro=0, State=0, AllLocations=0
[Phase 6A.63] No recipients found for Event {EventId}, skipping cancellation emails
```

---

## UI Testing Instructions (Option 1)

### Step 1: Start Local UI
```bash
cd web
npm run dev
```

**UI should open at**: http://localhost:3000 (pointing to staging API)

### Step 2: Login as Event Organizer
- Navigate to http://localhost:3000/login
- Login with organizer credentials

### Step 3: Create Event with Email Groups
1. Go to "Create Event" page
2. Fill in event details:
   - Title: "Phase 6A.63 UI Test - Cancellation Emails"
   - Description: "Testing event cancellation email consolidation"
   - Date/Time: Future date
   - Location: Orlando, Florida (or any metro area with newsletter subscribers)
   - Category: Social
   - Ticket Price: Free
3. **IMPORTANT**: Select email groups (at least 1)
4. Save as Draft

### Step 4: Publish Event
1. Navigate to Dashboard
2. Find your event in "Created Events"
3. Click "Publish"
4. **This triggers**: EventPublishedEventHandler → sends emails to email groups + newsletter subscribers

### Step 5: (Optional) Register Attendees
1. Open event details page
2. Click "Register"
3. Fill in attendee details
4. Submit registration (repeat 1-2 times)

### Step 6: Cancel Event
1. Navigate to Dashboard
2. Find your event in "Created Events"
3. Click "Cancel" (or "Manage" → "Cancel Event")
4. **IMPORTANT**: Enter cancellation reason (minimum 10 characters)
   - Example: "Testing Phase 6A.63 - event cancellation email consolidation"
5. Confirm cancellation

### Step 7: Verify Email Sending
**Check Azure Logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow false \
  --tail 500 | grep "\[Phase 6A.63\]"
```

**Expected**: Logs showing emails sent to ALL recipients (registrations + email groups + newsletter subscribers)

---

## Success Criteria

### ✅ Phase 6A.63 is successful if:
1. ✅ Database template `event-cancelled-notification` exists and is active
2. ✅ Event cancellation sends emails to confirmed registrations (existing behavior)
3. ✅ Event cancellation sends emails to email group recipients (NEW - bug fix!)
4. ✅ Event cancellation sends emails to newsletter subscribers (NEW - bug fix!)
5. ✅ Recipient consolidation deduplicates emails (case-insensitive)
6. ✅ Email template renders with orange/rose gradient branding
7. ✅ Cancellation reason appears in email
8. ✅ Logs show `[Phase 6A.63]` markers with correct recipient breakdown
9. ✅ Zero recipients correctly exits early without errors

---

## Troubleshooting

### Migration Not Applied
**Symptom**: Template not found in database

**Check**:
```sql
SELECT * FROM public.__efmigrationshistory
WHERE "MigrationId" LIKE '%Phase6A63%';
```

**If missing**:
- Check deployment logs: `gh run view 20660160985 --log`
- Check container startup logs for migration errors
- Manually run migration if needed

### No Emails Sent (Logs Show Handler Not Invoked)
**Symptom**: No `[Phase 6A.63]` logs appear

**Possible Causes**:
1. Event status not changed to Cancelled
2. Domain event not raised
3. MediatR not dispatching domain events

**Check**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow false \
  --tail 1000 | grep -i "cancel"
```

### Emails Only Sent to Registrations (Bug Not Fixed)
**Symptom**: Logs show `EmailGroups=0, Newsletter=0` when event has email groups

**Possible Causes**:
1. Event not associated with email groups
2. EventNotificationRecipientService not resolving recipients
3. Old code still deployed

**Verify Deployment**:
```bash
# Check latest commit deployed
gh run view 20660160985 --json headSha,conclusion,headCommit
```

**Expected**: Commit c6c6da60 or later

### Template Not Rendering (Plain Text Email)
**Symptom**: Email received but no branding/styling

**Possible Causes**:
1. Template name mismatch (should be `event-cancelled-notification`)
2. EmailService not loading template from database
3. HTML rendering disabled

**Check SendGrid/Email Service Logs**:
Look for template loading errors

---

## Next Steps After Testing

### If All Tests Pass ✅
1. Update [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md):
   - Mark Phase 6A.63 as "✅ Complete"
   - Add test results and email counts
2. Update [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md):
   - Mark Phase 6A.63 as complete
   - Link to PHASE_6A63_EVENT_CANCELLATION_SUMMARY.md
3. Update [EMAIL_SYSTEM_REMAINING_WORK_PLAN.md](./EMAIL_SYSTEM_REMAINING_WORK_PLAN.md):
   - Check off all Phase 6A.63 deployment checklist items
4. **Deploy to Production**:
   ```bash
   git checkout master
   git merge develop
   git push origin master
   ```

### If Tests Fail ❌
1. Document failure details in [PHASE_6A63_EVENT_CANCELLATION_SUMMARY.md](./PHASE_6A63_EVENT_CANCELLATION_SUMMARY.md)
2. Create issue with error logs
3. Fix and redeploy
4. Retest

---

## Summary

**Phase 6A.63** fixes a **CRITICAL BUG** where event cancellation emails were only sent to confirmed registrations, missing email groups and newsletter subscribers.

**Testing Focus**:
- ✅ Scenario 2 (Email Groups Only) - CRITICAL
- ✅ Scenario 3 (Newsletter Subscribers Only) - CRITICAL
- ✅ Verify recipient consolidation in logs

**Recommended Testing Approach**:
1. **Quick API Test** (15-20 minutes): Verify Scenario 2 via API
2. **Full UI Test** (30-45 minutes): Test all scenarios via UI
3. **Check Sent Emails**: Verify template rendering

**Questions?**
- Check [PHASE_6A63_EVENT_CANCELLATION_SUMMARY.md](./PHASE_6A63_EVENT_CANCELLATION_SUMMARY.md) for implementation details
- Check [RCA_Event_Cancellation_Email_No_Recipients.md](./RCA_Event_Cancellation_Email_No_Recipients.md) for root cause analysis

---

**Document Version**: 1.0
**Created By**: Claude Sonnet 4.5
**Last Updated**: 2026-01-02 (Deployment completed 14:43:09 UTC)
