# Preferred Metro Areas - Unified Architecture Design

**Date**: 2025-11-09
**Status**: 🎯 ARCHITECTURE APPROVED
**Epic**: Phase 5 - Metro Areas System (User Preferences + Newsletter)

---

## Executive Summary

This document defines a **unified architecture** for "Preferred Metro Areas" that works seamlessly across three features:

1. **User Registration**: Select preferred metro areas during signup (optional)
2. **Dashboard Community Activity Feed**: Filter by "My Preferred Metro Areas" instead of generic "All Locations"
3. **Newsletter Subscription**: Multi-select metro areas for email notifications

**Key Decision**: Use **TWO separate junction tables** to maintain separation of concerns:
- `identity.user_preferred_metro_areas` - User's general location preferences (part of User aggregate)
- `communications.newsletter_subscriber_metro_areas` - Newsletter-specific metro subscriptions (part of Newsletter aggregate)

---

## Architecture Principles

### 1. Domain-Driven Design (DDD)
- **User Aggregate Root**: Owns `user_preferred_metro_areas` (general preferences)
- **NewsletterSubscriber Aggregate Root**: Owns `newsletter_subscriber_metro_areas` (newsletter-specific)
- **Aggregate Independence**: Newsletter preferences can differ from user preferences
- **Bounded Contexts**: Identity context vs Communications context

### 2. Clean Architecture
- **Domain Layer**: User and NewsletterSubscriber aggregates with business logic
- **Application Layer**: CQRS commands/queries for metro area management
- **Infrastructure Layer**: EF Core repositories and configurations
- **Presentation Layer**: Reusable React components for metro selection

### 3. UI/UX Best Practices
- **Consistency**: Same metro selection component used across all features
- **Flexibility**: Users can have different metros for profile vs newsletter
- **Privacy**: Metro area selection is **optional** everywhere
- **Progressive Disclosure**: Smart defaults, collapsible groups by state

---

## Database Schema Design

### Table 1: `events.metro_areas` (Already Exists ✅)
**Purpose**: Master list of all metro areas

```sql
-- Already exists in database
CREATE TABLE events.metro_areas (
    id uuid PRIMARY KEY,
    name varchar(100) NOT NULL,
    state varchar(2) NOT NULL,
    center_latitude decimal(10,8) NOT NULL,
    center_longitude decimal(11,8) NOT NULL,
    radius_miles integer NOT NULL,
    is_state_level_area boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    updated_at timestamp with time zone
);

CREATE INDEX idx_metro_areas_state ON events.metro_areas(state);
CREATE INDEX idx_metro_areas_is_active ON events.metro_areas(is_active);
```

### Table 2: `identity.user_preferred_metro_areas` (New - Phase 5A)
**Purpose**: User's general metro area preferences (for profile, dashboard filtering, etc.)

```sql
CREATE TABLE identity.user_preferred_metro_areas (
    user_id uuid NOT NULL,
    metro_area_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),

    PRIMARY KEY (user_id, metro_area_id),

    CONSTRAINT fk_user_preferred_metro_areas_user
        FOREIGN KEY (user_id)
        REFERENCES identity.users(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_preferred_metro_areas_metro_area
        FOREIGN KEY (metro_area_id)
        REFERENCES events.metro_areas(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_user_preferred_metro_areas_user_id
    ON identity.user_preferred_metro_areas(user_id);

CREATE INDEX idx_user_preferred_metro_areas_metro_area_id
    ON identity.user_preferred_metro_areas(metro_area_id);
```

**Business Rules**:
- Optional: Users can have 0 preferred metros (privacy choice)
- Max 10 preferred metros per user (prevent abuse)
- No duplicates (enforced by composite PK)

### Table 3: `communications.newsletter_subscriber_metro_areas` (New - Phase 5B)
**Purpose**: Newsletter-specific metro subscriptions (can differ from user preferences)

```sql
CREATE TABLE communications.newsletter_subscriber_metro_areas (
    subscriber_id uuid NOT NULL,
    metro_area_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),

    PRIMARY KEY (subscriber_id, metro_area_id),

    CONSTRAINT fk_newsletter_subscriber_metro_areas_subscriber
        FOREIGN KEY (subscriber_id)
        REFERENCES communications.newsletter_subscribers(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_newsletter_subscriber_metro_areas_metro_area
        FOREIGN KEY (metro_area_id)
        REFERENCES events.metro_areas(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_newsletter_subscriber_metro_areas_subscriber_id
    ON communications.newsletter_subscriber_metro_areas(subscriber_id);

CREATE INDEX idx_newsletter_subscriber_metro_areas_metro_area_id
    ON communications.newsletter_subscriber_metro_areas(metro_area_id);
```

**Business Rules**:
- Optional if `receive_all_locations = true`
- If `receive_all_locations = false`, must have at least 1 metro area
- Max 10 metros per subscriber
- Newsletter metros can differ from user preferred metros

---

## Why Two Separate Tables?

### Option A: Single Shared Table ❌ (Rejected)
```sql
-- REJECTED APPROACH
CREATE TABLE shared.preferred_metro_areas (
    entity_id uuid,
    entity_type varchar(20), -- 'user' or 'newsletter'
    metro_area_id uuid
);
```

**Problems**:
- ❌ Violates DDD aggregate boundaries
- ❌ Couples User and Newsletter domains
- ❌ Cannot enforce different business rules
- ❌ Harder to query and maintain
- ❌ Newsletter subscribers without user accounts would be problematic

### Option B: Two Junction Tables ✅ (Recommended)
```sql
-- User aggregate owns its preferences
identity.user_preferred_metro_areas

-- Newsletter aggregate owns its subscriptions
communications.newsletter_subscriber_metro_areas
```

**Benefits**:
- ✅ Maintains aggregate independence
- ✅ Separate business rules per context
- ✅ Newsletter can work without user account
- ✅ User preferences separate from newsletter
- ✅ Clear ownership and consistency boundaries
- ✅ Easier to query and optimize

**Real-World Scenarios**:
1. **User without newsletter**: Has preferred metros but no newsletter subscription
2. **Newsletter without user**: Anonymous subscriber (email only) with metro preferences
3. **Different preferences**: User prefers Cleveland for profile but wants newsletter for all Ohio metros
4. **Privacy**: User sets preferred metros but doesn't want newsletter at all

---

## Domain Model Design

### User Aggregate (Updated)

**File**: `src/LankaConnect.Domain/Users/User.cs`

```csharp
public class User : BaseEntity
{
    // Existing properties...
    public Email Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public UserLocation? Location { get; private set; }

    // NEW: Preferred Metro Areas collection
    private readonly List<Guid> _preferredMetroAreaIds = new();
    public IReadOnlyList<Guid> PreferredMetroAreaIds => _preferredMetroAreaIds.AsReadOnly();

    // Existing cultural preferences...
    private readonly List<CulturalInterest> _culturalInterests = new();
    public IReadOnlyCollection<CulturalInterest> CulturalInterests => _culturalInterests.AsReadOnly();

    /// <summary>
    /// Update user's preferred metro areas (0-10 metros allowed)
    /// </summary>
    public Result UpdatePreferredMetroAreas(IEnumerable<Guid> metroAreaIds)
    {
        var metroList = metroAreaIds?.ToList() ?? new List<Guid>();

        // Validation: Max 10 metros
        if (metroList.Count > 10)
            return Result.Failure("Cannot select more than 10 preferred metro areas");

        // Validation: No duplicates
        if (metroList.Distinct().Count() != metroList.Count)
            return Result.Failure("Duplicate metro areas are not allowed");

        _preferredMetroAreaIds.Clear();
        _preferredMetroAreaIds.AddRange(metroList);

        // Update timestamp
        UpdatedAt = DateTime.UtcNow;

        // Raise domain event
        RaiseDomainEvent(new UserPreferredMetroAreasUpdatedEvent(Id, metroList));

        return Result.Success();
    }

    /// <summary>
    /// Factory method: Create user with optional preferred metro areas
    /// </summary>
    public static Result<User> Create(
        Email email,
        string firstName,
        string lastName,
        IEnumerable<Guid>? preferredMetroAreaIds = null,
        UserRole role = UserRole.User)
    {
        // Existing validation...
        var user = new User(email, firstName, lastName, role);

        // Set preferred metros if provided (during registration)
        if (preferredMetroAreaIds != null && preferredMetroAreaIds.Any())
        {
            var result = user.UpdatePreferredMetroAreas(preferredMetroAreaIds);
            if (!result.IsSuccess)
                return Result<User>.Failure(result.Error);
        }

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email.Value, user.FullName));
        return Result<User>.Success(user);
    }
}
```

### NewsletterSubscriber Aggregate (Updated)

**File**: `src/LankaConnect.Domain/Communications/Entities/NewsletterSubscriber.cs`

```csharp
public class NewsletterSubscriber : AggregateRoot
{
    // Existing properties...
    public Email Email { get; private set; }
    public bool ReceiveAllLocations { get; private set; }
    public bool IsActive { get; private set; }

    // NEW: Newsletter-specific metro areas
    private readonly List<Guid> _metroAreaIds = new();
    public IReadOnlyList<Guid> MetroAreaIds => _metroAreaIds.AsReadOnly();

    // DEPRECATED: Keep for backward compatibility during migration
    public Guid? MetroAreaId { get; private set; }

    /// <summary>
    /// Create newsletter subscriber with multiple metro areas
    /// </summary>
    public static Result<NewsletterSubscriber> Create(
        Email email,
        IEnumerable<Guid> metroAreaIds,
        bool receiveAllLocations)
    {
        var metroList = metroAreaIds?.ToList() ?? new List<Guid>();

        // Business rule: If not receiving all locations, must select at least 1 metro
        if (!receiveAllLocations && metroList.Count == 0)
        {
            return Result<NewsletterSubscriber>.Failure(
                "Must select at least one metro area or choose 'All Locations'");
        }

        // Business rule: If receiving all locations, ignore metro selections
        if (receiveAllLocations && metroList.Count > 0)
        {
            metroList.Clear(); // Clear metros if receiving all
        }

        // Validation: Max 10 metros
        if (metroList.Count > 10)
        {
            return Result<NewsletterSubscriber>.Failure(
                "Cannot select more than 10 metro areas");
        }

        var subscriber = new NewsletterSubscriber(email, receiveAllLocations);
        subscriber._metroAreaIds.AddRange(metroList);

        return Result<NewsletterSubscriber>.Success(subscriber);
    }
}
```

---

## EF Core Configuration

### UserConfiguration (Updated)

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/UserConfiguration.cs`

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");

        // Existing configuration...

        // Configure many-to-many relationship for preferred metro areas
        builder
            .HasMany<MetroArea>() // No navigation property in User
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "user_preferred_metro_areas",
                j => j
                    .HasOne<MetroArea>()
                    .WithMany()
                    .HasForeignKey("metro_area_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("user_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("user_preferred_metro_areas", "identity");
                    j.HasKey("user_id", "metro_area_id");
                    j.Property<DateTime>("created_at")
                        .HasDefaultValueSql("NOW()");
                });
    }
}
```

### NewsletterSubscriberConfiguration (Updated)

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/NewsletterSubscriberConfiguration.cs`

```csharp
public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.ToTable("newsletter_subscribers", "communications");

        // Existing configuration...

        // Configure many-to-many for newsletter metros
        builder
            .HasMany<MetroArea>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "newsletter_subscriber_metro_areas",
                j => j
                    .HasOne<MetroArea>()
                    .WithMany()
                    .HasForeignKey("metro_area_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<NewsletterSubscriber>()
                    .WithMany()
                    .HasForeignKey("subscriber_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("newsletter_subscriber_metro_areas", "communications");
                    j.HasKey("subscriber_id", "metro_area_id");
                    j.Property<DateTime>("created_at")
                        .HasDefaultValueSql("NOW()");
                });
    }
}
```

---

## Application Layer (CQRS)

### Commands

#### 1. UpdateUserPreferredMetroAreasCommand

**File**: `src/LankaConnect.Application/Users/Commands/UpdatePreferredMetroAreas/UpdateUserPreferredMetroAreasCommand.cs`

```csharp
public record UpdateUserPreferredMetroAreasCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public List<Guid> MetroAreaIds { get; init; } = new();
}

public class UpdateUserPreferredMetroAreasCommandHandler
    : IRequestHandler<UpdateUserPreferredMetroAreasCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result> Handle(
        UpdateUserPreferredMetroAreasCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            return Result.Failure("User not found");

        var result = user.UpdatePreferredMetroAreas(request.MetroAreaIds);
        if (!result.IsSuccess)
            return result;

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
```

#### 2. RegisterCommand (Updated)

**File**: `src/LankaConnect.Application/Users/Commands/Register/RegisterCommand.cs`

```csharp
public record RegisterCommand : IRequest<Result<RegisterResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;

    // NEW: Optional preferred metro areas during registration
    public List<Guid> PreferredMetroAreaIds { get; init; } = new();
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // Existing validation...

        // Create user with preferred metros
        var userResult = User.Create(
            email,
            request.FirstName,
            request.LastName,
            request.PreferredMetroAreaIds, // NEW PARAMETER
            UserRole.User);

        // Rest of registration logic...
    }
}
```

### Queries

#### GetUserPreferredMetroAreasQuery

**File**: `src/LankaConnect.Application/Users/Queries/GetPreferredMetroAreas/GetUserPreferredMetroAreasQuery.cs`

```csharp
public record GetUserPreferredMetroAreasQuery : IRequest<Result<List<MetroAreaDto>>>
{
    public Guid UserId { get; init; }
}

public class GetUserPreferredMetroAreasQueryHandler
    : IRequestHandler<GetUserPreferredMetroAreasQuery, Result<List<MetroAreaDto>>>
{
    private readonly IUserRepository _userRepository;

    public async Task<Result<List<MetroAreaDto>>> Handle(
        GetUserPreferredMetroAreasQuery query,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdWithPreferredMetrosAsync(
            query.UserId,
            cancellationToken);

        if (user == null)
            return Result<List<MetroAreaDto>>.Failure("User not found");

        // Map to DTOs (assumes repository loads metro area details)
        var metroDtos = user.PreferredMetroAreaIds
            .Select(id => /* map to DTO */)
            .ToList();

        return Result<List<MetroAreaDto>>.Success(metroDtos);
    }
}
```

---

## API Endpoints

### 1. Metro Areas Controller (New)

**File**: `src/LankaConnect.API/Controllers/MetroAreasController.cs`

```csharp
[ApiController]
[Route("api/metro-areas")]
public class MetroAreasController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Get all active metro areas
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 900)] // 15 minutes
    [ProducesResponseType(typeof(List<MetroAreaDto>), 200)]
    public async Task<IActionResult> GetMetroAreas()
    {
        var query = new GetMetroAreasQuery();
        var result = await _mediator.Send(query);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
```

### 2. Users Controller (Updated)

**File**: `src/LankaConnect.API/Controllers/UsersController.cs`

```csharp
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Get current user's preferred metro areas
    /// </summary>
    [HttpGet("me/preferred-metro-areas")]
    [ProducesResponseType(typeof(List<MetroAreaDto>), 200)]
    public async Task<IActionResult> GetMyPreferredMetroAreas()
    {
        var userId = User.GetUserId(); // Extension method to get user ID from JWT
        var query = new GetUserPreferredMetroAreasQuery { UserId = userId };
        var result = await _mediator.Send(query);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>
    /// Update current user's preferred metro areas
    /// </summary>
    [HttpPut("me/preferred-metro-areas")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UpdateMyPreferredMetroAreas(
        [FromBody] UpdatePreferredMetroAreasRequest request)
    {
        var userId = User.GetUserId();
        var command = new UpdateUserPreferredMetroAreasCommand
        {
            UserId = userId,
            MetroAreaIds = request.MetroAreaIds
        };

        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}

public record UpdatePreferredMetroAreasRequest
{
    public List<Guid> MetroAreaIds { get; init; } = new();
}
```

### 3. Auth Controller (Updated)

**File**: `src/LankaConnect.API/Controllers/AuthController.cs`

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Register new user with optional preferred metro areas
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), 200)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PreferredMetroAreaIds = request.PreferredMetroAreaIds // NEW
        };

        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}

public record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public List<Guid> PreferredMetroAreaIds { get; init; } = new(); // NEW - Optional
}
```

---

## Frontend Architecture

### 1. Reusable Component: MetroAreaCheckboxList

**File**: `web/src/presentation/components/features/metro-areas/MetroAreaCheckboxList.tsx`

**Purpose**: Single reusable component for metro selection used in:
- Registration form
- Dashboard filter
- Newsletter subscription

```typescript
export interface MetroAreaCheckboxListProps {
  metroAreas: MetroAreaDto[];
  selectedIds: string[];
  onChange: (ids: string[]) => void;
  isLoading?: boolean;
  showAllLocationsOption?: boolean; // For newsletter only
  allLocationsChecked?: boolean;
  onAllLocationsChange?: (checked: boolean) => void;
  maxSelections?: number; // Default 10
  groupByState?: boolean; // Default true
  collapsible?: boolean; // Collapse state groups
}

export function MetroAreaCheckboxList({
  metroAreas,
  selectedIds,
  onChange,
  isLoading = false,
  showAllLocationsOption = false,
  allLocationsChecked = false,
  onAllLocationsChange,
  maxSelections = 10,
  groupByState = true,
  collapsible = true
}: MetroAreaCheckboxListProps) {
  // Component implementation...
}
```

### 2. Updated RegisterForm

**File**: `web/src/presentation/components/features/auth/RegisterForm.tsx`

```typescript
export function RegisterForm() {
  const [selectedMetros, setSelectedMetros] = useState<string[]>([]);
  const { data: metroAreas, isLoading: metrosLoading } = useMetroAreas();

  const onSubmit = async (data: RegisterFormData) => {
    await authRepository.register({
      email: data.email,
      password: data.password,
      firstName: data.firstName,
      lastName: data.lastName,
      preferredMetroAreaIds: selectedMetros // NEW
    });
  };

  return (
    <Card>
      <form onSubmit={handleSubmit(onSubmit)}>
        {/* Existing fields: firstName, lastName, email, password */}

        {/* NEW: Metro Area Selection (Optional) */}
        <div className="space-y-2">
          <label className="text-sm font-medium">
            Preferred Metro Areas (Optional)
          </label>
          <p className="text-xs text-gray-600">
            Select areas you're interested in to personalize your feed
          </p>
          <MetroAreaCheckboxList
            metroAreas={metroAreas ?? []}
            selectedIds={selectedMetros}
            onChange={setSelectedMetros}
            isLoading={metrosLoading}
            maxSelections={10}
            collapsible={true}
          />
        </div>

        {/* Rest of form... */}
      </form>
    </Card>
  );
}
```

### 3. Updated Dashboard

**File**: `web/src/app/(dashboard)/dashboard/page.tsx` (lines 354-369)

```typescript
export default function DashboardPage() {
  const { user } = useAuthStore();
  const { data: preferredMetros } = useUserPreferredMetroAreas();
  const [selectedMetroFilter, setSelectedMetroFilter] = useState<string>('all');

  // Filter options: "All" or user's preferred metros
  const filterOptions = [
    { value: 'all', label: 'All Locations' },
    ...(preferredMetros?.map(m => ({ value: m.id, label: m.name })) ?? [])
  ];

  return (
    <div className="feed-header">
      <h2>Community Activity</h2>

      {/* UPDATED: "My Preferred Metro Areas" instead of generic dropdown */}
      <select
        value={selectedMetroFilter}
        onChange={(e) => setSelectedMetroFilter(e.target.value)}
      >
        <option value="all">All Locations</option>

        {/* Show user's preferred metros if they have any */}
        {preferredMetros && preferredMetros.length > 0 && (
          <optgroup label="My Preferred Metro Areas">
            {preferredMetros.map(metro => (
              <option key={metro.id} value={metro.id}>
                {metro.name}, {metro.state}
              </option>
            ))}
          </optgroup>
        )}
      </select>
    </div>
  );
}
```

### 4. Newsletter Subscription Form

**File**: `web/src/presentation/components/features/newsletter/NewsletterSubscriptionForm.tsx`

```typescript
export function NewsletterSubscriptionForm() {
  const [email, setEmail] = useState('');
  const [selectedMetros, setSelectedMetros] = useState<string[]>([]);
  const [receiveAll, setReceiveAll] = useState(false);
  const { data: metroAreas } = useMetroAreas();

  // Pre-populate with user's preferred metros if authenticated
  const { user } = useAuthStore();
  const { data: userPreferredMetros } = useUserPreferredMetroAreas(user?.id);

  useEffect(() => {
    // Smart default: Use user's preferred metros if available
    if (userPreferredMetros && userPreferredMetros.length > 0) {
      setSelectedMetros(userPreferredMetros.map(m => m.id));
    }
  }, [userPreferredMetros]);

  return (
    <form>
      <Input type="email" value={email} onChange={setEmail} />

      <MetroAreaCheckboxList
        metroAreas={metroAreas ?? []}
        selectedIds={selectedMetros}
        onChange={setSelectedMetros}
        showAllLocationsOption={true}
        allLocationsChecked={receiveAll}
        onAllLocationsChange={setReceiveAll}
      />

      <Button type="submit">Subscribe</Button>
    </form>
  );
}
```

---

## User Stories & Use Cases

### Story 1: New User Registration with Metro Preferences
**As a** new user
**I want to** select my preferred metro areas during registration
**So that** my dashboard feed is personalized from day one

**Acceptance Criteria**:
- Metro area selection is **optional** during registration
- Can select 0-10 metro areas
- Grouped by state, collapsible
- Shows all 28 available metros from database
- Registration succeeds even if no metros selected

### Story 2: Dashboard Activity Feed Filtering
**As a** logged-in user
**I want to** filter community activity by "My Preferred Metro Areas"
**So that** I see relevant posts without manually selecting locations each time

**Acceptance Criteria**:
- Dropdown shows "All Locations" by default
- If user has preferred metros, shows "My Preferred Metro Areas" optgroup
- Selecting a preferred metro filters feed to that location only
- Works seamlessly with existing feed infrastructure

### Story 3: Newsletter Subscription with Smart Defaults
**As a** user subscribing to newsletter
**I want to** see my preferred metros pre-selected
**So that** I don't have to re-enter my location preferences

**Acceptance Criteria**:
- If user is authenticated AND has preferred metros, pre-populate checkboxes
- Can override selections before subscribing
- Can choose "All Locations" instead
- Newsletter metros can differ from user preferred metros (separate tables)

### Story 4: Anonymous Newsletter Subscription
**As an** anonymous visitor
**I want to** subscribe to newsletter with metro selection
**So that** I get location-specific updates without creating an account

**Acceptance Criteria**:
- Works without user authentication
- Metro selection starts empty (no pre-population)
- Creates newsletter subscription with selected metros
- No user account created

---

## Migration Strategy

### Phase 5A: User Preferred Metro Areas (Week 1)

1. **Backend**:
   - Create migration: `AddUserPreferredMetroAreas`
   - Update User aggregate with metro collection
   - Update UserConfiguration for EF Core
   - Create UpdateUserPreferredMetroAreasCommand
   - Create GetUserPreferredMetroAreasQuery
   - Add API endpoints to UsersController
   - Update RegisterCommand to accept metros

2. **Frontend**:
   - Create MetroAreaCheckboxList component
   - Update RegisterForm with metro selection
   - Update UserProfile TypeScript interface
   - Create useUserPreferredMetroAreas hook
   - Update Dashboard to use preferred metros filter

3. **Testing**:
   - Unit tests for User.UpdatePreferredMetroAreas
   - Integration tests for registration with metros
   - E2E test for dashboard filtering

### Phase 5B: Newsletter Metro Areas (Week 2)

1. **Backend**:
   - Create migration: `AddNewsletterSubscriberMetroAreas`
   - Update NewsletterSubscriber aggregate
   - Update NewsletterSubscriberConfiguration
   - Update SubscribeToNewsletterCommand for array
   - Update SubscribeToNewsletterCommandHandler

2. **Frontend**:
   - Create NewsletterSubscriptionForm
   - Reuse MetroAreaCheckboxList component
   - Create newsletter API hooks
   - Integrate into landing page
   - Create confirmation page

3. **Testing**:
   - Update newsletter subscription tests
   - Test metro area selection UI
   - Test pre-population logic
   - End-to-end newsletter flow

### Phase 5C: Metro Areas API (Week 1-2 Parallel)

1. **Backend**:
   - Create GetMetroAreasQuery
   - Create MetroAreasController
   - Seed database with 28 metros (if missing)
   - Add response caching

2. **Frontend**:
   - Create metroAreasApi.ts
   - Create useMetroAreas hook
   - Replace hardcoded constants

---

## Testing Strategy

### Unit Tests

#### Domain Layer
```csharp
// User aggregate tests
[Test]
public void UpdatePreferredMetroAreas_WithValidMetros_ShouldSucceed()
[Test]
public void UpdatePreferredMetroAreas_WithMoreThan10Metros_ShouldFail()
[Test]
public void UpdatePreferredMetroAreas_WithDuplicates_ShouldFail()
[Test]
public void CreateUser_WithPreferredMetros_ShouldSetMetros()

// Newsletter aggregate tests
[Test]
public void CreateSubscriber_WithMetrosAndReceiveAll_ShouldClearMetros()
[Test]
public void CreateSubscriber_WithoutMetrosAndNotReceiveAll_ShouldFail()
```

#### Application Layer
```csharp
[Test]
public async Task UpdateUserPreferredMetroAreas_ShouldUpdateUser()
[Test]
public async Task Register_WithPreferredMetros_ShouldCreateUserWithMetros()
```

### Integration Tests

```csharp
[Test]
public async Task POST_Register_WithMetroAreas_ShouldSaveToDatabase()
[Test]
public async Task PUT_PreferredMetroAreas_ShouldUpdateJunctionTable()
[Test]
public async Task GET_MyPreferredMetroAreas_ShouldReturnUserMetros()
```

### E2E Tests (Frontend)

```typescript
describe('User Registration with Metro Areas', () => {
  it('should allow selecting multiple metro areas during registration');
  it('should validate max 10 metros');
  it('should register successfully without metros (optional)');
});

describe('Dashboard Metro Filtering', () => {
  it('should show preferred metros in dropdown');
  it('should filter feed by selected metro');
  it('should work with "All Locations"');
});

describe('Newsletter Subscription', () => {
  it('should pre-populate metros for authenticated users');
  it('should allow anonymous subscription with metro selection');
  it('should disable metros when "All Locations" checked');
});
```

---

## Success Criteria

✅ **Phase 5 is complete when**:

1. **Backend**:
   - [ ] `user_preferred_metro_areas` table created and migrated
   - [ ] `newsletter_subscriber_metro_areas` table created and migrated
   - [ ] User aggregate supports preferred metros with validation
   - [ ] Newsletter aggregate supports multiple metros
   - [ ] GET /api/metro-areas returns all metros
   - [ ] GET /api/users/me/preferred-metro-areas returns user metros
   - [ ] PUT /api/users/me/preferred-metro-areas updates user metros
   - [ ] POST /api/auth/register accepts preferredMetroAreaIds
   - [ ] POST /api/newsletter/subscribe accepts metroAreaIds array

2. **Frontend**:
   - [ ] MetroAreaCheckboxList component created and reusable
   - [ ] RegisterForm includes metro selection (optional)
   - [ ] Dashboard shows "My Preferred Metro Areas" filter
   - [ ] Newsletter form shows metro checkboxes
   - [ ] Newsletter form pre-populates for authenticated users
   - [ ] All components use database-driven metro data (no hardcoded)

3. **Testing**:
   - [ ] 90% test coverage maintained
   - [ ] All domain unit tests pass
   - [ ] All integration tests pass
   - [ ] E2E tests for registration, dashboard, newsletter

4. **Documentation**:
   - [ ] PROGRESS_TRACKER.md updated
   - [ ] STREAMLINED_ACTION_PLAN.md updated
   - [ ] API documentation updated

---

## Open Questions & Decisions

### Q1: Should registration REQUIRE metro selection?
**Decision**: ❌ NO - Make it **optional**
**Rationale**: Privacy choice, not all users care about location filtering

### Q2: Should newsletter metros match user preferred metros automatically?
**Decision**: ✅ YES - Pre-populate but allow override
**Rationale**: Smart default improves UX, but users might want different preferences

### Q3: Can newsletter subscribers exist without user accounts?
**Decision**: ✅ YES - Support anonymous subscriptions
**Rationale**: Lower barrier for newsletter signups, separate aggregates support this

### Q4: What happens if user has no preferred metros?
**Decision**: Dashboard shows "All Locations" only
**Rationale**: Graceful degradation to existing behavior

### Q5: Should we migrate existing newsletter single metro_area_id?
**Decision**: ✅ YES - Migrate to junction table
**Rationale**: Support multi-select, keep backward compatibility during transition

---

## Rollback Plan

If Phase 5 deployment fails:

1. **Backend Rollback**:
   - Revert migrations: Drop junction tables
   - User/Newsletter aggregates remain functional (new properties ignored)
   - API endpoints return 404 for new routes

2. **Frontend Rollback**:
   - Hide metro selection in RegisterForm
   - Dashboard uses hardcoded "All Locations"
   - Newsletter uses single dropdown (old behavior)

3. **Database Rollback**:
   - `DROP TABLE identity.user_preferred_metro_areas;`
   - `DROP TABLE communications.newsletter_subscriber_metro_areas;`
   - Existing data unaffected

---

**Last Updated**: 2025-11-09
**Status**: Ready for Implementation
**Estimated Effort**: 2-3 weeks (15-20 hours)
