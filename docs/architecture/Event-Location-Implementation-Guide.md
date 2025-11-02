# Event Location Implementation Guide

**Quick Start Guide for Implementing PostGIS Location Features**

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER (API)                      │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ GET /api/events/search/nearby?lat=6.9&lon=79.8&radius=25       │ │
│  │ GET /api/events/search/city?city=Colombo                       │ │
│  └────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER (Use Cases)                   │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ SearchEventsNearLocationQuery                                  │ │
│  │ - Validates coordinates                                        │ │
│  │ - Calls repository                                             │ │
│  │ - Maps to DTOs                                                 │ │
│  └────────────────────────────────────────────────────────────────┘ │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER (Core)                          │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐  │
│  │  Event Aggregate │  │  EventLocation   │  │  GeoCoordinate   │  │
│  │  ┌────────────┐  │  │  - Address       │  │  - Latitude      │  │
│  │  │ Location?  │──┼─►│  - Coordinates?  │──┼─►│  - Longitude     │  │
│  │  └────────────┘  │  │                  │  │  - DistanceTo()  │  │
│  │  - SetLocation() │  │  - Create()      │  │  - Create()      │  │
│  │  - HasCoords()   │  │  - WithCoords()  │  │  (REUSED!)       │  │
│  └──────────────────┘  └──────────────────┘  └──────────────────┘  │
│                                                                       │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │ IEventRepository Interface                                   │   │
│  │ - GetEventsWithinRadiusAsync(point, miles)                   │   │
│  │ - GetEventsByCityAsync(city)                                 │   │
│  │ - GetNearestEventsAsync(point, maxResults)                   │   │
│  └──────────────────────────────────────────────────────────────┘   │
└───────────────────────────────┬─────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER (Data)                        │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ EventRepository (EF Core + PostGIS)                            │ │
│  │ - Uses NetTopologySuite for spatial queries                   │ │
│  │ - EF.Functions.Distance() for PostGIS ST_Distance             │ │
│  │ - GIST indexes for performance                                │ │
│  └────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │ PostgreSQL Database (events.events table)                      │ │
│  │ ┌────────────────────────────────────────────────────────────┐ │ │
│  │ │ Domain Columns          │ PostGIS Column (Computed)        │ │ │
│  │ │ - address_street        │ location GEOGRAPHY(Point, 4326)  │ │ │
│  │ │ - address_city          │   ▲                              │ │ │
│  │ │ - address_state         │   │ Generated from:              │ │ │
│  │ │ - address_zip_code      │   │ - coordinates_latitude       │ │ │
│  │ │ - address_country       │   │ - coordinates_longitude      │ │ │
│  │ │ - coordinates_latitude  │   │                              │ │ │
│  │ │ - coordinates_longitude │◄──┘                              │ │ │
│  │ └────────────────────────────────────────────────────────────┘ │ │
│  │                                                                  │ │
│  │ Indexes:                                                         │ │
│  │ - GIST(location) ← PostGIS spatial index                        │ │
│  │ - BTREE(address_city) ← City searches                           │ │
│  │ - BTREE(status, start_date, city) ← Combined queries            │ │
│  └────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Step-by-Step Implementation

### PHASE 1: Domain Layer (Pure Business Logic)

#### Step 1.1: Create EventLocation Value Object

**File:** `src/LankaConnect.Domain/Events/ValueObjects/EventLocation.cs`

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Represents the physical location of an event
/// Composes Address (required) and GeoCoordinate (optional until geocoded)
/// </summary>
public class EventLocation : ValueObject
{
    public Address Address { get; }
    public GeoCoordinate? Coordinates { get; }

    private EventLocation(Address address, GeoCoordinate? coordinates)
    {
        Address = address;
        Coordinates = coordinates;
    }

    /// <summary>
    /// Creates a new event location with address and optional coordinates
    /// </summary>
    public static Result<EventLocation> Create(Address address, GeoCoordinate? coordinates = null)
    {
        if (address == null)
            return Result<EventLocation>.Failure("Address is required for event location");

        return Result<EventLocation>.Success(new EventLocation(address, coordinates));
    }

    /// <summary>
    /// Returns a new EventLocation with updated coordinates (for geocoding scenarios)
    /// </summary>
    public Result<EventLocation> WithCoordinates(GeoCoordinate coordinates)
    {
        if (coordinates == null)
            return Result<EventLocation>.Failure("Coordinates cannot be null");

        return Result<EventLocation>.Success(new EventLocation(Address, coordinates));
    }

    /// <summary>
    /// Checks if this location has been geocoded
    /// </summary>
    public bool HasCoordinates() => Coordinates != null;

    /// <summary>
    /// Calculates distance to another location (in kilometers)
    /// </summary>
    public Result<double> DistanceTo(EventLocation other)
    {
        if (!HasCoordinates() || !other.HasCoordinates())
            return Result<double>.Failure("Both locations must have coordinates to calculate distance");

        var distance = Coordinates!.DistanceTo(other.Coordinates!);
        return Result<double>.Success(distance);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address;
        if (Coordinates != null)
            yield return Coordinates;
    }

    public override string ToString()
    {
        var coordsString = Coordinates != null ? $" ({Coordinates})" : " (not geocoded)";
        return $"{Address}{coordsString}";
    }
}
```

**Tests:** `tests/LankaConnect.Domain.Tests/Events/ValueObjects/EventLocationTests.cs`

```csharp
using FluentAssertions;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

public class EventLocationTests
{
    [Fact]
    public void Create_WithValidAddress_ReturnsSuccess()
    {
        // Arrange
        var address = CreateValidAddress();

        // Act
        var result = EventLocation.Create(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(address);
        result.Value.Coordinates.Should().BeNull();
        result.Value.HasCoordinates().Should().BeFalse();
    }

    [Fact]
    public void Create_WithAddressAndCoordinates_ReturnsSuccess()
    {
        // Arrange
        var address = CreateValidAddress();
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        // Act
        var result = EventLocation.Create(address, coordinates);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address.Should().Be(address);
        result.Value.Coordinates.Should().Be(coordinates);
        result.Value.HasCoordinates().Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullAddress_ReturnsFailure()
    {
        // Act
        var result = EventLocation.Create(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Address is required for event location");
    }

    [Fact]
    public void WithCoordinates_AddsCoordinatesToLocation()
    {
        // Arrange
        var address = CreateValidAddress();
        var location = EventLocation.Create(address).Value;
        var coordinates = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        // Act
        var result = location.WithCoordinates(coordinates);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Coordinates.Should().Be(coordinates);
        result.Value.Address.Should().Be(address); // Address unchanged
    }

    [Fact]
    public void DistanceTo_BothLocationsHaveCoordinates_CalculatesDistance()
    {
        // Arrange
        var location1 = CreateLocationWithCoordinates(6.9271m, 79.8612m); // Colombo Fort
        var location2 = CreateLocationWithCoordinates(6.8570m, 79.8650m); // Dehiwala (~8km away)

        // Act
        var result = location1.DistanceTo(location2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeApproximately(8.0, 1.0); // ~8km ± 1km
    }

    [Fact]
    public void DistanceTo_LocationsMissingCoordinates_ReturnsFailure()
    {
        // Arrange
        var location1 = CreateLocationWithCoordinates(6.9271m, 79.8612m);
        var location2 = EventLocation.Create(CreateValidAddress()).Value; // No coordinates

        // Act
        var result = location1.DistanceTo(location2);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Both locations must have coordinates");
    }

    [Fact]
    public void Equals_SameAddressAndCoordinates_ReturnsTrue()
    {
        // Arrange
        var address = CreateValidAddress();
        var coords = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var location1 = EventLocation.Create(address, coords).Value;
        var location2 = EventLocation.Create(address, coords).Value;

        // Act & Assert
        location1.Should().Be(location2);
    }

    [Fact]
    public void Equals_DifferentCoordinates_ReturnsFalse()
    {
        // Arrange
        var address = CreateValidAddress();
        var coords1 = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
        var coords2 = GeoCoordinate.Create(6.8570m, 79.8650m).Value;
        var location1 = EventLocation.Create(address, coords1).Value;
        var location2 = EventLocation.Create(address, coords2).Value;

        // Act & Assert
        location1.Should().NotBe(location2);
    }

    // Helper methods
    private static Address CreateValidAddress()
    {
        return Address.Create(
            "123 Main Street",
            "Colombo",
            "Western Province",
            "10100",
            "Sri Lanka"
        ).Value;
    }

    private static EventLocation CreateLocationWithCoordinates(decimal lat, decimal lon)
    {
        var address = CreateValidAddress();
        var coords = GeoCoordinate.Create(lat, lon).Value;
        return EventLocation.Create(address, coords).Value;
    }
}
```

---

#### Step 1.2: Update Event Aggregate

**File:** `src/LankaConnect.Domain/Events/Event.cs` (ADD these members)

```csharp
// Add property
public EventLocation? Location { get; private set; }

// Update Create factory method signature
public static Result<Event> Create(
    EventTitle title,
    EventDescription description,
    DateTime startDate,
    DateTime endDate,
    Guid organizerId,
    int capacity,
    EventLocation? location = null) // NEW PARAMETER
{
    // ... existing validations ...

    var @event = new Event(title, description, startDate, endDate, organizerId, capacity)
    {
        Location = location // SET LOCATION
    };

    return Result<Event>.Success(@event);
}

// Add behavior methods
public Result SetLocation(EventLocation location)
{
    if (location == null)
        return Result.Failure("Location cannot be null");

    Location = location;
    MarkAsUpdated();

    // Optional: Raise domain event for geocoding triggers
    // RaiseDomainEvent(new EventLocationUpdatedEvent(Id, location, DateTime.UtcNow));

    return Result.Success();
}

public bool HasCoordinates() => Location?.Coordinates != null;

public Result<double> DistanceToEvent(Event otherEvent)
{
    if (Location == null || otherEvent.Location == null)
        return Result<double>.Failure("Both events must have locations");

    return Location.DistanceTo(otherEvent.Location);
}
```

---

#### Step 1.3: Update IEventRepository Interface

**File:** `src/LankaConnect.Domain/Events/IEventRepository.cs`

```csharp
// Add these method signatures
public interface IEventRepository : IRepository<Event>
{
    // ... existing methods ...

    /// <summary>
    /// Finds events within a specified radius of coordinates
    /// Uses PostGIS spatial indexes for high performance
    /// </summary>
    /// <param name="centerPoint">Search origin coordinates</param>
    /// <param name="radiusMiles">Search radius in miles (25, 50, 100 common values)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Events within radius, ordered by distance (nearest first) with calculated distances</returns>
    Task<IReadOnlyList<EventWithDistance>> GetEventsWithinRadiusAsync(
        GeoCoordinate centerPoint,
        double radiusMiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds published events in a specific city
    /// </summary>
    /// <param name="city">City name (case-insensitive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Events in the specified city, ordered by start date</returns>
    Task<IReadOnlyList<Event>> GetEventsByCityAsync(
        string city,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the nearest N events to a point
    /// </summary>
    /// <param name="centerPoint">Search origin coordinates</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Nearest events, ordered by distance</returns>
    Task<IReadOnlyList<EventWithDistance>> GetNearestEventsAsync(
        GeoCoordinate centerPoint,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for repository query results that include distance calculations
/// </summary>
public record EventWithDistance(Event Event, double DistanceMiles);
```

---

### PHASE 2: Infrastructure Layer (Data Access)

#### Step 2.1: Install NuGet Packages

**File:** `src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj`

```bash
# Run these commands in the Infrastructure project directory
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite --version 8.0.0
dotnet add package NetTopologySuite --version 2.5.0
```

**Expected packages in .csproj:**
```xml
<ItemGroup>
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite" Version="8.0.0" />
  <PackageReference Include="NetTopologySuite" Version="2.5.0" />
</ItemGroup>
```

---

#### Step 2.2: Enable PostGIS in AppDbContext

**File:** `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

```csharp
// Add this to the existing OnConfiguring method (or add method if not present)
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);

    // Enable PostGIS support via NetTopologySuite
    if (optionsBuilder.IsConfigured)
    {
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
        );
    }
}
```

**Note:** If connection string is configured externally (DependencyInjection.cs), update there:

**File:** `src/LankaConnect.Infrastructure/DependencyInjection.cs`

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite() // ADD THIS
    )
);
```

---

#### Step 2.3: Configure EventLocation in EF Core

**File:** `src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // ... KEEP existing configuration ...

        // ADD EventLocation configuration
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
                    .HasPrecision(10, 7); // 7 decimal places = ~1.1cm precision

                coordinates.Property(c => c.Longitude)
                    .HasColumnName("coordinates_longitude")
                    .HasPrecision(10, 7);
            });
        });

        // ADD City index (for city-based searches)
        builder.HasIndex(e => e.Location.Address.City)
            .HasDatabaseName("ix_events_city")
            .HasFilter("status = 'Published' AND start_date > NOW()");
    }
}
```

---

#### Step 2.4: Create Migration with PostGIS

```bash
# Create migration
dotnet ef migrations add AddEventLocation --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API

# IMPORTANT: Edit the generated migration file before applying
```

**File:** `src/LankaConnect.Infrastructure/Migrations/YYYYMMDDHHMMSS_AddEventLocation.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddEventLocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Enable PostGIS extension (idempotent - safe to run multiple times)
        migrationBuilder.Sql(@"
            CREATE EXTENSION IF NOT EXISTS postgis;
            CREATE EXTENSION IF NOT EXISTS postgis_topology;
        ");

        // 2. Add location columns (nullable for existing events)
        migrationBuilder.AddColumn<string>(
            name: "address_street",
            schema: "events",
            table: "events",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "address_city",
            schema: "events",
            table: "events",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "address_state",
            schema: "events",
            table: "events",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "address_zip_code",
            schema: "events",
            table: "events",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "address_country",
            schema: "events",
            table: "events",
            type: "character varying(100)",
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

        // 4. Create PostGIS spatial index (GIST)
        migrationBuilder.Sql(@"
            CREATE INDEX ix_events_location_gist
            ON events.events
            USING GIST (location)
            WHERE location IS NOT NULL;
        ");

        // 5. Create city index (for city-based searches)
        migrationBuilder.CreateIndex(
            name: "ix_events_city",
            schema: "events",
            table: "events",
            column: "address_city",
            filter: "status = 'Published' AND start_date > NOW()");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop indexes first
        migrationBuilder.Sql("DROP INDEX IF EXISTS events.ix_events_location_gist;");
        migrationBuilder.DropIndex(name: "ix_events_city", schema: "events", table: "events");

        // Drop PostGIS computed column
        migrationBuilder.Sql("ALTER TABLE events.events DROP COLUMN IF EXISTS location;");

        // Drop domain columns
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

```bash
# Apply migration to database
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
```

---

#### Step 2.5: Implement Spatial Queries in Repository

**File:** `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Business.ValueObjects;
using NetTopologySuite.Geometries;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class EventRepository : Repository<Event>, IEventRepository
{
    private const double METERS_PER_MILE = 1609.344;
    private const int WGS84_SRID = 4326; // Standard GPS coordinate system

    public EventRepository(AppDbContext context) : base(context)
    {
    }

    // ... KEEP existing methods ...

    public async Task<IReadOnlyList<EventWithDistance>> GetEventsWithinRadiusAsync(
        GeoCoordinate centerPoint,
        double radiusMiles,
        CancellationToken cancellationToken = default)
    {
        var radiusMeters = radiusMiles * METERS_PER_MILE;
        var searchPoint = CreatePoint(centerPoint);

        // PostGIS spatial query
        var results = await _context.Events
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published &&
                       e.StartDate > DateTime.UtcNow &&
                       e.Location != null &&
                       e.Location.Coordinates != null)
            .Select(e => new
            {
                Event = e,
                // PostGIS ST_Distance in meters
                DistanceMeters = EF.Functions.Distance(
                    searchPoint,
                    CreatePoint(
                        (double)e.Location.Coordinates.Latitude,
                        (double)e.Location.Coordinates.Longitude
                    )
                )
            })
            .Where(x => x.DistanceMeters <= radiusMeters)
            .OrderBy(x => x.DistanceMeters)
            .ToListAsync(cancellationToken);

        // Convert to domain DTOs
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

        var normalizedCity = city.Trim().ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published &&
                       e.StartDate > DateTime.UtcNow &&
                       e.Location != null &&
                       e.Location.Address.City.ToLower() == normalizedCity)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventWithDistance>> GetNearestEventsAsync(
        GeoCoordinate centerPoint,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var searchPoint = CreatePoint(centerPoint);

        var results = await _context.Events
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published &&
                       e.StartDate > DateTime.UtcNow &&
                       e.Location != null &&
                       e.Location.Coordinates != null)
            .Select(e => new
            {
                Event = e,
                DistanceMeters = EF.Functions.Distance(
                    searchPoint,
                    CreatePoint(
                        (double)e.Location.Coordinates.Latitude,
                        (double)e.Location.Coordinates.Longitude
                    )
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

    /// <summary>
    /// Creates a NetTopologySuite Point from domain GeoCoordinate
    /// CRITICAL: PostGIS uses (longitude, latitude) order - REVERSE of common convention!
    /// </summary>
    private static Point CreatePoint(GeoCoordinate coordinate)
    {
        return CreatePoint((double)coordinate.Latitude, (double)coordinate.Longitude);
    }

    private static Point CreatePoint(double latitude, double longitude)
    {
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance
            .CreateGeometryFactory(srid: WGS84_SRID);

        // CRITICAL: PostGIS expects (longitude, latitude) - NOT (latitude, longitude)!
        return geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
    }
}
```

---

### PHASE 3: Testing

#### Integration Tests

**File:** `tests/LankaConnect.IntegrationTests/Repositories/EventRepositoryLocationTests.cs`

```csharp
using FluentAssertions;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Infrastructure.Data.Repositories;
using Xunit;

namespace LankaConnect.IntegrationTests.Repositories;

public class EventRepositoryLocationTests : IClassFixture<DatabaseFixture>
{
    private readonly EventRepository _repository;
    private readonly AppDbContext _context;

    public EventRepositoryLocationTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _repository = new EventRepository(_context);
    }

    [Fact]
    public async Task GetEventsWithinRadiusAsync_FindsEventsInRadius()
    {
        // Arrange: Create events at known coordinates
        var colomboEvent = CreateEventAtLocation(
            "Colombo Event",
            6.9271m, 79.8612m, // Colombo Fort
            "Colombo"
        );

        var dehiwalaEvent = CreateEventAtLocation(
            "Dehiwala Event",
            6.8570m, 79.8650m, // ~8km from Colombo
            "Dehiwala"
        );

        var kandyEvent = CreateEventAtLocation(
            "Kandy Event",
            7.2906m, 80.6337m, // ~100km from Colombo
            "Kandy"
        );

        await _repository.AddAsync(colomboEvent);
        await _repository.AddAsync(dehiwalaEvent);
        await _repository.AddAsync(kandyEvent);
        await _context.SaveChangesAsync();

        var searchPoint = GeoCoordinate.Create(6.9271m, 79.8612m).Value; // Colombo Fort

        // Act: Search within 25 miles
        var results = await _repository.GetEventsWithinRadiusAsync(searchPoint, radiusMiles: 25);

        // Assert
        results.Should().HaveCount(2); // Colombo + Dehiwala (Kandy is ~62 miles away)
        results[0].Event.Title.Value.Should().Contain("Colombo"); // Nearest first
        results[0].DistanceMiles.Should().BeLessThan(1); // Almost at search point
        results[1].Event.Title.Value.Should().Contain("Dehiwala");
        results[1].DistanceMiles.Should().BeApproximately(5, precision: 1); // ~8km ≈ 5 miles
    }

    [Fact]
    public async Task GetEventsByCityAsync_FiltersByCityName()
    {
        // Arrange
        var colomboEvent = CreateEventAtLocation("Event 1", 6.9271m, 79.8612m, "Colombo");
        var kandyEvent = CreateEventAtLocation("Event 2", 7.2906m, 80.6337m, "Kandy");

        await _repository.AddAsync(colomboEvent);
        await _repository.AddAsync(kandyEvent);
        await _context.SaveChangesAsync();

        // Act
        var results = await _repository.GetEventsByCityAsync("Colombo");

        // Assert
        results.Should().ContainSingle();
        results[0].Location.Address.City.Should().Be("Colombo");
    }

    [Fact]
    public async Task GetNearestEventsAsync_ReturnsTopNNearest()
    {
        // Arrange: Create 5 events at varying distances
        var events = new[]
        {
            CreateEventAtLocation("1km away", 6.9180m, 79.8612m, "Colombo"),
            CreateEventAtLocation("5km away", 6.8800m, 79.8612m, "Colombo"),
            CreateEventAtLocation("10km away", 6.8400m, 79.8612m, "Dehiwala"),
            CreateEventAtLocation("20km away", 6.7600m, 79.8612m, "Panadura"),
            CreateEventAtLocation("30km away", 6.6800m, 79.8612m, "Kalutara")
        };

        foreach (var evt in events)
        {
            await _repository.AddAsync(evt);
        }
        await _context.SaveChangesAsync();

        var searchPoint = GeoCoordinate.Create(6.9271m, 79.8612m).Value;

        // Act: Get nearest 3
        var results = await _repository.GetNearestEventsAsync(searchPoint, maxResults: 3);

        // Assert
        results.Should().HaveCount(3);
        results[0].DistanceMiles.Should().BeLessThan(results[1].DistanceMiles);
        results[1].DistanceMiles.Should().BeLessThan(results[2].DistanceMiles);
        results[0].Event.Title.Value.Should().Contain("1km");
    }

    // Helper: Create event with location
    private Event CreateEventAtLocation(string title, decimal lat, decimal lon, string city)
    {
        var address = Address.Create(
            "123 Main Street",
            city,
            "Western Province",
            "10100",
            "Sri Lanka"
        ).Value;

        var coordinates = GeoCoordinate.Create(lat, lon).Value;
        var location = EventLocation.Create(address, coordinates).Value;

        var eventTitle = EventTitle.Create(title).Value;
        var description = EventDescription.Create("Test event").Value;

        var evt = Event.Create(
            eventTitle,
            description,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(8),
            Guid.NewGuid(),
            100,
            location
        ).Value;

        evt.Publish(); // Make it searchable

        return evt;
    }
}
```

---

## Performance Validation

### Test Query Performance

```sql
-- Test spatial index usage
EXPLAIN ANALYZE
SELECT
    id,
    title,
    ST_Distance(
        location,
        ST_SetSRID(ST_MakePoint(79.8612, 6.9271), 4326)::geography
    ) / 1609.344 AS distance_miles
FROM events.events
WHERE location IS NOT NULL
    AND status = 'Published'
    AND start_date > NOW()
    AND ST_DWithin(
        location,
        ST_SetSRID(ST_MakePoint(79.8612, 6.9271), 4326)::geography,
        40233.6 -- 25 miles in meters
    )
ORDER BY location <-> ST_SetSRID(ST_MakePoint(79.8612, 6.9271), 4326)::geography
LIMIT 50;

-- Expected output should show:
-- "Index Scan using ix_events_location_gist on events"
-- Execution time: < 10ms (with proper indexes)
```

---

## Common Pitfalls

### 1. Coordinate Order
**WRONG:**
```csharp
CreatePoint(latitude, longitude) // Common convention
```

**CORRECT:**
```csharp
CreatePoint(longitude, latitude) // PostGIS requirement!
```

### 2. Missing SRID
**WRONG:**
```csharp
var point = geometryFactory.CreatePoint(new Coordinate(lon, lat)); // No SRID
```

**CORRECT:**
```csharp
var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
var point = factory.CreatePoint(new Coordinate(lon, lat));
```

### 3. Null Coordinates
Always check for null coordinates before spatial queries:
```csharp
.Where(e => e.Location != null && e.Location.Coordinates != null)
```

---

## Summary Checklist

- [ ] EventLocation value object created
- [ ] Event aggregate updated with Location property
- [ ] IEventRepository interface extended
- [ ] NuGet packages installed
- [ ] AppDbContext configured for PostGIS
- [ ] EventConfiguration updated
- [ ] Migration created and applied
- [ ] EventRepository spatial methods implemented
- [ ] Unit tests for EventLocation written
- [ ] Integration tests for spatial queries written
- [ ] Query performance validated (EXPLAIN ANALYZE)

---

**Next Steps:**
1. Application Layer: Create query handlers (SearchEventsNearLocationQuery)
2. Presentation Layer: Create API endpoints (GET /api/events/search/nearby)
3. Future: Implement geocoding service for auto-populating coordinates
