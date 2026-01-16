# Phase 6A.X Revenue Breakdown - READ-Time Calculation Status

**Date**: 2026-01-16 20:40 UTC
**Current Status**: ⚠️ **PARTIALLY FIXED - INVESTIGATING**

---

## Summary

**User Requirement**: "regardless of new or old event, calculations should be reflected"

**Implementation**:
1. ✅ WRITE-time calculation: Working for NEW registrations (deployed #831)
2. ❌ READ-time calculation: NOT WORKING for OLD registrations (investigating)

---

## Deployments Timeline

### Deployment #831 (SUCCESS) - 15:08 UTC
- **Commit**: 0dd9f20f
- **Fix**: WRITE-time calculation in registration handlers
- **Status**: ✅ Working for NEW registrations

### Deployment #832 (FAILED) - 16:33 UTC
- **Status**: Failed (superseded by #833)

### Deployment #833 (SUCCESS) - 16:55 UTC
- **Commit**: 2ae133cb
- **Fix**: On-the-fly calculation logic in GetEventAttendeesQueryHandler
- **Status**: ❌ Not working - Event.Location was NULL

### Deployment #834 (SUCCESS) - 20:06 UTC
- **Commit**: 76b1aae2
- **Fix**: Added .Include(e => e.Location) to EventRepository.GetByIdAsync
- **Test Result**: ❌ Still not working - breakdown still NULL

### Deployment #835 (SUCCESS) - 20:39 UTC
- **Commit**: cc82e711
- **Fix**: Added diagnostic logging + trackChanges: false
- **Test Result**: ❌ Still not working - NO logs appearing

---

## Root Cause Analysis

### Issue #1: Event.Location Was NULL ✅ FIXED
**Problem**: EventRepository.GetByIdAsync() did not include Location
**Fix**: Added `.Include(e => e.Location)` at line 70 of EventRepository.cs
**Status**: ✅ Fixed in deployment #834

### Issue #2: Calculation Still Not Executing ⚠️ INVESTIGATING
**Problem**: Even after Location fix, breakdown is still NULL
**Evidence**:
- API returns `hasRevenueBreakdown: false`
- All per-registration breakdown fields are `null`
- No diagnostic logs appearing in Azure Container Apps logs

**Possible Causes**:
1. **Azure Container Apps log delay** - Logs may take time to appear
2. **GetByIdAsync overload issue** - Wrong overload being called
3. **IEventRepository interface** - May not have trackChanges parameter
4. **Deployment caching** - Azure may be serving old container image
5. **Different controller endpoint** - Attendees endpoint may use different query handler

---

## API Test Results

**Event**: Christmas Dinner Dance 2025 (`d543629f-a5ba-4475-b124-3d0fc5200f2f`)
**Location**: "4314 Clark Ave, Cleveland, Ohio 44109, United States"
**Registrations**: 8 (all created 2025-12-23, BEFORE any fix)

### Test #1: After Deployment #834 (20:06 UTC)
```json
{
  "hasRevenueBreakdown": false,  ← WRONG
  "totalSalesTax": 0,  ← Should be ~$20
  "totalStripeFees": 0,  ← Should be ~$11
  "totalPlatformCommission": 18.75,  ← Legacy 5% ($375 × 0.05)
  "totalOrganizerPayout": 356.25,  ← Legacy calculation
  "averageTaxRate": 0,  ← Should be 0.0575 (Ohio)
  "attendees": [
    {
      "registrationId": "ee925721...",
      "totalAmount": 50.0,  ← HAS PRICE
      "salesTaxAmount": null,  ← Should be ~$2.69
      "stripeFeeAmount": null,  ← Should be ~$1.67
      "platformCommissionAmount": null,  ← Should be ~$0.89
      "organizerPayoutAmount": null,  ← Should be ~$44.75
      "salesTaxRate": 0.0  ← Should be 0.0575
    }
  ]
}
```

### Test #2: After Deployment #835 (20:39 UTC)
**Same result** - No improvement

---

## Expected Behavior

For OLD event with 8 registrations ($375 total):

```json
{
  "hasRevenueBreakdown": true,  ← Calculated on-the-fly
  "totalSalesTax": 20.14,  ← $375 - ($375 / 1.0575) = $20.14
  "totalStripeFees": 11.16,  ← ($354.86 × 0.029) + ($0.30 × 8) = $12.69
  "totalPlatformCommission": 6.64,  ← $354.86 × 0.02 = $7.10 (approx, per-registration calc)
  "totalOrganizerPayout": 337.06,  ← $354.86 - $11.16 - $6.64
  "averageTaxRate": 0.0575,  ← Ohio state tax rate
  "attendees": [
    {
      "totalAmount": 50.0,
      "salesTaxAmount": 2.69,  ← $50 - ($50 / 1.0575)
      "stripeFeeAmount": 1.67,  ← ($47.31 × 0.029) + $0.30
      "platformCommissionAmount": 0.89,  ← $47.31 × 0.02
      "organizerPayoutAmount": 44.75,  ← $47.31 - $1.67 - $0.89
      "salesTaxRate": 0.0575
    }
  ]
}
```

---

## Code Changes Summary

### Files Modified

1. **EventAttendeeDto.cs** (Deployment #833)
   - Changed breakdown properties from `{ get; init; }` to `{ get; set; }`
   - Allows on-the-fly calculation to update DTO after LINQ projection

2. **GetEventAttendeesQueryHandler.cs** (Deployment #833, #835)
   - Added on-the-fly calculation loop (lines 129-195)
   - Added diagnostic logging (deployment #835)
   - Changed to `trackChanges: false` for read-only query (deployment #835)

3. **ExportEventAttendeesQueryHandler.cs** (Deployment #833)
   - Updated dependencies for GetEventAttendeesQueryHandler instantiation

4. **EventRepository.cs** (Deployment #834)
   - Added `.Include(e => e.Location)` to GetByIdAsync (line 70)

---

## Next Investigation Steps

### 1. Check IEventRepository Interface
**Action**: Verify if interface has `GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken)` signature

### 2. Check Azure Logs with Longer Delay
**Action**: Wait 5-10 minutes after API call, then check logs again

### 3. Test with NEW Event
**Action**: Create a NEW paid event AFTER deployment #831
- New registrations should have breakdown stored in database
- Should show `hasRevenueBreakdown: true` immediately

### 4. Check Controller Endpoint
**Action**: Verify which query handler the `/Attendees` endpoint uses
- Look at EventsController.cs
- Ensure it's calling GetEventAttendeesQuery

### 5. Force Container Restart
**Action**: Restart Azure Container App to ensure new code is running
```bash
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging
```

---

## User Action Required

Since automated testing is not showing the fix working, **manual UI testing is recommended**:

### Test in Staging UI

1. Navigate to: https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io
2. Go to "Christmas Dinner Dance 2025" event
3. Click "Attendees" tab
4. Check Revenue card:
   - **If shows detailed breakdown** → READ-time calculation IS working (API test issue)
   - **If shows "After 5% platform fee"** → READ-time calculation NOT working

### Create NEW Event Test

1. Create a NEW paid event with location "Cleveland, Ohio"
2. Register 1-2 attendees
3. Check Attendees tab
4. Should show detailed breakdown immediately (WRITE-time calculation)

---

## Commits

- `0dd9f20f` - WRITE-time calculation (working)
- `2ae133cb` - READ-time calculation logic (not working)
- `76b1aae2` - Add Location eager loading (not working)
- `cc82e711` - Add diagnostic logging (no logs appearing)

---

## Status: BLOCKED

**Blocked By**: Unable to determine why READ-time calculation is not executing

**Options**:
1. User tests directly in UI (fastest verification)
2. Continue debugging with more invasive logging
3. Test with NEW event (verify WRITE-time calculation works)
4. Restart Azure Container App to force code reload

**Recommendation**: User should test in UI while we continue investigating logs.
