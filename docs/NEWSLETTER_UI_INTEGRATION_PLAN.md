# Newsletter Subscription UI Integration Plan

**Date**: 2025-11-09
**Status**: 📋 PLANNING PHASE
**Epic**: Newsletter Subscription System - Phase 5 (UI Integration)

---

## Requirements Analysis

### 1. Newsletter Subscription API Integration
**Requirement**: Integrate newsletter subscription APIs with the landing page UI

**Current State**:
- ✅ Backend APIs fully implemented and deployed to staging
  - POST `/api/newsletter/subscribe` - Create subscription
  - GET `/api/newsletter/confirm?token={token}` - Confirm subscription
- ✅ Email templates created (newsletter-confirmation-html.html)
- ⚠️ SMTP not configured in staging (emails won't send but API works)

**Endpoints Available**:
```typescript
// Subscribe to newsletter
POST /api/newsletter/subscribe
Body: {
  email: string;
  metroAreaId?: string | null;  // UUID or null
  receiveAllLocations: boolean;
}
Response: {
  success: boolean;
  message: string;
  subscriberId?: string;
  errorCode?: string;
}

// Confirm subscription
GET /api/newsletter/confirm?token={confirmationToken}
Response: {
  success: boolean;
  message: string;
}
```

### 2. Multi-Select Location Checkboxes
**Requirement**: Replace single dropdown with checkboxes for multiple location selection

**Current Implementation**:
- **UI**: Single `MetroAreaSelector` dropdown (web/src/presentation/components/features/location/MetroAreaSelector.tsx)
- **Data**: Metro areas hardcoded in TypeScript constants (web/src/domain/constants/metroAreas.constants.ts)
- **Available Areas**: 24 metro areas + 4 state-level areas = 28 total options

**Proposed Changes**:
- Replace single select with multi-select checkbox list
- Allow users to select multiple metro areas for newsletter notifications
- Include "All Locations" option (maps to `receiveAllLocations: true`)

### 3. Metro Area Data Source
**Requirement**: Determine if metro areas should be hardcoded or database-driven

**Current State Analysis**:

#### Frontend (UI)
**Location**: `web/src/domain/constants/metroAreas.constants.ts`
**Status**: ✅ **HARDCODED** - 559 lines of TypeScript constants

Metro Areas Available:
- 6 Ohio metros (Cleveland, Columbus, Cincinnati, Toledo, Akron, Dayton)
- 18 Major US metros (NYC, LA, Chicago, Houston, Phoenix, etc.)
- 4 State-level areas (All Ohio, All Pennsylvania, All New York, All Indiana)
- **Total**: 28 locations

```typescript
export const ALL_METRO_AREAS = [...US_METRO_AREAS, ...STATE_LEVEL_AREAS] as const;
```

#### Backend (API)
**Location**: Database table `events.metro_areas` (PostgreSQL)
**Status**: ✅ **DATABASE TABLE EXISTS**

Evidence from Phase 3:
- Script exists: `scripts/GetMetroAreaId.cs` - queries `events.metro_areas` table
- Newsletter migration references metro_area_id: `metro_area_id uuid REFERENCES events.metro_areas(id)`

**Gap Identified**: Frontend uses hardcoded data, Backend has database table with UUIDs

---

## Architectural Decision: Data Source

### Option A: Keep Hardcoded (Frontend Only) ❌ **NOT RECOMMENDED**
**Pros**:
- No API changes needed
- Fast client-side filtering
- No additional API calls

**Cons**:
- ❌ Data duplication (frontend constants vs backend database)
- ❌ No single source of truth
- ❌ Manual sync required when adding new metros
- ❌ Frontend metro IDs are strings (`'cleveland-oh'`) but backend expects UUIDs
- ❌ Impossible to map frontend selection to backend `metro_area_id` column

### Option B: Use Backend Database ✅ **RECOMMENDED**
**Pros**:
- ✅ Single source of truth (database)
- ✅ Easy to add new metro areas (no code changes)
- ✅ Consistent UUIDs across frontend/backend
- ✅ Can add additional metadata (population, timezone, etc.)
- ✅ Supports future features (admin panel for metro management)

**Cons**:
- Requires new API endpoint: `GET /api/metro-areas`
- Additional API call on page load (can be cached)

---

## Recommended Solution

### Architecture: Database-Driven Metro Areas with API

**Step 1: Create Metro Areas API Endpoint**
```csharp
// GET /api/metro-areas
public class MetroAreaDto {
  public Guid Id { get; set; }
  public string Name { get; set; }
  public string State { get; set; }
  public double CenterLatitude { get; set; }
  public double CenterLongitude { get; set; }
  public int RadiusMiles { get; set; }
  public bool IsStateLevelArea { get; set; }
}
```

**Step 2: Verify/Seed Metro Areas in Database**
- Check if `events.metro_areas` table has all 28 metros from frontend constants
- If not, create migration to seed missing data
- Ensure UUIDs are consistent

**Step 3: Create Frontend API Client**
```typescript
// web/src/infrastructure/api/metroAreasApi.ts
export async function getMetroAreas(): Promise<MetroArea[]> {
  const response = await fetch('/api/metro-areas');
  return response.json();
}
```

**Step 4: Update Landing Page to Fetch Metro Areas**
```typescript
// Replace hardcoded ALL_METRO_AREAS with API call
const { data: metroAreas } = useMetroAreas(); // React Query hook
```

**Step 5: Build Multi-Select Checkbox Component**
```typescript
<NewsletterSubscriptionForm
  metroAreas={metroAreas}
  onSubmit={(data) => subscribeToNewsletter(data)}
/>
```

---

## Implementation Plan - Phase 5

### Phase 5A: Metro Areas API (Backend) 🔧

**Estimated Time**: 2-3 hours

1. **Verify Database Table**
   - Query `events.metro_areas` to check existing data
   - Document current metro area UUIDs

2. **Create Metro Area Query Handler** (CQRS)
   - Command: `GetMetroAreasQuery.cs`
   - Handler: `GetMetroAreasQueryHandler.cs`
   - Response: `MetroAreaDto.cs`
   - Tests: `GetMetroAreasQueryHandlerTests.cs` (TDD)

3. **Add API Controller Endpoint**
   - Endpoint: `GET /api/metro-areas`
   - Cache response (15 minutes TTL)
   - Return all active metro areas

4. **Seed Missing Metro Areas** (if needed)
   - Create migration to seed 28 metro areas
   - Map frontend string IDs to database UUIDs
   - Include lat/lng coordinates and radius

5. **Test & Deploy**
   - Unit tests
   - Integration test
   - Deploy to staging

### Phase 5B: Newsletter Subscription UI (Frontend) 🎨

**Estimated Time**: 3-4 hours

1. **Create Newsletter Subscription Form Component**
   ```
   web/src/presentation/components/features/newsletter/
   ├── NewsletterSubscriptionForm.tsx
   ├── MetroAreaCheckboxList.tsx
   ├── SubscriptionSuccess.tsx
   └── SubscriptionConfirmation.tsx (for /newsletter/confirm page)
   ```

2. **Create API Client Hooks**
   ```typescript
   // web/src/infrastructure/api/newsletterApi.ts
   export function subscribeToNewsletter(data: NewsletterSubscriptionRequest)
   export function confirmNewsletterSubscription(token: string)

   // web/src/presentation/hooks/useNewsletterSubscription.ts
   export function useNewsletterSubscription()
   export function useNewsletterConfirmation(token: string)
   ```

3. **Multi-Select Checkbox List Component**
   - Display all metro areas as checkboxes
   - Group by state (Ohio, Pennsylvania, New York, etc.)
   - "All Locations" checkbox (unchecks all others)
   - Individual metro checkboxes
   - State-level checkboxes (All Ohio, All Pennsylvania, etc.)

4. **Integrate into Landing Page**
   - Add newsletter section to `web/src/app/page.tsx`
   - Position: After hero section, before community stats
   - Mobile-responsive design
   - Inline validation

5. **Create Confirmation Page**
   - Route: `web/src/app/newsletter/confirm/page.tsx`
   - Extract token from query params
   - Call confirmation API
   - Show success/error message

6. **Update Footer Component**
   - Add newsletter signup link
   - Link to newsletter privacy policy

### Phase 5C: Testing & Documentation 📝

**Estimated Time**: 1-2 hours

1. **Component Tests**
   - NewsletterSubscriptionForm.test.tsx
   - MetroAreaCheckboxList.test.tsx
   - Test multi-select behavior
   - Test "All Locations" toggle

2. **Integration Tests**
   - Newsletter subscription flow
   - Metro area selection
   - API error handling

3. **Documentation**
   - Update PROGRESS_TRACKER.md
   - Create NEWSLETTER_PHASE5_SUMMARY.md
   - User guide for newsletter subscription

---

## UI/UX Design Mockup

### Newsletter Subscription Section (Landing Page)

```
┌──────────────────────────────────────────────────────┐
│  📬 Stay Connected with LankaConnect Newsletter      │
│                                                       │
│  Get weekly updates on events, businesses, and       │
│  community news delivered to your inbox.             │
│                                                       │
│  ┌─────────────────────────────────────────────┐    │
│  │ Email Address *                             │    │
│  │ ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ │    │
│  └─────────────────────────────────────────────┘    │
│                                                       │
│  Get notifications for events in:                    │
│                                                       │
│  ☐ All Locations (nationwide)                        │
│                                                       │
│  Ohio:                                               │
│  ☐ Cleveland    ☐ Columbus    ☐ Cincinnati         │
│  ☐ Toledo       ☐ Akron       ☐ Dayton             │
│  ☐ All Ohio                                          │
│                                                       │
│  Other States:                                       │
│  ☐ New York City     ☐ Los Angeles                  │
│  ☐ Chicago           ☐ Houston                      │
│  ☐ Phoenix           ☐ Philadelphia                 │
│  ... (expandable/collapsible by state)               │
│                                                       │
│  [Subscribe to Newsletter]                           │
│                                                       │
│  By subscribing, you agree to our Privacy Policy.   │
└──────────────────────────────────────────────────────┘
```

### Mobile View (Responsive)

```
┌──────────────────────┐
│ 📬 Newsletter Signup │
│                      │
│ Email:               │
│ ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ │
│                      │
│ Locations:           │
│ ☐ All Locations      │
│                      │
│ [Expand Ohio ▼]      │
│                      │
│ [Subscribe]          │
└──────────────────────┘
```

---

## Technical Specifications

### Metro Area Multi-Select Logic

```typescript
interface NewsletterSubscriptionData {
  email: string;
  selectedMetroAreaIds: string[]; // Array of UUIDs
  receiveAllLocations: boolean;
}

// If "All Locations" is checked:
{
  email: "user@example.com",
  metroAreaId: null,
  receiveAllLocations: true
}

// If specific metros are selected:
// Backend needs to support multiple metro areas
// Option 1: Store as array in newsletter_subscribers table
// Option 2: Create junction table newsletter_subscriber_metro_areas

// Current backend expects SINGLE metro_area_id
// Need to decide on approach
```

### Backend Schema Decision Required

**Current Schema** (newsletter_subscribers table):
```sql
metro_area_id uuid NULL REFERENCES events.metro_areas(id)
receive_all_locations boolean NOT NULL DEFAULT false
```

**Issue**: Can only store ONE metro area per subscriber

**Option 1: Keep Single Metro** (Simple)
- User must choose ONE specific metro OR all locations
- No multi-select needed
- Matches current backend implementation

**Option 2: Support Multiple Metros** (Complex)
Requires backend changes:

**Option 2A: Array Column**
```sql
ALTER TABLE communications.newsletter_subscribers
ADD COLUMN metro_area_ids uuid[] NULL;
```

**Option 2B: Junction Table** (Most Flexible)
```sql
CREATE TABLE communications.newsletter_subscriber_metro_areas (
  subscriber_id uuid REFERENCES newsletter_subscribers(id),
  metro_area_id uuid REFERENCES events.metro_areas(id),
  PRIMARY KEY (subscriber_id, metro_area_id)
);
```

---

## Recommendation

### Simplified Approach (MVP - Fastest to Implement)

**Keep Current Backend Schema**: Single metro area OR all locations

**UI Changes**:
1. Radio button group (instead of checkboxes):
   - ○ All Locations (nationwide)
   - ○ Specific Location: [Dropdown with all metros]

**Rationale**:
- ✅ No backend changes needed
- ✅ Matches existing database schema
- ✅ Faster to implement (1-2 days vs 3-4 days)
- ✅ Still provides location-based filtering
- ⚠️ Users can only select ONE metro (limitation)

### Future Enhancement (v2)

If users demand multi-select:
1. Add junction table `newsletter_subscriber_metro_areas`
2. Update backend handler to support array of metro IDs
3. Convert radio buttons to checkboxes
4. Migration to split existing single metro_area_id into junction table

---

## Questions for User

Before proceeding with implementation, please confirm:

1. **Multi-Select Requirement**:
   - ❓ Do users NEED to select multiple metro areas for newsletter?
   - Or is "Select ONE metro OR all locations" acceptable?

2. **Backend Changes**:
   - ❓ Are you OK with backend changes (junction table, migration)?
   - Or prefer to keep current single metro_area_id design?

3. **Metro Area Data Source**:
   - ❓ Should we use database (`events.metro_areas` table) as source of truth?
   - Or keep frontend hardcoded constants?

4. **Implementation Priority**:
   - ❓ Which approach do you prefer?
     - **Option A**: Radio buttons (single metro OR all) - Faster, no backend changes
     - **Option B**: Checkboxes (multiple metros) - Slower, requires backend changes

---

## Next Steps

1. **User Decision**: Answer questions above
2. **Create Detailed Task Breakdown**: Based on chosen approach
3. **Start Implementation**: Following TDD and Clean Architecture principles
4. **Update Documentation**: Track progress in PROGRESS_TRACKER.md

---

**Last Updated**: 2025-11-09
**Author**: Claude Code
**Status**: Awaiting user decision on approach
