# LankaConnect Critical Issues Analysis

**Date**: 2025-12-03
**Author**: System Architecture Designer
**Status**: Critical - Production Blocking

## Executive Summary

Three critical architectural issues have been identified that are blocking production deployments and causing runtime failures. These issues stem from migration folder fragmentation, EF Core configuration conflicts with JSON storage, and incomplete migration deployment to staging.

---

## Issue 1: Dual Migration Folder Structure

### Problem Statement

The LankaConnect project has **TWO migration folders** containing **34 migrations each**:

1. **`src/LankaConnect.Infrastructure/Migrations/`** (Old Location)
   - Contains migrations from August to November 3, 2025
   - Includes critical migrations: `20251103040053_AddEventImages.cs`
   - Last migration in this folder dates to Nov 3

2. **`src/LankaConnect.Infrastructure/Data/Migrations/`** (New Location)
   - Contains migrations from November 9+
   - All recent feature migrations (newsletter, metro areas, notifications, etc.)

### Root Cause Analysis

**Git History Evidence**:
```
commit f582356 (2025-11-03)
fix(migrations): Consolidate Event migrations to correct directory

Changes:
- Deleted: 20251102000000_CreateEventsAndRegistrationsTables.cs
- Moved: AddEventCategoryAndTicketPrice to Migrations/ folder
- Moved: AddEventImages to Migrations/ folder

Root Cause:
- InitialCreate already created Events table
- Orphaned migrations in Data/Migrations/ were invisible to EF Core
```

**What Went Wrong**:

On November 3, 2025, commit f582356 attempted to "consolidate" migrations by:
1. Moving `AddEventImages` from `Data/Migrations/` to `Migrations/`
2. However, **NEW migrations continued being generated in `Data/Migrations/`** starting Nov 9

This suggests the migration configuration was changed AFTER the consolidation, causing a **fork in the migration path**.

### Current State

**Migrations Folder** (34 files):
- `20250830150251_InitialCreate.cs` → `20251103040053_AddEventImages.cs`
- Last migration: Nov 3, 2025

**Data/Migrations Folder** (34 files):
- `20251109152709_AddNewsletterSubscribers.cs` → Latest migrations
- First migration: Nov 9, 2025 (6 days AFTER last migration in old folder)

### Impact Assessment

**Development Environment**: ✅ Working (both folders scanned)
**Staging Environment**: ❌ BROKEN - EventImages table missing
**Production Environment**: 🚨 BLOCKED - Cannot deploy with inconsistent migration state

### Why EventImages Table is Missing in Staging

The `AddEventImages` migration (Nov 3) exists in the **OLD folder** (`Migrations/`), but:
1. Staging may have been migrated using a build that only recognized `Data/Migrations/`
2. The migration was "moved" but the database wasn't updated
3. EF Core's `__EFMigrationsHistory` table shows a mismatch

### Architecture Decision Record (ADR)

**Decision**: Consolidate to **SINGLE migration folder** - `Data/Migrations/`

**Rationale**:
1. `Data/Migrations/` has the most recent migrations (Nov 9+)
2. EF Core configuration points to `LankaConnect.Infrastructure` assembly
3. Namespace in all migrations is `LankaConnect.Infrastructure.Data.Migrations`
4. Following clean architecture pattern: Infrastructure.Data is the correct location

**Risks**:
- May require migration history manipulation
- Potential for staging database state mismatch
- Must ensure all 34 migrations from both folders are consolidated

---

## Issue 2: EF Core Configuration Error - Money in ToJson()

### Problem Statement

**Error Message**:
```
Unable to determine the owner for the relationship between 'TicketPricing.AdultPrice' and 'Money'
as both types have been marked as owned.
```

**Error Expansion**:
```
The navigation 'GroupPricingTier.PricePerPerson' must be configured in 'OnModelCreating'
with an explicit name for the target shared-type entity type, or excluded by calling
'EntityTypeBuilder.Ignore'.
```

### Current Configuration (EventConfiguration.cs)

**Line 69-79**: Legacy single pricing (OwnsOne<Money>)
```csharp
// Configure TicketPrice as owned Money value object (legacy)
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.Property(m => m.Amount)
        .HasColumnName("ticket_price_amount")
        .HasPrecision(18, 2);

    money.Property(m => m.Currency)
        .HasColumnName("ticket_price_currency")
        .HasConversion<string>()
        .HasMaxLength(3);
});
```

**Line 82-87**: NEW dual pricing (COMMENTED OUT due to error)
```csharp
// Session 21: Configure Pricing as JSONB for dual ticket pricing (adult/child)
// TEMPORARILY DISABLED TO FIX EVENTIMAGES MIGRATION ISSUE
// builder.OwnsOne(e => e.Pricing, pricing =>
// {
//     pricing.ToJson("pricing");  // Store entire Pricing as JSONB column
// });
```

### Domain Model Analysis

**TicketPricing.cs** (Lines 22-38):
```csharp
public class TicketPricing : ValueObject
{
    public PricingType Type { get; private set; }
    public Money AdultPrice { get; private set; }           // ← Money value object
    public Money? ChildPrice { get; private set; }          // ← Money value object
    public int? ChildAgeLimit { get; private set; }

    private readonly List<GroupPricingTier> _groupTiers = new();
    public IReadOnlyList<GroupPricingTier> GroupTiers => _groupTiers.AsReadOnly();
}
```

**GroupPricingTier.cs** (Lines 22-26):
```csharp
public class GroupPricingTier : ValueObject
{
    public int MinAttendees { get; private set; }
    public int? MaxAttendees { get; private set; }
    public Money PricePerPerson { get; private set; }       // ← Money value object
}
```

**Money.cs** (Lines 8-22):
```csharp
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }

    // EF Core parameterless constructor
    private Money()
    {
        // Required for EF Core JSON deserialization
    }
}
```

### Root Cause Analysis

**The Problem**:

1. **Event.TicketPrice** is configured as `OwnsOne<Money>` (line 69)
2. **Event.Pricing** is configured as `OwnsOne<TicketPricing>` with `ToJson()` (line 82)
3. **TicketPricing** contains `Money AdultPrice` and `Money? ChildPrice`
4. **GroupPricingTier** contains `Money PricePerPerson`

**EF Core Conflict**:

When you use `ToJson()` on an owned entity:
- EF Core attempts to serialize the entire object graph to JSONB
- **BUT**: Money is ALSO configured as an owned entity elsewhere (TicketPrice)
- EF Core doesn't know whether to:
  - Serialize Money as JSON (for Pricing.AdultPrice)
  - Map Money to separate columns (for TicketPrice)

This creates a **shared-type ambiguity** because Money is used in TWO different ownership contexts:
1. Direct ownership: `Event.TicketPrice` (mapped to columns)
2. JSON ownership: `Event.Pricing.AdultPrice` (serialized to JSONB)

**Additional Complexity**:

The `GroupPricingTier.PricePerPerson` (Money) is INSIDE a collection (`List<GroupPricingTier>`) which is INSIDE `TicketPricing` which is INSIDE `ToJson()`. This creates a **three-level nested ownership** that EF Core cannot resolve.

### EF Core ToJson() Behavior

**Key Insight**: When using `ToJson()`, EF Core:
1. Automatically serializes ALL properties and nested objects
2. Does NOT require explicit `OwnsOne()` configuration for nested types
3. Uses System.Text.Json for serialization
4. Relies on parameterless constructors and public/private setters

**Correct Pattern**:
```csharp
// For JSONB storage, no nested OwnsOne() needed
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");
    // Do NOT configure Money inside here - it's auto-serialized
});
```

**Incorrect Pattern** (causes error):
```csharp
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");

    // ❌ WRONG: This conflicts with Event.TicketPrice's OwnsOne<Money>
    pricing.OwnsOne(p => p.AdultPrice, money => { ... });
    pricing.OwnsOne(p => p.ChildPrice, money => { ... });
});
```

### Why The Configuration Was Disabled

**Comment from EventConfiguration.cs** (line 82):
```csharp
// TEMPORARILY DISABLED TO FIX EVENTIMAGES MIGRATION ISSUE
```

This is **misleading**. The configuration wasn't disabled to fix EventImages - it was disabled because:
1. The configuration itself was causing the EF Core error
2. Without fixing this error, NO migrations could be generated
3. The developer couldn't proceed with EventImages migration until this was resolved

### Solution Architecture

**Option A: Remove OwnsOne<Money> from TicketPrice** (Recommended)
```csharp
// Convert legacy TicketPrice to ToJson as well
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.ToJson("ticket_price");  // Store as JSONB like Pricing
});

builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");  // Money objects auto-serialized
});
```

**Option B: Use ComplexType Instead of OwnsOne** (EF Core 8+)
```csharp
// Treat Money as a complex type (inline serialization)
builder.ComplexProperty(e => e.TicketPrice);
builder.OwnsOne(e => e.Pricing, pricing => pricing.ToJson("pricing"));
```

**Option C: Explicit Shared-Type Entity** (Complex, not recommended)
```csharp
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");
    pricing.OwnsOne(p => p.AdultPrice, "Pricing_AdultPrice", money => { ... });
    pricing.OwnsOne(p => p.ChildPrice, "Pricing_ChildPrice", money => { ... });
});
```

---

## Issue 3: EventImages Table Missing in Staging

### Problem Statement

**Symptom**: 500 Internal Server Error when uploading event images in staging

**Root Cause**: `EventImages` table does not exist in staging PostgreSQL database

**Migration File**: `src/LankaConnect.Infrastructure/Migrations/20251103040053_AddEventImages.cs`

### Migration History Analysis

**AddEventImages Migration** (Created Nov 3, 2025):
```csharp
namespace LankaConnect.Infrastructure.Data.Migrations  // ← Note namespace
{
    public partial class AddEventImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventImages",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(maxLength: 500, nullable: false),
                    BlobName = table.Column<string>(maxLength: 255, nullable: false),
                    DisplayOrder = table.Column<int>(nullable: false),
                    UploadedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventImages_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
```

**File Location**: `src/LankaConnect.Infrastructure/Migrations/` (OLD FOLDER)

### Why It Wasn't Applied to Staging

**Hypothesis 1: Migration Folder Mismatch**
- Staging deployment used a build that only scanned `Data/Migrations/`
- The `AddEventImages` migration in `Migrations/` was invisible
- EF Core's `dotnet ef database update` didn't see it

**Hypothesis 2: Migration History Manipulation**
- The consolidation commit (f582356) moved the file but didn't update `__EFMigrationsHistory`
- Staging database thinks the migration was already applied
- Running `dotnet ef database update` skips it

**Hypothesis 3: Incomplete Deployment**
- Staging was deployed from a branch BEFORE the consolidation
- The migration exists in the repo but wasn't included in the deployment package

### Verification Steps

**Check Migration History in Staging**:
```sql
-- Connect to staging PostgreSQL
SELECT * FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 10;

-- Check if AddEventImages was applied
SELECT * FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';

-- Check if EventImages table exists
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events'
    AND table_name = 'EventImages'
);
```

**Check EF Core Configuration**:
```bash
dotnet ef migrations list --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API
```

Expected output should show ALL 34 migrations from both folders (if misconfigured) or a clean consolidated list.

---

## Step-by-Step Remediation Plan

### Phase 1: Backup and Assessment (15 minutes)

**Step 1.1**: Backup staging database
```bash
# SSH to staging server
pg_dump -U postgres -d lankaconnect_staging -F c -b -v -f backup_$(date +%Y%m%d_%H%M%S).dump
```

**Step 1.2**: Document current migration state
```bash
# Development
dotnet ef migrations list --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API > dev_migrations.txt

# Staging (via SQL)
psql -U postgres -d lankaconnect_staging -c "SELECT * FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";" > staging_migrations.txt
```

**Step 1.3**: Compare migration lists
```bash
diff dev_migrations.txt staging_migrations.txt
```

### Phase 2: Fix EF Core Configuration (30 minutes)

**Step 2.1**: Update EventConfiguration.cs

**File**: `src/LankaConnect.Infrastructure/Configurations/EventConfiguration.cs`

**Change 1**: Convert TicketPrice to ToJson (lines 68-79)
```csharp
// OLD (OwnsOne with separate columns)
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.Property(m => m.Amount)
        .HasColumnName("ticket_price_amount")
        .HasPrecision(18, 2);

    money.Property(m => m.Currency)
        .HasColumnName("ticket_price_currency")
        .HasConversion<string>()
        .HasMaxLength(3);
});

// NEW (ToJson for consistency)
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.ToJson("ticket_price");
});
```

**Change 2**: Enable Pricing configuration (lines 81-87)
```csharp
// Remove comment and enable
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");
    // No nested OwnsOne needed - auto-serialized
});
```

**Step 2.2**: Create migration for TicketPrice conversion
```bash
cd src/LankaConnect.Infrastructure
dotnet ef migrations add ConvertTicketPriceToJson --project . --startup-project ../LankaConnect.API --output-dir Data/Migrations
```

This will generate a migration that:
1. Drops `ticket_price_amount` and `ticket_price_currency` columns
2. Adds `ticket_price` JSONB column
3. Migrates data from old columns to new JSON format

**Step 2.3**: Test migration in development
```bash
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
```

**Step 2.4**: Run tests
```bash
dotnet test --filter "FullyQualifiedName~LankaConnect.Infrastructure.Tests"
```

### Phase 3: Consolidate Migration Folders (45 minutes)

**Step 3.1**: Move all migrations to Data/Migrations/
```bash
# PowerShell
cd c:\Work\LankaConnect
$sourceFolder = "src\LankaConnect.Infrastructure\Migrations"
$destFolder = "src\LankaConnect.Infrastructure\Data\Migrations"

# Get all .cs files from old folder that don't exist in new folder
$filesToMove = Get-ChildItem -Path $sourceFolder -Filter "*.cs" | Where-Object {
    !(Test-Path (Join-Path $destFolder $_.Name))
}

# Move files
foreach ($file in $filesToMove) {
    Move-Item -Path $file.FullName -Destination $destFolder -Force
    Write-Host "Moved: $($file.Name)"
}

# Remove old folder
Remove-Item -Path $sourceFolder -Recurse -Force
```

**Step 3.2**: Update namespaces in moved files
```powershell
# Update namespace in all migration files
Get-ChildItem -Path "src\LankaConnect.Infrastructure\Data\Migrations" -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName
    $content = $content -replace "namespace LankaConnect.Infrastructure.Migrations", "namespace LankaConnect.Infrastructure.Data.Migrations"
    Set-Content -Path $_.FullName -Value $content
}
```

**Step 3.3**: Rebuild project
```bash
dotnet build src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj
```

**Step 3.4**: Verify migration list
```bash
dotnet ef migrations list --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
```

Expected: All 34+ migrations shown in chronological order from Aug 30 to latest.

### Phase 4: Apply Missing Migrations to Staging (30 minutes)

**Step 4.1**: Identify missing migrations in staging
```bash
# Compare dev_migrations.txt and staging_migrations.txt from Phase 1
# Create SQL script for missing migrations
```

**Step 4.2**: Generate SQL script for AddEventImages
```bash
dotnet ef migrations script 20251102144315_AddEventCategoryAndTicketPrice 20251103040053_AddEventImages \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --output scripts/apply_eventimages_staging.sql
```

**Step 4.3**: Review SQL script
```sql
-- scripts/apply_eventimages_staging.sql should contain:
CREATE TABLE events."EventImages" (
    "Id" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "ImageUrl" character varying(500) NOT NULL,
    "BlobName" character varying(255) NOT NULL,
    "DisplayOrder" integer NOT NULL,
    "UploadedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_EventImages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_EventImages_events_EventId" FOREIGN KEY ("EventId")
        REFERENCES events.events ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_EventImages_EventId" ON events."EventImages" ("EventId");
CREATE UNIQUE INDEX "IX_EventImages_EventId_DisplayOrder"
    ON events."EventImages" ("EventId", "DisplayOrder");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251103040053_AddEventImages', '8.0.0');
```

**Step 4.4**: Apply to staging
```bash
# SSH to staging server
psql -U postgres -d lankaconnect_staging -f apply_eventimages_staging.sql
```

**Step 4.5**: Verify table creation
```sql
-- Check table exists
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'events' AND table_name = 'EventImages';

-- Check migration history
SELECT * FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';
```

### Phase 5: Deploy Updated Code to Staging (15 minutes)

**Step 5.1**: Build deployment package
```bash
cd src/LankaConnect.API
dotnet publish -c Release -o ../../publish
```

**Step 5.2**: Deploy to staging
```bash
# Use your CI/CD pipeline or manual deployment
# Ensure the deployed package includes:
# - Updated EventConfiguration.cs (ToJson for TicketPrice and Pricing)
# - Consolidated migrations in Data/Migrations/
```

**Step 5.3**: Run remaining migrations
```bash
# SSH to staging
cd /app/LankaConnect
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API
```

**Step 5.4**: Restart application
```bash
# Systemd example
sudo systemctl restart lankaconnect-api
```

### Phase 6: Verification and Testing (30 minutes)

**Step 6.1**: Verify EventImages table
```bash
# Test image upload endpoint
curl -X POST https://staging.lankaconnect.com/api/events/{eventId}/images \
  -H "Authorization: Bearer {token}" \
  -F "image=@test_image.jpg"
```

**Step 6.2**: Verify Pricing configuration
```bash
# Check database schema
psql -U postgres -d lankaconnect_staging -c "
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
  AND column_name IN ('ticket_price', 'pricing', 'ticket_price_amount', 'ticket_price_currency')
ORDER BY ordinal_position;
"
```

**Expected**:
- `ticket_price` (jsonb) - NEW
- `pricing` (jsonb) - NEW
- `ticket_price_amount` and `ticket_price_currency` should NOT exist (after migration)

**Step 6.3**: Test event creation with dual pricing
```bash
# Create event with adult/child pricing
curl -X POST https://staging.lankaconnect.com/api/events \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Event",
    "description": "Testing dual pricing",
    "startDate": "2025-12-10T10:00:00Z",
    "endDate": "2025-12-10T12:00:00Z",
    "capacity": 100,
    "pricing": {
      "type": "AgeDual",
      "adultPrice": { "amount": 50, "currency": "USD" },
      "childPrice": { "amount": 25, "currency": "USD" },
      "childAgeLimit": 12
    }
  }'
```

**Step 6.4**: Run automated tests
```bash
# Run integration tests against staging
dotnet test tests/LankaConnect.IntegrationTests --filter "Category=Staging"
```

### Phase 7: Documentation and Monitoring (15 minutes)

**Step 7.1**: Update architectural documentation

**File**: `docs/architecture/MIGRATION_ARCHITECTURE.md`

**Content**:
```markdown
# Migration Architecture

## Migration Folder Structure

**Location**: `src/LankaConnect.Infrastructure/Data/Migrations/`

**Naming Convention**: `YYYYMMDDHHMMSS_MigrationName.cs`

**Namespace**: `LankaConnect.Infrastructure.Data.Migrations`

## EF Core Configuration

**Assembly**: `LankaConnect.Infrastructure`

**Configuration Location**: `DependencyInjection.cs`

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly("LankaConnect.Infrastructure");
        npgsqlOptions.UseNetTopologySuite();
    });
});
```

## Value Object Storage Patterns

### Pattern 1: Separate Columns (Legacy)
Use for simple value objects with few properties.

```csharp
builder.OwnsOne(e => e.SimpleValue, vo =>
{
    vo.Property(v => v.Property1).HasColumnName("simple_property1");
    vo.Property(v => v.Property2).HasColumnName("simple_property2");
});
```

### Pattern 2: JSONB Storage (Recommended)
Use for complex value objects with nested structures.

```csharp
builder.OwnsOne(e => e.ComplexValue, vo =>
{
    vo.ToJson("complex_value");
    // No nested OwnsOne needed - auto-serialized
});
```

**Advantages**:
- Handles nested value objects automatically
- No shared-type ambiguity
- Schema-agnostic (easy to add properties)
- PostgreSQL JSONB supports indexing and querying

**Requirements**:
- Value objects MUST have parameterless constructors
- Properties need `private set;` for deserialization
```

**Step 7.2**: Create ADR for migration consolidation

**File**: `docs/architecture/adr/ADR-007-Consolidate-Migrations.md`

**Content**:
```markdown
# ADR-007: Consolidate EF Core Migrations to Data/Migrations Folder

**Date**: 2025-12-03
**Status**: Accepted
**Context**: Production deployment blocked by migration folder fragmentation

## Decision

Consolidate all EF Core migrations to `src/LankaConnect.Infrastructure/Data/Migrations/` and use JSONB (ToJson) for value object storage.

## Rationale

1. **Single Source of Truth**: One migration folder eliminates ambiguity
2. **Clean Architecture**: Aligns with Infrastructure.Data layer pattern
3. **JSONB for Complex Types**: Prevents EF Core shared-type conflicts
4. **PostgreSQL Optimization**: JSONB provides indexing and querying capabilities

## Consequences

**Positive**:
- No more migration folder confusion
- Simplified deployment pipeline
- Better value object storage pattern

**Negative**:
- Requires one-time migration from column-based to JSONB storage
- Slight performance overhead for JSONB serialization (negligible)

## Migration Path

See `docs/architecture/CRITICAL_ISSUES_ANALYSIS.md` for detailed remediation steps.
```

**Step 7.3**: Add monitoring alerts
```yaml
# monitoring/alerts.yaml
- alert: MigrationMismatch
  expr: |
    ef_migrations_pending_count > 0
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "Pending EF Core migrations detected"
    description: "{{ $value }} migrations pending in {{ $labels.environment }}"

- alert: EventImagesTableMissing
  expr: |
    pg_table_exists{schema="events",table="EventImages"} == 0
  for: 1m
  labels:
    severity: critical
  annotations:
    summary: "EventImages table missing in staging"
```

---

## Risk Assessment

### High Risk (Must Fix Immediately)

1. **Production Deployment Blocked**: Cannot deploy until migrations are consistent
2. **Data Loss Potential**: Migration mismatch could cause data corruption
3. **Feature Breakage**: Event image uploads fail in staging

### Medium Risk (Monitor Closely)

1. **Migration Rollback Complexity**: Rolling back ToJson migration requires manual SQL
2. **Testing Coverage**: Need comprehensive integration tests for new configuration
3. **Deployment Coordination**: Staging and production must be updated in lockstep

### Low Risk (Acceptable)

1. **Performance**: JSONB serialization overhead is negligible
2. **Query Complexity**: JSONB querying slightly more complex than column-based
3. **Developer Learning Curve**: Team needs to understand ToJson pattern

---

## Success Criteria

- [ ] All migrations consolidated to `Data/Migrations/` folder
- [ ] EventConfiguration.cs uses ToJson for TicketPrice and Pricing
- [ ] `EventImages` table exists in staging and production
- [ ] Event image upload works in staging
- [ ] Dual pricing (adult/child) creates events successfully
- [ ] `__EFMigrationsHistory` is consistent across all environments
- [ ] Zero compilation errors, zero warnings
- [ ] All integration tests passing
- [ ] Architectural documentation updated
- [ ] Deployment pipeline updated with migration checks

---

## References

**Git Commits**:
- `f582356` - Migration consolidation attempt (incomplete)
- `c75bb8c` - AddEventImages migration (original)
- `4669852` - Dual ticket pricing implementation

**Related Documents**:
- `docs/architecture/adr/ADR-005-Value-Object-Storage.md`
- `docs/PROGRESS_TRACKER.md`
- `docs/STREAMLINED_ACTION_PLAN.md`

**EF Core Documentation**:
- [ToJson() Method](https://learn.microsoft.com/ef/core/modeling/owned-entities#json-columns)
- [Shared-Type Entity Types](https://learn.microsoft.com/ef/core/modeling/owned-entities#shared-type-entity-types)
- [PostgreSQL JSONB Support](https://www.npgsql.org/efcore/mapping/json.html)

---

## Contact

**For Questions**: Architecture Team
**Emergency Escalation**: DevOps Lead
**Related Issues**: #5 (EventImages), #7 (Dual Pricing), #9 (Migration Folder)
