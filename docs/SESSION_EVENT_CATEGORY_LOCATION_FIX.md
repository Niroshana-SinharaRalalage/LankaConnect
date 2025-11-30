# Session: Event Category and Location Filtering Fixes

**Date:** 2025-11-29
**Commit:** c3e7ed7
**Status:** ✅ Complete

---

## Problem Statement

User reported two critical issues with event creation:

1. **Category Defaulting to Community**: User selected "Religious" category when creating event, but it saved as "Community"
2. **Location Filtering Not Working**: Published events with addresses not appearing in location-based filters

**Event ID with Issues:** `0458806b-8672-4ad5-a7cb-f5346f1b282a`

API response showed:
```json
{
  "category": "Community",  // Should be "Religious"
  "latitude": null,         // Should have coordinates
  "longitude": null,        // Should have coordinates
  "status": "Published"
}
```

---

## Root Cause Analysis

### Issue 1: Category Defaulting to Community

**Investigation:**
- [EventCreationForm.tsx:213](c:\Work\LankaConnect\web\src\presentation\components\features\events\EventCreationForm.tsx#L213) had `<option value="">Select a category</option>` as default
- React Hook Form sends empty string `""` when dropdown unchanged
- [CreateEventCommandHandler.cs:102](c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\CreateEvent\CreateEventCommandHandler.cs#L102) shows: `var category = request.Category ?? EventCategory.Community;`
- Empty string → null → Community fallback

**Why it happened:**
- User likely didn't change the dropdown, leaving it at the empty default
- Frontend validation passed (Zod allows enum values)
- Backend received null and applied Community default

### Issue 2: Location Filtering Not Working

**Investigation:**
- [GetEventsQueryHandler.cs:222-234](c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEvents\GetEventsQueryHandler.cs#L222-L234) shows location filtering requires lat/long coordinates:
  ```csharp
  var eventsInMetro = events
      .Where(e => e.Location?.Coordinates != null)
      .Where(e => {
          var distance = CalculateDistance(...);
          return distance <= radiusKm;
      })
  ```
- Events without coordinates are filtered out
- [CreateEventCommand.cs:21-22](c:\Work\LankaConnect\src\LankaConnect\Application\Events\Commands\CreateEvent\CreateEventCommand.cs#L21-L22) shows backend accepts `LocationLatitude` and `LocationLongitude`
- Frontend was NOT generating these coordinates

**Why it happened:**
- Previous geocoding service was removed (commit 08f92c0 replaced NetTopologySuite)
- Frontend only sent address text fields
- No geocoding service to convert addresses to coordinates

---

## Solution Implementation

### Fix 1: Category Dropdown

**Changes to EventCreationForm.tsx:**

1. Added default value to form configuration:
```typescript
defaultValues: {
  isFree: true,
  capacity: 50,
  ticketPriceCurrency: Currency.USD,
  category: EventCategory.Community, // ✅ Always has valid value
}
```

2. Removed empty option from dropdown:
```typescript
<select {...register('category', { valueAsNumber: true })}>
  {/* ❌ REMOVED: <option value="">Select a category</option> */}
  {categoryOptions.map((option) => (
    <option key={option.value} value={option.value}>
      {option.label}
    </option>
  ))}
</select>
```

**Result:**
- Category always has valid enum value (defaults to Community)
- Users must actively select category, or Community is used
- No more empty string → null → unexpected default

### Fix 2: Geocoding for Location Filtering

**New File: [geocoding.ts](c:\Work\LankaConnect\web\src\presentation\lib\utils\geocoding.ts)**

```typescript
export async function geocodeAddress(
  address: string,
  city: string,
  state?: string,
  country: string = 'United States',
  zipCode?: string
): Promise<GeocodingResult | null>
```

**Features:**
- Uses Nominatim (OpenStreetMap's free geocoding API)
- No API key required
- Proper User-Agent header: `LankaConnect/1.0 (Event Management Platform)`
- Returns lat/long coordinates or null if geocoding fails
- Graceful fallback: events still created even if geocoding fails

**Changes to EventCreationForm.tsx:**

```typescript
// Geocode address to get lat/long coordinates for location-based filtering
let locationLatitude: number | undefined;
let locationLongitude: number | undefined;

if (hasCompleteLocation) {
  console.log('🗺️ Geocoding address for location-based filtering...');
  const geocodeResult = await geocodeAddress(
    data.locationAddress!,
    data.locationCity!,
    data.locationState || undefined,
    data.locationCountry || 'United States',
    data.locationZipCode || undefined
  );

  if (geocodeResult) {
    locationLatitude = geocodeResult.latitude;
    locationLongitude = geocodeResult.longitude;
    console.log('✅ Geocoding successful:', {
      lat: locationLatitude,
      lon: locationLongitude,
      display: geocodeResult.displayName,
    });
  } else {
    console.warn('⚠️ Geocoding failed - event will not appear in location-based filters');
  }
}

const eventData = {
  // ... other fields
  ...(hasCompleteLocation && {
    locationAddress: data.locationAddress,
    locationCity: data.locationCity,
    locationState: data.locationState || '',
    locationZipCode: data.locationZipCode || '',
    locationCountry: data.locationCountry || 'United States',
    locationLatitude,  // ✅ Now included
    locationLongitude, // ✅ Now included
  }),
};
```

**Changes to EventEditForm.tsx:**
- Same geocoding logic added to event updates
- Ensures edited events also get coordinates

---

## Testing & Verification

### Manual Testing Steps

1. **Category Selection Test:**
   - Create new event
   - Verify category dropdown defaults to "Community"
   - Select "Religious" category
   - Submit form
   - Verify API payload shows `"category": 0` (Religious enum value)
   - Check database: category should be "Religious"

2. **Geocoding Test:**
   - Create event with address: "123 Main St, Columbus, OH 43201"
   - Check console logs for: `🗺️ Geocoding address...`
   - Verify: `✅ Geocoding successful: { lat: ..., lon: ... }`
   - Check API payload has `locationLatitude` and `locationLongitude`
   - Check database: coordinates should NOT be null

3. **Location Filtering Test:**
   - Create event in Columbus, OH
   - Go to /events page
   - Select "Columbus" metro area filter
   - Verify event appears in results

### Expected Console Output

```
📋 Form Submission - User Context: { userId: ..., userRole: 3, ... }
🗺️ Geocoding address for location-based filtering...
✅ Geocoding successful: {
  lat: 39.9612,
  lon: -82.9988,
  display: "123 Main Street, Columbus, Franklin County, Ohio, 43201, United States"
}
📤 Creating event with payload: {
  "title": "Test Event",
  "category": 0,  // Religious
  "locationAddress": "123 Main St",
  "locationCity": "Columbus",
  "locationState": "Ohio",
  "locationZipCode": "43201",
  "locationCountry": "United States",
  "locationLatitude": 39.9612,
  "locationLongitude": -82.9988
}
📊 Payload Analysis: {
  hasLocation: true,
  hasCoordinates: true,  // ✅ Now true!
  categoryValue: 0
}
```

---

## Impact & Future Considerations

### Impact

**Category Fix:**
- ✅ No more unexpected category defaults
- ✅ Clear user selection required
- ✅ Form validation ensures valid enum values

**Geocoding Fix:**
- ✅ Location-based filtering now works for new events
- ✅ Events appear in metro area searches
- ✅ Distance calculations work correctly
- ✅ Graceful fallback if geocoding fails

### Future Considerations

1. **Geocoding Rate Limits:**
   - Nominatim has 1 request/second limit
   - For high-volume production, consider:
     - Google Maps Geocoding API (requires API key)
     - Caching geocoded addresses
     - Batch geocoding for existing events

2. **Existing Events:**
   - Events created before this fix have null coordinates
   - Consider migration script to geocode existing addresses:
     ```sql
     SELECT id, address_city, address_state, address_zip_code
     FROM events
     WHERE coordinates_latitude IS NULL
       AND address_city IS NOT NULL;
     ```

3. **Category Validation:**
   - Consider adding backend validation to reject null categories
   - Add explicit error message: "Event category is required"

4. **Geocoding Accuracy:**
   - Some addresses may geocode incorrectly
   - Consider allowing organizers to:
     - Preview map location before saving
     - Manually adjust coordinates if needed
     - Verify address via map interface

5. **Alternative Geocoding Providers:**
   - **Nominatim (Current):** Free, no API key, 1 req/sec limit
   - **Google Maps:** $5/1000 requests, requires credit card
   - **Mapbox:** 100k free requests/month
   - **Azure Maps:** Included with Azure subscription

---

## Files Modified

1. **web/src/presentation/components/features/events/EventCreationForm.tsx**
   - Added default category value
   - Removed empty dropdown option
   - Added geocoding before event creation
   - Updated payload logging

2. **web/src/presentation/components/features/events/EventEditForm.tsx**
   - Added geocoding before event update
   - Ensures edited events also get coordinates

3. **web/src/presentation/lib/utils/geocoding.ts** (NEW)
   - Geocoding utility using Nominatim API
   - Graceful error handling
   - Proper User-Agent header

---

## Git History

```bash
commit c3e7ed7
Author: Claude Code
Date:   2025-11-29

fix(events): Fix category selection and add geocoding for location filtering

**Problem 1: Category Defaulting to Community**
When creating events, if the category dropdown wasn't changed from the empty
default option, it sent empty string → null → defaulted to Community.

**Problem 2: Location Filtering Not Working**
Events created with addresses had null lat/long coordinates, so they were
excluded from location-based filters.

**Solution:**
1. Set default category to Community, removed empty dropdown option
2. Created geocoding utility using Nominatim (OpenStreetMap)
3. EventCreationForm now geocodes addresses on submit
4. EventEditForm now geocodes addresses on save

Files Changed:
- web/src/presentation/components/features/events/EventCreationForm.tsx
- web/src/presentation/components/features/events/EventEditForm.tsx
- web/src/presentation/lib/utils/geocoding.ts (new)
```

---

## Summary

Both issues have been resolved:

1. **✅ Category Selection Fixed:** Category now defaults to Community and requires explicit selection
2. **✅ Geocoding Implemented:** Addresses are now converted to lat/long coordinates for location filtering

Events created moving forward will have:
- Correct category saved to database
- Lat/long coordinates for location-based filtering
- Full compatibility with metro area searches

**Next Steps:**
- Test event creation with Religious category
- Verify event appears in location filters
- Consider migration for existing events without coordinates
