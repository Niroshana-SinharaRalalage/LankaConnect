# Newsletter Metro Matching Solution - Reuse Events Logic

## Problem Statement

**Current Behavior (WRONG):**
- Newsletter linked to Aurora, OH event
- System gets ALL Ohio subscribers (Columbus, Cleveland, Toledo, etc.)
- Columbus subscriber gets email about Cleveland-area event (irrelevant)

**Expected Behavior (CORRECT):**
- Newsletter linked to Aurora, OH event
- System determines Aurora is within Cleveland metro radius
- Only Cleveland metro subscribers get the email

## Existing Solution in Events

**File**: `GetEventsQueryHandler.cs` lines 232-270

**Logic**:
```csharp
// 1. Get metro area center coordinates + radius
var metroData = await GetMetroAreaDataAsync(metroId, cancellationToken);

// 2. Calculate distance between metro center and event location
var distance = CalculateDistance(
    metroData.Value.Latitude,
    metroData.Value.Longitude,
    e.Location!.Coordinates!.Latitude,
    e.Location.Coordinates.Longitude);

// 3. Check if event is within metro radius
var radiusKm = metroData.Value.RadiusMiles * 1.60934;
return distance <= radiusKm;  // Event belongs to this metro if TRUE
```

## Implementation Plan

### Step 1: Create EventMetroAreaMatcher Service

**File**: `src/LankaConnect.Infrastructure/Services/EventMetroAreaMatcher.cs`

```csharp
using LankaConnect.Domain.Events;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Service to determine which metro areas an event belongs to based on geographic proximity
/// Reuses the same logic as GetEventsQueryHandler.FilterEventsByMetroAreasAsync
/// </summary>
public class EventMetroAreaMatcher
{
    private readonly IAppDbContext _dbContext;

    public EventMetroAreaMatcher(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Determines which metro areas contain the given event based on distance/radius
    /// </summary>
    /// <param name="event">Event with location coordinates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of metro area IDs that contain this event</returns>
    public async Task<List<Guid>> GetMetroAreasForEventAsync(
        Event @event,
        CancellationToken cancellationToken = default)
    {
        if (@event.Location?.Coordinates == null)
            return new List<Guid>();

        var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
            ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

        // Get all active metro areas
        var allMetros = await dbContext.Set<MetroArea>()
            .Where(m => m.IsActive)
            .ToListAsync(cancellationToken);

        var matchingMetroIds = new List<Guid>();

        foreach (var metro in allMetros)
        {
            // Calculate distance between metro center and event
            var distance = CalculateDistance(
                metro.CenterLatitude,
                metro.CenterLongitude,
                @event.Location.Coordinates.Latitude,
                @event.Location.Coordinates.Longitude);

            // Convert radius from miles to kilometers
            var radiusKm = metro.RadiusMiles * 1.60934;

            // Event is within this metro area
            if (distance <= radiusKm)
            {
                matchingMetroIds.Add(metro.Id);
            }
        }

        return matchingMetroIds;
    }

    /// <summary>
    /// Calculates the distance between two geographic points using the Haversine formula
    /// EXACT COPY from GetEventsQueryHandler.CalculateDistance (lines 313-330)
    /// </summary>
    private static double CalculateDistance(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = R * c;

        return distance;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
```

### Step 2: Register Service in DI

**File**: `src/LankaConnect.Infrastructure/DependencyInjection.cs`

```csharp
// Add in ConfigureServices method
services.AddScoped<EventMetroAreaMatcher>();
```

### Step 3: Update NewsletterRecipientService

**File**: `src/LankaConnect.Infrastructure/Services/NewsletterRecipientService.cs`

**Replace lines 263-303** with:

```csharp
private readonly EventMetroAreaMatcher _metroMatcher; // Add to constructor

private async Task<NewsletterSubscriberBreakdown> GetSubscribersForEventAsync(
    Guid eventId,
    CancellationToken cancellationToken)
{
    // Fetch event with location
    var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
    if (@event == null || @event.Location?.Coordinates == null)
    {
        _logger.LogWarning("[Phase 6A.74] Event {EventId} not found or has no location", eventId);
        return new NewsletterSubscriberBreakdown(new HashSet<string>(), 0, 0, 0);
    }

    _logger.LogDebug("[Phase 6A.74] Event location: {City}, {State}, Coordinates: ({Lat}, {Lng})",
        @event.Location.Address.City,
        @event.Location.Address.State,
        @event.Location.Coordinates.Latitude,
        @event.Location.Coordinates.Longitude);

    // NEW: Determine which metro areas this event belongs to
    var eventMetroIds = await _metroMatcher.GetMetroAreasForEventAsync(@event, cancellationToken);

    if (!eventMetroIds.Any())
    {
        _logger.LogWarning(
            "[Phase 6A.74] Event {EventId} at ({Lat}, {Lng}) does not fall within any metro area radius",
            eventId,
            @event.Location.Coordinates.Latitude,
            @event.Location.Coordinates.Longitude);

        // Fallback: Use state-level matching if no metro match
        var state = @event.Location.Address.State;
        var stateSubscribers = await _subscriberRepository.GetConfirmedSubscribersByStateAsync(state, cancellationToken);
        var emails = stateSubscribers.Select(s => s.Email.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new NewsletterSubscriberBreakdown(
            Emails: emails,
            MetroCount: 0,
            StateCount: stateSubscribers.Count,
            AllLocationsCount: 0);
    }

    _logger.LogInformation(
        "[Phase 6A.74] Event {EventId} belongs to {Count} metro areas: [{MetroIds}]",
        eventId,
        eventMetroIds.Count,
        string.Join(", ", eventMetroIds));

    // Get subscribers for all matching metro areas
    return await GetSubscribersByMetroAreasAsync(eventMetroIds, cancellationToken);
}
```

## Expected Behavior After Fix

### Scenario 1: Aurora, OH Event

**Event**: Aurora, OH at coordinates (41.3175, -81.3473)

**Metro Area Matching**:
1. Calculate distance to Cleveland metro center (41.4993, -81.6944, radius 30 miles)
2. Distance ≈ 16 miles
3. 16 miles < 30 miles → **Aurora event belongs to Cleveland metro**

**Subscribers**:
- abc@xyz.com (Cleveland metro) → ✅ Gets email
- pqr@srv.com (Columbus metro) → ❌ Does NOT get email

### Scenario 2: Columbus Event

**Event**: Columbus, OH at coordinates (39.9612, -82.9988)

**Metro Area Matching**:
1. Calculate distance to Columbus metro center (39.9612, -82.9988, radius 30 miles)
2. Distance ≈ 0 miles
3. 0 miles < 30 miles → **Columbus event belongs to Columbus metro**

**Subscribers**:
- abc@xyz.com (Cleveland metro) → ❌ Does NOT get email
- pqr@srv.com (Columbus metro) → ✅ Gets email

## Testing Plan

### Unit Tests

**File**: `tests/LankaConnect.Infrastructure.Tests/Services/EventMetroAreaMatcherTests.cs`

```csharp
[Fact]
public async Task GetMetroAreasForEvent_AuroraEvent_ReturnsClevelandandMetro()
{
    // Arrange: Event in Aurora, OH (41.3175, -81.3473)
    var auroraEvent = CreateEventAtCoordinates(41.3175, -81.3473);

    // Act
    var metroIds = await _matcher.GetMetroAreasForEventAsync(auroraEvent);

    // Assert
    metroIds.Should().Contain(ClevelandMetroId);
    metroIds.Should().NotContain(ColumbusMetroId);
}

[Fact]
public async Task GetMetroAreasForEvent_ColumbusEvent_ReturnsColumbusMetro()
{
    // Arrange: Event in Columbus, OH (39.9612, -82.9988)
    var columbusEvent = CreateEventAtCoordinates(39.9612, -82.9988);

    // Act
    var metroIds = await _matcher.GetMetroAreasForEventAsync(columbusEvent);

    // Assert
    metroIds.Should().Contain(ColumbusMetroId);
    metroIds.Should().NotContain(ClevelandMetroId);
}
```

### Integration Tests

**File**: `tests/LankaConnect.IntegrationTests/Services/NewsletterRecipientServiceTests.cs`

```csharp
[Fact]
public async Task GetSubscribersForEvent_AuroraEvent_OnlyClevelandandSubscribers()
{
    // Arrange
    var clevelandSubscriber = await CreateSubscriber("cleveland@test.com", ClevelandMetroId);
    var columbusSubscriber = await CreateSubscriber("columbus@test.com", ColumbusMetroId);
    var auroraEvent = await CreateEventInAurora();

    // Act
    var breakdown = await _service.GetSubscribersForEventAsync(auroraEvent.Id);

    // Assert
    breakdown.Emails.Should().Contain("cleveland@test.com");
    breakdown.Emails.Should().NotContain("columbus@test.com");
}
```

## Files to Modify

1. ✅ Create: `src/LankaConnect.Infrastructure/Services/EventMetroAreaMatcher.cs`
2. ✅ Modify: `src/LankaConnect.Infrastructure/DependencyInjection.cs` (add DI registration)
3. ✅ Modify: `src/LankaConnect.Infrastructure/Services/NewsletterRecipientService.cs` (update GetSubscribersForEventAsync)
4. ✅ Create: `tests/LankaConnect.Infrastructure.Tests/Services/EventMetroAreaMatcherTests.cs`
5. ✅ Create: `tests/LankaConnect.IntegrationTests/Services/NewsletterRecipientServiceTests.cs`

## Success Criteria

- ✅ Aurora event → Only Cleveland metro subscribers
- ✅ Columbus event → Only Columbus metro subscribers
- ✅ Event outside all metros → Fallback to state-level matching
- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ Build: 0 errors, 0 warnings

## Notes

- **Reuses existing logic** from GetEventsQueryHandler (lines 232-270, 313-330)
- **No database changes** required
- **Backward compatible** with state-level fallback
- **Same geographic calculation** ensures consistency between event filtering and newsletter recipients
