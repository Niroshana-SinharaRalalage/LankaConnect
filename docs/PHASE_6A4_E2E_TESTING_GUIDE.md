# Phase 6A.4: Stripe Payment Integration - E2E Testing Guide

**Document Version**: 1.0
**Created**: 2025-11-26
**Purpose**: End-to-end testing guide for Stripe payment integration
**Environment**: Azure Staging + Local UI

---

## Prerequisites

### 1. Environment Setup
- ✅ Azure staging API deployed: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`
- ✅ Local UI running: `http://localhost:3000` (or 3001 if port conflict)
- ✅ Stripe test mode API keys configured in Azure App Settings
- ✅ Frontend Stripe packages installed (@stripe/stripe-js, @stripe/react-stripe-js)

### 2. Required Accounts
- **LankaConnect Account**: Event Organizer role with active free trial
- **Stripe Test Account**: Access to Stripe Dashboard test mode

### 3. Required Information
- Stripe Test Publishable Key: `pk_test_...`
- Stripe Test Secret Key: `sk_test_...`
- Test card numbers (see below)

---

## Stripe Test Cards

### Primary Test Cards
| Card Number | Description | Expected Result |
|------------|-------------|-----------------|
| `4242 4242 4242 4242` | Visa - Success | Payment succeeds |
| `4000 0056 0000 0004` | Visa - Decline | Card declined error |
| `4000 0000 0000 3220` | Visa - 3D Secure | Authentication required |
| `4000 0025 0000 3155` | Visa - Insufficient Funds | Insufficient funds error |

### Card Details to Use
- **Expiration Date**: Any future date (e.g., 12/34)
- **CVC**: Any 3 digits (e.g., 123)
- **ZIP Code**: Any 5 digits (e.g., 12345)

---

## Testing Scenarios

### Scenario 1: Monthly Subscription Upgrade (Success Path)

**Objective**: Verify successful monthly subscription purchase

**Steps**:
1. **Login to LankaConnect**
   - Navigate to `http://localhost:3000/login`
   - Login with Event Organizer credentials
   - Verify you're in free trial period

2. **Navigate to Dashboard**
   - URL: `http://localhost:3000/dashboard`
   - Verify FreeTrialCountdown component is visible
   - Should show "X days remaining in your 6-month free trial"

3. **Open Subscription Modal**
   - Click "Subscribe Now - $10/month" button
   - Modal should appear with title "Upgrade to Event Organizer Plan"
   - Verify billing toggle is set to "Monthly" by default

4. **Verify Pricing Display**
   - Monthly option should show: `$20.00/monthly`
   - Annual option should show: `$200.00/annual` with "Save 17%" badge
   - Features list should display 6 features

5. **Initiate Checkout**
   - Keep "Monthly" selected
   - Click "Subscribe Now - $20.00/monthly" button
   - Loading spinner should appear
   - Wait for redirect to Stripe Checkout (new tab/window)

6. **Complete Stripe Checkout**
   - **Email**: Use your test email (e.g., `test@example.com`)
   - **Card Number**: `4242 4242 4242 4242`
   - **Expiration**: `12/34`
   - **CVC**: `123`
   - **ZIP**: `12345`
   - Click "Subscribe" button

7. **Verify Success Redirect**
   - Should redirect back to: `http://localhost:3000/dashboard?checkout=success`
   - Dashboard should reload
   - FreeTrialCountdown should update to show "Active Subscription" (green badge)

8. **Verify Backend Updates**
   - Check browser Network tab → Filter by `/api/users/me`
   - Response should show:
     ```json
     {
       "subscriptionStatus": "Active",
       "stripeCustomerId": "cus_...",
       "stripeSubscriptionId": "sub_..."
     }
     ```

**Expected Results**:
- ✅ Payment succeeds
- ✅ User redirected back to dashboard
- ✅ Subscription status updated to "Active"
- ✅ Stripe customer and subscription IDs stored
- ✅ UI reflects active subscription state

---

### Scenario 2: Annual Subscription Upgrade (Success Path)

**Objective**: Verify successful annual subscription purchase with discount

**Steps**:
1. Login and navigate to dashboard (same as Scenario 1, steps 1-2)

2. **Open Modal and Select Annual**
   - Click "Subscribe Now" button
   - Modal opens
   - Click "Annual" billing toggle
   - Verify price changes to `$200.00/annual`
   - Verify monthly equivalent shows: `($16.67/month when billed annually)`

3. **Complete Checkout**
   - Click "Subscribe Now - $200.00/annual"
   - Redirect to Stripe Checkout
   - Enter card: `4242 4242 4242 4242`
   - Complete checkout

4. **Verify Success**
   - Redirect to dashboard with `?checkout=success`
   - Subscription status: "Active"
   - Annual billing cycle set in Stripe

**Expected Results**:
- ✅ Annual price correctly displayed ($200)
- ✅ Savings badge shows "Save 17%"
- ✅ Payment succeeds
- ✅ Annual subscription created in Stripe

---

### Scenario 3: Declined Card (Error Handling)

**Objective**: Verify proper error handling for declined cards

**Steps**:
1. Login and open subscription modal

2. **Initiate Checkout**
   - Select Monthly
   - Click "Subscribe Now"
   - Redirect to Stripe Checkout

3. **Enter Declined Card**
   - Email: `test@example.com`
   - Card: `4000 0056 0000 0004` (Decline card)
   - Expiration: `12/34`
   - CVC: `123`
   - Click "Subscribe"

4. **Verify Error Handling**
   - Stripe should show error: "Your card was declined."
   - User should remain on Stripe Checkout page
   - No redirect back to LankaConnect
   - No subscription created

**Expected Results**:
- ✅ Error message displayed in Stripe UI
- ✅ User can retry with different card
- ✅ No partial subscription created
- ✅ User subscription status unchanged

---

### Scenario 4: Cancel Checkout Flow

**Objective**: Verify proper handling when user cancels

**Steps**:
1. Login and open subscription modal

2. **Initiate Checkout**
   - Click "Subscribe Now"
   - Redirect to Stripe Checkout

3. **Cancel Checkout**
   - Click browser back button OR
   - Click "← Back" link on Stripe page

4. **Verify Cancel Redirect**
   - Should redirect to: `http://localhost:3000/dashboard?checkout=canceled`
   - Dashboard should reload
   - Subscription status should remain "Trialing"
   - FreeTrialCountdown should still show trial days remaining

**Expected Results**:
- ✅ User redirected back to dashboard
- ✅ Query param indicates cancellation
- ✅ No subscription created
- ✅ User can retry subscription later

---

### Scenario 5: Modal Close Without Checkout

**Objective**: Verify modal can be closed without initiating checkout

**Steps**:
1. Login and navigate to dashboard

2. **Open and Close Modal**
   - Click "Subscribe Now"
   - Modal appears
   - Click X button (top right)
   - Modal should close
   - Dashboard should remain unchanged

3. **Reopen Modal**
   - Click "Subscribe Now" again
   - Modal should reopen with same state
   - No errors in console

**Expected Results**:
- ✅ Modal opens and closes smoothly
- ✅ No console errors
- ✅ State resets when reopened
- ✅ No API calls made until "Subscribe Now" clicked

---

### Scenario 6: Expired Trial (Subscribe Now)

**Objective**: Test subscription upgrade for expired trial users

**Prerequisites**:
- User account with `subscriptionStatus: "Expired"`
- `freeTrialEndsAt` date in the past

**Steps**:
1. Login with expired trial account

2. **Verify Expired State**
   - FreeTrialCountdown should show red banner
   - Message: "Your free trial has ended. Subscribe to continue creating events."
   - Button text: "Subscribe Now - $10/month"

3. **Complete Subscription**
   - Click "Subscribe Now"
   - Complete checkout with test card
   - Verify redirect and subscription activation

**Expected Results**:
- ✅ Expired state clearly communicated
- ✅ Subscription flow works identically
- ✅ Subscription activates successfully
- ✅ UI updates to "Active Subscription"

---

## API Endpoint Testing

### 1. GET /api/payments/config
**Purpose**: Retrieve Stripe publishable key for client-side

```bash
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/payments/config
```

**Expected Response**:
```json
{
  "publishableKey": "pk_test_..."
}
```

### 2. POST /api/payments/create-checkout-session
**Purpose**: Create Stripe Checkout session

**Request** (requires JWT token):
```bash
curl -X POST \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/payments/create-checkout-session \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "priceId": "price_organizer_monthly",
    "successUrl": "http://localhost:3000/dashboard?checkout=success",
    "cancelUrl": "http://localhost:3000/dashboard?checkout=canceled"
  }'
```

**Expected Response**:
```json
{
  "sessionId": "cs_test_...",
  "sessionUrl": "https://checkout.stripe.com/c/pay/cs_test_..."
}
```

### 3. POST /api/payments/webhook
**Purpose**: Process Stripe webhook events

**Note**: This endpoint is called by Stripe, not the frontend. Testing requires Stripe CLI.

---

## Browser Developer Tools Checks

### Network Tab Monitoring
1. **Open DevTools** (F12)
2. **Navigate to Network tab**
3. **Filter by XHR/Fetch**
4. **Monitor these requests**:
   - `POST /api/payments/create-checkout-session` - Status 200
   - `GET /api/users/me` - Updated subscription status after payment

### Console Tab Monitoring
**No errors should appear during**:
- Modal open/close
- Billing interval toggle
- Checkout session creation
- Payment completion redirect

### Application Tab Checks
1. **Local Storage**: Check for any cached payment data (should be none)
2. **Session Storage**: Verify auth token present
3. **Cookies**: Check for secure, httpOnly flags on auth cookies

---

## Stripe Dashboard Verification

### After Successful Payment
1. **Login to Stripe Dashboard** (test mode)
2. **Navigate to Customers**
   - New customer should appear
   - Email matches test account
   - Metadata contains `user_id`

3. **Navigate to Subscriptions**
   - Active subscription listed
   - Correct pricing tier (Monthly $20 or Annual $200)
   - Status: "Active"

4. **Navigate to Payments**
   - Successful payment recorded
   - Amount matches selected tier
   - Payment method: Visa ending in 4242

---

## Common Issues and Troubleshooting

### Issue 1: "Payment processing error"
**Symptoms**: Error message in modal after clicking "Subscribe Now"

**Possible Causes**:
- Stripe API keys not configured in Azure
- Network timeout (staging API slow)
- Invalid price IDs in frontend configuration

**Solution**:
1. Check Azure App Settings for `Stripe__SecretKey`
2. Verify price IDs match backend configuration
3. Check browser console for error details

### Issue 2: Redirect doesn't return to dashboard
**Symptoms**: After payment, stuck on Stripe page

**Possible Causes**:
- Success URL incorrectly configured
- Browser blocking redirect
- CORS issues

**Solution**:
1. Verify success URL in checkout session request
2. Check browser console for errors
3. Try different browser

### Issue 3: Subscription status not updating
**Symptoms**: Payment succeeds but UI still shows "Trialing"

**Possible Causes**:
- Webhook not configured (expected for now)
- User state not refreshed after redirect
- Cache issue

**Solution**:
1. Refresh page manually
2. Clear browser cache and retry
3. Check `/api/users/me` response in Network tab

---

## Test Data Cleanup

### After Testing
1. **Delete test subscriptions in Stripe Dashboard**
   - Prevents unnecessary test charges
   - Keeps dashboard clean

2. **Reset test user accounts in database**
   ```sql
   UPDATE users
   SET subscription_status = 1,  -- Trialing
       stripe_customer_id = NULL,
       stripe_subscription_id = NULL
   WHERE email = 'test@example.com';
   ```

---

## Success Criteria

### Phase 6A.4 is 100% complete when:
- ✅ All 6 test scenarios pass
- ✅ API endpoints return correct responses
- ✅ No console errors during payment flow
- ✅ Stripe Dashboard shows correct customer/subscription data
- ✅ UI updates correctly after payment success/failure
- ✅ Error handling works for declined cards
- ✅ Cancel flow returns user to dashboard
- ✅ Modal state management works correctly

---

## Next Steps (Post-Testing)

### If All Tests Pass:
1. ✅ Mark Phase 6A.4 as 100% complete
2. ✅ Update PROGRESS_TRACKER.md with test results
3. ✅ Update STREAMLINED_ACTION_PLAN.md to complete status
4. ✅ Commit E2E test documentation

### If Issues Found:
1. Document failed test scenarios
2. Create bug tickets with reproduction steps
3. Fix issues and retest
4. Update documentation with findings

---

## Appendix: Stripe CLI Testing (Optional)

### Install Stripe CLI
```bash
# Windows (via Scoop)
scoop install stripe

# Verify installation
stripe --version
```

### Test Webhook Locally
```bash
# Login to Stripe
stripe login

# Forward webhooks to local API
stripe listen --forward-to https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/payments/webhook

# Trigger test events
stripe trigger payment_intent.succeeded
stripe trigger customer.subscription.created
```

---

**Document Status**: Ready for E2E Testing
**Review Date**: 2025-11-26
**Approved By**: Development Team
