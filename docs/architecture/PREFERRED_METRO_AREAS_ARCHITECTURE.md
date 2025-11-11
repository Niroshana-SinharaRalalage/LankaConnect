# Preferred Metro Areas - System Architecture

**Date**: 2025-11-09
**Status**: ARCHITECTURAL DESIGN
**Epic**: User Preferred Metro Areas Feature
**Architect**: System Architecture Designer

---

## Executive Summary

This document provides a comprehensive architectural design for implementing a "Preferred Metro Areas" feature in the LankaConnect application, following Clean Architecture and Domain-Driven Design (DDD) principles.

### Key Decisions Summary

| Decision Point | Recommendation | Rationale |
|---------------|----------------|-----------|
| **Data Storage** | Separate junction table: `users.user_preferred_metro_areas` | Single Responsibility Principle; distinct from newsletter |
| **Domain Modeling** | Collection within User aggregate root | Metro preferences are intrinsic to user identity |
| **Newsletter Relationship** | Keep separate from newsletter subscription metros | Different business purposes; user control vs notification preferences |
| **Registration Requirement** | Optional during registration; can set/update later | Privacy-first; progressive disclosure |
| **Metro Update Policy** | Unrestricted updates allowed | Users move frequently; reflects real-world flexibility |

---

## 1. Requirements Analysis

### 1.1 Feature Context

The feature will be used in **three distinct places**:

#### **Use Case 1: Dashboard Community Activity Feed**
- **Current**: "Nearby Metro Areas" dropdown (location-based filtering)
- **New**: "My Preferred Metro Areas" filter
- **Business Logic**: Show events from user's preferred metros (union of all selected areas)
- **Default Behavior**: If no preferences set, show events from user's detected location or all metros

#### **Use Case 2: Newsletter Subscription**
- **Current Design**: Multi-select metro areas for newsletter notifications
- **Relationship**: INDEPENDENT from preferred metros (different business purpose)
- **User Story**: "I live in Cleveland but want newsletters for NYC and LA (where my family lives)"
- **Flexibility**: User can prefer Cleveland but get newsletters for other cities

#### **Use Case 3: User Registration**
- **UX**: Optional step during signup flow
- **Value**: Helps personalize initial feed experience
- **Implementation**: "Select your metro areas" (0-10 selections allowed)
- **Validation**: Not required (can skip and set later in profile)

### 1.2 Non-Functional Requirements

| Requirement | Specification | Rationale |
|-------------|---------------|-----------|
| **Privacy** | Metro areas are OPTIONAL | Respect user privacy; some may not want to disclose |
| **Flexibility** | Allow 0-10 metro areas | Users may have ties to multiple regions |
| **Performance** | Indexed queries; cached results | Feed filtering must be fast (sub-200ms) |
| **Consistency** | User aggregate maintains invariants | Prevent invalid states |
| **Auditability** | Track when preferences change | Useful for analytics and debugging |

---

## 2. Domain-Driven Design Analysis

### 2.1 Aggregate Root Selection

**Question**: Should preferred metro areas be part of the User aggregate or a separate aggregate?

**Answer**: **Part of User aggregate root**

#### Justification

1. **Cohesion**: Metro area preferences are intrinsic attributes of a user's profile
2. **Invariants**: Business rules (e.g., max 10 metros) are enforced by User entity
3. **Transaction Boundary**: Updates to preferences should be atomic with User updates
4. **Lifecycle**: Preferences lifecycle is tied to User lifecycle (deleted when user deleted)
5. **No Independent Identity**: Preferred metros don't exist without a User

#### Anti-Pattern to Avoid
Do NOT create a separate `PreferredMetroArea` aggregate root. This would violate DDD principles:
- Metro preferences have no independent business meaning without a User
- Would require distributed transactions to maintain consistency
- Unnecessary complexity

### 2.2 Entity vs Value Object

**Question**: Should individual preferred metros be entities or value objects?

**Answer**: **Neither - simple collection of Guid (MetroAreaId)**

#### Justification

1. **No Complex Behavior**: A preferred metro is just a reference to `events.metro_areas.id`
2. **No State**: No additional attributes beyond the metro area ID
3. **Simple Collection**: `List<Guid>` is sufficient
4. **Performance**: No need for additional entity overhead
5. **Query Efficiency**: Joining on `user_preferred_metro_areas.metro_area_id` is fast

### 2.3 Relationship to Newsletter Subscription

**Question**: Should preferred metros and newsletter subscription metros be the same?

**Answer**: **NO - Keep them SEPARATE**

#### Business Justification

| Aspect | User Preferred Metros | Newsletter Subscription Metros |
|--------|----------------------|--------------------------------|
| **Purpose** | "Where I have ties/interests" | "Where I want email notifications" |
| **Use Case** | Feed filtering, recommendations | Weekly email digest |
| **User Story** | "I prefer Cleveland and Columbus" | "Email me about NYC events (where my family lives)" |
| **Overlap** | May overlap with newsletter metros | May overlap with preferred metros |
| **Business Rule** | User profile attribute | Subscription preference |

#### Example Scenario
```
User: John Doe
- Lives in: Cleveland, OH
- Preferred Metros: Cleveland, Columbus, Cincinnati (Ohio-centric)
- Newsletter Subscriptions: NYC, Los Angeles (family connections)
```

This separation gives users maximum flexibility and control.

### 2.4 Domain Model Design

```csharp
public class User : BaseEntity, IAggregateRoot
{
    // Existing properties...
    public Email Email { get; private set; }
    public UserLocation? Location { get; private set; }

    // NEW: Preferred Metro Areas
    private readonly List<Guid> _preferredMetroAreaIds = new();
    public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();

    /// <summary>
    /// Updates user's preferred metro areas for feed filtering
    /// Business Rule: Maximum 10 preferred metros allowed
    /// Empty collection clears all preferences (privacy choice)
    /// </summary>
    public Result UpdatePreferredMetroAreas(IEnumerable<Guid>? metroAreaIds)
    {
        var metroList = metroAreaIds?.ToList() ?? new List<Guid>();

        // Validate max 10 metros
        if (metroList.Count > 10)
        {
            return Result.Failure("Cannot have more than 10 preferred metro areas");
        }

        // Clear and add metros (removes duplicates)
        _preferredMetroAreaIds.Clear();
        _preferredMetroAreaIds.AddRange(metroList.Distinct());

        MarkAsUpdated();

        // Raise domain event if preferences changed
        if (_preferredMetroAreaIds.Any())
        {
            RaiseDomainEvent(new PreferredMetroAreasUpdatedEvent(Id, _preferredMetroAreaIds.AsReadOnly()));
        }

        return Result.Success();
    }

    /// <summary>
    /// Helper method to check if user has any preferred metros set
    /// </summary>
    public bool HasPreferredMetroAreas() => _preferredMetroAreaIds.Any();
}
```

---

## 3. Database Schema Design

### 3.1 Junction Table Schema

**Table Name**: `users.user_preferred_metro_areas`

```sql
CREATE TABLE users.user_preferred_metro_areas (
    user_id UUID NOT NULL,
    metro_area_id UUID NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Composite primary key
    PRIMARY KEY (user_id, metro_area_id),

    -- Foreign key constraints
    CONSTRAINT fk_user_preferred_metro_areas_user
        FOREIGN KEY (user_id)
        REFERENCES users.users(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_preferred_metro_areas_metro_area
        FOREIGN KEY (metro_area_id)
        REFERENCES events.metro_areas(id)
        ON DELETE CASCADE
);

-- Index for efficient lookups
CREATE INDEX idx_user_preferred_metro_areas_user
    ON users.user_preferred_metro_areas(user_id);

CREATE INDEX idx_user_preferred_metro_areas_metro
    ON users.user_preferred_metro_areas(metro_area_id);

-- Optional: Audit table for tracking changes
COMMENT ON TABLE users.user_preferred_metro_areas IS
    'Stores user preferred metro areas for feed filtering and personalization.
     Separate from newsletter subscription preferences.';
```

### 3.2 Schema Comparison

| Table | Purpose | Foreign Keys | Business Rules |
|-------|---------|--------------|----------------|
| `users.user_preferred_metro_areas` | Feed filtering, profile preferences | users.users, events.metro_areas | 0-10 metros per user |
| `communications.newsletter_subscriber_metro_areas` | Newsletter notifications | newsletter_subscribers, events.metro_areas | 1+ metros if not "All Locations" |
| `users.users.location` | Current/primary location | None (value object) | Single location, optional |

### 3.3 Migration Strategy

**Migration File**: `[Timestamp]_AddUserPreferredMetroAreas.cs`

```csharp
public partial class AddUserPreferredMetroAreas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create junction table
        migrationBuilder.CreateTable(
            name: "user_preferred_metro_areas",
            schema: "users",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                metro_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false,
                    defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_preferred_metro_areas",
                    x => new { x.user_id, x.metro_area_id });

                table.ForeignKey(
                    name: "fk_user_preferred_metro_areas_user",
                    column: x => x.user_id,
                    principalSchema: "users",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);

                table.ForeignKey(
                    name: "fk_user_preferred_metro_areas_metro_area",
                    column: x => x.metro_area_id,
                    principalSchema: "events",
                    principalTable: "metro_areas",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "idx_user_preferred_metro_areas_user",
            schema: "users",
            table: "user_preferred_metro_areas",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "idx_user_preferred_metro_areas_metro",
            schema: "users",
            table: "user_preferred_metro_areas",
            column: "metro_area_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "user_preferred_metro_areas",
            schema: "users");
    }
}
```

### 3.4 Data Migration Considerations

**Question**: Should we migrate existing user location data to preferred metros?

**Answer**: **NO - Keep them separate**

**Rationale**:
1. `UserLocation` (city/state/zip) is NOT the same as metro area preferences
2. UserLocation is more precise (specific city/zip), metro areas are broader regions
3. Users should explicitly choose their preferred metros (explicit consent)
4. Auto-migration could create incorrect assumptions

**Alternative**: Provide UI suggestion based on location
```typescript
// In registration/profile UI
if (user.location && !user.preferredMetroAreas.length) {
  suggestedMetro = findMetroAreaByLocation(user.location);
  showSuggestion("Would you like to add [suggestedMetro] to your preferred metros?");
}
```

---

## 4. Application Layer (CQRS)

### 4.1 Command: Update Preferred Metro Areas

**File**: `src/LankaConnect.Application/Users/Commands/UpdatePreferredMetroAreas/UpdatePreferredMetroAreasCommand.cs`

```csharp
public record UpdatePreferredMetroAreasCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public List<Guid> MetroAreaIds { get; init; } = new();
}

public class UpdatePreferredMetroAreasCommandHandler
    : IRequestHandler<UpdatePreferredMetroAreasCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result> Handle(
        UpdatePreferredMetroAreasCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Get user
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            return Result.Failure("User not found");

        // 2. Validate metro areas exist (optional - can defer to FK constraint)
        // For better UX, validate before updating
        if (request.MetroAreaIds.Any())
        {
            var validMetros = await _metroAreaRepository
                .ExistAsync(request.MetroAreaIds);

            if (!validMetros)
                return Result.Failure("One or more metro areas do not exist");
        }

        // 3. Update user aggregate
        var updateResult = user.UpdatePreferredMetroAreas(request.MetroAreaIds);
        if (updateResult.IsFailure)
            return updateResult;

        // 4. Persist
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### 4.2 Query: Get User Profile with Preferred Metros

**File**: `src/LankaConnect.Application/Users/Queries/GetUserProfile/UserProfileDto.cs`

```csharp
public record UserProfileDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;

    // Existing properties...
    public UserLocationDto? Location { get; init; }
    public List<string> CulturalInterests { get; init; } = new();
    public List<LanguageDto> Languages { get; init; } = new();

    // NEW: Preferred metro areas
    public List<PreferredMetroAreaDto> PreferredMetroAreas { get; init; } = new();
}

public record PreferredMetroAreaDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public bool IsStateLevelArea { get; init; }
}
```

**Handler Enhancement**:
```csharp
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository
            .GetByIdWithPreferredMetrosAsync(request.UserId);

        if (user == null)
            return Result<UserProfileDto>.Failure("User not found");

        // Map preferred metros (join with metro_areas table for names)
        var preferredMetros = await _metroAreaRepository
            .GetByIdsAsync(user.PreferredMetroAreaIds);

        var dto = new UserProfileDto
        {
            // ... existing mappings
            PreferredMetroAreas = preferredMetros.Select(m => new PreferredMetroAreaDto
            {
                Id = m.Id,
                Name = m.Name,
                State = m.State,
                IsStateLevelArea = m.IsStateLevelArea
            }).ToList()
        };

        return Result<UserProfileDto>.Success(dto);
    }
}
```

---

## 5. Infrastructure Layer

### 5.1 EF Core Configuration

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs`

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "users");

        // ... existing configuration

        // Configure preferred metro areas (many-to-many via junction table)
        builder.HasMany<MetroArea>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "user_preferred_metro_areas",
                j => j.HasOne<MetroArea>()
                      .WithMany()
                      .HasForeignKey("metro_area_id")
                      .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<User>()
                      .WithMany()
                      .HasForeignKey("user_id")
                      .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("user_preferred_metro_areas", "users");
                    j.HasKey("user_id", "metro_area_id");
                    j.Property<DateTime>("created_at")
                        .HasDefaultValueSql("NOW()");
                });
    }
}
```

### 5.2 Repository Enhancement

**File**: `src/LankaConnect.Infrastructure/Data/Repositories/UserRepository.cs`

```csharp
public class UserRepository : IUserRepository
{
    public async Task<User?> GetByIdWithPreferredMetrosAsync(Guid userId)
    {
        return await _context.Users
            .Include("_preferredMetroAreaIds") // EF Core will handle junction table
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<List<User>> GetUsersByPreferredMetroAsync(Guid metroAreaId)
    {
        // Find all users who have this metro in their preferences
        return await _context.Users
            .Where(u => u.PreferredMetroAreaIds.Contains(metroAreaId))
            .ToListAsync();
    }
}
```

---

## 6. API Layer

### 6.1 Controller Endpoint

**File**: `src/LankaConnect.API/Controllers/UsersController.cs`

```csharp
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    [HttpPut("{userId}/preferred-metros")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferredMetroAreas(
        Guid userId,
        [FromBody] UpdatePreferredMetroAreasRequest request)
    {
        // Verify user can only update their own preferences
        var currentUserId = User.GetUserId();
        if (userId != currentUserId && !User.IsInRole("Admin"))
            return Forbid();

        var command = new UpdatePreferredMetroAreasCommand
        {
            UserId = userId,
            MetroAreaIds = request.MetroAreaIds
        };

        var result = await _mediator.Send(command);

        return result.IsSuccess
            ? Ok(new { message = "Preferred metro areas updated successfully" })
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{userId}/preferred-metros")]
    [ProducesResponseType(typeof(List<PreferredMetroAreaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferredMetroAreas(Guid userId)
    {
        var query = new GetUserProfileQuery { UserId = userId };
        var result = await _mediator.Send(query);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return Ok(result.Value.PreferredMetroAreas);
    }
}
```

### 6.2 Request/Response Models

```csharp
public record UpdatePreferredMetroAreasRequest
{
    [Required]
    [MaxLength(10, ErrorMessage = "Cannot select more than 10 metro areas")]
    public List<Guid> MetroAreaIds { get; init; } = new();
}
```

---

## 7. Frontend Integration

### 7.1 TypeScript Domain Models

**File**: `web/src/domain/models/UserProfile.ts`

```typescript
export interface PreferredMetroArea {
  id: string; // UUID
  name: string;
  state: string;
  isStateLevelArea: boolean;
}

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;

  // Existing properties
  location?: Location | null;
  culturalInterests?: string[];
  languages?: Language[];

  // NEW: Preferred metro areas
  preferredMetroAreas?: PreferredMetroArea[];
}

export interface UpdatePreferredMetroAreasRequest {
  metroAreaIds: string[]; // Array of UUIDs (0-10 items)
}
```

### 7.2 API Client

**File**: `web/src/infrastructure/api/usersApi.ts`

```typescript
export async function updatePreferredMetroAreas(
  userId: string,
  metroAreaIds: string[]
): Promise<void> {
  await apiClient.put(`/users/${userId}/preferred-metros`, {
    metroAreaIds
  });
}

export async function getPreferredMetroAreas(
  userId: string
): Promise<PreferredMetroArea[]> {
  const response = await apiClient.get<PreferredMetroArea[]>(
    `/users/${userId}/preferred-metros`
  );
  return response.data;
}
```

### 7.3 React Components

**File**: `web/src/presentation/components/features/profile/PreferredMetroAreasSection.tsx`

```typescript
export function PreferredMetroAreasSection({ userId }: { userId: string }) {
  const { data: profile } = useUserProfile(userId);
  const { data: metroAreas } = useMetroAreas();
  const { mutate: updatePreferredMetros } = useUpdatePreferredMetroAreas();

  const [selectedMetroIds, setSelectedMetroIds] = useState<string[]>(
    profile?.preferredMetroAreas?.map(m => m.id) ?? []
  );

  const handleSave = () => {
    updatePreferredMetros({ userId, metroAreaIds: selectedMetroIds });
  };

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">My Preferred Metro Areas</h3>
      <p className="text-sm text-gray-600">
        Select up to 10 metro areas to personalize your community feed (optional)
      </p>

      <MetroAreaCheckboxList
        metroAreas={metroAreas ?? []}
        selectedIds={selectedMetroIds}
        onChange={setSelectedMetroIds}
        maxSelections={10}
      />

      <Button onClick={handleSave}>
        Save Preferences
      </Button>
    </div>
  );
}
```

### 7.4 Dashboard Feed Integration

**File**: `web/src/app/(dashboard)/dashboard/page.tsx`

```typescript
export default function DashboardPage() {
  const { data: profile } = useUserProfile();
  const [selectedMetroFilter, setSelectedMetroFilter] = useState<string[]>([]);

  useEffect(() => {
    // Default to user's preferred metros if set
    if (profile?.preferredMetroAreas?.length) {
      setSelectedMetroFilter(profile.preferredMetroAreas.map(m => m.id));
    }
  }, [profile]);

  return (
    <div>
      <h1>Community Activity Feed</h1>

      <div className="mb-4">
        <Select
          label="Filter by Metro Areas"
          multiple
          value={selectedMetroFilter}
          onChange={setSelectedMetroFilter}
          options={[
            { value: 'preferred', label: 'My Preferred Metros' },
            ...allMetroAreas.map(m => ({ value: m.id, label: m.name }))
          ]}
        />
      </div>

      <EventFeed metroAreaIds={selectedMetroFilter} />
    </div>
  );
}
```

### 7.5 Registration Flow

**File**: `web/src/app/(auth)/register/page.tsx`

```typescript
export default function RegisterPage() {
  const [step, setStep] = useState(1);
  const [preferredMetros, setPreferredMetros] = useState<string[]>([]);

  const handleRegistration = async (formData) => {
    // Step 1: Create user account
    const user = await register(formData);

    // Step 2: Optional - Set preferred metros
    if (preferredMetros.length > 0) {
      await updatePreferredMetroAreas(user.id, preferredMetros);
    }

    router.push('/dashboard');
  };

  return (
    <div>
      {step === 1 && <BasicInfoForm onNext={() => setStep(2)} />}
      {step === 2 && (
        <div>
          <h2>Select Your Preferred Metro Areas (Optional)</h2>
          <p>Help us personalize your experience</p>

          <MetroAreaSelector
            selectedIds={preferredMetros}
            onChange={setPreferredMetros}
          />

          <Button onClick={() => setStep(3)}>Skip</Button>
          <Button onClick={() => setStep(3)}>Continue</Button>
        </div>
      )}
    </div>
  );
}
```

---

## 8. Business Rules & Constraints

### 8.1 Validation Rules

| Rule | Constraint | Enforcement Layer |
|------|-----------|-------------------|
| Max metros per user | 10 | Domain (User.UpdatePreferredMetroAreas) |
| Min metros per user | 0 (optional) | Domain |
| Valid metro area IDs | Must exist in events.metro_areas | Application/Database FK |
| No duplicates | Automatically handled | Domain (Distinct()) |
| Authorization | Users can only update their own | API (Controller) |

### 8.2 Privacy Considerations

1. **Optional Feature**: Users are NOT required to set preferred metros
2. **Explicit Consent**: Only set when user explicitly chooses metros
3. **Data Minimization**: Only store metro area IDs (not GPS coordinates)
4. **User Control**: Can clear all preferences at any time
5. **No Public Visibility**: Preferred metros are private to user (not shown to others)

### 8.3 Update Frequency Policy

**Question**: Should we limit how often users can change preferred metros?

**Answer**: **NO - Allow unrestricted updates**

**Rationale**:
- Users move frequently
- Life circumstances change (new job, family events)
- No performance cost (indexed queries)
- Simpler UX (no "you changed too recently" errors)

---

## 9. Performance Considerations

### 9.1 Query Optimization

**Scenario**: Dashboard feed query with preferred metro filter

```sql
-- Optimized query using indexed junction table
SELECT e.*
FROM events.events e
WHERE e.metro_area_id IN (
    SELECT metro_area_id
    FROM users.user_preferred_metro_areas
    WHERE user_id = :userId
)
AND e.event_date >= NOW()
ORDER BY e.event_date ASC
LIMIT 50;
```

**Indexes Required**:
- `users.user_preferred_metro_areas(user_id)` - Already created
- `events.events(metro_area_id, event_date)` - Composite index for feed queries

### 9.2 Caching Strategy

**Frontend Caching**:
```typescript
// Cache user profile (including preferred metros) for 5 minutes
export function useUserProfile(userId: string) {
  return useQuery({
    queryKey: ['user-profile', userId],
    queryFn: () => getUserProfile(userId),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}
```

**Backend Caching**:
- No need for Redis/distributed cache (queries are fast)
- Rely on database query cache and indexes
- Consider caching metro area names (rarely change)

### 9.3 Estimated Performance

| Operation | Estimated Latency | Optimization |
|-----------|------------------|--------------|
| Get user preferred metros | 10-20ms | Indexed lookup |
| Update preferred metros | 50-100ms | Bulk insert/delete |
| Feed query with metro filter | 100-200ms | Composite index on events |
| Registration with metros | 150-250ms | Part of transaction |

---

## 10. Testing Strategy

### 10.1 Unit Tests (Domain Layer)

**File**: `tests/LankaConnect.Domain.Tests/Users/UserTests.cs`

```csharp
public class PreferredMetroAreasTests
{
    [Fact]
    public void UpdatePreferredMetroAreas_WithValidMetros_ShouldSucceed()
    {
        // Arrange
        var user = CreateUser();
        var metroIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        // Act
        var result = user.UpdatePreferredMetroAreas(metroIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PreferredMetroAreaIds.Should().HaveCount(2);
    }

    [Fact]
    public void UpdatePreferredMetroAreas_WithMoreThan10Metros_ShouldFail()
    {
        // Arrange
        var user = CreateUser();
        var metroIds = Enumerable.Range(1, 11).Select(_ => Guid.NewGuid()).ToList();

        // Act
        var result = user.UpdatePreferredMetroAreas(metroIds);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("more than 10");
    }

    [Fact]
    public void UpdatePreferredMetroAreas_WithDuplicates_ShouldRemoveDuplicates()
    {
        // Arrange
        var user = CreateUser();
        var metroId = Guid.NewGuid();
        var metroIds = new List<Guid> { metroId, metroId, metroId };

        // Act
        var result = user.UpdatePreferredMetroAreas(metroIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PreferredMetroAreaIds.Should().HaveCount(1);
    }
}
```

### 10.2 Integration Tests (Application Layer)

```csharp
public class UpdatePreferredMetroAreasCommandTests : IntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidMetroIds_ShouldUpdateUser()
    {
        // Arrange
        var user = await CreateUserAsync();
        var metros = await CreateMetroAreasAsync(3);

        var command = new UpdatePreferredMetroAreasCommand
        {
            UserId = user.Id,
            MetroAreaIds = metros.Select(m => m.Id).ToList()
        };

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify database state
        var updatedUser = await _userRepository.GetByIdAsync(user.Id);
        updatedUser.PreferredMetroAreaIds.Should().HaveCount(3);
    }
}
```

### 10.3 Frontend Tests

```typescript
describe('PreferredMetroAreasSection', () => {
  it('should render user preferred metros', () => {
    const profile = createMockProfile({
      preferredMetroAreas: [
        { id: '1', name: 'Cleveland', state: 'OH' },
        { id: '2', name: 'Columbus', state: 'OH' }
      ]
    });

    render(<PreferredMetroAreasSection profile={profile} />);

    expect(screen.getByText('Cleveland')).toBeInTheDocument();
    expect(screen.getByText('Columbus')).toBeInTheDocument();
  });

  it('should enforce 10 metro limit', () => {
    // Test max selection enforcement
  });
});
```

---

## 11. Migration & Deployment Plan

### 11.1 Phase 1: Backend Foundation (Week 1)

**Tasks**:
1. Create database migration for junction table
2. Update User domain model with UpdatePreferredMetroAreas method
3. Add domain event: PreferredMetroAreasUpdatedEvent
4. Create CQRS command/handler: UpdatePreferredMetroAreasCommand
5. Update UserRepository with preferred metros queries
6. Unit tests (90% coverage)

**Deliverables**:
- Migration file ready to apply
- Domain logic tested and working
- API endpoints functional

### 11.2 Phase 2: API & Frontend Integration (Week 2)

**Tasks**:
1. Add API endpoints to UsersController
2. Update UserProfileDto with PreferredMetroAreas field
3. Create TypeScript interfaces for frontend
4. Build PreferredMetroAreasSection component
5. Integration tests for API endpoints

**Deliverables**:
- Profile page shows preferred metros
- Users can update preferences

### 11.3 Phase 3: Dashboard Feed Integration (Week 3)

**Tasks**:
1. Update dashboard feed query to use preferred metros
2. Add "My Preferred Metros" filter option
3. Default feed to show preferred metros if set
4. Fallback to all metros if none selected
5. Performance testing and optimization

**Deliverables**:
- Dashboard feed respects user preferences
- Query performance < 200ms

### 11.4 Phase 4: Registration Flow (Week 4)

**Tasks**:
1. Add optional metro selection step in registration
2. Update registration flow UX
3. Skip/Continue options for flexibility
4. E2E tests for full registration flow

**Deliverables**:
- New users can set preferred metros during signup
- Optional step (can skip)

---

## 12. Monitoring & Analytics

### 12.1 Metrics to Track

| Metric | Purpose | Query |
|--------|---------|-------|
| % of users with preferred metros | Feature adoption | `SELECT COUNT(*) / total_users WHERE has_preferences` |
| Avg number of preferred metros | User behavior | `SELECT AVG(metro_count) FROM user_metro_counts` |
| Most popular metro areas | Product insights | `SELECT metro_area_id, COUNT(*) FROM junction GROUP BY` |
| Preference update frequency | User engagement | Track domain events |

### 12.2 Analytics Queries

```sql
-- Feature adoption rate
SELECT
    COUNT(DISTINCT user_id) * 100.0 / (SELECT COUNT(*) FROM users.users) as adoption_percentage
FROM users.user_preferred_metro_areas;

-- Metro area popularity
SELECT
    m.name,
    m.state,
    COUNT(upma.user_id) as user_count
FROM events.metro_areas m
LEFT JOIN users.user_preferred_metro_areas upma ON m.id = upma.metro_area_id
GROUP BY m.id, m.name, m.state
ORDER BY user_count DESC;
```

---

## 13. Security Considerations

### 13.1 Authorization

**Rule**: Users can only update their own preferred metros (except admins)

**Implementation**:
```csharp
[Authorize]
[HttpPut("{userId}/preferred-metros")]
public async Task<IActionResult> UpdatePreferredMetroAreas(Guid userId, ...)
{
    var currentUserId = User.GetUserId();
    if (userId != currentUserId && !User.IsInRole("Admin"))
        return Forbid();

    // ... proceed with update
}
```

### 13.2 Data Privacy

1. **No Public Exposure**: Preferred metros are private to user
2. **Not Shared with Third Parties**: Internal use only
3. **GDPR Compliance**: User can clear preferences (right to erasure)
4. **Cascade Delete**: If user deleted, preferences auto-deleted (ON DELETE CASCADE)

---

## 14. Answers to Architect Questions

### Q1: Should we create a separate junction table or add to User entity?

**Answer**: **Separate junction table** (`users.user_preferred_metro_areas`)

**Rationale**:
- Clean many-to-many relationship modeling
- Efficient queries with composite indexes
- Follows PostgreSQL best practices
- Easy to audit changes (created_at timestamp)
- Doesn't bloat users table with arrays

### Q2: Should this be part of User aggregate root or separate entity?

**Answer**: **Part of User aggregate root**

**Rationale**:
- Metro preferences are intrinsic to user profile
- Business rules (max 10) enforced by User entity
- Lifecycle tied to User (cascade delete)
- Transaction boundary aligns with user updates
- Simpler domain model (no distributed transactions)

### Q3: How does this relate to newsletter subscription metro areas?

**Answer**: **Keep them SEPARATE (different business purposes)**

**Comparison**:
```
User Preferred Metros:
- Purpose: Feed filtering, recommendations
- Business Context: "Where I have ties/interests"
- Use Case: Dashboard community activity feed

Newsletter Subscription Metros:
- Purpose: Email notifications
- Business Context: "Where I want event alerts"
- Use Case: Weekly newsletter digest
```

**User Flexibility**: User can prefer Cleveland but subscribe to NYC newsletters

### Q4: Should registration require metro area selection?

**Answer**: **OPTIONAL during registration**

**Rationale**:
- Privacy-first approach
- Reduces signup friction
- Users can set later in profile
- Progressive disclosure (ask when context makes sense)
- Not critical for account creation

### Q5: How should we handle users who move?

**Answer**: **Allow unrestricted updates**

**Rationale**:
- Life circumstances change frequently
- No performance cost (indexed queries)
- Simpler UX (no artificial constraints)
- Reflects real-world flexibility
- Users may have ties to multiple regions simultaneously

---

## 15. Decision Records

### ADR-001: Separate Junction Table vs Array Column

**Decision**: Use junction table instead of PostgreSQL array column

**Context**:
- PostgreSQL supports UUID[] array types
- Could store preferred_metro_area_ids as array on users table

**Reasons**:
1. **Referential Integrity**: Foreign key constraints enforced at database level
2. **Query Performance**: Indexed lookups faster than array operations
3. **Audit Trail**: Junction table can track when each metro was added
4. **Normalization**: Follows Third Normal Form (3NF)
5. **EF Core Support**: Better ORM integration with junction tables

**Status**: Accepted

---

### ADR-002: Collection in User Aggregate vs Separate Aggregate

**Decision**: Model as collection within User aggregate root

**Context**:
- DDD allows collections within aggregates
- Could create separate PreferredMetroArea aggregate

**Reasons**:
1. **Cohesion**: Metro preferences are user attributes, not independent entities
2. **Consistency Boundary**: User enforces max 10 rule
3. **Transaction Boundary**: Updates should be atomic with User
4. **Lifecycle**: Preferences have no meaning without User
5. **Simplicity**: Avoids distributed transactions

**Status**: Accepted

---

### ADR-003: Separate from Newsletter Subscriptions

**Decision**: Keep user preferred metros separate from newsletter subscription metros

**Context**:
- Newsletter feature also uses metro areas
- Could consolidate into single preference

**Reasons**:
1. **Different Business Purposes**: Feed filtering vs email notifications
2. **User Flexibility**: Allow different preferences for different contexts
3. **Single Responsibility**: Each feature has focused purpose
4. **Future Extensibility**: Can evolve independently
5. **User Control**: Explicit choices for each use case

**Status**: Accepted

---

## 16. Future Enhancements

### 16.1 Post-MVP Features

1. **Smart Suggestions**: ML-based metro recommendations based on user behavior
2. **Temporary Preferences**: "I'm visiting NYC this month" (time-bound metros)
3. **Preference Sharing**: "Follow my friend's metro preferences"
4. **Weighted Preferences**: Primary vs secondary metros (ranking)
5. **Auto-Update from Location**: Suggest adding metros when user travels

### 16.2 Analytics Enhancements

1. **Preference Drift Analysis**: How do preferences change over time?
2. **Correlation with Engagement**: Do users with preferences engage more?
3. **Metro Clustering**: Which metros are commonly selected together?
4. **Onboarding Impact**: Conversion rate difference when metros set during registration

---

## 17. Appendix

### 17.1 Related Documents

- [Newsletter System Architecture](./Newsletter-System-Architecture.md)
- [User Domain Model Documentation](../src/LankaConnect.Domain/Users/README.md)
- [Clean Architecture Guidelines](./Clean-Architecture-Guidelines.md)

### 17.2 Database Schema ERD

```
┌─────────────────────┐
│   users.users       │
├─────────────────────┤
│ id (PK)            │
│ email              │──┐
│ first_name         │  │
│ last_name          │  │
│ location (VO)      │  │
└─────────────────────┘  │
                         │
                         │ 1
                         │
                         │ N
       ┌─────────────────────────────────────┐
       │ user_preferred_metro_areas          │
       ├─────────────────────────────────────┤
       │ user_id (PK, FK)                   │
       │ metro_area_id (PK, FK)             │
       │ created_at                          │
       └─────────────────────────────────────┘
                         │
                         │ N
                         │
                         │ 1
                         │
                ┌────────┴─────────────┐
                │ events.metro_areas   │
                ├──────────────────────┤
                │ id (PK)             │
                │ name                │
                │ state               │
                │ center_latitude     │
                │ center_longitude    │
                │ radius_miles        │
                │ is_state_level_area │
                └──────────────────────┘
```

---

**Document Status**: Ready for Implementation
**Approval Required**: Product Owner, Tech Lead
**Target Start Date**: [To be determined]
**Estimated Completion**: 4 weeks

