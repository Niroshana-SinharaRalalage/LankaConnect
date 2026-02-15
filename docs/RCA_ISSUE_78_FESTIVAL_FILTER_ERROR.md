# Root Cause Analysis: Issue #78 - Festival Filter Shows Error

**Issue**: When selecting 'Festival' from 'Event Type' filter on the Events page, it shows error message "Failed to load events. Please try again later." instead of showing Festival events.

**Date**: 2026-02-13
**Severity**: High - User-facing feature completely broken
**Issue Type**: **Database Data Missing / Enum Mismatch**

---

## Executive Summary

The "Festival" category filter is failing because **"Festival" does not exist in the backend domain enum** (`EventCategory.cs`) but **does exist in the frontend TypeScript enum** (`events.types.ts`). This is a critical enum synchronization issue between backend and frontend, combined with missing reference data in the database.

**Root Cause**: The backend `EventCategory` enum only has 8 values (Religious=0 through Entertainment=7), while the frontend TypeScript enum has 12 values (Religious=0 through Celebration=11), including Festival=9. When the user selects "Festival" from the dropdown, the frontend sends `category: 9` to the backend API, but the backend cannot parse this as a valid `EventCategory` enum value, causing the query to fail.

---

## Investigation Timeline

### 1. Backend EventCategory Enum Analysis

**File**: `C:\Work\LankaConnect\src\LankaConnect.Domain\Events\Enums\EventCategory.cs`

```csharp
public enum EventCategory
{
    Religious,      // 0
    Cultural,       // 1
    Community,      // 2
    Educational,    // 3
    Social,         // 4
    Business,       // 5
    Charity,        // 6
    Entertainment   // 7
}
```

**Finding**: Backend has **ONLY 8 categories** (0-7). **NO "Festival", "Workshop", "Ceremony", or "Celebration"**.

---

### 2. Frontend EventCategory Enum Analysis

**File**: `C:\Work\LankaConnect\web\src\infrastructure\api\types\events.types.ts`

```typescript
export enum EventCategory {
  Religious = 0,
  Cultural = 1,
  Community = 2,
  Educational = 3,
  Social = 4,
  Business = 5,
  Charity = 6,
  Entertainment = 7,
  Workshop = 8,        // ❌ DOES NOT EXIST IN BACKEND
  Festival = 9,        // ❌ DOES NOT EXIST IN BACKEND
  Ceremony = 10,       // ❌ DOES NOT EXIST IN BACKEND
  Celebration = 11,    // ❌ DOES NOT EXIST IN BACKEND
}
```

**Finding**: Frontend has **12 categories** (0-11). Includes 4 categories that DO NOT exist in backend enum.

---

### 3. API Filter Flow Analysis

**Flow when user selects "Festival"**:

1. **Frontend**: `CategoryFilter.tsx` (Line 50)
   ```typescript
   onChange(e.target.value !== '' ? parseInt(e.target.value, 10) as EventCategory : null)
   ```
   User selects "Festival" → sends `category: 9` to API

2. **API Request**: `GET /api/events?category=9`

3. **Backend**: `GetEventsQuery.cs` (Line 25)
   ```csharp
   EventCategory? Category = null,
   ```
   Backend tries to parse `9` as `EventCategory` enum

4. **Enum Parsing**: C# enum parsing behavior
   - C# allows parsing ANY integer to an enum, even if not defined
   - `(EventCategory)9` succeeds but creates an **undefined enum value**

5. **Query Handler**: `GetEventsQueryHandler.cs` (Line 654)
   ```csharp
   if (request.Category.HasValue)
   {
       filteredEvents = filteredEvents.Where(e => e.Category == request.Category.Value);
   }
   ```
   Query filters by `Category == 9` (undefined value)

6. **Database Query**: EF Core generates SQL
   ```sql
   WHERE category = 9
   ```
   No events have `category = 9` in database (only 0-7 exist)

7. **Result**: Empty result set, which frontend interprets as an error

---

### 4. Database Reference Data Analysis

**Source**: Reference data is loaded from `reference_data.reference_values` table via:
- **Controller**: `ReferenceDataController.cs` - `GET /api/reference-data?types=EventCategory`
- **Service**: `ReferenceDataService.cs` - queries `reference_values` table
- **Frontend Hook**: `useEventCategories()` - fetches and caches data

**Expected Behavior**:
1. Frontend calls `/api/reference-data?types=EventCategory`
2. Backend queries `reference_data.reference_values` WHERE `enum_type = 'EventCategory'`
3. Returns list of categories with `{ code, name, intValue }` for dropdown

**Problem**: If database contains 12 EventCategory entries (including Festival=9) but backend enum only has 8 values, there is a **data integrity issue**.

---

### 5. Historical Context

**From documentation** (`docs/COMPLETE_SOLUTION_CATEGORY_LABEL_ISSUE.md`):

> "Returns: **10 records** (Community, Religious, Cultural, Educational, Social, Business, Workshop, Festival, Ceremony, Celebration)"

**Analysis**: This suggests the database was seeded with 10 categories at some point, but the backend enum was never updated to match. The frontend TypeScript enum was likely auto-generated or manually synced with the database, creating the mismatch.

**From Phase 6A.105 Migration** (`docs/EVENTS_API_SPECIFICATION.md`):

> ### EventCategory
> ```typescript
> enum EventCategory {
>   Religious = 0,
>   Cultural = 1,
>   Community = 2,
>   ...
> }
> ```

This documentation shows the extended enum, suggesting it was an **intentional expansion** that was never implemented in the backend C# code.

---

## Root Cause Summary

**Primary Root Cause**: **Backend/Frontend Enum Mismatch**
- Backend `EventCategory` enum has 8 values (0-7)
- Frontend `EventCategory` enum has 12 values (0-11)
- Database reference_values table likely has 10-12 category entries

**Secondary Root Cause**: **Missing Database Migration**
- No migration exists to add Festival, Workshop, Ceremony, Celebration to backend enum
- Reference data seeding included categories that don't exist in backend enum

**Tertiary Root Cause**: **Poor Error Handling**
- Backend silently accepts invalid enum values (9, 10, 11) instead of validating
- Frontend doesn't validate API response matches frontend enum
- No enum synchronization validation in CI/CD pipeline

---

## Impact Analysis

### User Impact
- **Severity**: High - Complete feature failure
- **Scope**: All users attempting to filter by Festival, Workshop, Ceremony, or Celebration
- **Workaround**: None - filter is completely broken for these categories

### Data Integrity
- Events may exist in database with `category = 9` (Festival) if created via direct SQL or seeding
- These events would be invisible via API when category filter is applied
- Event creation form may allow creating Festival events if frontend allows it

### Business Impact
- Users cannot find Festival events using category filter
- Search functionality is degraded for major event types (festivals are popular)
- User experience is poor - error message instead of helpful feedback

---

## Recommended Fix

### Option 1: Add Missing Categories to Backend Enum (RECOMMENDED)

**Pros**:
- Fixes the root cause completely
- Aligns backend with frontend and database
- Future-proof for category expansion

**Cons**:
- Requires database migration
- Requires deployment of both backend and frontend

**Implementation**:

1. **Update Backend Enum** (`EventCategory.cs`):
   ```csharp
   public enum EventCategory
   {
       Religious,      // 0
       Cultural,       // 1
       Community,      // 2
       Educational,    // 3
       Social,         // 4
       Business,       // 5
       Charity,        // 6
       Entertainment,  // 7
       Workshop,       // 8  ← NEW
       Festival,       // 9  ← NEW
       Ceremony,       // 10 ← NEW
       Celebration     // 11 ← NEW
   }
   ```

2. **Create Migration** to seed reference_values table:
   ```bash
   dotnet ef migrations add Phase6A109_AddMissingEventCategories --project src/LankaConnect.Infrastructure
   ```

3. **Migration Up()**:
   ```csharp
   migrationBuilder.Sql(@"
       INSERT INTO reference_data.reference_values
       (id, enum_type, code, name, int_value, is_active, created_at, updated_at)
       VALUES
       (gen_random_uuid(), 'EventCategory', 'Workshop', 'Workshop', 8, true, NOW(), NOW()),
       (gen_random_uuid(), 'EventCategory', 'Festival', 'Festival', 9, true, NOW(), NOW()),
       (gen_random_uuid(), 'EventCategory', 'Ceremony', 'Ceremony', 10, true, NOW(), NOW()),
       (gen_random_uuid(), 'EventCategory', 'Celebration', 'Celebration', 11, true, NOW(), NOW())
       ON CONFLICT (enum_type, int_value) DO NOTHING;
   ");
   ```

4. **Update Existing Events** (if any have category > 7):
   ```sql
   -- Check if any events have category 8-11
   SELECT id, title, category FROM events.events WHERE category > 7;

   -- If found, data is already correct - just needed enum definition
   ```

5. **Test**: Create Festival event, filter by Festival category

---

### Option 2: Remove Extra Categories from Frontend (NOT RECOMMENDED)

**Pros**:
- Quick fix
- No database changes needed

**Cons**:
- Removes functionality that users may expect
- Doesn't fix data integrity issues
- May break existing events with category 8-11

**Implementation**: Delete Workshop, Festival, Ceremony, Celebration from `events.types.ts`

**Reason Not Recommended**: Based on documentation, Festival/Workshop/Ceremony/Celebration were intentionally added. Removing them is a step backward.

---

## Files to Modify

### Backend Changes
1. **`src/LankaConnect.Domain/Events/Enums/EventCategory.cs`**
   - Add: Workshop = 8, Festival = 9, Ceremony = 10, Celebration = 11

2. **`src/LankaConnect.Infrastructure/Data/Migrations/[timestamp]_Phase6A109_AddMissingEventCategories.cs`**
   - Create migration to seed reference_values table

### Frontend Changes
**NONE** - Frontend is already correct

### Database Changes
- Insert 4 new rows into `reference_data.reference_values` table (if not already present)

### Testing
- **Unit Tests**: `EventCategoryAndPricingTests.cs` - add tests for new categories
- **Integration Tests**: Filter events by Festival, Workshop, Ceremony, Celebration
- **E2E Tests**: Create event with Festival category, filter by Festival

---

## Prevention Measures

### 1. Enum Synchronization Validation
**Add CI/CD check** to ensure backend and frontend enums match:

```typescript
// scripts/validate-enums.ts
import { EventCategory as FrontendCategory } from '@/types/events.types';
import { execSync } from 'child_process';

// Parse backend EventCategory.cs enum
const backendEnum = parseBackendEnum('EventCategory');

// Compare
const frontendKeys = Object.keys(FrontendCategory).filter(k => isNaN(Number(k)));
const backendKeys = Object.keys(backendEnum);

if (!arraysEqual(frontendKeys, backendKeys)) {
  throw new Error('EventCategory enum mismatch between backend and frontend!');
}
```

### 2. Reference Data Validation
**Add startup validation** to ensure reference_values table matches enum definitions:

```csharp
public class ReferenceDataValidator : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var dbCategories = await _db.ReferenceValues
            .Where(r => r.EnumType == "EventCategory")
            .Select(r => r.IntValue)
            .ToListAsync(ct);

        var enumValues = Enum.GetValues<EventCategory>().Cast<int>();

        if (!dbCategories.SequenceEqual(enumValues))
        {
            _logger.LogError("EventCategory enum mismatch with reference_values table!");
        }
    }
}
```

### 3. API Validation
**Add enum validation middleware** to reject invalid enum values:

```csharp
public class ValidateEnumParametersFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var param in context.ActionArguments)
        {
            if (param.Value is EventCategory category)
            {
                if (!Enum.IsDefined(typeof(EventCategory), category))
                {
                    context.Result = new BadRequestObjectResult(
                        $"Invalid EventCategory value: {(int)category}");
                }
            }
        }
    }
}
```

---

## Timeline for Fix

**Effort Estimate**: 2-3 hours
- Backend enum update: 15 minutes
- Migration creation: 30 minutes
- Testing (unit + integration): 1 hour
- Deployment + verification: 30 minutes
- Documentation: 30 minutes

**Risk**: Low - Adding enum values is non-breaking

---

## Lessons Learned

1. **Enum synchronization is critical** - Backend and frontend enums must stay in sync
2. **Database reference data must match enums** - Seeding scripts should derive from enum definitions
3. **Validate enum values at API boundary** - Don't accept undefined enum values silently
4. **CI/CD should validate enum consistency** - Automated checks prevent drift
5. **Documentation doesn't replace code** - EVENTS_API_SPECIFICATION.md showed 12 categories, but backend only had 8

---

## Verification Steps

After implementing fix:

1. ✅ Run backend tests: `dotnet test`
2. ✅ Run database migration: `dotnet ef database update`
3. ✅ Verify reference_values table has 12 EventCategory entries
4. ✅ Test API: `GET /api/events?category=9` (Festival)
5. ✅ Test frontend: Select "Festival" filter on Events page
6. ✅ Create test event with Festival category
7. ✅ Verify event appears when filtering by Festival

---

## Conclusion

This is a **Database Data Missing / Enum Mismatch** issue caused by incomplete implementation of the EventCategory expansion. The frontend and documentation show 12 categories, but the backend only supports 8. The fix is straightforward: add the 4 missing categories to the backend enum and ensure the reference_values table is synchronized.

**Recommended Action**: Implement **Option 1** (Add Missing Categories to Backend Enum) immediately.
