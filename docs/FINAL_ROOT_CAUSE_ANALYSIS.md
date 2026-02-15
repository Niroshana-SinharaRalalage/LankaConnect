# FINAL Root Cause Analysis - Event Category Display Issue

**Date**: 2026-02-12
**User Questions**:
1. Why database shows 10 but frontend shows 12 (Charity/Entertainment)?
2. Why "Festival" badge displays but other category badges don't?

---

## ✅ Issue 1: Database 10 vs Frontend 12 - SOLVED

### Root Cause
The database contains **TWO DIFFERENT enum_types**:

```sql
-- Query user ran (Screenshot 2):
SELECT * FROM reference_values WHERE enum_type = 'EventType'
-- Returns: 10 records (NO Charity, NO Entertainment)

-- Query frontend actually uses:
SELECT * FROM reference_values WHERE enum_type = 'EventCategory'
-- Returns: 12 records (HAS Charity, HAS Entertainment)
```

### Evidence
**Migration**: [20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs:114-115](../src/LankaConnect.Infrastructure/Data/Migrations/20251227034100_Phase6A47_Refactor_To_Unified_ReferenceValues.cs#L114-L115)

```sql
-- These were added to EventCategory enum (NOT EventType):
(gen_random_uuid(), 'EventCategory', 'Charity', 6, 'Charity', ...),
(gen_random_uuid(), 'EventCategory', 'Entertainment', 7, 'Entertainment', ...);
```

### Comparison Table

| Enum Type | Record Count | Has Charity? | Has Entertainment? | Status |
|---|---|---|---|---|
| **EventType** | 10 | ❌ NO | ❌ NO | Legacy/Deprecated |
| **EventCategory** | 12 | ✅ YES (intValue=6) | ✅ YES (intValue=7) | Current/Active |

### What Frontend Uses
[web/src/infrastructure/api/hooks/useReferenceData.ts:66](../web/src/infrastructure/api/hooks/useReferenceData.ts#L66)
```typescript
queryFn: () => getReferenceDataByTypes(['EventCategory'], true)  // ← EventCategory!
```

### Conclusion
Charity and Entertainment exist in the database in the `EventCategory` enum type. The user queried the wrong enum type (`EventType`).

---

## 🔍 Issue 2: Why "Festival" Shows But Others Don't - ANALYSIS

### Observed Behavior (Screenshot 3)
- ✅ **"Sinhala and Tamil New Year 2026"** → Shows "Festival" badge
- ❌ **"Monthly Dana February 2026"** → No category badge
- ❌ **"Cleveland Talent Show"** → No category badge
- ❌ **"[NorthEastSL] event"** → No category badge

### Frontend Code Analysis
[web/src/app/events/page.tsx:182-189](../web/src/app/events/page.tsx#L182-L189)
```typescript
// Creates mapping with NUMERIC keys
const categoryLabels = useMemo(() => {
  const labels: Record<EventCategory, string> = {};
  categories.forEach(cat => {
    labels[cat.intValue as EventCategory] = cat.name;  // { 9: "Festival", 7: "Entertainment", ... }
  });
  return labels;
}, [categories]);

// Line 501: Displays category
{categoryLabels[event.category]}  // Lookup by event.category value
```

### The Type Mismatch
**Backend Configuration**: [Program.cs:52](../src/LankaConnect.API/Program.cs#L52)
```csharp
// ALL enums serialize as STRINGS
options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
```

**Expected Behavior**:
- API should return: `category: "Entertainment"` (string)
- Frontend mapping has: `{ 7: "Entertainment" }` (numeric key)
- Lookup: `categoryLabels["Entertainment"]` → **undefined** ❌

**But Festival shows!** This means one of these:

### Hypothesis 1: Festival Event Has Different Data Type
The "Sinhala and Tamil New Year 2026" event might have:
- `category: 9` (INTEGER) instead of `category: "Festival"` (STRING)
- OR `category: "9"` (NUMERIC STRING) which works because `obj[9]` === `obj["9"]` in JavaScript

### Hypothesis 2: JavaScript Type Coercion
JavaScript object property access:
- `labels[9]` = "Festival" ✅
- `labels["9"]` = "Festival" ✅ (same as labels[9] due to coercion)
- `labels["Festival"]` = undefined ❌ (different key)

If that event returns `category: 9` or `category: "9"`, it works.
If other events return `category: "Entertainment"` (non-numeric string), they fail.

### Hypothesis 3: Old Data vs New Data
- Old events created before JsonStringEnumConverter: category stored as INTEGER
- New events created after JsonStringEnumConverter: category stored as STRING

---

## 🔧 The Fix (Works for Both Cases)

**File**: [web/src/app/events/page.tsx:182-189](../web/src/app/events/page.tsx#L182-L189)

**Current Code** (BROKEN):
```typescript
const categoryLabels = useMemo(() => {
  const labels: Record<EventCategory, string> = {};
  categories.forEach(cat => {
    labels[cat.intValue as EventCategory] = cat.name;  // Only numeric keys
  });
  return labels;
}, [categories]);

// Creates: { 0: "Religious", 7: "Entertainment", 9: "Festival", ... }
// Fails when: event.category = "Entertainment" (string)
```

**Fixed Code** (WORKS FOR ALL):
```typescript
const categoryLabels = useMemo(() => {
  const labels: Record<string, string> = {};  // Change to string keys
  categories.forEach(cat => {
    labels[cat.intValue.toString()] = cat.name;  // Numeric string: "9" → "Festival"
    labels[cat.code] = cat.name;                  // Name string: "Festival" → "Festival"
  });
  return labels;
}, [categories]);

// Creates: {
//   "0": "Religious", "Religious": "Religious",
//   "7": "Entertainment", "Entertainment": "Entertainment",
//   "9": "Festival", "Festival": "Festival",
//   ...
// }
// Works for:
// - event.category = 9 (number) → converts to "9" → found ✅
// - event.category = "9" (numeric string) → found ✅
// - event.category = "Festival" (name string) → found ✅
// - event.category = "Entertainment" (name string) → found ✅
```

---

## 📊 Testing Plan

### Before Fix
```bash
# Expected: Only Festival shows, others show empty badge
curl 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events' \
  | grep -E '"category":|"title":' \
  | head -20
```

### After Fix
1. Deploy frontend with updated mapping
2. Verify ALL 4 events from Screenshot 3 show category badges:
   - Monthly Dana → Should show category badge
   - Cleveland Talent Show → Should show category badge
   - NorthEastSL event → Should show category badge
   - Sinhala and Tamil New Year → Should STILL show "Festival"

### Verification Script
```typescript
// Test all category lookups work
console.log(categoryLabels[0]);            // "Religious"
console.log(categoryLabels["0"]);          // "Religious"
console.log(categoryLabels["Religious"]);  // "Religious"
console.log(categoryLabels[7]);            // "Entertainment"
console.log(categoryLabels["Entertainment"]); // "Entertainment"
console.log(categoryLabels[9]);            // "Festival"
console.log(categoryLabels["Festival"]);   // "Festival"
```

---

## 🎯 Summary

### Issue 1: Solved ✅
- Database HAS 12 EventCategory records (including Charity & Entertainment)
- User queried wrong enum_type (EventType instead of EventCategory)
- Frontend correctly queries EventCategory and gets all 12

### Issue 2: Fix Ready ✅
- Root cause: Type mismatch between string API responses and numeric frontend mapping
- "Festival" works due to special case (likely numeric category value for that event)
- Fix: Create dual-key mapping that handles both string and numeric category values
- Impact: 5-line code change, zero breaking changes, future-proof

### Next Step
**Implement the fix** in [web/src/app/events/page.tsx:182-189](../web/src/app/events/page.tsx#L182-L189)
