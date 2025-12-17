# Email Groups Persistence Fix - Implementation Guide

**Phase**: 6A.32
**Issue**: Email groups not persisting despite 200 OK response
**Root Cause**: Domain collection `_emailGroupIds` not synced with EF Core shadow navigation `_emailGroupEntities`
**Solution**: Follow ADR-008 pattern (ChangeTracker API sync)

---

## Quick Reference: The Problem

```
API Call ──> Domain Method ──> _emailGroupIds Updated ✅
                              ──> _emailGroupEntities NOT Updated ❌
                              ──> EF Core Change Tracker: No Changes Detected
                              ──> Junction Table: No Records Created
```

---

## The Fix (3 Steps)

### Step 1: Update `UpdateEventCommandHandler.cs`

**Location**: `src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs`

**Line 264**: Replace existing email group update logic with:

```csharp
// Phase 6A.32: Validate and update email groups
if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
{
    var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

    // Load EmailGroup entities (batch query - already implemented)
    var emailGroups = await _emailGroupRepository.GetByIdsAsync(distinctGroupIds, cancellationToken);

    // Validate existence, ownership, and status (already implemented)
    foreach (var groupId in distinctGroupIds)
    {
        var emailGroup = emailGroups.FirstOrDefault(g => g.Id == groupId);

        if (emailGroup == null)
            return Result.Failure($"Email group with ID {groupId} not found");

        if (emailGroup.OwnerId != @event.OrganizerId)
            return Result.Failure($"You do not have permission to use email group '{emailGroup.Name}'");

        if (!emailGroup.IsActive)
            return Result.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
    }

    // Update domain model (business rules)
    var updateResult = @event.SetEmailGroups(distinctGroupIds);
    if (updateResult.IsFailure)
        return updateResult;

    // ✅ NEW: Sync EF Core shadow navigation (ChangeTracker API pattern)
    var dbContext = _dbContext as DbContext
        ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

    var emailGroupsCollection = dbContext.Entry(@event).Collection("_emailGroupEntities");
    await emailGroupsCollection.LoadAsync(cancellationToken);

    var currentEmailGroups = emailGroupsCollection.CurrentValue as ICollection<Domain.Communications.Entities.EmailGroup>
        ?? new List<Domain.Communications.Entities.EmailGroup>();

    currentEmailGroups.Clear();
    foreach (var emailGroup in emailGroups)
    {
        currentEmailGroups.Add(emailGroup);
    }
}
else if (request.EmailGroupIds != null && !request.EmailGroupIds.Any())
{
    // Empty list - clear all email groups
    @event.ClearEmailGroups();

    // ✅ NEW: Also clear EF Core shadow navigation
    var dbContext = _dbContext as DbContext
        ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

    var emailGroupsCollection = dbContext.Entry(@event).Collection("_emailGroupEntities");
    await emailGroupsCollection.LoadAsync(cancellationToken);

    var currentEmailGroups = emailGroupsCollection.CurrentValue as ICollection<Domain.Communications.Entities.EmailGroup>
        ?? new List<Domain.Communications.Entities.EmailGroup>();

    currentEmailGroups.Clear();
}
```

**Required Using Statements**:
```csharp
using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Communications.Entities;
```

---

### Step 2: Update `CreateEventCommandHandler.cs`

**Location**: `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs`

**After line where event is created**: Add email group sync logic:

```csharp
// Phase 6A.32: Assign email groups if provided
if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
{
    var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

    // Load and validate email groups
    var emailGroups = await _emailGroupRepository.GetByIdsAsync(distinctGroupIds, cancellationToken);

    foreach (var groupId in distinctGroupIds)
    {
        var emailGroup = emailGroups.FirstOrDefault(g => g.Id == groupId);

        if (emailGroup == null)
            return Result<Guid>.Failure($"Email group with ID {groupId} not found");

        if (emailGroup.OwnerId != request.OrganizerId)
            return Result<Guid>.Failure($"You do not have permission to use email group '{emailGroup.Name}'");

        if (!emailGroup.IsActive)
            return Result<Guid>.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
    }

    // Update domain model
    var assignResult = @event.AssignEmailGroups(distinctGroupIds);
    if (assignResult.IsFailure)
        return Result<Guid>.Failure(assignResult.Error);

    // ✅ NEW: Sync EF Core shadow navigation
    var dbContext = _dbContext as DbContext
        ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

    var emailGroupsCollection = dbContext.Entry(@event).Collection("_emailGroupEntities");
    await emailGroupsCollection.LoadAsync(cancellationToken);

    var currentEmailGroups = emailGroupsCollection.CurrentValue as ICollection<EmailGroup>
        ?? new List<EmailGroup>();

    foreach (var emailGroup in emailGroups)
    {
        currentEmailGroups.Add(emailGroup);
    }
}
```

---

### Step 3: Verify Query Handlers Load Shadow Navigation

**Location**: `src/LankaConnect.Application/Events/Queries/GetEventById/GetEventByIdQueryHandler.cs`

**After loading event**: Ensure shadow navigation is loaded:

```csharp
var @event = await _dbContext.Events
    .Include(e => e.Images)
    .Include(e => e.Videos)
    .Include(e => e.SignUpLists)
    .ThenInclude(s => s.Items)
    .AsSplitQuery()
    .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

if (@event == null)
    return Result<EventDto>.Failure("Event not found");

// ✅ NEW: Explicitly load email groups shadow navigation
var dbContext = _dbContext as DbContext
    ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

var emailGroupsCollection = dbContext.Entry(@event).Collection("_emailGroupEntities");
await emailGroupsCollection.LoadAsync(cancellationToken);

// Now EmailGroupIds property will return correct values
var dto = _mapper.Map<EventDto>(@event);
```

---

## Why This Pattern?

### Architectural Justification

1. **Established Pattern**: Already used for `User.PreferredMetroAreas` (ADR-008)
2. **Clean Architecture**: Domain works with IDs (no EF Core dependencies)
3. **Explicit Behavior**: Handler clearly shows sync logic (no magic)
4. **EF Core Best Practice**: Uses ChangeTracker API (not reflection)
5. **Maintainable**: Consistent with existing codebase

### Comparison to Alternatives

| Pattern | Domain Purity | Explicit | EF Core Native | Used in Codebase |
|---------|---------------|----------|----------------|------------------|
| ChangeTracker API ✅ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes (User.PreferredMetroAreas) |
| Domain accepts entities ❌ | ❌ No | ✅ Yes | ✅ Yes | ❌ No |
| DbContext override ❌ | ✅ Yes | ❌ No (hidden) | ✅ Yes | ❌ No |
| Reflection in handler ❌ | ✅ Yes | ⚠️ Sort of | ❌ No | ❌ No |
| Remove _emailGroupIds ❌ | ❌ No | ✅ Yes | ✅ Yes | ❌ No |

---

## Testing Checklist

### Manual Testing

1. **Create Event with Email Groups**:
   ```bash
   POST /api/events
   {
     "title": "Test Event",
     "emailGroupIds": ["guid1", "guid2"]
   }
   ```
   - Verify `event_email_groups` table has 2 records ✅

2. **Update Event Email Groups**:
   ```bash
   PUT /api/events/{id}
   {
     "emailGroupIds": ["guid3", "guid4"]
   }
   ```
   - Verify old records removed, new records added ✅

3. **Clear Email Groups**:
   ```bash
   PUT /api/events/{id}
   {
     "emailGroupIds": []
   }
   ```
   - Verify all records deleted from junction table ✅

4. **Get Event Details**:
   ```bash
   GET /api/events/{id}
   ```
   - Verify `emailGroupIds` array returned correctly ✅

### SQL Verification

```sql
-- Check junction table records
SELECT * FROM event_email_groups WHERE event_id = 'your-event-id';

-- Verify foreign key integrity
SELECT
    e.id as event_id,
    e.title,
    eg.id as email_group_id,
    eg.name as group_name
FROM events e
LEFT JOIN event_email_groups eeg ON e.id = eeg.event_id
LEFT JOIN email_groups eg ON eeg.email_group_id = eg.id
WHERE e.id = 'your-event-id';
```

---

## Rollback Plan

### If Implementation Fails

1. **Comment out ChangeTracker sync logic** in handlers
2. **Junction table structure remains** (no data loss)
3. **Domain model unchanged** (no rollback needed)
4. **Frontend continues working** (returns empty email groups array)

### Revert Command

```bash
# If needed, revert to previous commit
git revert HEAD
git push origin develop
```

---

## Performance Considerations

### Batch Query ✅ (Already Implemented)

```csharp
// GOOD: Single query for all email groups
var emailGroups = await _emailGroupRepository.GetByIdsAsync(distinctGroupIds, cancellationToken);

// BAD: N+1 queries (avoided)
foreach (var groupId in distinctGroupIds)
{
    var emailGroup = await _emailGroupRepository.GetByIdAsync(groupId);
}
```

### Loading Strategy

- **Lazy Loading**: Disabled (intentional)
- **Explicit Loading**: Using ChangeTracker API ✅
- **Eager Loading**: Consider for read queries with `.Include()`

---

## References

1. **ADR-009**: Email Groups Many-to-Many Architecture
2. **ADR-008**: User Preferred Metro Areas (precedent pattern)
3. **Existing Code**: `UpdateUserPreferredMetroAreasCommandHandler.cs:77-89`
4. **EF Core Docs**: [Change Tracker API](https://docs.microsoft.com/en-us/ef/core/change-tracking/)

---

## Completion Checklist

- [ ] Update `UpdateEventCommandHandler.cs` with ChangeTracker sync
- [ ] Update `CreateEventCommandHandler.cs` with ChangeTracker sync
- [ ] Update `GetEventByIdQueryHandler.cs` to load shadow navigation
- [ ] Add using statements: `Microsoft.EntityFrameworkCore`, `LankaConnect.Domain.Communications.Entities`
- [ ] Test create event with email groups
- [ ] Test update event email groups
- [ ] Test clear email groups
- [ ] Verify SQL junction table records
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update PHASE_6A_MASTER_INDEX.md
- [ ] Mark Phase 6A.32 as complete

---

**Estimated Implementation Time**: 30-45 minutes
**Risk Level**: Low (established pattern, non-breaking change)
**Testing Required**: Manual + Integration tests
