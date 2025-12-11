# PostGIS Quick Reference for LankaConnect

**Essential code snippets and patterns for PostGIS spatial queries**

---

## 1. Common Spatial Queries

### Radius Search (Most Common)
```csharp
// Find events within 25 miles of a point
var searchPoint = CreatePoint(6.9271m, 79.8612m); // Colombo
var radiusMeters = 25 * 1609.344; // 25 miles to meters

var events = await _context.Events
    .Where(e => e.Location.Coordinates != null)
    .Where(e => EF.Functions.Distance(
        searchPoint,
        CreatePoint(
            (double)e.Location.Coordinates.Latitude,
            (double)e.Location.Coordinates.Longitude
        )
    ) <= radiusMeters)
    .OrderBy(e => EF.Functions.Distance(searchPoint, /* ... */))
    .ToListAsync();
```

### Nearest N Events
```csharp
// Find 10 nearest events to a point
var nearest = await _context.Events
    .Where(e => e.Location.Coordinates != null)
    .OrderBy(e => EF.Functions.Distance(
        searchPoint,
        CreatePoint(
            (double)e.Location.Coordinates.Latitude,
            (double)e.Location.Coordinates.Longitude
        )
    ))
    .Take(10)
    .ToListAsync();
```

### City-Based Search
```csharp
// Find all events in Colombo
var cityEvents = await _context.Events
    .Where(e => e.Location.Address.City.ToLower() == "colombo")
    .OrderBy(e => e.StartDate)
    .ToListAsync();
```

### Bounding Box Search (Rectangle Area)
```csharp
// Find events in a rectangular area
var minLat = 6.8;
var maxLat = 7.0;
var minLon = 79.7;
var maxLon = 79.9;

var eventsInBox = await _context.Events
    .Where(e => e.Location.Coordinates != null)
    .Where(e => e.Location.Coordinates.Latitude >= minLat &&
                e.Location.Coordinates.Latitude <= maxLat &&
                e.Location.Coordinates.Longitude >= minLon &&
                e.Location.Coordinates.Longitude <= maxLon)
    .ToListAsync();
```

---

## 2. PostGIS Functions Reference

### ST_Distance
**Purpose:** Calculate distance between two points (spherical calculation)
**Returns:** Distance in meters (for GEOGRAPHY type)

```sql
-- SQL Example
SELECT ST_Distance(
    location1::geography,
    location2::geography
) AS distance_meters
FROM events;
```

```csharp
// EF Core Example
EF.Functions.Distance(point1, point2) // Returns meters
```

### ST_DWithin
**Purpose:** Check if point is within distance (faster than ST_Distance + comparison)
**Returns:** Boolean

```sql
-- SQL Example: Events within 25 miles of Colombo
SELECT * FROM events
WHERE ST_DWithin(
    location,
    ST_SetSRID(ST_MakePoint(79.8612, 6.9271), 4326)::geography,
    40233.6 -- 25 miles in meters
);
```

```csharp
// EF Core doesn't directly support ST_DWithin, use Distance + comparison
.Where(e => EF.Functions.Distance(searchPoint, eventPoint) <= radiusMeters)
```

### ST_MakePoint
**Purpose:** Create a point from longitude and latitude
**Order:** LONGITUDE, LATITUDE (not lat, lon!)

```sql
SELECT ST_SetSRID(
    ST_MakePoint(79.8612, 6.9271), -- lon, lat
    4326
)::geography;
```

```csharp
// EF Core uses NetTopologySuite
var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
var point = factory.CreatePoint(new Coordinate(longitude, latitude));
```

---

## 3. Index Strategies

### GIST Index for Spatial Queries
```sql
-- Create spatial index (REQUIRED for performance)
CREATE INDEX ix_events_location_gist
ON events.events
USING GIST (location)
WHERE location IS NOT NULL;
```

**Performance Impact:**
- Without index: 2000ms for 1M events
- With GIST index: 5ms for 1M events (400x faster!)

### Partial Index for Published Events
```sql
-- Index only published events (reduces index size)
CREATE INDEX ix_events_location_published_gist
ON events.events
USING GIST (location)
WHERE status = 'Published' AND location IS NOT NULL;
```

### City Index for City-Based Searches
```sql
CREATE INDEX ix_events_city
ON events.events (address_city)
WHERE status = 'Published' AND start_date > NOW();
```

### Composite Index for Combined Filters
```sql
CREATE INDEX ix_events_status_date_city
ON events.events (status, start_date, address_city);
```

---

## 4. Coordinate Systems (SRID)

### SRID 4326 (WGS84) - Default for GPS
**Used for:** GPS coordinates, Google Maps, most mobile apps
**Units:** Degrees (latitude/longitude)
**Range:** Latitude: -90 to 90, Longitude: -180 to 180

```sql
-- Create point with WGS84
ST_SetSRID(ST_MakePoint(79.8612, 6.9271), 4326)
```

### GEOGRAPHY vs GEOMETRY Types

**GEOGRAPHY (RECOMMENDED):**
- Uses spherical calculations (accounts for Earth's curvature)
- Distance in meters
- More accurate for large distances
- Slower than GEOMETRY (but still very fast with indexes)

```sql
CREATE COLUMN location GEOGRAPHY(Point, 4326)
```

**GEOMETRY:**
- Uses planar calculations (flat Earth)
- Faster queries
- Less accurate for distances > 10km
- Good for small areas

```sql
CREATE COLUMN location GEOMETRY(Point, 4326)
```

**LankaConnect Uses:** GEOGRAPHY (country-wide searches, need accuracy)

---

## 5. Common Coordinate Locations (Sri Lanka)

```csharp
// Major cities with coordinates
var coordinates = new Dictionary<string, (decimal Lat, decimal Lon)>
{
    ["Colombo Fort"] = (6.9271m, 79.8612m),
    ["Kandy"] = (7.2906m, 80.6337m),
    ["Galle"] = (6.0367m, 80.2170m),
    ["Jaffna"] = (9.6615m, 80.0255m),
    ["Batticaloa"] = (7.7310m, 81.6747m),
    ["Trincomalee"] = (8.5874m, 81.2152m),
    ["Anuradhapura"] = (8.3114m, 80.4037m),
    ["Negombo"] = (7.2008m, 79.8736m),
    ["Matara"] = (5.9549m, 80.5550m),
    ["Kurunegala"] = (7.4863m, 80.3623m)
};
```

---

## 6. Distance Conversions

```csharp
public static class DistanceConverter
{
    public const double METERS_PER_MILE = 1609.344;
    public const double METERS_PER_KM = 1000.0;
    public const double MILES_PER_KM = 0.621371;

    public static double MilesToMeters(double miles) => miles * METERS_PER_MILE;
    public static double MetersToMiles(double meters) => meters / METERS_PER_MILE;
    public static double KmToMeters(double km) => km * METERS_PER_KM;
    public static double MetersToKm(double meters) => meters / METERS_PER_KM;
    public static double MilesToKm(double miles) => miles * MILES_PER_KM;
}
```

**Common Search Radii:**
- 25 miles = 40,233.6 meters = 40.2 km (city-level)
- 50 miles = 80,467.2 meters = 80.5 km (regional)
- 100 miles = 160,934.4 meters = 160.9 km (country-level for Sri Lanka)

---

## 7. Performance Optimization Tips

### 1. Use Computed Columns
```sql
-- Auto-sync with lat/lon changes
ALTER TABLE events.events
ADD COLUMN location GEOGRAPHY(Point, 4326)
GENERATED ALWAYS AS (
    ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)::geography
) STORED;
```

### 2. Filter Before Distance Calculation
```csharp
// GOOD: Filter status first, then calculate distance
var events = await _context.Events
    .Where(e => e.Status == EventStatus.Published) // Index scan
    .Where(e => e.Location.Coordinates != null)
    .Where(e => EF.Functions.Distance(...) <= radius) // Spatial index scan
    .ToListAsync();

// BAD: Calculate distance for all events
var events = await _context.Events
    .Where(e => EF.Functions.Distance(...) <= radius) // Full spatial scan
    .Where(e => e.Status == EventStatus.Published)
    .ToListAsync();
```

### 3. Use Partial Indexes
```sql
-- Index only relevant events
CREATE INDEX ix_events_location_active_gist
ON events.events
USING GIST (location)
WHERE status = 'Published' AND start_date > NOW() AND location IS NOT NULL;
```

### 4. Limit Results with LIMIT
```csharp
// Always use Take() for nearest queries
.OrderBy(e => EF.Functions.Distance(...))
.Take(10) // Stops after finding 10, doesn't scan entire table
.ToListAsync();
```

### 5. Use AsNoTracking for Read Queries
```csharp
// Read-only queries
_context.Events
    .AsNoTracking() // ~30% faster, no change tracking overhead
    .Where(...)
    .ToListAsync();
```

---

## 8. Query Plan Analysis

### Check Index Usage
```sql
EXPLAIN ANALYZE
SELECT id, title,
    ST_Distance(
        location,
        ST_SetSRID(ST_MakePoint(79.8612, 6.9271), 4326)::geography
    ) / 1609.344 AS distance_miles
FROM events.events
WHERE location IS NOT NULL
    AND status = 'Published'
    AND ST_DWithin(
        location,
        ST_SetSRID(ST_MakePoint(79.8612, 6.9271), 4326)::geography,
        40233.6
    )
ORDER BY location <-> ST_SetSRID(ST_MakePoint(79.8612, 6.9271), 4326)::geography
LIMIT 50;
```

**Look for:**
- "Index Scan using ix_events_location_gist" ✅ GOOD
- "Seq Scan on events" ❌ BAD (missing index)
- Execution time < 10ms ✅ GOOD
- Execution time > 100ms ❌ BAD (needs optimization)

---

## 9. Testing Queries

### Test Data Setup
```csharp
// Create test events at known distances
var colomboFort = (Lat: 6.9271m, Lon: 79.8612m);
var testEvents = new[]
{
    (Name: "1km away", Lat: 6.9180m, Lon: 79.8612m),
    (Name: "5km away", Lat: 6.8800m, Lon: 79.8612m),
    (Name: "10km away", Lat: 6.8400m, Lon: 79.8612m),
    (Name: "25km away", Lat: 6.7000m, Lon: 79.8612m)
};

foreach (var (name, lat, lon) in testEvents)
{
    var coords = GeoCoordinate.Create(lat, lon).Value;
    var location = EventLocation.Create(address, coords).Value;
    var evt = CreateEvent(name, location);
    await _repository.AddAsync(evt);
}
```

### Verify Distance Calculations
```csharp
[Fact]
public async Task Distance_Calculations_AreAccurate()
{
    var colombo = GeoCoordinate.Create(6.9271m, 79.8612m).Value;
    var kandy = GeoCoordinate.Create(7.2906m, 80.6337m).Value;

    // Domain calculation (Haversine)
    var domainDistance = colombo.DistanceTo(kandy);

    // Database calculation (PostGIS)
    var dbDistance = await _repository.GetDistanceBetweenPoints(colombo, kandy);

    // Should be approximately equal (within 1%)
    dbDistance.Should().BeApproximately(domainDistance, domainDistance * 0.01);
}
```

---

## 10. Common Errors and Solutions

### Error: "type geography does not exist"
**Cause:** PostGIS extension not enabled
**Solution:**
```sql
CREATE EXTENSION IF NOT EXISTS postgis;
```

### Error: "function st_distance does not exist"
**Cause:** Missing NetTopologySuite configuration
**Solution:**
```csharp
options.UseNpgsql(
    connectionString,
    npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
);
```

### Error: "Coordinates out of range"
**Cause:** Invalid latitude/longitude values
**Solution:** Validate in domain layer
```csharp
if (latitude < -90 || latitude > 90)
    return Result.Failure("Latitude must be between -90 and 90");
if (longitude < -180 || longitude > 180)
    return Result.Failure("Longitude must be between -180 and 180");
```

### Error: "SRID mismatch"
**Cause:** Mixing different SRIDs
**Solution:** Always use SRID 4326
```csharp
var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
```

### Error: "Computed column cannot be updated"
**Cause:** Trying to directly set computed column
**Solution:** Update source columns (latitude/longitude), computed column auto-updates
```csharp
// DON'T: event.Location = newLocation; (tries to update computed column)
// DO: Update latitude/longitude, computed column updates automatically
```

---

## 11. Useful PostGIS Functions

| Function | Purpose | Returns |
|----------|---------|---------|
| `ST_Distance(g1, g2)` | Distance between geometries | Meters (geography) |
| `ST_DWithin(g1, g2, distance)` | Check if within distance | Boolean |
| `ST_MakePoint(lon, lat)` | Create point | Point |
| `ST_SetSRID(geom, srid)` | Set coordinate system | Geometry |
| `ST_AsText(geom)` | Convert to WKT string | Text |
| `ST_AsGeoJSON(geom)` | Convert to GeoJSON | JSON |
| `ST_X(point)` | Get longitude | Decimal |
| `ST_Y(point)` | Get latitude | Decimal |
| `ST_Contains(g1, g2)` | Check if g1 contains g2 | Boolean |
| `ST_Intersects(g1, g2)` | Check if geometries intersect | Boolean |

---

## 12. Migration Patterns

### Add Location to Existing Table
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. Enable PostGIS
    migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");

    // 2. Add columns
    migrationBuilder.AddColumn<decimal>("coordinates_latitude", ...);
    migrationBuilder.AddColumn<decimal>("coordinates_longitude", ...);

    // 3. Add computed column
    migrationBuilder.Sql(@"
        ALTER TABLE events.events
        ADD COLUMN location GEOGRAPHY(Point, 4326)
        GENERATED ALWAYS AS (
            ST_SetSRID(ST_MakePoint(coordinates_longitude, coordinates_latitude), 4326)::geography
        ) STORED;
    ");

    // 4. Create index
    migrationBuilder.Sql(@"
        CREATE INDEX ix_events_location_gist
        ON events.events
        USING GIST (location)
        WHERE location IS NOT NULL;
    ");
}
```

### Backfill Coordinates (Optional)
```csharp
// After migration, if you need to geocode existing events
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ... add columns ...

    // Backfill known locations (example)
    migrationBuilder.Sql(@"
        UPDATE events.events
        SET coordinates_latitude = 6.9271,
            coordinates_longitude = 79.8612
        WHERE address_city = 'Colombo' AND coordinates_latitude IS NULL;
    ");
}
```

---

## 13. API Response Formats

### Event with Distance
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "title": "Sri Lankan Food Festival",
  "startDate": "2025-11-15T10:00:00Z",
  "location": {
    "address": {
      "street": "123 Galle Road",
      "city": "Colombo",
      "state": "Western Province",
      "zipCode": "10100",
      "country": "Sri Lanka"
    },
    "coordinates": {
      "latitude": 6.9271,
      "longitude": 79.8612
    }
  },
  "distanceMiles": 5.2,
  "distanceKm": 8.4
}
```

### Nearby Events Response
```json
{
  "searchLocation": {
    "latitude": 6.9271,
    "longitude": 79.8612
  },
  "radiusMiles": 25,
  "totalResults": 12,
  "events": [
    {
      "id": "...",
      "title": "...",
      "distanceMiles": 0.5,
      "location": { ... }
    }
  ]
}
```

---

## 14. Resource Links

- [PostGIS Documentation](https://postgis.net/documentation/)
- [PostGIS Function Reference](https://postgis.net/docs/reference.html)
- [NetTopologySuite GitHub](https://github.com/NetTopologySuite/NetTopologySuite)
- [EF Core Spatial Data](https://learn.microsoft.com/en-us/ef/core/modeling/spatial)
- [SRID 4326 (WGS84)](https://epsg.io/4326)
- [PostGIS Performance Tuning](https://postgis.net/workshops/postgis-intro/performance.html)
- [Calculate Distance Online](https://www.movable-type.co.uk/scripts/latlong.html)
- [Sri Lanka Coordinates](https://www.latlong.net/place/sri-lanka-24411.html)

---

**Quick Test:**
```bash
# Test PostGIS installation
psql -U postgres -d lankaconnect -c "SELECT PostGIS_version();"

# Test spatial query
psql -U postgres -d lankaconnect -c "
SELECT COUNT(*) FROM events.events WHERE location IS NOT NULL;
"
```
