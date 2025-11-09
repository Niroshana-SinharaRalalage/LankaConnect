# Database Events & Cultural Calendar Implementation Plan

## Executive Summary
Convert the LankaConnect frontend from mock data to real database-driven events and implement a Cultural Calendar management system.

## Current State Analysis

### ✅ **COMPLETED (Backend)**
1. **Event Domain Model** (`Event.cs`)
   - Full CRUD operations
   - Event categories, locations, pricing
   - Registration/RSVP functionality
   - Image/video support
   - Waiting list support

2. **Events API** (`EventsController.cs`)
   - GET /api/events (with filters)
   - GET /api/events/{id}
   - POST /api/events (create)
   - PUT /api/events/{id} (update)
   - DELETE /api/events/{id}
   - Search, nearby events, RSVP endpoints

3. **Database Migrations**
   - Events table with PostGIS location support
   - Event images and videos
   - Event analytics
   - Registrations table

### ❌ **MISSING**
1. **Cultural Calendar System**
   - No database table for cultural events
   - Domain model exists but not integrated
   - Frontend uses hardcoded data

2. **Frontend Integration**
   - Still using `mockFeedData.ts`
   - No API calls to backend
   - No data fetching hooks

## Implementation Tasks

### Phase 1: Cultural Calendar Database Schema

#### Task 1.1: Create CulturalCalendarEvent Entity
**File**: `src/LankaConnect.Domain/CulturalCalendar/CulturalCalendarEvent.cs`

```csharp
public class CulturalCalendarEvent : BaseEntity
{
    public string Name { get; private set; }
    public string NativeName { get; private set; }
    public DateTime Date { get; private set; }
    public CulturalEventCategory Category { get; private set; }
    public CulturalCommunity Community { get; private set; }
    public string Description { get; private set; }
    public bool IsAnnual { get; private set; }
    public bool IsPublicHoliday { get; private set; }
    public int DisplayOrder { get; private set; }
}
```

#### Task 1.2: Create Database Migration
**File**: `src/LankaConnect.Infrastructure/Migrations/YYYYMMDD_CreateCulturalCalendarTable.cs`

#### Task 1.3: Create Repository & Configuration
- `ICulturalCalendarRepository.cs`
- `CulturalCalendarRepository.cs`
- `CulturalCalendarConfiguration.cs`

### Phase 2: Cultural Calendar API

#### Task 2.1: Create CQRS Commands/Queries
- `GetCulturalCalendarEventsQuery` - Get events for display
- `CreateCulturalEventCommand` - Admin: Add events
- `UpdateCulturalEventCommand` - Admin: Modify events
- `DeleteCulturalEventCommand` - Admin: Remove events

#### Task 2.2: Create Controller
**File**: `src/LankaConnect.API/Controllers/CulturalCalendarController.cs`

```csharp
[HttpGet]
public async Task<IActionResult> GetUpcomingEvents([FromQuery] int count = 10)

[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)

[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Create([FromBody] CreateCulturalEventCommand command)
```

### Phase 3: Seed Cultural Calendar Data

#### Task 3.1: Create Seeder
**File**: `src/LankaConnect.Infrastructure/Data/Seeders/CulturalCalendarSeeder.cs`

**Initial Events to Seed:**
1. Sinhala & Tamil New Year - April 13-14
2. Vesak Day - May (full moon)
3. Poson Day - June (full moon)
4. Esala Perahera - July-August
5. Independence Day - February 4
6. Deepavali - October-November
7. Christmas - December 25
8. Thai Pongal - January 14-15
9. Maha Shivaratri - February-March
10. Eid al-Fitr - Variable (Islamic calendar)

### Phase 4: Events Database Seeding

#### Task 4.1: Create Event Seeder
**File**: `src/LankaConnect.Infrastructure/Data/Seeders/EventSeeder.cs`

**Dummy Events to Create:**
- 20 events across different categories
- Mix of free and paid events
- Different locations (Cleveland, Columbus, Cincinnati, etc.)
- Various dates (past, present, future)
- Include images and descriptions
- RSVP/registration data

### Phase 5: Frontend API Integration

#### Task 5.1: Create API Client Services
**Files**:
- `web/src/infrastructure/api/services/EventService.ts`
- `web/src/infrastructure/api/services/CulturalCalendarService.ts`

#### Task 5.2: Create React Query Hooks
**Files**:
- `web/src/presentation/hooks/useEvents.ts`
- `web/src/presentation/hooks/useCulturalCalendar.ts`

#### Task 5.3: Update Components
**Files to Update:**
1. `web/src/app/page.tsx` - Replace mockFeedItems with API call
2. `web/src/app/(dashboard)/dashboard/page.tsx` - Replace CulturalCalendar hardcoded data
3. Remove or deprecate `web/src/domain/data/mockFeedData.ts`

### Phase 6: Testing & Validation

#### Task 6.1: Backend Tests
- Unit tests for Cultural Calendar commands/queries
- Integration tests for API endpoints
- Seed data validation

#### Task 6.2: Frontend Tests
- Test API integration
- Test data fetching hooks
- Test error handling

## Database Schema Designs

### CulturalCalendarEvents Table
```sql
CREATE TABLE cultural_calendar_events (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    native_name VARCHAR(200),
    date DATE NOT NULL,
    category VARCHAR(50) NOT NULL, -- national, religious, cultural, holiday
    community VARCHAR(50) NOT NULL, -- sinhala, tamil, muslim, burgher, etc
    description TEXT,
    is_annual BOOLEAN DEFAULT true,
    is_public_holiday BOOLEAN DEFAULT false,
    display_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_cultural_events_date ON cultural_calendar_events(date);
CREATE INDEX idx_cultural_events_category ON cultural_calendar_events(category);
```

## API Endpoints Summary

### Events (Already Exist)
- `GET /api/events` - List events with filters
- `GET /api/events/{id}` - Get event details
- `POST /api/events` - Create event
- `PUT /api/events/{id}` - Update event
- `DELETE /api/events/{id}` - Delete event

### Cultural Calendar (To Create)
- `GET /api/cultural-calendar` - List cultural events
- `GET /api/cultural-calendar/upcoming?count=10` - Upcoming events
- `GET /api/cultural-calendar/{id}` - Get event details
- `POST /api/cultural-calendar` - Create event (Admin)
- `PUT /api/cultural-calendar/{id}` - Update event (Admin)
- `DELETE /api/cultural-calendar/{id}` - Delete event (Admin)

## Migration Strategy

### Step 1: Backend Implementation (API Layer)
1. Create Cultural Calendar tables and migrations
2. Implement seeders with initial data
3. Create API endpoints
4. Test with Postman/Swagger

### Step 2: Frontend Integration
1. Create API services
2. Create React Query hooks
3. Update components one by one
4. Keep mock data as fallback during transition

### Step 3: Data Migration
1. Run migrations on staging database
2. Seed cultural calendar data
3. Seed dummy events data
4. Verify data integrity

### Step 4: Deployment
1. Deploy backend changes
2. Deploy frontend changes
3. Monitor for errors
4. Remove mock data after verification

## Timeline Estimate

- **Phase 1**: Cultural Calendar Schema - 2 hours
- **Phase 2**: Cultural Calendar API - 3 hours
- **Phase 3**: Cultural Calendar Seeding - 1 hour
- **Phase 4**: Events Seeding - 2 hours
- **Phase 5**: Frontend Integration - 4 hours
- **Phase 6**: Testing - 2 hours

**Total Estimated Time**: 14 hours

## Priority Order

1. **HIGH**: Seed dummy events in database (Phase 4)
2. **HIGH**: Frontend integration for events (Phase 5.3)
3. **MEDIUM**: Cultural Calendar database (Phases 1-3)
4. **MEDIUM**: Cultural Calendar frontend (Phase 5.1-5.2)
5. **LOW**: Advanced features (analytics, recommendations)

## Next Steps

Would you like me to:
1. ✅ Create the Cultural Calendar database schema and migrations
2. ✅ Seed dummy events into the existing Events table
3. ✅ Update the frontend to fetch from API instead of mock data
4. ✅ All of the above

**Recommendation**: Start with #2 (seed events) and #3 (frontend integration) since the Events API already exists. Then move to Cultural Calendar implementation.
