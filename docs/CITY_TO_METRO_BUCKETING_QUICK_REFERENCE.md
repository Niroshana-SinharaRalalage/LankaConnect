# City-to-Metro Bucketing - Quick Reference Table

**Date**: 2026-01-18
**Full Analysis**: `CITY_TO_METRO_BUCKETING_COMPREHENSIVE_ANALYSIS.md`

---

## Bucketing Status by Location

| # | Location | Has Bucketing? | Mechanism | Status | File Reference |
|---|----------|----------------|-----------|--------|----------------|
| 1 | /events page | ✅ YES | Geo-spatial (distance calc) | ✅ WORKS | `GetEventsQueryHandler.cs:232-285` |
| 2 | Dashboard events | ✅ YES | Same as /events | ✅ WORKS | `GetEventsQueryHandler.cs` |
| 3 | /newsletters page | ❌ NO | Junction table only | ❌ BROKEN | `NewsletterRepository.cs:489-495` |
| 4 | Event cancellation emails | ✅ YES | Geo-spatial (Phase 6A.70) | ✅ WORKS | `EventNotificationRecipientService.cs:318-442` |
| 5 | Newsletter emails (event-linked) | ❌ NO | State-level fallback | ❌ BROKEN | `NewsletterRecipientService.cs:283` |

---

## How Each Location Handles Aurora → Cleveland Matching

### Test Case: Event in Aurora, OH

- **Event Location**: Aurora, OH (41.3173°, -81.3460°)
- **Cleveland Metro**: Center (41.4993°, -81.6944°), Radius 50mi
- **Distance**: ~20 miles (within radius)
- **Expected**: Should match Cleveland metro

| Location | What Happens | Correct? |
|----------|-------------|----------|
| 1. /events page | Calculates distance (20mi) ≤ radius (50mi) → Shows event | ✅ YES |
| 2. Dashboard events | Same logic as /events → Shows event | ✅ YES |
| 3. /newsletters page | Checks newsletter_metro_areas table → No match (event city not bucketed) | ❌ NO |
| 4. Event cancel email | Calculates distance → Sends to Cleveland subscribers | ✅ YES |
| 5. Newsletter email | Extracts state "OH" → Sends to ALL Ohio subscribers (too broad) | ❌ NO |

---

## Bucketing Mechanisms Comparison

### Geo-Spatial Bucketing (CORRECT)

**Used By**: Locations 1, 2, 4

**Logic**:
1. Get metro area data (center coordinates + radius)
2. Calculate distance from event to metro center (Haversine formula)
3. If distance ≤ radius → Include

**Example**:
```
Event: Aurora (41.3173°, -81.3460°)
Metro: Cleveland (41.4993°, -81.6944°), Radius 50mi
Distance: Calculate((41.3173, -81.3460), (41.4993, -81.6944)) = 20mi
Result: 20mi ≤ 50mi → MATCH ✓
```

**Code Reference**:
```csharp
var distance = CalculateDistance(
    metroData.Value.Latitude,
    metroData.Value.Longitude,
    e.Location!.Coordinates!.Latitude,
    e.Location.Coordinates.Longitude);
var radiusKm = metroData.Value.RadiusMiles * 1.60934;
return distance <= radiusKm;  // ← Bucketing decision
```

### Junction Table (INCOMPLETE)

**Used By**: Location 3

**Logic**:
1. Check if newsletter explicitly assigned to metro areas
2. Query junction table for metro area IDs
3. No automatic bucketing for event-linked newsletters

**Example**:
```
Newsletter linked to Aurora event
Junction table: [No rows for this newsletter]
User filters by Cleveland metro
Result: Newsletter NOT shown (missing bucketing) ✗
```

**Code Reference**:
```csharp
query = query.Where(n =>
    n.TargetAllLocations ||
    _context.Set<Dictionary<string, object>>("newsletter_metro_areas")
        .Any(j => (Guid)j["newsletter_id"] == n.Id &&
                  metroAreaIds.Contains((Guid)j["metro_area_id"])));
```

### State-Level Fallback (TOO BROAD)

**Used By**: Location 5

**Logic**:
1. Extract event state ("OH")
2. Get ALL subscribers in that state
3. No metro-level filtering

**Example**:
```
Event: Aurora, OH
Extracts: "OH"
Query: SELECT * FROM newsletter_subscribers WHERE state = 'OH'
Result: Cincinnati, Columbus, Toledo, Dayton, Akron, Cleveland... (entire state) ✗
Expected: Only Cleveland/Akron metro subscribers ✓
```

**Code Reference**:
```csharp
var state = @event.Location.Address.State;
subscribers = await _subscriberRepository.GetConfirmedSubscribersByStateAsync(state, cancellationToken);
// Returns ALL state subscribers - no metro bucketing
```

---

## Data Flow Comparison

### /events Page (WORKS) ✅

```
User Action:         Select "Cleveland metro" dropdown
                              ↓
Frontend:            metroAreaIds: [<cleveland-guid>]
                              ↓
Backend:             Load metro data (center + radius)
                              ↓
For each event:      Calculate distance to metro center
                              ↓
Bucketing:           Distance ≤ radius? → Include
                              ↓
Result:              Events in Aurora, Lakewood, Parma shown
```

### /newsletters Page (BROKEN) ❌

```
User Action:         Select "Cleveland metro" dropdown
                              ↓
Frontend:            metroAreaIds: [<cleveland-guid>]
                              ↓
Backend:             Query newsletter_metro_areas junction table
                              ↓
Filtering:           Does newsletter have explicit metro assignment?
                              ↓
Event-linked:        NO bucketing - aurora event not mapped to Cleveland
                              ↓
Result:              Newsletter NOT shown (should be shown)
```

### Event Cancellation Email (WORKS) ✅

```
Event:               Aurora Summer Festival cancelled
                              ↓
Extract Location:    Aurora, OH (41.3173°, -81.3460°)
                              ↓
Get State Metros:    [Cleveland, Akron, Columbus, Toledo, ...]
                              ↓
Distance Check:      Cleveland: 20mi ✓, Akron: 15mi ✓, Columbus: 120mi ✗
                              ↓
Matching Metros:     [Cleveland, Akron]
                              ↓
Get Subscribers:     Cleveland subscribers + Akron subscribers
                              ↓
Result:              Send cancellation email to relevant metros only
```

### Newsletter Email (BROKEN) ❌

```
Newsletter:          Linked to Aurora event
                              ↓
Extract Event:       Aurora, OH
                              ↓
Extract State:       "OH"
                              ↓
Query Subscribers:   SELECT * WHERE state = 'OH'
                              ↓
Result:              ALL Ohio subscribers get email (Cincinnati, Columbus, Toledo...)
                              ↓
Expected:            Only Cleveland/Akron metro subscribers
```

---

## Original Issues Status (Phase 6A.74)

| Issue | Description | Status | Bucketing Related? |
|-------|-------------|--------|-------------------|
| #1 | Unknown status badges | ✅ FIXED | No (data integrity) |
| #2 | Publishing fails (400) | ✅ FIXED | No (validation) |
| #3 | Image embedding fails | ✅ FIXED | No (description length) |
| #4 | Update fails (400) | ✅ FIXED | No (validation) |
| #5 | Newsletter filtration broken | ⚠️ PARTIAL | **YES** (bucketing gap) |
| #6 | Landing page hardcoded data | ❓ UNKNOWN | Possibly |
| #7 | Grid layout | ✅ FIXED | No (UI) |
| #8 | Flat dropdown | ✅ FIXED | No (UI) |

### Issue #5 Breakdown

**What We Fixed**:
- ✅ Junction table query syntax error
- ✅ Newsletter filtering no longer crashes

**What We DIDN'T Fix**:
- ❌ Event-linked newsletters don't use geo-spatial bucketing
- ❌ Aurora event newsletter doesn't show when filtering by Cleveland metro

**Root Cause**:
- We treated it as a **query error**
- User was reporting a **bucketing inconsistency**

---

## Proposed Solution Summary

### EventMetroAreaMatcher Service

**Purpose**: Centralized city-to-metro bucketing for ALL 5 locations

**Interface**:
```csharp
public interface IEventMetroAreaMatcher
{
    Task<IReadOnlyList<Guid>> GetMatchingMetroAreasAsync(EventLocation location);
    Task<bool> MatchesAnyMetroAreaAsync(EventLocation location, IReadOnlyList<Guid> metroAreaIds);
}
```

**Implementation**: Reuse existing geo-spatial logic from Phase 6A.70

**Impact**:
- Location 1 & 2: No change (already works)
- Location 3: Add event bucketing
- Location 4: Refactor to use service (simplify)
- Location 5: Replace state-level with metro bucketing

**Effort**: 5.5 hours total

---

## Testing Checklist

### Test Case: Aurora Event → Cleveland Metro Filter

**Setup**:
1. Create event: "Aurora Summer Festival"
2. Location: Aurora, OH (41.3173°, -81.3460°)
3. Create newsletter linked to event
4. User selects "Cleveland metro" in dropdown

**Expected Results**:

| Location | Pass? | Notes |
|----------|-------|-------|
| ☑ /events page shows event | ✅ | Already works |
| ☑ Dashboard shows event | ✅ | Already works |
| ☐ /newsletters page shows newsletter | ❌ | **NEEDS FIX** |
| ☑ Cancel email → Cleveland subscribers | ✅ | Already works |
| ☐ Newsletter email → Cleveland subscribers only | ❌ | **NEEDS FIX** (currently sends to all OH) |

### Test Case: Cincinnati Event → Columbus Metro Filter

**Setup**:
1. Create event: "Cincinnati Expo"
2. Location: Cincinnati, OH (95 miles from Columbus)
3. User selects "Columbus metro" (50-mile radius)

**Expected Results**:

| Location | Pass? | Notes |
|----------|-------|-------|
| ☑ /events page hides event | ✅ | Distance > radius |
| ☑ Newsletter email → No Columbus subscribers | ❌ | **NEEDS FIX** (currently sends to all OH) |

---

## Quick Links

- **Full Analysis**: `CITY_TO_METRO_BUCKETING_COMPREHENSIVE_ANALYSIS.md`
- **Implementation Plan**: See full analysis, Phase 1-4
- **Code References**: See Appendix in full analysis
- **Original Issues**: `NEWSLETTER_ISSUES_EXECUTIVE_SUMMARY.md`

---

## Key Takeaways

1. **Bucketing DOES exist** - in /events page and event cancellation emails
2. **Mechanism is geo-spatial** - distance calculation with radius
3. **Inconsistency is the problem** - not applied to newsletters and newsletter emails
4. **Solution is reusable** - centralize into EventMetroAreaMatcher service
5. **Issue #5 is partially fixed** - query works, but bucketing gap remains
