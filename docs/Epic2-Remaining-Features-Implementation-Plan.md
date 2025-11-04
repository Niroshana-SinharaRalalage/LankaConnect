# Epic 2 - Remaining Features Implementation Plan
*Created: 2025-11-04*
*Status: In Progress - Event Analytics Domain Layer Complete*

---

## 🎯 Implementation Status

### ✅ Completed Today (Session 2025-11-04)

**Event Analytics - Domain Layer (GREEN ✅):**
- ✅ Created EventAnalytics aggregate (16 tests passing)
- ✅ Implemented RecordView, UpdateUniqueViewers, UpdateRegistrationCount methods
- ✅ Implemented ConversionRate calculated property
- ✅ Created EventViewRecordedDomainEvent
- ✅ All validation logic (coordinate validation, negative checks, etc.)
- ✅ Zero compilation errors maintained

**Files Created:**
- `src/LankaConnect.Domain/Analytics/EventAnalytics.cs` (95 lines)
- `src/LankaConnect.Domain/Analytics/DomainEvents/EventViewRecordedDomainEvent.cs` (12 lines)
- `tests/LankaConnect.Application.Tests/Analytics/Domain/EventAnalyticsTests.cs` (283 lines, 16 tests)

---

## 📋 Remaining Work (5 Features)

### 1️⃣ Event Analytics (70% Complete - 3 days remaining)

**Completed:**
- ✅ Domain model (EventAnalytics aggregate)
- ✅ Domain tests (16 tests passing)
- ✅ Domain events

**Remaining:**
```
Day 1 (4 hours):
├── Repository Interface (IEventAnalyticsRepository)
├── Repository Implementation (EventAnalyticsRepository)
├── EF Core Configuration (EventAnalyticsConfiguration)
├── Database Migration (20251104_AddEventAnalytics)
└── Application Layer Tests (RecordEventViewCommandTests)

Day 2 (4 hours):
├── RecordEventViewCommand + Handler
├── GetEventAnalyticsQuery + Handler
├── GetOrganizerDashboardQuery + Handler
├── EventAnalyticsDto / OrganizerDashboardDto
└── AutoMapper Configuration

Day 3 (4 hours):
├── API Controller (AnalyticsController)
├── Integration with GetEventByIdQuery (fire-and-forget tracking)
├── Hangfire Background Jobs (analytics rollup, unique viewer calculation)
├── Integration Tests
└── Deploy to staging
```

**Estimated Effort:** 3 days (12 hours)

---

### 2️⃣ Full-Text Search (PostgreSQL tsvector) - 1.5 days

**Requirements:**
- Add search_vector computed column to events table
- Create GIN index for fast search
- Implement SearchEventsQuery with ranking
- Support multi-term queries with AND/OR logic

**Implementation:**
```sql
-- Migration
ALTER TABLE events ADD COLUMN search_vector tsvector
    GENERATED ALWAYS AS (
        setweight(to_tsvector('english', title), 'A') ||
        setweight(to_tsvector('english', description), 'B')
    ) STORED;

CREATE INDEX idx_events_search ON events USING GIN(search_vector);
```

```csharp
// Query
public record SearchEventsQuery(string SearchTerm, int PageSize = 20, int Page = 1)
    : IQuery<Result<IReadOnlyList<EventDto>>>;

// Repository Method
Task<IReadOnlyList<Event>> SearchEventsAsync(string searchTerm, CancellationToken ct);

// Implementation
SELECT *, ts_rank(search_vector, websearch_to_tsquery('english', @searchTerm)) AS rank
FROM events
WHERE search_vector @@ websearch_to_tsquery('english', @searchTerm)
ORDER BY rank DESC, start_date ASC
LIMIT @pageSize OFFSET @offset;
```

**Files to Create:**
- `SearchEventsQuery.cs` + Handler + Tests
- `Migration: 20251104_AddFullTextSearch.cs`
- API endpoint: `GET /api/events/search?q={term}`

**Estimated Effort:** 1.5 days (6 hours)

---

### 3️⃣ ICS Calendar Export - 0.5 days

**Requirements:**
- Generate ICS (iCalendar) file for "Add to Calendar" functionality
- Support Google Calendar, Apple Calendar, Outlook

**Implementation:**
```csharp
// Query
public record GetEventIcsQuery(Guid EventId) : IQuery<Result<string>>;

// Handler generates ICS format
BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//LankaConnect//Event//EN
BEGIN:VEVENT
UID:event-{id}@lankaconnect.com
DTSTAMP:{timestamp}
DTSTART:{startDate}
DTEND:{endDate}
SUMMARY:{title}
DESCRIPTION:{description}
LOCATION:{address}
URL:{eventUrl}
END:VEVENT
END:VCALENDAR
```

**API Endpoint:**
```csharp
[HttpGet("{id}/ics")]
[ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
public async Task<IActionResult> GetEventIcs(Guid id)
{
    var result = await Mediator.Send(new GetEventIcsQuery(id));
    if (result.IsFailure)
        return NotFound();

    var bytes = Encoding.UTF8.GetBytes(result.Value);
    return File(bytes, "text/calendar", $"event-{id}.ics");
}
```

**Files to Create:**
- `GetEventIcsQuery.cs` + Handler + Tests
- API endpoint: `GET /api/events/{id}/ics`

**Estimated Effort:** 0.5 days (2 hours)

---

### 4️⃣ Social Sharing Tracking - 0.5 days

**Requirements:**
- Track share count (Facebook, Twitter, LinkedIn, WhatsApp)
- Open Graph meta tags for rich previews

**Implementation:**
```csharp
// Command
public record ShareEventCommand(Guid EventId, string Platform) : ICommand;

// Add to EventAnalytics aggregate
public class EventAnalytics : BaseEntity
{
    public int ShareCount { get; private set; }

    public void RecordShare(string platform)
    {
        ShareCount++;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new EventSharedDomainEvent(EventId, platform));
    }
}

// Migration
ALTER TABLE event_analytics ADD COLUMN share_count INTEGER DEFAULT 0;
```

**API Endpoint:**
```csharp
[HttpPost("{id}/share")]
public async Task<IActionResult> ShareEvent(Guid id, [FromBody] ShareEventRequest request)
{
    var command = new ShareEventCommand(id, request.Platform);
    var result = await Mediator.Send(command);
    return HandleResult(result);
}

// Share URL generation
[HttpGet("{id}/share-url/{platform}")]
public IActionResult GetShareUrl(Guid id, string platform)
{
    var eventUrl = $"{Request.Scheme}://{Request.Host}/events/{id}";
    var shareUrl = platform.ToLower() switch
    {
        "facebook" => $"https://www.facebook.com/sharer/sharer.php?u={Uri.EscapeDataString(eventUrl)}",
        "twitter" => $"https://twitter.com/intent/tweet?url={Uri.EscapeDataString(eventUrl)}&text={Uri.EscapeDataString(eventTitle)}",
        "linkedin" => $"https://www.linkedin.com/sharing/share-offsite/?url={Uri.EscapeDataString(eventUrl)}",
        "whatsapp" => $"https://wa.me/?text={Uri.EscapeDataString($"{eventTitle} {eventUrl}")}",
        _ => eventUrl
    };
    return Ok(new { shareUrl });
}
```

**Files to Create:**
- `ShareEventCommand.cs` + Handler + Tests
- Migration update for share_count
- API endpoints: `POST /api/events/{id}/share`, `GET /api/events/{id}/share-url/{platform}`

**Estimated Effort:** 0.5 days (2 hours)

---

### 5️⃣ Waiting List with Auto-Notification - 1.5 days

**Requirements:**
- When event is full (capacity reached), users can join waiting list
- When someone cancels RSVP, notify waiting list (FIFO)
- Automatic conversion from waiting list to confirmed registration

**Domain Model:**
```csharp
// Add to Event aggregate
public class Event : AggregateRoot
{
    private readonly List<WaitingListEntry> _waitingList = new();
    public IReadOnlyList<WaitingListEntry> WaitingList => _waitingList.AsReadOnly();

    public Result AddToWaitingList(Guid userId)
    {
        if (!IsAtCapacity())
            return Result.Failure("Event is not at capacity. Register normally instead.");

        if (_waitingList.Any(w => w.UserId == userId))
            return Result.Failure("Already on waiting list");

        var entry = new WaitingListEntry(userId, DateTime.UtcNow, _waitingList.Count + 1);
        _waitingList.Add(entry);

        RaiseDomainEvent(new UserAddedToWaitingListDomainEvent(Id, userId));
        return Result.Success();
    }

    public Result RemoveFromWaitingList(Guid userId)
    {
        var entry = _waitingList.FirstOrDefault(w => w.UserId == userId);
        if (entry == null)
            return Result.Failure("Not on waiting list");

        _waitingList.Remove(entry);
        ResequenceWaitingList();
        return Result.Success();
    }

    private void NotifyWaitingListIfCapacityAvailable()
    {
        // Called when RSVP is cancelled
        if (!IsAtCapacity() && _waitingList.Any())
        {
            var nextInLine = _waitingList.OrderBy(w => w.Position).First();
            RaiseDomainEvent(new WaitingListSpotAvailableDomainEvent(Id, nextInLine.UserId));
        }
    }
}

// Value Object
public class WaitingListEntry : ValueObject
{
    public Guid UserId { get; }
    public DateTime JoinedAt { get; }
    public int Position { get; private set; }

    public WaitingListEntry(Guid userId, DateTime joinedAt, int position)
    {
        UserId = userId;
        JoinedAt = joinedAt;
        Position = position;
    }

    public void UpdatePosition(int newPosition) => Position = newPosition;
}
```

**Commands:**
```csharp
public record AddToWaitingListCommand(Guid EventId, Guid UserId) : ICommand;
public record RemoveFromWaitingListCommand(Guid EventId, Guid UserId) : ICommand;
```

**Domain Event Handlers:**
```csharp
public class WaitingListSpotAvailableEventHandler
    : INotificationHandler<WaitingListSpotAvailableDomainEvent>
{
    public async Task Handle(WaitingListSpotAvailableDomainEvent notification, CancellationToken ct)
    {
        // Send email notification to user
        await _emailService.SendEmailAsync(new EmailMessageDto
        {
            ToEmail = userEmail,
            Subject = "Spot Available: Event Registration Now Open",
            HtmlBody = "A spot has opened up for the event you're waiting for. Register now!"
        }, ct);
    }
}

// Update CancelRsvpCommandHandler
public class CancelRsvpCommandHandler : IRequestHandler<CancelRsvpCommand, Result>
{
    public async Task<Result> Handle(CancelRsvpCommand request, CancellationToken ct)
    {
        // ... existing cancellation logic ...

        @event.CancelRegistration(request.UserId); // This raises NotifyWaitingListIfCapacityAvailable

        await _unitOfWork.CommitAsync(ct); // This dispatches domain events
        return Result.Success();
    }
}
```

**Database Schema:**
```sql
CREATE TABLE event_waiting_list (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    position INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_event_waiting_list_event_user UNIQUE(event_id, user_id)
);

CREATE INDEX idx_event_waiting_list_event_id ON event_waiting_list(event_id);
CREATE INDEX idx_event_waiting_list_position ON event_waiting_list(event_id, position);
```

**API Endpoints:**
```csharp
[HttpPost("{id}/waiting-list")]
[Authorize]
public async Task<IActionResult> JoinWaitingList(Guid id)
{
    var userId = User.GetUserId();
    var command = new AddToWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);
    return HandleResult(result);
}

[HttpDelete("{id}/waiting-list")]
[Authorize]
public async Task<IActionResult> LeaveWaitingList(Guid id)
{
    var userId = User.GetUserId();
    var command = new RemoveFromWaitingListCommand(id, userId);
    var result = await Mediator.Send(command);
    return HandleResult(result);
}

[HttpGet("{id}/waiting-list")]
[Authorize(Roles = "Admin,Organizer")]
public async Task<IActionResult> GetWaitingList(Guid id)
{
    var query = new GetEventWaitingListQuery(id);
    var result = await Mediator.Send(query);
    return HandleResult(result);
}
```

**Files to Create:**
- Domain: `WaitingListEntry.cs`, domain events
- Commands: `AddToWaitingListCommand`, `RemoveFromWaitingListCommand` + handlers
- Queries: `GetEventWaitingListQuery` + handler
- Event handlers: `WaitingListSpotAvailableEventHandler`
- Migration: `20251104_AddEventWaitingList.cs`
- Tests: Domain tests (10+), command tests (8+), query tests (4+)

**Estimated Effort:** 1.5 days (6 hours)

---

## 📊 Total Remaining Effort Summary

| Feature | Status | Estimated Time | Priority |
|---------|--------|---------------|----------|
| **Event Analytics** | 70% Complete | 3 days | HIGH |
| **Full-Text Search** | Not Started | 1.5 days | MEDIUM |
| **ICS Calendar Export** | Not Started | 0.5 days | LOW |
| **Social Sharing** | Not Started | 0.5 days | LOW |
| **Waiting List** | Not Started | 1.5 days | MEDIUM |
| **Total** | - | **7 days** | - |

---

## 🚀 Recommended Implementation Order

### **Option A: Complete All Features (7 days)**
1. Day 1-3: Complete Event Analytics
2. Day 4-5: Full-Text Search
3. Day 6: Waiting List
4. Day 7: ICS Export + Social Sharing

### **Option B: MVP Approach (4 days)**
1. Day 1-3: Complete Event Analytics
2. Day 4: Full-Text Search
3. Defer: ICS Export, Social Sharing, Waiting List to post-launch

---

## ✅ Success Criteria

**For Each Feature:**
- ✅ All domain tests passing (TDD RED-GREEN-REFACTOR)
- ✅ All application layer tests passing
- ✅ Zero compilation errors
- ✅ Integration tests passing
- ✅ API endpoints documented in Swagger
- ✅ Deployed to staging
- ✅ PROGRESS_TRACKER.md updated

**Overall Epic 2 Completion:**
- ✅ 100% feature completion (all 5 remaining features)
- ✅ All 700+ tests passing
- ✅ Performance benchmarks met
- ✅ Documentation complete

---

## 📝 Next Steps

**Immediate (Continue Event Analytics):**
1. Create IEventAnalyticsRepository interface
2. Implement EventAnalyticsRepository with EF Core
3. Create database migration
4. Implement RecordEventViewCommand + Handler (with tests)
5. Implement GetEventAnalyticsQuery + Handler (with tests)
6. Implement GetOrganizerDashboardQuery + Handler (with tests)
7. Add AnalyticsController with API endpoints
8. Integration tests
9. Deploy to staging

**After Event Analytics:**
- Proceed with Full-Text Search (highest value of remaining features)
- Then Waiting List
- Finally ICS Export and Social Sharing

---

*Document Status: Active Implementation Guide*
*Last Updated: 2025-11-04 - EventAnalytics Domain Layer Complete*
