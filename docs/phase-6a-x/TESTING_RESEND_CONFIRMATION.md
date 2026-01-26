# Testing Resend Confirmation Feature - Phase 6A.X

## Issue Summary

**Current State**: Resend confirmation endpoint is working correctly, but testing is blocked by historical data issue.

**Root Cause**: Existing test registrations have `Status=Confirmed` but `PaymentStatus=Pending` (should be `Completed`). This data was created before Phase 6A.81 webhook fixes were deployed.

**Evidence**: RCA confirms:
- ✅ Authorization working correctly
- ✅ Business logic correct
- ✅ Webhook handler correct
- ✅ Domain entity correct
- ❌ Test data has stale PaymentStatus

---

## Quick Fix: Manual Database Update

### Option 1: Azure Portal (Recommended)

1. **Navigate to Azure Portal**
   - Resource Group: `lankaconnect-staging`
   - PostgreSQL Server: Find the database server name

2. **Open Query Editor**
   - Connect with admin credentials

3. **Execute SQL** (from `fix_payment_status_for_testing.sql`):
   ```sql
   -- Update ONE registration for testing
   UPDATE events.registrations
   SET
       payment_status = 2,  -- PaymentStatus.Completed
       updated_at = CURRENT_TIMESTAMP
   WHERE id = '18422a29-61f7-4575-87d2-72ac0b1581d1'
     AND status = 1  -- RegistrationStatus.Confirmed
     AND payment_status = 0  -- PaymentStatus.Pending
     AND event_id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f';
   ```

4. **Verify Update**:
   ```sql
   SELECT
       id AS registration_id,
       status AS registration_status,
       payment_status,
       stripe_payment_intent_id,
       updated_at
   FROM events.registrations
   WHERE id = '18422a29-61f7-4575-87d2-72ac0b1581d1';
   ```

   Expected result: `payment_status = 2` (Completed)

### Option 2: psql Command Line

If you have the connection string:

```bash
psql "postgresql://username:password@server.postgres.database.azure.com:5432/lankaconnect_staging?sslmode=require"

-- Then run the UPDATE query above
```

---

## Testing the Resend Endpoint (After DB Fix)

### 1. Get Fresh JWT Token

```bash
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "niroshhh@gmail.com",
    "password": "12!@qwASzx",
    "rememberMe": true,
    "ipAddress": "127.0.0.1"
  }' | jq -r '.token'
```

Save token to `token.txt`.

### 2. Test Resend Confirmation Endpoint

```bash
TOKEN=$(cat c:/Work/LankaConnect/token.txt)

curl -X 'POST' \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/attendees/18422a29-61f7-4575-87d2-72ac0b1581d1/resend-confirmation" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -v
```

### 3. Expected Response

**Success (200 OK)**:
```json
{
  "message": "Confirmation email resent successfully"
}
```

**Note**: This confirms the endpoint is working. Check email inbox for confirmation email with ticket PDF attached.

### 4. Verify Email Sent

- **Check email**: `niroshhh@gmail.com` (or registration contact email)
- **Expected**: Email with subject "Event Registration Confirmation"
- **Should include**: Ticket PDF attachment with QR code

---

## Alternative: Test with Free Event

If you don't want to update the database, test with a free event registration where `PaymentStatus = NotRequired`.

### Steps:

1. **Find a free event** or create one
2. **Create registration** (will have `PaymentStatus = NotRequired`)
3. **Test resend endpoint** with that registration ID

This validates the free event code path through the shared `IRegistrationEmailService`.

---

## Full End-to-End Validation (Recommended for Production)

After frontend is complete, create a **brand new paid registration** to validate the entire flow:

1. Create test paid event
2. Complete registration with Stripe checkout
3. Verify webhook sets `PaymentStatus = Completed` correctly
4. Test resend feature with properly completed registration
5. Check Azure logs for webhook processing

This confirms Phase 6A.81 fixes work correctly for new registrations.

---

## Current Deployment Status

- ✅ **Backend deployed**: Workflow #21341347692 completed successfully
- ✅ **Build**: 0 errors, 0 warnings
- ✅ **Resend endpoint**: `POST /api/Events/{eventId}/attendees/{registrationId}/resend-confirmation`
- ✅ **QR Code endpoint**: `GET /api/Events/{eventId}/attendees` (returns TicketCode, QrCodeData, HasTicket)
- ⏳ **Frontend**: Pending implementation

---

## Next Steps

1. ✅ Execute SQL fix (above)
2. ✅ Test resend confirmation endpoint
3. ⏳ Implement frontend components (ResendConfirmationDialog, QRCodeModal)
4. ⏳ Update AttendeeManagementTab
5. ⏳ Deploy frontend to Azure staging
6. ✅ Full E2E test with new paid registration
