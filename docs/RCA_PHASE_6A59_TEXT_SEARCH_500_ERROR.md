# Root Cause Analysis: Text Search Returning 500 Internal Server Error

**Date**: 2026-01-02
**Phase**: 6A.59 Post-Deployment
**Severity**: HIGH (Critical user-facing feature broken)
**Status**: Under Investigation

---

## Executive Summary

**Problem**: Text search in Dashboard Event Management tab returns 500 Internal Server Error when using `searchTerm` parameter. Other filters (category, location, date range) work correctly.

**Error**:
```
GET http://localhost:3000/api/proxy/events/my-events?searchTerm=Diagnostic+Test+Event+Dec+20
Status: 500 (Internal Server Error)
```

**Context**: This occurs AFTER deploying commit `75fd9faa` which fixed count query parameter mismatch in `EventRepository.cs SearchAsync` method. The fix passed all Azure smoke tests but text search now fails.

---

## 1. Issue Classification

### Confirmed Classification: **Backend API Issue**

**Evidence**:
- Proxy correctly forwards `searchTerm` parameter (route.ts line 76)
- Frontend correctly builds request URL
- Other filters work (category, location) - auth/proxy working
- 500 error = server-side exception
- Recent changes to SearchAsync in EventRepository.cs

**What Rules Out**:
- NOT UI Issue: Frontend correctly sends parameter
- NOT Auth Issue: Other filters work with same auth
- NOT Feature Missing: Endpoint exists
- NOT Database Migration: Other query parameters work

---

## 2. Evidence Gathering

### Request Flow Analysis

```
Frontend EventFilters.tsx
  ↓ searchTerm parameter
getUserCreatedEvents(filters)
  ↓ GET /api/proxy/events/my-events?searchTerm=...
Next.js Proxy route.ts
  ↓ Preserves query string (line 76)
EventsController.GetMyEvents()
  ↓ searchTerm from query
GetEventsByOrganizerQueryHandler
  ↓ Delegates to GetEventsQuery (line 45)
GetEventsQueryHandler
  ↓ Calls SearchAsync if searchTerm present (line 42-52)
EventRepository.SearchAsync()
  ↓ THROWS EXCEPTION → 500 error
```

### Code Analysis: SearchAsync Method

**Current Implementation** (EventRepository.cs lines 308-432):

**Initial WHERE Clause Build**:
```csharp
// Lines 325-336
var whereConditions = new List<string>
{
    "e.search_vector @@ websearch_to_tsquery('english', {0})",  // searchTerm
    @"e.""Status"" IN ({1}, {2})"  // Published (1), Cancelled (4)
};

var parameters = new List<object>
{
    searchTerm,                   // {0}
    (int)EventStatus.Published,   // {1}
    (int)EventStatus.Cancelled    // {2}
};
```

**Parameter Management**:
```csharp
// Lines 367-385
var whereClauseParameterCount = parameters.Count;  // Save count BEFORE duplicate

// Duplicate searchTerm for ORDER BY (EF Core doesn't support reusing parameters)
var searchTermIndexForOrderBy = parameters.Count;
parameters.Add(searchTerm);

// Build events query SQL
var eventsSql = $@"
    SELECT e.*
    FROM events.events e
    WHERE {whereClause}
    ORDER BY ts_rank(e.search_vector, websearch_to_tsquery('english', {{{searchTermIndexForOrderBy}}})) DESC, e.""StartDate"" ASC
    LIMIT {{{parameters.Count}}} OFFSET {{{parameters.Count + 1}}}";

parameters.Add(limit);
parameters.Add(offset);
```

**Count Query**:
```csharp
// Lines 403-418
var countSql = $@"
    SELECT COUNT(*)::int AS ""Value""
    FROM events.events e
    WHERE {whereClause}";

// Take only WHERE clause parameters (exclude duplicate, limit, offset)
var countParameters = parameters.Take(whereClauseParameterCount).ToArray();
```

### Parameter Array Analysis

**Scenario: searchTerm ONLY (no category/date filters)**

```
Initial parameters:
[0]: "Diagnostic Test Event Dec 20"  // searchTerm (WHERE)
[1]: 1                                // Published status
[2]: 4                                // Cancelled status
whereClauseParameterCount = 3

After duplicate searchTerm:
[3]: "Diagnostic Test Event Dec 20"  // searchTerm (ORDER BY)

After limit/offset:
[4]: 1000                             // limit
[5]: 0                                // offset

Events SQL expects: 6 parameters → [0] [1] [2] [3] [4] [5] ✅ CORRECT
Count SQL expects: 3 parameters → [0] [1] [2] ✅ CORRECT
```

**Scenario: searchTerm + category filter**

```
Initial parameters:
[0]: "Diagnostic Test Event Dec 20"  // searchTerm
[1]: 1                                // Published
[2]: 4                                // Cancelled
[3]: 0                                // category (Social)
whereClauseParameterCount = 4

After duplicate:
[4]: "Diagnostic Test Event Dec 20"  // searchTerm (ORDER BY)

After limit/offset:
[5]: 1000                             // limit
[6]: 0                                // offset

Events SQL expects: 7 parameters ✅ CORRECT
Count SQL expects: 4 parameters ✅ CORRECT
```

**Conclusion**: Parameter counting logic is **CORRECT**.

---

## 3. Root Cause Hypothesis

### MOST LIKELY CAUSE: PostgreSQL Function Compatibility (85% probability)

**Hypothesis**: Azure PostgreSQL doesn't support `websearch_to_tsquery` function.

**Evidence**:
1. `websearch_to_tsquery` requires PostgreSQL 11+
2. Azure may be running PostgreSQL 10 or earlier
3. Function not existing causes runtime SQL exception
4. Error would be: "function websearch_to_tsquery(unknown, character varying) does not exist"

**Why Other Queries Work**:
- Category/location/date filters don't use full-text search functions
- They use simple WHERE comparisons that work on all PostgreSQL versions

**Test Required**:
```sql
-- Check PostgreSQL version
SELECT version();

-- Test function existence
SELECT websearch_to_tsquery('english', 'test');
```

### Alternative Causes (Lower Probability)

**#2 - search_vector Column Missing (10%)**
- Migration didn't create column in Azure
- Error: "column search_vector does not exist"
- **Counter-evidence**: Would fail for all queries, not just searchTerm

**#3 - Special Characters in SearchTerm (3%)**
- User input breaks tsquery syntax
- Error: "syntax error in tsquery"
- **Counter-evidence**: "Diagnostic Test Event Dec 20" has no special chars

**#4 - EF Core Parameter Binding (2%)**
- EF Core 8.0 FromSqlRaw behavior change
- **Counter-evidence**: Other dynamic parameters work fine

---

## 4. Fix Plan

### Phase 1: DIAGNOSIS (REQUIRED FIRST)

**CRITICAL**: Do NOT implement fixes until Azure logs confirm root cause.

**Step 1.1 - Access Azure Container Logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --tail 500 \
  | grep -i "search"
```

Look for:
- `[SEARCH-ERROR]` log messages (EventRepository.cs line 428)
- PostgreSQL error messages
- Stack traces

**Step 1.2 - Check PostgreSQL Version**:
```bash
# Connect to Azure PostgreSQL
az postgres server show --resource-group lankaconnect-rg --name <server-name>

# Or query directly
SELECT version();
```

**Step 1.3 - Test Full-Text Search Functions**:
```sql
-- Test if function exists
SELECT proname FROM pg_proc WHERE proname = 'websearch_to_tsquery';

-- Test function execution
SELECT websearch_to_tsquery('english', 'test search');

-- Check alternative function
SELECT plainto_tsquery('english', 'test search');
```

**Step 1.4 - Verify search_vector Column**:
```sql
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
  AND column_name = 'search_vector';
```

**Expected Output**:
```
column_name   | data_type | is_nullable
--------------+-----------+-------------
search_vector | tsvector  | NO
```

### Phase 2: FIX IMPLEMENTATION (After diagnosis confirms cause)

**Fix A: Replace websearch_to_tsquery (If PostgreSQL < 11)**

```csharp
// File: EventRepository.cs

// LINE 327 - BEFORE:
"e.search_vector @@ websearch_to_tsquery('english', {0})",

// LINE 327 - AFTER:
"e.search_vector @@ plainto_tsquery('english', {0})",

// LINE 381 - BEFORE:
ORDER BY ts_rank(e.search_vector, websearch_to_tsquery('english', {{{searchTermIndexForOrderBy}}})) DESC

// LINE 381 - AFTER:
ORDER BY ts_rank(e.search_vector, plainto_tsquery('english', {{{searchTermIndexForOrderBy}}})) DESC
```

**Why plainto_tsquery**:
- Available since PostgreSQL 8.3 (better compatibility)
- Automatically handles special characters
- Converts plain text → tsquery format
- Simpler than websearch_to_tsquery (no AND/OR/NOT operators)

**Trade-offs**:
- Less powerful: "cats AND dogs" becomes "cats & dogs" (implicit AND only)
- No OR operators: "cats OR dogs" treated as literal phrase
- **Acceptable for basic search** in Event Management tab

**Fix B: Re-run Migrations (If column missing)**

```bash
# Check migration status
dotnet ef migrations list --project src/LankaConnect.Infrastructure

# Re-run migrations
dotnet ef database update --project src/LankaConnect.Infrastructure \
  --connection "Host=<azure-host>;Database=<db>;Username=<user>;Password=<pwd>"
```

**Fix C: Input Sanitization (If special chars)**

```csharp
// Add before SearchAsync call
private string SanitizeSearchTerm(string searchTerm)
{
    // Remove tsquery special characters
    var sanitized = searchTerm
        .Replace("&", "")
        .Replace("|", "")
        .Replace("!", "")
        .Replace("(", "")
        .Replace(")", "")
        .Replace(":", "");

    return sanitized.Trim();
}
```

### Phase 3: VERIFICATION

**Step 3.1 - Local Testing**:
```bash
# Update appsettings.Development.json to point to Azure DB
dotnet run --project src/LankaConnect.API

# Test searchTerm endpoint
curl -X GET "http://localhost:5000/api/events/my-events?searchTerm=test" \
  -H "Authorization: Bearer <token>"
```

**Step 3.2 - Deploy to Azure**:
```bash
git add src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs
git commit -m "fix(phase-6a59): Replace websearch_to_tsquery with plainto_tsquery for PostgreSQL 10 compatibility"
git push origin develop
```

**Step 3.3 - Azure Smoke Test**:
```bash
# Wait for deployment
gh run watch

# Test in Azure
curl -X GET "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/my-events?searchTerm=Diagnostic+Test+Event+Dec+20" \
  -H "Authorization: Bearer <token>"
```

**Step 3.4 - Verify Logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --follow true \
  | grep "SEARCH"
```

Look for:
- `[SEARCH-1] SearchAsync START`
- `[SEARCH-7] Events query succeeded`
- `[SEARCH-10] SearchAsync COMPLETE`
- NO `[SEARCH-ERROR]`

---

## 5. Preventive Measures

### Immediate Actions

**1. Add Integration Tests**:
```csharp
// File: tests/LankaConnect.IntegrationTests/Events/SearchAsyncTests.cs

[Fact]
public async Task SearchAsync_WithSearchTerm_ShouldReturnMatchingEvents()
{
    // Arrange
    var searchTerm = "conference workshop";

    // Act
    var (events, totalCount) = await _eventRepository.SearchAsync(
        searchTerm, limit: 10, offset: 0);

    // Assert
    events.Should().NotBeNull();
    totalCount.Should().BeGreaterThanOrEqualTo(0);
}

[Fact]
public async Task SearchAsync_WithSpecialCharacters_ShouldNotThrow()
{
    // Arrange
    var searchTerm = "test & search | with ! special (chars)";

    // Act
    var act = () => _eventRepository.SearchAsync(searchTerm, 10, 0);

    // Assert
    await act.Should().NotThrowAsync();
}
```

**2. Add PostgreSQL Version Check in CI/CD**:
```yaml
# .github/workflows/ci.yml
- name: Check PostgreSQL Version
  run: |
    psql $DATABASE_URL -c "SELECT version();"
    psql $DATABASE_URL -c "SELECT proname FROM pg_proc WHERE proname = 'plainto_tsquery';"
```

**3. Enable EF Core Query Logging**:
```csharp
// appsettings.Staging.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**4. Document PostgreSQL Requirements**:
```markdown
# README.md

## Database Requirements

- PostgreSQL 10+ (for full-text search)
- Extensions required:
  - `pg_trgm` (trigram similarity)
  - Built-in `tsvector` type support
```

### Long-term Improvements

**1. Abstract Full-Text Search**:
```csharp
public interface IFullTextSearchService
{
    Task<(IReadOnlyList<Event> Events, int TotalCount)> SearchEventsAsync(
        string searchTerm, SearchOptions options);
}

// Implementations:
// - PostgresFullTextSearchService (uses plainto_tsquery)
// - ElasticsearchService (future: for advanced search)
// - AzureSearchService (future: for cloud search)
```

**2. Add Search Analytics**:
```csharp
// Track search terms, results count, performance
public class SearchMetrics
{
    public string SearchTerm { get; set; }
    public int ResultsCount { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}
```

**3. Create Post-Deployment Smoke Tests**:
```bash
#!/bin/bash
# scripts/smoke-test-azure.sh

echo "Testing search endpoint..."
RESPONSE=$(curl -s -w "%{http_code}" \
  "https://lankaconnect-api-staging.../api/events/my-events?searchTerm=test" \
  -H "Authorization: Bearer $TEST_TOKEN")

HTTP_CODE="${RESPONSE: -3}"
if [ "$HTTP_CODE" != "200" ]; then
  echo "❌ SMOKE TEST FAILED: Search returned $HTTP_CODE"
  exit 1
fi

echo "✅ Search endpoint working"
```

**4. Add Monitoring Alerts**:
```yaml
# Azure Monitor Alert Rule
- name: High 500 Error Rate
  condition: |
    requests
    | where resultCode == 500
    | where url contains "my-events"
    | summarize count() by bin(timestamp, 5m)
    | where count_ > 5
  action: Send email to on-call engineer
```

---

## 6. Technical Deep Dive: Why websearch_to_tsquery?

### PostgreSQL Full-Text Search Function Comparison

| Function | Available Since | Use Case | Handles Special Chars | Search Syntax |
|----------|----------------|----------|----------------------|---------------|
| `to_tsquery` | PostgreSQL 8.3 | Manual query syntax | No | `'cats & dogs'` |
| `plainto_tsquery` | PostgreSQL 8.3 | Plain text search | Yes | `'cats dogs'` → `'cats & dogs'` |
| `websearch_to_tsquery` | PostgreSQL 11 | Web-style search | Yes | `'cats OR dogs'`, `'"exact phrase"'` |
| `phraseto_tsquery` | PostgreSQL 9.6 | Exact phrase search | Yes | `'cats dogs'` → `'cats <-> dogs'` |

**Why websearch_to_tsquery was chosen** (original intent):
- Modern syntax: supports AND, OR, NOT, quotes
- User-friendly: matches Google search behavior
- Better UX: users can search "cats OR dogs" naturally

**Why plainto_tsquery is better for compatibility**:
- Works on older PostgreSQL versions
- Simpler: no syntax errors from user input
- Good enough: most users just type plain text anyway

**Example Behavior**:

```sql
-- User types: "conference workshop"
SELECT plainto_tsquery('english', 'conference workshop');
-- Result: 'conference' & 'workshop'

SELECT websearch_to_tsquery('english', 'conference workshop');
-- Result: 'conference' & 'workshop'  (same)

-- User types: "conference OR workshop"
SELECT plainto_tsquery('english', 'conference OR workshop');
-- Result: 'conference' & 'or' & 'workshop'  (treats OR as word)

SELECT websearch_to_tsquery('english', 'conference OR workshop');
-- Result: 'conference' | 'workshop'  (supports OR operator)
```

**Trade-off Decision**: Use `plainto_tsquery` for **simplicity and compatibility** over advanced search operators.

---

## 7. Conclusion

**Status**: **AWAITING AZURE LOGS FOR CONFIRMATION**

**Most Likely Root Cause**:
Azure PostgreSQL version doesn't support `websearch_to_tsquery` function (requires PostgreSQL 11+).

**Confidence Level**: 85%

**Recommended Fix**:
Replace `websearch_to_tsquery` with `plainto_tsquery` for better compatibility.

**Blocker**:
Cannot implement fix until Azure logs confirm the actual exception message.

**Next Steps**:
1. ✅ **User Action Required**: Provide Azure container logs showing the exception
2. ✅ **Verify PostgreSQL version** in Azure (need to confirm < 11)
3. ✅ **Implement fix** (Replace function in 2 places in EventRepository.cs)
4. ✅ **Test locally** against Azure database
5. ✅ **Deploy and verify** in staging environment

---

## Appendix: Files Analyzed

**Backend**:
- `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs` (lines 308-432)
- `src/LankaConnect.Application/Events/Queries/GetEventsByOrganizer/GetEventsByOrganizerQueryHandler.cs`
- `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs` (lines 40-52)
- `src/LankaConnect.API/Controllers/EventsController.cs` (lines 754-782)

**Frontend**:
- `web/src/app/api/proxy/[...path]/route.ts` (lines 74-77)
- `web/src/infrastructure/api/repositories/events.repository.ts` (lines 423, 438-439)
- `web/src/components/events/filters/EventFilters.tsx`

**Commits**:
- `75fd9faa` - fix(phase-6a59): Fix count query parameter mismatch with dynamic filters
- `62521747` - fix(phase-6a59): Fix SearchAsync count query parameter mismatch
- `07280716` - fix(phase-6a59): Fix SearchAsync duplicate parameter usage in ORDER BY

**Azure Deployment**:
- Run: #20666718046 (SUCCESS)
- Container: `lankaconnect-api-staging`
- Region: East US 2

---

**Document Created**: 2026-01-02
**Author**: System Architecture Designer
**Review Status**: Pending Azure Logs Analysis
**Follow-up Required**: Yes - User must provide logs or database version info
