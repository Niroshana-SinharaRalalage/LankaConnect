# Phase 6A.64 API Test Scripts

**For Claude to Execute**

---

## Test Configuration

**Base URL**: `https://your-staging-url.com` (Replace with actual staging URL)

**Metro Area IDs for Ohio** (from database):
```
Akron:      39111111-1111-1111-1111-111111111001
Cincinnati: 39111111-1111-1111-1111-111111111002
Cleveland:  39111111-1111-1111-1111-111111111003
Columbus:   39111111-1111-1111-1111-111111111004
Toledo:     39111111-1111-1111-1111-111111111005
```

---

## API Test 1: Newsletter Subscription with Multiple Metro Areas

### Request
```bash
curl -X POST https://your-staging-url.com/api/proxy/newsletter/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "email": "claude-test-phase6a64@example.com",
    "metroAreaIds": [
      "39111111-1111-1111-1111-111111111001",
      "39111111-1111-1111-1111-111111111002",
      "39111111-1111-1111-1111-111111111003",
      "39111111-1111-1111-1111-111111111004",
      "39111111-1111-1111-1111-111111111005"
    ],
    "receiveAllLocations": false
  }'
```

### Expected Response (HTTP 200)
```json
{
  "success": true,
  "data": {
    "id": "<guid>",
    "email": "claude-test-phase6a64@example.com",
    "metroAreaIds": [
      "39111111-1111-1111-1111-111111111001",
      "39111111-1111-1111-1111-111111111002",
      "39111111-1111-1111-1111-111111111003",
      "39111111-1111-1111-1111-111111111004",
      "39111111-1111-1111-1111-111111111005"
    ],
    "receiveAllLocations": false,
    "isActive": true,
    "isConfirmed": false,
    "confirmationToken": "<token>",
    "confirmationSentAt": "<timestamp>"
  }
}
```

### Verification Query
```sql
SELECT
    ns.email,
    COUNT(nsma.metro_area_id) as metro_count,
    STRING_AGG(ma.name, ', ' ORDER BY ma.name) as metro_areas
FROM communications.newsletter_subscribers ns
JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
JOIN events.metro_areas ma ON nsma.metro_area_id = ma.id
WHERE ns.email = 'claude-test-phase6a64@example.com'
GROUP BY ns.email;
```

**Expected**: 1 row with `metro_count = 5`

---

## API Test 2: Newsletter Subscription with Single Metro Area

### Request
```bash
curl -X POST https://your-staging-url.com/api/proxy/newsletter/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "email": "claude-test-single@example.com",
    "metroAreaIds": ["39111111-1111-1111-1111-111111111001"],
    "receiveAllLocations": false
  }'
```

### Expected Response (HTTP 200)
```json
{
  "success": true,
  "data": {
    "email": "claude-test-single@example.com",
    "metroAreaIds": ["39111111-1111-1111-1111-111111111001"],
    "receiveAllLocations": false,
    "isActive": true,
    "isConfirmed": false
  }
}
```

### Verification Query
```sql
SELECT
    ns.email,
    COUNT(nsma.metro_area_id) as metro_count,
    ma.name as metro_area
FROM communications.newsletter_subscribers ns
JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
JOIN events.metro_areas ma ON nsma.metro_area_id = ma.id
WHERE ns.email = 'claude-test-single@example.com'
GROUP BY ns.email, ma.name;
```

**Expected**: 1 row with `metro_count = 1`, `metro_area = 'Akron'`

---

## API Test 3: Newsletter Subscription - Receive All Locations

### Request
```bash
curl -X POST https://your-staging-url.com/api/proxy/newsletter/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "email": "claude-test-all-locations@example.com",
    "metroAreaIds": [],
    "receiveAllLocations": true
  }'
```

### Expected Response (HTTP 200)
```json
{
  "success": true,
  "data": {
    "email": "claude-test-all-locations@example.com",
    "metroAreaIds": [],
    "receiveAllLocations": true,
    "isActive": true,
    "isConfirmed": false
  }
}
```

### Verification Query
```sql
SELECT
    email,
    receive_all_locations,
    is_active,
    is_confirmed
FROM communications.newsletter_subscribers
WHERE email = 'claude-test-all-locations@example.com';
```

**Expected**: 1 row with `receive_all_locations = true`

---

## API Test 4: Validation - No Metro Areas and Not Receive All

### Request
```bash
curl -X POST https://your-staging-url.com/api/proxy/newsletter/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "email": "claude-test-invalid@example.com",
    "metroAreaIds": [],
    "receiveAllLocations": false
  }'
```

### Expected Response (HTTP 400 Bad Request)
```json
{
  "success": false,
  "error": "Must specify at least one metro area or choose to receive all locations"
}
```

---

## API Test 5: Get Confirmed Subscribers by State (Internal Repository Test)

This test requires direct database access or internal API endpoint.

### SQL Query (Mimics Repository Method)
```sql
-- Get all metro areas in Ohio
WITH ohio_metros AS (
    SELECT id, name, state
    FROM events.metro_areas
    WHERE state = 'OH'
)
-- Find all confirmed subscribers with ANY Ohio metro area
SELECT DISTINCT
    ns.id,
    ns.email,
    COUNT(DISTINCT nsma.metro_area_id) as ohio_metro_count,
    STRING_AGG(DISTINCT om.name, ', ' ORDER BY om.name) as subscribed_ohio_metros
FROM communications.newsletter_subscribers ns
JOIN communications.newsletter_subscriber_metro_areas nsma ON ns.id = nsma.subscriber_id
JOIN ohio_metros om ON nsma.metro_area_id = om.id
WHERE ns.is_active = true AND ns.is_confirmed = true
GROUP BY ns.id, ns.email
ORDER BY ns.email;
```

### Expected Results
Should include all confirmed subscribers who have at least one Ohio metro area subscription:
- `varunipw@gmail.com` - 5 Ohio metros (if Test 1 from UI passed)
- `claude-test-phase6a64@example.com` - 5 Ohio metros (if API Test 1 passed and confirmed)

---

## API Test 6: Newsletter Subscription - Duplicate Email

### Setup
First, subscribe with one email:
```bash
curl -X POST https://your-staging-url.com/api/proxy/newsletter/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "email": "claude-test-duplicate@example.com",
    "metroAreaIds": ["39111111-1111-1111-1111-111111111001"],
    "receiveAllLocations": false
  }'
```

Wait for response (HTTP 200).

### Then Try to Subscribe Again
```bash
curl -X POST https://your-staging-url.com/api/proxy/newsletter/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "email": "claude-test-duplicate@example.com",
    "metroAreaIds": ["39111111-1111-1111-1111-111111111002"],
    "receiveAllLocations": false
  }'
```

### Expected Response (HTTP 400 Bad Request)
```json
{
  "success": false,
  "error": "Email is already subscribed to the newsletter"
}
```

---

## Test Execution Checklist

When running these tests, verify:

- [ ] API Test 1: Multiple metro areas subscription succeeds
- [ ] API Test 1: Database has 5 junction table rows
- [ ] API Test 2: Single metro area subscription succeeds
- [ ] API Test 2: Database has 1 junction table row
- [ ] API Test 3: Receive all locations subscription succeeds
- [ ] API Test 3: Database has 0 junction table rows (receive_all_locations = true)
- [ ] API Test 4: Validation error for invalid input
- [ ] API Test 5: State query finds all Ohio subscribers
- [ ] API Test 6: Duplicate email rejection works

---

## Success Criteria

✅ **All API tests pass** when:
1. Newsletter subscription accepts `metroAreaIds` array
2. Junction table stores all metro area IDs correctly
3. Validation prevents invalid subscriptions
4. State-level queries find subscribers with any metro area in state
5. Duplicate email prevention works correctly

---

## Cleanup After Testing

Run this SQL to remove test data:
```sql
-- Delete test subscribers
DELETE FROM communications.newsletter_subscribers
WHERE email LIKE 'claude-test%@example.com';

-- Verify cleanup
SELECT COUNT(*) as remaining_test_subscribers
FROM communications.newsletter_subscribers
WHERE email LIKE 'claude-test%@example.com';

-- Expected: 0
```

---

**Ready for API Testing!** Provide the staging URL and I'll execute these tests.
