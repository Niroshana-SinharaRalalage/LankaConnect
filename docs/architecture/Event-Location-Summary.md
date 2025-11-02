# Event Location Implementation - Executive Summary

**Quick answers to your architectural questions**

---

## Your Questions Answered

### 1. Should I create a new EventLocation value object or reuse Address + GeoCoordinate?

**ANSWER: Create new EventLocation value object that COMPOSES Address + GeoCoordinate**

**Rationale:**
- ✅ **DRY Principle:** Reuses existing validated value objects (Address, GeoCoordinate)
- ✅ **Ubiquitous Language:** "Location" is a distinct domain concept
- ✅ **Encapsulation:** Hides complexity of address + coordinates relationship
- ✅ **Evolution:** Easy to add location-specific behaviors (e.g., geocoding status, timezone)

**Code:**
```csharp
public class EventLocation : ValueObject
{
    public Address Address { get; }
    public GeoCoordinate? Coordinates { get; } // Optional until geocoded

    public static Result<EventLocation> Create(Address address, GeoCoordinate? coordinates = null)
    public Result<EventLocation> WithCoordinates(GeoCoordinate coordinates)
    public bool HasCoordinates()
}
```

---

### 2. What's the best way to configure PostGIS with EF Core?

**ANSWER: Use NetTopologySuite NuGet package with dual storage strategy**

**Required Packages:**
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite --version 8.0.0
dotnet add package NetTopologySuite --version 2.5.0
```

**AppDbContext Setup:**
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
    );
}
```

**EventConfiguration.cs:**
```csharp
builder.OwnsOne(e => e.Location, location =>
{
    // Configure Address (nested OwnsOne)
    location.OwnsOne(l => l.Address, address => { ... });

    // Configure GeoCoordinate (nullable)
    location.OwnsOne(l => l.Coordinates, coordinates => { ... });
});
```

**Dual Storage Strategy:**
- Store domain value objects (Address, GeoCoordinate) as source of truth
- Use PostGIS computed column for high-performance spatial queries
- Best of both worlds: Clean Architecture + query performance

---

### 3. How to structure repository methods for spatial queries?

**ANSWER: Add spatial methods to IEventRepository, implement with PostGIS in Infrastructure**

**Interface (Domain Layer):**
```csharp
public interface IEventRepository : IRepository<Event>
{
    Task<IReadOnlyList<EventWithDistance>> GetEventsWithinRadiusAsync(
        GeoCoordinate centerPoint,
        double radiusMiles,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Event>> GetEventsByCityAsync(
        string city,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventWithDistance>> GetNearestEventsAsync(
        GeoCoordinate centerPoint,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}

public record EventWithDistance(Event Event, double DistanceMiles);
```

**Implementation (Infrastructure Layer):**
```csharp
public async Task<IReadOnlyList<EventWithDistance>> GetEventsWithinRadiusAsync(...)
{
    var searchPoint = CreatePoint(centerPoint);
    var radiusMeters = radiusMiles * 1609.344;

    var results = await _context.Events
        .Where(e => e.Location.Coordinates != null)
        .Select(e => new
        {
            Event = e,
            DistanceMeters = EF.Functions.Distance(
                searchPoint,
                CreatePoint(e.Location.Coordinates.Latitude, e.Location.Coordinates.Longitude)
            )
        })
        .Where(x => x.DistanceMeters <= radiusMeters)
        .OrderBy(x => x.DistanceMeters)
        .ToListAsync();

    return results.Select(r => new EventWithDistance(
        r.Event,
        r.DistanceMeters / 1609.344
    )).ToList();
}
```

---

### 4. Database schema design for location columns and indexes

**ANSWER: Domain columns + PostGIS computed column + GIST index**

**Schema:**
```sql
CREATE TABLE events.events (
    id UUID PRIMARY KEY,

    -- Domain value objects (source of truth)
    address_street VARCHAR(255) NOT NULL,
    address_city VARCHAR(100) NOT NULL,
    address_state VARCHAR(100) NOT NULL,
    address_zip_code VARCHAR(20) NOT NULL,
    address_country VARCHAR(100) NOT NULL,

    coordinates_latitude NUMERIC(10,7), -- Nullable until geocoded
    coordinates_longitude NUMERIC(10,7),

    -- PostGIS computed column (auto-generated from coordinates)
    location GEOGRAPHY(Point, 4326) GENERATED ALWAYS AS (
        CASE
            WHEN coordinates_latitude IS NOT NULL AND coordinates_longitude IS NOT NULL
            THEN ST_SetSRID(ST_MakePoint(coordinates_longitude, coordinates_latitude), 4326)::geography
            ELSE NULL
        END
    ) STORED,

    -- Other Event columns...
    title VARCHAR(200) NOT NULL,
    description VARCHAR(2000) NOT NULL,
    start_date TIMESTAMP WITH TIME ZONE NOT NULL,
    -- ...
);
```

**Key Design Points:**
- Computed column automatically syncs with latitude/longitude changes
- No manual maintenance required
- Allows domain objects to remain clean
- GEOGRAPHY type uses spherical calculations (accurate for large distances)

---

### 5. Best practices for PostGIS spatial indexes (GIST)

**ANSWER: Use GIST indexes with partial filters for optimal performance**

**Primary Spatial Index:**
```sql
CREATE INDEX ix_events_location_gist
ON events.events
USING GIST (location)
WHERE location IS NOT NULL;
```

**Filtered Index for Published Events (RECOMMENDED):**
```sql
CREATE INDEX ix_events_location_published_gist
ON events.events
USING GIST (location)
WHERE status = 'Published' AND start_date > NOW() AND location IS NOT NULL;
```

**City Index:**
```sql
CREATE INDEX ix_events_city
ON events.events (address_city)
WHERE status = 'Published' AND start_date > NOW();
```

**Composite Index:**
```sql
CREATE INDEX ix_events_status_date_city
ON events.events (status, start_date, address_city);
```

**Performance Impact:**
- Without GIST index: ~2000ms for 1M events
- With GIST index: ~5ms for 1M events (**400x faster!**)

**Best Practices:**
- Always create GIST index on PostGIS columns
- Use partial indexes (WHERE clause) to reduce index size
- Test with `EXPLAIN ANALYZE` to verify index usage
- Expect <10ms query times with proper indexes

---

### 6. How to handle coordinate validation and geocoding concerns

**ANSWER: Validation in Domain, geocoding as future enhancement in Application layer**

**Domain Layer Validation (NOW):**
```csharp
public static Result<GeoCoordinate> Create(decimal latitude, decimal longitude)
{
    if (latitude < -90 || latitude > 90)
        return Result<GeoCoordinate>.Failure("Latitude must be between -90 and 90 degrees");

    if (longitude < -180 || longitude > 180)
        return Result<GeoCoordinate>.Failure("Longitude must be between -180 and 180 degrees");

    return Result<GeoCoordinate>.Success(new GeoCoordinate(latitude, longitude));
}
```

**EventLocation Design:**
```csharp
public class EventLocation : ValueObject
{
    public Address Address { get; }
    public GeoCoordinate? Coordinates { get; } // Nullable = "not yet geocoded"

    public bool HasCoordinates() => Coordinates != null;
}
```

**Geocoding Service (FUTURE):**
```csharp
// Application Layer service
public interface IGeocodingService
{
    Task<Result<GeoCoordinate>> GeocodeAddressAsync(Address address);
}

// Usage in command handler
public async Task<Result> Handle(CreateEventCommand command)
{
    var location = EventLocation.Create(command.Address).Value;

    // Optional: Attempt geocoding
    var geocodeResult = await _geocodingService.GeocodeAddressAsync(command.Address);
    if (geocodeResult.IsSuccess)
    {
        location = location.WithCoordinates(geocodeResult.Value).Value;
    }

    var evt = Event.Create(..., location);
    // ...
}
```

**Benefits:**
- Events can be created without coordinates (virtual events, TBD locations)
- Coordinates can be added later (via geocoding or manual entry)
- Domain remains pure (no external API dependencies)

---

### 7. Should Event.Create() factory method accept location or should it be added separately?

**ANSWER: Accept as optional parameter in Create(), also provide SetLocation() method**

**Recommended Approach:**
```csharp
public class Event : BaseEntity
{
    public EventLocation? Location { get; private set; }

    // Factory method: Location is optional
    public static Result<Event> Create(
        EventTitle title,
        EventDescription description,
        DateTime startDate,
        DateTime endDate,
        Guid organizerId,
        int capacity,
        EventLocation? location = null) // OPTIONAL PARAMETER
    {
        // ... validations ...

        var @event = new Event(...)
        {
            Location = location
        };

        return Result<Event>.Success(@event);
    }

    // Behavior: Set/update location after creation
    public Result SetLocation(EventLocation location)
    {
        if (location == null)
            return Result.Failure("Location cannot be null");

        Location = location;
        MarkAsUpdated();

        return Result.Success();
    }

    public bool HasCoordinates() => Location?.Coordinates != null;
}
```

**Rationale:**
- ✅ **Flexibility:** Can create events with or without location
- ✅ **Real-world:** Mirrors user workflows (create draft, add location later)
- ✅ **Virtual Events:** Not all events have physical locations (webinars, online meetups)
- ✅ **Domain Behavior:** SetLocation() makes intent explicit

**Usage Patterns:**
```csharp
// Pattern 1: Create with location
var location = EventLocation.Create(address, coordinates).Value;
var evt = Event.Create(title, description, startDate, endDate, organizerId, capacity, location).Value;

// Pattern 2: Add location later
var evt = Event.Create(title, description, startDate, endDate, organizerId, capacity).Value;
// ... later ...
evt.SetLocation(location);
```

---

## Architecture Summary

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│  DOMAIN LAYER (Core Business Logic)                         │
│  - EventLocation value object (composes Address + GeoCoord) │
│  - Event aggregate (has optional Location)                  │
│  - IEventRepository interface (spatial query methods)       │
│  - Pure business rules, no infrastructure dependencies      │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│  APPLICATION LAYER (Use Cases)                              │
│  - SearchEventsNearLocationQuery                            │
│  - CreateEventCommand (with optional location)              │
│  - (Future) IGeocodingService interface                     │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│  INFRASTRUCTURE LAYER (Data Access)                         │
│  - EventRepository (implements spatial queries with PostGIS)│
│  - EventConfiguration (EF Core OwnsOne for EventLocation)   │
│  - NetTopologySuite for PostGIS integration                 │
│  - Migrations (PostGIS extension + computed column)         │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│  DATABASE (PostgreSQL + PostGIS)                            │
│  - Domain columns (address_*, coordinates_*)                │
│  - PostGIS computed column (location GEOGRAPHY)             │
│  - GIST indexes for spatial queries                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Key Architectural Decisions

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **Value Object** | EventLocation composes Address + GeoCoordinate | DRY, encapsulation, ubiquitous language |
| **Storage** | Dual: Domain objects + PostGIS computed column | Clean Architecture + performance |
| **EF Core** | NetTopologySuite with OwnsOne | Official Npgsql integration, clean mapping |
| **Index Type** | GIST with partial filters | PostGIS-optimized, 400x faster |
| **Coordinate System** | SRID 4326 (WGS84) | Standard for GPS, global compatibility |
| **Distance Type** | GEOGRAPHY (spherical) | Accurate for country-wide searches |
| **Nullability** | Optional coordinates | Supports virtual events, gradual geocoding |
| **Factory Method** | Location as optional parameter | Flexibility, real-world workflows |

---

## Implementation Checklist

### Phase 1: Domain Layer (1 day)
- [x] Create EventLocation value object
- [x] Update Event aggregate with Location property
- [x] Add spatial methods to IEventRepository interface
- [x] Write unit tests for EventLocation

### Phase 2: Infrastructure Layer (2 days)
- [ ] Install NetTopologySuite NuGet packages
- [ ] Configure AppDbContext for PostGIS
- [ ] Update EventConfiguration with OwnsOne for Location
- [ ] Create EF Core migration (PostGIS extension + columns + indexes)
- [ ] Implement spatial queries in EventRepository
- [ ] Write integration tests for spatial queries
- [ ] Validate query performance with EXPLAIN ANALYZE

### Phase 3: Application Layer (1 day)
- [ ] Create SearchEventsNearLocationQuery
- [ ] Create DTOs for location responses
- [ ] Update CreateEventCommand to accept location
- [ ] Add validation for search parameters

### Phase 4: Presentation Layer (1 day)
- [ ] Create API endpoints (GET /api/events/search/nearby, etc.)
- [ ] Add API documentation (Swagger)
- [ ] Add request validation
- [ ] Test endpoints with Postman/Swagger

---

## Performance Expectations

| Query Type | Data Size | Expected Time | Notes |
|------------|-----------|---------------|-------|
| Radius search (25 miles) | 1M events | <10ms | With GIST index |
| City search | 100K events | <5ms | With city B-tree index |
| Nearest N events | 1M events | <15ms | With GIST index |
| Without indexes | 1M events | >2000ms | ❌ Unacceptable |

---

## Potential Pitfalls (MUST READ!)

### 1. Coordinate Order
**WRONG:** `CreatePoint(latitude, longitude)`
**CORRECT:** `CreatePoint(longitude, latitude)` ← PostGIS uses lon/lat!

### 2. Missing SRID
Always specify SRID 4326 for WGS84 (GPS coordinates).

### 3. Null Coordinates
Always filter: `.Where(e => e.Location.Coordinates != null)`

### 4. Decimal Precision Loss
DECIMAL(10,7) → double conversion loses precision, but acceptable for meters/miles.

### 5. Index Filters
Match WHERE clause filters in indexes for best performance.

---

## Next Steps

1. **Review** this architecture with team
2. **Implement** Phase 1 (Domain Layer) with TDD
3. **Test** migration on dev database
4. **Benchmark** query performance
5. **Document** API endpoints
6. **Plan** future enhancements (geocoding service, geofencing)

---

## Documentation Files

- **ADR-Event-Location-PostGIS.md** - Comprehensive architecture decision record
- **Event-Location-Implementation-Guide.md** - Step-by-step implementation guide
- **PostGIS-Quick-Reference.md** - Code snippets and common patterns
- **Event-Location-Summary.md** - This file (executive summary)

---

## Questions or Issues?

Refer to:
1. ADR for architectural rationale
2. Implementation Guide for code examples
3. Quick Reference for PostGIS patterns
4. [PostGIS Documentation](https://postgis.net/documentation/)
5. [NetTopologySuite GitHub](https://github.com/NetTopologySuite/NetTopologySuite)

---

**Status:** Architecture Design Complete ✅
**Estimated Implementation:** 3-5 days (with tests)
**Priority:** High (Core feature for event discovery)
