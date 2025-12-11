# Architecture Decision Record: EF Core Many-to-Many with GUID Collection Persistence

**Date**: 2025-11-12
**Status**: RESOLVED
**Decision ID**: ADR-009

---

## Problem Statement

PUT `/api/users/{id}/preferred-metro-areas` returns 204 (success) but GET returns empty array `[]`. The many-to-many relationship between User and MetroArea is NOT persisting to the junction table `user_preferred_metro_areas`.

### Root Cause Analysis

**The fundamental architectural mismatch:**

1. **Domain Model Design** (`User.cs` lines 34-35):
   ```csharp
   private readonly List<Guid> _preferredMetroAreaIds = new();
   public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();
   ```
   - Stores **primitive GUIDs** (not entity references)
   - Pure domain design - no ORM concerns
   - Business logic operates on IDs

2. **EF Core Configuration** (`UserConfiguration.cs` lines 280-323):
   ```csharp
   var navigation = builder.HasMany<Domain.Events.MetroArea>()
       .WithMany()
       .UsingEntity<Dictionary<string, object>>("user_preferred_metro_areas", ...);
   ```
   - Creates a skip navigation for **MetroArea entities**
   - Shadow collection property expects `ICollection<MetroArea>` (not GUIDs)
   - No connection to `_preferredMetroAreaIds` backing field

3. **The Disconnect:**
   - EF Core's skip navigation creates a shadow `ICollection<MetroArea>` in its model metadata
   - The domain's `_preferredMetroAreaIds` is a separate `List<Guid>`
   - **These two collections are COMPLETELY UNRELATED in EF Core's view**
   - Setting `PropertyAccessMode.Field` doesn't help because the field type doesn't match the navigation type

---

## Why Current Approach Fails

### Change Tracking Mechanism

```
Domain Layer:                      EF Core Layer:
┌──────────────────────┐          ┌──────────────────────────┐
│ User                 │          │ User Entity              │
│  _preferredMetroArea │          │  [Shadow Navigation]     │
│   Ids: List<Guid>    │   ❌    │    MetroAreas:           │
│  - Add(guid1)        │   NO    │      ICollection<Metro>  │
│  - Add(guid2)        │ SYNC    │    (EMPTY - never set!)  │
└──────────────────────┘          └──────────────────────────┘
```

**What happens on SaveChangesAsync():**
1. EF Core checks change tracker for modified entities
2. Inspects the **shadow MetroAreas navigation** (expecting entity objects)
3. Finds it EMPTY because domain only modified `_preferredMetroAreaIds`
4. Detects NO changes to the many-to-many relationship
5. Skips junction table inserts/updates
6. Returns success (no errors, just no persistence)

---

## Architectural Solutions

### Option 1: EF-Aware Shadow Property (RECOMMENDED)

**Approach**: Add a shadow collection property that EF Core can track, synchronized with the domain backing field.

**Implementation**:

```csharp
// Domain Model (User.cs)
// Keep existing GUID collection for domain logic
private readonly List<Guid> _preferredMetroAreaIds = new();
public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();

// Add EF Core shadow navigation property (NOT exposed to domain consumers)
private ICollection<MetroArea>? _preferredMetroAreaEntities;

// Update method modified to sync both collections
public Result UpdatePreferredMetroAreas(
    IEnumerable<Guid>? metroAreaIds,
    ICollection<MetroArea>? metroAreaEntities = null)  // EF can pass entities
{
    var metroAreaList = metroAreaIds?.ToList() ?? new List<Guid>();

    if (metroAreaList.Count > 20)
        return Result.Failure("Cannot select more than 20 preferred metro areas");

    if (metroAreaList.Distinct().Count() != metroAreaList.Count)
        return Result.Failure("Duplicate metro area IDs are not allowed");

    _preferredMetroAreaIds.Clear();
    _preferredMetroAreaIds.AddRange(metroAreaList.Distinct());

    // CRITICAL: Also update the EF Core shadow property for persistence
    if (metroAreaEntities != null)
    {
        _preferredMetroAreaEntities = metroAreaEntities;
    }

    MarkAsUpdated();

    if (_preferredMetroAreaIds.Any())
        RaiseDomainEvent(new UserPreferredMetroAreasUpdatedEvent(Id, _preferredMetroAreaIds.AsReadOnly()));

    return Result.Success();
}
```

**EF Core Configuration** (`UserConfiguration.cs`):

```csharp
// Configure many-to-many with explicit backing field
builder.HasMany<Domain.Events.MetroArea>("_preferredMetroAreaEntities")
    .WithMany()
    .UsingEntity<Dictionary<string, object>>(
        "user_preferred_metro_areas",
        j => j.HasOne<Domain.Events.MetroArea>()
            .WithMany()
            .HasForeignKey("metro_area_id")
            .OnDelete(DeleteBehavior.Cascade),
        j => j.HasOne<User>()
            .WithMany()
            .HasForeignKey("user_id")
            .OnDelete(DeleteBehavior.Cascade),
        j =>
        {
            j.ToTable("user_preferred_metro_areas", "identity");
            j.HasKey("user_id", "metro_area_id");
            j.HasIndex("user_id").HasDatabaseName("ix_user_preferred_metro_areas_user_id");
            j.HasIndex("metro_area_id").HasDatabaseName("ix_user_preferred_metro_areas_metro_area_id");
            j.Property<DateTime>("created_at").HasDefaultValueSql("NOW()").IsRequired();
        });

// Field access for tracking
builder.Navigation("_preferredMetroAreaEntities")
    .HasField("_preferredMetroAreaEntities")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Command Handler Update** (`UpdateUserPreferredMetroAreasCommandHandler.cs`):

```csharp
public async Task<Result> Handle(
    UpdateUserPreferredMetroAreasCommand command,
    CancellationToken cancellationToken)
{
    var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
        return Result.Failure("User not found");

    // Validate & load MetroArea entities
    List<Domain.Events.MetroArea> metroAreaEntities = new();

    if (command.MetroAreaIds.Any())
    {
        metroAreaEntities = await _dbContext.MetroAreas
            .Where(m => command.MetroAreaIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        if (metroAreaEntities.Count != command.MetroAreaIds.Count())
        {
            var foundIds = metroAreaEntities.Select(m => m.Id).ToList();
            var invalidIds = command.MetroAreaIds.Except(foundIds).ToList();
            return Result.Failure($"Invalid metro area IDs: {string.Join(", ", invalidIds)}");
        }
    }

    // Pass BOTH IDs (for domain logic) AND entities (for EF persistence)
    var updateResult = user.UpdatePreferredMetroAreas(
        command.MetroAreaIds,
        metroAreaEntities);  // EF-aware parameter

    if (!updateResult.IsSuccess)
        return Result.Failure(updateResult.Errors.ToArray());

    _userRepository.Update(user);
    await _unitOfWork.CommitAsync(cancellationToken);

    return Result.Success();
}
```

**Repository Update** (`UserRepository.cs` line 23-31):

```csharp
public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _dbSet
        .AsSplitQuery()
        .Include(u => u.CulturalInterests)
        .Include(u => u.Languages)
        .Include(u => u.ExternalLogins)
        // CRITICAL: Load the skip navigation to populate _preferredMetroAreaEntities
        .Include("_preferredMetroAreaEntities")  // String-based for shadow property
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}
```

**Trade-offs:**
- ✅ Preserves clean domain model (GUIDs for business logic)
- ✅ EF Core gets entity references for change tracking
- ✅ No leakage of EF concerns to domain API (optional parameter)
- ⚠️ Requires synchronization between two collections
- ⚠️ Slightly more complex domain method signature

---

### Option 2: Owned Entity Collection (ALTERNATIVE)

**Approach**: Use `OwnsMany` with a junction entity instead of many-to-many.

**Implementation**:

```csharp
// Domain Model - New owned entity
public class UserMetroAreaPreference
{
    public Guid MetroAreaId { get; private set; }

    private UserMetroAreaPreference() { }

    public UserMetroAreaPreference(Guid metroAreaId)
    {
        MetroAreaId = metroAreaId;
    }
}

// User entity
private readonly List<UserMetroAreaPreference> _preferredMetroAreas = new();
public IReadOnlyList<Guid> PreferredMetroAreaIds =>
    _preferredMetroAreas.Select(p => p.MetroAreaId).ToList().AsReadOnly();

public Result UpdatePreferredMetroAreas(IEnumerable<Guid>? metroAreaIds)
{
    var metroAreaList = metroAreaIds?.ToList() ?? new List<Guid>();

    if (metroAreaList.Count > 20)
        return Result.Failure("Cannot select more than 20 preferred metro areas");

    _preferredMetroAreas.Clear();
    _preferredMetroAreas.AddRange(metroAreaList.Select(id => new UserMetroAreaPreference(id)));

    MarkAsUpdated();
    return Result.Success();
}
```

**EF Core Configuration**:

```csharp
builder.OwnsMany(u => u._preferredMetroAreas, pref =>
{
    pref.ToTable("user_preferred_metro_areas", "identity");
    pref.WithOwner().HasForeignKey("user_id");

    pref.Property(p => p.MetroAreaId)
        .HasColumnName("metro_area_id")
        .IsRequired();

    pref.HasKey("user_id", "metro_area_id");

    // Foreign key to metro_areas table
    pref.HasOne<MetroArea>()
        .WithMany()
        .HasForeignKey(p => p.MetroAreaId)
        .OnDelete(DeleteBehavior.Cascade);
});

builder.Navigation("_preferredMetroAreas")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Trade-offs:**
- ✅ EF Core automatically tracks OwnsMany collections
- ✅ Simpler - no shadow property needed
- ✅ Better fit for DDD (owned entity)
- ⚠️ Different table structure (needs migration)
- ⚠️ More invasive change to domain model

---

## Decision

**CHOOSE OPTION 1** - EF-Aware Shadow Property

### Rationale

1. **Minimal Domain Impact**:
   - Keeps `List<Guid>` for clean domain logic
   - Shadow property is internal implementation detail
   - Optional parameter preserves backward compatibility

2. **Preserves Existing Schema**:
   - No database migration required
   - Junction table already correct (`user_preferred_metro_areas`)
   - Only code changes needed

3. **Explicit EF Integration**:
   - Clear separation: `_preferredMetroAreaIds` = domain, `_preferredMetroAreaEntities` = persistence
   - No magic - developers can see synchronization happening
   - Easier to debug and test

4. **Follows Clean Architecture**:
   - Infrastructure layer (handler) loads entities
   - Domain layer accepts them as optional dependency injection
   - No ORM pollution in pure domain API

---

## Implementation Checklist

1. ✅ Add `_preferredMetroAreaEntities` field to User entity
2. ✅ Update `UpdatePreferredMetroAreas()` method with optional parameter
3. ✅ Modify EF Core configuration to use string-based navigation
4. ✅ Update command handler to load and pass entities
5. ✅ Add `Include("_preferredMetroAreaEntities")` to UserRepository
6. ✅ Write unit tests for both collections
7. ✅ Write integration test verifying persistence

---

## Testing Strategy

### Unit Tests (Domain)
```csharp
[Fact]
public void UpdatePreferredMetroAreas_WithValidIds_UpdatesBothCollections()
{
    // Arrange
    var user = CreateTestUser();
    var metroIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
    var metroEntities = metroIds.Select(id => new MetroArea { Id = id }).ToList();

    // Act
    var result = user.UpdatePreferredMetroAreas(metroIds, metroEntities);

    // Assert
    result.IsSuccess.Should().BeTrue();
    user.PreferredMetroAreaIds.Should().BeEquivalentTo(metroIds);
    // Access shadow property via reflection for testing
    var shadowProp = typeof(User).GetField("_preferredMetroAreaEntities", BindingFlags.NonPublic | BindingFlags.Instance);
    var entities = shadowProp.GetValue(user) as ICollection<MetroArea>;
    entities.Should().HaveCount(2);
}
```

### Integration Tests (Persistence)
```csharp
[Fact]
public async Task UpdateUserPreferredMetroAreas_PersistsToJunctionTable()
{
    // Arrange
    var userId = await CreateTestUserAsync();
    var metroIds = await GetTestMetroAreaIdsAsync(count: 3);

    // Act
    var command = new UpdateUserPreferredMetroAreasCommand(userId, metroIds);
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert - Verify junction table
    var junctionRecords = await _dbContext.Database
        .SqlQueryRaw<JunctionRecord>(
            "SELECT user_id, metro_area_id FROM identity.user_preferred_metro_areas WHERE user_id = {0}",
            userId)
        .ToListAsync();

    junctionRecords.Should().HaveCount(3);
    junctionRecords.Select(r => r.metro_area_id).Should().BeEquivalentTo(metroIds);
}
```

---

## Consequences

### Positive
- Clear separation of domain concerns (GUIDs) from persistence concerns (entities)
- Minimal refactoring required - backward compatible change
- Explicit about EF Core requirements
- Easy to understand for future maintainers

### Negative
- Two collections to maintain (though synchronized automatically)
- Handler needs to load entities before calling domain method
- Slightly more complex than pure many-to-many

### Neutral
- Sets pattern for other GUID-based many-to-many relationships
- Documents the "impedance mismatch" between DDD and ORMs

---

## References

- **Previous Attempts**:
  - Commit 96994de - Tried PropertyAccessMode.Field (failed - type mismatch)
  - Commit 75b0981 - Changed from FieldDuringConstruction to Field (failed - wrong root cause)
  - Commit 79adf77 - Reverted critical fix (failed - incomplete solution)

- **Related ADRs**:
  - ADR-008: Explicit junction table configuration

- **EF Core Documentation**:
  - [Many-to-Many Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many)
  - [Shadow Properties](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)
  - [Backing Fields](https://learn.microsoft.com/en-us/ef/core/modeling/backing-field)

---

## Author
Claude Code (System Architecture Designer)
Date: 2025-11-12
