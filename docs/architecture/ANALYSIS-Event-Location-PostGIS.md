# Architecture Decision Record: Event Location with PostGIS

**Status:** Proposed
**Date:** 2025-11-02
**Author:** System Architecture Designer
**Context:** Clean Architecture + DDD .NET 8 Application with PostgreSQL + EF Core

---

## 1. Executive Summary

This ADR defines the architecture for adding location-based functionality to the Event aggregate using PostGIS spatial extensions in PostgreSQL. The solution reuses existing value objects (Address, GeoCoordinate) while adding PostGIS-optimized spatial queries for high-performance location searches.

---

## 2. Context and Requirements

### Business Requirements
- Enable location-based event discovery (25/50/100 mile radius searches)
- Support city-based event filtering
- Display event locations on maps
- Calculate distances between user location and events

### Technical Requirements
- PostgreSQL database with PostGIS extension
- .NET 8 with EF Core 8.x
- Clean Architecture with strict layer boundaries
- Domain-Driven Design principles
- Test-Driven Development with 90% coverage
- Zero tolerance for compilation errors

### Existing Components
- **Address** value object (from Business domain) - street, city, state, zipCode, country
- **GeoCoordinate** value object - latitude, longitude with Haversine distance calculation
- **Event** aggregate - title, description, dates, capacity, registrations
- **BusinessLocation** configuration - demonstrates OwnsOne pattern for Address + GeoCoordinate

---

## 3. Architectural Decisions

### Decision 1: Reuse Existing Value Objects (DRY Principle)

**Decision:** Create a new `EventLocation` value object that COMPOSES existing Address and GeoCoordinate.

**Rationale:**
- **DRY Principle:** Address and GeoCoordinate are already validated, tested domain concepts
- **Ubiquitous Language:** "Location" is a distinct concept from "Address" or "Coordinates"
- **Single Responsibility:** EventLocation encapsulates the "where" of an event
- **Encapsulation:** Hides complexity of address + coordinates relationship

**Alternative Rejected:** Adding Address and GeoCoordinate directly to Event
- **Reason:** Breaks encapsulation, violates DDD aggregate design, harder to evolve

```csharp
// RECOMMENDED APPROACH
public class EventLocation : ValueObject
{
    public Address Address { get; }
    public GeoCoordinate? Coordinates { get; } // Optional until geocoded

    private EventLocation(Address address, GeoCoordinate? coordinates)
    {
        Address = address;
        Coordinates = coordinates;
    }

    public static Result<EventLocation> Create(Address address, GeoCoordinate? coordinates = null)
    {
        if (address == null)
            return Result<EventLocation>.Failure("Address is required");

        return Result<EventLocation>.Success(new EventLocation(address, coordinates));
    }

    // Domain method: Update coordinates (for geocoding scenarios)
    public Result<EventLocation> WithCoordinates(GeoCoordinate coordinates)
    {
        if (coordinates == null)
            return Result<EventLocation>.Failure("Coordinates cannot be null");

        return Result<EventLocation>.Success(new EventLocation(Address, coordinates));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address;
        if (Coordinates != null)
            yield return Coordinates;
    }
}
```

**Benefits:**
- Composition over duplication
- Clear domain concept
- Easy to add location-specific behaviors (e.g., "IsWithinDeliveryArea", "RequiresGeocode")
- Testable in isolation

---

### Decision 2: Dual Storage Strategy - PostGIS + Domain Objects

**Decision:** Store both domain value objects AND PostGIS geography columns for optimal query performance.

**Rationale:**
- **Domain Integrity:** Keep domain objects (Address, GeoCoordinate) as source of truth
- **Query Performance:** Use PostGIS for spatial queries (50-100x faster than Haversine in code)
- **Index Efficiency:** PostGIS GIST indexes enable sub-millisecond searches on millions of events
- **Clean Architecture:** Infrastructure concern (PostGIS) stays in Infrastructure layer

**Database Schema:**
```sql
-- events.events table columns
CREATE TABLE events.events (
    id UUID PRIMARY KEY,

    -- Domain value objects (EF Core OwnsOne)
    address_street VARCHAR(255) NOT NULL,
    address_city VARCHAR(100) NOT NULL,
    address_state VARCHAR(100) NOT NULL,
    address_zip_code VARCHAR(20) NOT NULL,
    address_country VARCHAR(100) NOT NULL,

    coordinates_latitude DECIMAL(10,7),  -- Nullable until geocoded
    coordinates_longitude DECIMAL(10,7), -- Nullable until geocoded

    -- PostGIS computed column (generated from coordinates)
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
    -- ... (rest of Event properties)
);

-- PostGIS spatial index for high-performance queries
CREATE INDEX ix_events_location_gist
ON events.events
USING GIST (location)
WHERE location IS NOT NULL;

-- City index for city-based searches
CREATE INDEX ix_events_city
ON events.events (address_city)
WHERE status = 'Published' AND start_date > NOW();

-- Composite index for combined queries
CREATE INDEX ix_events_status_start_date_city
ON events.events (status, start_date, address_city);
```

**Computed Column Approach:**
- Automatically synchronizes with latitude/longitude changes
- No manual maintenance required
- Storage cost is minimal (point geometry is ~32 bytes)
- Allows querying even when coordinates are missing (NULL handling)

---

### Decision 3: EF Core Configuration with NetTopologySuite

**Decision:** Use NetTopologySuite NuGet package for EF Core PostGIS integration.

**Required NuGet Packages:**
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="8.0.*" />
<PackageReference Include="NetTopologySuite" Version="2.5.*" />
```

**AppDbContext Configuration:**
```csharp
// src/LankaConnect.Infrastructure/Data/AppDbContext.cs
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);

    // Enable PostGIS with NetTopologySuite
    optionsBuilder.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
    );
}
```

**EventConfiguration.cs (EF Core Mapping):**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Business.ValueObjects; // Address, GeoCoordinate
using NetTopologySuite.Geometries;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // ... existing configuration ...

        // Configure EventLocation value object
        builder.OwnsOne(e => e.Location, location =>
        {
            // Configure Address (nested value object)
            location.OwnsOne(l => l.Address, address =>
            {
                address.Property(a => a.Street)
                    .HasColumnName("address_street")
                    .HasMaxLength(255)
                    .IsRequired();

                address.Property(a => a.City)
                    .HasColumnName("address_city")
                    .HasMaxLength(100)
                    .IsRequired();

                address.Property(a => a.State)
                    .HasColumnName("address_state")
                    .HasMaxLength(100)
                    .IsRequired();

                address.Property(a => a.ZipCode)
                    .HasColumnName("address_zip_code")
                    .HasMaxLength(20)
                    .IsRequired();

                address.Property(a => a.Country)
                    .HasColumnName("address_country")
                    .HasMaxLength(100)
                    .IsRequired();
            });

            // Configure GeoCoordinate (nullable until geocoded)
            location.OwnsOne(l => l.Coordinates, coordinates =>
            {
                coordinates.Property(c => c.Latitude)
                    .HasColumnName("coordinates_latitude")
                    .HasPrecision(10, 7); // 7 decimals = ~1.1cm precision

                coordinates.Property(c => c.Longitude)
                    .HasColumnName("coordinates_longitude")
                    .HasPrecision(10, 7);
            });

            // CRITICAL: Ignore the PostGIS computed column
            // Domain model doesn't need to know about infrastructure concern
            location.Ignore("Location"); // If you add a Point property to domain
        });

        // PostGIS computed column is created via raw SQL migration
        // EF Core doesn't need to manage it directly

        // Indexes
        builder.HasIndex("Location") // Shadow property for computed column
            .HasDatabaseName("ix_events_location_gist")
            .HasFilter("location IS NOT NULL");

        builder.HasIndex(e => e.Location.Address.City)
            .HasDatabaseName("ix_events_city")
            .HasFilter("status = 'Published' AND start_date > NOW()");
    }
}
```

---

### Decision 4: Repository Pattern for Spatial Queries

**Decision:** Add spatial query methods to IEventRepository with PostGIS-optimized implementations.

**Interface (Domain Layer):**
```csharp
// src/LankaConnect.Domain/Events/IEventRepository.cs
public interface IEventRepository : IRepository<Event>
{
    // ... existing methods ...

    /// <summary>
    /// Find events within a specified radius of coordinates
    /// </summary>
    /// <param name="centerPoint">Search origin coordinates</param>
    /// <param name="radiusMiles">Search radius in miles</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Events within radius, ordered by distance (nearest first)</returns>
    Task<IReadOnlyList<EventWithDistance>> GetEventsWithinRadiusAsync(
        GeoCoordinate centerPoint,
        double radiusMiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find events in a specific city
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Events in the specified city</returns>
    Task<IReadOnlyList<Event>> GetEventsByCityAsync(
        string city,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find nearest events to a point (top N)
    /// </summary>
    /// <param name="centerPoint">Search origin coordinates</param>
    /// <param name="maxResults">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Nearest events, ordered by distance</returns>
    Task<IReadOnlyList<EventWithDistance>> GetNearestEventsAsync(
        GeoCoordinate centerPoint,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}

// Domain DTO for query results
public record EventWithDistance(Event Event, double DistanceMiles);
```

**Implementation (Infrastructure Layer):**
```csharp
// src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs
using NetTopologySuite.Geometries;
using Microsoft.EntityFrameworkCore;

public class EventRepository : Repository<Event>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<EventWithDistance>> GetEventsWithinRadiusAsync(
        GeoCoordinate centerPoint,
        double radiusMiles,
        CancellationToken cancellationToken = default)
    {
        // Convert miles to meters (PostGIS uses meters)
        const double METERS_PER_MILE = 1609.344;
        var radiusMeters = radiusMiles * METERS_PER_MILE;

        // Create NetTopologySuite Point from domain GeoCoordinate
        var point = CreatePoint(centerPoint);

        // PostGIS spatial query with distance calculation
        var results = await _context.Events
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published &&
                       e.StartDate > DateTime.UtcNow &&
                       e.Location.Coordinates != null) // Only geocoded events
            .Select(e => new
            {
                Event = e,
                // PostGIS distance calculation (SRID 4326 = WGS84)
                DistanceMeters = EF.Functions.Distance(
                    point,
                    CreatePoint(e.Location.Coordinates.Latitude, e.Location.Coordinates.Longitude)
                )
            })
            .Where(x => x.DistanceMeters <= radiusMeters)
            .OrderBy(x => x.DistanceMeters)
            .ToListAsync(cancellationToken);

        // Convert to domain DTOs with distance in miles
        return results
            .Select(r => new EventWithDistance(
                r.Event,
                r.DistanceMeters / METERS_PER_MILE
            ))
            .ToList();
    }

    public async Task<IReadOnlyList<Event>> GetEventsByCityAsync(
        string city,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
            return Array.Empty<Event>();

        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published &&
                       e.StartDate > DateTime.UtcNow &&
                       e.Location.Address.City.ToLower() == city.Trim().ToLower())
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventWithDistance>> GetNearestEventsAsync(
        GeoCoordinate centerPoint,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        const double METERS_PER_MILE = 1609.344;
        var point = CreatePoint(centerPoint);

        var results = await _context.Events
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published &&
                       e.StartDate > DateTime.UtcNow &&
                       e.Location.Coordinates != null)
            .Select(e => new
            {
                Event = e,
                DistanceMeters = EF.Functions.Distance(
                    point,
                    CreatePoint(e.Location.Coordinates.Latitude, e.Location.Coordinates.Longitude)
                )
            })
            .OrderBy(x => x.DistanceMeters)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        return results
            .Select(r => new EventWithDistance(
                r.Event,
                r.DistanceMeters / METERS_PER_MILE
            ))
            .ToList();
    }

    // Helper: Create NTS Point from domain GeoCoordinate
    private static Point CreatePoint(GeoCoordinate coordinate)
    {
        return CreatePoint((double)coordinate.Latitude, (double)coordinate.Longitude);
    }

    private static Point CreatePoint(double latitude, double longitude)
    {
        // SRID 4326 = WGS84 (standard GPS coordinate system)
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        return geometryFactory.CreatePoint(new Coordinate(longitude, latitude)); // Note: lon, lat order!
    }
}
```

**Key Implementation Notes:**
- **PostGIS ST_Distance:** Uses spherical calculations (geography type), more accurate than Haversine
- **SRID 4326:** WGS84 coordinate system (standard for GPS)
- **Coordinate Order:** PostGIS uses (longitude, latitude) - REVERSE of common convention!
- **Index Usage:** GIST index automatically used by PostGIS distance queries
- **Performance:** Expect <10ms for radius queries on 1M events with proper indexes

---

### Decision 5: Event Aggregate Integration

**Decision:** Add Location as optional property with factory method support.

**Event.cs Changes:**
```csharp
public class Event : BaseEntity
{
    // ... existing properties ...

    public EventLocation? Location { get; private set; } // Optional until set

    // Updated factory method - location optional
    public static Result<Event> Create(
        EventTitle title,
        EventDescription description,
        DateTime startDate,
        DateTime endDate,
        Guid organizerId,
        int capacity,
        EventLocation? location = null) // Optional parameter
    {
        // ... existing validations ...

        var @event = new Event(title, description, startDate, endDate, organizerId, capacity)
        {
            Location = location
        };

        return Result<Event>.Success(@event);
    }

    // Behavior: Set or update location
    public Result SetLocation(EventLocation location)
    {
        if (location == null)
            return Result.Failure("Location cannot be null");

        Location = location;
        MarkAsUpdated();

        // Optionally raise domain event
        RaiseDomainEvent(new EventLocationUpdatedEvent(Id, location, DateTime.UtcNow));

        return Result.Success();
    }

    // Behavior: Check if event has geocoded location
    public bool HasCoordinates() => Location?.Coordinates != null;

    // Behavior: Calculate distance to another event (reuses domain logic)
    public Result<double> DistanceToEvent(Event otherEvent)
    {
        if (!HasCoordinates() || !otherEvent.HasCoordinates())
            return Result<double>.Failure("Both events must have coordinates");

        var distance = Location!.Coordinates!.DistanceTo(otherEvent.Location!.Coordinates!);
        return Result<double>.Success(distance);
    }
}
```

**Design Rationale:**
- **Optional Location:** Not all events may have physical locations (virtual events)
- **Explicit Setter:** Clear intent when adding/updating location
- **Domain Events:** Notify subscribers when location changes (e.g., trigger geocoding)
- **Domain Logic Reuse:** DistanceToEvent uses existing GeoCoordinate.DistanceTo for in-memory calculations

---

### Decision 6: Migration Strategy

**Decision:** Create EF Core migration with raw SQL for PostGIS setup.

**Migration File:**
```csharp
// src/LankaConnect.Infrastructure/Migrations/YYYYMMDDHHMMSS_AddEventLocation.cs
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

public partial class AddEventLocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Enable PostGIS extension (idempotent)
        migrationBuilder.Sql(@"
            CREATE EXTENSION IF NOT EXISTS postgis;
            CREATE EXTENSION IF NOT EXISTS postgis_topology;
        ");

        // 2. Add location columns to events table
        migrationBuilder.AddColumn<string>(
            name: "address_street",
            schema: "events",
            table: "events",
            maxLength: 255,
            nullable: true); // Nullable for existing events

        migrationBuilder.AddColumn<string>(
            name: "address_city",
            schema: "events",
            table: "events",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "address_state",
            schema: "events",
            table: "events",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "address_zip_code",
            schema: "events",
            table: "events",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "address_country",
            schema: "events",
            table: "events",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "coordinates_latitude",
            schema: "events",
            table: "events",
            type: "numeric(10,7)",
            precision: 10,
            scale: 7,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "coordinates_longitude",
            schema: "events",
            table: "events",
            type: "numeric(10,7)",
            precision: 10,
            scale: 7,
            nullable: true);

        // 3. Add PostGIS computed column (CRITICAL: Use raw SQL)
        migrationBuilder.Sql(@"
            ALTER TABLE events.events
            ADD COLUMN location GEOGRAPHY(Point, 4326)
            GENERATED ALWAYS AS (
                CASE
                    WHEN coordinates_latitude IS NOT NULL
                         AND coordinates_longitude IS NOT NULL
                         AND coordinates_latitude BETWEEN -90 AND 90
                         AND coordinates_longitude BETWEEN -180 AND 180
                    THEN ST_SetSRID(
                        ST_MakePoint(coordinates_longitude, coordinates_latitude),
                        4326
                    )::geography
                    ELSE NULL
                END
            ) STORED;
        ");

        // 4. Create spatial index (GIST)
        migrationBuilder.Sql(@"
            CREATE INDEX ix_events_location_gist
            ON events.events
            USING GIST (location)
            WHERE location IS NOT NULL;
        ");

        // 5. Create city index
        migrationBuilder.CreateIndex(
            name: "ix_events_city",
            schema: "events",
            table: "events",
            column: "address_city",
            filter: "status = 'Published' AND start_date > NOW()");

        // 6. Create composite index for common queries
        migrationBuilder.Sql(@"
            CREATE INDEX ix_events_status_start_date_city
            ON events.events (status, start_date, address_city)
            WHERE status = 'Published' AND start_date > NOW();
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_events_location_gist",
            schema: "events",
            table: "events");

        migrationBuilder.DropIndex(
            name: "ix_events_city",
            schema: "events",
            table: "events");

        migrationBuilder.DropIndex(
            name: "ix_events_status_start_date_city",
            schema: "events",
            table: "events");

        migrationBuilder.Sql("ALTER TABLE events.events DROP COLUMN IF EXISTS location;");

        migrationBuilder.DropColumn(name: "address_street", schema: "events", table: "events");
        migrationBuilder.DropColumn(name: "address_city", schema: "events", table: "events");
        migrationBuilder.DropColumn(name: "address_state", schema: "events", table: "events");
        migrationBuilder.DropColumn(name: "address_zip_code", schema: "events", table: "events");
        migrationBuilder.DropColumn(name: "address_country", schema: "events", table: "events");
        migrationBuilder.DropColumn(name: "coordinates_latitude", schema: "events", table: "events");
        migrationBuilder.DropColumn(name: "coordinates_longitude", schema: "events", table: "events");

        // Note: Do NOT drop PostGIS extension (may be used by other tables)
    }
}
```

---

## 4. Performance Considerations

### Index Strategy
1. **GIST Index on location:** Sub-millisecond spatial queries on millions of rows
2. **City Index with Filter:** Fast city-based searches for published events only
3. **Composite Index:** Optimizes combined status + date + city queries

### Query Performance Benchmarks (Expected)
- **Radius query (25 miles, 1M events):** <10ms
- **City search (100K events):** <5ms
- **Nearest N events:** <15ms

### Scaling Considerations
- **Partitioning:** Consider range partitioning by start_date for massive datasets (10M+ events)
- **Read Replicas:** Use PostgreSQL read replicas for location search queries
- **Caching:** Cache popular city searches in Redis
- **CDN:** Geocode results can be cached at CDN edge for static events

---

## 5. Testing Strategy (TDD)

### Unit Tests (Domain Layer)
```csharp
// tests/LankaConnect.Domain.Tests/Events/ValueObjects/EventLocationTests.cs
public class EventLocationTests
{
    [Fact]
    public void Create_ValidAddress_ReturnsSuccess()
    {
        // Arrange
        var address = Address.Create("123 Main", "Colombo", "Western", "10100", "Sri Lanka").Value;

        // Act
        var result = EventLocation.Create(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(address);
        result.Value.Coordinates.Should().BeNull();
    }

    [Fact]
    public void WithCoordinates_ValidCoordinates_UpdatesLocation()
    {
        // Arrange
        var address = Address.Create("123 Main", "Colombo", "Western", "10100", "Sri Lanka").Value;
        var location = EventLocation.Create(address).Value;
        var coords = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        // Act
        var result = location.WithCoordinates(coords);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Coordinates.Should().Be(coords);
    }
}
```

### Integration Tests (Repository)
```csharp
// tests/LankaConnect.IntegrationTests/Repositories/EventRepositoryLocationTests.cs
public class EventRepositoryLocationTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task GetEventsWithinRadiusAsync_ReturnsEventsInRadius()
    {
        // Arrange
        var address1 = Address.Create("100 Main", "Colombo", "Western", "10100", "Sri Lanka").Value;
        var coords1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value; // Colombo Fort
        var location1 = EventLocation.Create(address1, coords1).Value;

        var address2 = Address.Create("200 Galle Rd", "Dehiwala", "Western", "10350", "Sri Lanka").Value;
        var coords2 = GeoCoordinate.Create(6.8570m, 79.8650m).Value; // ~8km from Colombo
        var location2 = EventLocation.Create(address2, coords2).Value;

        var event1 = CreateTestEvent("Event 1", location1);
        var event2 = CreateTestEvent("Event 2", location2);

        await _repository.AddAsync(event1);
        await _repository.AddAsync(event2);
        await _unitOfWork.SaveChangesAsync();

        var searchPoint = GeoCoordinate.Create(6.9271m, 79.8612m).Value; // Colombo Fort

        // Act
        var results = await _repository.GetEventsWithinRadiusAsync(searchPoint, radiusMiles: 10);

        // Assert
        results.Should().HaveCount(2);
        results[0].Event.Id.Should().Be(event1.Id); // Nearest first
        results[0].DistanceMiles.Should().BeLessThan(1); // Almost at search point
        results[1].DistanceMiles.Should().BeApproximately(5, precision: 1); // ~8km = ~5 miles
    }

    [Fact]
    public async Task GetEventsByCityAsync_FiltersByCity()
    {
        // Arrange
        var colomboEvent = CreateEventInCity("Colombo");
        var kandyEvent = CreateEventInCity("Kandy");

        await _repository.AddAsync(colomboEvent);
        await _repository.AddAsync(kandyEvent);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var results = await _repository.GetEventsByCityAsync("Colombo");

        // Assert
        results.Should().ContainSingle();
        results[0].Location.Address.City.Should().Be("Colombo");
    }
}
```

---

## 6. Potential Pitfalls and Mitigations

### Pitfall 1: Coordinate Order Confusion
**Problem:** PostGIS uses (longitude, latitude), opposite of common (lat, lon) convention.
**Mitigation:** Always use helper method `CreatePoint()` that explicitly documents parameter order.

### Pitfall 2: Missing Geocoding
**Problem:** Events without coordinates cannot be searched spatially.
**Mitigation:**
- Make coordinates optional in EventLocation
- Add `HasCoordinates()` query to filter geocoded events
- Implement background geocoding service (future enhancement)

### Pitfall 3: Performance Degradation with Mixed Queries
**Problem:** Combining spatial + non-spatial filters can bypass indexes.
**Mitigation:**
- Use partial indexes with filters matching WHERE clauses
- Test query plans with EXPLAIN ANALYZE
- Consider materialized views for complex queries

### Pitfall 4: SRID Mismatch
**Problem:** Mixing different SRIDs causes query errors.
**Mitigation:** Always use SRID 4326 (WGS84) for consistency.

### Pitfall 5: Decimal Precision Loss
**Problem:** Converting decimal to double for NTS Point.
**Mitigation:** Use DECIMAL(10,7) in database (supports ±1.1cm precision), acceptable precision loss for meters/miles.

---

## 7. Future Enhancements

1. **Geocoding Service:** Auto-populate coordinates from addresses via Google Maps/Nominatim API
2. **Geofencing:** Notify users when events are created near their location
3. **Polygon Boundaries:** Support region-based searches (e.g., "all events in Western Province")
4. **Timezone Support:** Automatically detect timezone from coordinates
5. **Map Clustering:** Aggregate nearby events for map display
6. **Route Distance:** Use road network distance instead of straight-line (via PostGIS pgRouting)

---

## 8. Implementation Checklist

- [ ] Install NuGet packages (NetTopologySuite, Npgsql.NTS)
- [ ] Create EventLocation value object in Domain layer
- [ ] Update Event aggregate with Location property
- [ ] Create EF Core migration with PostGIS setup
- [ ] Configure EventConfiguration with OwnsOne for Location
- [ ] Update IEventRepository interface with spatial methods
- [ ] Implement spatial queries in EventRepository
- [ ] Write unit tests for EventLocation
- [ ] Write integration tests for spatial queries
- [ ] Test migration on dev database
- [ ] Document API endpoints that use location features
- [ ] Update Application layer DTOs to include location
- [ ] Add validation for coordinate bounds

---

## 9. References

- [PostGIS Documentation](https://postgis.net/documentation/)
- [NetTopologySuite GitHub](https://github.com/NetTopologySuite/NetTopologySuite)
- [EF Core Spatial Data](https://learn.microsoft.com/en-us/ef/core/modeling/spatial)
- [WGS84 Coordinate System (SRID 4326)](https://epsg.io/4326)
- [PostGIS Distance vs Haversine Performance](https://blog.rustprooflabs.com/2020/12/postgis-spatial-index)

---

## 10. Decision Summary

| Decision | Rationale | Alternative Rejected |
|----------|-----------|---------------------|
| Reuse Address + GeoCoordinate via EventLocation | DRY, encapsulation, ubiquitous language | Direct properties on Event |
| Dual storage (domain + PostGIS) | Clean Architecture + performance | PostGIS only |
| NetTopologySuite for EF Core | Official Npgsql integration | Custom SQL mappings |
| Computed column for PostGIS | Automatic synchronization | Manually maintained column |
| GIST indexes | PostGIS-optimized spatial queries | B-tree indexes |
| Optional location on Event | Not all events have physical location | Required location |

---

**Approved By:** [Pending Review]
**Implementation Priority:** High (Core feature for event discovery)
**Estimated Effort:** 3-5 days (including tests)
