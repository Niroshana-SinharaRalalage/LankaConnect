# Newsletter UI Integration + Preferred Metro Areas - Phase 5 Implementation Plan

**Date**: 2025-11-09
**Status**: 🚀 READY TO IMPLEMENT
**Epic**: Phase 5 - Metro Areas System (User Preferences + Newsletter)

**Related Documents**:
- Architecture Design: `PREFERRED_METRO_AREAS_ARCHITECTURE.md`
- Original Newsletter Plan: This document
- Progress Tracking: `PROGRESS_TRACKER.md`
- Action Plan: `STREAMLINED_ACTION_PLAN.md`

---

## User Requirements (Confirmed)

### ✅ Requirement 1: Integrate Newsletter Subscription APIs with UI
**APIs Available**:
- `POST /api/newsletter/subscribe` - Create subscription (deployed to staging)
- `GET /api/newsletter/confirm?token={token}` - Confirm subscription (ready)

### ✅ Requirement 2: Multi-Location Selection with Checkboxes
**User Request**: "How about having checkboxes for 'Get notifications for events in' dropdown, so that user can select multiple locations."

**Decision**: Implement true multi-select with checkboxes
- Users can select multiple metro areas
- Requires backend schema changes (junction table)
- More flexible and user-friendly

### ✅ Requirement 3: Database-Driven Metro Areas
**User Request**: "Are the data hardcoded... or coming from database table? If not can we create a database table and load them from there"

**Decision**: Use database as single source of truth
- Database table `events.metro_areas` already exists
- Create `GET /api/metro-areas` endpoint
- Frontend fetches from API (no hardcoded data)

### ✅ Requirement 4: "My Preferred Metro Areas" in Dashboard (NEW)
**User Request**: "In the Community Activity dropdown, instead of 'Near By Metro areas' we can use 'My preferred Metro Areas' and list all the metro areas that user has subscribed to."

**Decision**: Add preferred metros to User profile
- Create `user_preferred_metro_areas` junction table
- Dashboard filter shows user's preferred metros
- Separate from newsletter metros (different use cases)

### ✅ Requirement 5: Metro Selection During Registration (NEW)
**User Request**: "Same preferred metro area selection should be provided in the user registration as well. Users who don't have a newsletter subscription but wish to register as a regular user."

**Decision**: Add optional metro selection to registration
- Optional field (0-10 metros)
- Used for personalized dashboard feed
- Reusable component across registration/newsletter/dashboard

---

## Architectural Overview

### Current State (Before Phase 5)
```
Frontend:
- Metro areas hardcoded in web/src/domain/constants/metroAreas.constants.ts
- 28 locations with string IDs ('cleveland-oh', 'nyc-ny')
- Single-select dropdown component

Backend:
- Database table: events.metro_areas (with UUIDs)
- Newsletter table: metro_area_id uuid NULL (single value only)
- API: POST /api/newsletter/subscribe (single metro or all locations)
```

### Target State (After Phase 5)
```
Frontend:
- Fetch metro areas from GET /api/metro-areas
- Multi-select checkbox component
- Newsletter subscription form with multiple location selection
- TypeScript types aligned with backend DTOs

Backend:
- New API: GET /api/metro-areas
- Junction table: communications.newsletter_subscriber_metro_areas
- Updated handler: Support array of metro_area_ids
- Domain model: Support multiple metros per subscriber
```

---

## Implementation Strategy

### Phase 5A: Backend - Metro Areas API 🔧
**Estimated Time**: 2-3 hours

#### Task 5A.1: Create Metro Area Query (CQRS)
**Files to Create**:
```
src/LankaConnect.Application/
├── MetroAreas/
│   └── Queries/
│       └── GetMetroAreas/
│           ├── GetMetroAreasQuery.cs
│           ├── GetMetroAreasQueryHandler.cs
│           ├── MetroAreaDto.cs
│           └── GetMetroAreasQueryHandlerTests.cs (in tests project)
```

**DTO Structure**:
```csharp
public record MetroAreaDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public double CenterLatitude { get; init; }
    public double CenterLongitude { get; init; }
    public int RadiusMiles { get; init; }
    public bool IsStateLevelArea { get; init; }
}
```

#### Task 5A.2: Add API Controller Endpoint
**File to Modify**: `src/LankaConnect.API/Controllers/MetroAreasController.cs` (new)

```csharp
[ApiController]
[Route("api/metro-areas")]
public class MetroAreasController : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 900)] // 15 minutes cache
    public async Task<IActionResult> GetMetroAreas()
    {
        var query = new GetMetroAreasQuery();
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
```

#### Task 5A.3: Verify Database Metro Areas
**Action**: Query staging database to see what metros exist
- If metros are missing, we'll seed them in next step
- Map frontend constants to database UUIDs

---

### Phase 5B: Backend - Multi-Select Support 🔧
**Estimated Time**: 3-4 hours

#### Task 5B.1: Create Junction Table Migration
**File to Create**: `src/LankaConnect.Infrastructure/Data/Migrations/[Timestamp]_AddNewsletterSubscriberMetroAreas.cs`

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

CREATE INDEX idx_newsletter_subscriber_metro_areas_subscriber
    ON communications.newsletter_subscriber_metro_areas(subscriber_id);

CREATE INDEX idx_newsletter_subscriber_metro_areas_metro_area
    ON communications.newsletter_subscriber_metro_areas(metro_area_id);
```

#### Task 5B.2: Update Domain Model
**File to Modify**: `src/LankaConnect.Domain/Communications/Entities/NewsletterSubscriber.cs`

Add collection navigation property:
```csharp
public class NewsletterSubscriber : AggregateRoot
{
    // Existing properties...

    private readonly List<Guid> _metroAreaIds = new();
    public IReadOnlyList<Guid> MetroAreaIds => _metroAreaIds.AsReadOnly();

    // Keep for backward compatibility
    public Guid? MetroAreaId { get; private set; }

    public static Result<NewsletterSubscriber> Create(
        Email email,
        IEnumerable<Guid> metroAreaIds,
        bool receiveAllLocations)
    {
        // Validation logic
        if (!receiveAllLocations && !metroAreaIds.Any())
        {
            return Result<NewsletterSubscriber>.Failure(
                "Must select at least one metro area or choose 'All Locations'");
        }

        // Create instance with multiple metros
    }
}
```

#### Task 5B.3: Update Repository
**File to Modify**: `src/LankaConnect.Infrastructure/Data/Repositories/NewsletterSubscriberRepository.cs`

Add EF Core configuration for junction table:
```csharp
public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        // Existing configuration...

        // Configure many-to-many relationship
        builder.HasMany<MetroArea>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "newsletter_subscriber_metro_areas",
                j => j.HasOne<MetroArea>().WithMany().HasForeignKey("metro_area_id"),
                j => j.HasOne<NewsletterSubscriber>().WithMany().HasForeignKey("subscriber_id"),
                j =>
                {
                    j.ToTable("newsletter_subscriber_metro_areas", "communications");
                    j.HasKey("subscriber_id", "metro_area_id");
                });
    }
}
```

#### Task 5B.4: Update Command Handler
**File to Modify**: `src/LankaConnect.Application/Communications/Commands/SubscribeToNewsletter/SubscribeToNewsletterCommand.cs`

```csharp
public record SubscribeToNewsletterCommand : IRequest<Result<SubscribeToNewsletterResponse>>
{
    public string Email { get; init; } = string.Empty;
    public List<Guid> MetroAreaIds { get; init; } = new(); // Changed from single Guid?
    public bool ReceiveAllLocations { get; init; }
}
```

**File to Modify**: `src/LankaConnect.Application/Communications/Commands/SubscribeToNewsletter/SubscribeToNewsletterCommandHandler.cs`

Update to handle multiple metro areas:
```csharp
var createResult = NewsletterSubscriber.Create(
    email,
    request.MetroAreaIds,
    request.ReceiveAllLocations);
```

---

### Phase 5C: Frontend - Newsletter Subscription UI 🎨
**Estimated Time**: 4-5 hours

#### Task 5C.1: Create API Client
**File to Create**: `web/src/infrastructure/api/metroAreasApi.ts`

```typescript
import { apiClient } from './client';

export interface MetroAreaDto {
  id: string;
  name: string;
  state: string;
  centerLatitude: number;
  centerLongitude: number;
  radiusMiles: number;
  isStateLevelArea: boolean;
}

export async function getMetroAreas(): Promise<MetroAreaDto[]> {
  const response = await apiClient.get<MetroAreaDto[]>('/metro-areas');
  return response.data;
}
```

**File to Create**: `web/src/infrastructure/api/newsletterApi.ts`

```typescript
export interface NewsletterSubscriptionRequest {
  email: string;
  metroAreaIds: string[]; // Array of UUIDs
  receiveAllLocations: boolean;
}

export interface NewsletterSubscriptionResponse {
  success: boolean;
  message: string;
  subscriberId?: string;
  errorCode?: string;
}

export async function subscribeToNewsletter(
  data: NewsletterSubscriptionRequest
): Promise<NewsletterSubscriptionResponse> {
  const response = await apiClient.post<NewsletterSubscriptionResponse>(
    '/newsletter/subscribe',
    data
  );
  return response.data;
}

export async function confirmNewsletterSubscription(
  token: string
): Promise<NewsletterSubscriptionResponse> {
  const response = await apiClient.get<NewsletterSubscriptionResponse>(
    `/newsletter/confirm?token=${token}`
  );
  return response.data;
}
```

#### Task 5C.2: Create React Query Hooks
**File to Create**: `web/src/presentation/hooks/useMetroAreas.ts`

```typescript
import { useQuery } from '@tanstack/react-query';
import { getMetroAreas } from '@/infrastructure/api/metroAreasApi';

export function useMetroAreas() {
  return useQuery({
    queryKey: ['metro-areas'],
    queryFn: getMetroAreas,
    staleTime: 15 * 60 * 1000, // 15 minutes
    cacheTime: 60 * 60 * 1000, // 1 hour
  });
}
```

**File to Create**: `web/src/presentation/hooks/useNewsletterSubscription.ts`

```typescript
import { useMutation } from '@tanstack/react-query';
import { subscribeToNewsletter } from '@/infrastructure/api/newsletterApi';

export function useNewsletterSubscription() {
  return useMutation({
    mutationFn: subscribeToNewsletter,
    onSuccess: (data) => {
      // Show success message
    },
    onError: (error) => {
      // Show error message
    },
  });
}
```

#### Task 5C.3: Create Newsletter Components
**File to Create**: `web/src/presentation/components/features/newsletter/NewsletterSubscriptionForm.tsx`

```typescript
import React, { useState } from 'react';
import { useMetroAreas } from '@/presentation/hooks/useMetroAreas';
import { useNewsletterSubscription } from '@/presentation/hooks/useNewsletterSubscription';
import { MetroAreaCheckboxList } from './MetroAreaCheckboxList';
import { Input } from '@/presentation/components/ui/Input';
import { Button } from '@/presentation/components/ui/Button';

export function NewsletterSubscriptionForm() {
  const [email, setEmail] = useState('');
  const [selectedMetroIds, setSelectedMetroIds] = useState<string[]>([]);
  const [receiveAll, setReceiveAll] = useState(false);

  const { data: metroAreas, isLoading } = useMetroAreas();
  const { mutate: subscribe, isPending } = useNewsletterSubscription();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    subscribe({
      email,
      metroAreaIds: receiveAll ? [] : selectedMetroIds,
      receiveAllLocations: receiveAll,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <Input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Enter your email"
          required
        />
      </div>

      <div>
        <label className="flex items-center space-x-2">
          <input
            type="checkbox"
            checked={receiveAll}
            onChange={(e) => {
              setReceiveAll(e.target.checked);
              if (e.target.checked) setSelectedMetroIds([]);
            }}
          />
          <span>All Locations (nationwide)</span>
        </label>
      </div>

      {!receiveAll && (
        <MetroAreaCheckboxList
          metroAreas={metroAreas ?? []}
          selectedIds={selectedMetroIds}
          onChange={setSelectedMetroIds}
          isLoading={isLoading}
        />
      )}

      <Button type="submit" disabled={isPending}>
        {isPending ? 'Subscribing...' : 'Subscribe to Newsletter'}
      </Button>
    </form>
  );
}
```

**File to Create**: `web/src/presentation/components/features/newsletter/MetroAreaCheckboxList.tsx`

```typescript
interface MetroAreaCheckboxListProps {
  metroAreas: MetroAreaDto[];
  selectedIds: string[];
  onChange: (ids: string[]) => void;
  isLoading?: boolean;
}

export function MetroAreaCheckboxList({
  metroAreas,
  selectedIds,
  onChange,
  isLoading
}: MetroAreaCheckboxListProps) {
  // Group metros by state
  const metrosByState = metroAreas.reduce((acc, metro) => {
    if (!acc[metro.state]) acc[metro.state] = [];
    acc[metro.state].push(metro);
    return acc;
  }, {} as Record<string, MetroAreaDto[]>);

  const handleToggle = (metroId: string) => {
    if (selectedIds.includes(metroId)) {
      onChange(selectedIds.filter(id => id !== metroId));
    } else {
      onChange([...selectedIds, metroId]);
    }
  };

  if (isLoading) return <div>Loading metro areas...</div>;

  return (
    <div className="space-y-4">
      <p className="font-medium">Get notifications for events in:</p>
      {Object.entries(metrosByState).map(([state, metros]) => (
        <div key={state} className="space-y-2">
          <p className="font-semibold text-sm text-gray-700">{state}:</p>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
            {metros.map((metro) => (
              <label key={metro.id} className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  checked={selectedIds.includes(metro.id)}
                  onChange={() => handleToggle(metro.id)}
                />
                <span className="text-sm">{metro.name}</span>
              </label>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}
```

#### Task 5C.4: Create Confirmation Page
**File to Create**: `web/src/app/newsletter/confirm/page.tsx`

```typescript
'use client';

import { useSearchParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { confirmNewsletterSubscription } from '@/infrastructure/api/newsletterApi';

export default function NewsletterConfirmPage() {
  const searchParams = useSearchParams();
  const token = searchParams.get('token');

  const { data, isLoading, error } = useQuery({
    queryKey: ['newsletter-confirm', token],
    queryFn: () => confirmNewsletterSubscription(token!),
    enabled: !!token,
  });

  if (!token) {
    return <div>Invalid confirmation link</div>;
  }

  if (isLoading) {
    return <div>Confirming your subscription...</div>;
  }

  if (error || !data?.success) {
    return <div>Failed to confirm subscription: {data?.message || 'Unknown error'}</div>;
  }

  return (
    <div className="max-w-2xl mx-auto p-8 text-center">
      <h1 className="text-3xl font-bold text-green-600 mb-4">
        ✅ Subscription Confirmed!
      </h1>
      <p className="text-lg">
        Thank you for subscribing to the LankaConnect newsletter.
        You'll start receiving updates soon.
      </p>
    </div>
  );
}
```

#### Task 5C.5: Integrate into Landing Page
**File to Modify**: `web/src/app/page.tsx`

Add newsletter section after hero, before community stats:
```typescript
import { NewsletterSubscriptionForm } from '@/presentation/components/features/newsletter/NewsletterSubscriptionForm';

export default function HomePage() {
  return (
    <>
      {/* Hero Section */}
      <HeroSection />

      {/* Newsletter Section - NEW */}
      <section className="py-16 bg-gray-50">
        <div className="container mx-auto px-4">
          <div className="max-w-3xl mx-auto">
            <h2 className="text-3xl font-bold text-center mb-4">
              📬 Stay Connected with Our Newsletter
            </h2>
            <p className="text-center text-gray-600 mb-8">
              Get weekly updates on events, businesses, and community news
              delivered to your inbox.
            </p>
            <NewsletterSubscriptionForm />
          </div>
        </div>
      </section>

      {/* Community Stats Section */}
      <CommunityStatsSection />
    </>
  );
}
```

---

### Phase 5D: Testing 📝
**Estimated Time**: 2-3 hours

#### Task 5D.1: Backend Tests
**Files to Create**:
- `tests/LankaConnect.Application.Tests/MetroAreas/Queries/GetMetroAreasQueryHandlerTests.cs`
- `tests/LankaConnect.Application.Tests/Communications/Commands/SubscribeToNewsletterCommandHandlerTests.cs` (update for multi-select)

#### Task 5D.2: Frontend Tests
**Files to Create**:
- `web/tests/unit/presentation/components/features/newsletter/NewsletterSubscriptionForm.test.tsx`
- `web/tests/unit/presentation/components/features/newsletter/MetroAreaCheckboxList.test.tsx`
- `web/tests/integration/newsletter-subscription-flow.test.ts`

---

## Database Migration Strategy

### Step 1: Check Existing Metro Areas
Query staging database:
```sql
SELECT id, name, state FROM events.metro_areas ORDER BY state, name;
```

### Step 2: Seed Missing Metro Areas (if needed)
If database doesn't have all 28 metros from frontend constants, create seeding migration:
```csharp
migrationBuilder.InsertData(
    schema: "events",
    table: "metro_areas",
    columns: new[] { "id", "name", "state", "center_latitude", "center_longitude", "radius_miles", "is_state_level_area" },
    values: new object[,]
    {
        { Guid.Parse("..."), "Cleveland", "OH", 41.4993, -81.6944, 30, false },
        // ... 27 more metros
    });
```

### Step 3: Create Junction Table
Run migration with junction table schema (Phase 5B.1)

### Step 4: Data Migration (if subscribers exist)
Migrate existing newsletter_subscribers.metro_area_id to junction table:
```sql
INSERT INTO communications.newsletter_subscriber_metro_areas (subscriber_id, metro_area_id)
SELECT id, metro_area_id
FROM communications.newsletter_subscribers
WHERE metro_area_id IS NOT NULL;
```

---

## Deployment Checklist

### Backend Deployment
- [ ] Build passes locally
- [ ] All tests pass (TDD)
- [ ] Migration tested locally (optional, we use Azure DB directly)
- [ ] Push to develop branch
- [ ] GitHub Actions deploy-staging.yml runs
- [ ] Verify GET /api/metro-areas returns data
- [ ] Verify POST /api/newsletter/subscribe accepts array of metro IDs

### Frontend Deployment
- [ ] TypeScript builds without errors
- [ ] Tests pass
- [ ] Newsletter form renders correctly
- [ ] Metro areas load from API
- [ ] Multi-select checkboxes work
- [ ] Form submission works end-to-end
- [ ] Confirmation page works

---

## Testing Plan

### Manual Testing Scenarios

#### Scenario 1: Subscribe with Multiple Metros
1. Navigate to landing page
2. Enter email
3. Select 3 metros (e.g., Cleveland, Columbus, Cincinnati)
4. Click "Subscribe to Newsletter"
5. Verify success message
6. Check email for confirmation
7. Click confirmation link
8. Verify confirmation page shows success

#### Scenario 2: Subscribe with All Locations
1. Navigate to landing page
2. Enter email
3. Check "All Locations (nationwide)"
4. Verify metro checkboxes are disabled/hidden
5. Submit form
6. Verify backend receives `receiveAllLocations: true, metroAreaIds: []`

#### Scenario 3: Metro Areas Load from Database
1. Open browser DevTools Network tab
2. Navigate to landing page
3. Verify request to `GET /api/metro-areas`
4. Verify response contains all metros with UUIDs
5. Verify checkboxes render with correct metro names

---

## Rollback Plan

If Phase 5 deployment fails:

1. **Backend Rollback**:
   - Revert to previous commit
   - Junction table won't break existing functionality (no foreign key from newsletter_subscribers yet)

2. **Frontend Rollback**:
   - Keep using hardcoded constants temporarily
   - Comment out newsletter form integration

3. **Database Rollback**:
   - Drop junction table: `DROP TABLE communications.newsletter_subscriber_metro_areas;`
   - Existing newsletter_subscribers table remains intact

---

## Success Criteria

✅ Phase 5 is complete when:
1. GET /api/metro-areas returns all metros from database
2. Users can select multiple metro areas via checkboxes
3. POST /api/newsletter/subscribe accepts array of metro_area_ids
4. Confirmation email includes all selected metros
5. Newsletter form integrates into landing page
6. Confirmation page works (/newsletter/confirm?token=...)
7. All tests pass (90% coverage)
8. Documentation updated

---

## Next Steps

1. **Start with Phase 5A** (Backend - Metro Areas API)
2. **Then Phase 5B** (Backend - Multi-Select Support)
3. **Then Phase 5C** (Frontend - UI Components)
4. **Finally Phase 5D** (Testing)

Each phase should follow TDD:
- ❌ Red: Write failing test
- ✅ Green: Make test pass
- ♻️ Refactor: Clean up code
- 📝 Document: Update progress tracker

---

**Estimated Total Time**: 11-15 hours
**Target Completion**: Next 2-3 days
**Priority**: HIGH (User requested feature)

---

**Last Updated**: 2025-11-09
**Author**: Claude Code
**Status**: Ready for implementation - awaiting confirmation to proceed
