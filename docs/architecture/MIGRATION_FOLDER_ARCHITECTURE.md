# Migration Folder Architecture - Visual Guide

## Current State (Broken)

```
src/LankaConnect.Infrastructure/
├── Migrations/                    ← OLD FOLDER (34 migrations)
│   ├── 20250830_InitialCreate.cs
│   ├── 20250831_InitialMigration.cs
│   ├── 20250904_AddCommunicationsTables.cs
│   ├── 20251028_AddEntraExternalIdSupport.cs
│   ├── 20251031_AddUserProfilePhoto.cs
│   ├── 20251031_AddUserLocation.cs
│   ├── 20251031_AddUserCulturalInterestsAndLanguages.cs
│   ├── 20251101_CreateUserCulturalInterestsAndLanguagesTables.cs
│   ├── 20251102_CreateEventsAndRegistrationsTables.cs
│   │   └── ❌ DELETED in f582356 (redundant)
│   ├── 20251102_AddEventCategoryAndTicketPrice.cs
│   │   └── ✅ MOVED to Data/Migrations/ in f582356
│   └── 20251103_AddEventImages.cs  ← 🚨 CRITICAL: Missing in staging
│       └── ✅ MOVED to Data/Migrations/ in f582356
│
└── Data/
    └── Migrations/                ← NEW FOLDER (34 migrations)
        ├── 20251109_AddNewsletterSubscribers.cs
        ├── 20251110_CreateMetroAreasTable.cs
        ├── 20251110_AddUserPreferredMetroAreas.cs
        ├── 20251111_AddUserRoleUpgradeTracking.cs
        ├── 20251111_AddSubscriptionManagement.cs
        ├── 20251111_AddNotificationsTable.cs
        ├── 20251111_AddEventTemplatesTable.cs
        ├── 20251112_SeedMetroAreasReferenceData.cs
        └── 20251115_RecreateNewsletterTableFixVersionColumn.cs
```

## Timeline Analysis

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Migration Timeline                               │
└─────────────────────────────────────────────────────────────────────────┘

Aug 30                 Nov 1-3                 Nov 3               Nov 9+
   │                      │                      │                   │
   ▼                      ▼                      ▼                   ▼
   ┌──────────────────────┬──────────────────────┬──────────────────────┐
   │  Migrations/         │  Migrations/         │  Data/Migrations/    │
   │  (Original folder)   │  (AddEventImages)    │  (New migrations)    │
   │                      │                      │                      │
   │  ✅ Applied to dev   │  ❌ NOT in staging   │  ✅ Applied to dev   │
   │  ✅ Applied to stage │  ❌ NOT in staging   │  ✅ Applied to stage │
   └──────────────────────┴──────────────────────┴──────────────────────┘
                                    │
                                    │ Commit f582356 (Nov 3)
                                    │ "Consolidate migrations"
                                    │
                      ┌─────────────┴─────────────┐
                      │  MOVED to Data/Migrations/ │
                      │  but staging wasn't updated│
                      └───────────────────────────┘
```

## Git Commit Flow

```
f582356 (Nov 3) - "Consolidate Event migrations to correct directory"
   │
   ├─ Deleted: 20251102000000_CreateEventsAndRegistrationsTables.cs
   ├─ Moved: 20251102_AddEventCategoryAndTicketPrice.cs → Data/Migrations/
   └─ Moved: 20251103_AddEventImages.cs → Data/Migrations/

   ⚠️  BUT: EF Core configuration wasn't updated
   ⚠️  RESULT: New migrations continued being generated in Data/Migrations/
   ⚠️  IMPACT: Old folder still has 17 migrations not in Data/Migrations/

20251109+ - New migrations generated in Data/Migrations/ only
   │
   └─ GAP: 6 days between last migration in old folder and first in new folder
```

## Database State Comparison

### Development Database
```sql
SELECT COUNT(*) FROM "__EFMigrationsHistory";
-- Expected: ~34 migrations

SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%AddEventImages%';
-- Expected: 20251103040053_AddEventImages

SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
);
-- Expected: true
```

### Staging Database
```sql
SELECT COUNT(*) FROM "__EFMigrationsHistory";
-- Actual: ~32 migrations (missing AddEventImages and possibly others)

SELECT "MigrationId" FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%AddEventImages%';
-- Actual: No rows returned ❌

SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
);
-- Actual: false ❌
```

## EF Core Configuration Ambiguity

```
┌─────────────────────────────────────────────────────────────┐
│  Which folder does EF Core scan for migrations?             │
└─────────────────────────────────────────────────────────────┘

DependencyInjection.cs (line 45):
  npgsqlOptions.MigrationsAssembly("LankaConnect.Infrastructure");
                                       ^
                                       |
          What does this mean? Which folder?
                      |
        ┌─────────────┴─────────────┐
        │                           │
        ▼                           ▼
   Migrations/              Data/Migrations/
   (Old folder)             (New folder)
                               ^
                               |
                    SHOULD BE THIS ONE
                    (matches namespace in files)
```

## Namespace Analysis

### Old Folder Files
```csharp
// File: src/LankaConnect.Infrastructure/Migrations/20251103040053_AddEventImages.cs
namespace LankaConnect.Infrastructure.Data.Migrations  // ← Uses Data.Migrations namespace!
{
    public partial class AddEventImages : Migration { ... }
}
```

### New Folder Files
```csharp
// File: src/LankaConnect.Infrastructure/Data/Migrations/20251109152709_AddNewsletterSubscribers.cs
namespace LankaConnect.Infrastructure.Data.Migrations  // ← Same namespace
{
    public partial class AddNewsletterSubscribers : Migration { ... }
}
```

**Key Insight**: ALL migrations use `LankaConnect.Infrastructure.Data.Migrations` namespace, regardless of physical location!

This means:
1. EF Core can find migrations in BOTH folders (via reflection)
2. BUT: The build/deployment process may only package ONE folder
3. RESULT: Different environments have different migrations applied

## Target State (Fixed)

```
src/LankaConnect.Infrastructure/
└── Data/
    ├── Configurations/
    │   ├── EventConfiguration.cs   ← Updated to use ToJson
    │   └── ...
    └── Migrations/                 ← SINGLE FOLDER (all 34+ migrations)
        ├── 20250830_InitialCreate.cs
        ├── 20250831_InitialMigration.cs
        ├── 20250904_AddCommunicationsTables.cs
        ├── 20251028_AddEntraExternalIdSupport.cs
        ├── 20251031_AddUserProfilePhoto.cs
        ├── 20251031_AddUserLocation.cs
        ├── 20251031_AddUserCulturalInterestsAndLanguages.cs
        ├── 20251101_CreateUserCulturalInterestsAndLanguagesTables.cs
        ├── 20251102_AddEventCategoryAndTicketPrice.cs
        ├── 20251103_AddEventImages.cs   ← NOW VISIBLE IN ALL ENVIRONMENTS
        ├── 20251109_AddNewsletterSubscribers.cs
        ├── 20251110_CreateMetroAreasTable.cs
        ├── 20251110_AddUserPreferredMetroAreas.cs
        ├── 20251111_AddUserRoleUpgradeTracking.cs
        ├── 20251111_AddSubscriptionManagement.cs
        ├── 20251111_AddNotificationsTable.cs
        ├── 20251111_AddEventTemplatesTable.cs
        ├── 20251112_SeedMetroAreasReferenceData.cs
        ├── 20251115_RecreateNewsletterTableFixVersionColumn.cs
        └── 20251203_ConvertTicketPriceToJson.cs  ← NEW (fixes EF Core error)
```

## EventImages Schema

```sql
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

CREATE INDEX "IX_EventImages_EventId"
    ON events."EventImages" ("EventId");

CREATE UNIQUE INDEX "IX_EventImages_EventId_DisplayOrder"
    ON events."EventImages" ("EventId", "DisplayOrder");
```

## Business Rules Enforced by Schema

1. **Primary Key**: Guid-based ID for distributed systems
2. **Foreign Key**: EventId references events.events with CASCADE delete
3. **Unique Constraint**: (EventId, DisplayOrder) prevents duplicate positions
4. **Index**: Fast lookup by EventId for event galleries
5. **DisplayOrder**: Enforces ordering (1, 2, 3, ..., 10)
6. **Domain Invariant**: MAX_IMAGES = 10 (enforced in Event aggregate)

## EF Core Value Object Configuration Patterns

### Pattern 1: Separate Columns (Current TicketPrice)

```csharp
// EventConfiguration.cs (lines 68-79)
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.Property(m => m.Amount)
        .HasColumnName("ticket_price_amount")  // ← Separate column
        .HasPrecision(18, 2);

    money.Property(m => m.Currency)
        .HasColumnName("ticket_price_currency")  // ← Separate column
        .HasConversion<string>()
        .HasMaxLength(3);
});
```

**Schema**:
```sql
events.events
├── ticket_price_amount (numeric(18,2))
└── ticket_price_currency (varchar(3))
```

### Pattern 2: JSONB Storage (Target for Pricing)

```csharp
// EventConfiguration.cs (lines 81-87) - CURRENTLY COMMENTED OUT
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");  // ← Single JSONB column
    // No nested OwnsOne needed - Money objects auto-serialized
});
```

**Schema**:
```sql
events.events
└── pricing (jsonb)
```

**JSON Example**:
```json
{
  "Type": "AgeDual",
  "AdultPrice": {
    "Amount": 50.00,
    "Currency": "USD"
  },
  "ChildPrice": {
    "Amount": 25.00,
    "Currency": "USD"
  },
  "ChildAgeLimit": 12,
  "GroupTiers": []
}
```

### Pattern 3: JSONB with Collections (GroupTiered Pricing)

```json
{
  "Type": "GroupTiered",
  "AdultPrice": {
    "Amount": 0.00,
    "Currency": "USD"
  },
  "ChildPrice": null,
  "ChildAgeLimit": null,
  "GroupTiers": [
    {
      "MinAttendees": 1,
      "MaxAttendees": 2,
      "PricePerPerson": {
        "Amount": 15.00,
        "Currency": "USD"
      }
    },
    {
      "MinAttendees": 3,
      "MaxAttendees": null,
      "PricePerPerson": {
        "Amount": 12.00,
        "Currency": "USD"
      }
    }
  ]
}
```

## Why ToJson Solves the Shared-Type Conflict

### The Problem

```
Event
├── TicketPrice (Money)              ← Configured as OwnsOne with separate columns
└── Pricing (TicketPricing)          ← Configured as OwnsOne with ToJson
    ├── AdultPrice (Money)           ← ❌ EF Core doesn't know how to map this Money
    ├── ChildPrice (Money)           ← ❌ Same issue
    └── GroupTiers (List<GroupPricingTier>)
        └── PricePerPerson (Money)   ← ❌ Same issue
```

**EF Core Error**:
```
Unable to determine the owner for the relationship between 'TicketPricing.AdultPrice' and 'Money'
as both types have been marked as owned.
```

**Root Cause**: Money is used in TWO different ownership contexts:
1. **Direct ownership** (Event.TicketPrice) → Mapped to columns
2. **JSON ownership** (Event.Pricing.AdultPrice) → Serialized to JSONB

EF Core can't resolve which mapping to use for Money inside Pricing.

### The Solution

**Option 1: Convert TicketPrice to ToJson** (Recommended)
```csharp
// Both use ToJson - no ambiguity
builder.OwnsOne(e => e.TicketPrice, money => money.ToJson("ticket_price"));
builder.OwnsOne(e => e.Pricing, pricing => pricing.ToJson("pricing"));
```

**Option 2: Make TicketPrice and Pricing mutually exclusive** (Business logic)
```csharp
// Only configure one at a time based on which is non-null
if (entity.TicketPrice != null)
    builder.OwnsOne(e => e.TicketPrice, money => { ... });
else if (entity.Pricing != null)
    builder.OwnsOne(e => e.Pricing, pricing => pricing.ToJson("pricing"));
```

**Option 3: Rename Money inside Pricing** (Complex, not recommended)
```csharp
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");
    pricing.OwnsOne(p => p.AdultPrice, "Pricing_AdultPrice", money => { ... });
    pricing.OwnsOne(p => p.ChildPrice, "Pricing_ChildPrice", money => { ... });
});
```

## Migration Strategy

### Step 1: Create Migration for TicketPrice Conversion

```bash
dotnet ef migrations add ConvertTicketPriceToJson \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --output-dir Data/Migrations
```

### Step 2: Review Generated Migration

```csharp
public partial class ConvertTicketPriceToJson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new JSONB column
        migrationBuilder.AddColumn<string>(
            name: "ticket_price",
            schema: "events",
            table: "events",
            type: "jsonb",
            nullable: true);

        // Migrate data from old columns to JSONB
        migrationBuilder.Sql(@"
            UPDATE events.events
            SET ticket_price = json_build_object(
                'Amount', ticket_price_amount,
                'Currency', ticket_price_currency
            )::jsonb
            WHERE ticket_price_amount IS NOT NULL;
        ");

        // Drop old columns
        migrationBuilder.DropColumn(
            name: "ticket_price_amount",
            schema: "events",
            table: "events");

        migrationBuilder.DropColumn(
            name: "ticket_price_currency",
            schema: "events",
            table: "events");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse the process
        migrationBuilder.AddColumn<decimal>(
            name: "ticket_price_amount",
            schema: "events",
            table: "events",
            type: "numeric(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ticket_price_currency",
            schema: "events",
            table: "events",
            type: "character varying(3)",
            maxLength: 3,
            nullable: true);

        migrationBuilder.Sql(@"
            UPDATE events.events
            SET
                ticket_price_amount = (ticket_price->>'Amount')::numeric,
                ticket_price_currency = ticket_price->>'Currency'
            WHERE ticket_price IS NOT NULL;
        ");

        migrationBuilder.DropColumn(
            name: "ticket_price",
            schema: "events",
            table: "events");
    }
}
```

### Step 3: Apply Migration

```bash
# Development
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API

# Staging
dotnet ef database update --project src/LankaConnect.Infrastructure --startup-project src/LankaConnect.API --connection "Host=staging-db;Database=lankaconnect_staging;..."
```

## Verification Checklist

### Pre-Deployment
- [ ] All migrations in single folder (Data/Migrations/)
- [ ] Namespace is `LankaConnect.Infrastructure.Data.Migrations` in all files
- [ ] EventConfiguration uses ToJson for TicketPrice and Pricing
- [ ] ConvertTicketPriceToJson migration created and tested
- [ ] All tests passing (0 compilation errors)

### Post-Deployment (Staging)
- [ ] EventImages table exists in staging database
- [ ] Image upload endpoint returns 200 OK
- [ ] TicketPrice data migrated correctly (verify old events)
- [ ] New events with Pricing create successfully
- [ ] __EFMigrationsHistory shows AddEventImages migration
- [ ] Database schema matches dev environment

### Post-Deployment (Production)
- [ ] All staging checks passed
- [ ] Backup created before deployment
- [ ] Migration applied successfully
- [ ] No errors in application logs
- [ ] Event creation/update working
- [ ] Image upload working
- [ ] Dual pricing working

## Monitoring Queries

### Check Migration Status
```sql
-- List all applied migrations
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
ORDER BY "MigrationId" DESC
LIMIT 10;

-- Check for AddEventImages
SELECT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20251103040053_AddEventImages'
) AS "HasEventImagesMigration";

-- Check for ConvertTicketPriceToJson
SELECT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" LIKE '%ConvertTicketPriceToJson%'
) AS "HasTicketPriceJsonMigration";
```

### Check Schema
```sql
-- Check EventImages table
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
) AS "HasEventImagesTable";

-- Check events table columns
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'events' AND table_name = 'events'
  AND column_name IN ('ticket_price', 'pricing', 'ticket_price_amount', 'ticket_price_currency')
ORDER BY ordinal_position;

-- Expected result after migration:
-- ticket_price       | jsonb | YES
-- pricing            | jsonb | YES
-- (ticket_price_amount and ticket_price_currency should NOT exist)
```

### Check Data
```sql
-- Sample ticket price data (JSONB)
SELECT
    "Id",
    "title",
    ticket_price->>'Amount' AS "TicketAmount",
    ticket_price->>'Currency' AS "TicketCurrency",
    pricing->>'Type' AS "PricingType",
    pricing->'AdultPrice'->>'Amount' AS "AdultAmount",
    pricing->'ChildPrice'->>'Amount' AS "ChildAmount"
FROM events.events
WHERE ticket_price IS NOT NULL OR pricing IS NOT NULL
LIMIT 5;
```

## Emergency Rollback Procedure

### If Migration Fails in Staging

**Step 1**: Restore from backup
```bash
pg_restore -U postgres -d lankaconnect_staging -c backup_YYYYMMDD_HHMMSS.dump
```

**Step 2**: Remove failed migration from history
```sql
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251203_ConvertTicketPriceToJson';
```

**Step 3**: Investigate and fix migration script

**Step 4**: Re-apply after fixing

### If EventImages Upload Breaks

**Step 1**: Check table exists
```sql
SELECT * FROM information_schema.tables
WHERE table_schema = 'events' AND table_name = 'EventImages';
```

**Step 2**: If missing, apply migration manually
```bash
dotnet ef migrations script 20251102_AddEventCategoryAndTicketPrice 20251103_AddEventImages \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --output apply_eventimages.sql

psql -U postgres -d lankaconnect_staging -f apply_eventimages.sql
```

**Step 3**: Verify table and restart application
```bash
sudo systemctl restart lankaconnect-api
```

## Related Documents

- [CRITICAL_ISSUES_ANALYSIS.md](./CRITICAL_ISSUES_ANALYSIS.md) - Full analysis and remediation plan
- [ADR-007-Consolidate-Migrations.md](./adr/ADR-007-Consolidate-Migrations.md) - Architecture decision record
- [EventMediaUploadArchitecture.md](./EventMediaUploadArchitecture.md) - Event images feature documentation
