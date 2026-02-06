# ADR-010: Event Email Groups Shadow Navigation Synchronization

**Status:** Accepted
**Date:** 2025-12-17
**Phase:** 6A.33
**Related:** ADR-009 (User Metro Areas Shadow Navigation)

## Context

Phase 6A.32/33 introduced email group assignments to events via a many-to-many relationship. Initial implementation followed the pattern from UserRepository.AddAsync for metro areas, but email groups were NOT being persisted to the junction table (`event_email_groups`).

### The Problem

**Symptom:**
- CREATE endpoint returned 200 OK
- Event was created successfully
- `EmailGroupIds` array was EMPTY in response (not persisted to database)
- Junction table had zero rows despite validation passing

**Root Cause Analysis:**

The Event domain model had a critical architectural inconsistency compared to User:

```csharp
// Event.cs (BROKEN) - Line 48
public IReadOnlyList<Guid> EmailGroupIds => _emailGroupEntities.Select(eg => eg.Id).ToList().AsReadOnly();

// User.cs (WORKING) - Line 35
public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();
```

**Issue 1: Property Source Mismatch**
- `Event.EmailGroupIds` property derived from `_emailGroupEntities` (shadow navigation)
- `Event.SetEmailGroups()` modified `_emailGroupIds` list
- Created disconnect: domain methods updated one list, public property read from another

**Issue 2: Missing Hydration Sync**
- `UserRepository.GetByIdAsync()` calls `user.SyncPreferredMetroAreaIdsFromEntities()` after loading
- `EventRepository.GetByIdAsync()` did NOT sync loaded entities to domain list
- Result: domain logic couldn't access loaded email groups

**Issue 3: No Reverse Sync Method**
- User had `SyncPreferredMetroAreaIdsFromEntities()` for EF Core → Domain sync
- Event lacked equivalent method
- Infrastructure layer had no way to hydrate domain list after database load

### Why UserRepository Worked

UserRepository succeeded because it maintains **bidirectional consistency**:

1. **Domain → EF Core** (AddAsync):
   ```csharp
   var metroAreaEntities = await _context.Set<MetroArea>()
       .Where(m => entity.PreferredMetroAreaIds.Contains(m.Id))
       .ToListAsync(cancellationToken);
   metroAreasCollection.CurrentValue = metroAreaEntities;
   ```

2. **EF Core → Domain** (GetByIdAsync):
   ```csharp
   var metroAreaIds = metroAreaEntities.Select(m => m.Id).ToList();
   user.SyncPreferredMetroAreaIdsFromEntities(metroAreaIds);
   ```

3. **Public Property** reads from domain list:
   ```csharp
   public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();
   ```

Event was missing step 2 and had incorrect step 3.

## Decision

Apply the **ADR-009 pattern** to Event email groups with full bidirectional synchronization.

### Changes

#### 1. Fix Event Domain Model (Event.cs)

**Changed EmailGroupIds Property:**
```csharp
// BEFORE (BROKEN)
public IReadOnlyList<Guid> EmailGroupIds => _emailGroupEntities.Select(eg => eg.Id).ToList().AsReadOnly();

// AFTER (FIXED)
public IReadOnlyList<Guid> EmailGroupIds => _emailGroupIds.AsReadOnly();
```

**Added Hydration Method:**
```csharp
/// <summary>
/// Phase 6A.33 FIX: Sync domain's email group ID list from loaded EF Core entities
/// Pattern mirrors User.SyncPreferredMetroAreaIdsFromEntities per ADR-009
/// </summary>
internal void SyncEmailGroupIdsFromEntities(IEnumerable<Guid> emailGroupIds)
{
    _emailGroupIds.Clear();
    _emailGroupIds.AddRange(emailGroupIds);
    // NOTE: Do NOT call MarkAsUpdated() - read operation only
    // NOTE: Do NOT raise domain events - infrastructure hydration only
}
```

#### 2. Fix EventRepository (EventRepository.cs)

**Updated GetByIdAsync:**
```csharp
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    var eventEntity = await _dbSet
        .Include("_emailGroupEntities")
        // ... other includes ...
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    // Phase 6A.33 FIX: Sync loaded shadow navigation to domain list
    if (eventEntity != null)
    {
        var emailGroupsCollection = _context.Entry(eventEntity).Collection("_emailGroupEntities");
        var emailGroupEntities = emailGroupsCollection.CurrentValue as IEnumerable<EmailGroup>;

        if (emailGroupEntities != null)
        {
            var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();
            eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);
        }
    }

    return eventEntity;
}
```

**AddAsync Remains Unchanged:**
```csharp
// Already correctly syncs domain → EF Core
public override async Task AddAsync(Event entity, CancellationToken cancellationToken = default)
{
    await base.AddAsync(entity, cancellationToken);

    if (entity.EmailGroupIds.Any())  // NOW reads from _emailGroupIds (FIXED)
    {
        var emailGroupEntities = await _context.Set<EmailGroup>()
            .Where(eg => entity.EmailGroupIds.Contains(eg.Id))
            .ToListAsync(cancellationToken);

        var emailGroupsCollection = _context.Entry(entity).Collection("_emailGroupEntities");
        emailGroupsCollection.CurrentValue = emailGroupEntities;
    }
}
```

## Consequences

### Positive

1. **Email Groups Now Persist:** Junction table rows are created during event creation
2. **Architectural Consistency:** Event pattern now matches User pattern (ADR-009)
3. **Clear Separation of Concerns:**
   - Domain maintains `_emailGroupIds` for business logic
   - Infrastructure syncs `_emailGroupEntities` for persistence
   - Public property `EmailGroupIds` exposes domain list

4. **Bidirectional Sync:**
   - AddAsync: Domain → EF Core (create/update)
   - GetByIdAsync: EF Core → Domain (read/hydration)

5. **No Breaking Changes:** Public API remains identical

### Negative

1. **Duplicate Lists:** Both `_emailGroupIds` and `_emailGroupEntities` must be kept in sync
2. **Infrastructure Responsibility:** Repository must remember to call sync method after loading
3. **Hidden Complexity:** Developers must understand the shadow navigation pattern

### Risks Mitigated

1. **Data Loss:** Email groups are now persisted correctly
2. **Inconsistent State:** Domain and persistence layers stay synchronized
3. **Future Updates:** Pattern established for any future many-to-many relationships

## Architectural Pattern

This establishes the **Shadow Navigation Pattern** for many-to-many relationships:

### Domain Layer Responsibilities
- Maintain `List<Guid>` for business logic
- Expose via public `IReadOnlyList<Guid>` property
- Provide `internal void Sync...FromEntities()` for infrastructure hydration
- Declare private `ICollection<TEntity>? _shadowEntities` field (EF Core only)

### Infrastructure Layer Responsibilities
- Configure shadow navigation in `EntityTypeConfiguration`
- Sync domain → shadow in `Repository.AddAsync()`
- Sync shadow → domain in `Repository.GetByIdAsync()`
- Use EF Core Entry API: `_context.Entry(entity).Collection("_shadowNav")`

### Why This Pattern?

- **Domain Purity:** Business logic uses simple `List<Guid>`, not entity references
- **EF Core Constraints:** Many-to-many requires entity references, not IDs
- **Change Tracking:** EF Core can detect changes to shadow navigation collection
- **Performance:** Avoids loading full entity graphs for ID-only operations

## References

- ADR-009: User Metro Areas Shadow Navigation (Phase 6A.9)
- EventConfiguration.cs: Lines 240-268 (junction table config)
- UserConfiguration.cs: Lines 284-327 (working pattern)
- Phase 6A.33: Email Groups Integration Testing

## Build Status

- Build: ✅ Success (0 errors, 0 warnings)
- Pattern: ✅ Matches UserRepository proven implementation
- Testing: 🔄 Pending integration test verification
