# PostgreSQL Full-Text Search: Implementation Checklist

## Quick Reference: File Locations

### Domain Layer (`src/LankaConnect.Domain/`)
```
Domain/
├── Specifications/
│   └── Events/
│       └── EventSearchSpecification.cs       [NEW]
├── Repositories/
│   └── IEventRepository.cs                    [MODIFY - Add SearchAsync, CountSearchAsync]
```

### Application Layer (`src/LankaConnect.Application/`)
```
Application/
├── Events/
│   ├── Queries/
│   │   └── SearchEvents/
│   │       ├── SearchEventsQuery.cs           [NEW]
│   │       ├── SearchEventsQueryHandler.cs    [NEW]
│   │       ├── SearchEventsQueryValidator.cs  [NEW]
│   │       └── EventSearchResultDto.cs        [NEW]
│   └── MappingProfiles/
│       └── EventMappingProfile.cs             [MODIFY - Add EventSearchResultDto mapping]
├── Common/
│   └── Models/
│       └── PagedResult.cs                     [NEW or MODIFY if exists]
```

### Infrastructure Layer (`src/LankaConnect.Infrastructure/`)
```
Infrastructure/
├── Data/
│   ├── Configuration/
│   │   └── EventConfiguration.cs              [MODIFY - Add SearchVector property]
│   └── Repositories/
│       └── EventRepository.cs                 [MODIFY - Add SearchAsync, CountSearchAsync]
├── Migrations/
│   └── 20251104_AddFullTextSearchSupport.cs   [NEW]
```

### API Layer (`src/LankaConnect.API/`)
```
API/
├── Controllers/
│   └── EventsController.cs                    [MODIFY - Add SearchEvents endpoint]
└── Program.cs                                 [MODIFY - Add services if needed]
```

### Tests (`tests/`)
```
Tests/
├── Application.Tests/
│   └── Events/
│       └── Queries/
│           ├── SearchEventsQueryHandlerTests.cs        [NEW]
│           └── SearchEventsQueryValidatorTests.cs      [NEW]
├── Infrastructure.Tests/
│   └── Repositories/
│       └── EventRepositoryIntegrationTests.cs          [NEW]
└── API.Tests/
    └── Controllers/
        └── EventsControllerIntegrationTests.cs         [NEW]
```

---

## Implementation Phases

### Phase 1: Database Migration (30 minutes)

#### Step 1.1: Create Migration File
```bash
cd src/LankaConnect.Infrastructure
dotnet ef migrations add AddFullTextSearchSupport
```

#### Step 1.2: Verify Migration Content
```csharp
// Migrations/20251104_AddFullTextSearchSupport.cs
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add computed tsvector column
    migrationBuilder.Sql(@"
        ALTER TABLE events
        ADD COLUMN search_vector tsvector
        GENERATED ALWAYS AS (
            setweight(to_tsvector('english', COALESCE(title, '')), 'A') ||
            setweight(to_tsvector('english', COALESCE(description, '')), 'B')
        ) STORED;
    ");

    // Create GIN index
    migrationBuilder.Sql(@"
        CREATE INDEX idx_events_search_vector
        ON events
        USING GIN(search_vector)
        WITH (fastupdate = off);
    ");

    // Add supporting indexes for filters
    migrationBuilder.Sql(@"
        CREATE INDEX idx_events_category ON events(category) WHERE category IS NOT NULL;
        CREATE INDEX idx_events_start_date ON events(start_date);
        CREATE INDEX idx_events_ticket_price ON events(ticket_price) WHERE ticket_price IS NOT NULL;
    ");
}
```

#### Step 1.3: Apply Migration
```bash
dotnet ef database update
```

#### Step 1.4: Verify Index Creation
```sql
-- Connect to PostgreSQL and run:
\d events
SELECT * FROM pg_indexes WHERE tablename = 'events';
```

**Checklist:**
- [ ] Migration created successfully
- [ ] Migration applied without errors
- [ ] `search_vector` column exists in `events` table
- [ ] GIN index `idx_events_search_vector` created
- [ ] Supporting indexes created
- [ ] Test data populated with search vectors

---

### Phase 2: Domain Layer (15 minutes)

#### Step 2.1: Create EventSearchSpecification
```csharp
// Domain/Specifications/Events/EventSearchSpecification.cs
namespace LankaConnect.Domain.Specifications.Events;

public class EventSearchSpecification : ISpecification<Event>
{
    public string SearchTerm { get; }
    public string? Category { get; }
    public bool? IsFreeOnly { get; }
    public DateTime? StartDateFrom { get; }

    public EventSearchSpecification(
        string searchTerm,
        string? category = null,
        bool? isFreeOnly = null,
        DateTime? startDateFrom = null)
    {
        SearchTerm = Guard.Against.NullOrWhiteSpace(searchTerm, nameof(searchTerm));
        Category = category;
        IsFreeOnly = isFreeOnly;
        StartDateFrom = startDateFrom;
    }

    public bool IsSatisfiedBy(Event entity) => true;
}
```

#### Step 2.2: Update IEventRepository Interface
```csharp
// Domain/Repositories/IEventRepository.cs
public interface IEventRepository
{
    // Existing methods...

    Task<IReadOnlyList<Event>> SearchAsync(
        EventSearchSpecification specification,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountSearchAsync(
        EventSearchSpecification specification,
        CancellationToken cancellationToken = default);
}
```

**Checklist:**
- [ ] EventSearchSpecification created
- [ ] ISpecification<Event> interface implemented
- [ ] Guard clauses for validation
- [ ] IEventRepository extended with search methods
- [ ] No PostgreSQL-specific code in domain layer

---

### Phase 3: Infrastructure Layer (45 minutes)

#### Step 3.1: Update EventConfiguration
```csharp
// Infrastructure/Data/Configuration/EventConfiguration.cs
public void Configure(EntityTypeBuilder<Event> builder)
{
    // Existing configuration...

    // Configure computed column (EF Core aware)
    builder.Property<NpgsqlTsVector>("SearchVector")
        .HasColumnName("search_vector")
        .HasComputedColumnSql(
            "setweight(to_tsvector('english', COALESCE(title, '')), 'A') || " +
            "setweight(to_tsvector('english', COALESCE(description, '')), 'B')",
            stored: true)
        .IsRequired(false);

    builder.Ignore("SearchVector");
}
```

#### Step 3.2: Implement Repository Methods
```csharp
// Infrastructure/Data/Repositories/EventRepository.cs
using Npgsql;

public async Task<IReadOnlyList<Event>> SearchAsync(
    EventSearchSpecification specification,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
{
    var offset = (page - 1) * pageSize;

    var sql = @"
        SELECT e.*, ts_rank(e.search_vector, websearch_to_tsquery('english', @searchTerm)) AS rank
        FROM events e
        WHERE e.search_vector @@ websearch_to_tsquery('english', @searchTerm)
          AND (@category IS NULL OR e.category = @category)
          AND (@isFreeOnly IS NULL OR
               (@isFreeOnly = true AND (e.ticket_price IS NULL OR e.ticket_price = 0)) OR
               (@isFreeOnly = false))
          AND (@startDateFrom IS NULL OR e.start_date >= @startDateFrom)
        ORDER BY rank DESC, e.start_date ASC
        LIMIT @pageSize OFFSET @offset;
    ";

    var parameters = new[]
    {
        new NpgsqlParameter("@searchTerm", specification.SearchTerm),
        new NpgsqlParameter("@category", (object?)specification.Category ?? DBNull.Value),
        new NpgsqlParameter("@isFreeOnly", (object?)specification.IsFreeOnly ?? DBNull.Value),
        new NpgsqlParameter("@startDateFrom", (object?)specification.StartDateFrom ?? DBNull.Value),
        new NpgsqlParameter("@pageSize", pageSize),
        new NpgsqlParameter("@offset", offset)
    };

    return await _context.Events
        .FromSqlRaw(sql, parameters)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}

public async Task<int> CountSearchAsync(
    EventSearchSpecification specification,
    CancellationToken cancellationToken = default)
{
    var sql = @"
        SELECT COUNT(*)::integer
        FROM events e
        WHERE e.search_vector @@ websearch_to_tsquery('english', @searchTerm)
          AND (@category IS NULL OR e.category = @category)
          AND (@isFreeOnly IS NULL OR
               (@isFreeOnly = true AND (e.ticket_price IS NULL OR e.ticket_price = 0)) OR
               (@isFreeOnly = false))
          AND (@startDateFrom IS NULL OR e.start_date >= @startDateFrom);
    ";

    var parameters = new[]
    {
        new NpgsqlParameter("@searchTerm", specification.SearchTerm),
        new NpgsqlParameter("@category", (object?)specification.Category ?? DBNull.Value),
        new NpgsqlParameter("@isFreeOnly", (object?)specification.IsFreeOnly ?? DBNull.Value),
        new NpgsqlParameter("@startDateFrom", (object?)specification.StartDateFrom ?? DBNull.Value)
    };

    return await _context.Database
        .SqlQueryRaw<int>(sql, parameters)
        .FirstOrDefaultAsync(cancellationToken);
}
```

**Checklist:**
- [ ] EventConfiguration updated with SearchVector property
- [ ] SearchAsync method implemented with parameterized SQL
- [ ] CountSearchAsync method implemented
- [ ] AsNoTracking() used for read-only queries
- [ ] SQL injection prevention via parameterized queries
- [ ] Error handling and logging added

---

### Phase 4: Application Layer (45 minutes)

#### Step 4.1: Create Query, DTO, and Validator
See detailed implementations in `docs/architecture/full-text-search-application-layer.md`

**Files to create:**
1. `SearchEventsQuery.cs`
2. `EventSearchResultDto.cs`
3. `SearchEventsQueryValidator.cs`
4. `SearchEventsQueryHandler.cs`
5. `PagedResult.cs` (if doesn't exist)

#### Step 4.2: Update Mapping Profile
```csharp
// Application/Events/MappingProfiles/EventMappingProfile.cs
CreateMap<Event, EventSearchResultDto>()
    .ForMember(dest => dest.SearchRelevance, opt => opt.Ignore());
```

**Checklist:**
- [ ] SearchEventsQuery record created
- [ ] EventSearchResultDto created
- [ ] FluentValidation rules implemented
- [ ] SearchEventsQueryHandler implemented
- [ ] PagedResult<T> class created
- [ ] AutoMapper profile updated
- [ ] Unit tests written for handler
- [ ] Unit tests written for validator

---

### Phase 5: API Layer (30 minutes)

#### Step 5.1: Add Controller Endpoint
```csharp
// API/Controllers/EventsController.cs
[HttpGet("search")]
[ProducesResponseType(typeof(PagedResult<EventSearchResultDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<PagedResult<EventSearchResultDto>>> SearchEvents(
    [FromQuery] string q,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? category = null,
    [FromQuery] bool? isFreeOnly = null,
    [FromQuery] DateTime? startDateFrom = null,
    CancellationToken cancellationToken = default)
{
    var query = new SearchEventsQuery(q, page, pageSize, category, isFreeOnly, startDateFrom);
    var result = await _mediator.Send(query, cancellationToken);

    Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
    Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());
    Response.Headers.Add("X-Current-Page", result.Page.ToString());
    Response.Headers.Add("X-Page-Size", result.PageSize.ToString());

    return Ok(result);
}
```

#### Step 5.2: Update Swagger Documentation
```csharp
// Add XML comments to controller method
// Enable XML documentation in .csproj:
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

**Checklist:**
- [ ] SearchEvents endpoint added
- [ ] XML comments added for Swagger
- [ ] Pagination headers configured
- [ ] CORS configured to expose headers
- [ ] Rate limiting configured (optional)
- [ ] API integration tests written
- [ ] Swagger UI tested manually

---

### Phase 6: Testing (2 hours)

#### Step 6.1: Unit Tests
- [ ] SearchEventsQueryHandlerTests (8+ test cases)
- [ ] SearchEventsQueryValidatorTests (10+ test cases)
- [ ] EventSearchSpecificationTests

#### Step 6.2: Integration Tests
- [ ] EventRepositoryIntegrationTests with Testcontainers
- [ ] Test search with real PostgreSQL
- [ ] Test pagination
- [ ] Test filters (category, free, date)
- [ ] Test performance (< 50ms for 1000 events)

#### Step 6.3: API Tests
- [ ] EventsControllerIntegrationTests
- [ ] Test 200 OK response
- [ ] Test 400 Bad Request for invalid input
- [ ] Test pagination headers
- [ ] Test empty results

**Checklist:**
- [ ] All unit tests passing (90%+ coverage)
- [ ] All integration tests passing
- [ ] All API tests passing
- [ ] Performance tests meeting SLA (< 100ms)
- [ ] Test data seeded correctly

---

### Phase 7: Performance Optimization (1 hour)

#### Step 7.1: Verify Index Usage
```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM events
WHERE search_vector @@ websearch_to_tsquery('english', 'cricket')
ORDER BY ts_rank(search_vector, websearch_to_tsquery('english', 'cricket')) DESC
LIMIT 20;
```

**Expected output:**
- Bitmap Index Scan on `idx_events_search_vector`
- Execution time < 50ms for 10K events

#### Step 7.2: Add Application Caching (Optional)
```csharp
// Program.cs
builder.Services.AddMemoryCache();
```

#### Step 7.3: Load Testing
```bash
ab -n 1000 -c 10 "http://localhost:5000/api/events/search?q=cricket"
```

**Target metrics:**
- Requests/sec: > 100
- Mean response time: < 100ms
- Failed requests: 0

**Checklist:**
- [ ] EXPLAIN ANALYZE shows GIN index usage
- [ ] Query times < 50ms for 10K events
- [ ] Load testing completed successfully
- [ ] Caching implemented (if needed)
- [ ] Monitoring/logging configured
- [ ] Slow query logging enabled in PostgreSQL

---

## Testing Commands

### Run All Tests
```bash
# Unit tests
dotnet test tests/Application.Tests/

# Integration tests
dotnet test tests/Infrastructure.Tests/

# API tests
dotnet test tests/API.Tests/

# All tests
dotnet test
```

### Manual Testing
```bash
# Start application
dotnet run --project src/LankaConnect.API

# Test search endpoint
curl "http://localhost:5000/api/events/search?q=cricket&page=1&pageSize=20"

# Test with filters
curl "http://localhost:5000/api/events/search?q=cricket&category=Sports&isFreeOnly=true"

# Test pagination
curl "http://localhost:5000/api/events/search?q=cricket&page=2&pageSize=5"
```

---

## Rollback Plan

If implementation fails, rollback in reverse order:

1. **Remove API endpoint** - Comment out controller method
2. **Remove application layer** - Delete query files
3. **Remove infrastructure** - Delete repository methods
4. **Revert migration**
   ```bash
   dotnet ef database update <previous-migration-name>
   dotnet ef migrations remove
   ```

---

## Go-Live Checklist

### Pre-Deployment
- [ ] All tests passing (unit, integration, API)
- [ ] Code review completed
- [ ] Performance benchmarks met
- [ ] Documentation updated
- [ ] Migration tested on staging database

### Deployment
- [ ] Database migration applied to production
- [ ] GIN index built successfully (check `pg_stat_progress_create_index`)
- [ ] Application deployed
- [ ] Smoke tests passed

### Post-Deployment
- [ ] Monitor slow query logs (first 24 hours)
- [ ] Check index usage statistics
- [ ] Monitor API response times
- [ ] Track search success rate (zero-result queries)
- [ ] Collect user feedback

### Monitoring Queries
```sql
-- Check index usage
SELECT idx_scan, idx_tup_read FROM pg_stat_user_indexes
WHERE indexname = 'idx_events_search_vector';

-- Check slow queries
SELECT query, mean_exec_time, calls
FROM pg_stat_statements
WHERE query LIKE '%search_vector%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

---

## Success Criteria

✅ **Functional Requirements**
- [ ] Users can search events by keywords
- [ ] Multi-term queries work correctly
- [ ] Results ordered by relevance
- [ ] Filters work (category, price, date)
- [ ] Pagination works correctly

✅ **Non-Functional Requirements**
- [ ] Query response time < 100ms (p95)
- [ ] 90%+ test coverage
- [ ] Zero SQL injection vulnerabilities
- [ ] Clean Architecture maintained
- [ ] Documentation complete

✅ **Operational Requirements**
- [ ] Monitoring configured
- [ ] Logging implemented
- [ ] Error handling robust
- [ ] Rollback plan tested
- [ ] Performance benchmarks met

---

## Quick Command Reference

```bash
# Create migration
dotnet ef migrations add AddFullTextSearchSupport -p src/LankaConnect.Infrastructure -s src/LankaConnect.API

# Apply migration
dotnet ef database update -p src/LankaConnect.Infrastructure -s src/LankaConnect.API

# Run tests
dotnet test

# Build project
dotnet build

# Run application
dotnet run --project src/LankaConnect.API

# Load test
ab -n 1000 -c 10 "http://localhost:5000/api/events/search?q=cricket"
```

---

## Estimated Timeline

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Database Migration | 30 min | None |
| Phase 2: Domain Layer | 15 min | Phase 1 |
| Phase 3: Infrastructure | 45 min | Phase 2 |
| Phase 4: Application Layer | 45 min | Phase 3 |
| Phase 5: API Layer | 30 min | Phase 4 |
| Phase 6: Testing | 2 hours | Phase 5 |
| Phase 7: Performance | 1 hour | Phase 6 |
| **Total** | **~6 hours** | |

---

## Support Resources

- **PostgreSQL FTS Docs:** https://www.postgresql.org/docs/16/textsearch.html
- **EF Core Raw SQL:** https://learn.microsoft.com/en-us/ef/core/querying/sql-queries
- **Testcontainers:** https://dotnet.testcontainers.org/
- **Architecture Docs:** `docs/architecture/ADR-003-PostgreSQL-FullText-Search.md`
