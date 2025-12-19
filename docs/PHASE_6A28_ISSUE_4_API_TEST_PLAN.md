# Phase 6A.28 Issue 4 - API Testing Guide

**Fix Deployed:** 2025-12-19
**Commit:** 5a988c30
**Endpoint:** `DELETE /api/events/{eventId}/rsvp?deleteSignUpCommitments=true`

---

## Quick Test Summary

The fix ensures that when users cancel registration with `deleteSignUpCommitments=true`, **user-created Open Items are deleted** from the database (matching the "Cancel Sign Up" button behavior).

---

## Prerequisites

1. **Authentication Token:**
```bash
# Login to get token
curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login \
  -H "Content-Type: application/json" \
  -d @login_test_payload.json
```

Where `login_test_payload.json` contains:
```json
{
  "email": "niroshhh@gmail.com",
  "password": "12!@qwASzx",
  "rememberMe": true,
  "ipAddress": "string"
}
```

2. **Extract Token:**
```bash
# Save accessToken to token.txt for subsequent requests
```

---

## Test Scenario 1: Cancel Registration WITH Deleting Commitments (THE FIX)

### Step 1: Register for Event

```bash
# Find an event
curl -X GET "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events" \
  -H "Authorization: Bearer $(cat token.txt)"

# Register for event (use event ID from above)
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/rsvp" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "5e782b4d-29ed-4e1d-9039-6c8f698aeea9",
    "quantity": 1,
    "attendees": [{"name": "Test User", "age": 30}],
    "email": "niroshhh@gmail.com",
    "phoneNumber": "+1234567890",
    "address": "123 Test Street"
  }'
```

### Step 2: Get Signup Lists

```bash
curl -X GET "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/signup-lists" \
  -H "Authorization: Bearer $(cat token.txt)"
```

**Note the signup list ID for Open items category.**

### Step 3: Create Open Items

```bash
# Create 2-3 Open Items (user-created)
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/signup-lists/{OPEN_LIST_ID}/items" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Content-Type: application/json" \
  -d '{
    "itemDescription": "Test Open Item 1",
    "quantity": 5,
    "notes": "Test item for Issue 4 fix"
  }'

# Repeat for items 2 and 3
```

### Step 4: Commit to Open Items

```bash
# Commit to each Open Item
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/signup-lists/{OPEN_LIST_ID}/items/{ITEM_ID}/commit" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -H "Content-Type: application/json" \
  -d '{
    "quantity": 1,
    "notes": "My commitment"
  }'
```

### Step 5: Verify Items Exist (BEFORE Cancellation)

```bash
curl -X GET "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/signup-lists" \
  -H "Authorization: Bearer $(cat token.txt)" \
  | grep -o "\"itemDescription\":\"Test Open Item"

# Should show all 3 items
```

### Step 6: Cancel Registration WITH Checkbox (**THE TEST**)

```bash
curl -X DELETE "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/rsvp?deleteSignUpCommitments=true" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -w "\nHTTP Status: %{http_code}\n"
```

**Expected:** HTTP 204 No Content

### Step 7: Verify Open Items DELETED (AFTER Cancellation)

```bash
curl -X GET "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/signup-lists" \
  -H "Authorization: Bearer $(cat token.txt)" \
  | grep -o "\"itemDescription\":\"Test Open Item"

# Should return NOTHING - items deleted ✅
```

### Expected Results

- ✅ Registration cancelled
- ✅ Commitments deleted
- ✅ **Open Items DELETED** (this is the fix)
- ✅ API returns no Open Items in subsequent GET requests

### Before Fix (Broken Behavior)

- ❌ Open Items remained in database
- ❌ Items visible in UI with Update/Cancel buttons
- ❌ Page reload showed items still there

---

## Test Scenario 2: Verify Mandatory/Suggested Items Unchanged

### Steps

1. Register for event with Mandatory and Suggested items
2. Commit to Mandatory/Suggested items
3. Cancel registration WITH `deleteSignUpCommitments=true`
4. Verify items STILL EXIST (organizer-owned)

```bash
curl -X GET "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/signup-lists" \
  -H "Authorization: Bearer $(cat token.txt)"

# Mandatory/Suggested items should still be present ✅
```

---

## Test Scenario 3: Cancel WITHOUT Checkbox (Unchanged Behavior)

### Step: Cancel Registration WITHOUT deleteSignUpCommitments

```bash
curl -X DELETE "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/{EVENT_ID}/rsvp" \
  -H "Authorization: Bearer $(cat token.txt)" \
  -w "\nHTTP Status: %{http_code}\n"
```

**Expected:**
- ✅ Registration cancelled
- ✅ Commitments REMAIN
- ✅ Open Items REMAIN

---

## Database Verification (Optional)

If you have PostgreSQL access:

```sql
-- Check Open Items for specific event
SELECT
    si.id,
    si.item_description,
    si.item_category,
    si.created_by_user_id,
    COUNT(sc.id) as commitment_count
FROM sign_up_items si
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
JOIN sign_up_lists sl ON si.sign_up_list_id = sl.id
WHERE sl.event_id = '{EVENT_ID}'
    AND si.item_category = 'Open'
GROUP BY si.id, si.item_description, si.item_category, si.created_by_user_id;

-- After cancellation with deleteSignUpCommitments=true:
-- Open items where created_by_user_id = {USER_ID} should be GONE ✅
```

---

## Success Criteria

Issue 4 is RESOLVED when:

- [x] Code deployed to staging (commit 5a988c30)
- [x] Local build successful (0 errors)
- [x] GitHub Actions deployment successful
- [ ] API test: Open Items deleted when `deleteSignUpCommitments=true`
- [ ] API test: Mandatory/Suggested items remain (unchanged)
- [ ] API test: Items remain when `deleteSignUpCommitments` omitted
- [ ] UI test: Open Items disappear from manage page
- [ ] UI test: Update/Cancel buttons gone after cancellation
- [ ] Database verification: Items deleted from database

---

## Troubleshooting

### 401 Unauthorized
- Token expired (30-minute expiry)
- Re-login to get fresh token

### 400 Bad Request
- Check payload format matches examples
- Ensure userId matches authenticated user

### 404 Not Found
- Event ID or signup list ID incorrect
- Verify IDs from GET requests

### Items Not Deleted
- Check `created_by_user_id` column - only user-created Open items deleted
- Organizer-created Open items will NOT be deleted (by design)
- Verify using `deleteSignUpCommitments=true` query parameter

---

## Implementation Details

**File Changed:** [src/LankaConnect.Domain/Events/Event.cs:1337-1404](../src/LankaConnect.Domain/Events/Event.cs)

**Logic:**
1. Cancel commitment for all items (existing behavior)
2. Check if item is Open AND created by this user (`CreatedByUserId == userId`)
3. If yes, delete the item using `signUpList.RemoveItem(itemId)`
4. Maintain consistency with "Cancel Sign Up" button behavior

**Key Code:**
```csharp
// After canceling commitment
if (item.CreatedByUserId.HasValue && item.CreatedByUserId.Value == userId)
{
    itemsToRemove.Add((signUpList, item.Id));
}

// Delete marked items
foreach (var (signUpList, itemId) in itemsToRemove)
{
    var removeResult = signUpList.RemoveItem(itemId);
}
```

---

## Related Documentation

- **Fix Summary:** [PHASE_6A28_ISSUE_4_OPEN_ITEMS_FIX.md](PHASE_6A28_ISSUE_4_OPEN_ITEMS_FIX.md)
- **Root Cause Analysis:** [PHASE_6A_28_OPEN_ITEMS_ROOT_CAUSE_ANALYSIS.md](PHASE_6A_28_OPEN_ITEMS_ROOT_CAUSE_ANALYSIS.md)
- **Comparison Document:** [architecture/OPEN_ITEMS_VS_MANDATORY_SUGGESTED_COMPARISON.md](architecture/OPEN_ITEMS_VS_MANDATORY_SUGGESTED_COMPARISON.md)

---

**Status:** ✅ Ready for Testing
**Next:** User to validate fix in staging environment using above test plan
