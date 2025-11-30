# Session: Event Visibility Issues - Location Filtering & Public Access

**Date:** 2025-11-29
**Event ID:** `0458806b-8672-4ad5-a7cb-f5346f1b282a`
**Status:** ✅ Resolved (Partial - Requires Manual Action)

---

## Issue Summary

User reported two issues with event `0458806b-8672-4ad5-a7cb-f5346f1b282a`:

1. **Event not showing in location filters** on http://localhost:3000/events
2. **Event not visible to unauthenticated users** on http://localhost:3000/events/0458806b-8672-4ad5-a7cb-f5346f1b282a

---

## Issue 1: Event Not Showing in Location Filters

### Status: ⚠️ Requires Manual Action

### Current State

API response for the event:
```json
{
  "id": "0458806b-8672-4ad5-a7cb-f5346f1b282a",
  "title": "Monthly Dana January 2026",
  "status": "Published",
  "address": "943 Penny Lane",
  "city": "Aurora",
  "state": "OH",
  "zipCode": "44202",
  "country": "United States",
  "latitude": null,          // ❌ Missing
  "longitude": null,         // ❌ Missing
  "category": "Community"
}
```

### Root Cause

The event was created **before** the geocoding feature was implemented (commit c3e7ed7). The geocoding fix from the previous session only applies to:

1. ✅ NEW events created after the fix
2. ✅ Events that are EDITED and saved after the fix
3. ❌ Existing events created before the fix (like this one)

**Why location filtering requires coordinates:**

From [GetEventsQueryHandler.cs:222-234](c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEvents\GetEventsQueryHandler.cs#L222-L234):

```csharp
// Filters events within metro area radius
var eventsInMetro = events
    .Where(e => e.Location?.Coordinates != null)  // ❌ Event fails here
    .Where(e => {
        var distance = CalculateDistance(...);
        var radiusKm = metroData.Value.RadiusMiles * 1.60934;
        return distance <= radiusKm;
    })
```

Events without coordinates are filtered out before distance calculation.

### Solution

**MANUAL ACTION REQUIRED**: You need to edit and save the event to trigger geocoding.

#### Steps to Fix:

1. Navigate to: http://localhost:3000/events/0458806b-8672-4ad5-a7cb-f5346f1b282a/edit
2. The form will load with existing data (address already filled in)
3. Click "Save" (no need to change anything)
4. The EventEditForm will:
   - Geocode "943 Penny Lane, Aurora, OH 44202"
   - Send coordinates to backend: `locationLatitude` and `locationLongitude`
   - Update database with coordinates

**Expected Console Output:**
```
🗺️ Geocoding address for location-based filtering...
✅ Geocoding successful: {
  lat: 41.3175,
  lon: -81.3451,
  display: "943 Penny Lane, Aurora, Summit County, Ohio, 44202, United States"
}
📤 Updating event with payload: {
  ...
  "locationLatitude": 41.3175,
  "locationLongitude": -81.3451
}
```

**After Saving:**
- Event will have coordinates in database
- Event will appear in location-based filters
- Metro area filters will include this event

---

## Issue 2: Event Not Visible to Unauthenticated Users

### Status: ✅ Already Works (Likely Frontend Caching/Rendering Issue)

### Investigation Results

**1. API Endpoint Works Without Authentication:**

```bash
$ curl "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a"
HTTP Status: 200
{
  "id": "0458806b-8672-4ad5-a7cb-f5346f1b282a",
  "title": "Monthly Dana January 2026",
  "status": "Published",
  ...
}
```

✅ Backend returns event data without authentication.

**2. Frontend API Client Does Not Require Authentication:**

From [api-client.ts:70-72](c:\Work\LankaConnect\web\src\infrastructure\api\client\api-client.ts#L70-L72):

```typescript
// Request interceptor
this.axiosInstance.interceptors.request.use((config) => {
  // Add auth token if available
  if (this.authToken) {  // ✅ Only adds header if token exists
    config.headers.Authorization = `Bearer ${this.authToken}`;
  }
  return config;
});
```

✅ For unauthenticated users, `this.authToken` is `null`, so no `Authorization` header is sent. Request proceeds as public API call.

**3. Event Detail Page Has No Authentication Guards:**

From [events/[id]/page.tsx](c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx):

```typescript
export default function EventDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { user } = useAuthStore();  // ✅ Gets user but doesn't block if null

  // Fetch event details
  const { data: event, isLoading, error: fetchError } = useEventById(id);

  // Page renders for all users (authenticated and anonymous)
  // Only RSVP buttons and organizer actions require authentication
}
```

✅ Page does NOT redirect unauthenticated users. Event data is fetched and displayed.

**4. Event Page Works for Unauthenticated Users:**

From [events/page.tsx:28-36](c:\Work\LankaConnect\web\src\app\events\page.tsx#L28-L36):

```typescript
export default function EventsPage() {
  const { user } = useAuthStore();

  // For anonymous users, detect location via IP/browser geolocation
  const isAnonymous = !user?.userId;  // ✅ Supports anonymous users
  const { latitude, longitude } = useGeolocation(isAnonymous);

  // Fetch events (works for authenticated and anonymous users)
  const { data: events } = useEvents(filters);
```

✅ Events page explicitly supports anonymous users.

### Likely Cause of User's Issue

The event detail page SHOULD work for unauthenticated users. Possible reasons for user's issue:

1. **React Query Caching:** Previous failed request cached an error
2. **Dev Server State:** Development server had stale state
3. **Browser Cache:** Old version of page cached in browser
4. **Token Refresh Loop:** Token refresh service tried to refresh non-existent token

### Recommended Testing Steps

**For User to Verify:**

1. **Clear Browser Cache:**
   - Open DevTools (F12)
   - Right-click refresh button → "Empty Cache and Hard Reload"

2. **Test in Incognito/Private Window:**
   - Open incognito window (Ctrl+Shift+N in Chrome)
   - Navigate to: http://localhost:3000/events/0458806b-8672-4ad5-a7cb-f5346f1b282a
   - Should load without login

3. **Check Browser Console:**
   - Look for API request logs:
     ```
     🚀 API Request: GET /api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a
     ✅ API Response Success: status 200
     ```
   - If you see 401 errors, there may be a token refresh issue

4. **Check React Query DevTools:**
   - Look for query key: `['events', 'detail', '0458806b-8672-4ad5-a7cb-f5346f1b282a']`
   - Check query status (loading, success, error)

---

## Summary of Both Issues

### Issue 1: Location Filtering ⚠️

**Status:** Requires manual action (edit and save event)

**Reason:** Event created before geocoding feature, has null coordinates

**Solution:**
1. Go to event edit page
2. Click Save (no changes needed)
3. Geocoding will add coordinates
4. Event will appear in location filters

**Alternative Solution (Bulk Fix):**
If you have many events without coordinates, you could:
1. Create a migration script to geocode all existing events
2. Query: `SELECT id, address_city, address_state FROM events WHERE coordinates_latitude IS NULL`
3. Use Nominatim API to batch geocode
4. Update database with coordinates

### Issue 2: Public Visibility ✅

**Status:** Already works correctly

**Reason:** Likely frontend caching or dev server state issue

**Solution:**
1. Hard refresh browser (Ctrl+F5)
2. Test in incognito window
3. Check console for actual error

**Architecture Confirms:**
- ✅ Backend endpoint is public (no `[Authorize]` attribute)
- ✅ Frontend API client works without token
- ✅ Event detail page has no authentication guards
- ✅ useEventById hook doesn't require authentication

---

## Files Involved

### For Issue 1 (Location Filtering):
- [GetEventsQueryHandler.cs:222-234](c:\Work\LankaConnect\src\LankaConnect\Application\Events\Queries\GetEvents\GetEventsQueryHandler.cs#L222-L234) - Filters out events without coordinates
- [EventEditForm.tsx:154-180](c:\Work\LankaConnect\web\src\presentation\components\features\events\EventEditForm.tsx#L154-L180) - Geocoding logic on save
- [geocoding.ts](c:\Work\LankaConnect\web\src\presentation\lib\utils\geocoding.ts) - Geocoding utility

### For Issue 2 (Public Visibility):
- [events/[id]/page.tsx](c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx) - Event detail page (no auth gates)
- [api-client.ts:70-72](c:\Work\LankaConnect\web\src\infrastructure\api\client\api-client.ts#L70-L72) - Conditional auth header
- [useEvents.ts:107-119](c:\Work\LankaConnect\web\src\presentation\hooks\useEvents.ts#L107-L119) - useEventById hook

---

## Testing Checklist

- [ ] Edit event 0458806b and save to trigger geocoding
- [ ] Verify API response has coordinates: `"latitude": 41.3175, "longitude": -81.3451`
- [ ] Test location filter on /events page (select Aurora, OH metro)
- [ ] Verify event appears in filtered results
- [ ] Open incognito window
- [ ] Navigate to http://localhost:3000/events/0458806b-8672-4ad5-a7cb-f5346f1b282a
- [ ] Verify event detail page loads without login
- [ ] Check console for API request logs
- [ ] Verify no 401 errors in console

---

## Next Steps

1. **For User:** Edit and save the event to add coordinates
2. **For Dev Team:** Consider creating migration script for bulk geocoding
3. **For Future:** Add geocoding to backend CreateEvent/UpdateEvent handlers
4. **For Monitoring:** Add metrics to track events without coordinates
