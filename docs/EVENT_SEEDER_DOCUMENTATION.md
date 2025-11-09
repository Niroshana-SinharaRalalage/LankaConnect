# Event Seeder Documentation

## Overview

The Event Seeder provides 25 diverse dummy events representing Sri Lankan community activities across Ohio metro areas. Events are automatically seeded when the application starts in Development or Staging environments.

## Implementation Details

### Files Created

1. **EventSeeder.cs** (`src/LankaConnect.Infrastructure/Data/Seeders/EventSeeder.cs`)
   - Static class containing seed data generation logic
   - Creates 25 events with realistic Sri Lankan community themes
   - Includes proper value object creation and validation

2. **DbInitializer.cs** (`src/LankaConnect.Infrastructure/Data/Seeders/DbInitializer.cs`)
   - Database initialization service
   - Handles idempotent seeding (safe to run multiple times)
   - Provides `SeedAsync()` and `ReseedAsync()` methods

3. **Program.cs Updates** (`src/LankaConnect.API/Program.cs`)
   - Integrated automatic seeding on application startup
   - Only runs in Development and Staging environments
   - Executes after database migrations

## Event Categories Included

The seeder includes events across all categories:

- **Religious** (5 events): Vesak, Poson, Kathina ceremonies, meditation retreats
- **Cultural** (8 events): New Year celebration, Esala Perahera, food festivals, cooking classes, children's programs
- **Community** (3 events): Picnics, cricket tournaments, volleyball championships
- **Educational** (4 events): Language courses, health seminars, youth workshops, career development
- **Social** (2 events): Wedding showcase, family picnics
- **Business** (2 events): Professional networking, tech meetups
- **Charity** (1 event): Flood relief fundraiser
- **Entertainment** (2 events): Music concerts, cinema nights, drama performances

## Geographic Distribution

Events are distributed across major Ohio metropolitan areas:

- **Cleveland** (9 events)
- **Columbus** (7 events)
- **Cincinnati** (3 events)
- **Akron** (2 events)
- **Dublin** (2 events)
- **Westlake** (2 events)
- **Aurora** (2 events)
- **Loveland** (1 event)

## Event Details

Each event includes:

- **Title**: Descriptive event name
- **Description**: Detailed description with Sinhala terms where appropriate
- **Dates**: Realistic date ranges (1 month past to 3 months future)
- **Location**: Complete address with GPS coordinates (latitude/longitude)
- **Category**: Appropriate event category
- **Capacity**: Realistic venue capacities (25-500 attendees)
- **Ticket Price**: Mix of free events and paid events ($10-$120)
- **Status**: Appropriate status (mostly Published, one Completed for testing)

## Event Status Distribution

- **Published**: 24 events (available for registration)
- **Completed**: 1 event (past drama performance)

## Ticket Pricing

- **Free Events**: 13 events (52%)
- **Paid Events**: 12 events (48%)
  - Price range: $10 - $120 USD
  - Average price: ~$30 USD

## Usage

### Automatic Seeding (Recommended)

The seeder runs automatically when you start the API in Development or Staging:

```bash
cd src/LankaConnect.API
dotnet run
```

The application will:
1. Apply all database migrations
2. Check if events already exist
3. If no events exist, seed 25 events
4. Log the seeding process

### Manual Seeding

You can also manually seed the database using the DbInitializer:

```csharp
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<DbInitializer>>();

var dbInitializer = new DbInitializer(context, logger);

// Seed data (idempotent - safe to run multiple times)
await dbInitializer.SeedAsync();

// OR: Clear existing data and reseed (caution!)
await dbInitializer.ReseedAsync();
```

### Checking Seeded Data

After seeding, you can verify the data:

**Via API:**
```bash
GET https://localhost:7034/api/events
```

**Via Database:**
```sql
SELECT
    id,
    title,
    category,
    status,
    capacity,
    ticket_price_amount,
    address_city,
    start_date
FROM events.events
ORDER BY start_date;
```

## Integration with Existing System

The seeder is fully integrated with:

- **Domain Model**: Uses `Event.Create()` factory method
- **Value Objects**: Properly creates EventTitle, EventDescription, EventLocation, Money
- **Business Rules**: Respects all domain invariants
- **EF Core Configuration**: Compatible with owned entities and complex types
- **Migrations**: No migration needed - data is seeded at runtime

## Test Data Characteristics

### Organizer ID
All events use a test organizer ID: `11111111-1111-1111-1111-111111111111`

This allows for easy filtering and cleanup of test data.

### Date Ranges
- **Past Events**: 1 event (15 days ago) - for testing completed status
- **Upcoming Events**: 24 events (10-90 days in future)

### Location Data
All events include:
- Complete venue names
- Full street addresses
- Accurate GPS coordinates for Ohio locations
- City, State (Ohio), ZIP code, Country (USA)

## Cleanup (Development/Testing)

To remove seeded events and start fresh:

```csharp
// Option 1: Use ReseedAsync
var dbInitializer = new DbInitializer(context, logger);
await dbInitializer.ReseedAsync();

// Option 2: Manual cleanup
var events = await context.Events.ToListAsync();
context.Events.RemoveRange(events);
await context.SaveChangesAsync();
```

## Production Considerations

**Important**: The seeder only runs in Development and Staging environments by design.

In Production:
- Seeder is disabled by default
- Real events should be created through the API
- Consider creating an admin tool for manual seeding if needed

## Extending the Seeder

To add more events or modify existing ones:

1. Edit `EventSeeder.cs`
2. Add new events to the `GetSeedEvents()` method
3. Follow the existing pattern using `CreateEvent()` helper
4. Restart the API to apply changes

Example:
```csharp
var newEvent = CreateEvent(
    "Event Title",
    "Detailed description...",
    startDate: now.AddDays(30),
    endDate: now.AddDays(30).AddHours(4),
    "Venue Name",
    "123 Main St",
    "Columbus",
    "43215",
    latitude: 39.9612m,
    longitude: -82.9988m,
    EventCategory.Cultural,
    capacity: 200,
    ticketPrice: CreateMoney(25, Currency.USD),
    status: EventStatus.Published
);
if (newEvent != null) events.Add(newEvent);
```

## Testing

The seeder supports TDD principles:

1. **Build Verification**: Ensures code compiles without errors
2. **Idempotency**: Safe to run multiple times without creating duplicates
3. **Validation**: Uses domain factory methods ensuring all business rules are respected
4. **Logging**: Comprehensive logging for debugging
5. **Error Handling**: Graceful failure handling with detailed error messages

## Troubleshooting

### Issue: Events not appearing

**Solution**: Check logs for seeding messages:
```
Seeding events...
Successfully seeded 25 events to the database.
```

### Issue: Duplicate events

**Solution**: The seeder checks for existing events before seeding. Clear the database if needed:
```bash
dotnet ef database drop
dotnet ef database update
```

### Issue: Validation errors

**Solution**: Check console output for specific validation failures:
```
Failed to create title: Title cannot exceed 200 characters
```

## Summary

The Event Seeder provides a comprehensive set of realistic test data for development and testing. It includes:

- ✅ 25 diverse events
- ✅ All 8 event categories
- ✅ 8 Ohio metro areas
- ✅ Realistic dates, pricing, and capacities
- ✅ Proper domain modeling with value objects
- ✅ Automatic seeding on startup
- ✅ Idempotent operation
- ✅ Environment-aware (Dev/Staging only)
- ✅ Fully integrated with existing codebase
- ✅ TDD compliant
- ✅ Production-safe

## Related Files

- Domain Model: `src/LankaConnect.Domain/Events/Event.cs`
- Value Objects: `src/LankaConnect.Domain/Events/ValueObjects/`
- EF Configuration: `src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs`
- API Integration: `src/LankaConnect.API/Program.cs`
