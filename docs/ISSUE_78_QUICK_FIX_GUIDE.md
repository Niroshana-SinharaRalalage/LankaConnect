# Issue #78: Festival Filter Error - Quick Fix Guide

**Problem**: Selecting "Festival" from Event Type filter shows error instead of events.

**Root Cause**: Backend has 8 EventCategory values (0-7), frontend has 12 values (0-11). Festival=9 doesn't exist in backend enum.

---

## 🚀 Quick Fix (15 minutes)

### Step 1: Update Backend Enum
**File**: `src/LankaConnect.Domain/Events/Enums/EventCategory.cs`

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
    Workshop,       // 8  ← ADD
    Festival,       // 9  ← ADD
    Ceremony,       // 10 ← ADD
    Celebration     // 11 ← ADD
}
```

### Step 2: Create Database Migration
```bash
dotnet ef migrations add Phase6A109_AddMissingEventCategories --project src/LankaConnect.Infrastructure
```

### Step 3: Add Reference Data Seeding
**In generated migration file**:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
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
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        DELETE FROM reference_data.reference_values
        WHERE enum_type = 'EventCategory'
        AND int_value IN (8, 9, 10, 11);
    ");
}
```

### Step 4: Test Locally
```bash
# Run migration
dotnet ef database update --project src/LankaConnect.Infrastructure

# Verify reference data
psql -d lankaconnect -c "SELECT * FROM reference_data.reference_values WHERE enum_type = 'EventCategory' ORDER BY int_value;"

# Test API
curl "http://localhost:5000/api/events?category=9"

# Should return events (or empty array if none exist)
```

### Step 5: Deploy to Staging
```bash
git add .
git commit -m "fix(enums): Add missing EventCategory values (Workshop, Festival, Ceremony, Celebration) - Issue #78"
git push origin feature/issue-78-festival-filter-fix
```

---

## ✅ Verification Checklist

- [ ] Backend enum has 12 values (0-11)
- [ ] Migration created and applied locally
- [ ] Reference data table has 12 EventCategory entries
- [ ] API accepts `category=9` without error
- [ ] Frontend dropdown shows all 12 categories
- [ ] Selecting Festival filter works without error
- [ ] Create event with Festival category succeeds
- [ ] Tests pass: `dotnet test`

---

## 📊 Impact

**Before Fix**:
- ❌ Festival filter: ERROR
- ❌ Workshop filter: ERROR
- ❌ Ceremony filter: ERROR
- ❌ Celebration filter: ERROR

**After Fix**:
- ✅ Festival filter: Works
- ✅ Workshop filter: Works
- ✅ Ceremony filter: Works
- ✅ Celebration filter: Works

---

## 🔍 Technical Details

**Enum Mismatch**:
- Backend: `EventCategory` has 8 values (Religious=0 to Entertainment=7)
- Frontend: `EventCategory` has 12 values (Religious=0 to Celebration=11)
- User selects Festival (9) → Backend query: `WHERE category = 9` → No results → Error

**Fix**:
- Add 4 missing enum values to backend
- Seed reference_values table with new categories
- Frontend already correct - no changes needed

**Files Modified**:
1. `src/LankaConnect.Domain/Events/Enums/EventCategory.cs` (enum definition)
2. `src/LankaConnect.Infrastructure/Data/Migrations/[timestamp]_Phase6A109_AddMissingEventCategories.cs` (new migration)

**Files NOT Modified**:
- Frontend already has correct enum values
- No API changes needed
- No controller changes needed

---

## 🛡️ Prevention (Optional - Add Later)

### Add CI/CD Enum Validation
```typescript
// scripts/validate-enums.ts
import { EventCategory } from '@/infrastructure/api/types/events.types';

// Parse backend EventCategory.cs
const backendEnumValues = parseBackendEnum('EventCategory');
const frontendEnumValues = Object.values(EventCategory).filter(v => typeof v === 'number');

if (backendEnumValues.length !== frontendEnumValues.length) {
  throw new Error('EventCategory enum mismatch between backend and frontend!');
}
```

### Add Startup Validation
```csharp
// src/LankaConnect.API/Validators/EnumValidator.cs
public class EnumValidator : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var dbCategories = await _db.ReferenceValues
            .Where(r => r.EnumType == "EventCategory")
            .CountAsync(ct);

        var enumCount = Enum.GetValues<EventCategory>().Length;

        if (dbCategories != enumCount)
        {
            _logger.LogError(
                "EventCategory mismatch! DB: {DbCount}, Enum: {EnumCount}",
                dbCategories, enumCount);
        }
    }
}
```

---

## 📚 Related Documentation

- **Full RCA**: `docs/RCA_ISSUE_78_FESTIVAL_FILTER_ERROR.md`
- **Flow Diagram**: `docs/diagrams/issue-78-festival-filter-flow.md`
- **Issue #78**: GitHub Issue (user reported problem)

---

## ⏱️ Time Estimate

- Backend enum update: 5 minutes
- Migration creation: 5 minutes
- Testing: 5 minutes
- **Total**: 15 minutes

**Effort**: Low
**Risk**: Low (adding enum values is non-breaking)
**Priority**: High (user-facing feature broken)
