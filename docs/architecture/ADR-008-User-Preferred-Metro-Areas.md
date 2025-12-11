# ADR-008: User Preferred Metro Areas Architecture

**Status**: Proposed
**Date**: 2025-11-09
**Context**: Phase 5A - User Preferred Metro Areas Implementation
**Decision Makers**: System Architecture Designer

---

## Executive Summary

This ADR provides architectural guidance for implementing user preferred metro areas, allowing users to select multiple geographic regions of interest during registration and profile management. The system must support database-driven metro area selection with proper domain modeling, data integrity, and scalability.

---

## Context and Problem Statement

Users need the ability to:
1. Select multiple preferred metro areas during registration (optional)
2. Manage preferred metro areas in their profile settings
3. See community activity filtered by "My Preferred Metro Areas" instead of location-based "Nearby"
4. Have metro areas persist across sessions in a database-driven manner

### Requirements
- Support 0-10 metro areas per user (0 during registration, manageable later)
- Database-driven (not hardcoded)
- Proper validation and data integrity
- Clean Architecture + DDD compliance
- Efficient querying for dashboard filters
- Support for future features (notifications, recommendations)

---

## Decision Drivers

1. **DDD Aggregate Boundaries**: Where does metro area preference belong?
2. **Data Integrity**: How to ensure referential integrity?
3. **Scalability**: Many-to-many relationship performance
4. **Validation Strategy**: Domain vs. Application layer
5. **EF Core Patterns**: Owned entities vs. explicit junction tables
6. **Event Sourcing**: Need for domain events?
7. **User Experience**: Registration flow complexity

---

## Considered Options

### Option 1: User Aggregate Owns Metro Area IDs (RECOMMENDED)

**Approach**: Add `PreferredMetroAreaIds` collection directly to User aggregate.

**Pros**:
- Simple and pragmatic
- User preferences are cohesive with User identity
- No new aggregate required
- Easy to query and validate
- Aligns with existing patterns (CulturalInterests, Languages)

**Cons**:
- Slightly larger User aggregate (still manageable)
- Metro area validation requires repository access

**Architecture**:
```csharp
// User.cs
private readonly List<Guid> _preferredMetroAreaIds = new();
public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();

public Result UpdatePreferredMetroAreas(List<Guid> metroAreaIds)
{
    // Validation: max 10 metro areas
    if (metroAreaIds.Count > 10)
        return Result.Failure("Cannot select more than 10 preferred metro areas");

    // Validation: no duplicates
    if (metroAreaIds.Distinct().Count() != metroAreaIds.Count)
        return Result.Failure("Duplicate metro area IDs detected");

    _preferredMetroAreaIds.Clear();
    _preferredMetroAreaIds.AddRange(metroAreaIds);

    MarkAsUpdated();
    RaiseDomainEvent(new UserPreferredMetroAreasUpdatedEvent(Id, _preferredMetroAreaIds.AsReadOnly()));

    return Result.Success();
}
```

### Option 2: Separate UserPreferences Aggregate

**Approach**: Create dedicated `UserPreferences` aggregate root.

**Pros**:
- Cleaner separation of concerns
- Could handle other preferences (notifications, theme, etc.)
- Smaller aggregates

**Cons**:
- Over-engineering for current requirements
- Additional complexity (2 repositories, 2 aggregates)
- Eventual consistency challenges
- More database queries for user data

**Verdict**: Rejected - YAGNI (You Aren't Gonna Need It). Current preferences fit naturally in User aggregate.

### Option 3: Domain Service for Metro Area Validation

**Approach**: Use domain service to validate metro area existence.

**Pros**:
- Pure DDD approach
- Domain layer fully self-contained
- No infrastructure concerns in domain

**Cons**:
- Requires repository in domain layer (controversial)
- Adds complexity for little benefit
- Database FK constraint already enforces integrity

**Verdict**: Rejected - Pragmatic approach preferred (see Option 4).

### Option 4: Application Layer + Database Constraint Validation (RECOMMENDED)

**Approach**:
1. Domain validates business rules (count, duplicates)
2. Application layer validates metro areas exist (via repository check)
3. Database enforces referential integrity via FK constraint

**Pros**:
- Clean separation: Domain = business rules, Application = orchestration
- Database provides ultimate safety net
- No repository dependency in domain layer
- Pragmatic and maintainable

**Cons**:
- Validation split across layers (by design)

**Architecture**:
```csharp
// UpdateUserPreferredMetroAreasHandler.cs (Application Layer)
public async Task<Result> Handle(UpdateUserPreferredMetroAreasCommand request)
{
    // Application-layer validation: Check metro areas exist
    var metroAreas = await _metroAreaRepository.GetByIdsAsync(request.MetroAreaIds);
    if (metroAreas.Count != request.MetroAreaIds.Count)
        return Result.Failure("One or more metro area IDs are invalid");

    // Load user and apply domain logic
    var user = await _userRepository.GetByIdAsync(request.UserId);
    var result = user.UpdatePreferredMetroAreas(request.MetroAreaIds);

    if (result.IsFailure)
        return result;

    await _userRepository.UpdateAsync(user);
    return Result.Success();
}
```

---

## Database Design Decision

### Option A: EF Core Many-to-Many (Built-in)

**Approach**: Use EF Core 5+ implicit many-to-many.

```csharp
builder.HasMany(u => u.PreferredMetroAreas)
    .WithMany()
    .UsingEntity(j => j.ToTable("user_preferred_metro_areas", "identity"));
```

**Pros**:
- Simple configuration
- Less code

**Cons**:
- Less control over junction table
- Harder to add audit columns later
- Cannot add additional properties easily

### Option B: Explicit Junction Table (RECOMMENDED)

**Approach**: Create explicit junction table with full control.

```sql
CREATE TABLE identity.user_preferred_metro_areas (
    user_id uuid NOT NULL,
    metro_area_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, metro_area_id),
    FOREIGN KEY (user_id) REFERENCES identity.users(id) ON DELETE CASCADE,
    FOREIGN KEY (metro_area_id) REFERENCES events.metro_areas(id) ON DELETE CASCADE
);

CREATE INDEX idx_user_preferred_metro_areas_user_id ON identity.user_preferred_metro_areas(user_id);
CREATE INDEX idx_user_preferred_metro_areas_metro_area_id ON identity.user_preferred_metro_areas(metro_area_id);
```

**EF Core Configuration**:
```csharp
// UserConfiguration.cs
builder.HasMany<MetroArea>()
    .WithMany()
    .UsingEntity<Dictionary<string, object>>(
        "UserPreferredMetroArea",
        j => j.HasOne<MetroArea>()
            .WithMany()
            .HasForeignKey("MetroAreaId")
            .OnDelete(DeleteBehavior.Cascade),
        j => j.HasOne<User>()
            .WithMany()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade),
        j =>
        {
            j.ToTable("user_preferred_metro_areas", "identity");
            j.HasKey("UserId", "MetroAreaId");

            j.Property<DateTime>("CreatedAt")
                .HasDefaultValueSql("now()")
                .IsRequired();

            j.HasIndex("UserId")
                .HasDatabaseName("idx_user_preferred_metro_areas_user_id");

            j.HasIndex("MetroAreaId")
                .HasDatabaseName("idx_user_preferred_metro_areas_metro_area_id");
        });
```

**Rationale**: Explicit junction table provides:
1. Full control over schema
2. Audit trail (CreatedAt)
3. Future extensibility (e.g., notification preferences per metro)
4. Clear ownership (identity schema for user data)

---

## Domain Events Decision

### Should we raise domain events?

**YES - RECOMMENDED**

**Event**: `UserPreferredMetroAreasUpdatedEvent`

**Rationale**:
1. **Integration**: Other bounded contexts may need to react (e.g., Notifications)
2. **Analytics**: Track user preferences for insights
3. **Audit**: Record when users change preferences
4. **Future Features**: Recommendations, trending areas

**Implementation**:
```csharp
public class UserPreferredMetroAreasUpdatedEvent : IDomainEvent
{
    public Guid UserId { get; }
    public IReadOnlyList<Guid> MetroAreaIds { get; }
    public DateTime OccurredAt { get; }

    public UserPreferredMetroAreasUpdatedEvent(Guid userId, IReadOnlyList<Guid> metroAreaIds)
    {
        UserId = userId;
        MetroAreaIds = metroAreaIds;
        OccurredAt = DateTime.UtcNow;
    }
}
```

---

## Registration Flow Decision

### Option A: Required Field
**Approach**: Force users to select at least 1 metro area during registration.

**Pros**:
- Better initial data
- Improved onboarding

**Cons**:
- Friction in registration
- Users may not know preferences yet

### Option B: Optional Field (RECOMMENDED)
**Approach**: Allow 0 metro areas during registration, prompt in dashboard/profile.

**Pros**:
- Lower registration friction
- Users can explore first
- Can be set later in settings

**Cons**:
- Some users may never set preferences

**Rationale**: Epic 1 Phase 2 established principle of "progressive disclosure" - collect data when needed, not upfront. Align with this pattern.

---

## Deleted Metro Areas Handling

**Decision**: `ON DELETE CASCADE` (RECOMMENDED)

**Rationale**:
1. Metro areas are rarely deleted (soft-delete preferred in production)
2. User preferences become invalid if metro no longer exists
3. Automatic cleanup prevents orphaned data
4. User can re-select preferences

**Alternative Considered**: `ON DELETE SET NULL`
- Not applicable for junction table (no nullable column)

**Best Practice**: Implement soft-delete for MetroArea (IsActive flag) rather than hard deletes in production.

---

## Recommended Architecture

### 1. Domain Layer

**C:\Work\LankaConnect\src\LankaConnect.Domain\Users\User.cs**
```csharp
// Add to User aggregate
private readonly List<Guid> _preferredMetroAreaIds = new();
public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();

/// <summary>
/// Updates user's preferred metro areas for community activity filtering
/// Business rules: Maximum 10 metro areas, no duplicates
/// </summary>
public Result UpdatePreferredMetroAreas(List<Guid> metroAreaIds)
{
    // Defensive copy
    var ids = metroAreaIds?.ToList() ?? new List<Guid>();

    // Business rule: Maximum 10 metro areas
    if (ids.Count > 10)
        return Result.Failure("Cannot select more than 10 preferred metro areas");

    // Business rule: No duplicates
    if (ids.Distinct().Count() != ids.Count)
        return Result.Failure("Duplicate metro area IDs detected");

    // Apply changes
    _preferredMetroAreaIds.Clear();
    _preferredMetroAreaIds.AddRange(ids);

    MarkAsUpdated();

    // Raise domain event for integration
    RaiseDomainEvent(new UserPreferredMetroAreasUpdatedEvent(Id, _preferredMetroAreaIds.AsReadOnly()));

    return Result.Success();
}
```

**C:\Work\LankaConnect\src\LankaConnect.Domain\Users\Events\UserPreferredMetroAreasUpdatedEvent.cs**
```csharp
namespace LankaConnect.Domain.Users.Events;

public class UserPreferredMetroAreasUpdatedEvent : IDomainEvent
{
    public Guid UserId { get; }
    public IReadOnlyList<Guid> MetroAreaIds { get; }
    public DateTime OccurredAt { get; }

    public UserPreferredMetroAreasUpdatedEvent(Guid userId, IReadOnlyList<Guid> metroAreaIds)
    {
        UserId = userId;
        MetroAreaIds = metroAreaIds;
        OccurredAt = DateTime.UtcNow;
    }
}
```

**C:\Work\LankaConnect\src\LankaConnect.Domain\Users\IUserRepository.cs**
```csharp
// Add method to interface
Task<User?> GetByIdWithPreferredMetroAreasAsync(Guid userId, CancellationToken cancellationToken = default);
```

**C:\Work\LankaConnect\src\LankaConnect.Domain\Events\IMetroAreaRepository.cs** (NEW)
```csharp
namespace LankaConnect.Domain.Events;

public interface IMetroAreaRepository : IRepository<MetroArea>
{
    Task<IReadOnlyList<MetroArea>> GetActiveMetroAreasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetroArea>> GetByStateAsync(string state, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetroArea>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
}
```

### 2. Infrastructure Layer

**C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\UserConfiguration.cs**
```csharp
// Add to Configure method in UserConfiguration class

// Configure PreferredMetroAreas many-to-many relationship
// Store as junction table: identity.user_preferred_metro_areas
builder.HasMany<MetroArea>()
    .WithMany()
    .UsingEntity<Dictionary<string, object>>(
        "UserPreferredMetroArea",
        j => j.HasOne<MetroArea>()
            .WithMany()
            .HasForeignKey("MetroAreaId")
            .HasConstraintName("fk_user_preferred_metro_areas_metro_area_id")
            .OnDelete(DeleteBehavior.Cascade),
        j => j.HasOne<User>()
            .WithMany()
            .HasForeignKey("UserId")
            .HasConstraintName("fk_user_preferred_metro_areas_user_id")
            .OnDelete(DeleteBehavior.Cascade),
        j =>
        {
            j.ToTable("user_preferred_metro_areas", "identity");

            // Composite primary key
            j.HasKey("UserId", "MetroAreaId");

            // Audit column
            j.Property<DateTime>("CreatedAt")
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()")
                .IsRequired();

            // Indexes for efficient querying
            j.HasIndex("UserId")
                .HasDatabaseName("idx_user_preferred_metro_areas_user_id");

            j.HasIndex("MetroAreaId")
                .HasDatabaseName("idx_user_preferred_metro_areas_metro_area_id");
        });
```

**C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\MetroAreaRepository.cs** (NEW)
```csharp
using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Infrastructure.Data;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class MetroAreaRepository : IMetroAreaRepository
{
    private readonly AppDbContext _context;

    public MetroAreaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MetroArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MetroAreas
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MetroArea>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MetroAreas
            .OrderBy(m => m.State)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetroArea>> GetActiveMetroAreasAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MetroAreas
            .Where(m => m.IsActive)
            .OrderBy(m => m.State)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetroArea>> GetByStateAsync(string state, CancellationToken cancellationToken = default)
    {
        return await _context.MetroAreas
            .Where(m => m.State == state && m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetroArea>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.MetroAreas
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);
    }

    // IRepository implementation
    public Task AddAsync(MetroArea entity, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("MetroArea is a read-only entity managed by database seeding");
    }

    public Task UpdateAsync(MetroArea entity, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("MetroArea is a read-only entity managed by database seeding");
    }

    public Task DeleteAsync(MetroArea entity, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("MetroArea is a read-only entity managed by database seeding");
    }
}
```

**C:\Work\LankaConnect\src\LankaConnect.Infrastructure\DependencyInjection.cs**
```csharp
// Add to ConfigureInfrastructure method
services.AddScoped<IMetroAreaRepository, MetroAreaRepository>();
```

### 3. Application Layer

**C:\Work\LankaConnect\src\LankaConnect.Application\Users\Commands\UpdatePreferredMetroAreas\UpdateUserPreferredMetroAreasCommand.cs** (NEW)
```csharp
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Users.Commands.UpdatePreferredMetroAreas;

public class UpdateUserPreferredMetroAreasCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
    public List<Guid> MetroAreaIds { get; set; } = new();
}
```

**C:\Work\LankaConnect\src\LankaConnect.Application\Users\Commands\UpdatePreferredMetroAreas\UpdateUserPreferredMetroAreasValidator.cs** (NEW)
```csharp
using FluentValidation;

namespace LankaConnect.Application.Users.Commands.UpdatePreferredMetroAreas;

public class UpdateUserPreferredMetroAreasValidator : AbstractValidator<UpdateUserPreferredMetroAreasCommand>
{
    public UpdateUserPreferredMetroAreasValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.MetroAreaIds)
            .NotNull()
            .WithMessage("Metro area IDs list cannot be null");

        RuleFor(x => x.MetroAreaIds)
            .Must(ids => ids.Count <= 10)
            .WithMessage("Cannot select more than 10 metro areas");
    }
}
```

**C:\Work\LankaConnect\src\LankaConnect.Application\Users\Commands\UpdatePreferredMetroAreas\UpdateUserPreferredMetroAreasHandler.cs** (NEW)
```csharp
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users;
using MediatR;

namespace LankaConnect.Application.Users.Commands.UpdatePreferredMetroAreas;

public class UpdateUserPreferredMetroAreasHandler : IRequestHandler<UpdateUserPreferredMetroAreasCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IMetroAreaRepository _metroAreaRepository;
    private readonly IApplicationDbContext _context;

    public UpdateUserPreferredMetroAreasHandler(
        IUserRepository userRepository,
        IMetroAreaRepository metroAreaRepository,
        IApplicationDbContext context)
    {
        _userRepository = userRepository;
        _metroAreaRepository = metroAreaRepository;
        _context = context;
    }

    public async Task<Result> Handle(UpdateUserPreferredMetroAreasCommand request, CancellationToken cancellationToken)
    {
        // Load user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result.Failure("User not found");

        // Application-layer validation: Verify all metro areas exist and are active
        if (request.MetroAreaIds.Any())
        {
            var metroAreas = await _metroAreaRepository.GetByIdsAsync(request.MetroAreaIds, cancellationToken);

            // Check all IDs are valid
            if (metroAreas.Count != request.MetroAreaIds.Count)
                return Result.Failure("One or more metro area IDs are invalid");

            // Check all are active
            if (metroAreas.Any(m => !m.IsActive))
                return Result.Failure("One or more metro areas are inactive");
        }

        // Apply domain logic
        var result = user.UpdatePreferredMetroAreas(request.MetroAreaIds);
        if (result.IsFailure)
            return result;

        // Persist changes
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _context.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
```

**C:\Work\LankaConnect\src\LankaConnect.Application\Users\Queries\GetUserPreferredMetroAreas\GetUserPreferredMetroAreasQuery.cs** (NEW)
```csharp
using LankaConnect.Application.Common.DTOs;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Users.Queries.GetUserPreferredMetroAreas;

public class GetUserPreferredMetroAreasQuery : IRequest<Result<List<MetroAreaDto>>>
{
    public Guid UserId { get; set; }
}
```

**C:\Work\LankaConnect\src\LankaConnect.Application\Users\Queries\GetUserPreferredMetroAreas\GetUserPreferredMetroAreasHandler.cs** (NEW)
```csharp
using LankaConnect.Application.Common.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Users.Queries.GetUserPreferredMetroAreas;

public class GetUserPreferredMetroAreasHandler : IRequestHandler<GetUserPreferredMetroAreasQuery, Result<List<MetroAreaDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMetroAreaRepository _metroAreaRepository;

    public GetUserPreferredMetroAreasHandler(
        IUserRepository userRepository,
        IMetroAreaRepository metroAreaRepository)
    {
        _userRepository = userRepository;
        _metroAreaRepository = metroAreaRepository;
    }

    public async Task<Result<List<MetroAreaDto>>> Handle(GetUserPreferredMetroAreasQuery request, CancellationToken cancellationToken)
    {
        // Load user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result<List<MetroAreaDto>>.Failure("User not found");

        // If no preferences, return empty list
        if (!user.PreferredMetroAreaIds.Any())
            return Result<List<MetroAreaDto>>.Success(new List<MetroAreaDto>());

        // Load metro areas
        var metroAreas = await _metroAreaRepository.GetByIdsAsync(user.PreferredMetroAreaIds.ToList(), cancellationToken);

        // Map to DTOs
        var dtos = metroAreas
            .Select(m => new MetroAreaDto
            {
                Id = m.Id,
                Name = m.Name,
                State = m.State,
                CenterLatitude = m.CenterLatitude,
                CenterLongitude = m.CenterLongitude,
                RadiusMiles = m.RadiusMiles,
                IsStateLevelArea = m.IsStateLevelArea,
                IsActive = m.IsActive
            })
            .OrderBy(m => m.State)
            .ThenBy(m => m.Name)
            .ToList();

        return Result<List<MetroAreaDto>>.Success(dtos);
    }
}
```

**Update RegisterUserCommand.cs**
```csharp
// Add optional property
public List<Guid>? PreferredMetroAreaIds { get; set; }
```

**Update RegisterUserHandler.cs**
```csharp
// After user creation, set preferred metro areas if provided
if (request.PreferredMetroAreaIds?.Any() == true)
{
    // Validate metro areas exist (application-layer concern)
    var metroAreas = await _metroAreaRepository.GetByIdsAsync(request.PreferredMetroAreaIds, cancellationToken);
    if (metroAreas.Count != request.PreferredMetroAreaIds.Count)
        return Result<RegisterUserResponse>.Failure("One or more metro area IDs are invalid");

    var updateResult = user.UpdatePreferredMetroAreas(request.PreferredMetroAreaIds);
    if (updateResult.IsFailure)
        return Result<RegisterUserResponse>.Failure(updateResult.Error);
}
```

### 4. API Layer

**C:\Work\LankaConnect\src\LankaConnect.API\Controllers\UsersController.cs**
```csharp
/// <summary>
/// Updates user's preferred metro areas
/// </summary>
[HttpPut("{userId:guid}/preferred-metro-areas")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdatePreferredMetroAreas(
    Guid userId,
    [FromBody] UpdatePreferredMetroAreasRequest request)
{
    // Authorization: Users can only update their own preferences
    var currentUserId = User.GetUserId();
    if (currentUserId != userId)
        return Forbid();

    var command = new UpdateUserPreferredMetroAreasCommand
    {
        UserId = userId,
        MetroAreaIds = request.MetroAreaIds
    };

    var result = await _mediator.Send(command);

    return result.IsSuccess
        ? Ok()
        : BadRequest(new { error = result.Error });
}

/// <summary>
/// Gets user's preferred metro areas
/// </summary>
[HttpGet("{userId:guid}/preferred-metro-areas")]
[Authorize]
[ProducesResponseType(typeof(List<MetroAreaDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetPreferredMetroAreas(Guid userId)
{
    // Authorization: Users can view their own preferences, admins can view any
    var currentUserId = User.GetUserId();
    var isAdmin = User.IsInRole("Admin");

    if (currentUserId != userId && !isAdmin)
        return Forbid();

    var query = new GetUserPreferredMetroAreasQuery { UserId = userId };
    var result = await _mediator.Send(query);

    return result.IsSuccess
        ? Ok(result.Value)
        : NotFound(new { error = result.Error });
}

public class UpdatePreferredMetroAreasRequest
{
    public List<Guid> MetroAreaIds { get; set; } = new();
}
```

**Update RegisterRequest.cs**
```csharp
// Add optional property
public List<Guid>? PreferredMetroAreaIds { get; set; }
```

### 5. Database Migration

**Create Migration**:
```bash
cd src/LankaConnect.Infrastructure
dotnet ef migrations add AddUserPreferredMetroAreas --context AppDbContext --startup-project ../LankaConnect.API
```

**Expected Migration SQL**:
```sql
CREATE TABLE identity.user_preferred_metro_areas (
    user_id uuid NOT NULL,
    metro_area_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_user_preferred_metro_areas PRIMARY KEY (user_id, metro_area_id),
    CONSTRAINT fk_user_preferred_metro_areas_user_id FOREIGN KEY (user_id)
        REFERENCES identity.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_preferred_metro_areas_metro_area_id FOREIGN KEY (metro_area_id)
        REFERENCES events.metro_areas(id) ON DELETE CASCADE
);

CREATE INDEX idx_user_preferred_metro_areas_user_id
    ON identity.user_preferred_metro_areas(user_id);

CREATE INDEX idx_user_preferred_metro_areas_metro_area_id
    ON identity.user_preferred_metro_areas(metro_area_id);
```

### 6. Frontend Implementation

**Components to Create**:
1. `web/src/presentation/components/features/metro-areas/MetroAreaMultiSelect.tsx`
2. Update `web/src/presentation/components/features/auth/RegisterForm.tsx`
3. Update `web/src/app/(dashboard)/profile/page.tsx`
4. Create `web/src/presentation/components/features/profile/PreferredMetroAreasSection.tsx`

**API Type Definitions** (`web/src/infrastructure/api/types/user.types.ts`):
```typescript
export interface UpdatePreferredMetroAreasRequest {
  metroAreaIds: string[];
}

export interface MetroAreaDto {
  id: string;
  name: string;
  state: string;
  centerLatitude: number;
  centerLongitude: number;
  radiusMiles: number;
  isStateLevelArea: boolean;
  isActive: boolean;
}
```

---

## Answers to Specific Questions

### 1. Should User aggregate own the metro area IDs?

**YES - RECOMMENDED**

**Rationale**:
- Metro area preferences are core to user identity and experience
- Small, cohesive collection (max 10 IDs)
- Aligns with existing User aggregate patterns (CulturalInterests, Languages)
- No need for separate aggregate at this scale
- Simple to query and maintain

**Aggregate Boundary**: User aggregate is appropriate because:
- Preferences are always accessed with User data
- Transactional consistency naturally aligned
- No independent lifecycle from User

### 2. Should we validate that metro area IDs exist?

**HYBRID APPROACH - RECOMMENDED (Option C)**

**Implementation**:
1. **Domain Layer**: Validates business rules (count, duplicates)
2. **Application Layer**: Validates metro areas exist via repository
3. **Database**: Enforces referential integrity via FK constraint

**Rationale**:
- Clean separation of concerns
- Domain remains pure (no infrastructure dependencies)
- Application orchestrates validation
- Database provides safety net
- Pragmatic and maintainable

### 3. How should we handle the many-to-many relationship in EF Core?

**EXPLICIT JUNCTION TABLE - RECOMMENDED (Option A from Database section)**

**Rationale**:
- Full control over schema and indexes
- Audit column (CreatedAt) for analytics
- Future extensibility (e.g., NotifyForArea flag)
- Clear ownership (identity schema)
- Better performance tuning options

### 4. Should PreferredMetroAreaIds be part of User aggregate?

**YES - RECOMMENDED**

**Rationale**:
- Cohesive with User bounded context
- No independent lifecycle
- Always accessed with User data
- Simpler than separate bounded context
- Follows principle of "aggregates as transaction boundaries"

### 5. Should metro selection be required or optional during registration?

**OPTIONAL - RECOMMENDED**

**Rationale**:
- Reduces registration friction
- Aligns with "progressive disclosure" pattern (Epic 1 Phase 2)
- Users can explore before committing
- Can be prompted later in dashboard/profile
- Better user experience for newcomers

### 6. Should we create a domain event?

**YES - RECOMMENDED**

**Event**: `UserPreferredMetroAreasUpdatedEvent`

**Rationale**:
- **Integration**: Other bounded contexts can react (Notifications, Analytics)
- **Audit Trail**: Track preference changes over time
- **Future Features**: Recommendations, trending areas, community insights
- **Event Sourcing**: Enable future event-driven architecture

### 7. How should we handle deleted metro areas?

**ON DELETE CASCADE - RECOMMENDED**

**Rationale**:
- User preferences become invalid if metro no longer exists
- Automatic cleanup prevents orphaned data
- Metro areas rarely deleted (use soft-delete: IsActive flag)
- User can re-select preferences

**Best Practice**: Implement soft-delete pattern (IsActive = false) rather than hard deletes in production.

---

## Testing Strategy

### 1. Domain Layer Tests
```csharp
// UserTests.cs - TDD Red-Green-Refactor

[Fact]
public void UpdatePreferredMetroAreas_WithValidIds_ShouldSucceed()
{
    // Arrange
    var user = CreateTestUser();
    var metroAreaIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

    // Act
    var result = user.UpdatePreferredMetroAreas(metroAreaIds);

    // Assert
    result.IsSuccess.Should().BeTrue();
    user.PreferredMetroAreaIds.Should().HaveCount(2);
    user.PreferredMetroAreaIds.Should().BeEquivalentTo(metroAreaIds);
}

[Fact]
public void UpdatePreferredMetroAreas_WithMoreThan10Areas_ShouldFail()
{
    // Arrange
    var user = CreateTestUser();
    var metroAreaIds = Enumerable.Range(0, 11).Select(_ => Guid.NewGuid()).ToList();

    // Act
    var result = user.UpdatePreferredMetroAreas(metroAreaIds);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("10 preferred metro areas");
}

[Fact]
public void UpdatePreferredMetroAreas_WithDuplicates_ShouldFail()
{
    // Arrange
    var user = CreateTestUser();
    var id = Guid.NewGuid();
    var metroAreaIds = new List<Guid> { id, id };

    // Act
    var result = user.UpdatePreferredMetroAreas(metroAreaIds);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("Duplicate");
}

[Fact]
public void UpdatePreferredMetroAreas_ShouldRaiseDomainEvent()
{
    // Arrange
    var user = CreateTestUser();
    var metroAreaIds = new List<Guid> { Guid.NewGuid() };

    // Act
    user.UpdatePreferredMetroAreas(metroAreaIds);

    // Assert
    user.DomainEvents.Should().ContainSingle(e => e is UserPreferredMetroAreasUpdatedEvent);
}
```

### 2. Application Layer Tests
```csharp
// UpdateUserPreferredMetroAreasHandlerTests.cs

[Fact]
public async Task Handle_WithInvalidMetroAreaIds_ShouldFail()
{
    // Arrange
    var handler = CreateHandler();
    var command = new UpdateUserPreferredMetroAreasCommand
    {
        UserId = Guid.NewGuid(),
        MetroAreaIds = new List<Guid> { Guid.NewGuid() }
    };

    _metroAreaRepository
        .GetByIdsAsync(command.MetroAreaIds, default)
        .Returns(new List<MetroArea>()); // No metros found

    // Act
    var result = await handler.Handle(command, default);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("invalid");
}
```

### 3. Integration Tests
```csharp
// UsersControllerIntegrationTests.cs

[Fact]
public async Task UpdatePreferredMetroAreas_AsAuthenticatedUser_ShouldSucceed()
{
    // Arrange
    var user = await CreateAndAuthenticateUser();
    var metroArea = await CreateMetroArea();

    var request = new UpdatePreferredMetroAreasRequest
    {
        MetroAreaIds = new List<Guid> { metroArea.Id }
    };

    // Act
    var response = await _client.PutAsJsonAsync(
        $"/api/users/{user.Id}/preferred-metro-areas",
        request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify in database
    var updatedUser = await GetUserFromDatabase(user.Id);
    updatedUser.PreferredMetroAreaIds.Should().Contain(metroArea.Id);
}
```

---

## Performance Considerations

### Query Optimization

1. **Indexed Foreign Keys**: Both `user_id` and `metro_area_id` indexed for efficient lookups
2. **Composite Primary Key**: Prevents duplicate entries and enables fast lookups
3. **Eager Loading**: Use `Include()` when loading user with preferences
4. **Batch Queries**: Load multiple metro areas in single query

### Example Efficient Query
```csharp
// Efficient: Single query with Include
var user = await _context.Users
    .Include(u => u.PreferredMetroAreas)
    .FirstOrDefaultAsync(u => u.Id == userId);

// Inefficient: Multiple queries (N+1 problem)
var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
foreach (var id in user.PreferredMetroAreaIds)
{
    var metro = await _context.MetroAreas.FirstOrDefaultAsync(m => m.Id == id); // N+1!
}
```

### Caching Strategy
```csharp
// Cache metro areas (they rarely change)
public class CachedMetroAreaRepository : IMetroAreaRepository
{
    private readonly IMetroAreaRepository _inner;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "metro_areas_all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<MetroArea>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _inner.GetAllAsync(cancellationToken);
        });
    }
}
```

---

## Security Considerations

1. **Authorization**: Users can only update their own preferences
2. **Validation**: Prevent injection of invalid metro area IDs
3. **Rate Limiting**: Prevent abuse of preference update endpoint
4. **Audit Trail**: CreatedAt column tracks when preferences were set

**Example Authorization**:
```csharp
[HttpPut("{userId:guid}/preferred-metro-areas")]
[Authorize]
public async Task<IActionResult> UpdatePreferredMetroAreas(Guid userId, ...)
{
    var currentUserId = User.GetUserId();
    if (currentUserId != userId)
        return Forbid(); // 403 Forbidden

    // ... rest of implementation
}
```

---

## Migration Rollback Strategy

**If something goes wrong**:
1. Rollback migration: `dotnet ef database update <previous-migration>`
2. User data preserved (CASCADE only removes junction entries)
3. No data loss (User aggregate unchanged)

**Best Practice**: Test migration on staging environment first.

---

## Future Enhancements

1. **Notification Preferences**: Add `NotifyForArea` flag to junction table
2. **Recommendation Engine**: Use preferred metros for personalized content
3. **Analytics**: Track popular metro areas, user migration patterns
4. **Geofencing**: Trigger notifications when user enters preferred metro
5. **Smart Defaults**: Suggest metros based on user location

---

## Conclusion

**RECOMMENDED ARCHITECTURE**:

1. **User Aggregate Owns IDs**: Simple, cohesive, aligns with existing patterns
2. **Hybrid Validation**: Domain (business rules) + Application (existence) + Database (FK)
3. **Explicit Junction Table**: Full control, audit trail, future-proof
4. **Domain Events**: YES - enables integration and future features
5. **Optional Registration Field**: Reduces friction, progressive disclosure
6. **CASCADE Deletes**: Automatic cleanup (with soft-delete best practice)

**Benefits**:
- Clean Architecture compliance
- DDD best practices
- TDD-ready design
- Scalable and maintainable
- Future-proof for extensions

**Risks Mitigated**:
- Referential integrity via FK constraints
- Duplicate prevention via composite PK
- Authorization via endpoint guards
- Performance via indexes and caching

This architecture balances pragmatism with best practices, providing a solid foundation for Phase 5A implementation.

---

**Next Steps**:
1. Implement domain layer (TDD: write tests first)
2. Implement infrastructure (EF Core config + migration)
3. Implement application layer (commands + queries)
4. Implement API endpoints
5. Implement frontend components
6. Integration testing
7. Documentation update

**Estimated Effort**: 8-12 hours (1.5-2 days)

---

**References**:
- [DDD Aggregate Design](https://www.dddcommunity.org/library/vernon_2011/)
- [EF Core Many-to-Many](https://docs.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
