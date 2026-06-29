# ADR-007: SignUp Commitment Contact Name Display Architecture

**Status**: Proposed
**Date**: 2025-12-19
**Context**: Phase 6A.28 - Anonymous user display issue for sign-up commitments

## Problem Statement

The Rice Tay sign-up item shows "2 of 5 filled" but displays "Anonymous" instead of user names who committed. This occurs because `SignUpCommitment.ContactName` is nullable, and older commitments have `NULL` values with no fallback to the User table.

### Current Implementation

**Domain Layer** (`SignUpCommitment.cs`):
```csharp
public string? ContactName { get; private set; }  // Nullable

public static Result<SignUpCommitment> CreateForItem(
    Guid signUpItemId,
    Guid userId,
    string itemDescription,
    int quantity,
    string? notes = null,
    string? contactName = null,  // Optional parameter
    string? contactEmail = null,
    string? contactPhone = null)
{
    // ContactName can be null - no validation
    var commitment = new SignUpCommitment(..., contactName?.Trim(), ...);
    return Result<SignUpCommitment>.Success(commitment);
}
```

**Application Layer** (`GetEventSignUpListsQueryHandler.cs`):
```csharp
Commitments = item.Commitments.Select(c => new SignUpCommitmentDto
{
    Id = c.Id,
    UserId = c.UserId,
    ContactName = c.ContactName,  // Direct mapping, nullable
    // ... other fields
}).ToList()
```

**Presentation Layer** (Frontend):
```typescript
{commitment.contactName || 'Anonymous'}  // Simple fallback
```

### Root Cause Analysis

1. **Domain Design**: `ContactName` is optional in domain model
2. **No User Join**: Application layer doesn't join User table when `ContactName` is null
3. **Frontend Limitation**: UI has no access to User.FullName to fill gaps
4. **Data Migration**: Existing records have NULL values

## Architecture Options Analysis

### Option 1: Backend DTO Mapping Enhancement (RECOMMENDED)

**Approach**: Modify Application layer to populate `ContactName` from `User.FullName` when null.

**Implementation Location**:
- `GetEventSignUpListsQueryHandler.cs`
- Inject `IUserRepository`
- Perform left join: `commitment.ContactName ?? user.FullName`

**Architectural Justification** (Clean Architecture + DDD):

1. **Single Responsibility Principle**: Application layer's responsibility is to orchestrate and transform domain data for presentation. Enriching DTOs with related data is a proper application service concern.

2. **Separation of Concerns**:
   - Domain layer: Business rules (contact info is optional)
   - Application layer: Data orchestration (join User table when needed)
   - Presentation layer: Display logic (no business logic)

3. **Performance**: Single database query with join is more efficient than N+1 queries or frontend API calls.

4. **Consistency**: All API clients (web, mobile, future) get consistent data without duplicating fallback logic.

5. **Testability**: Application layer query handlers are easily testable with mocked repositories.

**Pros**:
- ✅ Aligns with Clean Architecture principles
- ✅ Single source of truth for commitment display names
- ✅ Consistent across all API clients
- ✅ No breaking changes to existing contracts
- ✅ Efficient single query with join
- ✅ No frontend complexity

**Cons**:
- ⚠️ Adds database join to Users table
- ⚠️ Requires IUserRepository injection in query handler

**Implementation Strategy**:
```csharp
// GetEventSignUpListsQueryHandler.cs
public class GetEventSignUpListsQueryHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;  // NEW

    // Fetch event with commitments
    var @event = await _eventRepository.GetByIdAsync(eventId);

    // Extract all unique user IDs from commitments
    var userIds = @event.SignUpLists
        .SelectMany(s => s.Items)
        .SelectMany(i => i.Commitments)
        .Select(c => c.UserId)
        .Distinct()
        .ToList();

    // Fetch all users in one query
    var users = await _userRepository.GetByIdsAsync(userIds);
    var userDict = users.ToDictionary(u => u.Id, u => u.FullName);

    // Map with fallback logic
    ContactName = c.ContactName ?? userDict.GetValueOrDefault(c.UserId, "Anonymous")
}
```

**Migration Strategy**: No data migration needed - runtime fallback handles existing NULL values.

---

### Option 2: Frontend Fallback Enhancement

**Approach**: Keep DTO as-is, fetch user info in UI when contactName is null.

**Pros**:
- ✅ No backend changes
- ✅ Flexible UI control

**Cons**:
- ❌ N+1 API calls for each anonymous commitment
- ❌ Inconsistent across different UIs
- ❌ Violates Clean Architecture (business logic in presentation layer)
- ❌ Poor performance
- ❌ Duplicated logic across clients

**Architectural Issues**: This violates separation of concerns by pushing data orchestration logic to the presentation layer.

---

### Option 3: Data Migration + Backend Fix

**Approach**: Backfill existing `ContactName` from Users table, enforce non-null going forward.

**Pros**:
- ✅ Clean data model
- ✅ No runtime lookups

**Cons**:
- ⚠️ One-time migration effort
- ⚠️ Privacy concern (users may not want name shared)
- ⚠️ Doesn't handle future edge cases
- ⚠️ Breaking change if enforcing non-null

**Architectural Issues**: Changing `ContactName` from optional to required is a breaking change that violates existing domain rules.

---

### Option 4: Domain Event Enhancement

**Approach**: Make `ContactName` required in domain model, force callers to provide it.

**Pros**:
- ✅ Prevents future NULL values
- ✅ Domain integrity

**Cons**:
- ❌ **BREAKING CHANGE** to existing API contracts
- ❌ Violates privacy choice (user may want anonymity)
- ❌ Requires updating all callers
- ❌ Still needs migration for existing data

**Architectural Issues**: Making contact info mandatory violates the original SignUpGenius-style feature requirement where contact info is optional.

---

## Decision

**Option 1: Backend DTO Mapping Enhancement** is the recommended approach.

### Rationale

1. **Architectural Alignment**: Follows Clean Architecture by keeping data orchestration in Application layer
2. **Performance**: Efficient single query with join
3. **Consistency**: All clients get consistent data
4. **No Breaking Changes**: Preserves existing API contracts
5. **Domain Integrity**: Maintains ContactName as optional (respects privacy)
6. **Testability**: Easy to unit test with mocked repositories

### Why ContactName Should Remain Optional in Domain

The domain model correctly treats `ContactName` as optional for these business reasons:

1. **Privacy Choice**: Users may choose not to share contact info publicly
2. **SignUpGenius Pattern**: Follows industry standard where contact info is optional
3. **Flexibility**: Users can override with different contact name if needed
4. **Backwards Compatibility**: Existing commitments without contact info remain valid

The Application layer's job is to **enrich** the DTO with fallback data when domain fields are NULL, not to change domain rules.

---

## Implementation Plan

### Phase 1: Add Batch User Lookup Method (Infrastructure Layer)

**File**: `src/LankaConnect.Infrastructure/Data/Repositories/UserRepository.cs`

Add method to fetch multiple users efficiently:
```csharp
public async Task<IReadOnlyList<User>> GetByIdsAsync(
    IEnumerable<Guid> userIds,
    CancellationToken cancellationToken = default)
{
    return await _dbSet
        .AsNoTracking()
        .Where(u => userIds.Contains(u.Id))
        .ToListAsync(cancellationToken);
}
```

Add to interface: `src/LankaConnect.Domain/Users/IUserRepository.cs`

### Phase 2: Update Query Handler (Application Layer)

**File**: `src/LankaConnect.Application/Events/Queries/GetEventSignUpLists/GetEventSignUpListsQueryHandler.cs`

Modify to inject `IUserRepository` and populate contact names:

```csharp
public class GetEventSignUpListsQueryHandler
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;

    public GetEventSignUpListsQueryHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository)  // NEW
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<List<SignUpListDto>>> Handle(...)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId);

        // Collect all unique user IDs from commitments
        var userIds = @event.SignUpLists
            .SelectMany(s => s.Items)
            .SelectMany(i => i.Commitments)
            .Where(c => string.IsNullOrEmpty(c.ContactName))  // Only fetch if needed
            .Select(c => c.UserId)
            .Distinct()
            .ToList();

        // Fetch users in single query
        var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);
        var userLookup = users.ToDictionary(u => u.Id, u => u.FullName);

        // Map with fallback
        var signUpListDtos = @event.SignUpLists.Select(signUpList => new SignUpListDto
        {
            // ... existing fields
            Items = signUpList.Items.Select(item => new SignUpItemDto
            {
                // ... existing fields
                Commitments = item.Commitments.Select(c => new SignUpCommitmentDto
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    // NEW: Fallback logic
                    ContactName = c.ContactName
                        ?? userLookup.GetValueOrDefault(c.UserId, "Anonymous"),
                    // ... other fields
                }).ToList()
            }).ToList()
        }).ToList();

        return Result<List<SignUpListDto>>.Success(signUpListDtos);
    }
}
```

### Phase 3: Add Unit Tests (Tests Layer)

**File**: `tests/LankaConnect.Application.Tests/Events/Queries/GetEventSignUpListsQueryHandlerTests.cs`

Add test cases:
```csharp
[Fact]
public async Task Handle_CommitmentWithoutContactName_FallsBackToUserFullName()
{
    // Arrange: Commitment with null ContactName
    // Act: Query handler
    // Assert: DTO.ContactName = User.FullName
}

[Fact]
public async Task Handle_CommitmentWithContactName_UsesProvidedValue()
{
    // Arrange: Commitment with ContactName = "Custom Name"
    // Act: Query handler
    // Assert: DTO.ContactName = "Custom Name" (not user's name)
}

[Fact]
public async Task Handle_UserNotFound_FallsBackToAnonymous()
{
    // Arrange: Commitment with invalid UserId
    // Act: Query handler
    // Assert: DTO.ContactName = "Anonymous"
}
```

### Phase 4: Frontend Cleanup (Optional)

**File**: `web/src/presentation/components/features/events/SignUpCommitmentModal.tsx`

Simplify frontend since backend now handles fallback:
```typescript
// Before:
{commitment.contactName || 'Anonymous'}

// After:
{commitment.contactName}  // Always populated by backend
```

---

## Trade-offs Summary

| Aspect | Option 1 (Backend DTO) | Option 2 (Frontend) | Option 3 (Migration) | Option 4 (Domain Change) |
|--------|------------------------|---------------------|----------------------|--------------------------|
| Clean Architecture | ✅ Excellent | ❌ Violates | ⚠️ Neutral | ⚠️ Breaking |
| Performance | ✅ Single query | ❌ N+1 queries | ✅ No runtime cost | ✅ No runtime cost |
| Consistency | ✅ All clients | ❌ Per-client | ✅ All clients | ✅ All clients |
| Privacy | ✅ Respects choice | ✅ Respects choice | ⚠️ Forces disclosure | ❌ Mandatory |
| Complexity | ⚠️ Medium | ⚠️ High (duplication) | ⚠️ Migration effort | ❌ Breaking change |
| Testability | ✅ Easy | ⚠️ E2E needed | ✅ Easy | ⚠️ Contract changes |

---

## Alternative Considered: Repository Pattern Enhancement

An alternative implementation would add a specialized query method to `IEventRepository`:

```csharp
Task<Event?> GetByIdWithCommitmentUsersAsync(Guid eventId, CancellationToken ct);
```

This would encapsulate the join logic in the Infrastructure layer. However, this was rejected because:

1. **Query Explosion**: Would require new repository methods for every DTO enrichment scenario
2. **Violates SRP**: Repository should provide data access, not DTO construction logic
3. **Application Layer Responsibility**: Orchestrating multiple repositories is the Application layer's job

---

## Related ADRs

- **ADR-009**: Shadow Navigation Pattern (User Preferred Metro Areas)
  - Similar pattern for bridging domain collections and EF Core navigation properties
  - Validates keeping domain model clean while infrastructure handles persistence concerns

---

## Consequences

### Positive

1. Clean separation of concerns maintained
2. Performance optimized with single query
3. Consistent UX across all clients
4. No breaking changes to APIs or contracts
5. Domain model integrity preserved

### Negative

1. Additional query complexity in Application layer
2. Requires injecting multiple repositories in query handlers
3. Slight increase in query execution time (join overhead)

### Neutral

1. Frontend still renders the fallback value (but backend provides it)
2. Existing NULL values remain in database (no migration needed)

---

## Implementation Checklist

- [ ] Add `GetByIdsAsync` to `IUserRepository` interface
- [ ] Implement `GetByIdsAsync` in `UserRepository`
- [ ] Modify `GetEventSignUpListsQueryHandler` to inject `IUserRepository`
- [ ] Add user lookup and fallback logic in query handler
- [ ] Add unit tests for fallback scenarios
- [ ] Update API documentation if needed
- [ ] Optional: Simplify frontend fallback code
- [ ] Verify no performance regression (query execution time)

---

## References

- Clean Architecture by Robert C. Martin
- Domain-Driven Design by Eric Evans
- SignUpGenius feature inspiration
- Existing codebase patterns (Metro Areas shadow navigation)
