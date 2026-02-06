# Phase 6A.32 Fix Plan - Interactive Badge Positioning

## ROOT CAUSE SUMMARY

**ISSUE**: Phase 6A.32 implementation is correct in code, but migration `20251217175941_EnforceBadgeLocationConfigNotNull` was never applied to Azure staging database.

**RESULT**: Existing badges have NULL location configs in database, causing defensive null-safe mapping to return identical defaults for all three display locations.

## EVIDENCE

1. ✅ Migration file exists: `20251217175941_EnforceBadgeLocationConfigNotNull.cs`
2. ❌ Migration not applied to Azure staging database
3. ✅ Frontend components render correctly (Position Badge button, dialog, editors)
4. ✅ React-rnd library installed and working
5. ✅ Event handlers properly wired
6. ❌ Database returns NULL for location config columns
7. ✅ Defensive `ToDto()` mapping returns safe defaults
8. ❌ All three locations get IDENTICAL default configs (x=1.0, y=0.0, size=0.26)
9. ✅ UI interactions work (drag, resize, sliders)
10. ❌ Save fails because EF Core expects NOT NULL but database schema is still NULLABLE

## FIX PLAN

### OPTION 1: Apply Migration to Azure (RECOMMENDED - Permanent Fix)

#### Step 1: Apply Migration to Staging Database

```bash
# Connect to Azure staging database and run migration
cd c:\Work\LankaConnect\src\LankaConnect.Infrastructure
dotnet ef database update --startup-project ..\LankaConnect.Api --connection "[STAGING_CONNECTION_STRING]"
```

**What this does**:
1. Updates ALL existing badge rows with default location configs:
   - Listing: TopRight (x=1.0, y=0.0, size=0.26)
   - Featured: TopRight (x=1.0, y=0.0, size=0.26)
   - Detail: TopRight (x=1.0, y=0.0, size=0.21)
2. Alters columns from NULLABLE to NOT NULL with DEFAULT constraints
3. Prevents future NULL insertions at database level

#### Step 2: Verify Migration Applied

```sql
-- Connect to Azure staging database
SELECT
    id,
    name,
    position_x_listing,
    position_y_listing,
    size_width_listing,
    size_height_listing,
    rotation_listing,
    position_x_featured,
    position_y_featured,
    size_width_featured,
    size_height_featured,
    rotation_featured,
    position_x_detail,
    position_y_detail,
    size_width_detail,
    size_height_detail,
    rotation_detail
FROM badges.badges
WHERE id IS NOT NULL;

-- Expected result: ALL columns should have non-NULL values
-- Example: position_x_listing = 1.0000, position_y_listing = 0.0000, etc.
```

#### Step 3: Restart Azure API Container

```bash
# Restart the staging API container to clear EF Core model cache
az webapp restart --name [WEBAPP_NAME] --resource-group [RESOURCE_GROUP]
```

#### Step 4: Test in UI

1. Navigate to Badge Management page
2. Click "Position Badge" on any badge
3. **EXPECTED**: All three tabs show badge at TopRight with default sizes
4. Drag badge to new position
5. **EXPECTED**: Badge moves, sliders update, percentage displays update
6. Adjust sliders
7. **EXPECTED**: Badge position/size changes in real-time
8. Click "Save Positions"
9. **EXPECTED**: Success, no errors
10. Refresh page and click "Position Badge" again
11. **EXPECTED**: Badge shows at saved position (not default)

#### Step 5: Verify API Logs

```bash
# Check Azure container logs for any EF Core errors
az webapp log tail --name [WEBAPP_NAME] --resource-group [RESOURCE_GROUP]
```

**Look for**:
- ✅ No "Cannot insert NULL" errors
- ✅ No "Column does not allow nulls" errors
- ✅ UPDATE statements completing successfully

---

### OPTION 2: Temporary Workaround (If Migration Cannot Be Applied Immediately)

If you cannot apply migration to Azure staging immediately, use this workaround:

#### Step 1: Modify Badge Entity Configuration

Edit `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\BadgeConfiguration.cs`:

```csharp
// Make location config columns NULLABLE in EF model (temporary)
// This allows writes to succeed even with NULLABLE database schema

// Listing config
b.OwnsOne(badge => badge.ListingConfig, listing =>
{
    listing.Property(c => c.PositionX)
        .HasColumnName("position_x_listing")
        .HasColumnType("decimal(5,4)")
        .IsRequired(false)  // ⚠️ TEMPORARY: Allow NULL
        .HasDefaultValue(1.0m);
    // ... repeat for all listing columns
});

// Featured config
b.OwnsOne(badge => badge.FeaturedConfig, featured =>
{
    featured.Property(c => c.PositionX)
        .HasColumnName("position_x_featured")
        .HasColumnType("decimal(5,4)")
        .IsRequired(false)  // ⚠️ TEMPORARY: Allow NULL
        .HasDefaultValue(1.0m);
    // ... repeat for all featured columns
});

// Detail config
b.OwnsOne(badge => badge.DetailConfig, detail =>
{
    detail.Property(c => c.PositionX)
        .HasColumnName("position_x_detail")
        .HasColumnType("decimal(5,4)")
        .IsRequired(false)  // ⚠️ TEMPORARY: Allow NULL
        .HasDefaultValue(1.0m);
    // ... repeat for all detail columns
});
```

#### Step 2: Manually Update Existing Badges via SQL

```sql
-- Run this SQL directly against Azure staging database
-- Updates all badges with default configs without altering schema

UPDATE badges.badges
SET
    position_x_listing = COALESCE(position_x_listing, 1.0),
    position_y_listing = COALESCE(position_y_listing, 0.0),
    size_width_listing = COALESCE(size_width_listing, 0.26),
    size_height_listing = COALESCE(size_height_listing, 0.26),
    rotation_listing = COALESCE(rotation_listing, 0.0),
    position_x_featured = COALESCE(position_x_featured, 1.0),
    position_y_featured = COALESCE(position_y_featured, 0.0),
    size_width_featured = COALESCE(size_width_featured, 0.26),
    size_height_featured = COALESCE(size_height_featured, 0.26),
    rotation_featured = COALESCE(rotation_featured, 0.0),
    position_x_detail = COALESCE(position_x_detail, 1.0),
    position_y_detail = COALESCE(position_y_detail, 0.0),
    size_width_detail = COALESCE(size_width_detail, 0.21),
    size_height_detail = COALESCE(size_height_detail, 0.21),
    rotation_detail = COALESCE(rotation_detail, 0.0)
WHERE id IS NOT NULL;
```

#### Step 3: Rebuild and Redeploy

```bash
# Rebuild with modified configuration
cd c:\Work\LankaConnect\src\LankaConnect.Api
dotnet build --configuration Release

# Deploy to Azure staging
# (Use your deployment script/CI-CD pipeline)
```

**⚠️ WARNING**: This workaround DOES NOT enforce NOT NULL at database level. Future inserts could still create NULL values. MUST apply proper migration later.

---

## VERIFICATION CHECKLIST

After applying fix (either option):

- [ ] Migration applied to database (Option 1) OR manual SQL update completed (Option 2)
- [ ] All existing badges have non-NULL location config values
- [ ] API container restarted
- [ ] Position Badge button appears on badge cards
- [ ] Position Badge dialog opens
- [ ] Three tabs render (Events Listing, Home Featured, Event Detail)
- [ ] Badge image displays in preview
- [ ] Drag badge to reposition - works
- [ ] Resize badge by corners - works
- [ ] Rotation slider changes badge angle - works
- [ ] Position sliders (X/Y) update badge position - works
- [ ] Size sliders (Width/Height) update badge size - works
- [ ] Percentage displays update in real-time - works
- [ ] Mini preview shows all 3 locations with correct positions
- [ ] Click another tab, badge position persists
- [ ] Save Positions button enabled when changes made
- [ ] Save Positions succeeds without errors
- [ ] Refresh page and reopen - saved positions restored
- [ ] Unsaved changes warning when closing dialog - works
- [ ] Browser console has no errors
- [ ] Network tab shows successful PUT requests
- [ ] Azure logs show no EF Core errors

## DIAGNOSTIC STEPS FOR USER

To confirm the root cause before applying fix:

### Step 1: Check Database Schema

Connect to Azure staging database and run:

```sql
-- Check if columns are NULLABLE (root cause)
SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    DATA_TYPE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'badges'
    AND TABLE_NAME = 'badges'
    AND COLUMN_NAME LIKE '%_listing'
ORDER BY COLUMN_NAME;

-- Expected BEFORE fix: IS_NULLABLE = 'YES'
-- Expected AFTER fix: IS_NULLABLE = 'NO' with DEFAULT values
```

### Step 2: Check Existing Badge Data

```sql
-- Check if existing badges have NULL values (root cause)
SELECT
    id,
    name,
    position_x_listing,
    position_y_listing,
    size_width_listing,
    size_height_listing,
    rotation_listing
FROM badges.badges
WHERE position_x_listing IS NULL
    OR position_y_listing IS NULL
    OR size_width_listing IS NULL
    OR size_height_listing IS NULL
    OR rotation_listing IS NULL;

-- Expected BEFORE fix: Rows with NULL values
-- Expected AFTER fix: No rows (empty result set)
```

### Step 3: Check Applied Migrations

```bash
# Check if migration is applied
cd c:\Work\LankaConnect\src\LankaConnect.Infrastructure
dotnet ef migrations list --startup-project ..\LankaConnect.Api

# Look for: 20251217175941_EnforceBadgeLocationConfigNotNull
# Status should be "Applied" (not "Pending")
```

### Step 4: Test API Endpoint Directly

```bash
# Get badges from API
curl -X GET "https://[STAGING_API_URL]/api/badges?activeOnly=false&forManagement=true" \
  -H "Authorization: Bearer [TOKEN]"

# Check response - look for listingConfig, featuredConfig, detailConfig
# BEFORE fix: All three configs will have identical values (x:1.0, y:0.0, size:0.26)
# AFTER fix: Configs should reflect database values (may still be defaults initially)
```

### Step 5: Browser DevTools Check

1. Open Badge Management page
2. Open browser DevTools (F12)
3. Go to Console tab
4. Click "Position Badge" button
5. **Look for**:
   - ❌ React errors about undefined/null props
   - ❌ TypeError when accessing config properties
   - ❌ react-rnd mounting errors
   - ❌ onChange handler errors
6. Go to Network tab
7. Click "Save Positions"
8. **Look for**:
   - ✅ PUT request to `/api/badges/{id}`
   - ❌ 400 Bad Request (validation errors)
   - ❌ 500 Internal Server Error (database constraint violation)
   - ✅ 200 OK with updated badge DTO

## EXPECTED BEHAVIOR AFTER FIX

Once migration is applied:

1. **Initial State**: All badges have default TopRight positioning
2. **User Action**: Click "Position Badge" on any badge
3. **Dialog Opens**: Shows badge at TopRight corner (default)
4. **User Drags Badge**: Badge moves smoothly, sliders update
5. **User Adjusts Sliders**: Badge repositions/resizes in real-time
6. **User Switches Tabs**: Each location preserves its independent config
7. **User Clicks Save**: API accepts update, database persists values
8. **User Reopens Dialog**: Badge appears at saved position (not default)
9. **User Tests Featured Tab**: Different position from Listing tab
10. **User Tests Detail Tab**: Different position from Listing/Featured tabs

## KNOWN LIMITATIONS

1. **Default positioning**: All existing badges will start at TopRight after migration
2. **Manual repositioning required**: Admins must manually position existing badges
3. **No bulk edit**: Must position each badge individually
4. **No copy/paste**: Cannot copy position from one badge to another
5. **No templates**: Cannot save position templates

## FUTURE ENHANCEMENTS (Deferred)

- Bulk position update for multiple badges
- Position templates (save/load common positions)
- Copy position between badges
- Auto-detect optimal position based on image content
- Preview on real event images (not just background)

## RELATED PHASES

- **Phase 6A.31a**: Added per-location configuration to domain model
- **Phase 6A.31b**: Created migration to enforce NOT NULL constraints
- **Phase 6A.32**: Built interactive positioning UI (THIS PHASE)

## REFERENCES

- Migration file: `src/LankaConnect.Infrastructure/Data/Migrations/20251217175941_EnforceBadgeLocationConfigNotNull.cs`
- Badge entity: `src/LankaConnect.Domain/Badges/Badge.cs`
- Mapping extensions: `src/LankaConnect.Application/Badges/DTOs/BadgeMappingExtensions.cs`
- UI components: `web/src/presentation/components/features/badges/`
- Hook: `web/src/presentation/hooks/useBadgePositioning.ts`

---

**Created**: 2025-12-17
**Status**: Ready for deployment
**Priority**: HIGH - Migration must be applied before Phase 6A.32 can be tested
