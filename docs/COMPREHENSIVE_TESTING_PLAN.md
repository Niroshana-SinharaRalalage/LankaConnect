# Comprehensive Testing Plan - Event Registration & Payment Flow

## Overview

This document outlines the complete testing plan for event registration, payment integration, and email functionality following the Stripe API key configuration fix.

## Prerequisites

- ✅ Database migration deployed (Phase 6A.4 - Stripe tables restored)
- 🔄 Stripe API keys configured in Azure Key Vault
- 🔄 Deployment workflow fixed (system-assigned identity + secretref pattern)
- 🔄 Zero-tolerance build passing
- Event ID for testing: `68f675f1-327f-42a9-be9e-f66148d826c3`

## Test User Credentials

- **Email**: `niroshhh2@gmail.com`
- **Password**: `12!@qwASzx`
- **User ID**: `5e782b4d-29ed-4e1d-9039-6c8f698aeea9` (from previous login)

## Test Scenarios

### 1. Authentication Test

**Endpoint**: `POST /api/auth/login`

**Request**:
```bash
curl -i -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "niroshhh2@gmail.com",
    "password": "12!@qwASzx",
    "rememberMe": true,
    "ipAddress": "127.0.0.1"
  }'
```

**Expected Response**:
- HTTP 200 OK
- JSON body with JWT token
- Store token in `fresh_token.txt`

**Success Criteria**:
- Valid JWT token returned
- Token can be used for authenticated requests

---

### 2. Free Event Registration (Legacy Format)

**Endpoint**: `POST /api/Events/{id}/rsvp`

**Request**:
```bash
curl -i -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/68f675f1-327f-42a9-be9e-f66148d826c3/rsvp" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "userId": "5e782b4d-29ed-4e1d-9039-6c8f698aeea9",
    "quantity": 2
  }'
```

**Expected Response**:
- HTTP 204 No Content (legacy format returns no body)
- OR HTTP 200 with null checkout URL (for free events)

**Success Criteria**:
- Registration created in database
- No Stripe customer/session created (event is free)
- User can view registration in their profile

**Database Verification**:
```sql
SELECT * FROM events.registrations
WHERE event_id = '68f675f1-327f-42a9-be9e-f66148d826c3'
  AND user_id = '5e782b4d-29ed-4e1d-9039-6c8f698aeea9'
ORDER BY created_at DESC;
```

---

### 3. Paid Event Registration (New Format with Payment)

**Endpoint**: `POST /api/Events/{id}/rsvp`

**Request**:
```bash
curl -i -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/68f675f1-327f-42a9-be9e-f66148d826c3/rsvp" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "userId": "5e782b4d-29ed-4e1d-9039-6c8f698aeea9",
    "attendees": [
      {"name": "Test User", "age": 30}
    ],
    "email": "niroshhh2@gmail.com",
    "phoneNumber": "+1234567890",
    "successUrl": "https://lankaconnect-staging.azurewebsites.net/events/payment/success",
    "cancelUrl": "https://lankaconnect-staging.azurewebsites.net/events/payment/cancel"
  }'
```

**Expected Response**:
- HTTP 200 OK
- JSON body with Stripe Checkout session URL
```json
{
  "isSuccess": true,
  "value": "https://checkout.stripe.com/c/pay/cs_test_..."
}
```

**Success Criteria**:
- ✅ Stripe customer created (check `payments.stripe_customers` table)
- ✅ Stripe checkout session created
- ✅ Checkout URL is valid and accessible
- ✅ No "Invalid API Key" errors in logs
- ✅ Registration created in pending payment state

**Database Verification**:
```sql
-- Check Stripe customer
SELECT * FROM payments.stripe_customers
WHERE user_id = '5e782b4d-29ed-4e1d-9039-6c8f698aeea9';

-- Check registration
SELECT * FROM events.registrations
WHERE event_id = '68f675f1-327f-42a9-be9e-f66148d826c3'
  AND user_id = '5e782b4d-29ed-4e1d-9039-6c8f698aeea9'
ORDER BY created_at DESC;
```

**Azure Logs Verification**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep -i stripe
```

Look for:
- ✅ "Creating Stripe customer"
- ✅ "Stripe customer created: cus_..."
- ✅ "Creating Stripe checkout session"
- ❌ NO "Invalid API Key" errors

---

### 4. Email Sending Test

**After successful registration, verify email was sent**

**Check Azure logs for email sending**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep -i email
```

**Success Criteria**:
- Email sent to `niroshhh2@gmail.com`
- Email contains registration confirmation
- Email received in inbox (check spam folder)

---

### 5. Stripe Dashboard Verification

**Manual verification in Stripe Dashboard**:
1. Go to https://dashboard.stripe.com/test/customers
2. Search for customer email: `niroshhh2@gmail.com`
3. Verify customer exists
4. Go to https://dashboard.stripe.com/test/checkout-sessions
5. Verify checkout session created for the event
6. Check session status and payment intent

---

### 6. UI Testing Safety Check

**Before proceeding with UI testing**:

1. ✅ All API endpoints tested and working
2. ✅ Free event registration works
3. ✅ Paid event registration returns checkout URL
4. ✅ Stripe integration verified
5. ✅ Email sending verified
6. ✅ Database records created correctly

**If all above pass**: ✅ **SAFE TO TEST UI**

**UI Test Flow**:
1. Login to web app: https://lankaconnect-staging.azurewebsites.net
2. Navigate to Events page
3. Select the test event
4. Click "Register" or "RSVP"
5. Fill registration form
6. For paid events: Verify redirect to Stripe Checkout
7. Complete test payment with Stripe test card: `4242 4242 4242 4242`
8. Verify redirect back to success URL
9. Check registration appears in user profile

---

## Newly Created APIs (Phase 6A Coverage)

### Phase 6A.11 - Multi-Attendee Registration
- ✅ Enhanced RSVP endpoint with attendees array
- ✅ Support for contact information (email, phone, address)

### Phase 6A.23 - Stripe Payment Integration
- ✅ Stripe customer creation
- ✅ Stripe checkout session creation
- ✅ Payment success/cancel callbacks
- ✅ Fixed database tables (Phase 6A.4)

### Phase 6A.24 - Ticket Generation & Email (IN PROGRESS)
- ⏸️ Ticket PDF generation (NOT YET TESTED)
- ⏸️ Email with ticket attachment (NOT YET TESTED)
- ⏸️ Resend ticket email endpoint (NOT YET TESTED)

### Phase 6A.25 - Email Groups
- ✅ Email group management APIs (created but not tested here)

### Phase 6A.28 - Open Signup Items
- ✅ Add/Update/Cancel open signup item APIs (just added to codebase)

### Phase 6A.29 - Badge Enhancements
- ✅ Badge creator name display (completed)
- ✅ Badge preview dialog (completed)

---

## Testing Status Summary

| Feature | API Created | API Tested | UI Tested | Status |
|---------|------------|-----------|-----------|--------|
| Authentication | ✅ | ✅ | - | Complete |
| Free Event Registration | ✅ | ⏸️ | ❌ | Pending |
| Paid Event Registration | ✅ | ⏸️ | ❌ | Pending |
| Stripe Customer Creation | ✅ | ⏸️ | - | Blocked (config) |
| Stripe Checkout | ✅ | ⏸️ | ❌ | Blocked (config) |
| Email Sending | ✅ | ⏸️ | - | Pending |
| Ticket Generation | ✅ | ❌ | ❌ | Not Started |
| Email Groups | ✅ | ❌ | ❌ | Not Started |
| Open Signup Items | ✅ | ❌ | ❌ | Not Started |

**Legend**:
- ✅ Complete
- ⏸️ Blocked/Pending deployment
- ❌ Not started
- \- Not applicable

---

## Next Steps After Deployment Success

1. ✅ Get fresh authentication token
2. ✅ Test free event registration (legacy format)
3. ✅ Test paid event registration (new format)
4. ✅ Verify Stripe customer in dashboard
5. ✅ Verify Stripe checkout session created
6. ✅ Check email sending logs
7. ✅ Verify no API key errors in logs
8. ✅ Confirm UI testing is safe
9. ⏸️ Continue with Phase 6A.24 ticket generation work

---

## Common Issues & Troubleshooting

### Issue: "Invalid API Key"
- Check Azure Key Vault has correct Stripe keys
- Verify system-assigned identity has Key Vault permissions
- Check Container App env vars use `secretref:stripe-secret-key` pattern
- Review deployment logs for secret retrieval errors

### Issue: "Table does not exist"
- Verify migration 20251213120000_RestoreStripePaymentTables was applied
- Check database schema: `\dt payments.*` in psql
- Run diagnostic query to verify tables exist

### Issue: "Unauthorized" on API calls
- Token expired - get fresh token via login
- Token format: `Authorization: Bearer <token>`
- Verify user exists and credentials correct

### Issue: Email not received
- Check spam folder
- Check Azure logs for SMTP errors
- Verify SMTP configuration in Key Vault
- Check email service quota limits

---

## Deployment Monitoring

**Monitor deployment**:
```bash
gh run watch <RUN_ID> --interval 5
```

**Check Container App status**:
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query '{status: properties.runningStatus, fqdn: properties.configuration.ingress.fqdn}'
```

**Health check**:
```bash
curl -i https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health
```

---

## Success Criteria for "All Done"

✅ **Ready to proceed with UI testing when**:
1. Deployment succeeds (build + deploy + health check)
2. Authentication works (can get valid JWT token)
3. Free event registration works (HTTP 204 or 200 with null)
4. Paid event registration returns Stripe checkout URL
5. Stripe customer created in dashboard
6. No "Invalid API Key" errors in logs
7. Email sending works (check logs and inbox)

**Then and only then**: ✅ **UI testing is safe to proceed**

---

*Document created: 2025-12-14*
*Last updated: 2025-12-14*
*Related: Phase 6A.4, 6A.11, 6A.23, 6A.24*
