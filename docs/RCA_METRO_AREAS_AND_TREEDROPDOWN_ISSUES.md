# Root Cause Analysis: Production Metro Areas Gap & TreeDropdown Checkbox Bug

**Date**: 2026-02-11
**Severity**: P1 (Metro Areas) / P3 (TreeDropdown)
**Author**: System Architect
**Phase**: 6A.104 (Metro Area Data Fix) + 6A.105 (TreeDropdown UX Fix)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Issue 1: Production Missing Metro Areas](#2-issue-1-production-missing-metro-areas)
3. [Issue 2: TreeDropdown Parent-Child Checkbox Bug](#3-issue-2-treedropdown-parent-child-checkbox-bug)
4. [Fix Plan](#4-fix-plan)
5. [Risk Assessment](#5-risk-assessment)
6. [Deployment & Verification](#6-deployment--verification)
7. [Architectural Lessons & Prevention](#7-architectural-lessons--prevention)

---

## 1. Executive Summary

Two production issues affect the metro area feature set:

| Issue | Classification | Severity | Impact |
|-------|---------------|----------|--------|
| Missing metro areas in production DB | **Configuration + Database** | P1 - Critical | Users in 43 states cannot select their location. Events, Registration, Newsletter all affected. |
| TreeDropdown parent uncheck bug | **Frontend** | P3 - Minor UX | Users cannot uncheck a visually auto-checked parent node. Workaround: uncheck children individually. |

**Root Cause Summary (Issue 1)**: The data for 43 US states was added to a runtime seeder (`MetroAreaSeeder.cs`) during Phase 6A.70, but was NEVER captured in an EF Core migration. The runtime seeder only executes in `Development` mode (`Program.cs` line 206), and the admin seed endpoint is blocked in `Production` (line 63 of `AdminController.cs`). The EF migration generated during Phase 6A.70 (`Phase6A70_AddMetroAreasFor5States`) contained ONLY auto-generated `reference_values` timestamp updates -- zero metro area INSERT statements.

**Root Cause Summary (Issue 2)**: The `toggleSelection` function uses `newSelected.has(nodeId)` to decide check vs. uncheck. But for parent nodes, the parent ID is never added to `selectedIds` (only children are). When all children are selected, the parent visually appears checked (via render-time derivation on line 163), but clicking it enters the ELSE/check branch (not the IF/uncheck branch), creating a no-op because all children are already in the set.

---

## 2. Issue 1: Production Missing Metro Areas

### 2.1 Root Cause Classification

**Primary**: Configuration Defect (data pathway gated behind environment check)
**Secondary**: Process Defect (missing migration for data changes)

### 2.2 Observed State

| Environment | Table: `events.metro_areas` | States | Structure |
|-------------|----------------------------|--------|-----------|
| Production  | 22 rows | 7 (AK, AL, AZ, CA, IL, NY, TX) | 7 state-level "All [State]" + 15 city-level |
| Staging     | 84 rows | 50 | 84 city-level ONLY (no state-level entries) |

**Key observation**: Staging and production have **structurally different data**. Production has `is_state_level_area = true` rows for 7 states that do NOT exist in staging. This is because:
- The initial EF migration (`20251112204434_SeedMetroAreasReferenceData`) inserted BOTH state-level and city-level rows for 7 states
- The runtime seeder (`MetroAreaSeeder.cs`) only creates city-level rows (`isStateLevelArea: false`) for all 50 states
- In staging, the seeder ran (Development mode) and added the 43 missing states as city-only data
- The seeder uses `ON CONFLICT` / existing-ID checks, so the 7 original states' city metros were not duplicated
- But the seeder never creates state-level metros, so those remain from the original migration only

### 2.3 Three Data Pathways Analysis

```
Path 1: EF Core Migration (20251112204434_SeedMetroAreasReferenceData)
  - Runs: ALL environments (via CI/CD pipeline or startup MigrateAsync)
  - Data: 22 rows (7 state-level + 15 city-level) for 7 states only
  - Uses: ON CONFLICT (id) DO NOTHING
  - Status: Ran successfully in both staging and production

Path 2: Runtime Seeder (MetroAreaSeeder.SeedAsync)
  - Called from: DbInitializer.SeedAsync() -> Program.cs
  - Gate: if (app.Environment.IsDevelopment()) { ... } [Program.cs line 206]
  - Data: 74 city-level metros for ALL 50 states (no state-level rows)
  - Status: Runs in Development ONLY -> never ran in Production

Path 3: Admin Seed Endpoint (POST /api/Admin/seed?seedType=metroareas)
  - Gate: !_environment.IsDevelopment() && !_environment.IsStaging() [AdminController.cs line 63]
  - Returns: 403 Forbidden in Production
  - Status: Blocked in Production
```

### 2.4 Contributing Factors

1. **Phase 6A.70 migration did not capture data**: When `dotnet ef migrations add` was run, EF Core detected no changes to the `MetroArea` entity model (the entity itself did not change). The migration auto-generated only `reference_values` timestamp diffs. The developer likely assumed the runtime seeder would handle production data, not realizing the environment gate.

2. **No integration test for metro area count in production**: There is no post-deployment check that validates "expected number of metro areas >= 84" after a deploy.

3. **Misleading migration name**: `Phase6A70_AddMetroAreasFor5States` implies data insertion, but the migration body contains zero INSERT statements. This masked the gap during code review.

4. **Dual data management strategy without parity**: Using BOTH EF migrations (for initial data) AND runtime seeders (for expanded data) without ensuring both paths are reachable in all environments.

5. **Environment gate too broad**: `Program.cs` line 206 gates the ENTIRE `DbInitializer.SeedAsync()` call behind `IsDevelopment()`, not just the destructive operations. Metro area seeding is idempotent and safe for all environments.

### 2.5 Timeline

| Date | Event | Impact |
|------|-------|--------|
| 2025-11-10 | `CreateMetroAreasTable` migration created | Table schema established |
| 2025-11-12 | `SeedMetroAreasReferenceData` migration seeded 7 states (22 rows) | Production received initial 22 rows |
| ~2026-01-03 | Phase 6A.70: `MetroAreaSeeder.cs` expanded to 50 states (74 city rows) | Runtime seeder updated, but no migration |
| 2026-01-03 | `Phase6A70_AddMetroAreasFor5States` migration generated | **Contained NO metro area data** - only reference_values timestamps |
| 2026-01-04 | `Phase6A70_RemoveDuplicateMetros` migration applied | Cleanup migration |
| 2026-01-03..2026-02-11 | **39 days** with production showing only 7 states | Users in 43 states unable to select location |

### 2.6 Impact Assessment

**Direct User Impact**:
- Location dropdown (Events page filter, Registration page, Newsletter subscription, Profile preferred metros) shows only 7 states
- Users in 43 states (e.g., Florida, Georgia, Ohio, Pennsylvania, Washington) cannot register preferred metro areas
- Events cannot be filtered by 43 states
- Newsletter metro targeting limited to 7 states

**Affected Components** (all fetch from `GET /api/metro-areas`):
1. `web/src/presentation/components/features/auth/MetroAreasSelector.tsx` - Registration
2. `web/src/presentation/components/features/profile/PreferredMetroAreasSection.tsx` - Profile
3. `web/src/presentation/components/features/newsletter/NewsletterMetroSelector.tsx` - Newsletter
4. `web/src/components/events/filters/LocationFilter.tsx` - Event filter
5. `web/src/presentation/components/features/newsletters/NewsletterForm.tsx` - Newsletter form

**Data Integrity**: No corruption. Missing data only -- additive fix is safe.

---

## 3. Issue 2: TreeDropdown Parent-Child Checkbox Bug

### 3.1 Root Cause Classification

**Primary**: Frontend Logic Defect

### 3.2 Detailed Bug Analysis

**File**: `web/src/presentation/components/ui/TreeDropdown.tsx`

The bug is a state inconsistency between the **selection model** (which parent IDs are in `selectedIds`) and the **visual model** (which checkboxes appear checked).

#### Selection Model (toggleSelection, lines 114-151):

```typescript
// When CHECKING a parent node:
if (hasChildren) {
  idsToAdd.push(...getAllChildIds(node));  // Adds children only
  // Parent ID is NOT added to selectedIds
}

// When UNCHECKING:
if (newSelected.has(nodeId)) {        // Checks if nodeId is in selectedIds
  newSelected.delete(nodeId);          // Would remove nodeId
  // ... remove children
}
```

#### Visual Model (renderTreeNode, lines 157-165):

```typescript
let isSelected = selectedIds.includes(node.id);    // false for parent
if (hasChildren && !isSelected) {
  const childIds = getAllChildIds(node);
  isSelected = childIds.every(childId => selectedIds.includes(childId));
  // Sets visual to true when all children selected
}
```

#### The Bug Sequence:

```
Step 1: User clicks parent "California" checkbox
  -> toggleSelection("CA") enters ELSE branch (checking)
  -> Adds all child IDs: [LA, SF, San Diego, Sacramento, Fresno, Inland Empire]
  -> selectedIds = {LA, SF, SD, SAC, FRE, IE}  (no "CA")
  -> Visual: CA checkbox shows checked (derived: all children in set)

Step 2: User clicks parent "California" checkbox again to uncheck
  -> toggleSelection("CA") checks: newSelected.has("CA")
  -> "CA" is NOT in selectedIds -> returns FALSE
  -> Enters ELSE branch AGAIN (checking logic, not unchecking)
  -> getAllChildIds returns [LA, SF, SD, SAC, FRE, IE]
  -> All already in Set -> Set.add() is no-op
  -> selectedIds unchanged -> visual unchanged
  -> BUG: Cannot uncheck the parent
```

#### Why This Only Manifests for "Manually-Completed Parents":

When a user directly clicks a parent, the parent adds all children and NEVER adds itself. The visual auto-check kicks in. But `toggleSelection` cannot detect that the visual is "checked" because it only looks at `selectedIds.has(nodeId)`, not the derived visual state.

This does NOT occur if a parent was initially checked via the checking branch because the parent ID IS included in the scenario where `hasChildren` is false (leaf nodes only). Parents never get their own ID in the set.

### 3.3 Impact Assessment

**Severity**: P3 - Minor UX Annoyance
**Workaround**: Users can uncheck individual child nodes to effectively uncheck the parent.
**Affected Pages**: All 5 pages that use TreeDropdown (same list as Issue 1 Section 2.6).

---

## 4. Fix Plan

### 4.1 Phase 6A.104: EF Core Migration for Missing Metro Areas

**Strategy**: Create a hand-written EF Core migration with raw SQL INSERT statements for the 62 missing metro areas. This ensures the data is applied via the standard CI/CD migration pipeline.

#### 4.1.1 Data Reconciliation

**Production currently has (22 rows):**
- 7 state-level rows: `is_state_level_area = true` for AK, AL, AZ, CA, IL, NY, TX
- 15 city-level rows: for those same 7 states

**Staging currently has (84 rows):**
- 84 city-level rows: `is_state_level_area = false` for all 50 states (0 state-level)

**Target state (both environments should converge to):**
- Keep existing 7 state-level rows in production (they serve a valid "All [State]" purpose)
- Add 43 new state-level rows for the missing states (so ALL 50 states have "All [State]")
- Add all missing city-level rows from the seeder

**Migration must INSERT exactly these missing rows:**

**Part A - 43 state-level "All [State]" rows for missing states** (AR, CO, CT, DE, FL, GA, HI, ID, IN, IA, KS, KY, LA, ME, MD, MA, MI, MN, MS, MO, MT, NE, NV, NH, NJ, NM, NC, ND, OH, OK, OR, PA, RI, SC, SD, TN, UT, VT, VA, WA, WV, WI, WY):
- These ensure every state has an "All [State]" parent node in the TreeDropdown
- Uses deterministic IDs following the pattern: `{FIPS}000000-0000-0000-0000-000000000001`

**Part B - 62 city-level rows for 43 missing states** (from MetroAreaSeeder.cs):
- These are the actual metro areas users select
- IDs must match exactly those in `MetroAreaSeeder.cs` for consistency
- Uses `ON CONFLICT (id) DO NOTHING` for idempotency

**Total new rows in production**: Up to 105 (43 state-level + 62 city-level)
- Some city-level rows already exist (the 15 for original 7 states) -- `ON CONFLICT` handles this
- Production post-migration target: ~127 rows (22 existing + 43 new state-level + 62 new city-level)

**Staging impact**: The `ON CONFLICT (id) DO NOTHING` clause means this migration is safe for staging too:
- The 62 city-level rows already exist in staging -> skipped
- The 43 new state-level rows do NOT exist in staging -> will be added
- The 7 original state-level rows do NOT exist in staging -> will be added
- Staging post-migration target: ~134 rows (84 existing + 50 new state-level)

**Data parity post-migration**: Both environments will have ~134 rows (50 state-level + 84 city-level), bringing them into alignment.

#### 4.1.2 Migration File Design

```
File: src/LankaConnect.Infrastructure/Data/Migrations/
      {timestamp}_Phase6A104_SeedAllMetroAreasProduction.cs
```

**Key design decisions:**

1. **Raw SQL with `ON CONFLICT (id) DO NOTHING`**: Idempotent. Safe to run multiple times. Safe for both staging and production regardless of current state.

2. **Deterministic GUIDs**: Follow existing convention where FIPS state code prefixes the GUID. All state-level rows use pattern `{FIPS}000000-0000-0000-0000-000000000001`. All city-level rows use pattern `{FIPS}111111-1111-1111-1111-111111111{NNN}`.

3. **Single SQL statement**: All INSERTs in one `VALUES` clause for atomicity.

4. **`Down()` method**: Will DELETE only the rows inserted by this migration using their known GUIDs, excluding the 22 already present from the original migration.

#### 4.1.3 Frontend Constants Alignment

The file `web/src/domain/constants/metroAreas.constants.ts` contains hardcoded metro area data including state-level "All [State]" entries for ALL states. However, since the frontend fetches from the API (`useMetroAreas` hook -> `GET /api/metro-areas`), the constants file is a BACKUP / reference only. The fix is purely backend (migration).

No frontend code changes needed for Issue 1.

---

### 4.2 Phase 6A.105: TreeDropdown Parent-Child Checkbox Fix

**Strategy**: Modify `toggleSelection()` to detect the derived visual state of parent nodes when deciding check vs. uncheck.

#### 4.2.1 Code Change

**File**: `web/src/presentation/components/ui/TreeDropdown.tsx`

**Change in `toggleSelection` function (lines 114-151):**

Current logic:
```typescript
if (newSelected.has(nodeId)) {
  // Unchecking
} else {
  // Checking
}
```

Fixed logic:
```typescript
// Determine if this node is effectively selected
// For parent nodes: visually checked = all children are in selectedIds
let isEffectivelySelected = newSelected.has(nodeId);
if (hasChildren && !isEffectivelySelected) {
  const childIds = getAllChildIds(node);
  if (childIds.length > 0) {
    isEffectivelySelected = childIds.every(childId => newSelected.has(childId));
  }
}

if (isEffectivelySelected) {
  // Unchecking: remove node ID (if present) and all children
  newSelected.delete(nodeId);
  if (hasChildren) {
    const childIds = getAllChildIds(node);
    childIds.forEach((id) => newSelected.delete(id));
  }
} else {
  // Checking: for parents add children, for leaves add self
  const idsToAdd: string[] = [];
  if (hasChildren) {
    idsToAdd.push(...getAllChildIds(node));
  } else {
    idsToAdd.push(nodeId);
  }

  if (maxSelections && newSelected.size + idsToAdd.length > maxSelections) {
    return;
  }

  idsToAdd.forEach((id) => newSelected.add(id));
}
```

This mirrors the same derived-check logic used in `renderTreeNode` (lines 157-165) to determine visual state, ensuring `toggleSelection` agrees with what the user sees.

#### 4.2.2 Test Plan

Create test file: `web/src/presentation/components/ui/__tests__/TreeDropdown.test.tsx`

**Test Cases:**

1. **Clicking parent checks all children**: Verify all child IDs appear in callback.
2. **Clicking checked parent unchecks all children**: Verify empty callback when parent was the only group.
3. **Manually selecting all children auto-checks parent visually**: Verify parent checkbox renders as checked.
4. **Clicking auto-checked parent unchecks all children**: **THE BUG FIX** - Verify clicking parent in "derived checked" state removes all children from selection.
5. **Partial child selection does not auto-check parent**: Verify parent checkbox remains unchecked.
6. **Unchecking one child of a checked parent unchecks parent visual**: Verify derived state recalculates.
7. **Max selection limit respected on parent check**: Verify no-op when limit would be exceeded.
8. **Leaf node check/uncheck**: Verify direct toggle works for nodes without children.

---

## 5. Risk Assessment

### 5.1 Phase 6A.104 (Metro Area Migration) Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Migration fails on production DB | Low | High | `ON CONFLICT (id) DO NOTHING` makes it idempotent. Test on staging first. |
| Duplicate metro area IDs cause constraint violations | Very Low | Medium | Using `ON CONFLICT DO NOTHING` explicitly. All GUIDs are deterministic and verified against seeder. |
| Existing user `preferred_metro_areas` references break | None | N/A | Additive change only. No existing rows modified or deleted. Foreign keys to existing IDs are unaffected. |
| Frontend breaks with new data | Very Low | Low | Frontend already handles dynamic metro area lists from API. More data just means more dropdown options. |
| Rollback needed | Low | Low | `Down()` method will DELETE only the rows added by this migration (using explicit ID list). |
| State-level rows in staging cause UI issues | Low | Low | Frontend TreeDropdown groups by state code and renders state-level rows as "All [State]" children. Tested in production for 7 states already. |

**Overall Risk: LOW**. This is an additive, idempotent data migration with no schema changes.

### 5.2 Phase 6A.105 (TreeDropdown Fix) Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Fix breaks existing parent-check behavior | Low | Medium | The fix only adds a derived-state check before the existing if/else. Existing "check parent -> add children" path is unchanged. |
| Performance impact from extra `every()` call | Very Low | Negligible | `getAllChildIds` returns at most ~6 items per state. `every()` on 6 items is O(1) effectively. |
| Breaking change for consuming components | Very Low | Low | `TreeDropdownProps` interface is unchanged. The fix is internal to the component. |
| Regression in multi-level trees | Low | Medium | Current usage is only 2-level (state -> city). Test with 2-level trees thoroughly. |

**Overall Risk: LOW**. Localized frontend change with well-defined test coverage.

---

## 6. Deployment & Verification

### 6.1 Pre-Deployment Checklist

- [ ] Phase 6A.104 migration SQL reviewed for correctness (all 50 state-level + all city-level rows)
- [ ] Migration GUIDs cross-referenced with `MetroAreaSeeder.cs` (exact match)
- [ ] Migration tested locally with `dotnet ef database update`
- [ ] `dotnet test` passes with migration applied
- [ ] TreeDropdown unit tests written and passing
- [ ] Frontend build succeeds (`npm run build` in `web/`)

### 6.2 Staging Verification

```bash
# 1. Deploy to staging
git push origin develop  # Triggers deploy-staging.yml

# 2. Verify migration applied - check metro area count
curl -s 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/metro-areas?activeOnly=true' \
  -H 'Authorization: Bearer <token>' | jq 'length'
# Expected: >= 130 (50 state-level + 84 city-level, minus any overlaps)

# 3. Verify all 50 states present
curl -s 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/metro-areas?activeOnly=true' \
  -H 'Authorization: Bearer <token>' | jq '[.[].state] | unique | length'
# Expected: 50

# 4. Verify state-level entries exist
curl -s 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/metro-areas?activeOnly=true' \
  -H 'Authorization: Bearer <token>' | jq '[.[] | select(.isStateLevelArea)] | length'
# Expected: 50

# 5. Manual UI test on staging
# - Navigate to Events page -> Location filter shows 50 states
# - Navigate to Register page -> Metro selector shows 50 states
# - Navigate to Newsletter -> Metro selector shows 50 states
# - Test TreeDropdown: check parent -> all children checked -> click parent again -> all unchecked
```

### 6.3 Production Verification

```bash
# Same API checks as staging, against production URL
# Verify metro_areas table count: ~127+ rows
# Verify all 50 unique states
# Verify 50 state-level entries
# Verify UI location dropdowns show all 50 states
```

### 6.4 Rollback Plan

**Phase 6A.104 (Migration)**:
```bash
# If needed, the Down() migration removes only the rows added
dotnet ef database update Phase6A103_AddEventImageToEmailTemplates \
  --project src/LankaConnect.Infrastructure
```

**Phase 6A.105 (TreeDropdown)**:
```bash
# Revert the single file change
git revert <commit-hash>
```

---

## 7. Architectural Lessons & Prevention

### 7.1 Root Cause Pattern: "Seeder Drift"

The fundamental issue is maintaining reference data in TWO places (EF migrations + runtime seeders) without a mechanism to ensure they stay synchronized. The environment gate (`IsDevelopment()`) created an invisible divergence between staging/production that accumulated over 39 days.

### 7.2 Prevention Recommendations

| Recommendation | Priority | Effort |
|---------------|----------|--------|
| **All reference data changes MUST go through EF Core migrations** - never rely solely on runtime seeders for production data | P1 | Low |
| **Add post-deployment health check** that validates metro area count >= expected minimum | P1 | Medium |
| **Remove environment gate on idempotent seeders** (or add a Staging/Production-safe seeder path for reference data) | P2 | Medium |
| **Migration naming convention enforcement**: Migration name must accurately describe the SQL operations, not just the intent | P2 | Low |
| **Code review checklist item**: "If a seeder was modified, was a corresponding migration created?" | P2 | Low |
| **Data parity dashboard**: Automated comparison of key reference table counts between staging and production | P3 | High |

### 7.3 Architectural Decision: Single Source of Truth for Metro Areas

Going forward, the canonical metro area data should live EXCLUSIVELY in EF Core migrations. The `MetroAreaSeeder.cs` runtime seeder should be retained only for local development convenience and should be marked with a comment:

```csharp
/// WARNING: This seeder is for local development only.
/// Production/Staging metro area data MUST be managed via EF Core migrations.
/// See: Phase6A104_SeedAllMetroAreasProduction migration.
```

---

## Appendix A: Exact Metro Area Counts

### MetroAreaSeeder.cs Content (74 city-level rows, 0 state-level):

| State | City Metros | IDs |
|-------|------------|-----|
| AL | Birmingham, Montgomery, Mobile | 01111111..001-003 |
| AK | Anchorage | 02111111..001 |
| AZ | Phoenix, Tucson, Mesa | 04111111..001-003 |
| AR | Little Rock, Fayetteville | 05111111..001-002 |
| CA | LA, SF, San Diego, Sacramento, Fresno, Inland Empire | 06111111..001-006 |
| CO | Denver, Colorado Springs | 08111111..001-002 |
| CT | Hartford, Bridgeport | 09111111..001-002 |
| DE | Wilmington | 10111111..001 |
| FL | Miami, Orlando, Tampa Bay, Jacksonville | 12111111..001-004 |
| GA | Atlanta, Savannah | 13111111..001-002 |
| HI | Honolulu | 15111111..001 |
| ID | Boise | 16111111..001 |
| IL | Chicago | 17111111..001 |
| IN | Indianapolis | 18111111..001 |
| IA | Des Moines | 19111111..001 |
| KS | Kansas City | 20111111..001 |
| KY | Louisville | 21111111..001 |
| LA | New Orleans | 22111111..001 |
| ME | Portland | 23111111..001 |
| MD | Baltimore | 24111111..001 |
| MA | Boston | 25111111..001 |
| MI | Detroit | 26111111..001 |
| MN | Minneapolis-St. Paul | 27111111..001 |
| MS | Jackson | 28111111..001 |
| MO | St. Louis, Kansas City | 29111111..001-002 |
| MT | Billings | 30111111..001 |
| NE | Omaha | 31111111..001 |
| NV | Las Vegas, Reno | 32111111..001-002 |
| NH | Manchester | 33111111..001 |
| NJ | Newark | 34111111..001 |
| NM | Albuquerque | 35111111..001 |
| NY | NYC, Buffalo, Albany | 36111111..001-003 |
| NC | Charlotte, Raleigh | 37111111..001-002 |
| ND | Fargo, Bismarck | 38111111..001-002 |
| OH | Cleveland, Columbus, Cincinnati, Toledo, Akron | 39111111..001-005 |
| OK | Oklahoma City | 40111111..001 |
| OR | Portland | 41111111..001 |
| PA | Philadelphia, Pittsburgh | 42111111..001-002 |
| RI | Providence | 44111111..001 |
| SC | Charleston | 45111111..001 |
| SD | Sioux Falls, Rapid City | 46111111..001-002 |
| TN | Nashville, Memphis | 47111111..001-002 |
| TX | Houston, Dallas-FW, Austin, San Antonio | 48111111..001-004 |
| UT | Salt Lake City | 49111111..001 |
| VT | Burlington | 50111111..001 |
| VA | Richmond | 51111111..001 |
| WA | Seattle | 53111111..001 |
| WV | Charleston, Huntington | 54111111..001-002 |
| WI | Milwaukee | 55111111..001 |
| WY | Cheyenne, Casper | 56111111..001-002 |

**Total city-level in seeder**: 74

### Initial Migration Content (22 rows: 7 state-level + 15 city-level):

| State | State-Level | City Metros |
|-------|-----------|------------|
| AL | All Alabama | Birmingham, Montgomery, Mobile |
| AK | All Alaska | Anchorage |
| AZ | All Arizona | Phoenix, Tucson, Mesa |
| CA | All California | LA, SF, San Diego |
| IL | All Illinois | Chicago |
| NY | All New York | NYC |
| TX | All Texas | Houston, Dallas-FW, Austin |

### Migration 6A.104 Must Insert:

**State-level rows needed (50 total, 7 already exist = 43 new):**
- AR, CO, CT, DE, FL, GA, HI, ID, IN, IA, KS, KY, LA, ME, MD, MA, MI, MN, MS, MO, MT, NE, NV, NH, NJ, NM, NC, ND, OH, OK, OR, PA, RI, SC, SD, TN, UT, VT, VA, WA, WV, WI, WY

**City-level rows needed (from seeder, missing in production):**
- All metros for the 43 missing states
- Sacramento, Fresno, Inland Empire for CA (migration had 3, seeder has 6)
- Buffalo, Albany for NY (migration had 1, seeder has 3)
- San Antonio for TX (migration had 3, seeder has 4)

**Net new rows for production**: 43 state-level + 62 city-level = **105 new rows**
**Net new rows for staging**: 50 state-level + 0 city-level = **50 new rows** (staging already has all 84 city-level)

---

## Appendix B: TreeDropdown Component Dependency Map

```
TreeDropdown.tsx (UI component)
  Used by:
    MetroAreasSelector.tsx (Registration)
    PreferredMetroAreasSection.tsx (Profile)
    NewsletterMetroSelector.tsx (Newsletter)
    LocationFilter.tsx (Event search)
    NewsletterForm.tsx (Newsletter admin)
    events/page.tsx (Events page)

  Data flow:
    GET /api/metro-areas (API)
      -> metro-areas.repository.ts (API client)
        -> useMetroAreas.ts (React hook)
          -> MetroAreasSelector / LocationFilter / etc. (consumers)
            -> Converts MetroAreaDto[] to TreeNode[] (state = parent, metro = child)
              -> TreeDropdown (renders tree)
```

---

*End of RCA Document*
