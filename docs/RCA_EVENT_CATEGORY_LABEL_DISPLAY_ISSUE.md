# Root Cause Analysis: Event Category Label Display Issue

**Date**: 2026-02-12
**Issue**: Event category labels (e.g., "Entertainment", "Business") not displaying on `/events` page
**Reported By**: User observed only "Festival" label displayed on "Sinhala and Tamil New Year 2026" event
**Severity**: HIGH - Affects user experience and event discoverability

---

## Executive Summary

Event category labels are not displaying on the events listing page due to a **type mismatch** between backend API serialization (strings) and frontend type expectations (numeric enum values). The backend serializes enums as strings (e.g., `"Entertainment"`), but the frontend creates a category label lookup table with numeric keys (e.g., `7: "Entertainment"`), resulting in failed lookups and undefined values.

---

## Problem Statement

### Observed Behavior
- **Events page** (`/events`): Only "Sinhala and Tamil New Year 2026" displays its category label "Festival"
- **Other events**: No category labels displayed (Monthly Dana, Cleveland Talent Show, NorthEastSL event all missing labels)
- **Expected**: ALL events should display their category labels as badges

### User-Facing Impact
- Reduced event discoverability (users can't filter by category visually)
- Inconsistent UI (some events have badges, others don't)
- Loss of categorical context for event browsing

---

## Investigation Process

### Phase 1: Initial Hypotheses (INCORRECT)
1. ❌ **Hypothesis 1**: Reference data incomplete/inactive
   - Disproved: User's database query showed 10 active EventType records
2. ❌ **Hypothesis 2**: Missing database records (Charity, Entertainment)
   - Disproved: User clarified this was a separate issue

### Phase 2: Deep Dive Investigation

#### Step 1: API Response Analysis
Tested staging API endpoint:
```bash
curl 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events?pageSize=2'
```

**Result**: Backend serializes `EventCategory` enum as **STRING values**:
```json
{
  "id": "5fbcea92-bd5b-486f-9eab-1c4ee0146307",
  "title": "Sinhala Drama Performance: 'Maname'",
  "category": "Entertainment",  ← STRING, not integer
  "status": "Completed"
}
```

#### Step 2: Reference Data API Analysis
```bash
curl 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/reference-data?types=EventCategory'
```

**Result**: Returns 12 EventCategory records with correct integer-to-name mappings:
```json
[
  {"enumType":"EventCategory","code":"Religious","intValue":0,"name":"Religious"},
  {"enumType":"EventCategory","code":"Cultural","intValue":1,"name":"Cultural"},
  ...
  {"enumType":"EventCategory","code":"Entertainment","intValue":7,"name":"Entertainment"},
  {"enumType":"EventCategory","code":"Festival","intValue":9,"name":"Festival"},
  ...
]
```

#### Step 3: Database Analysis
Database contains **TWO DIFFERENT enum_types**:
1. **EventType** (10 records): intValue 0-9, Festival=7
2. **EventCategory** (12 records): intValue 0-11, Entertainment=7, Festival=9

This is expected - EventType is legacy/deprecated, EventCategory is current.

---

## Root Cause

### The Type Mismatch

#### Backend Configuration
**File**: [src/LankaConnect.API/Program.cs:52](../src/LankaConnect.API/Program.cs#L52)
```csharp
// Enable string-to-enum conversion for both regular and nullable enums
options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
```
✅ **Result**: Backend serializes ALL enums as **strings** (e.g., `"Entertainment"`)

#### Frontend Type Definition
**File**: [web/src/infrastructure/api/types/events.types.ts:281](../web/src/infrastructure/api/types/events.types.ts#L281)
```typescript
export interface EventDto {
  // ...
  category: EventCategory;  // TypeScript expects numeric enum (0-11)
  // ...
}

export enum EventCategory {
  Religious = 0,
  Cultural = 1,
  // ...
  Entertainment = 7,
  // ...
  Festival = 9,
  // ...
}
```
❌ **Problem**: TypeScript type expects **numeric enum values**, but runtime receives **strings**

#### Frontend Mapping Logic
**File**: [web/src/app/events/page.tsx:182-189](../web/src/app/events/page.tsx#L182-L189)
```typescript
const categoryLabels = useMemo(() => {
  if (!categories) return {} as Record<EventCategory, string>;
  const labels: Record<EventCategory, string> = {} as Record<EventCategory, string>;
  categories.forEach(cat => {
    labels[cat.intValue as EventCategory] = cat.name;  // Uses intValue as key
  });
  return labels;
}, [categories]);

// Creates mapping:
// { 0: "Religious", 1: "Cultural", ..., 7: "Entertainment", 9: "Festival", ... }
```

#### Frontend Display Logic
**File**: [web/src/app/events/page.tsx:501](../web/src/app/events/page.tsx#L501)
```typescript
<Badge variant="outline" className="px-2 py-1 text-xs">
  {categoryLabels[event.category]}  // Looks up STRING key in NUMERIC-keyed object
</Badge>

// Evaluation:
// event.category = "Entertainment" (string from API)
// categoryLabels["Entertainment"] = undefined ← LOOKUP FAILS!
// Should be: categoryLabels[7] = "Entertainment"
```

---

## Why Only Festival Might Show (Hypothesis)

**Possible Explanations**:
1. **TypeScript enum reverse mapping collision**: TypeScript enums create bidirectional mappings. If "Festival" coincidentally matches both a key and value, it might resolve
2. **Hardcoded fallback**: There might be special handling for Festival category somewhere
3. **User's observation was about EventBadge system**: The "Festival" badge might be from the Badge Management System (Phase 6A.25), not the category label system

**Requires Further Investigation**: Check if "Sinhala and Tamil New Year 2026" event has an EventBadge assigned with "Festival" text.

---

## Impact Assessment

### Affected Components
- ✅ **Events Listing Page** (`/events`): Category labels not displayed
- ✅ **Event Detail Page**: Likely affected (if it uses same mapping)
- ✅ **Event Filtering**: Users can't visually identify event categories
- ✅ **Search Results**: Category context lost

### Unaffected Components
- ✅ **Event Creation/Edit Forms**: Uses dropdown populated by reference data API (works correctly)
- ✅ **Backend Logic**: EventCategory enum stored and queried correctly as integers
- ✅ **Database Integrity**: reference_values table is correct and complete

---

## Proposed Fix

### Option 1: Change Backend to Serialize Enums as Integers (BREAKING CHANGE)
**NOT RECOMMENDED** - Would break existing frontend code that expects string values

### Option 2: Update Frontend Mapping to Handle String Keys (RECOMMENDED)

#### Fix Location: [web/src/app/events/page.tsx:182-189](../web/src/app/events/page.tsx#L182-L189)

**Current Code**:
```typescript
const categoryLabels = useMemo(() => {
  if (!categories) return {} as Record<EventCategory, string>;
  const labels: Record<EventCategory, string> = {} as Record<EventCategory, string>;
  categories.forEach(cat => {
    labels[cat.intValue as EventCategory] = cat.name;  // Numeric keys only
  });
  return labels;
}, [categories]);
```

**Fixed Code**:
```typescript
const categoryLabels = useMemo(() => {
  if (!categories) return {} as Record<string, string>;  // Change to string keys
  const labels: Record<string, string> = {};
  categories.forEach(cat => {
    // Create mapping using BOTH intValue AND code (string name)
    labels[cat.intValue.toString()] = cat.name;  // For integer lookups (future-proof)
    labels[cat.code] = cat.name;                  // For string lookups (current API behavior)
  });
  return labels;
}, [categories]);

// Results in:
// {
//   "0": "Religious", "Religious": "Religious",
//   "1": "Cultural", "Cultural": "Cultural",
//   ...
//   "7": "Entertainment", "Entertainment": "Entertainment",
//   "9": "Festival", "Festival": "Festival",
//   ...
// }
```

**Benefits**:
- ✅ Works with current backend string serialization
- ✅ Future-proof if backend changes to integer serialization
- ✅ No breaking changes to API contract
- ✅ Minimal code change (single function)

### Option 3: Fix TypeScript Type Definition

Update EventDto type to reflect actual runtime behavior:
```typescript
export interface EventDto {
  // ...
  category: string;  // Change from EventCategory enum to string
  // ...
}
```

**Pros**: Type safety matches runtime reality
**Cons**: Loses TypeScript enum benefits (autocomplete, type checking)

---

## Recommended Solution

**Implement Option 2 (Frontend Mapping Fix)**

### Implementation Plan

#### Step 1: Update Frontend Mapping
- **File**: `web/src/app/events/page.tsx`
- **Lines**: 182-189
- **Action**: Change categoryLabels to use string keys and map both intValue and code

#### Step 2: Verify All Category Label Usages
Search for all places that use category labels:
```bash
grep -r "categoryLabels\[" web/src/
```

Ensure all usages work with the new string-keyed mapping.

#### Step 3: Test in Staging
1. Deploy updated frontend to staging
2. Verify all events display category labels correctly
3. Test edge cases: all 12 categories, different event statuses

#### Step 4: Update TypeScript Types (Optional but recommended)
Update EventDto type to use `category: string` to match runtime behavior and prevent future confusion.

---

## Testing Strategy

### Test Cases
1. ✅ **All 12 categories display labels**: Create/verify events for each category
2. ✅ **Festival label shows**: Verify "Sinhala and Tamil New Year 2026" still displays
3. ✅ **Entertainment label shows**: Verify "Cleveland Talent Show" displays
4. ✅ **Business label shows**: Verify "Sri Lankan Tech Professionals Meetup" displays
5. ✅ **Label styling consistent**: All badges use same styling
6. ✅ **No React errors**: Check browser console for errors

### Verification Commands
```bash
# Test events API
curl 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events?pageSize=20' | grep -o '"category":"[^"]*"' | sort | uniq -c

# Test reference data API
curl 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/reference-data?types=EventCategory' | python -m json.tool
```

---

## Related Issues

### Issue: Database Has Two enum_types (EventType vs EventCategory)
**Status**: SEPARATE ISSUE (not root cause)
**Description**: Database contains both EventType (10 records, legacy) and EventCategory (12 records, current)
**Impact**: Low - Frontend correctly queries EventCategory, backend uses EventCategory enum
**Recommendation**: Create cleanup migration to remove EventType records if confirmed unused

### Issue: Dropdown Shows 12 Categories but Database Query Showed 10
**Resolution**: User's database query used `enum_type = 'EventType'` (legacy). Frontend correctly uses `enum_type = 'EventCategory'` which returns all 12 categories.

---

## Prevention Strategy

### Type Safety Improvements
1. ✅ Add runtime validation for API responses (validate category is valid enum string)
2. ✅ Use Zod or similar schema validation library
3. ✅ Add unit tests for category label mapping logic

### Documentation Updates
1. ✅ Document enum serialization strategy in ARCHITECTURE.md
2. ✅ Add comment in Program.cs explaining JsonStringEnumConverter impact
3. ✅ Update API documentation (Swagger) to show enums serialize as strings

### Code Review Checklist
- [ ] Verify enum serialization strategy matches frontend expectations
- [ ] Check for type mismatches between API types and runtime values
- [ ] Test mapping logic with actual API responses, not mock data

---

## Timeline

| Phase | Duration | Status |
|---|---|---|
| Investigation | 2 hours | ✅ COMPLETED |
| RCA Documentation | 1 hour | ✅ COMPLETED |
| Fix Implementation | 30 minutes | 🔄 PENDING APPROVAL |
| Testing | 1 hour | 🔄 PENDING |
| Deployment | 15 minutes | 🔄 PENDING |

---

## Conclusion

The root cause is a **type mismatch** between backend enum serialization (strings) and frontend mapping logic (numeric keys). The fix is straightforward: update the frontend category label mapping to use string keys that match the API response format. This is a 5-line code change with minimal risk and immediate impact.

**Recommended Action**: Proceed with Option 2 (Frontend Mapping Fix) after user approval.
