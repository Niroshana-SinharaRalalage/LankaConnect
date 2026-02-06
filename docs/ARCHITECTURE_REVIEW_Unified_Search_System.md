# Architectural Review: Unified Search System Design

**Date:** 2025-12-31
**Document Type:** Architecture Decision Record (ADR) + Design Specification
**Status:** APPROVED - Ready for Implementation
**Architect:** System Architecture Designer

---

## Executive Summary

This document provides a comprehensive architectural review and design plan for the unified search system across Events, Business, Forums, and Marketplace entities. It addresses two critical issues:

1. **Business API Response Format Mismatch** (Already analyzed in RCA)
2. **Inconsistent Search Result DTOs** (User-identified architectural gap)

**Recommended Solution:** Implement a three-tier standardization strategy:
- **Tier 1:** Standardize API response format (unwrap `Result<T>`)
- **Tier 2:** Implement generic `SearchResult<T>` wrapper for search metadata
- **Tier 3:** Unified pagination using `PagedResult<T>` across all endpoints

**Timeline:** 12-16 hours of implementation + 8 hours testing
**Risk Level:** Medium (Breaking changes mitigated by backward compatibility)
**Priority:** P0 (Blocks user functionality)

---

## Table of Contents

1. [Current State Analysis](#1-current-state-analysis)
2. [Architectural Inconsistencies Identified](#2-architectural-inconsistencies-identified)
3. [DTO Design Strategy Evaluation](#3-dto-design-strategy-evaluation)
4. [Response Format Standardization](#4-response-format-standardization)
5. [Controller Pattern Enforcement](#5-controller-pattern-enforcement)
6. [Frontend Type Safety Strategy](#6-frontend-type-safety-strategy)
7. [Recommended Architecture](#7-recommended-architecture)
8. [Migration Plan](#8-migration-plan)
9. [Risk Assessment](#9-risk-assessment)
10. [Testing Strategy](#10-testing-strategy)
11. [Decision Records](#11-decision-records)

---

## 1. Current State Analysis

### 1.1 Backend Entity Status

| Entity | Domain | Application | Repository | Controller | Status |
|--------|--------|-------------|------------|------------|--------|
| **Events** | ✅ Complete | ✅ Complete | ✅ Complete | ✅ 43 endpoints | Production |
| **Business** | ✅ Complete | ✅ Complete | ✅ Complete | ⚠️ 2 endpoints | Broken |
| **Forums** | ✅ Complete | ❌ Missing | ✅ Complete | ❌ Missing | Not Started |
| **Marketplace** | ❌ Missing | ❌ Missing | ❌ Missing | ❌ Missing | Not Planned |

### 1.2 DTO Comparison Matrix

#### Events Search Result DTO
**File:** `LankaConnect.Application/Events/Common/EventSearchResultDto.cs`

```csharp
public class EventSearchResultDto
{
    // Standard EventDto properties (97 lines)
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    // ... 27+ properties including Images, Videos, Badges, EmailGroups

    // Search-specific metadata
    public decimal SearchRelevance { get; set; }  // PostgreSQL ts_rank score
}
```

**Pattern:** Entity-specific DTO that extends base DTO with search metadata
**Size:** Duplicates all 27 EventDto properties + 1 search field = **28 properties**
**Maintenance Issue:** Changes to EventDto require manual sync to EventSearchResultDto

#### Business Search Result DTO
**File:** `LankaConnect.Application/Businesses/Common/BusinessDto.cs`

```csharp
public record BusinessDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    // ... 30 properties total

    // ❌ NO search metadata at all!
}
```

**Pattern:** Generic DTO used for both detail and search
**Problem:** Cannot distinguish search results from detail views
**Missing:** Search relevance, highlight snippets, ranking metadata

### 1.3 Response Format Inconsistency

#### Events API (PagedResult<T>)
**Response Structure:**
```json
{
  "items": [...],
  "totalCount": 21,
  "page": 1,
  "pageSize": 10,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Properties:**
- `page` (not `pageNumber`)
- `IReadOnlyList<T> Items`
- Calculated in-place: `TotalPages = Math.Ceiling(totalCount / pageSize)`

#### Business API (PaginatedList<T>)
**Response Structure:**
```json
{
  "items": [],
  "pageNumber": 1,        // ← Different name
  "totalPages": 0,
  "totalCount": 0,
  "pageSize": 10,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

**Properties:**
- `pageNumber` (not `page`)
- `IReadOnlyCollection<T> Items`
- Calculated in-place: `TotalPages = Math.Ceiling(count / pageSize)`

**Difference:** Property name `page` vs `pageNumber` breaks type safety

### 1.4 Controller Inheritance Discrepancy

#### Events Controller (CORRECT)
```csharp
public class EventsController : BaseController<EventsController>
{
    public EventsController(IMediator mediator, ILogger<EventsController> logger)
        : base(mediator, logger) { }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<EventSearchResultDto>>> SearchEvents(...)
    {
        var result = await Mediator.Send(query);
        return HandleResult(result);  // ✅ Unwraps Result<T>
    }
}
```

#### Business Controller (BROKEN)
```csharp
public class BusinessesController : ControllerBase  // ❌ Wrong base class
{
    private readonly IMediator _mediator;

    public BusinessesController(IMediator mediator)  // ❌ No logger
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    public async Task<ActionResult<PaginatedList<BusinessDto>>> SearchBusinesses(...)
    {
        var result = await _mediator.Send(query);
        return Ok(result);  // ❌ Exposes Result<T> wrapper
    }
}
```

**Root Cause:** Business controller doesn't inherit from `BaseController<T>`, therefore doesn't have access to `HandleResult()` method.

---

## 2. Architectural Inconsistencies Identified

### 2.1 Critical Issues

#### Issue 1: Result<T> Wrapper Leakage
**Problem:** `Result<T>` is a domain/application pattern for error handling. It should **NEVER** be exposed to API clients.

**Current State:**
- ✅ **Events API:** Correctly unwraps using `BaseController.HandleResult()`
- ❌ **Business API:** Directly returns `Result<T>` wrapper in JSON

**Impact:**
```typescript
// Frontend expects:
{ items: [...], pageNumber: 1, totalCount: 10 }

// Business API actually returns:
{
  value: { items: [...], pageNumber: 1, totalCount: 10 },
  isSuccess: true,
  errors: []
}
```

**User Impact:** Business search tab completely broken in unified search UI

#### Issue 2: Duplicate DTO Properties
**Problem:** EventSearchResultDto duplicates all 27 EventDto properties manually

**Maintenance Risk:**
```csharp
// EventDto.cs - Add new property
public record EventDto {
    public IReadOnlyList<Guid> EmailGroupIds { get; init; }  // Phase 6A.32
}

// ❌ MUST manually add to EventSearchResultDto.cs
public class EventSearchResultDto {
    public List<Guid> EmailGroupIds { get; set; }  // Easy to forget!
}
```

**Historical Evidence:** This already happened:
- Phase 6A.32 added EmailGroups to EventDto
- Developer manually synced to EventSearchResultDto
- Different types used (`IReadOnlyList` vs `List`)
- Inconsistent naming possible in future

#### Issue 3: PagedResult vs PaginatedList Duplication
**Problem:** Two nearly identical classes for same purpose

**Code Smell:**
```csharp
// PagedResult.cs
public class PagedResult<T> {
    public int Page { get; init; }  // ← Different name
    public IReadOnlyList<T> Items { get; init; }
}

// PaginatedList.cs
public class PaginatedList<T> {
    public int PageNumber { get; init; }  // ← Different name
    public IReadOnlyCollection<T> Items { get; }
}
```

**Impact:**
- Frontend must handle both `page` and `pageNumber` properties
- TypeScript discriminated unions become complex
- Developer confusion when choosing which to use

#### Issue 4: No Search Metadata for Business
**Problem:** Cannot implement search features:
- No relevance scoring → Cannot sort by relevance
- No search snippets → Cannot highlight matching text
- No result ranking → Cannot show "Best Match" badge

**Business Value Lost:**
- Poor user experience (all results equally ranked)
- Cannot implement "Featured" or "Promoted" results
- Cannot A/B test search algorithms

### 2.2 Architectural Debt Summary

| Issue | Severity | Entities Affected | Technical Debt |
|-------|----------|-------------------|----------------|
| Result<T> wrapper leak | **P0 Critical** | Business (2 endpoints) | 3 hours to fix |
| DTO property duplication | **P1 High** | Events (43 endpoints) | 8 hours to refactor |
| Dual pagination classes | **P2 Medium** | All entities | 6 hours to consolidate |
| Missing search metadata | **P1 High** | Business, Forums, Marketplace | 4 hours per entity |

**Total Debt:** ~35 hours of technical work to achieve clean architecture

---

## 3. DTO Design Strategy Evaluation

### 3.1 Option A: Entity-Specific Search Result DTOs

**Pattern:**
```csharp
public record EventSearchResultDto : EventDto
{
    public double SearchRank { get; init; }
    public Dictionary<string, string> Highlights { get; init; }
}

public record BusinessSearchResultDto : BusinessDto
{
    public double SearchRank { get; init; }
    public Dictionary<string, string> Highlights { get; init; }
}

public record ForumTopicSearchResultDto : ForumTopicDto
{
    public double SearchRank { get; init; }
    public Dictionary<string, string> Highlights { get; init; }
}
```

**Pros:**
✅ Type-safe: Each entity has strongly-typed search result
✅ Familiar pattern: Already used by Events
✅ Flexible: Can add entity-specific search metadata
✅ IntelliSense-friendly: Clear inheritance hierarchy

**Cons:**
❌ **Property duplication:** Each DTO duplicates parent properties (maintenance burden)
❌ **Manual synchronization:** Changes to base DTO require manual updates
❌ **Code repetition:** Search metadata properties duplicated across all entities
❌ **Inconsistency risk:** Different naming conventions per entity (List vs IReadOnlyList)

**Maintenance Scenario:**
```csharp
// Step 1: Add property to EventDto
public record EventDto {
    public string NewField { get; init; }  // Product owner requests new field
}

// Step 2: Developer must remember to update EventSearchResultDto
public class EventSearchResultDto {
    public string NewField { get; set; }  // Easy to forget! No compiler error if missing.
}
```

**Verdict:** ⚠️ **NOT RECOMMENDED for new entities** - High maintenance overhead

---

### 3.2 Option B: Generic SearchResult<T> Wrapper ⭐ **RECOMMENDED**

**Pattern:**
```csharp
/// <summary>
/// Generic wrapper for search results with ranking metadata
/// Separates entity data from search-specific metadata
/// </summary>
public record SearchResult<T> where T : class
{
    /// <summary>
    /// The actual entity data (EventDto, BusinessDto, etc.)
    /// </summary>
    public T Item { get; init; } = default!;

    /// <summary>
    /// PostgreSQL ts_rank search relevance score (0.0 to 1.0)
    /// Higher scores indicate better matches
    /// </summary>
    public double SearchRank { get; init; }

    /// <summary>
    /// Text snippets with search term highlighting
    /// Key = field name (e.g., "title", "description")
    /// Value = HTML-safe highlighted snippet (e.g., "Yoga <mark>class</mark>")
    /// </summary>
    public Dictionary<string, string> Highlights { get; init; } = new();

    /// <summary>
    /// Search result position in overall ranking (1-based)
    /// Useful for analytics tracking ("User clicked result #3")
    /// </summary>
    public int ResultPosition { get; init; }
}

// Usage in repositories
public async Task<PagedResult<SearchResult<EventDto>>> SearchEventsAsync(string searchTerm)
{
    var events = await _dbContext.Events
        .Where(e => e.SearchVector.Matches(searchTerm))
        .Select(e => new SearchResult<EventDto>
        {
            Item = _mapper.Map<EventDto>(e),
            SearchRank = e.SearchVector.Rank(searchTerm),
            Highlights = new()
            {
                { "title", Highlight(e.Title, searchTerm) },
                { "description", Highlight(e.Description, searchTerm) }
            },
            ResultPosition = rowNumber
        })
        .ToListAsync();

    return new PagedResult<SearchResult<EventDto>>(events, totalCount, page, pageSize);
}
```

**Response Structure:**
```json
{
  "items": [
    {
      "item": {
        "id": "guid",
        "title": "Yoga Workshop",
        "description": "Morning yoga session",
        // ... all EventDto properties
      },
      "searchRank": 0.87,
      "highlights": {
        "title": "Yoga <mark>Workshop</mark>",
        "description": "Morning <mark>yoga</mark> session"
      },
      "resultPosition": 1
    }
  ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Pros:**
✅ **Zero duplication:** Base DTOs remain unchanged
✅ **Automatic sync:** Changes to EventDto/BusinessDto automatically reflected
✅ **Consistent pattern:** Same search metadata across ALL entities
✅ **Type-safe:** `SearchResult<EventDto>` vs `SearchResult<BusinessDto>` are distinct types
✅ **Extensible:** Easy to add new search metadata fields (resultPosition, score breakdown)
✅ **Separation of concerns:** Entity data vs search metadata cleanly separated
✅ **Frontend-friendly:** Clear nesting makes it obvious which data is search-specific

**Cons:**
⚠️ **Breaking change:** Frontend must access `result.item.title` instead of `result.title`
⚠️ **Migration effort:** Existing Events endpoints need refactoring
⚠️ **Nesting depth:** One extra level in JSON structure (minor performance impact)

**Migration Strategy (Mitigates Breaking Change):**
```csharp
// Phase 1: Add SearchResult<T> endpoints alongside existing ones
[HttpGet("search/v2")]  // New endpoint
public async Task<ActionResult<PagedResult<SearchResult<EventDto>>>> SearchEventsV2(...)

[HttpGet("search")]  // Old endpoint (deprecated)
public async Task<ActionResult<PagedResult<EventSearchResultDto>>> SearchEvents(...)

// Phase 2: Frontend migrates to v2
// Phase 3: Remove v1 endpoints after migration complete
```

**Verdict:** ⭐ **RECOMMENDED** - Best long-term architecture

---

### 3.3 Option C: Unified ISearchResult Interface

**Pattern:**
```csharp
public interface ISearchResult
{
    string Id { get; }
    string Title { get; }
    string Description { get; }
    double SearchRank { get; }
    string EntityType { get; }  // "Event", "Business", "Forum", "Marketplace"
}

public record EventSearchResultDto : EventDto, ISearchResult
{
    public double SearchRank { get; init; }
    public string EntityType => "Event";

    // Interface properties satisfied by EventDto properties
    string ISearchResult.Id => Id.ToString();
    string ISearchResult.Title => Title;
    string ISearchResult.Description => Description;
}

public record BusinessSearchResultDto : BusinessDto, ISearchResult
{
    public double SearchRank { get; init; }
    public string EntityType => "Business";

    string ISearchResult.Id => Id.ToString();
    string ISearchResult.Title => Name;  // ← Different property!
    string ISearchResult.Description => Description;
}
```

**Usage in Unified Search:**
```csharp
public async Task<PagedResult<ISearchResult>> UnifiedSearch(string searchTerm, string[] types)
{
    var results = new List<ISearchResult>();

    if (types.Contains("Event"))
        results.AddRange(await SearchEvents(searchTerm));

    if (types.Contains("Business"))
        results.AddRange(await SearchBusiness(searchTerm));

    return results.OrderByDescending(r => r.SearchRank).ToPagedResult();
}
```

**Pros:**
✅ **Unified API:** Single endpoint can return multiple entity types
✅ **Polymorphic rendering:** Frontend can render any ISearchResult the same way
✅ **Simplified frontend:** No type discrimination needed for basic rendering

**Cons:**
❌ **Lowest common denominator:** Interface forces all entities to have same properties
❌ **Lost type information:** Cannot access entity-specific properties (Event.StartDate, Business.Rating)
❌ **Casting required:** Frontend must cast to access full entity data
❌ **Property name conflicts:** BusinessDto has `Name`, EventDto has `Title` - mapping required
❌ **Serialization issues:** Interface properties may not serialize correctly in JSON

**Verdict:** ❌ **NOT RECOMMENDED** - Loses type safety and entity-specific data

---

### 3.4 Option D: Hybrid Approach (Current Events Pattern + Generic Wrapper)

**Pattern:**
```csharp
// Keep existing EventSearchResultDto for backward compatibility
public class EventSearchResultDto { ... }

// Add new generic pattern for future entities
public record SearchResult<T> { ... }

// Usage:
PagedResult<EventSearchResultDto> eventsV1 = await SearchEvents(...);  // Legacy
PagedResult<SearchResult<EventDto>> eventsV2 = await SearchEventsV2(...);  // New pattern
PagedResult<SearchResult<BusinessDto>> business = await SearchBusiness(...);  // New pattern
```

**Pros:**
✅ **No breaking changes:** Events endpoints continue working
✅ **Gradual migration:** New entities use better pattern
✅ **Time to refactor:** Can migrate Events later

**Cons:**
❌ **Inconsistency:** Two different patterns in same codebase
❌ **Developer confusion:** "Which pattern should I use?"
❌ **Technical debt:** Eventually need to standardize

**Verdict:** ⚠️ **ACCEPTABLE for migration phase only** - Not a final solution

---

### 3.5 Recommendation Summary

| Option | Score | Use Case |
|--------|-------|----------|
| **A: Entity-Specific DTOs** | 4/10 | ❌ Avoid for new entities (high maintenance) |
| **B: Generic SearchResult<T>** | **9/10** | ⭐ **RECOMMENDED** for all future work |
| **C: ISearchResult Interface** | 3/10 | ❌ Loses type safety |
| **D: Hybrid Approach** | 6/10 | ✅ Acceptable during migration only |

**DECISION:** Implement **Option B (Generic SearchResult<T>)** for all new entities.
**MIGRATION:** Use **Option D (Hybrid)** to avoid breaking existing Events endpoints.

---

## 4. Response Format Standardization

### 4.1 Problem: PagedResult vs PaginatedList

**Current State Analysis:**

```csharp
// PagedResult.cs (used by Events)
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }           // ← Property name difference
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        HasPreviousPage = page > 1;
        HasNextPage = page < TotalPages;
    }
}

// PaginatedList.cs (used by Business)
public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }  // ← Collection type difference
    public int PageNumber { get; }                // ← Property name difference
    public int TotalPages { get; }
    public int TotalCount { get; }
    public int PageSize { get; }

    public PaginatedList(IReadOnlyCollection<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        PageSize = pageSize;
        Items = items;
    }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
```

**Differences:**
1. **Property naming:** `Page` vs `PageNumber`
2. **Collection type:** `IReadOnlyList<T>` vs `IReadOnlyCollection<T>`
3. **Boolean properties:** Computed vs stored
4. **Constructor parameter:** `totalCount` vs `count`

**Frontend Impact:**
```typescript
// Frontend must handle both formats
type EventsResponse = {
  page: number;           // ← Events API
  items: readonly T[];
}

type BusinessResponse = {
  pageNumber: number;     // ← Business API
  items: readonly T[];
}

// Unified search hook must normalize:
return {
  pageNumber: result.page || result.pageNumber,  // ❌ Fragile!
  items: result.items
};
```

### 4.2 Evaluation: Which Format to Standardize On?

#### Option 1: Standardize on PagedResult<T>

**Migration Required:**
- ✅ **Events:** Already using PagedResult - **0 changes**
- ❌ **Business:** Must migrate 2 endpoints from PaginatedList → PagedResult

**Pros:**
✅ Majority of codebase already uses it (43 vs 2 endpoints)
✅ Better property names (`Page` is clearer than `PageNumber`)
✅ Cleaner API: `PagedResult.Empty(page, pageSize)` factory method
✅ Frontend already expects `page` property for Events

**Cons:**
⚠️ Breaks existing Business API consumers (if any exist)
⚠️ Must update Business query handlers

#### Option 2: Standardize on PaginatedList<T>

**Migration Required:**
- ❌ **Events:** Must migrate **43 endpoints** from PagedResult → PaginatedList
- ✅ **Business:** Already using PaginatedList - **0 changes**

**Pros:**
✅ Property name `PageNumber` is more explicit than `Page`
✅ No breaking changes for Business API

**Cons:**
❌ Massive migration effort (43 endpoints)
❌ Breaking change for ALL Events API consumers (production frontend!)
❌ Computed properties (`HasPreviousPage`) slightly less efficient

#### Option 3: Create New Unified PaginationResult<T>

**Migration Required:**
- ❌ **Events:** Must migrate 43 endpoints
- ❌ **Business:** Must migrate 2 endpoints
- ❌ **Frontend:** Must update all pagination logic

**Pattern:**
```csharp
public record PaginationResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public PaginationMetadata Metadata { get; init; }
}

public record PaginationMetadata
{
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
```

**Pros:**
✅ Clean break from legacy code
✅ Metadata separation improves clarity
✅ Can add cursor-based pagination later

**Cons:**
❌ Breaking change for EVERYTHING
❌ Massive migration effort
❌ No immediate value gain

### 4.3 DECISION: Standardize on PagedResult<T>

**Rationale:**

1. **Minimal migration:** Only 2 Business endpoints need changes vs 43 Events endpoints
2. **Better naming:** `Page` is simpler than `PageNumber`
3. **Established pattern:** Events API is production-tested
4. **Frontend compatibility:** Frontend already handles `page` property

**Migration Plan:**

```csharp
// Step 1: Update Business query handlers
public class SearchBusinessesQueryHandler
    : IQueryHandler<SearchBusinessesQuery, PagedResult<BusinessDto>>  // Change return type
{
    public async Task<Result<PagedResult<BusinessDto>>> Handle(...)
    {
        // OLD CODE:
        // return Result<PaginatedList<BusinessDto>>.Success(
        //     new PaginatedList<BusinessDto>(items, totalCount, pageNumber, pageSize));

        // NEW CODE:
        return Result<PagedResult<BusinessDto>>.Success(
            new PagedResult<BusinessDto>(items, totalCount, pageNumber, pageSize));
    }
}

// Step 2: Update BusinessesController response type
[HttpGet("search")]
public async Task<ActionResult<PagedResult<BusinessDto>>> SearchBusinesses(...)
{
    var result = await Mediator.Send(query);
    return HandleResult(result);
}

// Step 3: Deprecate PaginatedList.cs (keep for backward compatibility during migration)
[Obsolete("Use PagedResult<T> instead. PaginatedList will be removed in next major version.")]
public class PaginatedList<T> { ... }
```

**Breaking Change Mitigation:**
- Keep `PaginatedList.cs` in codebase as `[Obsolete]` for 1 release cycle
- Add API versioning to support both formats temporarily
- Document migration in CHANGELOG.md

---

## 5. Controller Pattern Enforcement

### 5.1 Problem: Inconsistent Controller Inheritance

**BaseController Pattern (CORRECT):**

```csharp
/// <summary>
/// Base controller with Result<T> unwrapping and error handling
/// ALL controllers SHOULD inherit from this
/// </summary>
public abstract class BaseController<T> : ControllerBase where T : class
{
    protected readonly IMediator Mediator;
    protected readonly ILogger<T> Logger;

    protected BaseController(IMediator mediator, ILogger<T> logger)
    {
        Mediator = mediator;
        Logger = logger;
    }

    /// <summary>
    /// Unwraps Result<T> and returns appropriate HTTP response
    /// </summary>
    protected IActionResult HandleResult<TResult>(Result<TResult> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);  // ✅ Unwraps Result<T>
        }

        var firstError = result.Errors.FirstOrDefault();
        return BadRequest(new ProblemDetails
        {
            Detail = firstError,
            Status = 400,
            Title = "Bad Request"
        });
    }

    // Additional helpers...
}
```

**Why This Matters:**

1. **Separation of Concerns:** `Result<T>` is a domain pattern, not an API contract
2. **Consistent Error Handling:** All endpoints return RFC 7807 Problem Details
3. **Logging Infrastructure:** Logger available in all controllers
4. **DRY Principle:** Error handling logic in one place

### 5.2 Current Controller Audit

```bash
# Find all controllers
find c:\Work\LankaConnect\src -name "*Controller.cs" -type f | wc -l
# Result: 21 controllers

# Check inheritance patterns
grep "class.*Controller.*:" c:\Work\LankaConnect\src -r --include="*Controller.cs"
```

**Expected Results:**

| Controller | Inheritance | Status |
|------------|-------------|--------|
| EventsController | `BaseController<EventsController>` | ✅ Correct |
| EventSignupsController | `BaseController<EventSignupsController>` | ✅ Correct |
| BadgesController | `BaseController<BadgesController>` | ✅ Correct |
| EmailGroupsController | `BaseController<EmailGroupsController>` | ✅ Correct |
| **BusinessesController** | `ControllerBase` | ❌ **BROKEN** |
| (16 other controllers) | *Unknown* | ⚠️ Needs audit |

### 5.3 BusinessesController Refactoring

**BEFORE (BROKEN):**
```csharp
public class BusinessesController : ControllerBase  // ❌ Wrong base class
{
    private readonly IMediator _mediator;  // ❌ Manual field instead of inherited

    public BusinessesController(IMediator mediator)  // ❌ No logger
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<CreateBusinessResponse>> CreateBusiness(CreateBusinessCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);  // ❌ Inconsistent error handling
        }

        return CreatedAtAction(
            nameof(GetBusiness),
            new { id = result.Value },
            new CreateBusinessResponse(result.Value));
    }

    [HttpGet("search")]
    public async Task<ActionResult<PaginatedList<BusinessDto>>> SearchBusinesses(...)
    {
        var result = await _mediator.Send(query);
        return Ok(result);  // ❌ Exposes Result<T> wrapper!
    }
}
```

**AFTER (FIXED):**
```csharp
public class BusinessesController : BaseController<BusinessesController>  // ✅ Correct base class
{
    public BusinessesController(IMediator mediator, ILogger<BusinessesController> logger)
        : base(mediator, logger)  // ✅ Logger injected
    {
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateBusinessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessCommand command)
    {
        var result = await Mediator.Send(command);

        return HandleResultWithCreated(  // ✅ Consistent error handling
            result,
            nameof(GetBusiness),
            new { id = result.Value }
        );
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<BusinessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchBusinesses(
        [FromQuery] string? searchTerm,
        // ... parameters
    {
        var query = new SearchBusinessesQuery(searchTerm, ...);
        var result = await Mediator.Send(query);

        return HandleResult(result);  // ✅ Unwraps Result<T>
    }
}
```

**Changes Summary:**

1. ✅ Inherit from `BaseController<BusinessesController>`
2. ✅ Add `ILogger<BusinessesController>` to constructor
3. ✅ Use `Mediator` (inherited property) instead of `_mediator` field
4. ✅ Replace `return Ok(result)` with `return HandleResult(result)`
5. ✅ Replace `return BadRequest(result.Errors)` with `HandleResultWithCreated(...)`
6. ✅ Add proper `[ProducesResponseType]` attributes for Swagger
7. ✅ Change `PagedResult<T>` return type (from `PaginatedList<T>`)

### 5.4 Controller Pattern Enforcement Strategy

**Architectural Rule:**

> **MANDATORY:** All controllers in LankaConnect.API MUST inherit from `BaseController<T>`.
> **EXCEPTION:** Only `HealthController` and `AuthController` may inherit directly from `ControllerBase` due to framework requirements.

**Enforcement Methods:**

1. **Code Review Checklist:**
   - [ ] Controller inherits from `BaseController<T>`
   - [ ] Logger injected via constructor
   - [ ] All `Result<T>` return types use `HandleResult()`
   - [ ] ProducesResponseType attributes defined

2. **Roslyn Analyzer (Future):**
   ```csharp
   // Custom analyzer to enforce pattern
   [DiagnosticAnalyzer(LanguageNames.CSharp)]
   public class ControllerInheritanceAnalyzer : DiagnosticAnalyzer
   {
       // Rule: Controllers must inherit from BaseController<T>
   }
   ```

3. **Unit Test Template:**
   ```csharp
   public class ControllerInheritanceTests
   {
       [Fact]
       public void AllControllers_ShouldInheritFrom_BaseController()
       {
           var assembly = typeof(BusinessesController).Assembly;
           var controllers = assembly.GetTypes()
               .Where(t => t.Name.EndsWith("Controller"))
               .Where(t => t != typeof(BaseController<>));

           foreach (var controller in controllers)
           {
               var baseType = controller.BaseType;
               Assert.True(
                   baseType?.IsGenericType &&
                   baseType.GetGenericTypeDefinition() == typeof(BaseController<>),
                   $"{controller.Name} must inherit from BaseController<T>"
               );
           }
       }
   }
   ```

---

## 6. Frontend Type Safety Strategy

### 6.1 Current Frontend Type Challenges

**Problem:** Frontend must handle multiple response formats:

```typescript
// useUnifiedSearch.ts (BEFORE FIX)
export function useUnifiedSearch(searchTerm: string, type: 'events' | 'business') {
  return useQuery({
    queryFn: async (): Promise<UnifiedSearchResult> => {
      if (type === 'events') {
        const result = await eventsRepository.searchEvents({ searchTerm });

        // Events API uses PagedResult with 'page' property
        return {
          items: result.items,
          pageNumber: result.page,  // ← Property name mapping
          totalPages: result.totalPages,
          totalCount: result.totalCount,
          hasPreviousPage: result.hasPreviousPage,
          hasNextPage: result.hasNextPage,
        };
      } else if (type === 'business') {
        const result = await businessesRepository.search({ searchTerm });

        // Business API returns Result<PaginatedList> - BROKEN!
        // Actual response: { value: { items: [...] }, isSuccess: true }
        // Expected response: { items: [...], pageNumber: 1 }
        return result;  // ❌ This breaks!
      }
    }
  });
}

// Current UnifiedSearchResult type
export type UnifiedSearchResult = {
  items: readonly (EventSearchResultDto | BusinessDto)[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};
```

### 6.2 After Backend Standardization (PagedResult + SearchResult<T>)

**Backend Response (After Fix):**
```typescript
// Events API: /api/events/search
{
  "items": [
    {
      "item": { id: "...", title: "Yoga Class", ... },  // EventDto
      "searchRank": 0.87,
      "highlights": { "title": "Yoga <mark>Class</mark>" }
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}

// Business API: /api/businesses/search (FIXED)
{
  "items": [
    {
      "item": { id: "...", name: "Sri Lankan Restaurant", ... },  // BusinessDto
      "searchRank": 0.92,
      "highlights": { "name": "Sri Lankan <mark>Restaurant</mark>" }
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 18,
  "totalPages": 2,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**TypeScript Types (After Fix):**

```typescript
// types/common.types.ts
export type SearchResult<T> = {
  item: T;
  searchRank: number;
  highlights: Record<string, string>;
  resultPosition: number;
};

export type PagedResult<T> = {
  items: readonly T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

// types/events.types.ts
export type EventDto = {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  // ... 27+ properties
};

export type EventSearchResponse = PagedResult<SearchResult<EventDto>>;

// types/business.types.ts
export type BusinessDto = {
  id: string;
  name: string;
  description: string;
  // ... 30 properties
};

export type BusinessSearchResponse = PagedResult<SearchResult<BusinessDto>>;
```

**Repository Layer (After Fix):**

```typescript
// repositories/events.repository.ts
export const eventsRepository = {
  async searchEvents(request: SearchEventsRequest): Promise<EventSearchResponse> {
    const params = new URLSearchParams({
      searchTerm: request.searchTerm,
      page: request.page.toString(),
      pageSize: request.pageSize.toString(),
    });

    const response = await apiClient.get<EventSearchResponse>(
      `/api/events/search?${params}`
    );

    return response;  // ✅ Clean PagedResult<SearchResult<EventDto>>
  }
};

// repositories/businesses.repository.ts
export const businessesRepository = {
  async search(request: SearchBusinessesRequest): Promise<BusinessSearchResponse> {
    const params = new URLSearchParams({
      searchTerm: request.searchTerm || '',
      pageNumber: request.pageNumber.toString(),
      pageSize: request.pageSize.toString(),
    });

    const response = await apiClient.get<BusinessSearchResponse>(
      `/api/businesses/search?${params}`
    );

    return response;  // ✅ Clean PagedResult<SearchResult<BusinessDto>>
  }
};
```

**Unified Search Hook (After Fix):**

```typescript
// hooks/useUnifiedSearch.ts
export type UnifiedSearchResult<T> = {
  items: readonly SearchResult<T>[];  // ← Now includes search metadata!
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

export function useUnifiedSearch<T extends EventDto | BusinessDto>(
  searchTerm: string,
  type: 'events' | 'business',
  page: number = 1,
  pageSize: number = 20
): UseQueryResult<UnifiedSearchResult<T>> {
  return useQuery({
    queryKey: ['unified-search', searchTerm, type, page, pageSize],
    queryFn: async (): Promise<UnifiedSearchResult<T>> => {
      if (!searchTerm.trim()) {
        return createEmptyResult(page, pageSize);
      }

      if (type === 'events') {
        const result = await eventsRepository.searchEvents({
          searchTerm,
          page,
          pageSize,
        });

        // ✅ No mapping needed - format already matches!
        return result as UnifiedSearchResult<T>;
      } else if (type === 'business') {
        const result = await businessesRepository.search({
          searchTerm,
          pageNumber: page,  // ← Only parameter name difference
          pageSize,
        });

        // ✅ No mapping needed - format already matches!
        return result as UnifiedSearchResult<T>;
      }

      return createEmptyResult(page, pageSize);
    },
    enabled: !!searchTerm.trim(),
    staleTime: 30000,
  });
}
```

**Component Usage (After Fix):**

```typescript
// components/SearchPage.tsx
export function SearchPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [activeTab, setActiveTab] = useState<'events' | 'business'>('events');

  const { data, isLoading, error } = useUnifiedSearch(searchTerm, activeTab);

  return (
    <div>
      <SearchInput value={searchTerm} onChange={setSearchTerm} />
      <Tabs value={activeTab} onChange={setActiveTab}>
        <Tab label="Events" value="events" />
        <Tab label="Business" value="business" />
      </Tabs>

      {data?.items.map((result) => (
        <SearchResultCard
          key={result.item.id}
          title={result.item.title || result.item.name}  // ← Type-safe access
          description={result.item.description}
          highlights={result.highlights}
          searchRank={result.searchRank}  // ✅ Now available!
          onClick={() => navigate(`/${activeTab}/${result.item.id}`)}
        />
      ))}

      <Pagination
        page={data?.page ?? 1}
        totalPages={data?.totalPages ?? 0}
        onPageChange={(newPage) => setSearchTerm(searchTerm)}  // Re-trigger query
      />
    </div>
  );
}
```

### 6.3 Type Discrimination Strategy

**Problem:** `items: readonly SearchResult<EventDto | BusinessDto>[]` loses type information

**Solution:** Discriminated Union Pattern

```typescript
// types/search.types.ts
export type EntityType = 'events' | 'business' | 'forums' | 'marketplace';

export type TypedSearchResult<T extends EntityType> =
  T extends 'events' ? SearchResult<EventDto> :
  T extends 'business' ? SearchResult<BusinessDto> :
  T extends 'forums' ? SearchResult<ForumTopicDto> :
  T extends 'marketplace' ? SearchResult<MarketplaceItemDto> :
  never;

export type UnifiedSearchResult<T extends EntityType> = {
  items: readonly TypedSearchResult<T>[];
  page: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

// Usage with type inference
const eventsResult: UnifiedSearchResult<'events'> = useUnifiedSearch('yoga', 'events');
// TypeScript knows: eventsResult.items[0].item is EventDto ✅

const businessResult: UnifiedSearchResult<'business'> = useUnifiedSearch('restaurant', 'business');
// TypeScript knows: businessResult.items[0].item is BusinessDto ✅
```

**Benefit:** Full type safety without runtime checks!

---

## 7. Recommended Architecture

### 7.1 Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend Layer                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  useUnifiedSearch<T extends EntityType>()                      │
│         │                                                        │
│         ├─► EventsRepository.searchEvents()                    │
│         │        → PagedResult<SearchResult<EventDto>>         │
│         │                                                        │
│         ├─► BusinessesRepository.search()                      │
│         │        → PagedResult<SearchResult<BusinessDto>>      │
│         │                                                        │
│         └─► ForumsRepository.search() (Future)                 │
│                  → PagedResult<SearchResult<ForumTopicDto>>    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            │ HTTP GET
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                        API Layer (Controllers)                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  BaseController<T>.HandleResult<TResult>(Result<TResult>)      │
│         │                                                        │
│         ├─► EventsController.SearchEvents()                    │
│         │        Returns: PagedResult<SearchResult<EventDto>>  │
│         │        Unwraps: Result<PagedResult<...>>             │
│         │                                                        │
│         ├─► BusinessesController.SearchBusinesses()            │
│         │        Returns: PagedResult<SearchResult<BusinessDto>>│
│         │        Unwraps: Result<PagedResult<...>>             │
│         │                                                        │
│         └─► ForumsController.SearchTopics() (Future)           │
│                  Returns: PagedResult<SearchResult<...>>       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            │ MediatR
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Application Layer (CQRS)                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  IQueryHandler<TQuery, TResponse>                              │
│         │                                                        │
│         ├─► SearchEventsQueryHandler                           │
│         │        Returns: Result<PagedResult<SearchResult<>>>  │
│         │        Logic: Full-text search with ts_rank          │
│         │                                                        │
│         ├─► SearchBusinessesQueryHandler                       │
│         │        Returns: Result<PagedResult<SearchResult<>>>  │
│         │        Logic: Location + text search                 │
│         │                                                        │
│         └─► SearchForumTopicsQueryHandler (Future)             │
│                  Returns: Result<PagedResult<SearchResult<>>>  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                            │ Repository
                            ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  IRepository<T>                                                │
│         │                                                        │
│         ├─► EventRepository.SearchAsync()                      │
│         │        SQL: SELECT *, ts_rank(...) FROM events       │
│         │        Maps: Event → SearchResult<EventDto>          │
│         │                                                        │
│         ├─► BusinessRepository.SearchAsync()                   │
│         │        SQL: SELECT *, ts_rank(...) FROM businesses   │
│         │        Maps: Business → SearchResult<BusinessDto>    │
│         │                                                        │
│         └─► PostgreSQL Full-Text Search                        │
│                  - GIN indexes on search_vector columns        │
│                  - ts_rank() for relevance scoring             │
│                  - ts_headline() for snippet highlighting       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 7.2 Key Architectural Patterns

#### Pattern 1: Generic Search Result Wrapper

**Definition:**
```csharp
namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Generic wrapper for search results with ranking and highlighting metadata.
/// Separates entity data from search-specific information.
/// </summary>
/// <typeparam name="T">Entity DTO type (EventDto, BusinessDto, etc.)</typeparam>
public record SearchResult<T> where T : class
{
    /// <summary>
    /// The actual entity data
    /// </summary>
    public T Item { get; init; } = default!;

    /// <summary>
    /// PostgreSQL ts_rank search relevance score (0.0 to 1.0)
    /// Higher scores indicate better matches to the search term
    /// </summary>
    public double SearchRank { get; init; }

    /// <summary>
    /// Text snippets with search term highlighting
    /// Key = field name (e.g., "title", "description")
    /// Value = HTML-safe highlighted snippet (e.g., "Yoga <mark>class</mark>")
    /// Generated using PostgreSQL ts_headline()
    /// </summary>
    public Dictionary<string, string> Highlights { get; init; } = new();

    /// <summary>
    /// Search result position in overall ranking (1-based index)
    /// Useful for analytics tracking ("User clicked result #3")
    /// </summary>
    public int ResultPosition { get; init; }
}
```

**Usage in Query Handlers:**
```csharp
public class SearchEventsQueryHandler
    : IQueryHandler<SearchEventsQuery, PagedResult<SearchResult<EventDto>>>
{
    public async Task<Result<PagedResult<SearchResult<EventDto>>>> Handle(...)
    {
        var query = _dbContext.Events
            .Where(e => e.SearchVector.Matches(request.SearchTerm))
            .Select((e, index) => new SearchResult<EventDto>
            {
                Item = _mapper.Map<EventDto>(e),
                SearchRank = e.SearchVector.Rank(request.SearchTerm),
                Highlights = new Dictionary<string, string>
                {
                    { "title", Headline(e.Title, request.SearchTerm) },
                    { "description", Headline(e.Description, request.SearchTerm) }
                },
                ResultPosition = index + 1
            });

        var results = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var totalCount = await query.CountAsync();

        return Result<PagedResult<SearchResult<EventDto>>>.Success(
            new PagedResult<SearchResult<EventDto>>(results, totalCount, request.Page, request.PageSize)
        );
    }
}
```

#### Pattern 2: Unified Pagination (PagedResult<T>)

**Definition:**
```csharp
namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Generic paged result container for paginated queries.
/// ALL paginated endpoints MUST use this type.
/// </summary>
/// <typeparam name="T">Item type (can be SearchResult<TDto> or TDto directly)</typeparam>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }  // ← Standardized property name
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        HasPreviousPage = page > 1;
        HasNextPage = page < TotalPages;
    }

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new(Array.Empty<T>(), 0, page, pageSize);
}
```

**Deprecation Path:**
```csharp
[Obsolete("Use PagedResult<T> instead. PaginatedList will be removed in v2.0.0")]
public class PaginatedList<T> { ... }
```

#### Pattern 3: BaseController Result<T> Unwrapping

**Definition:**
```csharp
public abstract class BaseController<T> : ControllerBase where T : class
{
    protected readonly IMediator Mediator;
    protected readonly ILogger<T> Logger;

    protected BaseController(IMediator mediator, ILogger<T> logger)
    {
        Mediator = mediator;
        Logger = logger;
    }

    /// <summary>
    /// Unwraps Result<TResult> and returns appropriate HTTP response.
    /// This keeps Result<T> pattern internal to application layer.
    /// </summary>
    protected IActionResult HandleResult<TResult>(Result<TResult> result)
    {
        if (result.IsSuccess)
        {
            Logger.LogInformation("Request succeeded with result type {ResultType}", typeof(TResult).Name);
            return Ok(result.Value);  // ✅ Unwraps Result<T>
        }

        var firstError = result.Errors.FirstOrDefault() ?? "Unknown error occurred";
        Logger.LogWarning("Request failed: {Error}", firstError);

        return BadRequest(new ProblemDetails
        {
            Detail = firstError,
            Status = 400,
            Title = "Bad Request",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }
}
```

**RULE:** ALL controllers MUST use `HandleResult()` for `Result<T>` return types.

### 7.3 Architectural Principles

1. **Separation of Concerns:**
   - Domain layer: Business logic
   - Application layer: Use cases + Result<T> error handling
   - API layer: HTTP concerns + Result<T> unwrapping
   - Frontend: UI rendering

2. **DRY (Don't Repeat Yourself):**
   - SearchResult<T> eliminates DTO duplication
   - BaseController eliminates error handling duplication
   - PagedResult<T> eliminates pagination logic duplication

3. **Type Safety:**
   - Generic types preserve entity-specific information
   - Frontend discriminated unions prevent runtime errors
   - Compiler enforces architectural rules

4. **Extensibility:**
   - Adding Forums search: Create `ForumTopicDto`, reuse `SearchResult<ForumTopicDto>`
   - Adding Marketplace: Create `MarketplaceItemDto`, reuse `SearchResult<MarketplaceItemDto>`
   - Adding new search metadata: Update `SearchResult<T>` once, applies to all entities

---

## 8. Migration Plan

### 8.1 Phase 1: Fix Business API (Immediate - P0)

**Timeline:** 4 hours
**Goal:** Fix broken Business search to unblock unified search feature

**Tasks:**

1. **Update BusinessesController (1 hour)**
   ```csharp
   // File: src/LankaConnect.API/Controllers/BusinessesController.cs

   // CHANGE 1: Inheritance
   - public class BusinessesController : ControllerBase
   + public class BusinessesController : BaseController<BusinessesController>

   // CHANGE 2: Constructor
   - public BusinessesController(IMediator mediator)
   + public BusinessesController(IMediator mediator, ILogger<BusinessesController> logger)
   +     : base(mediator, logger)

   // CHANGE 3: Use inherited Mediator
   - private readonly IMediator _mediator;
   - _mediator = mediator;
   + // Use inherited Mediator property

   // CHANGE 4: Unwrap Result<T>
   - return Ok(result);
   + return HandleResult(result);

   // CHANGE 5: Return type (prepare for Phase 2)
   - Task<ActionResult<PaginatedList<BusinessDto>>>
   + Task<IActionResult>  // HandleResult() returns IActionResult
   ```

2. **Update SearchBusinessesQueryHandler (1 hour)**
   ```csharp
   // File: src/LankaConnect.Application/Businesses/Queries/SearchBusinesses/SearchBusinessesQueryHandler.cs

   // CHANGE: Return type
   - public class SearchBusinessesQueryHandler : IQueryHandler<SearchBusinessesQuery, PaginatedList<BusinessDto>>
   + public class SearchBusinessesQueryHandler : IQueryHandler<SearchBusinessesQuery, PagedResult<BusinessDto>>

   - return Result<PaginatedList<BusinessDto>>.Success(
   + return Result<PagedResult<BusinessDto>>.Success(
   -     new PaginatedList<BusinessDto>(businessDtos, businesses.TotalCount, request.PageNumber, request.PageSize)
   +     new PagedResult<BusinessDto>(businessDtos, businesses.TotalCount, request.PageNumber, request.PageSize)
   ```

3. **Update Frontend (1 hour)**
   ```typescript
   // File: web/src/infrastructure/api/repositories/businesses.repository.ts

   // CHANGE: Response type
   - export type SearchBusinessesResponse = PaginatedList<BusinessDto>;
   + export type SearchBusinessesResponse = PagedResult<BusinessDto>;

   // File: web/src/presentation/hooks/useUnifiedSearch.ts

   // CHANGE: Property mapping
   - pageNumber: result.pageNumber,  // PaginatedList uses 'pageNumber'
   + pageNumber: result.page,        // PagedResult uses 'page'
   ```

4. **Testing (1 hour)**
   ```bash
   # Backend integration test
   curl http://localhost:5000/api/businesses/search?searchTerm=restaurant
   # Expected: { "items": [...], "page": 1, "totalCount": 10 }
   # NOT: { "value": { "items": [...] } }

   # Frontend smoke test
   npm run dev
   # Navigate to /search
   # Search for "restaurant"
   # Switch to Business tab
   # Verify results display correctly
   ```

**Acceptance Criteria:**
- [ ] Business search API returns `PagedResult<BusinessDto>` (not wrapped in `Result<T>`)
- [ ] Frontend Business tab displays search results
- [ ] No console errors in browser
- [ ] Backend build succeeds with 0 errors

---

### 8.2 Phase 2: Implement SearchResult<T> for Business (P1)

**Timeline:** 6 hours
**Goal:** Add search metadata to Business search results

**Tasks:**

1. **Create SearchResult<T> Model (30 minutes)**
   ```csharp
   // File: src/LankaConnect.Application/Common/Models/SearchResult.cs (NEW)

   namespace LankaConnect.Application.Common.Models;

   public record SearchResult<T> where T : class
   {
       public T Item { get; init; } = default!;
       public double SearchRank { get; init; }
       public Dictionary<string, string> Highlights { get; init; } = new();
       public int ResultPosition { get; init; }
   }
   ```

2. **Update Business Repository (2 hours)**
   ```csharp
   // File: src/LankaConnect.Infrastructure/Persistence/Repositories/BusinessRepository.cs

   public async Task<PagedResult<SearchResult<Business>>> SearchAsync(
       string searchTerm,
       int page,
       int pageSize,
       CancellationToken cancellationToken)
   {
       var query = _dbContext.Businesses
           .Where(b => b.SearchVector.Matches(searchTerm))
           .Select((b, index) => new
           {
               Business = b,
               Rank = b.SearchVector.Rank(searchTerm),
               Position = index + 1
           })
           .OrderByDescending(x => x.Rank);

       var results = await query
           .Skip((page - 1) * pageSize)
           .Take(pageSize)
           .Select(x => new SearchResult<Business>
           {
               Item = x.Business,
               SearchRank = x.Rank,
               Highlights = new Dictionary<string, string>
               {
                   { "name", Headline(x.Business.Name, searchTerm) },
                   { "description", Headline(x.Business.Description, searchTerm) }
               },
               ResultPosition = x.Position
           })
           .ToListAsync(cancellationToken);

       var totalCount = await query.CountAsync(cancellationToken);

       return new PagedResult<SearchResult<Business>>(results, totalCount, page, pageSize);
   }
   ```

3. **Update Query Handler (1 hour)**
   ```csharp
   // File: src/LankaConnect.Application/Businesses/Queries/SearchBusinesses/SearchBusinessesQueryHandler.cs

   public class SearchBusinessesQueryHandler
       : IQueryHandler<SearchBusinessesQuery, PagedResult<SearchResult<BusinessDto>>>
   {
       public async Task<Result<PagedResult<SearchResult<BusinessDto>>>> Handle(...)
       {
           var repositoryResult = await _businessRepository.SearchAsync(
               request.SearchTerm,
               request.PageNumber,
               request.PageSize,
               cancellationToken);

           var dtoResults = repositoryResult.Items.Select(sr => new SearchResult<BusinessDto>
           {
               Item = _mapper.Map<BusinessDto>(sr.Item),
               SearchRank = sr.SearchRank,
               Highlights = sr.Highlights,
               ResultPosition = sr.ResultPosition
           }).ToList();

           return Result<PagedResult<SearchResult<BusinessDto>>>.Success(
               new PagedResult<SearchResult<BusinessDto>>(
                   dtoResults,
                   repositoryResult.TotalCount,
                   repositoryResult.Page,
                   repositoryResult.PageSize));
       }
   }
   ```

4. **Update Controller (30 minutes)**
   ```csharp
   // File: src/LankaConnect.API/Controllers/BusinessesController.cs

   [HttpGet("search")]
   [ProducesResponseType(typeof(PagedResult<SearchResult<BusinessDto>>), StatusCodes.Status200OK)]
   public async Task<IActionResult> SearchBusinesses(...)
   {
       var query = new SearchBusinessesQuery(searchTerm, ...);
       var result = await Mediator.Send(query);
       return HandleResult(result);
   }
   ```

5. **Update Frontend Types (1 hour)**
   ```typescript
   // File: web/src/infrastructure/api/types/common.types.ts

   export type SearchResult<T> = {
     item: T;
     searchRank: number;
     highlights: Record<string, string>;
     resultPosition: number;
   };

   // File: web/src/infrastructure/api/types/business.types.ts

   export type BusinessSearchResponse = PagedResult<SearchResult<BusinessDto>>;

   // File: web/src/presentation/hooks/useUnifiedSearch.ts

   export type UnifiedSearchResult = PagedResult<SearchResult<EventDto | BusinessDto>>;
   ```

6. **Update Search UI Components (1 hour)**
   ```typescript
   // File: web/src/presentation/components/SearchResultCard.tsx

   type SearchResultCardProps = {
     result: SearchResult<EventDto | BusinessDto>;
   };

   export function SearchResultCard({ result }: SearchResultCardProps) {
     const { item, searchRank, highlights } = result;

     return (
       <Card>
         <CardHeader>
           <div className="flex justify-between">
             <h3 dangerouslySetInnerHTML={{ __html: highlights.title || item.title }} />
             <Badge>Relevance: {(searchRank * 100).toFixed(0)}%</Badge>
           </div>
         </CardHeader>
         <CardContent>
           <p dangerouslySetInnerHTML={{ __html: highlights.description || item.description }} />
         </CardContent>
       </Card>
     );
   }
   ```

**Acceptance Criteria:**
- [ ] Business search returns `SearchResult<BusinessDto>` with search metadata
- [ ] Frontend displays search relevance scores
- [ ] Search term highlighting works in title and description
- [ ] User can sort by relevance (default) or other fields

---

### 8.3 Phase 3: Migrate Events API to SearchResult<T> (P2)

**Timeline:** 8 hours
**Goal:** Refactor Events to use `SearchResult<EventDto>` pattern (remove duplication)

**Tasks:**

1. **Deprecate EventSearchResultDto (30 minutes)**
   ```csharp
   // File: src/LankaConnect.Application/Events/Common/EventSearchResultDto.cs

   [Obsolete("Use SearchResult<EventDto> instead. This class will be removed in v2.0.0", false)]
   public class EventSearchResultDto
   {
       // Keep for backward compatibility during migration
   }
   ```

2. **Create Parallel V2 Endpoints (2 hours)**
   ```csharp
   // File: src/LankaConnect.API/Controllers/EventsController.cs

   /// <summary>
   /// NEW: Search events with generic SearchResult wrapper
   /// Replaces /search endpoint in v2.0.0
   /// </summary>
   [HttpGet("search/v2")]
   [ProducesResponseType(typeof(PagedResult<SearchResult<EventDto>>), StatusCodes.Status200OK)]
   public async Task<IActionResult> SearchEventsV2(
       [FromQuery] string? searchTerm,
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 10)
   {
       var query = new SearchEventsQueryV2(searchTerm, page, pageSize);
       var result = await Mediator.Send(query);
       return HandleResult(result);
   }

   /// <summary>
   /// DEPRECATED: Use /search/v2 instead
   /// </summary>
   [Obsolete("Use /search/v2 endpoint instead")]
   [HttpGet("search")]
   [ProducesResponseType(typeof(PagedResult<EventSearchResultDto>), StatusCodes.Status200OK)]
   public async Task<IActionResult> SearchEvents(
       [FromQuery] string? searchTerm,
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 10)
   {
       // Keep old implementation for backward compatibility
   }
   ```

3. **Create SearchEventsQueryV2 Handler (2 hours)**
   ```csharp
   // File: src/LankaConnect.Application/Events/Queries/SearchEventsV2/SearchEventsQueryV2.cs

   public record SearchEventsQueryV2(string? SearchTerm, int Page, int PageSize)
       : IQuery<PagedResult<SearchResult<EventDto>>>;

   // File: SearchEventsQueryV2Handler.cs

   public class SearchEventsQueryV2Handler
       : IQueryHandler<SearchEventsQueryV2, PagedResult<SearchResult<EventDto>>>
   {
       public async Task<Result<PagedResult<SearchResult<EventDto>>>> Handle(...)
       {
           // Use same repository method as Business
           var repositoryResult = await _eventRepository.SearchAsync(
               request.SearchTerm,
               request.Page,
               request.PageSize,
               cancellationToken);

           var dtoResults = repositoryResult.Items.Select(sr => new SearchResult<EventDto>
           {
               Item = _mapper.Map<EventDto>(sr.Item),
               SearchRank = sr.SearchRank,
               Highlights = sr.Highlights,
               ResultPosition = sr.ResultPosition
           }).ToList();

           return Result<PagedResult<SearchResult<EventDto>>>.Success(
               new PagedResult<SearchResult<EventDto>>(
                   dtoResults,
                   repositoryResult.TotalCount,
                   repositoryResult.Page,
                   repositoryResult.PageSize));
       }
   }
   ```

4. **Update Frontend to Use V2 Endpoints (2 hours)**
   ```typescript
   // File: web/src/infrastructure/api/repositories/events.repository.ts

   export const eventsRepository = {
     // NEW: Use v2 endpoint
     async searchEvents(request: SearchEventsRequest): Promise<EventSearchResponse> {
       const response = await apiClient.get<PagedResult<SearchResult<EventDto>>>(
         `/api/events/search/v2?searchTerm=${request.searchTerm}&page=${request.page}`
       );
       return response;
     },

     // DEPRECATED: Keep for rollback capability
     async searchEventsV1(request: SearchEventsRequest): Promise<PagedResult<EventSearchResultDto>> {
       const response = await apiClient.get<PagedResult<EventSearchResultDto>>(
         `/api/events/search?searchTerm=${request.searchTerm}&page=${request.page}`
       );
       return response;
     }
   };
   ```

5. **Testing & Validation (1.5 hours)**
   ```bash
   # Test both v1 and v2 endpoints return same data
   curl http://localhost:5000/api/events/search?searchTerm=yoga > v1.json
   curl http://localhost:5000/api/events/search/v2?searchTerm=yoga > v2.json

   # Compare results (item data should match)
   jq '.items[0]' v1.json  # EventSearchResultDto
   jq '.items[0].item' v2.json  # SearchResult<EventDto>.Item
   ```

**Acceptance Criteria:**
- [ ] Both `/search` and `/search/v2` endpoints work
- [ ] V2 endpoint uses `SearchResult<EventDto>` pattern
- [ ] Frontend works with both endpoints (A/B testing capability)
- [ ] No regressions in search functionality
- [ ] Migration plan documented for removing v1

---

### 8.4 Phase 4: Remove Deprecated Code (P3)

**Timeline:** 2 hours
**Goal:** Clean up deprecated code after migration complete

**Tasks:**

1. **Remove EventSearchResultDto (30 minutes)**
   ```bash
   # Delete file
   rm src/LankaConnect.Application/Events/Common/EventSearchResultDto.cs

   # Update all references to use SearchResult<EventDto>
   ```

2. **Remove PaginatedList.cs (30 minutes)**
   ```bash
   # Delete file
   rm src/LankaConnect.Application/Common/Models/PaginatedList.cs

   # Verify no references remain
   grep -r "PaginatedList" src/ --include="*.cs"
   ```

3. **Remove V1 Search Endpoints (30 minutes)**
   ```csharp
   // Delete deprecated endpoints from controllers
   - [HttpGet("search")]
   - public async Task<IActionResult> SearchEvents(...) { ... }
   ```

4. **Update Documentation (30 minutes)**
   ```markdown
   # docs/API_CHANGELOG.md

   ## v2.0.0 (Breaking Changes)

   ### Search Endpoints
   - **BREAKING:** `/api/events/search` removed → Use `/api/events/search/v2`
   - **BREAKING:** `EventSearchResultDto` removed → Use `SearchResult<EventDto>`
   - **BREAKING:** `PaginatedList<T>` removed → Use `PagedResult<T>`

   ### Migration Guide

   Old code:
   ```typescript
   const result: PagedResult<EventSearchResultDto> = await api.get('/events/search');
   console.log(result.items[0].title);
   ```

   New code:
   ```typescript
   const result: PagedResult<SearchResult<EventDto>> = await api.get('/events/search');
   console.log(result.items[0].item.title);
   ```
   ```

**Acceptance Criteria:**
- [ ] All deprecated files deleted
- [ ] All deprecated endpoints removed
- [ ] Build succeeds with 0 warnings
- [ ] Migration guide published
- [ ] Changelog updated

---

### 8.5 Phase 5: Implement Forums & Marketplace (Future)

**Timeline:** TBD (depends on product roadmap)

**Pattern to Follow:**
```csharp
// 1. Create ForumTopicDto
public record ForumTopicDto { ... }

// 2. Create SearchForumTopicsQuery
public record SearchForumTopicsQuery(...) : IQuery<PagedResult<SearchResult<ForumTopicDto>>>;

// 3. Create SearchForumTopicsQueryHandler
public class SearchForumTopicsQueryHandler
    : IQueryHandler<SearchForumTopicsQuery, PagedResult<SearchResult<ForumTopicDto>>>
{
    // Use same pattern as Events/Business
}

// 4. Create ForumsController : BaseController<ForumsController>
[HttpGet("search")]
public async Task<IActionResult> SearchTopics(...) => HandleResult(await Mediator.Send(query));

// 5. Update Frontend
export type ForumSearchResponse = PagedResult<SearchResult<ForumTopicDto>>;
```

**No additional architectural work needed** - Just replicate existing pattern!

---

## 9. Risk Assessment

### 9.1 Technical Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **Breaking changes to production API** | High | Critical | Versioned endpoints (/search/v2), parallel deployment |
| **Frontend type errors during migration** | Medium | High | TypeScript strict mode, comprehensive testing |
| **Performance degradation with nested JSON** | Low | Medium | Benchmark tests, response size monitoring |
| **Database query performance issues** | Medium | Medium | Index on search_vector, EXPLAIN ANALYZE queries |
| **SearchResult<T> serialization issues** | Low | High | Integration tests, JSON schema validation |
| **Backward compatibility breakage** | Medium | High | Keep deprecated code for 1 release cycle |

### 9.2 Risk Mitigation Strategies

#### Strategy 1: Parallel Endpoint Deployment

**Approach:**
```csharp
// Deploy both v1 and v2 endpoints simultaneously
[HttpGet("search")]      // v1 - deprecated
[HttpGet("search/v2")]   // v2 - new

// Frontend feature flag
const USE_SEARCH_V2 = process.env.NEXT_PUBLIC_SEARCH_V2_ENABLED === 'true';

const endpoint = USE_SEARCH_V2 ? '/search/v2' : '/search';
```

**Benefits:**
- Zero-downtime deployment
- Gradual rollout (A/B testing)
- Instant rollback capability
- Production validation before removing v1

#### Strategy 2: Response Size Monitoring

**Concern:** `SearchResult<T>` adds extra nesting layer

**Before:**
```json
{
  "items": [
    { "id": "1", "title": "Event A" }  // 35 bytes
  ]
}
```

**After:**
```json
{
  "items": [
    {
      "item": { "id": "1", "title": "Event A" },  // 42 bytes
      "searchRank": 0.87,                         // 18 bytes
      "highlights": {},                            // 15 bytes
      "resultPosition": 1                          // 19 bytes
    }
  ]
}
```

**Impact:** +94 bytes per item (~170% increase)

**Mitigation:**
```csharp
// Option 1: Exclude empty highlights from JSON
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
public Dictionary<string, string> Highlights { get; init; } = new();

// Option 2: Enable gzip compression in Kestrel
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});

// Result: 170% uncompressed → ~30% compressed (JSON compresses well)
```

#### Strategy 3: Database Query Optimization

**Concern:** `ts_rank()` calculation adds CPU cost

**Benchmark Tests:**
```sql
-- Test query performance with ts_rank
EXPLAIN ANALYZE
SELECT
    e.*,
    ts_rank(e.search_vector, to_tsquery('yoga')) AS rank
FROM events e
WHERE e.search_vector @@ to_tsquery('yoga')
ORDER BY rank DESC
LIMIT 10;

-- Expected: < 50ms for 10k events with GIN index
```

**Optimizations:**
```sql
-- 1. Create GIN index on search_vector
CREATE INDEX idx_events_search_vector ON events USING GIN(search_vector);

-- 2. Use covering index for common queries
CREATE INDEX idx_events_search_covering ON events (status, start_date)
INCLUDE (id, title, description)
WHERE status = 'Published';

-- 3. Materialized view for popular searches (optional)
CREATE MATERIALIZED VIEW popular_event_searches AS
SELECT search_term, results
FROM cached_searches
WHERE search_count > 100;
```

### 9.3 Rollback Plan

**Scenario:** V2 endpoints cause production issues

**Rollback Steps:**

1. **Immediate (< 5 minutes):**
   ```typescript
   // Frontend: Disable v2 via feature flag
   NEXT_PUBLIC_SEARCH_V2_ENABLED=false
   // Deploy frontend change
   vercel --prod
   ```

2. **Short-term (< 1 hour):**
   ```bash
   # Backend: Remove v2 endpoints
   git revert <commit-hash>
   dotnet publish -c Release
   # Deploy to Azure
   ```

3. **Validation:**
   ```bash
   # Verify v1 endpoints still work
   curl https://api.lankaconnect.com/api/events/search?searchTerm=test
   # Check logs for errors
   az monitor logs query --workspace <id> --query "exceptions | where timestamp > ago(1h)"
   ```

**Recovery Time Objective (RTO):** < 15 minutes
**Recovery Point Objective (RPO):** 0 (no data loss, only feature rollback)

---

## 10. Testing Strategy

### 10.1 Unit Tests

**Coverage Target:** 90% for new code

#### Test 1: SearchResult<T> Serialization
```csharp
public class SearchResultTests
{
    [Fact]
    public void SearchResult_SerializesToJson_Correctly()
    {
        // Arrange
        var eventDto = new EventDto { Id = Guid.NewGuid(), Title = "Yoga Class" };
        var searchResult = new SearchResult<EventDto>
        {
            Item = eventDto,
            SearchRank = 0.87,
            Highlights = new() { { "title", "Yoga <mark>Class</mark>" } },
            ResultPosition = 1
        };

        // Act
        var json = JsonSerializer.Serialize(searchResult);
        var deserialized = JsonSerializer.Deserialize<SearchResult<EventDto>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(eventDto.Id, deserialized.Item.Id);
        Assert.Equal(0.87, deserialized.SearchRank);
        Assert.Equal("Yoga <mark>Class</mark>", deserialized.Highlights["title"]);
    }
}
```

#### Test 2: PagedResult<T> Calculation
```csharp
public class PagedResultTests
{
    [Theory]
    [InlineData(100, 10, 1, 10, 1, true, false)]   // First page
    [InlineData(100, 10, 5, 10, 5, true, true)]    // Middle page
    [InlineData(100, 10, 10, 10, 10, false, true)]  // Last page
    public void PagedResult_CalculatesPaginationMetadata_Correctly(
        int totalCount, int totalPages, int page, int pageSize,
        int expectedTotalPages, bool expectedHasPrevious, bool expectedHasNext)
    {
        // Arrange
        var items = Enumerable.Range(1, pageSize).ToList();

        // Act
        var result = new PagedResult<int>(items, totalCount, page, pageSize);

        // Assert
        Assert.Equal(expectedTotalPages, result.TotalPages);
        Assert.Equal(expectedHasPrevious, result.HasPreviousPage);
        Assert.Equal(expectedHasNext, result.HasNextPage);
    }
}
```

#### Test 3: BaseController HandleResult
```csharp
public class BaseControllerTests
{
    [Fact]
    public async Task HandleResult_WithSuccess_ReturnsOkResult()
    {
        // Arrange
        var controller = new TestController();
        var successResult = Result<string>.Success("Test data");

        // Act
        var actionResult = controller.TestHandleResult(successResult);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("Test data", okResult.Value);
    }

    [Fact]
    public async Task HandleResult_WithFailure_ReturnsBadRequest()
    {
        // Arrange
        var controller = new TestController();
        var failureResult = Result<string>.Failure("Validation error");

        // Act
        var actionResult = controller.TestHandleResult(failureResult);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal("Validation error", problemDetails.Detail);
    }
}
```

### 10.2 Integration Tests

#### Test 1: Business Search API
```csharp
public class BusinessSearchIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BusinessSearchIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SearchBusinesses_ReturnsPagedResult_NotWrappedInResult()
    {
        // Arrange
        var searchTerm = "restaurant";

        // Act
        var response = await _client.GetAsync($"/api/businesses/search?searchTerm={searchTerm}");

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        // Should NOT have Result<T> wrapper
        Assert.DoesNotContain("\"isSuccess\"", json);
        Assert.DoesNotContain("\"errors\"", json);

        // Should have PagedResult properties
        Assert.Contains("\"items\"", json);
        Assert.Contains("\"page\"", json);
        Assert.Contains("\"totalCount\"", json);

        var result = JsonSerializer.Deserialize<PagedResult<SearchResult<BusinessDto>>>(json);
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task SearchBusinesses_WithSearchResult_IncludesSearchMetadata()
    {
        // Arrange
        await SeedDatabase();  // Add test businesses

        // Act
        var response = await _client.GetAsync("/api/businesses/search?searchTerm=yoga");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<SearchResult<BusinessDto>>>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Items.Count > 0);

        var firstResult = result.Items.First();
        Assert.NotNull(firstResult.Item);  // BusinessDto
        Assert.True(firstResult.SearchRank > 0);  // Has relevance score
        Assert.NotEmpty(firstResult.Highlights);  // Has highlights
    }
}
```

#### Test 2: Events Search V2 API
```csharp
public class EventsSearchV2IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task SearchEventsV2_ReturnsSearchResultFormat()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var response = await _client.GetAsync("/api/events/search/v2?searchTerm=yoga");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<SearchResult<EventDto>>>();

        // Assert
        Assert.NotNull(result);
        var firstResult = result.Items.First();

        // Verify SearchResult<EventDto> structure
        Assert.NotNull(firstResult.Item);
        Assert.IsType<EventDto>(firstResult.Item);
        Assert.True(firstResult.SearchRank > 0);
        Assert.True(firstResult.ResultPosition > 0);
    }

    [Fact]
    public async Task SearchEventsV1AndV2_ReturnSameData()
    {
        // Arrange
        await SeedDatabase();

        // Act
        var v1Response = await _client.GetAsync("/api/events/search?searchTerm=yoga");
        var v2Response = await _client.GetAsync("/api/events/search/v2?searchTerm=yoga");

        var v1Result = await v1Response.Content.ReadFromJsonAsync<PagedResult<EventSearchResultDto>>();
        var v2Result = await v2Response.Content.ReadFromJsonAsync<PagedResult<SearchResult<EventDto>>>();

        // Assert
        Assert.Equal(v1Result.TotalCount, v2Result.TotalCount);
        Assert.Equal(v1Result.Items[0].Id, v2Result.Items[0].Item.Id);  // Same event
        Assert.Equal(v1Result.Items[0].Title, v2Result.Items[0].Item.Title);
    }
}
```

### 10.3 Frontend E2E Tests

#### Test 1: Unified Search Integration
```typescript
// tests/e2e/unified-search.spec.ts

import { test, expect } from '@playwright/test';

test.describe('Unified Search', () => {
  test('Business search displays results with metadata', async ({ page }) => {
    // Arrange
    await page.goto('/search');
    await page.fill('[data-testid="search-input"]', 'restaurant');

    // Act
    await page.click('[data-testid="business-tab"]');
    await page.waitForSelector('[data-testid="search-result"]');

    // Assert
    const results = await page.locator('[data-testid="search-result"]');
    await expect(results).toHaveCount(10);  // First page

    // Verify search metadata visible
    const firstResult = results.first();
    await expect(firstResult.locator('[data-testid="relevance-badge"]')).toBeVisible();
    await expect(firstResult.locator('[data-testid="highlighted-text"]')).toContainText('restaurant');
  });

  test('Pagination works correctly', async ({ page }) => {
    // Arrange
    await page.goto('/search?q=yoga&type=events');
    await page.waitForSelector('[data-testid="pagination"]');

    // Act
    await page.click('[data-testid="next-page-button"]');

    // Assert
    await expect(page).toHaveURL(/page=2/);
    await expect(page.locator('[data-testid="current-page"]')).toHaveText('2');
  });

  test('Switch between entity types preserves search term', async ({ page }) => {
    // Arrange
    await page.goto('/search');
    await page.fill('[data-testid="search-input"]', 'yoga');
    await page.click('[data-testid="events-tab"]');
    await page.waitForSelector('[data-testid="search-result"]');

    // Act
    await page.click('[data-testid="business-tab"]');
    await page.waitForSelector('[data-testid="search-result"]');

    // Assert
    await expect(page.locator('[data-testid="search-input"]')).toHaveValue('yoga');
    await expect(page).toHaveURL(/type=business/);
  });
});
```

### 10.4 Performance Tests

#### Test 1: Response Size Comparison
```csharp
[Fact]
public async Task SearchResult_ResponseSize_IsAcceptable()
{
    // Arrange
    await SeedDatabase(1000);  // 1000 test events

    // Act
    var v1Response = await _client.GetAsync("/api/events/search?pageSize=50");
    var v2Response = await _client.GetAsync("/api/events/search/v2?pageSize=50");

    var v1Size = v1Response.Content.Headers.ContentLength;
    var v2Size = v2Response.Content.Headers.ContentLength;

    // Assert
    var sizeIncrease = ((v2Size - v1Size) / (double)v1Size) * 100;
    Assert.True(sizeIncrease < 200, $"Response size increased by {sizeIncrease}% (expected < 200%)");

    // With gzip compression
    Assert.True(v2Size < 50_000, "Compressed response should be < 50KB");
}
```

#### Test 2: Query Performance
```csharp
[Fact]
public async Task SearchWithRanking_CompletesWithinTimeLimit()
{
    // Arrange
    await SeedDatabase(10_000);  // 10k events

    var stopwatch = Stopwatch.StartNew();

    // Act
    var response = await _client.GetAsync("/api/events/search/v2?searchTerm=yoga&pageSize=20");

    stopwatch.Stop();

    // Assert
    Assert.True(stopwatch.ElapsedMilliseconds < 500,
        $"Search took {stopwatch.ElapsedMilliseconds}ms (expected < 500ms)");
}
```

---

## 11. Decision Records

### ADR-001: Use Generic SearchResult<T> Wrapper

**Status:** APPROVED
**Date:** 2025-12-31
**Deciders:** System Architecture Designer, Development Team

**Context:**

Need to standardize search result format across Events, Business, Forums, and Marketplace entities. Current approach (EventSearchResultDto duplicates EventDto properties) is not scalable.

**Decision:**

Implement `SearchResult<T>` generic wrapper that separates entity data from search metadata.

**Rationale:**

1. **Eliminates DTO duplication** - Base DTOs remain unchanged
2. **Automatic synchronization** - Changes to EventDto/BusinessDto automatically reflected
3. **Consistent pattern** - Same search metadata across ALL entities
4. **Type-safe** - Generic constraints preserve entity-specific information
5. **Extensible** - Easy to add new search metadata fields

**Consequences:**

**Positive:**
- Reduces maintenance burden (no manual DTO synchronization)
- Consistent developer experience across entities
- Future-proof for new entities (Forums, Marketplace)

**Negative:**
- Requires frontend refactoring (`result.item.title` instead of `result.title`)
- One extra nesting level in JSON (mitigated by gzip compression)
- Migration effort for existing Events endpoints

**Alternatives Considered:**

1. **Entity-specific DTOs** - Rejected due to high maintenance overhead
2. **ISearchResult interface** - Rejected due to loss of type safety
3. **Keep current approach** - Rejected due to scalability issues

---

### ADR-002: Standardize on PagedResult<T>

**Status:** APPROVED
**Date:** 2025-12-31

**Context:**

Two pagination classes exist: `PagedResult<T>` (Events) and `PaginatedList<T>` (Business). Property names differ (`page` vs `pageNumber`), causing frontend complexity.

**Decision:**

Deprecate `PaginatedList<T>` and standardize on `PagedResult<T>` for ALL paginated endpoints.

**Rationale:**

1. **Majority usage** - 43 Events endpoints vs 2 Business endpoints
2. **Better naming** - `Page` is clearer than `PageNumber`
3. **Frontend compatibility** - Frontend already handles `page` property
4. **Minimal migration** - Only 2 endpoints need changes

**Consequences:**

**Positive:**
- Single source of truth for pagination
- Simplified frontend type definitions
- Reduced codebase complexity

**Negative:**
- Breaking change for Business API consumers (mitigated by versioning)
- Migration effort for Business endpoints

---

### ADR-003: Enforce BaseController<T> Inheritance

**Status:** APPROVED
**Date:** 2025-12-31

**Context:**

`Result<T>` pattern is used for error handling in application layer. Some controllers (Business) expose `Result<T>` to API clients, violating architectural layering.

**Decision:**

ALL controllers MUST inherit from `BaseController<T>` and use `HandleResult()` to unwrap `Result<T>` before returning to clients.

**Rationale:**

1. **Separation of concerns** - `Result<T>` is domain pattern, not API contract
2. **Consistent error handling** - All endpoints return RFC 7807 Problem Details
3. **DRY principle** - Error handling logic in one place
4. **Logging infrastructure** - Logger available in all controllers

**Consequences:**

**Positive:**
- Clean API contracts (no internal abstractions exposed)
- Consistent error responses across all endpoints
- Centralized logging and monitoring

**Negative:**
- BusinessesController requires refactoring (estimated 1 hour)

---

## 12. Appendix

### 12.1 File Inventory

**Files to Create:**
- `src/LankaConnect.Application/Common/Models/SearchResult.cs` (NEW)
- `src/LankaConnect.Application/Events/Queries/SearchEventsV2/SearchEventsQueryV2.cs` (NEW)
- `src/LankaConnect.Application/Events/Queries/SearchEventsV2/SearchEventsQueryV2Handler.cs` (NEW)
- `docs/ARCHITECTURE_REVIEW_Unified_Search_System.md` (THIS FILE)

**Files to Modify:**
- `src/LankaConnect.API/Controllers/BusinessesController.cs` (Inherit from BaseController)
- `src/LankaConnect.Application/Businesses/Queries/SearchBusinesses/SearchBusinessesQueryHandler.cs` (Change return type)
- `src/LankaConnect.Infrastructure/Persistence/Repositories/BusinessRepository.cs` (Add SearchAsync method)
- `web/src/infrastructure/api/types/common.types.ts` (Add SearchResult<T> type)
- `web/src/infrastructure/api/types/business.types.ts` (Update response types)
- `web/src/presentation/hooks/useUnifiedSearch.ts` (Update type handling)

**Files to Deprecate (Phase 4):**
- `src/LankaConnect.Application/Common/Models/PaginatedList.cs` (Mark as [Obsolete])
- `src/LankaConnect.Application/Events/Common/EventSearchResultDto.cs` (Mark as [Obsolete])

**Files to Delete (Phase 4, after migration):**
- `src/LankaConnect.Application/Common/Models/PaginatedList.cs`
- `src/LankaConnect.Application/Events/Common/EventSearchResultDto.cs`

### 12.2 API Endpoint Inventory

**Current State:**

| Entity | Endpoint | Response Type | Status |
|--------|----------|---------------|--------|
| Events | GET /api/events/search | PagedResult<EventSearchResultDto> | ✅ Works |
| Business | GET /api/businesses/search | Result<PaginatedList<BusinessDto>> | ❌ BROKEN |

**After Phase 1:**

| Entity | Endpoint | Response Type | Status |
|--------|----------|---------------|--------|
| Events | GET /api/events/search | PagedResult<EventSearchResultDto> | ✅ Works |
| Business | GET /api/businesses/search | PagedResult<BusinessDto> | ✅ FIXED |

**After Phase 2:**

| Entity | Endpoint | Response Type | Status |
|--------|----------|---------------|--------|
| Events | GET /api/events/search | PagedResult<EventSearchResultDto> | ✅ Works |
| Business | GET /api/businesses/search | PagedResult<SearchResult<BusinessDto>> | ✅ New format |

**After Phase 3 (Final):**

| Entity | Endpoint | Response Type | Status |
|--------|----------|---------------|--------|
| Events | GET /api/events/search (deprecated) | PagedResult<EventSearchResultDto> | ⚠️ Deprecated |
| Events | GET /api/events/search/v2 | PagedResult<SearchResult<EventDto>> | ✅ New format |
| Business | GET /api/businesses/search | PagedResult<SearchResult<BusinessDto>> | ✅ New format |
| Forums | GET /api/forums/search | PagedResult<SearchResult<ForumTopicDto>> | 🔮 Future |
| Marketplace | GET /api/marketplace/search | PagedResult<SearchResult<MarketplaceItemDto>> | 🔮 Future |

### 12.3 TypeScript Type Definitions Reference

**Final Type Structure:**

```typescript
// types/common.types.ts
export type SearchResult<T> = {
  item: T;
  searchRank: number;
  highlights: Record<string, string>;
  resultPosition: number;
};

export type PagedResult<T> = {
  items: readonly T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

// types/events.types.ts
export type EventDto = {
  id: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  // ... 27+ properties
};

export type EventSearchResponse = PagedResult<SearchResult<EventDto>>;

// types/business.types.ts
export type BusinessDto = {
  id: string;
  name: string;
  description: string;
  // ... 30 properties
};

export type BusinessSearchResponse = PagedResult<SearchResult<BusinessDto>>;

// types/search.types.ts
export type EntityType = 'events' | 'business' | 'forums' | 'marketplace';

export type TypedSearchResult<T extends EntityType> =
  T extends 'events' ? SearchResult<EventDto> :
  T extends 'business' ? SearchResult<BusinessDto> :
  T extends 'forums' ? SearchResult<ForumTopicDto> :
  T extends 'marketplace' ? SearchResult<MarketplaceItemDto> :
  never;
```

---

## Conclusion

This architectural review provides a comprehensive design for the unified search system that:

1. **Fixes immediate issues** - Business API format mismatch resolved
2. **Implements best practices** - Generic wrappers, consistent patterns, type safety
3. **Scales to future entities** - Forums and Marketplace can use same pattern
4. **Reduces technical debt** - Eliminates DTO duplication and inconsistent pagination
5. **Maintains backward compatibility** - Gradual migration with versioned endpoints

**Next Steps:**

1. Review and approve this document
2. Create GitHub issues for each phase
3. Begin Phase 1 implementation (Business API fix)
4. Schedule team meeting to discuss migration timeline

**Estimated Total Effort:** 20-24 hours
**Recommended Timeline:** 3 sprints (2 weeks each)

---

**Document Version:** 1.0
**Last Updated:** 2025-12-31
**Author:** System Architecture Designer
**Reviewers:** [To be assigned]
