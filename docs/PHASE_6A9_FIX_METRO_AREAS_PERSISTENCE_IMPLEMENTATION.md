# Implementation Guide: Fix Metro Areas Persistence

**Phase**: 6A.9
**Priority**: CRITICAL
**Scope**: Fix many-to-many persistence for User.PreferredMetroAreaIds

---

## Summary

PUT `/api/users/{id}/preferred-metro-areas` returns 204 but GET returns `[]`. The root cause is an architectural mismatch between the domain model (stores GUIDs) and EF Core's many-to-many configuration (expects entity references).

**Solution**: Add a shadow navigation property that EF Core can track, synchronized with the domain's GUID collection.

---

## Complete Code Changes

### 1. Domain Model: User.cs

**File**: `src/LankaConnect.Domain/Users/User.cs`

**Changes**:

```csharp
// BEFORE (lines 33-35):
private readonly List<Guid> _preferredMetroAreaIds = new();
public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();

// AFTER:
private readonly List<Guid> _preferredMetroAreaIds = new();
public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();

// ADDED: EF Core shadow navigation property (internal use only)
// This allows EF Core to track entity references for persistence to junction table
// NOT exposed through public API - infrastructure concern
private ICollection<Domain.Events.MetroArea>? _preferredMetroAreaEntities;
```

**Updated Method** (lines 546-568):

```csharp
/// <summary>
/// Updates user's preferred metro areas for location-based filtering (0-20 allowed)
/// Empty collection clears all preferences (privacy choice - user can opt out)
/// Phase 5B: User Preferred Metro Areas - Expanded to 20 max limit
/// Phase 6A.9: Added EF Core entity parameter for proper persistence
/// Architecture: Follows ADR-008 & ADR-009 - Domain validates max count, Application validates existence
/// </summary>
/// <param name="metroAreaIds">List of metro area GUIDs for domain logic</param>
/// <param name="metroAreaEntities">Optional: Loaded MetroArea entities for EF Core persistence (infrastructure concern)</param>
public Result UpdatePreferredMetroAreas(
    IEnumerable<Guid>? metroAreaIds,
    ICollection<Domain.Events.MetroArea>? metroAreaEntities = null)
{
    var metroAreaList = metroAreaIds?.ToList() ?? new List<Guid>();

    // Validate max 20 metro areas (Phase 5B: Expanded from 10 to 20)
    if (metroAreaList.Count > 20)
        return Result.Failure("Cannot select more than 20 preferred metro areas");

    // Validate no duplicates
    if (metroAreaList.Distinct().Count() != metroAreaList.Count)
        return Result.Failure("Duplicate metro area IDs are not allowed");

    // Update domain collection (used by business logic)
    _preferredMetroAreaIds.Clear();
    _preferredMetroAreaIds.AddRange(metroAreaList.Distinct());

    // CRITICAL FIX: Update EF Core shadow navigation property for persistence
    // This enables EF Core to detect changes and persist to junction table
    if (metroAreaEntities != null)
    {
        _preferredMetroAreaEntities = metroAreaEntities;
    }
    else if (!metroAreaList.Any())
    {
        // Clear entities when clearing preferences
        _preferredMetroAreaEntities = new List<Domain.Events.MetroArea>();
    }

    MarkAsUpdated();

    // Only raise event if setting preferences (not clearing for privacy)
    if (_preferredMetroAreaIds.Any())
        RaiseDomainEvent(new UserPreferredMetroAreasUpdatedEvent(Id, _preferredMetroAreaIds.AsReadOnly()));

    return Result.Success();
}
```

---

### 2. EF Core Configuration: UserConfiguration.cs

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs`

**Replace lines 277-323 with**:

```csharp
// Configure PreferredMetroAreas many-to-many relationship (Phase 5B + 6A.9 Fix)
// ADR-009: Domain stores List<Guid> for business logic, EF Core needs entity references for persistence
// Solution: Shadow navigation property "_preferredMetroAreaEntities" synced with "_preferredMetroAreaIds"
builder.HasMany<Domain.Events.MetroArea>("_preferredMetroAreaEntities")
    .WithMany()
    .UsingEntity<Dictionary<string, object>>(
        "user_preferred_metro_areas",
        j => j
            .HasOne<Domain.Events.MetroArea>()
            .WithMany()
            .HasForeignKey("metro_area_id")
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_preferred_metro_areas_metro_area_id"),
        j => j
            .HasOne<User>()
            .WithMany()
            .HasForeignKey("user_id")
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_preferred_metro_areas_user_id"),
        j =>
        {
            j.ToTable("user_preferred_metro_areas", "identity");

            // Composite primary key
            j.HasKey("user_id", "metro_area_id");

            // Indexes for query performance
            j.HasIndex("user_id")
                .HasDatabaseName("ix_user_preferred_metro_areas_user_id");

            j.HasIndex("metro_area_id")
                .HasDatabaseName("ix_user_preferred_metro_areas_metro_area_id");

            // Audit column
            j.Property<DateTime>("created_at")
                .HasDefaultValueSql("NOW()")
                .IsRequired();
        });

// CRITICAL: Configure field access for shadow navigation property
// This tells EF Core to use the private "_preferredMetroAreaEntities" field for change tracking
builder.Navigation("_preferredMetroAreaEntities")
    .HasField("_preferredMetroAreaEntities")
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

---

### 3. Repository: UserRepository.cs

**File**: `src/LankaConnect.Infrastructure/Data/Repositories/UserRepository.cs`

**Update method** (lines 23-31):

```csharp
/// <summary>
/// Override to include Epic 1 Phase 3 navigation properties (CulturalInterests, Languages)
/// + Phase 6A.9: Include _preferredMetroAreaEntities shadow navigation for many-to-many persistence
/// Base Repository uses FindAsync which doesn't load OwnsMany collections
/// Using AsSplitQuery() + explicit Include() loads OwnsMany collections and tracks changes
/// CRITICAL: Do NOT use AsNoTracking() - we need tracking for UPDATE operations
/// ARCHITECTURE NOTE: OwnsMany collections with nested OwnsOne value objects (like Languages.Language)
/// are automatically loaded by EF Core due to AutoInclude() configuration in UserConfiguration.cs.
/// However, we keep explicit Include() for clarity and to ensure proper split query optimization.
/// The nested LanguageCode owned entity is loaded automatically - no ThenInclude() needed.
/// </summary>
public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _dbSet
        .AsSplitQuery()
        .Include(u => u.CulturalInterests)
        .Include(u => u.Languages)
        .Include(u => u.ExternalLogins)
        // CRITICAL FIX Phase 6A.9: Load shadow navigation for metro areas many-to-many
        // This populates _preferredMetroAreaEntities so EF Core can track changes
        .Include("_preferredMetroAreaEntities")  // String-based for shadow property
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}
```

---

### 4. Command Handler: UpdateUserPreferredMetroAreasCommandHandler.cs

**File**: `src/LankaConnect.Application/Users/Commands/UpdatePreferredMetroAreas/UpdateUserPreferredMetroAreasCommandHandler.cs`

**Replace entire Handle method** (lines 30-68):

```csharp
public async Task<Result> Handle(
    UpdateUserPreferredMetroAreasCommand command,
    CancellationToken cancellationToken)
{
    // Get user with tracked metro area entities
    var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
    if (user == null)
    {
        return Result.Failure("User not found");
    }

    // Phase 6A.9 FIX: Load MetroArea entities for EF Core persistence
    // Application layer validates existence (ADR-008)
    // Infrastructure layer provides entity references (ADR-009)
    List<Domain.Events.MetroArea> metroAreaEntities = new();

    if (command.MetroAreaIds.Any())
    {
        // Load actual entities from database
        metroAreaEntities = await _dbContext.MetroAreas
            .Where(m => command.MetroAreaIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        // Validate all IDs exist
        if (metroAreaEntities.Count != command.MetroAreaIds.Count())
        {
            var foundIds = metroAreaEntities.Select(m => m.Id).ToList();
            var invalidIds = command.MetroAreaIds.Except(foundIds).ToList();
            return Result.Failure($"Invalid metro area IDs: {string.Join(", ", invalidIds)}");
        }
    }

    // Update user's preferred metro areas
    // Pass both IDs (for domain logic) AND entities (for EF Core persistence)
    var updateResult = user.UpdatePreferredMetroAreas(
        command.MetroAreaIds,
        metroAreaEntities);  // NEW: Entity references for EF tracking

    if (!updateResult.IsSuccess)
    {
        return Result.Failure(updateResult.Errors.ToArray());
    }

    // Save changes - EF Core now detects changes via _preferredMetroAreaEntities
    _userRepository.Update(user);
    await _unitOfWork.CommitAsync(cancellationToken);

    return Result.Success();
}
```

---

## Testing Plan

### 1. Unit Tests (Domain Layer)

**File**: Create or update `tests/LankaConnect.Domain.Tests/Users/UserTests.cs`

```csharp
[Fact]
public void UpdatePreferredMetroAreas_WithValidIdsAndEntities_UpdatesBothCollections()
{
    // Arrange
    var user = CreateTestUser();
    var metroId1 = Guid.NewGuid();
    var metroId2 = Guid.NewGuid();
    var metroIds = new[] { metroId1, metroId2 };

    var metroEntities = new List<MetroArea>
    {
        CreateMetroArea(metroId1),
        CreateMetroArea(metroId2)
    };

    // Act
    var result = user.UpdatePreferredMetroAreas(metroIds, metroEntities);

    // Assert
    result.IsSuccess.Should().BeTrue();
    user.PreferredMetroAreaIds.Should().HaveCount(2);
    user.PreferredMetroAreaIds.Should().BeEquivalentTo(metroIds);

    // Verify shadow property via reflection (infrastructure concern)
    var shadowField = typeof(User).GetField(
        "_preferredMetroAreaEntities",
        BindingFlags.NonPublic | BindingFlags.Instance);
    var shadowCollection = shadowField?.GetValue(user) as ICollection<MetroArea>;

    shadowCollection.Should().NotBeNull();
    shadowCollection.Should().HaveCount(2);
    shadowCollection.Select(m => m.Id).Should().BeEquivalentTo(metroIds);
}

[Fact]
public void UpdatePreferredMetroAreas_ClearingPreferences_ClearsBothCollections()
{
    // Arrange
    var user = CreateTestUserWithMetroAreas();

    // Act
    var result = user.UpdatePreferredMetroAreas(new List<Guid>(), new List<MetroArea>());

    // Assert
    result.IsSuccess.Should().BeTrue();
    user.PreferredMetroAreaIds.Should().BeEmpty();

    var shadowField = typeof(User).GetField(
        "_preferredMetroAreaEntities",
        BindingFlags.NonPublic | BindingFlags.Instance);
    var shadowCollection = shadowField?.GetValue(user) as ICollection<MetroArea>;

    shadowCollection.Should().BeEmpty();
}
```

### 2. Integration Tests (Persistence)

**File**: Create or update `tests/LankaConnect.Infrastructure.Tests/Repositories/UserRepositoryTests.cs`

```csharp
[Fact]
public async Task UpdatePreferredMetroAreas_PersistsToJunctionTable()
{
    // Arrange
    var user = await CreateAndSaveUserAsync();
    var metroAreas = await CreateAndSaveMetroAreasAsync(count: 3);
    var metroIds = metroAreas.Select(m => m.Id).ToList();

    // Act
    var loadedUser = await _userRepository.GetByIdAsync(user.Id);
    loadedUser.UpdatePreferredMetroAreas(metroIds, metroAreas);
    _userRepository.Update(loadedUser);
    await _unitOfWork.CommitAsync();

    // Clear context to force database read
    _dbContext.ChangeTracker.Clear();

    // Assert - Verify junction table records
    var junctionRecords = await _dbContext.Database
        .SqlQueryRaw<(Guid user_id, Guid metro_area_id)>(
            @"SELECT user_id, metro_area_id
              FROM identity.user_preferred_metro_areas
              WHERE user_id = {0}",
            user.Id)
        .ToListAsync();

    junctionRecords.Should().HaveCount(3);
    junctionRecords.Select(r => r.metro_area_id).Should().BeEquivalentTo(metroIds);
}

[Fact]
public async Task UpdatePreferredMetroAreas_LoadedUserHasCorrectIds()
{
    // Arrange
    var user = await CreateUserWithMetroAreasAsync(count: 5);

    // Act - Clear context and reload
    _dbContext.ChangeTracker.Clear();
    var reloadedUser = await _userRepository.GetByIdAsync(user.Id);

    // Assert
    reloadedUser.Should().NotBeNull();
    reloadedUser.PreferredMetroAreaIds.Should().HaveCount(5);

    // Verify shadow property is also loaded
    var shadowField = typeof(User).GetField(
        "_preferredMetroAreaEntities",
        BindingFlags.NonPublic | BindingFlags.Instance);
    var shadowCollection = shadowField?.GetValue(reloadedUser) as ICollection<MetroArea>;

    shadowCollection.Should().HaveCount(5);
}
```

### 3. E2E Test (API)

**File**: Existing E2E tests should now pass without modification

```bash
# Run specific E2E test
cd web
npm test -- --grep "PUT /api/users/:id/preferred-metro-areas"
```

**Expected Behavior**:
1. PUT returns 204
2. GET returns array with correct metro area IDs
3. Database junction table has correct records

---

## Deployment Steps

### 1. Code Changes
```bash
# Apply all 4 file changes above
# User.cs, UserConfiguration.cs, UserRepository.cs, UpdateUserPreferredMetroAreasCommandHandler.cs
```

### 2. Build & Test
```bash
# Backend
cd src
dotnet build
dotnet test

# Frontend (if E2E tests)
cd ../web
npm test
```

### 3. Database
```bash
# No migration needed - schema already correct
# Junction table "identity.user_preferred_metro_areas" exists
```

### 4. Verify Fix
```bash
# Manual test via Swagger or Postman:

# 1. Get existing metro area IDs
GET http://localhost:5001/api/metro-areas

# 2. Update user preferences
PUT http://localhost:5001/api/users/{userId}/preferred-metro-areas
Body: { "metroAreaIds": ["guid1", "guid2", "guid3"] }
Expected: 204 No Content

# 3. Verify persistence
GET http://localhost:5001/api/users/{userId}/preferred-metro-areas
Expected: ["guid1", "guid2", "guid3"]

# 4. Check database
SELECT * FROM identity.user_preferred_metro_areas WHERE user_id = '{userId}';
Expected: 3 rows
```

---

## Rollback Plan

If issues occur:

```bash
# Revert commits
git revert HEAD~4..HEAD

# Or restore previous version
git checkout <previous-commit-hash> -- src/LankaConnect.Domain/Users/User.cs
git checkout <previous-commit-hash> -- src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs
git checkout <previous-commit-hash> -- src/LankaConnect.Infrastructure/Data/Repositories/UserRepository.cs
git checkout <previous-commit-hash> -- src/LankaConnect.Application/Users/Commands/UpdatePreferredMetroAreas/UpdateUserPreferredMetroAreasCommandHandler.cs
```

---

## Success Criteria

- [ ] PUT `/api/users/{id}/preferred-metro-areas` returns 204
- [ ] GET `/api/users/{id}/preferred-metro-areas` returns correct array (not empty)
- [ ] Database junction table has correct records
- [ ] Unit tests pass (domain logic)
- [ ] Integration tests pass (persistence)
- [ ] E2E tests pass (API behavior)
- [ ] No breaking changes to existing functionality
- [ ] Code review approved
- [ ] Documentation updated (ADR-009)

---

## Related Files

- **ADR-009**: `docs/ADR-009-EF-CORE-MANY-TO-MANY-GUID-COLLECTION-PERSISTENCE.md`
- **Previous ADR**: `docs/ADR-008-*.md` (explicit junction table)
- **Migration**: Not needed (schema correct)
- **API Docs**: No changes needed

---

## Questions & Answers

**Q: Why not just change the domain model to use entities?**
A: That would leak EF Core concerns into the pure domain layer, violating Clean Architecture principles.

**Q: Why not use OwnsMany instead?**
A: Possible alternative, but requires migration and more invasive changes. Shadow property is less intrusive.

**Q: Does this pattern apply to other many-to-many relationships?**
A: Yes, this is now the standard pattern for GUID-based many-to-many in this codebase.

**Q: What if we need to query by metro areas?**
A: Use the shadow navigation in LINQ queries: `_dbContext.Users.Include("_preferredMetroAreaEntities")`

---

**Author**: Claude Code (System Architecture Designer)
**Date**: 2025-11-12
**Status**: Ready for Implementation
