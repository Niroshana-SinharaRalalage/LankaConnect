# ADR-009: Event-EmailGroup Many-to-Many Relationship Architecture

**Date**: 2025-12-16
**Status**: Recommended Solution
**Context**: Phase 6A.32 - Email Groups Integration
**Related**: ADR-008 (User Preferred Metro Areas)

---

## Executive Summary

**Problem**: Email groups are not persisting to the database despite successful API calls (200 OK). Root cause is a mismatch between domain model and EF Core persistence requirements.

**Root Cause**: The Event entity has TWO separate collections:
1. `_emailGroupIds` (List<Guid>) - Used by domain methods ✅
2. `_emailGroupEntities` (List<EmailGroup>) - Required by EF Core for many-to-many ❌ (never populated)

**Recommended Solution**: Follow the established ADR-008 pattern used successfully for `User.PreferredMetroAreas` - use EF Core ChangeTracker API to sync shadow navigation property in application layer.

---

## Context

### Current Implementation (Broken)

**Domain Layer** (`Event.cs:19-20`):
```csharp
private readonly List<Guid> _emailGroupIds = new();
private readonly List<EmailGroup> _emailGroupEntities = new(); // Shadow navigation for EF Core

public IReadOnlyList<Guid> EmailGroupIds => _emailGroupIds.AsReadOnly();

public Result SetEmailGroups(IEnumerable<Guid> emailGroupIds)
{
    // PROBLEM: Only updates _emailGroupIds, not _emailGroupEntities
    _emailGroupIds.Clear();
    _emailGroupIds.AddRange(emailGroupIds.Distinct());
    MarkAsUpdated();
    return Result.Success();
}
```

**Infrastructure Layer** (`EventConfiguration.cs:244`):
```csharp
builder
    .HasMany<EmailGroup>("_emailGroupEntities")
    .WithMany()
    .UsingEntity<Dictionary<string, object>>(
        "event_email_groups",
        j => j.HasOne<EmailGroup>().WithMany().HasForeignKey("email_group_id"),
        j => j.HasOne<Event>().WithMany().HasForeignKey("event_id")
    );
```

**Application Layer** (`UpdateEventCommandHandler.cs:265`):
```csharp
// PROBLEM: Only modifies _emailGroupIds collection
var updateResult = @event.SetEmailGroups(distinctGroupIds);
// EF Core has NOTHING to persist to junction table!
```

### Why This Fails

1. Domain method `SetEmailGroups()` only updates `_emailGroupIds`
2. EF Core change tracking monitors `_emailGroupEntities` for many-to-many relationships
3. Since `_emailGroupEntities` is never modified, no junction table records are created
4. Migration created successfully ✅
5. API returns 200 OK ✅
6. But data doesn't persist ❌

---

## Architectural Constraints

### Clean Architecture Principles

1. **Domain Independence**: Domain layer cannot reference infrastructure (no EF Core dependencies)
2. **Encapsulation**: Domain methods should control business logic
3. **Dependency Inversion**: Infrastructure adapts domain to persistence needs
4. **Separation of Concerns**: Business rules ≠ persistence mechanics

### DDD Guidelines

- Aggregates control consistency boundaries
- Value objects are immutable
- Domain services contain business logic that doesn't belong to entities
- Repositories provide persistence abstraction

---

## Decision: Follow ADR-008 Pattern (Recommended)

### Architecture Overview

**Pattern Name**: Shadow Navigation Synchronization via ChangeTracker API

**Precedent**: `User.PreferredMetroAreas` (Phase 5B + 6A.9 Fix)

### Implementation Strategy

1. **Domain Layer**: Manages business logic with `List<Guid>`
2. **Infrastructure Layer**: Declares shadow navigation property `_emailGroupEntities`
3. **Application Layer**: Syncs shadow navigation using EF Core ChangeTracker API

### Why This Pattern?

This is the **established architectural pattern** in this codebase:

| Aspect | User.PreferredMetroAreas | Event.EmailGroups |
|--------|-------------------------|-------------------|
| Domain Collection | `_preferredMetroAreaIds` | `_emailGroupIds` |
| Shadow Navigation | `_preferredMetroAreaEntities` | `_emailGroupEntities` |
| EF Core Config | `UserConfiguration.cs:280` | `EventConfiguration.cs:244` |
| Handler Sync | `UpdateUserPreferredMetroAreasCommandHandler.cs:77-89` | **MISSING** ❌ |
| ADR Documentation | ADR-008 | ADR-009 (this doc) |

**Consistency**: Using the same pattern ensures maintainability and developer understanding.

---

## Solution Implementation

### Step 1: Domain Layer (No Changes Needed)

The domain layer is already correct:

```csharp
// src/LankaConnect.Domain/Events/Event.cs
private readonly List<Guid> _emailGroupIds = new();
private readonly List<EmailGroup> _emailGroupEntities = new(); // Shadow navigation

public IReadOnlyList<Guid> EmailGroupIds => _emailGroupIds.AsReadOnly();

public Result SetEmailGroups(IEnumerable<Guid> emailGroupIds)
{
    if (emailGroupIds == null)
        return Result.Failure("Email group IDs cannot be null");

    var groupList = emailGroupIds.Where(id => id != Guid.Empty).Distinct().ToList();
    _emailGroupIds.Clear();
    _emailGroupIds.AddRange(groupList);
    MarkAsUpdated();
    return Result.Success();
}
```

**Design Rationale**:
- Domain works with IDs (lightweight, business logic focus)
- No EF Core dependencies
- Business rules enforced (distinct IDs, no empty GUIDs)

### Step 2: Infrastructure Layer (Already Correct)

```csharp
// src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs
builder
    .HasMany<EmailGroup>("_emailGroupEntities")
    .WithMany()
    .UsingEntity<Dictionary<string, object>>(
        "event_email_groups",
        j => j.HasOne<EmailGroup>().WithMany()
            .HasForeignKey("email_group_id")
            .OnDelete(DeleteBehavior.Cascade),
        j => j.HasOne<Event>().WithMany()
            .HasForeignKey("event_id")
            .OnDelete(DeleteBehavior.Cascade)
    );
```

**Design Rationale**:
- Shadow navigation property (string-based configuration)
- Junction table manages many-to-many relationship
- Cascade delete on both FKs (safe with soft delete pattern)

### Step 3: Application Layer (NEEDS FIX)

**Current Implementation** (Broken):
```csharp
// src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs
var updateResult = @event.SetEmailGroups(distinctGroupIds);
// PROBLEM: EF Core change tracking doesn't detect changes in _emailGroupEntities
```

**Corrected Implementation** (Following ADR-008):
```csharp
// Phase 6A.32: Validate and update email groups
if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
{
    var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

    // Step 1: Load EmailGroup entities (application layer validation)
    var emailGroups = await _emailGroupRepository.GetByIdsAsync(distinctGroupIds, cancellationToken);

    // Step 2: Validate existence, ownership, and status
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

    // Step 3: Update domain model (business rules validation)
    var updateResult = @event.SetEmailGroups(distinctGroupIds);
    if (updateResult.IsFailure)
        return updateResult;

    // Step 4: CRITICAL FIX - Sync EF Core shadow navigation using ChangeTracker API
    // This is the CORRECT pattern per ADR-008 (User.PreferredMetroAreas)
    var dbContext = _dbContext as DbContext
        ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

    var emailGroupsCollection = dbContext.Entry(@event).Collection("_emailGroupEntities");
    await emailGroupsCollection.LoadAsync(cancellationToken); // Ensure tracked

    var currentEmailGroups = emailGroupsCollection.CurrentValue as ICollection<EmailGroup>
        ?? new List<EmailGroup>();

    // Clear existing and add new entities using EF Core's tracked collection
    currentEmailGroups.Clear();
    foreach (var emailGroup in emailGroups)
    {
        currentEmailGroups.Add(emailGroup);
    }
}
else if (request.EmailGroupIds != null && !request.EmailGroupIds.Any())
{
    // Empty list provided - clear all email groups
    @event.ClearEmailGroups();

    // Also clear EF Core shadow navigation
    var dbContext = _dbContext as DbContext
        ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

    var emailGroupsCollection = dbContext.Entry(@event).Collection("_emailGroupEntities");
    await emailGroupsCollection.LoadAsync(cancellationToken);

    var currentEmailGroups = emailGroupsCollection.CurrentValue as ICollection<EmailGroup>
        ?? new List<EmailGroup>();

    currentEmailGroups.Clear();
}
// If null, don't modify existing email groups
```

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                        │
│  UpdateEventRequest { EmailGroupIds: [guid1, guid2] }          │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                       APPLICATION LAYER                          │
│  UpdateEventCommandHandler                                       │
│  ┌──────────────────────────────────────────────────────┐      │
│  │ 1. Load EmailGroup entities (batch query)           │      │
│  │ 2. Validate business rules (ownership, status)       │      │
│  │ 3. Call domain method: @event.SetEmailGroups(ids)  │      │
│  │ 4. Sync shadow navigation via ChangeTracker API     │      │
│  │    - dbContext.Entry(@event).Collection(...)        │      │
│  │    - currentEmailGroups.Clear()                      │      │
│  │    - currentEmailGroups.Add(emailGroup)             │      │
│  └──────────────────────────────────────────────────────┘      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER                             │
│  Event Aggregate                                                 │
│  ┌──────────────────────────────────────────────────────┐      │
│  │ _emailGroupIds: List<Guid>          (Business Logic)│      │
│  │ _emailGroupEntities: List<EmailGroup> (EF Core Only)│      │
│  │                                                       │      │
│  │ SetEmailGroups(ids):                                 │      │
│  │   - Validates distinct IDs                           │      │
│  │   - Clears _emailGroupIds                           │      │
│  │   - Adds new IDs                                     │      │
│  │   - MarkAsUpdated()                                  │      │
│  │   - Returns Result                                   │      │
│  └──────────────────────────────────────────────────────┘      │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                     INFRASTRUCTURE LAYER                         │
│  EventConfiguration (EF Core)                                    │
│  ┌──────────────────────────────────────────────────────┐      │
│  │ HasMany<EmailGroup>("_emailGroupEntities")          │      │
│  │   .WithMany()                                        │      │
│  │   .UsingEntity("event_email_groups")                │      │
│  │                                                       │      │
│  │ Junction Table: event_email_groups                   │      │
│  │   - event_id (FK)                                    │      │
│  │   - email_group_id (FK)                             │      │
│  │   - assigned_at (audit)                              │      │
│  └──────────────────────────────────────────────────────┘      │
│                                                                  │
│  EF Core ChangeTracker (monitors _emailGroupEntities)           │
│   ┌─────────────────────────────────────────────┐             │
│   │ Added:    Creates junction table records    │             │
│   │ Removed:  Deletes junction table records    │             │
│   │ Modified: Updates junction table records    │             │
│   └─────────────────────────────────────────────┘             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Comparison with Other Patterns

### Option A: Domain Method Accepts Entities (Rejected)

```csharp
public Result SetEmailGroupEntities(IEnumerable<EmailGroup> emailGroups)
{
    _emailGroupEntities.Clear();
    _emailGroupEntities.AddRange(emailGroups);
    // Also sync IDs
    _emailGroupIds.Clear();
    _emailGroupIds.AddRange(emailGroups.Select(g => g.Id));
}
```

**Pros**: Simple, domain controls both collections
**Cons**: Domain depends on infrastructure entity (breaks Clean Architecture)
**Verdict**: ❌ Violates architectural boundaries

---

### Option B: DbContext SaveChanges Override (Rejected)

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
{
    // Sync _emailGroupEntities from _emailGroupIds before save
    foreach (var entry in ChangeTracker.Entries<Event>())
    {
        if (entry.State == EntityState.Modified)
        {
            var eventEntity = entry.Entity;
            var currentIds = eventEntity.EmailGroupIds;
            var groups = await EmailGroups.Where(g => currentIds.Contains(g.Id)).ToListAsync();

            // Sync via reflection
            var entitiesField = typeof(Event).GetField("_emailGroupEntities", BindingFlags.NonPublic);
            ((List<EmailGroup>)entitiesField.GetValue(eventEntity)).Clear();
            ((List<EmailGroup>)entitiesField.GetValue(eventEntity)).AddRange(groups);
        }
    }
    return await base.SaveChangesAsync(cancellationToken);
}
```

**Pros**: Automatic sync, handler stays simple
**Cons**: Hidden magic, uses reflection, hard to debug, breaks transparency
**Verdict**: ❌ Too clever, violates explicit behavior principle

---

### Option C: Handler Manipulates Field Directly (Rejected)

```csharp
// In handler after domain method call
var entitiesField = typeof(Event).GetField("_emailGroupEntities", BindingFlags.NonPublic);
var entitiesList = (List<EmailGroup>)entitiesField.GetValue(@event);
entitiesList.Clear();
entitiesList.AddRange(emailGroups);
```

**Pros**: Explicit, handler controls sync
**Cons**: Breaks encapsulation, uses reflection, fragile
**Verdict**: ❌ Violates encapsulation principle

---

### Option D: Remove _emailGroupIds (Rejected)

```csharp
// Domain model changes:
private readonly List<EmailGroup> _emailGroupEntities = new();
public IReadOnlyList<Guid> EmailGroupIds => _emailGroupEntities.Select(g => g.Id).ToList();

public Result SetEmailGroupEntities(IEnumerable<EmailGroup> emailGroups) { ... }
```

**Pros**: Single source of truth
**Cons**: Domain depends on infrastructure entity, breaks DDD layering
**Verdict**: ❌ Domain should work with IDs (value types), not entities

---

### Option E: Explicit Join Entity (Rejected)

```csharp
public class EventEmailGroup
{
    public Guid EventId { get; set; }
    public Guid EmailGroupId { get; set; }
    public DateTime AssignedAt { get; set; }
}

private readonly List<EventEmailGroup> _eventEmailGroups = new();
```

**Pros**: Explicit relationship, domain controls join entity
**Cons**: More boilerplate, join entity might be infrastructure concern
**Verdict**: ⚠️ Valid but unnecessary complexity for simple many-to-many

---

### ✅ Recommended: ChangeTracker API Sync (ADR-008 Pattern)

**Why This Pattern Wins**:

1. **Established Precedent**: Already used for `User.PreferredMetroAreas` ✅
2. **Clean Architecture**: Domain stays pure (works with IDs) ✅
3. **Explicit**: Handler clearly shows sync logic ✅
4. **EF Core Best Practice**: Uses ChangeTracker API (not reflection) ✅
5. **Maintainable**: Follows existing codebase patterns ✅
6. **Testable**: Can mock ChangeTracker behavior ✅

---

## Testing Strategy

### Unit Tests (Domain Layer)

```csharp
[Fact]
public void SetEmailGroups_Should_Update_EmailGroupIds()
{
    // Arrange
    var @event = CreateTestEvent();
    var groupIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

    // Act
    var result = @event.SetEmailGroups(groupIds);

    // Assert
    result.IsSuccess.Should().BeTrue();
    @event.EmailGroupIds.Should().BeEquivalentTo(groupIds);
}

[Fact]
public void SetEmailGroups_Should_Remove_Duplicates()
{
    // Arrange
    var @event = CreateTestEvent();
    var groupId = Guid.NewGuid();
    var groupIds = new List<Guid> { groupId, groupId };

    // Act
    var result = @event.SetEmailGroups(groupIds);

    // Assert
    @event.EmailGroupIds.Should().HaveCount(1);
    @event.EmailGroupIds.Should().Contain(groupId);
}
```

### Integration Tests (Application Layer)

```csharp
[Fact]
public async Task UpdateEvent_Should_Persist_EmailGroups_To_Database()
{
    // Arrange
    var organizer = await CreateTestUser(UserRole.EventOrganizer);
    var emailGroup1 = await CreateTestEmailGroup(organizer.Id, "Group 1");
    var emailGroup2 = await CreateTestEmailGroup(organizer.Id, "Group 2");
    var @event = await CreateTestEvent(organizer.Id);

    var command = new UpdateEventCommand
    {
        EventId = @event.Id,
        EmailGroupIds = new[] { emailGroup1.Id, emailGroup2.Id }
    };

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    // Verify junction table records created
    var junctionRecords = await _dbContext.Database
        .SqlQueryRaw<JunctionRecord>(
            "SELECT * FROM event_email_groups WHERE event_id = {0}",
            @event.Id)
        .ToListAsync();

    junctionRecords.Should().HaveCount(2);
    junctionRecords.Select(j => j.EmailGroupId)
        .Should().BeEquivalentTo(new[] { emailGroup1.Id, emailGroup2.Id });
}
```

---

## Migration Plan

### Phase 1: Fix UpdateEventCommandHandler ✅

1. Add ChangeTracker API sync logic after domain method call
2. Handle both update and clear scenarios
3. Follow ADR-008 pattern exactly

### Phase 2: Add CreateEventCommandHandler Support

```csharp
// Similar sync logic needed for event creation
if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
{
    var emailGroups = await _emailGroupRepository.GetByIdsAsync(request.EmailGroupIds, cancellationToken);

    // Sync shadow navigation after event creation
    var emailGroupsCollection = dbContext.Entry(newEvent).Collection("_emailGroupEntities");
    await emailGroupsCollection.LoadAsync(cancellationToken);
    var currentEmailGroups = emailGroupsCollection.CurrentValue as ICollection<EmailGroup>;

    foreach (var emailGroup in emailGroups)
    {
        currentEmailGroups.Add(emailGroup);
    }
}
```

### Phase 3: Verify GetEventById Query

Ensure query handler loads email groups correctly:

```csharp
var @event = await _dbContext.Events
    .Include(e => e.Images)
    .Include(e => e.Videos)
    .Include(e => e.SignUpLists)
    .ThenInclude(s => s.Items)
    // CRITICAL: Load email groups shadow navigation
    .AsSplitQuery()
    .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);

// Load shadow navigation explicitly
if (@event != null)
{
    var emailGroupsCollection = _dbContext.Entry(@event).Collection("_emailGroupEntities");
    await emailGroupsCollection.LoadAsync(cancellationToken);
}
```

### Phase 4: Update Documentation

- [x] Create ADR-009 (this document)
- [ ] Update PHASE_6A_MASTER_INDEX.md
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update STREAMLINED_ACTION_PLAN.md

---

## Performance Considerations

### Batch Query Optimization ✅

```csharp
// GOOD: Single batch query (already implemented)
var emailGroups = await _emailGroupRepository.GetByIdsAsync(distinctGroupIds, cancellationToken);

// BAD: N+1 queries
foreach (var groupId in distinctGroupIds)
{
    var emailGroup = await _emailGroupRepository.GetByIdAsync(groupId, cancellationToken);
}
```

### Loading Strategy

**Lazy Loading**: ❌ Not enabled (intentional)
**Explicit Loading**: ✅ Using ChangeTracker API
**Eager Loading**: ⚠️ Consider for read queries with `.Include()`

### Query Projection

```csharp
// For read-only queries, project to DTOs directly
var events = await _dbContext.Events
    .Select(e => new EventDto
    {
        Id = e.Id,
        Title = e.Title.Value,
        // Use SQL join to fetch email group IDs
        EmailGroupIds = _dbContext.Set<Dictionary<string, object>>("event_email_groups")
            .Where(j => EF.Property<Guid>(j, "event_id") == e.Id)
            .Select(j => EF.Property<Guid>(j, "email_group_id"))
            .ToList()
    })
    .ToListAsync();
```

---

## Security Considerations

### Ownership Validation ✅

```csharp
// Already implemented correctly
if (emailGroup.OwnerId != @event.OrganizerId)
    return Result.Failure($"You do not have permission to use email group '{emailGroup.Name}'");
```

### Status Validation ✅

```csharp
// Prevents using inactive groups
if (!emailGroup.IsActive)
    return Result.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
```

### Authorization

Ensure controller checks user is the event organizer:
```csharp
[Authorize(Roles = "EventOrganizer")]
[HttpPut("{id}")]
public async Task<IActionResult> UpdateEvent(Guid id, UpdateEventRequest request)
{
    var userId = User.GetUserId();
    // Verify user owns the event before calling handler
}
```

---

## Rollback Strategy

### If Implementation Fails

1. **Remove sync logic** from handler (reverts to current behavior)
2. **Junction table data** will be empty but database structure is safe
3. **Domain model** unchanged (no rollback needed)
4. **EF Core configuration** unchanged (no rollback needed)

### Database Rollback

```sql
-- If needed, drop junction table (safe - no data loss)
DROP TABLE IF EXISTS event_email_groups;
```

---

## Monitoring & Observability

### Logging Points

```csharp
_logger.Information(
    "Syncing email groups for event {EventId}: {GroupCount} groups",
    @event.Id,
    emailGroups.Count);

_logger.Debug(
    "Loading shadow navigation for event {EventId}",
    @event.Id);

_logger.Information(
    "Successfully updated email groups for event {EventId}",
    @event.Id);
```

### Metrics

- Count of events with email groups assigned
- Average email groups per event
- Failure rate of email group assignments

---

## References

1. **ADR-008**: User Preferred Metro Areas (Phase 5B + 6A.9)
2. **EF Core Documentation**: [Many-to-Many Relationships](https://docs.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many)
3. **Clean Architecture**: Robert C. Martin (Uncle Bob)
4. **Domain-Driven Design**: Eric Evans
5. **Existing Implementation**: `UpdateUserPreferredMetroAreasCommandHandler.cs:77-89`

---

## Conclusion

**Recommended Pattern**: ChangeTracker API Sync (ADR-008)

This solution:
- Respects Clean Architecture boundaries ✅
- Maintains DDD principles ✅
- Works with EF Core change tracking ✅
- Consistent with existing codebase patterns ✅
- Testable and maintainable ✅

**Next Steps**:
1. Implement fix in `UpdateEventCommandHandler` (Phase 1)
2. Add support in `CreateEventCommandHandler` (Phase 2)
3. Verify query handlers load shadow navigation (Phase 3)
4. Update tracking documentation (Phase 4)

---

**Document History**:
- 2025-12-16: Initial ADR created by System Architect
- Related Session: Session 33 (Phase 6A.32)
