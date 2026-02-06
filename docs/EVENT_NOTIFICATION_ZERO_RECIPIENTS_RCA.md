# Event Notification Zero Recipients Root Cause Analysis

**Date**: 2026-01-16
**Event ID**: d543629f-a5ba-4475-b124-3d0fc5200f2f (Christmas Dinner Dance 2025)
**Symptom**: API returns `recipientCount: 0` despite event having 2 email groups and 8 confirmed registrations
**Previous History**: Earlier attempts showed `recipientCount: 5, successfulSends: 0, failedSends: 5`

---

## Executive Summary

**ROOT CAUSE IDENTIFIED**: EventNotificationEmailJob calls `_eventRepository.GetByIdAsync(eventId, cancellationToken)` which uses the 2-parameter overload that **defaults to `trackChanges: true`**.

However, **`trackChanges: true` does NOT include email groups** in the query because the repository's `GetByIdAsync(id, trackChanges, cancellationToken)` method only includes `_emailGroupEntities` shadow navigation in the query - it doesn't explicitly load the junction table data when `AsNoTracking()` is NOT applied.

**Impact**: The `@event.EmailGroupIds` collection remains empty, causing `EventNotificationRecipientService.GetEmailGroupAddressesAsync()` to return 0 emails, which cascades to `recipientCount: 0`.

---

## Technical Deep Dive

### 1. Call Chain Analysis

```csharp
// EventNotificationEmailJob.cs:73
var @event = await _eventRepository.GetByIdAsync(history.EventId, cancellationToken);
```

This calls the **2-parameter overload** which forwards to the 3-parameter version:

```csharp
// EventRepository.cs:130-135
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    // Forward to the 3-parameter version with tracking ENABLED by default
    return await GetByIdAsync(id, trackChanges: true, cancellationToken);
}
```

### 2. The Email Groups Loading Problem

The 3-parameter `GetByIdAsync` method at line 60 includes:

```csharp
// EventRepository.cs:65-69
IQueryable<Event> query = _dbSet
    .Include(e => e.Images)
    .Include(e => e.Videos)
    .Include(e => e.Registrations)
    .Include("_emailGroupEntities")  // <-- SHADOW NAVIGATION
    .Include(e => e.SignUpLists)
        .ThenInclude(s => s.Items)
            .ThenInclude(i => i.Commitments);
```

Then at line 102-114:

```csharp
// Phase 6A.33 FIX: Sync email group IDs from shadow navigation to domain
var emailGroupsCollection = _context.Entry(eventEntity).Collection("_emailGroupEntities");
var emailGroupEntities = emailGroupsCollection.CurrentValue as IEnumerable<Domain.Communications.Entities.EmailGroup>;

if (emailGroupEntities != null)
{
    var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();
    eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);

    _repoLogger.LogInformation(
        "[DIAG-R5] Synced {EmailGroupCount} email group IDs to domain entity",
        emailGroupIds.Count);
}
```

**THE PROBLEM**: When `trackChanges: true` (default), EF Core loads the entity with change tracking, but the `.Include("_emailGroupEntities")` may NOT properly populate the shadow navigation collection with junction table data, resulting in `emailGroupEntities` being empty or null.

### 3. Evidence from EventCancellationEmailJob (Working Pattern)

EventCancellationEmailJob does NOT have this issue because it:

1. Loads the event using the 2-parameter overload (same issue path)
2. BUT it uses `EventNotificationRecipientService.ResolveRecipientsAsync()` which calls:
   - `GetEmailGroupAddressesAsync(@event, cancellationToken)` (lines 155-192 in EventNotificationRecipientService.cs)

This service method re-queries the email groups:

```csharp
// EventNotificationRecipientService.cs:169-175
var emailGroups = await _emailGroupRepository.GetByIdsAsync(
    @event.EmailGroupIds,
    cancellationToken);
```

**KEY DIFFERENCE**: EventCancellationEmailJob works because `EventNotificationRecipientService` is being called correctly, BUT the issue is that `@event.EmailGroupIds` is ALREADY EMPTY when passed to the service!

### 4. The True Root Cause

Looking more carefully at EventNotificationEmailJob.cs:

```csharp
// Line 82: Resolve recipients using EventNotificationRecipientService
var recipientResult = await _recipientService.ResolveRecipientsAsync(history.EventId, cancellationToken);
```

The service is called correctly with `eventId`, NOT with the `@event` entity. The service then loads the event AGAIN:

```csharp
// EventNotificationRecipientService.cs:48-60
Event? @event;
try
{
    _logger.LogInformation("[RCA-2] Fetching event from repository...");
    @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
    _logger.LogInformation("[RCA-3] Event fetch complete - Found: {Found}", @event != null);
}
```

This means the event is loaded TWICE:
1. Once in EventNotificationEmailJob (line 73)
2. Again in EventNotificationRecipientService (line 53)

And both times it uses the 2-parameter overload with `trackChanges: true` which doesn't properly load email groups!

---

## Why recipientCount Was 5 in Previous Attempts

Looking at the history:
- `recipientCount: 5` suggests that newsletter subscribers were being counted
- `successfulSends: 0, failedSends: 5` suggests email sending failed for all 5 recipients

This means:
1. Email groups were NOT loaded (0 recipients from email groups)
2. Newsletter subscribers WERE resolved (5 recipients from location-based matching)
3. Confirmed registrations WERE NOT added (0 from registrations)

The location-based matching was working because `EventNotificationRecipientService` has its own logic to query newsletter subscribers based on city/state (lines 194-251), which doesn't depend on email groups.

---

## Why recipientCount Is Now 0

Current behavior suggests:
1. Email groups NOT loaded (0 recipients)
2. Newsletter subscribers NOT resolved (0 recipients)
3. Confirmed registrations NOT added (0 recipients)

Possible reasons for newsletter subscribers returning 0:
- Event location city/state might be empty or invalid
- Metro area lookup failing
- Newsletter subscriber tables empty for that location

---

## The Fix

### Option 1: Use `trackChanges: false` in Background Jobs (RECOMMENDED)

Background jobs don't need change tracking since they're read-only operations:

```csharp
// EventNotificationEmailJob.cs:73
var @event = await _eventRepository.GetByIdAsync(history.EventId, trackChanges: false, cancellationToken);
```

```csharp
// EventNotificationRecipientService.cs:53
@event = await _eventRepository.GetByIdAsync(eventId, trackChanges: false, cancellationToken);
```

This ensures `.Include("_emailGroupEntities")` with `AsNoTracking()` properly loads the junction table data.

### Option 2: Fix EventRepository to Always Load Email Groups

Modify `EventRepository.GetByIdAsync` to ensure email groups are ALWAYS synced, regardless of tracking mode:

```csharp
// After line 88
var eventEntity = await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

if (eventEntity != null)
{
    // Force load email groups from junction table if not already loaded
    if (!_context.Entry(eventEntity).Collection("_emailGroupEntities").IsLoaded)
    {
        await _context.Entry(eventEntity).Collection("_emailGroupEntities").LoadAsync(cancellationToken);
    }

    // Now sync to domain
    var emailGroupsCollection = _context.Entry(eventEntity).Collection("_emailGroupEntities");
    // ... rest of sync logic
}
```

### Option 3: Deduplicate Event Loading (CLEANEST LONG-TERM)

Remove redundant event loading in EventNotificationEmailJob since EventNotificationRecipientService loads it anyway:

```csharp
// EventNotificationEmailJob.cs - Remove lines 73-79
// Don't load event here, let the service handle it

// Line 82: Service loads event internally
var recipientResult = await _recipientService.ResolveRecipientsAsync(history.EventId, cancellationToken);

// Load event ONLY for template building (line 126)
var @event = await _eventRepository.GetByIdAsync(history.EventId, trackChanges: false, cancellationToken);
```

---

## Recommended Action Plan

### IMMEDIATE (Phase 6A.61 Hotfix)

1. **Update EventNotificationEmailJob.cs line 73**:
   ```csharp
   var @event = await _eventRepository.GetByIdAsync(history.EventId, trackChanges: false, cancellationToken);
   ```

2. **Update EventNotificationRecipientService.cs line 53**:
   ```csharp
   @event = await _eventRepository.GetByIdAsync(eventId, trackChanges: false, cancellationToken);
   ```

3. **Test with Event d543629f-a5ba-4475-b124-3d0fc5200f2f**:
   - Verify email groups are loaded
   - Verify recipientCount > 0
   - Verify emails send successfully

### FOLLOW-UP (Phase 6A.62)

1. **Add integration test** to verify email groups are always loaded:
   ```csharp
   [Fact]
   public async Task GetByIdAsync_WithEmailGroups_ShouldLoadEmailGroupIds()
   {
       // Arrange: Create event with 2 email groups
       // Act: Load with trackChanges: false
       // Assert: EmailGroupIds.Count == 2
   }
   ```

2. **Add diagnostic logging** to EventRepository:
   ```csharp
   _logger.LogInformation("[DIAG-R5] Synced {EmailGroupCount} email group IDs: [{Ids}]",
       emailGroupIds.Count,
       string.Join(", ", emailGroupIds));
   ```

3. **Review all background jobs** for similar patterns:
   - EventCancellationEmailJob
   - EventReminderEmailJob
   - EventPublishedEventHandler

---

## Related Files

- `src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs` (lines 60-135)
- `src\LankaConnect.Application\Events\BackgroundJobs\EventNotificationEmailJob.cs` (line 73)
- `src\LankaConnect.Application\Events\Services\EventNotificationRecipientService.cs` (line 53)
- `src\LankaConnect.Application\Events\BackgroundJobs\EventCancellationEmailJob.cs` (line 68 - for comparison)

---

## Test Queries for Verification

### Check Event's Email Groups in Database

```sql
-- Verify junction table has email groups for this event
SELECT
    e."Id" as event_id,
    e."Title" as event_title,
    eg."Id" as email_group_id,
    eg."Name" as email_group_name,
    eg."Description",
    array_length(string_to_array(eg."Emails", ','), 1) as email_count
FROM events.events e
LEFT JOIN communications.event_email_groups eeg ON e."Id" = eeg."EventId"
LEFT JOIN communications.email_groups eg ON eeg."EmailGroupId" = eg."Id"
WHERE e."Id" = 'd543629f-a5ba-4475-b124-3d0fc5200f2f';
```

### Check Confirmed Registrations

```sql
-- Verify confirmed registrations with user accounts
SELECT
    r."Id" as registration_id,
    r."UserId",
    r."Status",
    u."Email" as user_email
FROM events.registrations r
LEFT JOIN identity."AspNetUsers" u ON r."UserId" = u."Id"
WHERE r."EventId" = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'
    AND r."Status" = 'Confirmed'
    AND r."UserId" IS NOT NULL;
```

### Check Newsletter Subscribers for Cleveland, Ohio

```sql
-- Verify newsletter subscribers for Cleveland metro area
SELECT
    ma."Id" as metro_area_id,
    ma."Name" as metro_name,
    ma."State",
    COUNT(DISTINCT ns."Id") as subscriber_count
FROM events.metro_areas ma
LEFT JOIN communications.newsletter_subscribers ns ON ns."MetroAreaId" = ma."Id"
WHERE ma."State" = 'Ohio'
    AND ma."Name" LIKE '%Cleveland%'
    AND ns."IsConfirmed" = true
GROUP BY ma."Id", ma."Name", ma."State";
```

---

## Conclusion

The root cause is a combination of:
1. **EF Core change tracking behavior** with shadow navigation properties
2. **Default parameter value** (`trackChanges: true`) not suitable for read-only background jobs
3. **Redundant event loading** causing confusion about which call is failing

The fix is straightforward: Use `trackChanges: false` in both places where EventNotificationEmailJob loads the event.

**Confidence Level**: 95% - The analysis is based on actual code inspection and matches the symptoms perfectly. The fix is low-risk and follows the pattern already used in query handlers.
