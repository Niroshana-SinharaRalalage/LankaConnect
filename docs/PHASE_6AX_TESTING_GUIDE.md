# Phase 6A.X Revenue Breakdown Testing Guide

## ⚠️ CRITICAL: When Revenue Breakdown Is Applied

**Revenue breakdown is ONLY calculated and displayed for:**
- ✅ **NEW paid event registrations** created AFTER Phase 6A.X deployment
- ✅ Events where the Event entity has a **location with state** set
- ✅ Registrations where `SetRevenueBreakdown()` was successfully called

**Revenue breakdown is NOT available for:**
- ❌ Events created BEFORE Phase 6A.X deployment
- ❌ Events without a location or without a state set
- ❌ Free events (no payment, so no breakdown needed)
- ❌ Legacy registrations created before Phase 6A.X

---

## Understanding hasRevenueBreakdown Flag

The `hasRevenueBreakdown` flag in the API response indicates whether ANY registration has breakdown data:

```csharp
// Backend: GetEventAttendeesQueryHandler.cs (lines 137-141)
bool hasRevenueBreakdown = attendeeDtos.Any(a =>
    a.SalesTaxAmount.HasValue ||
    a.StripeFeeAmount.HasValue ||
    a.PlatformCommissionAmount.HasValue ||
    a.OrganizerPayoutAmount.HasValue);
```

**When true:** Frontend displays detailed breakdown with sales tax, Stripe fees, platform commission
**When false:** Frontend displays legacy "After 5% platform fee" message

---

## Step-by-Step Testing Instructions

### Prerequisites
1. ✅ Backend deployed to Azure staging (latest build with Phase 6A.X changes)
2. ✅ Frontend deployed to Azure staging (latest build with breakdown UI)
3. ✅ Database has `state_tax_rates` table with all 50 US states
4. ✅ User account with event organizer permissions

### Test Scenario 1: Create NEW Paid Event with Revenue Breakdown

**Step 1: Create Event with Location**
1. Login to staging environment
2. Navigate to "Create Event" page
3. Fill in event details:
   - Title: "Test Event - Phase 6A.X Revenue Breakdown"
   - Date: Future date
   - **Location:** Set full address with state (e.g., "123 Main St, San Francisco, CA 94102")
   - **Event Type:** In-Person (to enable location)
4. **Pricing:** Set ticket price (e.g., $100)
   - Note: This should trigger revenue breakdown calculation
5. Submit and create event
6. **Verify:** Revenue breakdown preview should appear during creation

**Step 2: Register for Event (Create First Registration)**
1. Navigate to the event details page
2. Click "Register" or "Buy Tickets"
3. Fill in attendee information
4. **Complete payment** via Stripe test mode
5. Wait for registration confirmation

**Step 3: Verify Attendee Management UI**
1. Navigate to "Manage Event" → "Attendees" tab
2. **Expected (NEW):**
   ```
   Your Payout: $88.58
   After tax, Stripe fees & platform commission

   Gross Revenue: $100.00
   Sales Tax (7.25%): -$6.54
   Stripe Fees (2.9% + $0.30): -$3.01
   Platform Commission (2%): -$1.87
   Your Payout: $88.58
   ```
3. **NOT Expected (OLD):**
   ```
   Your Payout: $95.00
   After 5% platform fee
   ```

**Step 4: Verify CSV Export**
1. Click "Export CSV" button
2. Open CSV file in Excel/Numbers
3. **Verify columns exist:**
   - `SalesTax` column
   - `TaxRate` column
   - `StripeFee` column
   - `PlatformCommission` column
   - `NetAmount` column
4. **Verify values:** Should show actual breakdown amounts per registration

**Step 5: Verify Excel Export**
1. Click "Export Excel" button
2. Open XLSX file in Excel
3. **Verify "Attendees" sheet** has breakdown columns with proper formatting
4. **Verify totals row** shows breakdown summary

**Step 6: Verify API Response**
```bash
# Get authentication token
TOKEN=$(curl -X POST "https://lankaconnect-staging.azurewebsites.net/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}' | jq -r '.token')

# Get event attendees with breakdown
curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/attendees" \
  -H "Authorization: Bearer $TOKEN" | jq '.'
```

**Expected API fields:**
```json
{
  "hasRevenueBreakdown": true,
  "totalSalesTax": 6.54,
  "totalStripeFees": 3.01,
  "totalPlatformCommission": 1.87,
  "totalOrganizerPayout": 88.58,
  "averageTaxRate": 0.0725,
  "attendees": [
    {
      "salesTaxAmount": 6.54,
      "stripeFeeAmount": 3.01,
      "platformCommissionAmount": 1.87,
      "organizerPayoutAmount": 88.58,
      "salesTaxRate": 0.0725,
      "totalAmount": 100.00,
      "netAmount": 88.58
    }
  ]
}
```

---

### Test Scenario 2: OLD Event (Created Before Phase 6A.X)

**Expected Behavior:**
- `hasRevenueBreakdown`: `false`
- UI displays legacy "After 5% platform fee" message
- CSV/Excel exports show only `NetAmount` column (no breakdown columns)
- This is CORRECT and expected for backward compatibility

**Reason:** Old events don't have breakdown data because:
1. Event was created before `SetRevenueBreakdown()` was implemented
2. Existing registrations don't have breakdown fields populated
3. System falls back to legacy 5% calculation

**How to Upgrade (Optional):**
If an organizer wants breakdown for an old event, they must:
1. Edit the event and ensure location/state is set
2. New registrations will then get breakdown
3. Old registrations will continue showing legacy calculation

---

## Troubleshooting

### Issue: UI Still Shows "After 5% platform fee"

**Diagnosis Steps:**

1. **Check Event Location:**
   ```bash
   # Verify event has location with state
   curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}" \
     -H "Authorization: Bearer $TOKEN" | jq '.location.address.state'
   ```
   - If `null`, event has no state → breakdown won't be calculated

2. **Check API Response:**
   ```bash
   # Get attendees response
   curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/attendees" \
     -H "Authorization: Bearer $TOKEN" | jq '.hasRevenueBreakdown'
   ```
   - If `false`, no registrations have breakdown data

3. **Check Registration Breakdown Fields:**
   ```bash
   # Check individual registration
   curl -X GET "https://lankaconnect-staging.azurewebsites.net/api/Events/{eventId}/attendees" \
     -H "Authorization: Bearer $TOKEN" | jq '.attendees[0] | {salesTaxAmount, stripeFeeAmount, platformCommissionAmount}'
   ```
   - If all `null`, breakdown was not calculated for this registration

4. **Check Azure Container Logs:**
   ```bash
   # View logs from Azure
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group lankaconnect-staging \
     --follow
   ```
   - Look for: "Revenue breakdown calculated successfully" log messages
   - Look for: Any errors during breakdown calculation

**Common Causes:**

1. **Event has no location/state:**
   - Solution: Edit event, add full address with state

2. **Registration created before deployment:**
   - Solution: Create a NEW registration after deployment

3. **IRevenueCalculatorService failed:**
   - Check logs for exceptions
   - Verify `state_tax_rates` table has data

4. **Frontend cache:**
   - Clear browser cache
   - Hard refresh (Ctrl+Shift+R)

---

## Success Criteria

✅ **Phase 6A.X is working correctly when:**

1. New paid event with location/state shows breakdown in creation UI
2. New registrations for that event have breakdown data in API response
3. `hasRevenueBreakdown` flag is `true` in attendees API
4. UI displays detailed breakdown with sales tax, Stripe fees, platform commission
5. CSV export includes breakdown columns
6. Excel export includes breakdown columns with proper formatting
7. Old events continue showing legacy "After 5% platform fee" (backward compatibility)

❌ **Phase 6A.X has issues if:**

1. New event with location shows "After 5% platform fee" instead of breakdown
2. API returns `hasRevenueBreakdown: false` for new registrations
3. Breakdown fields are `null` in API response
4. CSV/Excel exports don't show breakdown columns for new events

---

## Notes

- **Tax-Inclusive Pricing:** Sales tax is extracted from ticket price, not added on top
- **Formula:** `preTaxAmount = grossAmount / (1 + taxRate)`
- **Stripe Fee:** 2.9% of taxable amount + $0.30 per transaction
- **Platform Commission:** 2% of taxable amount (reduced from 5% combined)
- **Zero Tax:** States with no sales tax (e.g., Alaska) will show 0% tax rate

---

## Contact

If breakdown is not working for new events with location/state:
1. Check Azure container logs for errors
2. Verify database migrations applied successfully
3. Create GitHub issue with reproduction steps
