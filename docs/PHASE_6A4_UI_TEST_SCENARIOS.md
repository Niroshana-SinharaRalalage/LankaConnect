# Phase 6A.4: UI Test Scenarios - What You Can Test

**Local UI**: http://localhost:3001
**API**: Azure Staging (https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io)

---

## 🎯 Quick Start - What You'll See

### Before Stripe Keys Configured
If Stripe keys aren't set in Azure yet, you'll see:
- ✅ Modal opens correctly
- ✅ UI looks good (pricing, toggle, features)
- ❌ "Subscribe Now" button will show error: "Failed to start checkout process"

### After Stripe Keys Configured
- ✅ Modal opens
- ✅ Clicking "Subscribe Now" redirects to Stripe Checkout page
- ✅ Can complete payment with test card
- ✅ Redirects back with success/cancel

---

## 📱 UI Test Scenarios (Visual Testing)

### Scenario 1: **Open Subscription Modal** (Always Works)

**What to do**:
1. Go to http://localhost:3001
2. Login with Event Organizer credentials
3. Navigate to Dashboard (should redirect automatically)
4. Scroll down to find the **FreeTrialCountdown** card

**What you'll see**:
```
┌────────────────────────────────────────┐
│ 🕒 Free Trial                          │
│                                        │
│ 165 days                               │
│ remaining in your 6-month free trial  │
│                                        │
│ Enjoy unlimited event creation...     │
│                                        │
│ [No button if not expiring]           │
└────────────────────────────────────────┘
```

OR (if trial ending in <7 days):
```
┌────────────────────────────────────────┐
│ 🕒 Free Trial                          │
│                                        │
│ 5 days                                 │
│ remaining in your 6-month free trial  │
│                                        │
│ Your trial is ending soon. Subscribe  │
│ now to continue creating events.      │
│                                        │
│ [Subscribe Now - $10/month]           │
└────────────────────────────────────────┘
```

**Test**: Click "Subscribe Now" button

---

### Scenario 2: **View Subscription Modal - Monthly** (Always Works)

**What you'll see after clicking "Subscribe Now"**:

```
┌─────────────────────────────────────────────────────┐
│ Upgrade to Event Organizer Plan               [X]  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────┬─────────────┐                    │
│  │  Monthly    │   Annual    │  ← Billing Toggle  │
│  │ (selected)  │ Save 17%    │                    │
│  └─────────────┴─────────────┘                    │
│                                                     │
│           $20.00                                    │
│           /monthly                                  │
│                                                     │
│  Plan Features                                     │
│  ✓ All General features                           │
│  ✓ Advanced event templates                       │
│  ✓ Priority event placement                       │
│  ✓ Detailed analytics & insights                  │
│  ✓ Custom branding options                        │
│  ✓ Priority support                               │
│                                                     │
│  [Subscribe Now - $20.00/monthly]                 │
│                                                     │
│  Secure payment processing powered by Stripe.     │
│  You can cancel anytime.                          │
└─────────────────────────────────────────────────────┘
```

**UI Tests** (No Stripe keys needed):
- ✅ Modal appears centered
- ✅ Close button (X) works
- ✅ Title shows "Upgrade to Event Organizer Plan"
- ✅ Billing toggle shows "Monthly" and "Annual"
- ✅ "Monthly" is selected by default
- ✅ Price shows "$20.00/monthly"
- ✅ Features list shows 6 items with checkmarks
- ✅ Subscribe button enabled
- ✅ Security notice visible at bottom

**Click Test**:
- Click [X] button → Modal closes ✅
- Click outside modal → Modal stays open (expected) ✅
- Reopen modal → State resets ✅

---

### Scenario 3: **View Subscription Modal - Annual** (Always Works)

**What to do**:
1. Open modal (click "Subscribe Now")
2. Click **"Annual"** toggle button

**What you'll see**:

```
┌─────────────────────────────────────────────────────┐
│ Upgrade to Event Organizer Plan               [X]  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────┬─────────────┐                    │
│  │   Monthly   │   Annual    │  ← Billing Toggle  │
│  │             │ (selected)  │                    │
│  │             │  Save 17%   │                    │
│  └─────────────┴─────────────┘                    │
│                                                     │
│           $200.00                                   │
│           /annual                                   │
│                                                     │
│  ($16.67/month when billed annually)               │
│                                                     │
│  Plan Features                                     │
│  ✓ All General features                           │
│  ✓ Advanced event templates                       │
│  ✓ Priority event placement                       │
│  ✓ Detailed analytics & insights                  │
│  ✓ Custom branding options                        │
│  ✓ Priority support                               │
│                                                     │
│  [Subscribe Now - $200.00/annual]                 │
│                                                     │
│  Secure payment processing powered by Stripe.     │
│  You can cancel anytime.                          │
└─────────────────────────────────────────────────────┘
```

**UI Tests**:
- ✅ Clicking "Annual" updates toggle state
- ✅ Price changes to "$200.00/annual"
- ✅ Monthly equivalent shows "($16.67/month when billed annually)"
- ✅ "Save 17%" badge visible on Annual button
- ✅ Subscribe button text updates to "$200.00/annual"
- ✅ Features list remains same

**Toggle Test**:
- Click Monthly → Annual → Monthly → Annual ✅
- Prices update correctly each time ✅

---

### Scenario 4: **Test Subscribe Button** (Needs Stripe Keys)

**Without Stripe Keys Configured**:
1. Click "Subscribe Now" button in modal
2. Loading spinner appears briefly
3. Error message shows:

```
┌─────────────────────────────────────────────────────┐
│ ...modal content...                                 │
│                                                     │
│  ⚠️ Failed to start checkout process               │
│                                                     │
│  [Subscribe Now - $20.00/monthly]                  │
└─────────────────────────────────────────────────────┘
```

**UI Tests**:
- ✅ Loading spinner shows during API call
- ✅ Subscribe button disabled while loading
- ✅ Error message appears in red banner
- ✅ Can retry after error

**With Stripe Keys Configured** (After Azure setup):
1. Click "Subscribe Now" button
2. Loading spinner appears
3. Redirects to Stripe Checkout page (new tab/window)

**What the Stripe page looks like**:
```
┌─────────────────────────────────────────────────────┐
│  Stripe Logo                                   [X]  │
│                                                     │
│  Complete your subscription                        │
│                                                     │
│  Email: [________________]                         │
│                                                     │
│  Card information:                                 │
│  [1234 5678 9012 3456]  Visa logo                 │
│  [MM/YY]  [CVC]  [ZIP]                            │
│                                                     │
│  Subtotal          $20.00                          │
│  Total due today   $20.00                          │
│                                                     │
│  [Subscribe]                                       │
│                                                     │
│  ← Back to LankaConnect                           │
│                                                     │
│  Powered by Stripe                                 │
└─────────────────────────────────────────────────────┘
```

---

### Scenario 5: **Complete Payment** (Needs Stripe Keys + Test Card)

**Test Card**: `4242 4242 4242 4242` (Success)

**What to do**:
1. On Stripe Checkout page, enter:
   - Email: `test@example.com`
   - Card: `4242 4242 4242 4242`
   - Expiry: `12/34`
   - CVC: `123`
   - ZIP: `12345`
2. Click "Subscribe" button

**What happens**:
1. Stripe processes payment (~2 seconds)
2. Redirects back to: `http://localhost:3001/dashboard?checkout=success`
3. Dashboard reloads
4. FreeTrialCountdown updates:

**Before Payment**:
```
┌────────────────────────────────────────┐
│ 🕒 Free Trial                          │
│ 165 days remaining...                  │
└────────────────────────────────────────┘
```

**After Payment**:
```
┌────────────────────────────────────────┐
│ ✓ Active Subscription                  │
│                                        │
│ Your subscription is active. You have │
│ full access to create and manage      │
│ events.                                │
└────────────────────────────────────────┘
```

**UI Tests**:
- ✅ Green badge with checkmark
- ✅ Title changes to "Active Subscription"
- ✅ Message confirms active status
- ✅ No "Subscribe Now" button
- ✅ URL shows `?checkout=success` query param

---

### Scenario 6: **Test Declined Card** (Needs Stripe Keys)

**Test Card**: `4000 0056 0000 0004` (Decline)

**What to do**:
1. Open modal, click "Subscribe Now"
2. On Stripe page, enter:
   - Card: `4000 0056 0000 0004`
   - Other details: same as above
3. Click "Subscribe"

**What you'll see**:
```
┌─────────────────────────────────────────────────────┐
│  Stripe Logo                                   [X]  │
│                                                     │
│  ⚠️ Your card was declined.                        │
│                                                     │
│  Email: test@example.com                           │
│                                                     │
│  Card information:                                 │
│  [4000 0056 0000 0004]  ⚠️ Error                  │
│  [12/34]  [123]  [12345]                          │
│                                                     │
│  [Try again]                                       │
└─────────────────────────────────────────────────────┘
```

**UI Tests**:
- ✅ Error message shown on Stripe page
- ✅ User stays on Stripe page (no redirect)
- ✅ Can retry with different card
- ✅ LankaConnect subscription status unchanged

---

### Scenario 7: **Test Cancel Checkout** (Needs Stripe Keys)

**What to do**:
1. Open modal, click "Subscribe Now"
2. Redirect to Stripe Checkout page
3. Click **"← Back"** link OR browser back button

**What happens**:
1. Redirects to: `http://localhost:3001/dashboard?checkout=canceled`
2. Dashboard reloads
3. FreeTrialCountdown remains in trial state

**UI Tests**:
- ✅ URL shows `?checkout=canceled` query param
- ✅ Subscription status unchanged
- ✅ Still shows trial days remaining
- ✅ "Subscribe Now" button still available
- ✅ Can retry subscription later

---

### Scenario 8: **Expired Trial State** (Special Account Needed)

**Prerequisites**: User account with expired trial

**What you'll see**:
```
┌────────────────────────────────────────┐
│ ⚠️ Trial Expired                       │
│                                        │
│ Your free trial has ended. Subscribe  │
│ to continue creating events.          │
│                                        │
│ [Subscribe Now - $10/month]           │
└────────────────────────────────────────┘
```

**UI Tests**:
- ✅ Red/orange warning colors
- ✅ Alert icon visible
- ✅ Clear expiration message
- ✅ Subscribe button prominently displayed
- ✅ Modal flow works identically

---

## 🎨 UI/UX Elements to Test

### Colors & Branding
- **Maroon**: `#8B1538` (headings, expired state)
- **Orange**: `#FF7900` (expiring soon warning)
- **Green**: Subscription active state
- **Blue**: Trial active state

### Responsive Design
- ✅ Modal centered on all screen sizes
- ✅ Modal scrollable if content exceeds viewport
- ✅ Max width constraint prevents oversized modal
- ✅ Mobile: Full width with padding

### Accessibility
- ✅ Button states (normal, hover, disabled, loading)
- ✅ Close button clearly visible
- ✅ Semantic HTML (button, heading tags)
- ✅ Focus states on interactive elements

### Loading States
- ✅ Spinner appears when clicking "Subscribe Now"
- ✅ Button text changes to "Processing..."
- ✅ Button disabled during API call
- ✅ Modal can't be closed during loading

---

## 📊 What You Can Test WITHOUT Stripe Keys

| Test | Works Without Keys? | What You'll See |
|------|--------------------|-----------------
| Modal opens | ✅ YES | Full modal UI visible |
| Modal closes (X button) | ✅ YES | Modal closes smoothly |
| Billing toggle Monthly/Annual | ✅ YES | Prices update correctly |
| Price calculations | ✅ YES | $20 monthly, $200 annual |
| Features list displays | ✅ YES | 6 features with checkmarks |
| Modal state resets | ✅ YES | Reopening resets to Monthly |
| Subscribe button enabled | ✅ YES | Button clickable |
| Click Subscribe button | ⚠️ PARTIAL | Shows error: "Failed to start checkout process" |
| Redirect to Stripe | ❌ NO | Needs Stripe keys configured |
| Complete payment | ❌ NO | Needs Stripe keys + test card |

---

## 📊 What You Can Test WITH Stripe Keys

| Test | Needs Stripe Keys? | Additional Requirements |
|------|-------------------|------------------------|
| All above tests | ✅ YES | Plus Stripe keys in Azure |
| Redirect to Stripe Checkout | ✅ YES | Valid price IDs |
| Complete payment (success) | ✅ YES | Test card 4242... |
| Declined card error | ✅ YES | Test card 4000 0056... |
| Cancel checkout flow | ✅ YES | Click back on Stripe page |
| Success redirect | ✅ YES | Complete payment |
| Subscription status update | ✅ YES | Backend webhook (optional for now) |
| Stripe Dashboard verification | ✅ YES | Access to Stripe account |

---

## 🚀 Quick Test Plan (5 Minutes)

### Test 1: UI Only (No Stripe Keys)
1. Open http://localhost:3001
2. Login as Event Organizer
3. Go to Dashboard
4. Click "Subscribe Now"
5. Verify:
   - ✅ Modal opens
   - ✅ Toggle Monthly/Annual works
   - ✅ Prices correct ($20/$200)
   - ✅ Features list visible
   - ✅ Close button works

### Test 2: With Stripe Keys (After Azure Config)
1. Same steps 1-4 above
2. Click "Subscribe Now" in modal
3. Verify:
   - ✅ Redirects to Stripe Checkout
   - ✅ Can enter test card
   - ✅ Payment succeeds with 4242...
   - ✅ Redirects back to dashboard
   - ✅ Status updates to "Active"

---

## 📸 Screenshots to Take (Optional)

1. **FreeTrialCountdown card** (trial state)
2. **Subscription modal** (Monthly selected)
3. **Subscription modal** (Annual selected)
4. **Stripe Checkout page**
5. **Active Subscription** (after payment)
6. **Error state** (if declined card)

---

## ✅ Current Status

**What Works Right Now** (without any setup):
- ✅ Dev server running on http://localhost:3001
- ✅ Frontend code deployed
- ✅ Modal UI complete and functional
- ✅ All UI interactions work
- ✅ Billing toggle and price calculations

**What Needs Setup**:
- ⏳ Stripe API keys in Azure (see PHASE_6A4_STRIPE_SETUP_GUIDE.md)
- ⏳ Actual payment testing with test cards

---

**Ready to test!** Start with Test 1 (UI Only) to verify everything looks good, then configure Stripe keys for Test 2. 🎉
