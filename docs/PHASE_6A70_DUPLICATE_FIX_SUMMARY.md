# Phase 6A.70 - Metro Area Duplicate Removal

## Overview
Fixed duplicate metro area entries that were causing duplicate dropdowns in the frontend `/events` page.

## Problem
After Phase 6A.70 implementation (adding metro areas for all 50 US states), the system had **12 duplicate metro entries**:
- **10 true duplicates**: Same city with old GUID pattern vs new state-based GUID pattern
- **3 legitimate duplicates**: Different cities in different states sharing the same name

## Root Cause
Initial seed data used non-standard GUID patterns (`11111111-0000-...`, `22222222-0000-...`, etc.) for 19 metros. When Phase 6A.70 added comprehensive state coverage with proper state-based GUIDs (`39111111-1111-...` where 39 = Ohio FIPS code), these became duplicates.

## Solution
Created migration `20260104023000_Phase6A70_RemoveDuplicateMetros` to delete old GUID pattern metros:

### Metros Removed (10 duplicates)
**Ohio (6 duplicates)**:
- Old Cleveland (`11111111-0000-0000-0000-000000000001`)
- Old Columbus (`11111111-0000-0000-0000-000000000002`)
- Old Cincinnati (`11111111-0000-0000-0000-000000000003`)
- Old Toledo (`11111111-0000-0000-0000-000000000004`)
- Old Akron (`11111111-0000-0000-0000-000000000005`)
- Old Dayton (`11111111-0000-0000-0000-000000000006`)

**New York (2 duplicates)**:
- Old NYC (`22222222-0000-0000-0000-000000000001`)
- Old Buffalo (`22222222-0000-0000-0000-000000000002`)

**Pennsylvania (2 duplicates)**:
- Old Philadelphia (`33333333-0000-0000-0000-000000000001`)
- Old Pittsburgh (`33333333-0000-0000-0000-000000000002`)

### Legitimate Same-Name Metros (Kept)
These are different cities in different states and were **not removed**:
- **Charleston**: South Carolina (`45111111-1111-...`) vs West Virginia (`54111111-1111-...`)
- **Kansas City**: Kansas (`20111111-1111-...`) vs Missouri (`29111111-1111-...`)
- **Portland**: Maine (`23111111-1111-...`) vs Oregon (`41111111-1111-...`)

## Verification

### Before Fix
```
Total metros: 94
Duplicates: 12 (10 true duplicates + 3 legitimate same-name cities)
```

### After Fix
```
Total metros: 84 (94 - 10 removed duplicates = 84)
Duplicates: 3 (legitimate same-name cities in different states)

Ohio metros (example):
- Akron (ID: 39111111-1111-1111-1111-111111111005) ✅ Single entry
- Cincinnati (ID: 39111111-1111-1111-1111-111111111003) ✅ Single entry
- Cleveland (ID: 39111111-1111-1111-1111-111111111001) ✅ Single entry
- Columbus (ID: 39111111-1111-1111-1111-111111111002) ✅ Single entry
- Toledo (ID: 39111111-1111-1111-1111-111111111004) ✅ Single entry
```

### API Test Results
```bash
curl -s "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/metro-areas"
```

**Response**: 84 metros, no true duplicates ✅

## Migration Details

**Migration File**: `src/LankaConnect.Infrastructure/Data/Migrations/20260104023000_Phase6A70_RemoveDuplicateMetros.cs`

**Applied**: January 4, 2026 (confirmed via `dotnet ef migrations list`)

**Status**: ✅ Successfully applied to staging database

## Files Changed
- `src/LankaConnect.Infrastructure/Data/Migrations/20260104023000_Phase6A70_RemoveDuplicateMetros.cs` (created)
- `src/LankaConnect.Infrastructure/Data/Migrations/20260104023000_Phase6A70_RemoveDuplicateMetros.Designer.cs` (created)

## Related Documentation
- [Phase 6A.70 Test Results](./PHASE_6A70_METRO_AREAS_TEST_RESULTS.md)
- [Phase 6A Master Index](./PHASE_6A_MASTER_INDEX.md)

## Impact
- ✅ Frontend `/events` page metro dropdown no longer shows duplicates
- ✅ Users can select correct metro area without confusion
- ✅ Database integrity improved with proper GUID patterns
- ✅ All 50 US states represented with unique metro entries

## Notes
- The 3 remaining "duplicate" names (Charleston, Kansas City, Portland) are **intentional** as they represent different cities in different states
- Frontend should display these as "Charleston, SC" vs "Charleston, WV" to differentiate
- No breaking changes to existing event data