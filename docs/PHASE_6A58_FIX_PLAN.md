# Phase 6A.58: Fix Plan - Search API Column Naming

**Date**: 2025-12-30
**Priority**: P0 - CRITICAL
**Issue**: Search API returns 500 due to mixed PascalCase/snake_case columns
**Root Cause**: See [PHASE_6A58_ROOT_CAUSE_ANALYSIS.md](./PHASE_6A58_ROOT_CAUSE_ANALYSIS.md)

---

## Quick Reference

**Problem**: Database has mixed naming convention (PascalCase + snake_case)
**Solution**: Fix SQL queries to use correct column names with quotes
**Timeline**: Immediate (1-2 hours)
**Risk Level**: LOW (SQL-only changes)

---

## Fix Plan: Option A (RECOMMENDED)

### Overview

Fix the raw SQL queries in `EventRepository.SearchAsync()` to use:
1. **Quoted PascalCase** for enum/date columns: `"Status"`, `"Category"`, `"StartDate"`
2. **Unquoted snake_case** for value object columns: `search_vector`, `title`, `description`
3. **String values** for enum comparisons (NOT integers)

### Implementation Steps

#### Step 1: Verify Current Database Schema (5 minutes)

**Purpose**: Confirm actual column naming before making changes.

**Query**:
```sql
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
  AND column_name IN ('Status', 'status', 'Category', 'category', 'StartDate', 'start_date')
ORDER BY column_name;
```

**Expected Result**:
```
column_name | data_type
------------|------------------
Category    | character varying
StartDate   | timestamp with time zone
Status      | character varying
```

If you see `status`, `category`, `start_date` instead, then the database was migrated and you need Option B instead.

#### Step 2: Update EventRepository.cs (15 minutes)

**File**: `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`
**Lines**: 279-357

**Changes**:

```csharp
public async Task<(IReadOnlyList<Event> Events, int TotalCount)> SearchAsync(
    string searchTerm,
    int limit,
    int offset,
    EventCategory? category = null,
    bool? isFreeOnly = null,
    DateTime? startDateFrom = null,
    CancellationToken cancellationToken = default)
{
    // Build the WHERE clause dynamically based on filters
    // Phase 6A.58 FIX: Use correct column names - quoted PascalCase for enums/dates, snake_case for value objects
    var whereConditions = new List<string>
    {
        "e.search_vector @@ websearch_to_tsquery('english', {0})",
        @"e.""Status"" = {1}" // Quoted PascalCase, string comparison
    };

    var parameters = new List<object>
    {
        searchTerm,
        EventStatus.Published.ToString() // String value: "Published"
    };

    // Add category filter if provided
    if (category.HasValue)
    {
        whereConditions.Add($@"e.""Category"" = {{{parameters.Count}}}");
        parameters.Add(category.Value.ToString()); // String value: "Entertainment", "Community", etc.
    }

    // Add free-only filter if provided
    if (isFreeOnly.HasValue && isFreeOnly.Value)
    {
        // ticket_price is JSONB column (snake_case), access Amount property
        whereConditions.Add("(e.ticket_price->>'Amount')::numeric = 0");
    }

    // Add start date filter if provided
    if (startDateFrom.HasValue)
    {
        whereConditions.Add($@"e.""StartDate"" >= {{{parameters.Count}}}");
        parameters.Add(startDateFrom.Value);
    }

    var whereClause = string.Join(" AND ", whereConditions);

    // Query for events with ranking
    var eventsSql = $@"
        SELECT e.*
        FROM events.events e
        WHERE {whereClause}
        ORDER BY ts_rank(e.search_vector, websearch_to_tsquery('english', {{0}})) DESC,
                 e.""StartDate"" ASC
        LIMIT {{{parameters.Count}}} OFFSET {{{parameters.Count + 1}}}";

    parameters.Add(limit);
    parameters.Add(offset);

    var events = await _dbSet
        .FromSqlRaw(eventsSql, parameters.ToArray())
        .AsNoTracking()
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .ToListAsync(cancellationToken);

    // Count query (same filters, no ranking needed)
    var countSql = $@"
        SELECT COUNT(*)
        FROM events.events e
        WHERE {whereClause}";

    // Remove limit and offset parameters for count query
    var countParameters = parameters.Take(parameters.Count - 2).ToArray();

    var totalCount = await _context.Database
        .SqlQueryRaw<int>(countSql, countParameters)
        .FirstOrDefaultAsync(cancellationToken);

    return (events, totalCount);
}
```

**Key Changes**:
1. Line 293: `e.status` → `e."Status"` (quoted PascalCase)
2. Line 299: `EventStatus.Published.ToString()` instead of enum integer
3. Line 305: `e.category` → `e."Category"` (quoted PascalCase)
4. Line 306: `category.Value.ToString()` instead of enum integer
5. Line 319: `e.start_date` → `e."StartDate"` (quoted PascalCase)
6. Line 330: `e.start_date` → `e."StartDate"` (quoted PascalCase)

#### Step 3: Create Integration Test (10 minutes)

**File**: `tests/LankaConnect.IntegrationTests/Phase6A58VerificationTests.cs`

```csharp
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LankaConnect.IntegrationTests;

public class Phase6A58VerificationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly EventRepository _repository;

    public Phase6A58VerificationTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new EventRepository(_context);
    }

    [Fact]
    public async Task SearchAsync_WithSearchTerm_ReturnsResults()
    {
        // Arrange
        var searchTerm = "music";

        // Act
        var (events, totalCount) = await _repository.SearchAsync(
            searchTerm,
            limit: 10,
            offset: 0
        );

        // Assert
        Assert.NotNull(events);
        Assert.True(totalCount >= 0);
        Assert.All(events, e => Assert.Equal(EventStatus.Published, e.Status));
    }

    [Fact]
    public async Task SearchAsync_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        var searchTerm = "event";
        var category = EventCategory.Community;

        // Act
        var (events, totalCount) = await _repository.SearchAsync(
            searchTerm,
            limit: 10,
            offset: 0,
            category: category
        );

        // Assert
        Assert.NotNull(events);
        Assert.All(events, e => Assert.Equal(EventStatus.Published, e.Status));
        Assert.All(events, e => Assert.Equal(category, e.Category));
    }

    [Fact]
    public async Task SearchAsync_WithDateFilter_ReturnsUpcomingEvents()
    {
        // Arrange
        var searchTerm = "event";
        var startDateFrom = DateTime.UtcNow;

        // Act
        var (events, totalCount) = await _repository.SearchAsync(
            searchTerm,
            limit: 10,
            offset: 0,
            startDateFrom: startDateFrom
        );

        // Assert
        Assert.NotNull(events);
        Assert.All(events, e => Assert.True(e.StartDate >= startDateFrom));
    }

    [Fact]
    public async Task SearchAsync_WithAllFilters_ReturnsCorrectResults()
    {
        // Arrange
        var searchTerm = "event";
        var category = EventCategory.Community;
        var isFreeOnly = true;
        var startDateFrom = DateTime.UtcNow;

        // Act
        var (events, totalCount) = await _repository.SearchAsync(
            searchTerm,
            limit: 10,
            offset: 0,
            category: category,
            isFreeOnly: isFreeOnly,
            startDateFrom: startDateFrom
        );

        // Assert
        Assert.NotNull(events);
        Assert.All(events, e =>
        {
            Assert.Equal(EventStatus.Published, e.Status);
            Assert.Equal(category, e.Category);
            Assert.True(e.StartDate >= startDateFrom);
            // Free event check: TicketPrice.Amount == 0 or Pricing.Type == Free
        });
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

#### Step 4: Local Testing (10 minutes)

**Build and test**:
```bash
# Build project
dotnet build

# Run integration tests
dotnet test --filter "FullyQualifiedName~Phase6A58VerificationTests"

# Expected: All tests pass
```

**Manual API test**:
```bash
# Start API locally
cd src/LankaConnect.API
dotnet run

# Test search endpoint
curl -X GET "http://localhost:5000/api/events?searchTerm=music" \
  -H "Accept: application/json"

# Expected: HTTP 200 with search results
```

#### Step 5: Deployment (15 minutes)

**Commit and push**:
```bash
git add src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs
git add tests/LankaConnect.IntegrationTests/Phase6A58VerificationTests.cs
git commit -m "fix(phase-6a58): Fix SQL column naming for PostgreSQL mixed case schema

- Use quoted PascalCase for enum/date columns: \"Status\", \"Category\", \"StartDate\"
- Use unquoted snake_case for value objects: search_vector, ticket_price
- Fix enum comparisons to use string values instead of integers
- Add comprehensive integration tests for search functionality

Root Cause: Database has mixed PascalCase (Status, Category, StartDate) and
snake_case (title, description, search_vector) columns due to inconsistent
EF Core configuration. PostgreSQL requires quoted identifiers for mixed case.

Refs: docs/PHASE_6A58_ROOT_CAUSE_ANALYSIS.md"

git push origin develop
```

**Monitor deployment**:
```bash
# Check GitHub Actions
# URL: https://github.com/your-repo/actions

# Wait for deployment to complete
# Expected: Run completes successfully
```

**Force container restart** (if needed):
```bash
# Azure CLI
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --revision <latest-revision-name>

# Or via Azure Portal:
# 1. Navigate to Container App
# 2. Go to "Revisions and replicas"
# 3. Select latest revision
# 4. Click "Restart"
```

#### Step 6: Verification in Production (10 minutes)

**Test API endpoint**:
```bash
curl -X GET "https://lankaconnect-api-staging.azurecontainerapps.io/api/events?searchTerm=music" \
  -H "Accept: application/json" \
  -v

# Expected: HTTP 200 with JSON response
```

**Check Azure Container Logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --follow

# Look for:
# - NO "column e.status does not exist" errors
# - Successful search query logs
# - HTTP 200 responses
```

**Verify with different filters**:
```bash
# Category filter
curl -X GET "https://lankaconnect-api-staging.../api/events?searchTerm=event&category=Community"

# Date filter
curl -X GET "https://lankaconnect-api-staging.../api/events?searchTerm=event&startDateFrom=2025-12-30T00:00:00Z"

# Free only filter
curl -X GET "https://lankaconnect-api-staging.../api/events?searchTerm=event&isFreeOnly=true"

# All filters combined
curl -X GET "https://lankaconnect-api-staging.../api/events?searchTerm=music&category=Entertainment&isFreeOnly=true&startDateFrom=2025-12-30T00:00:00Z"

# All should return HTTP 200
```

---

## Rollback Plan

If deployment fails or causes issues:

**Step 1: Revert commit**:
```bash
git revert HEAD
git push origin develop
```

**Step 2: Monitor deployment**:
- Wait for GitHub Actions to complete
- Verify container restarts with previous code

**Step 3: Investigate**:
- Check if database schema is different than expected
- Run schema verification query (Step 1)
- Review Azure logs for specific error

---

## Alternative Fix: Option B (Long-term)

If Option A doesn't work (database already has snake_case columns), implement Option B:

### Step 1: Verify Database Has snake_case

**Query**:
```sql
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
  AND column_name IN ('status', 'category', 'start_date');
```

**If this returns results**, database is already snake_case.

### Step 2: Remove Quotes from SQL

**EventRepository.cs changes**:
```csharp
var whereConditions = new List<string>
{
    "e.search_vector @@ websearch_to_tsquery('english', {0})",
    "CAST(e.status AS text) = {1}" // Unquoted snake_case
};

// ...

whereConditions.Add($"CAST(e.category AS text) = {{{parameters.Count}}}");

// ...

whereConditions.Add($"e.start_date >= {{{parameters.Count}}}");

// ...

var eventsSql = $@"
    SELECT e.*
    FROM events.events e
    WHERE {whereClause}
    ORDER BY ts_rank(e.search_vector, websearch_to_tsquery('english', {{0}})) DESC,
             e.start_date ASC
    LIMIT {{{parameters.Count}}} OFFSET {{{parameters.Count + 1}}}";
```

### Step 3: Add CAST for Enum Comparisons

**Important**: If enums are stored as strings, you MUST cast:
```sql
CAST(e.status AS text) = 'Published'
CAST(e.category AS text) = 'Community'
```

Without CAST, PostgreSQL may fail if column type is enum or varchar.

---

## Post-Deployment Tasks

### 1. Update Documentation

**Files to update**:
- [ ] `docs/PHASE_6A58_ROOT_CAUSE_ANALYSIS.md` - Add deployment outcome
- [ ] `docs/PROGRESS_TRACKER.md` - Mark Phase 6A.58 as Complete
- [ ] `docs/STREAMLINED_ACTION_PLAN.md` - Update status

### 2. Create Follow-up Tasks

**Long-term improvements**:
- [ ] Standardize database schema to snake_case (Option B full implementation)
- [ ] Add EF Core schema validation tests
- [ ] Update developer guidelines with naming conventions
- [ ] Review all raw SQL queries for similar issues

### 3. Performance Monitoring

**After deployment**:
- Monitor search API response times
- Check for any new errors in logs
- Verify search ranking is correct
- Test pagination with large result sets

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Wrong column names | LOW | HIGH | Verified with schema query |
| Deployment caching | MEDIUM | HIGH | Force container restart |
| Enum comparison fails | LOW | HIGH | Use string values, not integers |
| Other queries break | LOW | MEDIUM | Only changes SearchAsync method |
| Performance regression | LOW | LOW | No query logic changes |

---

## Success Criteria

- [ ] Search API returns HTTP 200 with valid results
- [ ] No "column e.status does not exist" errors in logs
- [ ] All integration tests pass
- [ ] Search filters work correctly (category, date, free)
- [ ] Search ranking is accurate (ts_rank)
- [ ] Pagination works (limit/offset)
- [ ] No performance degradation

---

## Timeline

| Step | Duration | Status |
|------|----------|--------|
| 1. Verify schema | 5 min | Pending |
| 2. Update code | 15 min | Pending |
| 3. Create tests | 10 min | Pending |
| 4. Local testing | 10 min | Pending |
| 5. Deployment | 15 min | Pending |
| 6. Verification | 10 min | Pending |
| **TOTAL** | **65 min** | **Pending** |

---

## Next Actions

1. Execute Step 1: Verify database schema
2. Based on results, choose Option A or Option B
3. Implement code changes
4. Test locally
5. Deploy to staging
6. Verify in production
7. Update documentation

---

**Plan Created**: 2025-12-30
**Estimated Completion**: 2025-12-30 (same day)
**Risk Level**: LOW
**Confidence**: HIGH (root cause clearly identified)
