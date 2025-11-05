# ADR-007: Swagger Missing Endpoints - Root Cause Analysis

**Status**: IDENTIFIED
**Date**: 2025-01-04
**Decision Makers**: System Architect
**Tags**: #swagger #openapi #bug-analysis #dto-design

## Context

After deploying to Azure App Service (run 19086696362), 7 Event endpoints were missing from Swagger JSON despite:
- Endpoints existing in EventsController.cs
- FileUploadOperationFilter being added to Program.cs
- Successful build with 0 errors
- Proper HTTP attributes ([HttpGet], [HttpPost], etc.)
- Proper ProducesResponseType attributes

### Missing Endpoints
1. GET /api/Events/search
2. POST /api/Events/{id}/waiting-list
3. DELETE /api/Events/{id}/waiting-list
4. POST /api/Events/{id}/waiting-list/promote
5. GET /api/Events/{id}/waiting-list
6. GET /api/Events/{id}/ics
7. POST /api/Events/{id}/share

## Investigation Findings

### Initial Hypotheses (All WRONG)
1. ❌ FileUploadOperationFilter issue - Already added, didn't fix it
2. ❌ Missing CQRS handler registration - All handlers are registered via MediatR assembly scanning
3. ❌ Internal visibility on DTOs - All DTOs are public
4. ❌ Swagger document/operation filters excluding endpoints - No exclusion filters found
5. ❌ ApiExplorer filtering - No [ApiExplorerSettings(IgnoreApi = true)] found
6. ❌ Route conflicts - All routes are unique and properly defined
7. ❌ Build configuration issue - Release build succeeds with 0 errors

### ROOT CAUSE IDENTIFIED

**Primary Issue**: `PagedResult<T>` lacks a parameterless constructor

**File**: `src/LankaConnect.Application/Common/Models/PagedResult.cs`

```csharp
public class PagedResult<T>
{
    // Properties with init-only getters
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }

    // ONLY constructor - requires all parameters
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
```

**Why This Breaks Swagger**:
1. Swashbuckle/OpenAPI schema generation requires DTOs to be **serializable**
2. System.Text.Json and Newtonsoft.Json prefer parameterless constructors for deserialization
3. Without a parameterless constructor, Swagger cannot generate a valid schema
4. When schema generation fails for a return type, the ENTIRE endpoint is excluded from swagger.json

**Evidence**:
- Only `/api/Events/search` returns `PagedResult<EventSearchResultDto>`
- All other endpoints return simple DTOs, collections, or primitives
- PowerShell check confirmed: "PagedResult has visibility issues"

### Secondary Question: Why Are 6 Other Endpoints Missing?

**Initial Assessment**: The other 6 endpoints (waiting-list, ics, share) should NOT be affected by PagedResult<T> issue.

**Possible Explanations**:
1. User may have miscounted Swagger endpoints
2. Endpoints may be grouped/collapsed in Swagger UI but present in JSON
3. Build artifacts may be cached on Azure (old build without these endpoints)
4. Deployment may have failed to copy latest DLL files

**Requires Verification**: After fixing PagedResult<T>, check if all 7 or only 1 endpoint appears.

## Decision

### Fix Strategy

**Approach 1: Add Parameterless Constructor (RECOMMENDED)**
```csharp
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }

    // Parameterless constructor for Swagger/serialization
    public PagedResult()
    {
    }

    // Primary constructor for creating instances
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

**Approach 2: Use Swashbuckle Schema Filter (NOT RECOMMENDED)**
- Add custom ISchemaFilter to manually define PagedResult<T> schema
- Complex, error-prone, requires maintenance for every generic type

**Approach 3: Replace with Record Type (NOT VIABLE)**
- Records with positional parameters also require all parameters
- Would need to add parameterless constructor anyway

### Trade-offs

| Approach | Pros | Cons |
|----------|------|------|
| Parameterless Constructor | ✅ Simple, standard pattern<br>✅ Works with all serializers<br>✅ Swagger auto-generates schema<br>✅ No custom filters needed | ⚠️ Loses immutability (properties become settable)<br>⚠️ Can create invalid instances |
| Schema Filter | ✅ Keeps immutability | ❌ Complex implementation<br>❌ Maintenance burden<br>❌ Error-prone |
| Record Type | ✅ Concise syntax | ❌ Still needs parameterless constructor<br>❌ Same immutability loss |

## Consequences

### Positive
- ✅ Fixes Swagger schema generation for `/api/Events/search`
- ✅ Standard pattern used across .NET ecosystem
- ✅ Compatible with System.Text.Json and Newtonsoft.Json
- ✅ No custom Swagger configuration needed

### Negative
- ⚠️ PagedResult<T> properties become mutable (can be set after construction)
- ⚠️ Developers could create invalid PagedResult instances (e.g., negative page numbers)
- ⚠️ Loses compile-time guarantees of immutability

### Mitigation
1. Document that PagedResult should only be created via the parameterized constructor
2. Add unit tests to verify proper PagedResult creation
3. Consider adding a Validate() method for runtime checks
4. Use code reviews to prevent misuse

## Alternative Considered: Wrapper Pattern

**Not Chosen**: Create a separate `PagedResultDto<T>` for API responses:

```csharp
// Internal domain model (immutable)
public class PagedResult<T> { /* existing code */ }

// Public API DTO (mutable, Swagger-friendly)
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; }
    public int TotalCount { get; set; }
    // ...
}
```

**Why Not Chosen**:
- Adds mapping layer complexity
- Requires AutoMapper configuration
- More code to maintain
- Minimal benefit for this use case

## Implementation Plan

1. ✅ Update `PagedResult<T>` to add parameterless constructor
2. ✅ Change properties from init-only to set
3. ✅ Test Swagger JSON generation locally
4. ✅ Deploy to staging
5. ✅ Verify all 31+ endpoints visible in Swagger
6. ✅ Run integration tests
7. ✅ Document in release notes

## References

- [Swashbuckle Schema Generation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore#schema-generation-options)
- [System.Text.Json Deserialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization)
- [OpenAPI Specification - Schema Object](https://swagger.io/specification/#schema-object)

## Related ADRs

- ADR-001: Clean Architecture with DDD
- ADR-003: CQRS with MediatR
- ADR-006: Swagger/OpenAPI Documentation Strategy
