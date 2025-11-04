# Performance Optimization Guide: PostgreSQL Full-Text Search

## Index Performance Characteristics

### GIN Index vs GiST Index

| Feature | GIN (Generalized Inverted Index) | GiST (Generalized Search Tree) |
|---------|----------------------------------|--------------------------------|
| Build Time | Slower (3x) | Faster |
| Index Size | Larger (40% of table) | Smaller (20% of table) |
| Query Speed | **Faster** (3-5x) | Slower |
| Update Speed | Slower | Faster |
| Best For | Read-heavy workloads | Write-heavy workloads |

**Decision: GIN Index (Recommended for this use case)**
- Events are read more than written
- Search speed is critical for user experience
- Index size is acceptable trade-off

### GIN Index Fastupdate Setting

```sql
-- Default: fastupdate = ON (batches updates, slower searches)
-- Recommended: fastupdate = OFF (immediate updates, faster searches)

CREATE INDEX idx_events_search_vector
ON events
USING GIN(search_vector)
WITH (fastupdate = off);
```

## Query Performance Benchmarks

### Expected Performance (with GIN index)

| Dataset Size | Simple Search | Multi-term Search | With Filters | With Pagination |
|--------------|---------------|-------------------|--------------|-----------------|
| 1K events    | < 5ms         | < 10ms            | < 15ms       | < 20ms          |
| 10K events   | < 10ms        | < 20ms            | < 30ms       | < 40ms          |
| 100K events  | < 30ms        | < 50ms            | < 70ms       | < 90ms          |
| 1M events    | < 80ms        | < 120ms           | < 150ms      | < 200ms         |

### Without Index (for comparison)

| Dataset Size | Simple Search | Multi-term Search |
|--------------|---------------|-------------------|
| 1K events    | 50-100ms      | 100-150ms         |
| 10K events   | 500-800ms     | 800-1200ms        |
| 100K events  | 5-8s          | 8-12s             |

**Impact: 10-100x improvement with GIN index**

## Index Monitoring

### Check Index Usage

```sql
-- Check if index is being used
EXPLAIN (ANALYZE, BUFFERS)
SELECT *
FROM events
WHERE search_vector @@ websearch_to_tsquery('english', 'cricket tournament')
ORDER BY ts_rank(search_vector, websearch_to_tsquery('english', 'cricket tournament')) DESC
LIMIT 20;

-- Expected output should show:
-- Bitmap Heap Scan on events
-- -> Bitmap Index Scan on idx_events_search_vector
```

### Index Statistics

```sql
-- Check index size
SELECT
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size,
    idx_scan AS index_scans,
    idx_tup_read AS tuples_read,
    idx_tup_fetch AS tuples_fetched
FROM pg_stat_user_indexes
WHERE indexname = 'idx_events_search_vector';

-- Check index bloat
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS table_size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS indexes_size
FROM pg_tables
WHERE tablename = 'events';
```

## Additional Performance Indexes

### Composite Indexes for Common Filter Combinations

```sql
-- Index for category + date filtering (common combination)
CREATE INDEX idx_events_category_start_date
ON events(category, start_date)
WHERE category IS NOT NULL;

-- Index for free events (common filter)
CREATE INDEX idx_events_free_events
ON events(start_date)
WHERE ticket_price IS NULL OR ticket_price = 0;

-- Partial index for upcoming events (most searched)
CREATE INDEX idx_events_upcoming
ON events(start_date)
WHERE start_date >= CURRENT_DATE;
```

### Index Selection Strategy

PostgreSQL will automatically choose the best index based on query patterns:
1. For search-only: `idx_events_search_vector` (GIN)
2. For search + category: `idx_events_search_vector` + `idx_events_category`
3. For search + date: `idx_events_search_vector` + `idx_events_start_date`
4. For search + free: `idx_events_search_vector` + `idx_events_free_events`

## Query Optimization Techniques

### 1. Covering Indexes (Not applicable to GIN)

GIN indexes cannot be covering indexes, so we rely on efficient heap access.

### 2. Materialized Views (for complex aggregations)

```sql
-- If you need to show popular search terms or category statistics
CREATE MATERIALIZED VIEW event_search_stats AS
SELECT
    category,
    COUNT(*) AS event_count,
    AVG(ticket_price) AS avg_price,
    COUNT(*) FILTER (WHERE ticket_price IS NULL OR ticket_price = 0) AS free_count
FROM events
GROUP BY category;

-- Refresh periodically (e.g., daily via background job)
REFRESH MATERIALIZED VIEW event_search_stats;
```

### 3. Query Result Caching (Application Layer)

```csharp
// Infrastructure/Caching/SearchCacheService.cs
public class SearchCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IEventRepository _repository;

    public SearchCacheService(IMemoryCache cache, IEventRepository repository)
    {
        _cache = cache;
        _repository = repository;
    }

    public async Task<IReadOnlyList<Event>> GetCachedSearchResults(
        EventSearchSpecification specification,
        int page,
        int pageSize)
    {
        var cacheKey = $"search:{specification.SearchTerm}:{page}:{pageSize}:{specification.Category}:{specification.IsFreeOnly}:{specification.StartDateFrom}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);

            return await _repository.SearchAsync(specification, page, pageSize);
        });
    }
}
```

**Cache Strategy:**
- Cache duration: 5 minutes absolute, 2 minutes sliding
- Cache popular searches (top 100 terms)
- Invalidate on event updates (optional)

### 4. Database Connection Pooling

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=lankaconnect;Username=app;Password=***;Pooling=true;MinPoolSize=5;MaxPoolSize=50;ConnectionIdleLifetime=300;ConnectionPruningInterval=10"
  }
}
```

**Pool Configuration:**
- Min Pool Size: 5 (keep warm connections)
- Max Pool Size: 50 (handle burst traffic)
- Connection Idle Lifetime: 5 minutes
- Connection Pruning Interval: 10 seconds

## Edge Case Optimizations

### 1. Stop Words Handling

PostgreSQL automatically removes stop words ("the", "and", "or", etc.) from search terms. No additional handling needed.

```sql
-- Test stop words behavior
SELECT to_tsvector('english', 'the cricket tournament');
-- Result: 'cricket':2 'tournament':3
-- "the" is automatically removed
```

### 2. Very Long Search Terms

Already handled by validator (max 500 chars). PostgreSQL efficiently handles terms up to several KB.

### 3. Special Characters in Search

`websearch_to_tsquery` handles special characters gracefully:

```sql
-- Handles quotes, operators, hyphens automatically
SELECT websearch_to_tsquery('english', 'cricket "world cup" -football');
-- Result: 'cricket' & 'world' <-> 'cup' & !'football'
```

### 4. Empty Search Results

Optimized by COUNT query running first - avoids fetching data if no results.

## Monitoring & Alerting

### Application Performance Monitoring (APM)

```csharp
// Middleware/SearchPerformanceMonitor.cs
public class SearchPerformanceMonitor
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SearchPerformanceMonitor> _logger;
    private readonly IMeterFactory _meterFactory;

    public SearchPerformanceMonitor(
        RequestDelegate next,
        ILogger<SearchPerformanceMonitor> logger,
        IMeterFactory meterFactory)
    {
        _next = next;
        _logger = logger;
        _meterFactory = meterFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/events/search"))
        {
            await _next(context);
            return;
        }

        var meter = _meterFactory.Create("LankaConnect.Search");
        var searchDuration = meter.CreateHistogram<double>("search_duration_ms");
        var searchRequests = meter.CreateCounter<long>("search_requests_total");

        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        var duration = stopwatch.Elapsed.TotalMilliseconds;

        searchDuration.Record(duration, new TagList
        {
            { "status_code", context.Response.StatusCode },
            { "search_term", context.Request.Query["q"].ToString() }
        });

        searchRequests.Add(1, new TagList
        {
            { "status_code", context.Response.StatusCode }
        });

        if (duration > 100) // Alert if over 100ms
        {
            _logger.LogWarning(
                "Slow search query: {SearchTerm} took {Duration}ms",
                context.Request.Query["q"],
                duration);
        }
    }
}
```

### Database Performance Alerts

```sql
-- Slow query logging (postgresql.conf)
log_min_duration_statement = 100  -- Log queries > 100ms
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '
log_statement = 'none'
log_duration = off

-- Check for missing index usage
SELECT
    schemaname,
    tablename,
    seq_scan,
    seq_tup_read,
    idx_scan,
    seq_tup_read / NULLIF(seq_scan, 0) AS avg_seq_tup_read
FROM pg_stat_user_tables
WHERE schemaname = 'public'
  AND tablename = 'events'
  AND seq_scan > 0
ORDER BY seq_tup_read DESC;
```

## Performance Testing Checklist

- [ ] Verify GIN index is used in EXPLAIN ANALYZE
- [ ] Test search with 1K, 10K, 100K events
- [ ] Measure query duration (should be < 100ms for 100K events)
- [ ] Test with multiple concurrent users (load testing)
- [ ] Monitor index size growth
- [ ] Check query plan for bitmap index scans
- [ ] Verify filter indexes are used when applicable
- [ ] Test cache hit rates (should be > 60% for popular searches)
- [ ] Monitor connection pool utilization (should be < 80%)
- [ ] Test database failover and recovery

## Load Testing Script

```bash
# Using Apache Bench (ab)
ab -n 1000 -c 10 "http://localhost:5000/api/events/search?q=cricket&page=1&pageSize=20"

# Expected results:
# Requests per second: > 100 req/sec
# Time per request: < 100ms (mean)
# Failed requests: 0
```

## Optimization Recommendations by Scale

### Small Scale (< 10K events)
- Basic GIN index sufficient
- No caching needed
- Simple connection pooling

### Medium Scale (10K - 100K events)
- GIN index with fastupdate=off
- Application-level caching (5 min TTL)
- Optimized connection pooling
- Partial indexes for common filters

### Large Scale (> 100K events)
- GIN index with fastupdate=off
- Distributed caching (Redis)
- Read replicas for search queries
- Materialized views for aggregations
- CDN caching for popular searches

## Key Performance Decisions

1. **GIN Index**: Fastest search, acceptable build time
2. **Fastupdate OFF**: Prioritize query speed over write speed
3. **Computed Column**: Automatic updates, no maintenance overhead
4. **Application Caching**: 5-minute TTL reduces database load
5. **Connection Pooling**: Handles burst traffic efficiently
6. **Partial Indexes**: Optimize common filter combinations
7. **Monitoring**: Track slow queries > 100ms
8. **Load Testing**: Verify performance under realistic load
