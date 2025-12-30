# Phase 6A.58: Root Cause Analysis - Search API 500 Error

**Date**: 2025-12-30
**Severity**: CRITICAL
**Status**: Analysis Complete - Solution Identified
**Error**: `column e.status does not exist` (PostgreSQL Error 42703)

---

## Executive Summary

The search API endpoint returns HTTP 500 due to a **mixed naming convention in the database schema**. The database contains BOTH PascalCase (`Status`, `Category`, `StartDate`) AND snake_case (`title`, `description`, `search_vector`) column names, causing SQL queries to fail when they assume a consistent convention.

**Root Cause**: Database schema has inconsistent column naming due to:
1. EF Core migrations that didn't explicitly specify column names (defaulted to PascalCase)
2. Explicit `.HasColumnName()` configurations using snake_case
3. Raw SQL queries assuming snake_case throughout

**Impact**: All search functionality broken in production (Azure Container Apps Staging environment).

---

## 1. Root Cause Analysis

### 1.1 The Real Problem: Mixed Naming Convention

**EVIDENCE FROM MIGRATIONS**:

#### Initial Event Table Creation (Migration: `20251102000000_CreateEventsAndRegistrationsTables.cs`)
```sql
-- Snake_case columns (explicit HasColumnName):
title                   -- From EventTitle value object
description             -- From EventDescription value object
address_street          -- From EventLocation value object
coordinates_latitude    -- From EventLocation value object

-- PascalCase columns (NO HasColumnName specified):
Id                      -- Primary key
StartDate              -- Date property
EndDate                -- Date property
OrganizerId            -- Foreign key
Capacity               -- Integer property
Status                 -- Enum property (stored as string)
CancellationReason     -- String property
CreatedAt              -- Audit field
UpdatedAt              -- Audit field
```

#### Category Column Added (Migration: `20251102144315_AddEventCategoryAndTicketPrice.cs`)
```sql
-- PascalCase (NO HasColumnName in EventConfiguration.cs):
Category               -- Enum property (stored as string)

-- Snake_case (part of JSONB value object):
ticket_price_amount
ticket_price_currency
```

#### Full-Text Search Support (Migration: `20251104184035_AddFullTextSearchSupport.cs`)
```sql
-- Snake_case (raw SQL migration):
search_vector          -- Generated tsvector column
```

### 1.2 Why EF Core Configuration Didn't Match Database

**EventConfiguration.cs Analysis**:

```csharp
// Lines 67-72: Category enum - NO HasColumnName()
builder.Property(e => e.Category)
    .HasConversion<string>()
    .HasMaxLength(20)
    .IsRequired()
    .HasDefaultValue(EventCategory.Community);
// Result: EF Core defaults to "Category" (PascalCase)

// Lines 52-57: Status enum - NO HasColumnName()
builder.Property(e => e.Status)
    .HasConversion<string>()
    .HasMaxLength(20)
    .IsRequired()
    .HasDefaultValue(EventStatus.Draft);
// Result: EF Core defaults to "Status" (PascalCase)

// Lines 38-40: StartDate - NO HasColumnName()
builder.Property(e => e.StartDate)
    .IsRequired()
    .HasColumnType("timestamp with time zone");
// Result: EF Core defaults to "StartDate" (PascalCase)

// Lines 20-26: Title value object - HAS HasColumnName()
builder.OwnsOne(e => e.Title, title =>
{
    title.Property(t => t.Value)
        .HasColumnName("title")  // EXPLICIT snake_case
        .HasMaxLength(200)
        .IsRequired();
});
```

**Pattern Identified**:
- **Value Objects**: Explicitly use `.HasColumnName("snake_case")`
- **Simple Properties**: NO `.HasColumnName()` → Default to PascalCase
- **Enums**: NO `.HasColumnName()` → Default to PascalCase
- **Audit Fields**: NO `.HasColumnName()` → Default to PascalCase

### 1.3 Why Previous Fixes Failed

#### Attempt 1 (Commit 1a9d7825): Added Schema Prefix
```sql
-- BEFORE: FROM events e
-- AFTER:  FROM events.events e
```
**Result**: FAILED - Schema prefix correct, but column names still wrong.

#### Attempt 2 (Commit 98c17a42): Used Quoted PascalCase
```sql
-- Changed: e.status → e."Status"
-- Changed: e.category → e."Category"
-- Changed: e.start_date → e."StartDate"
```
**Result**: FAILED - But this was actually CORRECT for the database!

#### Attempt 3 (Commit cb401029): Used snake_case
```sql
-- Changed: e."Status" → e.status
-- Changed: e."Category" → e.category
-- Changed: e."StartDate" → e.start_date
```
**Result**: FAILED - Wrong assumption about database schema.

### 1.4 Deployment Mystery: Why Old Code Still Running?

**Azure Container Logs at 21:07:56 UTC still show**:
```
column e.status does not exist
PostgreSQL Error: 42703
Hint: Perhaps you meant to reference the column "e.Status".
```

**Possible Causes**:
1. **Docker Image Caching**: Azure Container Apps may be pulling cached image
2. **Deployment Lag**: Container restart doesn't guarantee immediate code deployment
3. **Build Cache**: GitHub Actions may have used cached build artifacts
4. **Container Revision**: Azure may still be serving old revision

**Evidence**:
- Commit cb401029 successfully deployed (Run #20604969563)
- Container manually restarted
- BUT error message unchanged (still references `e.status`)

---

## 2. The ACTUAL Database Schema

Based on migration analysis, the **real database schema** is:

### Events Table (`events.events`)

| Column Name | Type | Source |
|------------|------|--------|
| `Id` | uuid | PascalCase (EF default) |
| `title` | varchar(200) | snake_case (HasColumnName) |
| `description` | varchar(2000) | snake_case (HasColumnName) |
| **`StartDate`** | timestamp with time zone | **PascalCase (EF default)** |
| **`EndDate`** | timestamp with time zone | **PascalCase (EF default)** |
| `OrganizerId` | uuid | PascalCase (EF default) |
| `Capacity` | integer | PascalCase (EF default) |
| **`Status`** | varchar(20) | **PascalCase (EF default)** |
| **`Category`** | varchar(20) | **PascalCase (EF default)** |
| `CancellationReason` | varchar(500) | PascalCase (EF default) |
| `has_location` | boolean | snake_case (HasColumnName) |
| `address_street` | varchar(200) | snake_case (HasColumnName) |
| `address_city` | varchar(100) | snake_case (HasColumnName) |
| `address_state` | varchar(100) | snake_case (HasColumnName) |
| `address_zip_code` | varchar(20) | snake_case (HasColumnName) |
| `address_country` | varchar(100) | snake_case (HasColumnName) |
| `coordinates_latitude` | numeric(10,7) | snake_case (HasColumnName) |
| `coordinates_longitude` | numeric(10,7) | snake_case (HasColumnName) |
| `ticket_price` | jsonb | snake_case (ToJson) |
| `pricing` | jsonb | snake_case (ToJson) |
| `search_vector` | tsvector | snake_case (raw SQL) |
| `CreatedAt` | timestamp with time zone | PascalCase (EF default) |
| `UpdatedAt` | timestamp with time zone | PascalCase (EF default) |
| `PublishedAt` | timestamp with time zone | PascalCase (EF default) |

### Key Findings

1. **Enums are PascalCase**: `Status`, `Category` (stored as strings, NOT integers)
2. **Dates are PascalCase**: `StartDate`, `EndDate`, `CreatedAt`, `UpdatedAt`
3. **Search is snake_case**: `search_vector` (generated column)
4. **Value objects are snake_case**: `title`, `description`, `address_*`, `coordinates_*`

---

## 3. PostgreSQL Case Sensitivity Rules

### Unquoted Identifiers
```sql
SELECT Status FROM events.events;  -- Treated as: select status from events.events;
-- PostgreSQL converts to lowercase, fails if actual column is "Status"
```

### Quoted Identifiers
```sql
SELECT "Status" FROM events.events;  -- EXACT match required
-- PostgreSQL preserves case, matches PascalCase column
```

### Mixed Case Columns REQUIRE Quotes
```sql
-- CORRECT for PascalCase columns:
SELECT e."Status", e."Category", e."StartDate"
FROM events.events e;

-- CORRECT for snake_case columns:
SELECT e.title, e.description, e.search_vector
FROM events.events e;
```

---

## 4. Fix Plan (Durable Solution)

### Option A: Fix SQL Queries (RECOMMENDED - Immediate Fix)

**Advantages**:
- No database migration required
- No data migration
- Immediate deployment
- No risk of breaking other queries

**Implementation**:
1. Use quoted PascalCase for enum/date columns
2. Use unquoted snake_case for value object columns
3. Add enum string comparison (NOT integer)

**EventRepository.cs Changes**:
```csharp
var whereConditions = new List<string>
{
    "e.search_vector @@ websearch_to_tsquery('english', {0})",
    @"e.""Status"" = {1}" // Quoted PascalCase, string comparison
};

var parameters = new List<object>
{
    searchTerm,
    EventStatus.Published.ToString() // String, not integer
};

if (category.HasValue)
{
    whereConditions.Add($@"e.""Category"" = {{{parameters.Count}}}");
    parameters.Add(category.Value.ToString()); // String, not integer
}

if (startDateFrom.HasValue)
{
    whereConditions.Add($@"e.""StartDate"" >= {{{parameters.Count}}}");
    parameters.Add(startDateFrom.Value);
}

var eventsSql = $@"
    SELECT e.*
    FROM events.events e
    WHERE {whereClause}
    ORDER BY ts_rank(e.search_vector, websearch_to_tsquery('english', {{0}})) DESC,
             e.""StartDate"" ASC
    LIMIT {{{parameters.Count}}} OFFSET {{{parameters.Count + 1}}}";
```

### Option B: Standardize Database Schema (LONG-TERM - Migration Required)

**Advantages**:
- Consistent naming convention
- Easier to maintain
- Standard PostgreSQL practice

**Disadvantages**:
- Requires migration
- Potential downtime
- Must update all queries
- Risk of breaking changes

**Implementation**:
1. Create migration to rename PascalCase columns to snake_case
2. Update all EF Core configurations with `.HasColumnName("snake_case")`
3. Update all raw SQL queries
4. Update integration tests

**Migration Example**:
```sql
ALTER TABLE events.events RENAME COLUMN "Status" TO status;
ALTER TABLE events.events RENAME COLUMN "Category" TO category;
ALTER TABLE events.events RENAME COLUMN "StartDate" TO start_date;
ALTER TABLE events.events RENAME COLUMN "EndDate" TO end_date;
ALTER TABLE events.events RENAME COLUMN "OrganizerId" TO organizer_id;
ALTER TABLE events.events RENAME COLUMN "Capacity" TO capacity;
ALTER TABLE events.events RENAME COLUMN "CreatedAt" TO created_at;
ALTER TABLE events.events RENAME COLUMN "UpdatedAt" TO updated_at;
ALTER TABLE events.events RENAME COLUMN "PublishedAt" TO published_at;
ALTER TABLE events.events RENAME COLUMN "CancellationReason" TO cancellation_reason;
```

---

## 5. Verification Steps

### Step 1: Verify Database Schema (BEFORE FIX)

**Connect to Azure PostgreSQL**:
```bash
# Get connection string from Azure Portal
psql "<connection_string>"
```

**Query column names**:
```sql
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
ORDER BY ordinal_position;
```

**Expected Output** (confirms mixed case):
```
column_name          | data_type
---------------------|------------------
Id                   | uuid
title                | character varying
description          | character varying
StartDate            | timestamp with time zone
EndDate              | timestamp with time zone
OrganizerId          | uuid
Capacity             | integer
Status               | character varying
Category             | character varying
...
```

### Step 2: Test SQL Query Manually

**Test CORRECT syntax (quoted PascalCase)**:
```sql
SELECT
    e."Id",
    e.title,
    e.description,
    e."Status",
    e."Category",
    e."StartDate"
FROM events.events e
WHERE e.search_vector @@ websearch_to_tsquery('english', 'music')
  AND e."Status" = 'Published'
LIMIT 10;
```

**Test WRONG syntax (unquoted PascalCase)**:
```sql
SELECT
    e.Id,
    e.Status,
    e.Category
FROM events.events e
WHERE e.status = 'Published'; -- Should fail with "column e.status does not exist"
```

### Step 3: Deploy Fix and Verify

**After deploying Option A fix**:

1. **Check Azure Container Logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --follow
```

2. **Test API Endpoint**:
```bash
curl -X GET "https://lankaconnect-api-staging.azurecontainerapps.io/api/events?searchTerm=music" \
  -H "Accept: application/json" \
  -v
```

**Expected Response**: HTTP 200 with search results

3. **Verify No Errors in Logs**:
- Search for "column e.status does not exist"
- Should NOT appear after fix deployed

### Step 4: Integration Test

**Create automated test**:
```csharp
[Fact]
public async Task SearchAsync_WithValidFilters_ReturnsPublishedEvents()
{
    // Arrange
    var searchTerm = "music";
    var category = EventCategory.Entertainment;

    // Act
    var (events, totalCount) = await _eventRepository.SearchAsync(
        searchTerm,
        limit: 10,
        offset: 0,
        category: category,
        isFreeOnly: false,
        startDateFrom: DateTime.UtcNow
    );

    // Assert
    Assert.NotNull(events);
    Assert.All(events, e => Assert.Equal(EventStatus.Published, e.Status));
    if (category.HasValue)
        Assert.All(events, e => Assert.Equal(category.Value, e.Category));
}
```

---

## 6. Prevention Strategy

### 6.1 Enforce Consistent Naming Convention

**Add to project guidelines**:

1. **ALWAYS specify `.HasColumnName()` for ALL properties**:
```csharp
// REQUIRED for all properties
builder.Property(e => e.Status)
    .HasColumnName("status") // EXPLICIT
    .HasConversion<string>()
    .IsRequired();
```

2. **Use snake_case for ALL database columns**:
```csharp
// Standardize on PostgreSQL convention
builder.Property(e => e.StartDate)
    .HasColumnName("start_date") // snake_case
    .HasColumnType("timestamp with time zone");
```

### 6.2 Code Review Checklist

**For EF Core Configurations**:
- [ ] All properties have `.HasColumnName()` specified
- [ ] All column names use snake_case
- [ ] Enums specify string conversion: `.HasConversion<string>()`
- [ ] Value objects use `.HasColumnName()` for nested properties

**For Raw SQL Queries**:
- [ ] Column names match database schema exactly
- [ ] Use quoted identifiers for mixed-case columns (if any remain)
- [ ] Enum comparisons use string values, not integers
- [ ] Schema prefix used: `events.events`

### 6.3 Automated Testing

**Add schema validation tests**:
```csharp
[Fact]
public void EventConfiguration_AllPropertiesHaveColumnName()
{
    var configuration = new EventConfiguration();
    var builder = new ModelBuilder();
    configuration.Configure(builder.Entity<Event>());

    // Verify all properties have explicit column names
    var properties = builder.Model.FindEntityType(typeof(Event))!.GetProperties();
    foreach (var property in properties)
    {
        Assert.NotNull(property.GetColumnName());
        Assert.True(property.GetColumnName() == property.GetColumnName().ToLowerInvariant());
    }
}
```

### 6.4 Migration Review Process

**Before applying migrations**:
1. Review generated SQL for column naming consistency
2. Check for mixed case column names
3. Verify migrations match EF Core configuration
4. Test migrations on local PostgreSQL instance first

---

## 7. Lessons Learned

### 7.1 PostgreSQL Case Sensitivity

**Key Insight**: PostgreSQL converts unquoted identifiers to lowercase, but EF Core defaults to PascalCase when `.HasColumnName()` not specified.

**Impact**: Migrations created PascalCase columns, but developers assumed snake_case.

**Solution**: ALWAYS be explicit with `.HasColumnName()`.

### 7.2 Enum Storage in PostgreSQL

**Key Insight**: EF Core stores enums as strings (when using `.HasConversion<string>()`), NOT integers.

**Impact**: SQL queries using `e.Status = 1` fail, must use `e.Status = 'Published'`.

**Solution**: Use `.ToString()` for enum values in raw SQL.

### 7.3 Deployment Verification

**Key Insight**: Successful deployment doesn't guarantee code is running.

**Impact**: Multiple failed fix attempts because old code still running.

**Solution**:
1. Check Azure Container revision history
2. Verify container image digest changed
3. Test API endpoint immediately after deployment
4. Monitor logs for specific error patterns

### 7.4 PostgreSQL Error Messages Are Accurate

**Key Insight**: PostgreSQL said "Perhaps you meant to reference the column \"e.Status\"" - this was literally correct.

**Impact**: Ignored the hint, assumed it was a generic message.

**Solution**: Trust PostgreSQL error messages, they're usually right.

---

## 8. Related Files

- **Repository**: `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs` (Lines 279-357)
- **Configuration**: `src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs`
- **Migration (Initial)**: `src/LankaConnect.Infrastructure/Migrations/20251102000000_CreateEventsAndRegistrationsTables.cs`
- **Migration (Category)**: `src/LankaConnect.Infrastructure/Migrations/20251102144315_AddEventCategoryAndTicketPrice.cs`
- **Migration (Search)**: `src/LankaConnect.Infrastructure/Migrations/20251104184035_AddFullTextSearchSupport.cs`

---

## 9. Next Steps

1. **Immediate**: Implement Option A (Fix SQL queries)
2. **Short-term**: Verify deployment process (why old code ran)
3. **Long-term**: Plan Option B (Standardize schema to snake_case)
4. **Documentation**: Update developer guidelines with naming conventions

---

## Appendix A: Commit History

| Commit | Description | Result |
|--------|-------------|--------|
| 1a9d7825 | Added schema prefix `events.events` | FAILED - Column names still wrong |
| 98c17a42 | Used quoted PascalCase `e."Status"` | FAILED - But syntax was CORRECT! |
| cb401029 | Changed to snake_case `e.status` | FAILED - Wrong assumption |

---

## Appendix B: PostgreSQL Documentation References

- [Case Sensitivity in PostgreSQL](https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS)
- [Quoted Identifiers](https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS)
- [Full-Text Search](https://www.postgresql.org/docs/current/textsearch.html)

---

**Analysis Completed**: 2025-12-30
**Analyst**: System Architect
**Severity**: CRITICAL
**Recommended Action**: Implement Option A immediately, plan Option B for next sprint
