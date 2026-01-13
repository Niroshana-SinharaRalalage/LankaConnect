# Phase 6A.74 Part 8 - Public Newsletters Pages Implementation Plan

**Created**: 2026-01-13
**Status**: PLANNING
**Priority**: HIGH - Critical missing features identified by user

---

## 🎯 User Requirements (New Clarifications)

### 1. Edit Existing Newsletter ✅
**Requirement**: Ability to edit newsletters
**Status**: ALREADY IMPLEMENTED in Part 4-6
**Location**: `[id]/edit/page.tsx` (exists in dashboard)
**Verification Needed**: Confirm this works correctly

### 2. Send Email Button with Template ❓
**Requirement**: "Button to send an email related to that newsletter based on a template stored in the database"
**Current State**: Send Email button exists in `[id]/page.tsx` (line 169-175)
**Question**: Does it use database template? Need to verify `SendNewsletterCommand`
**Action**: Verify backend uses EmailTemplateService with database templates

### 3. Landing Page Display Logic 🔴 MISSING
**Requirement**:
- Landing page shows 3 most recent newsletters ✅ (DONE in Part 5)
- When user clicks link, should see ALL newsletters in `/newsletters` page ❌ MISSING
- Display logic should be similar to 4 events on landing page
- Based on newsletter subscription AND current location

**Critical Gap**: No public `/newsletters` list page exists!

### 4. Public Newsletter Details Page 🔴 MISSING
**Requirement**: `/newsletters/{id}` public page to view details (not dashboard)
**Current State**: Only `/dashboard/newsletters/{id}` exists (route group)
**Critical Gap**: No public details page for non-organizers to read newsletters!

### 5. Filtration & Search 🔴 MISSING
**Requirement**: `/newsletters` should be similar to `/events` with:
- Location-based filtering
- Search functionality
- Default sorting (like events: location relevance + recency)

### 6. Routing Clarification
**Current**:
- `app/(dashboard)/newsletters/` → Dashboard routes (authenticated users)
- `app/newsletter/` → Subscription management (`/newsletter/confirm`, `/newsletter/unsubscribe`)

**Need to Add**:
- `app/newsletters/` → Public newsletter browsing (ALL users)
- `app/newsletters/page.tsx` → Public list page
- `app/newsletters/[id]/page.tsx` → Public details page

---

## 🏗️ Architectural Analysis (System Architect Consultation)

### Pattern 1: `/events` Page Architecture (562 lines)

**Key Components**:
1. **Location-Based Sorting**:
   ```typescript
   const { latitude, longitude, loading } = useGeolocation(isAnonymous);
   const filters = {
     userId: user?.userId,
     latitude: isAnonymous ? latitude : undefined,
     longitude: isAnonymous ? longitude : undefined,
     metroAreaIds: selectedMetroIds,
     state: selectedState,
   };
   ```

2. **Three-Column Filtration**:
   - Event Type (dropdown)
   - Event Date (dropdown: upcoming, this week, next week, next month, all)
   - Location (TreeDropdown with State → Metro Areas)

3. **Search with Debouncing**:
   ```typescript
   const [searchInput, setSearchInput] = useState('');
   const debouncedSearchTerm = useDebounce(searchInput, 500);
   ```

4. **Grid Display**: 3-column responsive grid with event cards

5. **Empty States**: Handles loading, error, no results

### Pattern 2: Landing Page Featured Events (4 events)

**Hook**: `useFeaturedEvents(userId, latitude, longitude)`
- For authenticated: Uses preferred metro areas
- For anonymous: Uses geolocation coordinates
- Returns up to 4 events sorted by location relevance
- 5-minute cache

### Pattern 3: Event Card vs. Newsletter Card

**Event Card** has:
- Image with badge overlays
- Category badge
- Display label (lifecycle status)
- Registration badge
- Date, location, capacity
- Pricing
- "View Details" button

**Newsletter Card** (current) has:
- No image (newsletters don't have images)
- Status badge
- Created date
- Sent date (if sent)
- Expires date (if Active)
- Linked event (if applicable)
- Email groups count
- Location targeting badges

---

## 📋 Implementation Plan

### Part 8A: Backend - Public Newsletter Queries (If Needed)

**Verify Existing**:
1. Check if `/api/newsletters/published` supports location filtering
2. Check if it supports search parameter
3. Check if it returns data needed for public display

**Add If Missing**:
```csharp
// NewslettersController.cs
[HttpGet("public")]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<NewsletterSummaryDto>>> GetPublicNewsletters(
    [FromQuery] string? searchTerm,
    [FromQuery] Guid[]? metroAreaIds,
    [FromQuery] string? state,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 20)
{
    // Query Active newsletters (Status = Active, ExpiresAt > now)
    // Filter by location if metroAreaIds or state provided
    // Filter by search term in title/description
    // Sort by: location relevance (if user location), then PublishedAt DESC
    // Return paginated results
}
```

**Newsletter Summary DTO** (for list):
```csharp
public class NewsletterSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string DescriptionPreview { get; set; } // First 200 chars, stripped HTML
    public DateTime PublishedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? EventTitle { get; set; } // If linked to event
    public Guid? EventId { get; set; }
    public int EmailGroupCount { get; set; }
    public bool TargetAllLocations { get; set; }
    public List<MetroAreaDto> MetroAreas { get; set; } // Location targeting
}
```

**Newsletter Public Details DTO** (for details page):
```csharp
public class NewsletterPublicDetailsDto : NewsletterSummaryDto
{
    public string Description { get; set; } // Full HTML content
    public string? EventLocation { get; set; }
    public DateTime? EventStartDate { get; set; }
    public string? EventDetailsUrl { get; set; }
    public string? EventSignupListsUrl { get; set; }
}
```

### Part 8B: Frontend - Repository & Hooks

**Add to `newsletters.repository.ts`**:
```typescript
// Get public newsletters with filters (mirror events pattern)
getPublicNewsletters(params: {
  searchTerm?: string;
  metroAreaIds?: string[];
  state?: string;
  userId?: string; // For location-based sorting
  latitude?: number;
  longitude?: number;
  skip?: number;
  take?: number;
}): Promise<NewsletterSummaryDto[]>

// Get public newsletter details (no auth required)
getPublicNewsletterDetails(id: string): Promise<NewsletterPublicDetailsDto>
```

**Add to `useNewsletters.ts`**:
```typescript
// Mirror useFeaturedEvents pattern
export function useFeaturedNewsletters(
  userId?: string,
  latitude?: number,
  longitude?: number
) {
  return useQuery({
    queryKey: newsletterKeys.featured(userId, latitude, longitude),
    queryFn: () => newslettersRepository.getFeaturedNewsletters(userId, latitude, longitude),
    staleTime: 5 * 60 * 1000, // 5 minutes (same as events)
  });
}

// Mirror useEvents pattern
export function usePublicNewsletters(filters: NewsletterFilters) {
  return useQuery({
    queryKey: newsletterKeys.publicList(filters),
    queryFn: () => newslettersRepository.getPublicNewsletters(filters),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

// Public details (no auth)
export function usePublicNewsletterDetails(id: string) {
  return useQuery({
    queryKey: newsletterKeys.publicDetail(id),
    queryFn: () => newslettersRepository.getPublicNewsletterDetails(id),
    enabled: !!id,
  });
}
```

### Part 8C: Frontend - Public Newsletters List Page

**File**: `web/src/app/newsletters/page.tsx`

**Architecture** (mirror `/events/page.tsx`):
1. **Same filtration UI**:
   - Search input with 500ms debounce
   - Date range dropdown (this week, next week, next month, all)
   - Location TreeDropdown (State → Metro Areas)

2. **Same geolocation logic**:
   ```typescript
   const isAnonymous = !user?.userId;
   const { latitude, longitude, loading } = useGeolocation(isAnonymous);
   ```

3. **Newsletter-specific filters**:
   - No "Event Type" dropdown (newsletters don't have categories)
   - Add "Related to Events" toggle? (optional)

4. **Grid Display**: 3-column responsive grid with newsletter cards

5. **Newsletter Card** (for list):
   - Title (bold, 2-line clamp)
   - Description preview (3-line clamp, stripped HTML)
   - Published date
   - Expires date (if applicable)
   - Linked event badge (if applicable)
   - Location badges (metro areas or "All Locations")
   - Status badge (Active only for public)
   - "Read More" button → `/newsletters/{id}`

6. **Empty States**: Same as events (loading, error, no results)

### Part 8D: Frontend - Public Newsletter Details Page

**File**: `web/src/app/newsletters/[id]/page.tsx`

**Architecture**:
1. **Hero Section** (like event details):
   - Newsletter title
   - Published date
   - Expires date
   - Status badge (Active only)

2. **Content Section**:
   - Full HTML description (rendered safely with DOMPurify)
   - If linked to event: Event card with details, "View Event" button

3. **Location Targeting Display**:
   - Shows metro areas OR "Available to all locations"

4. **Back Button**: "← Back to Newsletters" → `/newsletters`

5. **Share Button**: Copy link, social media sharing (optional)

**No Edit/Delete/Send Buttons** - Public view only!

### Part 8E: Frontend - Update Landing Page

**File**: `web/src/app/page.tsx`

**Current**: Shows 3 newsletters with `usePublishedNewsletters()`

**Update**:
1. Change hook to `useFeaturedNewsletters(userId, latitude, longitude)`
2. Add "View All Newsletters" link → `/newsletters`
3. Ensure newsletter cards link to `/newsletters/{id}` (not dashboard)

---

## 🧪 Testing Strategy

### Backend API Testing
1. `/api/newsletters/public` with location filters
2. `/api/newsletters/public` with search term
3. `/api/newsletters/{id}/public` (no auth)
4. Verify location-based sorting logic

### Frontend E2E Testing
1. **Landing Page**:
   - Verify 3 featured newsletters display
   - Verify location-based sorting (authenticated vs anonymous)
   - Click "View All" → redirects to `/newsletters`
   - Click newsletter card → redirects to `/newsletters/{id}`

2. **`/newsletters` List Page**:
   - Search functionality
   - Location filtering
   - Date range filtering
   - Empty states (no results, loading, error)
   - Pagination (if implemented)

3. **`/newsletters/{id}` Details Page**:
   - Displays full content safely (HTML rendering)
   - Shows linked event (if applicable)
   - Shows location targeting
   - "Back to Newsletters" works
   - No edit/delete buttons visible

### Authorization Testing
1. **Anonymous users**: Can access `/newsletters` and `/newsletters/{id}`
2. **Authenticated users**: Can access both public and dashboard routes
3. **Dashboard routes**: Still require authentication

---

## 📊 Comparison: Events vs. Newsletters

| Feature | Events | Newsletters (Plan) |
|---------|--------|-------------------|
| Public List Page | `/events` ✅ | `/newsletters` ❌ (Part 8) |
| Public Details Page | `/events/{id}` ✅ | `/newsletters/{id}` ❌ (Part 8) |
| Search | ✅ Debounced | ✅ Same pattern |
| Location Filter | ✅ TreeDropdown | ✅ Same pattern |
| Date Filter | ✅ Dropdown | ✅ Similar |
| Category Filter | ✅ Event Type | ❌ N/A |
| Featured Display | ✅ 4 events | ✅ 3 newsletters |
| Location Sorting | ✅ Geo + metros | ✅ Same logic |
| Anonymous Access | ✅ Yes | ✅ Yes |
| Card Component | EventCard | NewsletterCard |
| Images | ✅ Yes | ❌ No images |

---

## 🚀 Implementation Order

### Sprint 1: Backend (1-2 hours)
1. Add `GetPublicNewsletters` endpoint
2. Add `GetPublicNewsletterDetails` endpoint
3. Add DTOs for summary and details
4. Add location-based sorting logic (mirror events)
5. Add unit tests
6. Deploy to staging

### Sprint 2: Frontend Repository & Hooks (1 hour)
1. Add `getPublicNewsletters()` to repository
2. Add `getPublicNewsletterDetails()` to repository
3. Add `useFeaturedNewsletters()` hook
4. Add `usePublicNewsletters()` hook
5. Add `usePublicNewsletterDetails()` hook
6. Update query key factories

### Sprint 3: Public List Page (2-3 hours)
1. Create `app/newsletters/page.tsx`
2. Implement filtration UI (mirror events)
3. Implement geolocation logic
4. Create NewsletterListCard component
5. Handle loading/error/empty states
6. Add "Clear Filters" functionality
7. Test responsiveness

### Sprint 4: Public Details Page (1-2 hours)
1. Create `app/newsletters/[id]/page.tsx`
2. Implement hero section
3. Implement content rendering (safe HTML)
4. Add linked event display
5. Add location targeting display
6. Add "Back to Newsletters" button
7. Test on mobile

### Sprint 5: Landing Page Update (30 mins)
1. Change `usePublishedNewsletters()` to `useFeaturedNewsletters()`
2. Update newsletter card links to `/newsletters/{id}`
3. Add "View All Newsletters" link → `/newsletters`
4. Test location-based sorting

### Sprint 6: Testing & Deployment (1-2 hours)
1. End-to-end testing (all scenarios)
2. Cross-browser testing
3. Mobile responsiveness testing
4. Performance testing (page load times)
5. Deploy to staging
6. User acceptance testing

---

## 📝 Files to Create/Modify

### Backend (C#)
- [ ] `src/LankaConnect.API/Controllers/NewslettersController.cs` (add endpoints)
- [ ] `src/LankaConnect.Application/Communications/Queries/GetPublicNewsletters/` (new)
- [ ] `src/LankaConnect.Application/Communications/Queries/GetPublicNewsletterDetails/` (new)
- [ ] `src/LankaConnect.Application/Communications/DTOs/NewsletterSummaryDto.cs` (new)
- [ ] `src/LankaConnect.Application/Communications/DTOs/NewsletterPublicDetailsDto.cs` (new)

### Frontend (TypeScript/React)
- [ ] `web/src/app/newsletters/page.tsx` (new - list page)
- [ ] `web/src/app/newsletters/[id]/page.tsx` (new - details page)
- [ ] `web/src/infrastructure/api/repositories/newsletters.repository.ts` (modify)
- [ ] `web/src/presentation/hooks/useNewsletters.ts` (modify)
- [ ] `web/src/presentation/components/features/newsletters/NewsletterListCard.tsx` (new)
- [ ] `web/src/app/page.tsx` (modify landing page)

### Documentation
- [ ] Update `PHASE_6A74_COMPLETE_REQUIREMENTS_CHECKLIST.md`
- [ ] Create `PHASE_6A74_PART_8_SUMMARY.md`
- [ ] Update `PROGRESS_TRACKER.md`
- [ ] Update `STREAMLINED_ACTION_PLAN.md`

---

## ✅ Definition of Done

- [ ] All 6 sprints completed
- [ ] All files created/modified
- [ ] Backend tests passing (>90% coverage)
- [ ] Frontend builds with 0 errors
- [ ] All E2E test scenarios passing
- [ ] Deployed to staging successfully
- [ ] User acceptance testing passed
- [ ] Documentation updated
- [ ] "Unknown" status bug fixed (Part 8 dependency)
- [ ] Edit functionality verified (requirement #1)
- [ ] Email template functionality verified (requirement #2)

---

**Estimated Total Time**: 8-12 hours
**Priority**: HIGH - Critical user requirements
**Dependencies**: Fix "Unknown" status bug first (database migration)

**Next Step**: Get user approval of this plan before implementation
