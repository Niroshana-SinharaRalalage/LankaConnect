# Enum Type Comparison: EventType vs EventCategory

**Date**: 2026-02-12

## The Confusion

Your database query: `SELECT * FROM reference_values WHERE enum_type = 'EventType'`
- Returned: **10 records**

Frontend dropdown shows: **12 categories**

## Root Cause: TWO Different Enum Types Exist

The database contains **TWO DIFFERENT enum_types** for event categorization:

### 1. EventType (LEGACY - 10 records)
```sql
SELECT * FROM reference_values WHERE enum_type = 'EventType' ORDER BY int_value;
```

| int_value | code | name |
|---|---|---|
| 0 | Community | Community |
| 1 | Religious | Religious |
| 2 | Cultural | Cultural |
| 3 | Educational | Educational |
| 4 | Social | Social |
| 5 | Business | Business |
| 6 | Workshop | Workshop |
| **7** | **Festival** | **Festival** |
| 8 | Ceremony | Ceremony |
| 9 | Celebration | Celebration |

**Status**: Legacy/deprecated enum type
**Missing**: Charity, Entertainment

---

### 2. EventCategory (CURRENT - 12 records)
```sql
SELECT * FROM reference_values WHERE enum_type = 'EventCategory' ORDER BY int_value;
```

| int_value | code | name |
|---|---|---|
| 0 | Religious | Religious |
| 1 | Cultural | Cultural |
| 2 | Community | Community |
| 3 | Educational | Educational |
| 4 | Social | Social |
| 5 | Business | Business |
| 6 | **Charity** | **Charity** |
| 7 | **Entertainment** | **Entertainment** |
| 8 | Workshop | Workshop |
| **9** | **Festival** | **Festival** |
| 10 | Ceremony | Ceremony |
| 11 | Celebration | Celebration |

**Status**: Current/active enum type used by frontend and backend
**Complete**: All 12 categories present

---

## What Frontend Uses

**File**: [web/src/infrastructure/api/hooks/useReferenceData.ts:66](../web/src/infrastructure/api/hooks/useReferenceData.ts#L66)

```typescript
export function useEventCategories() {
  return useQuery<ReferenceValue[], Error>({
    queryKey: referenceDataKeys.byTypes(['EventCategory'], true),  // ← EventCategory, NOT EventType!
    queryFn: () => getReferenceDataByTypes(['EventCategory'], true),
    staleTime: 1000 * 60 * 60,
  });
}
```

**API Endpoint Called**:
```
GET /api/reference-data?types=EventCategory&activeOnly=true
```

**Result**: Returns **12 records** from EventCategory enum type

---

## Key Differences

| Aspect | EventType | EventCategory |
|---|---|---|
| **Record Count** | 10 | 12 |
| **Festival intValue** | 7 | 9 |
| **Entertainment intValue** | N/A | 7 |
| **Charity intValue** | N/A | 6 |
| **Status** | Legacy | Current |
| **Used By** | Nothing | Frontend + Backend |

---

## Why This Matters

**Your query** looked at EventType (10 records, Festival=7)
**Frontend uses** EventCategory (12 records, Festival=9, Entertainment=7)

This is why you saw 10 records but the dropdown shows 12!

---

## Verified by API Test

```bash
# EventType (your query) - 10 records
curl 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/reference-data?types=EventType&activeOnly=true'

# EventCategory (frontend uses) - 12 records
curl 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/reference-data?types=EventCategory&activeOnly=true'
```

Both endpoints work, both return valid data, but they're **different enum types**.

---

## Recommendation

**Query the correct enum type**:
```sql
-- ✅ CORRECT - What frontend uses
SELECT * FROM reference_data.reference_values
WHERE enum_type = 'EventCategory'  -- NOT 'EventType'
ORDER BY int_value;
```

This will return all 12 categories that match the frontend dropdown.
