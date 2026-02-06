# Root Cause Analysis: LankaConnect Event System Issues

**Date**: 2025-12-23
**System**: LankaConnect Event Management
**Architecture**: .NET 8 Clean Architecture with DDD + Next.js Frontend
**Environment**: Azure Container Apps (backend), Local Dev (frontend)

---

## Executive Summary

Three separate issues have been identified in the event system:

1. **Event Publication Emails Not Sending** - Template was missing from database despite migration existing
2. **No Unpublish Functionality** - Domain model doesn't support unpublishing published events
3. **Post-Creation Navigation Issue** - After creating event, user redirected to dashboard instead of manage page

**Overall Risk Level**: MEDIUM
**User Impact**: HIGH (Issue 1), MEDIUM (Issue 2), LOW (Issue 3)

---

## Issue 1: Event Publication Emails Not Sending

### Symptom
User created and published event ID `539a4ca3-8c5f-4024-b226-bfa43111bd6c` but received no email notification.

### Root Cause Analysis

**Code Flow**:
1. User publishes event via `POST /api/Events/{id}/publish`
2. `PublishEventCommandHandler` calls `@event.Publish()` domain method
3. Domain aggregate (`Event.cs` line 120-135) changes status and raises `EventPublishedEvent`
4. `EventPublishedEventHandler` listens for domain event and sends emails
5. Handler uses `_emailService.SendTemplatedEmailAsync("event-published", ...)` (line 101-105)
6. Email service queries database table `communications.email_templates` for template code `event-published`

**Root Cause**: Database Migration Not Applied

**Evidence**:
- Migration file exists: `20251221160725_SeedEventPublishedTemplate_Phase6A39.cs`
- Script file exists: `scripts/InsertEventPublishedTemplate.cs`
- Template was inserted manually AFTER event 539a4ca3 was created
- Migration was recorded in `__EFMigrationsHistory` but template never inserted (likely migration rollback/rerun issue)

**Why It Failed**:
```csharp
// EventPublishedEventHandler.cs line 101-105
var result = await _emailService.SendTemplatedEmailAsync(
    "event-published",  // Template code not in database
    email,
    parameters,
    cancellationToken);
```

When template missing from database:
- `SendTemplatedEmailAsync` returns failure result
- Handler logs warning but doesn't throw (fail-silent pattern)
- User never notified of failure

**Current Status**: FIXED (template manually inserted)

### Verification Needed

**Azure Container Logs Check**:
```bash
# Check if email was attempted for event 539a4ca3-8c5f-4024-b226-bfa43111bd6c
az containerapp logs show \
  --name lankaconnect-api \
  --resource-group <resource-group> \
  --filter "EventId=539a4ca3-8c5f-4024-b226-bfa43111bd6c"

# Look for these log patterns:
# 1. "[Phase 6A] EventPublishedEventHandler INVOKED"
# 2. "No email recipients found" OR "Resolved N unique email recipients"
# 3. "Failed to send event notification email" (if template missing)
# 4. "Event notification emails completed" (success count)
```

---

## Issue 2: No Way to Unpublish an Event

### Symptom
Once an event is published, there's no way to change status back to Draft for editing.

### Root Cause Analysis

**Domain Model Limitation**:

```csharp
// Event.cs lines 120-135
public Result Publish()
{
    if (Status == EventStatus.Published)
        return Result.Failure("Event is already published");

    if (Status != EventStatus.Draft)
        return Result.Failure("Only draft events can be published");

    Status = EventStatus.Published;
    MarkAsUpdated();

    // Raise domain event
    RaiseDomainEvent(new EventPublishedEvent(Id, DateTime.UtcNow, OrganizerId));

    return Result.Success();
}
```

**No Unpublish Method Exists** - Domain aggregate has no `Unpublish()` method.

**State Transition Rules (Current)**:
```
Draft --> Published --> [Active/Cancelled/Postponed/Completed/Archived]
          ^^^^^ ONE WAY ONLY
```

**Business Logic Gaps**:
1. **No unpublish domain method** in `Event.cs`
2. **No unpublish command** in `Application/Events/Commands/`
3. **No unpublish API endpoint** in `EventsController.cs`
4. **No unpublish button** in frontend `EventManagePage.tsx`

**Why This Matters**:
- Organizers may publish prematurely
- Need to make changes after publication
- Currently must cancel entire event (harsh penalty)
- Cancellation sends notifications to all registrants (unnecessary if just editing)

### Architecture Impact

**Missing Components**:

1. **Domain Layer** (`Event.cs`):
   - Missing `Unpublish()` method with business rules
   - Missing `EventUnpublishedEvent` domain event

2. **Application Layer**:
   - Missing `UnpublishEventCommand` + handler
   - Missing domain event handler for notifications

3. **API Layer** (`EventsController.cs`):
   - Missing `POST /api/Events/{id}/unpublish` endpoint

4. **Frontend** (`EventManagePage.tsx`):
   - Missing "Unpublish Event" button when status = Published
   - Missing confirmation dialog

**Business Rules to Consider**:
- Can events with registrations be unpublished?
- Should registrants be notified?
- Should pending payments be cancelled?
- Can Active events be unpublished? (probably not)

---

## Issue 3: After Creating Event, User Should Stay on Manage Page

### Symptom
After successfully creating an event, user is redirected to `/dashboard` instead of `/events/{id}/manage`.

### Root Cause Analysis

**Current Code Flow**:

```typescript
// EventCreationForm.tsx lines 211-216
console.log('⏳ Sending request to API...');
const eventId = await createEventMutation.mutateAsync(eventData);
console.log('✅ Event created successfully! ID:', eventId);

// Session 33: Redirect to dashboard instead of event management page
router.push('/dashboard');
```

**Root Cause**: Intentional Design Decision (Session 33)

**Comment Evidence**: "Session 33: Redirect to dashboard instead of event management page"

This was deliberately changed from redirecting to manage page. However, the UX is suboptimal:

**User Journey Problem**:
1. User creates event
2. Redirected to dashboard
3. User must find event in "My Events" list
4. User clicks "Manage" to upload images/videos/badges
5. **Extra clicks, cognitive load**

**Expected UX**:
1. User creates event
2. Immediately lands on `/events/{id}/manage`
3. User can upload media, publish, configure sign-ups
4. **Seamless workflow**

**Why Dashboard Redirect is Suboptimal**:
- User wants to **complete event setup** after creation
- Images, videos, badges, sign-up lists all managed from manage page
- Dashboard is for **viewing** events, not **completing** setup

---

## Fix Plan

### Issue 1: Event Publication Emails (Status: FIXED)

**Verification Steps**:

1. **Check Azure Logs for Event 539a4ca3**:
```bash
# Azure CLI
az containerapp logs show \
  --name lankaconnect-api \
  --resource-group <rg-name> \
  --filter "539a4ca3-8c5f-4024-b226-bfa43111bd6c" \
  --follow

# Look for:
# - "EventPublishedEventHandler INVOKED"
# - "Resolved N unique email recipients"
# - "Event notification emails completed"
```

2. **Test Email Sending**:
```bash
# Create new test event and publish
# Check if email arrives for event email groups + metro subscribers
```

3. **Database Verification**:
```sql
-- Verify template exists
SELECT template_code, subject, is_active
FROM communications.email_templates
WHERE template_code = 'event-published';

-- Should return 1 row with is_active = true
```

**No Code Changes Needed** - Template manually inserted, issue resolved.

---

### Issue 2: Add Unpublish Event Feature

**Required Changes**:

#### 1. Domain Layer (`Event.cs`)

Add unpublish method after line 135:

```csharp
/// <summary>
/// Unpublishes a published event, returning it to Draft status
/// Business Rules:
/// - Only Published events can be unpublished
/// - Cannot unpublish Active, Cancelled, or Completed events
/// - Events with registrations can be unpublished (organizer decision)
/// </summary>
public Result Unpublish()
{
    if (Status != EventStatus.Published)
        return Result.Failure("Only published events can be unpublished");

    // Optional: Add restriction for events with registrations
    // if (CurrentRegistrations > 0)
    //     return Result.Failure("Cannot unpublish event with active registrations");

    Status = EventStatus.Draft;
    MarkAsUpdated();

    // Raise domain event for notification (optional)
    RaiseDomainEvent(new EventUnpublishedEvent(Id, DateTime.UtcNow));

    return Result.Success();
}
```

**Create Domain Event** (`src/LankaConnect.Domain/Events/DomainEvents/EventUnpublishedEvent.cs`):

```csharp
namespace LankaConnect.Domain.Events.DomainEvents;

public record EventUnpublishedEvent(
    Guid EventId,
    DateTime UnpublishedAt) : BaseDomainEvent;
```

#### 2. Application Layer

**Create Command** (`src/LankaConnect.Application/Events/Commands/UnpublishEvent/UnpublishEventCommand.cs`):

```csharp
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Events.Commands.UnpublishEvent;

public record UnpublishEventCommand(Guid EventId) : ICommand;
```

**Create Handler** (`UnpublishEventCommandHandler.cs`):

```csharp
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.UnpublishEvent;

public class UnpublishEventCommandHandler : ICommandHandler<UnpublishEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnpublishEventCommandHandler> _logger;

    public UnpublishEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UnpublishEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UnpublishEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unpublishing event {EventId}", request.EventId);

        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("Event not found: {EventId}", request.EventId);
            return Result.Failure("Event not found");
        }

        var unpublishResult = @event.Unpublish();
        if (unpublishResult.IsFailure)
        {
            _logger.LogWarning("Failed to unpublish event {EventId}: {Error}",
                request.EventId, unpublishResult.Error);
            return unpublishResult;
        }

        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Event {EventId} unpublished successfully", request.EventId);
        return Result.Success();
    }
}
```

#### 3. API Layer (`EventsController.cs`)

Add endpoint after line 383 (after PublishEvent):

```csharp
/// <summary>
/// Unpublish an event (return to Draft status) (Owner only)
/// </summary>
[HttpPost("{id:guid}/unpublish")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> UnpublishEvent(Guid id)
{
    Logger.LogInformation("Unpublishing event: {EventId}", id);

    var command = new UnpublishEventCommand(id);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

#### 4. Frontend (`EventManagePage.tsx`)

Update manage page UI (around line 258-267):

```typescript
{/* Publish/Unpublish Button */}
{isDraft && (
  <Button
    onClick={handlePublishEvent}
    disabled={isPublishing}
    className="flex items-center gap-2 text-white"
    style={{ background: '#10B981', color: 'white' }}
  >
    {isPublishing ? 'Publishing...' : 'Publish Event'}
  </Button>
)}

{/* NEW: Unpublish Button - Show for Published events */}
{event.status === EventStatus.Published && (
  <Button
    onClick={handleUnpublishEvent}
    disabled={isUnpublishing}
    className="flex items-center gap-2 text-white"
    style={{ background: '#EF4444', color: 'white' }}
  >
    {isUnpublishing ? 'Unpublishing...' : 'Unpublish Event'}
  </Button>
)}
```

Add handler function (after line 109):

```typescript
const [isUnpublishing, setIsUnpublishing] = useState(false);

const handleUnpublishEvent = async () => {
  if (!event || event.organizerId !== user?.userId) {
    return;
  }

  // Confirmation dialog
  const confirmed = window.confirm(
    'Are you sure you want to unpublish this event? It will return to Draft status and may not be visible to attendees.'
  );

  if (!confirmed) return;

  try {
    setIsUnpublishing(true);
    setError(null);
    await eventsRepository.unpublishEvent(id);
    setIsUnpublishing(false);
    await refetch();
  } catch (err) {
    console.error('Failed to unpublish event:', err);
    setError(err instanceof Error ? err.message : 'Failed to unpublish event.');
    setIsUnpublishing(false);
  }
};
```

Add API method (`events.repository.ts`):

```typescript
async unpublishEvent(eventId: string): Promise<void> {
  await this.apiClient.post(`/Events/${eventId}/unpublish`);
}
```

---

### Issue 3: Fix Post-Creation Navigation

**Simple One-Line Fix**:

**File**: `web/src/presentation/components/features/events/EventCreationForm.tsx`
**Line**: 216

**Change**:
```typescript
// OLD (Session 33):
router.push('/dashboard');

// NEW (Fix):
router.push(`/events/${eventId}/manage`);
```

**Rationale**:
- User completes event creation
- Immediately lands on manage page
- Can upload images, videos, badges
- Can publish event
- Can configure sign-up lists
- **Seamless workflow**

**Alternative** (if dashboard is preferred for some reason):
- Redirect to dashboard
- Show success toast: "Event created! Click 'Manage' to upload media and publish."

But this adds friction. Direct navigation to manage page is better UX.

---

## Risk Assessment

### Issue 1: Event Publication Emails
**Risk**: LOW
**Status**: Fixed (template inserted)
**Mitigation**: Monitor Azure logs for next published event

### Issue 2: Add Unpublish Feature
**Risk**: MEDIUM

**Risks**:
1. **Domain Logic**: Incorrect business rules could allow invalid state transitions
   - **Mitigation**: Add comprehensive unit tests for `Event.Unpublish()`
   - **Mitigation**: Add validation in handler to check organizer ownership

2. **Registration Confusion**: Users with active registrations may be confused if event unpublished
   - **Mitigation**: Add UI warning: "This event has X registrations. Unpublishing may affect attendees."
   - **Mitigation**: Consider business rule: Disallow unpublish if registrations > 0 (configurable)

3. **Email Notifications**: Should registrants be notified of unpublish?
   - **Mitigation**: Decide: Either send "Event Unpublished" email OR no email (silent unpublish)
   - **Recommendation**: Silent unpublish (no email) - organizer may be fixing typos

4. **API Authorization**: Must verify organizer owns event
   - **Mitigation**: Reuse existing authorization pattern from PublishEvent

**Breaking Changes**: NONE - New feature, backward compatible

### Issue 3: Post-Creation Navigation
**Risk**: VERY LOW

**Risks**:
1. **User Confusion**: Users may expect dashboard
   - **Mitigation**: Add "Back to Dashboard" button prominently on manage page (already exists line 247)

2. **URL Bookmarking**: Direct link to `/events/create` would navigate away from dashboard
   - **Mitigation**: Non-issue - this is expected behavior

**Breaking Changes**: NONE - UX improvement only

---

## Testing Strategy

### Issue 1: Event Publication Emails

**Manual Testing**:
1. Create new test event
2. Assign email groups OR ensure event location matches metro areas with newsletter subscribers
3. Publish event
4. Check Azure logs for:
   - "EventPublishedEventHandler INVOKED"
   - "Resolved N unique email recipients"
   - "Event notification emails completed"
5. Verify email received at test email addresses

**Database Verification**:
```sql
-- Check sent emails
SELECT email_to, subject, sent_at, status
FROM communications.sent_emails
WHERE template_code = 'event-published'
ORDER BY sent_at DESC
LIMIT 10;
```

### Issue 2: Add Unpublish Feature

**Unit Tests** (`Event.cs` domain logic):

```csharp
[Fact]
public void Unpublish_PublishedEvent_ShouldSucceed()
{
    // Arrange
    var @event = CreateTestEvent();
    @event.Publish();

    // Act
    var result = @event.Unpublish();

    // Assert
    result.IsSuccess.Should().BeTrue();
    @event.Status.Should().Be(EventStatus.Draft);
}

[Fact]
public void Unpublish_DraftEvent_ShouldFail()
{
    // Arrange
    var @event = CreateTestEvent();

    // Act
    var result = @event.Unpublish();

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("Only published events");
}

[Fact]
public void Unpublish_CancelledEvent_ShouldFail()
{
    // Arrange
    var @event = CreateTestEvent();
    @event.Publish();
    @event.Cancel("Test reason");

    // Act
    var result = @event.Unpublish();

    // Assert
    result.IsFailure.Should().BeTrue();
}

[Fact]
public void Unpublish_ShouldRaiseDomainEvent()
{
    // Arrange
    var @event = CreateTestEvent();
    @event.Publish();
    @event.ClearDomainEvents();

    // Act
    @event.Unpublish();

    // Assert
    @event.DomainEvents.Should().ContainSingle(e => e is EventUnpublishedEvent);
}
```

**Integration Tests** (Command Handler):

```csharp
[Fact]
public async Task UnpublishEvent_ValidEvent_ShouldSucceed()
{
    // Arrange
    var @event = await CreateAndPublishEvent();
    var command = new UnpublishEventCommand(@event.Id);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    var updatedEvent = await _eventRepository.GetByIdAsync(@event.Id);
    updatedEvent.Status.Should().Be(EventStatus.Draft);
}
```

**API Integration Tests**:

```csharp
[Fact]
public async Task POST_Unpublish_PublishedEvent_ReturnsOk()
{
    // Arrange
    var @event = await CreateAndPublishEvent(organizerId: CurrentUserId);

    // Act
    var response = await Client.PostAsync($"/api/Events/{@event.Id}/unpublish", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}

[Fact]
public async Task POST_Unpublish_DraftEvent_ReturnsBadRequest()
{
    // Arrange
    var @event = await CreateDraftEvent(organizerId: CurrentUserId);

    // Act
    var response = await Client.PostAsync($"/api/Events/{@event.Id}/unpublish", null);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

**Frontend E2E Tests** (Playwright/Cypress):

```typescript
test('Organizer can unpublish published event', async ({ page }) => {
  // Arrange: Create and publish event
  const eventId = await createTestEvent();
  await publishEvent(eventId);

  // Navigate to manage page
  await page.goto(`/events/${eventId}/manage`);

  // Act: Click unpublish button
  await page.click('button:has-text("Unpublish Event")');
  await page.click('button:has-text("OK")'); // Confirm dialog

  // Assert: Event status changed to Draft
  await expect(page.locator('text=Draft')).toBeVisible();
  await expect(page.locator('button:has-text("Publish Event")')).toBeVisible();
});
```

### Issue 3: Post-Creation Navigation

**Manual Testing**:
1. Login as EventOrganizer
2. Navigate to `/events/create`
3. Fill out event form with valid data
4. Submit form
5. **Verify**: Redirected to `/events/{eventId}/manage` (NOT `/dashboard`)
6. **Verify**: Can see "Edit Event", "Publish Event", image upload sections

**Automated Test**:

```typescript
test('After creating event, user lands on manage page', async ({ page }) => {
  // Arrange: Login and navigate to create page
  await loginAsOrganizer(page);
  await page.goto('/events/create');

  // Act: Fill form and submit
  await fillEventForm(page, {
    title: 'Test Event',
    description: 'Test Description',
    startDate: '2025-12-25T10:00',
    endDate: '2025-12-25T12:00',
    capacity: 50,
    category: 'Community',
  });
  await page.click('button[type="submit"]');

  // Assert: Redirected to manage page
  await page.waitForURL(/\/events\/[a-f0-9-]+\/manage/);
  await expect(page.locator('h1:has-text("Manage Event")')).toBeVisible();
  await expect(page.locator('button:has-text("Publish Event")')).toBeVisible();
});
```

---

## Implementation Priority

1. **Issue 3** (Post-Creation Navigation) - **HIGH PRIORITY**
   - Simple one-line fix
   - Immediate UX improvement
   - Zero risk
   - Effort: 5 minutes

2. **Issue 2** (Add Unpublish Feature) - **MEDIUM PRIORITY**
   - User-requested feature
   - Moderate complexity
   - Requires testing
   - Effort: 2-3 hours

3. **Issue 1** (Email Verification) - **LOW PRIORITY**
   - Already fixed (template inserted)
   - Just needs verification in Azure logs
   - Effort: 10 minutes (log review)

---

## Appendix: File Locations

### Backend (.NET 8)
- **Domain**: `src/LankaConnect.Domain/Events/Event.cs`
- **Domain Events**: `src/LankaConnect.Domain/Events/DomainEvents/`
- **Commands**: `src/LankaConnect.Application/Events/Commands/`
- **Event Handlers**: `src/LankaConnect.Application/Events/EventHandlers/`
- **API Controller**: `src/LankaConnect.Api/Controllers/EventsController.cs`
- **Email Service**: `src/LankaConnect.Application/Common/Interfaces/IEmailService.cs`

### Frontend (Next.js)
- **Create Page**: `web/src/app/events/create/page.tsx`
- **Manage Page**: `web/src/app/events/[id]/manage/page.tsx`
- **Event Form**: `web/src/presentation/components/features/events/EventCreationForm.tsx`
- **Events Repo**: `web/src/infrastructure/api/repositories/events.repository.ts`

### Database
- **Email Templates**: `communications.email_templates`
- **Sent Emails**: `communications.sent_emails`
- **Migrations**: `src/LankaConnect.Infrastructure/Data/Migrations/`

---

## Conclusion

All three issues have been thoroughly analyzed:

1. **Email Publication** - Root cause identified (missing template), already fixed, needs verification
2. **Unpublish Feature** - Complete design provided with domain logic, API endpoints, UI, and testing strategy
3. **Post-Creation Navigation** - Simple one-line fix to improve UX

**Recommended Implementation Order**: Issue 3 → Issue 2 → Issue 1 (verification)

**Total Estimated Effort**: 3-4 hours including testing

**Risk Level**: LOW - All changes are additive or non-breaking UX improvements
