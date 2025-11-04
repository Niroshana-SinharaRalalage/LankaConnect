# Infrastructure Layer: PostgreSQL Full-Text Search Implementation

## Database Schema Approach

**Decision: Use Computed Column (GENERATED ALWAYS AS STORED)**

### Rationale
- **Pros:**
  - Always in sync with source data (automatic updates)
  - Simpler than triggers (no procedural code)
  - EF Core Code-First compatible
  - Less maintenance overhead
  - No risk of stale data

- **Cons:**
  - Slightly more storage (but negligible)
  - Cannot customize weighting per-event (acceptable for this use case)

### Migration Script

```csharp
// Infrastructure/Migrations/20251104_AddFullTextSearchSupport.cs
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddFullTextSearchSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add tsvector computed column with weighted text
        migrationBuilder.Sql(@"
            ALTER TABLE events
            ADD COLUMN search_vector tsvector
            GENERATED ALWAYS AS (
                setweight(to_tsvector('english', COALESCE(title, '')), 'A') ||
                setweight(to_tsvector('english', COALESCE(description, '')), 'B')
            ) STORED;
        ");

        // Create GIN index for fast full-text search
        migrationBuilder.Sql(@"
            CREATE INDEX idx_events_search_vector
            ON events
            USING GIN(search_vector);
        ");

        // Add indexes for filter columns to improve combined queries
        migrationBuilder.Sql(@"
            CREATE INDEX idx_events_category
            ON events(category)
            WHERE category IS NOT NULL;
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX idx_events_start_date
            ON events(start_date);
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX idx_events_ticket_price
            ON events(ticket_price)
            WHERE ticket_price IS NOT NULL;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS idx_events_ticket_price;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS idx_events_start_date;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS idx_events_category;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS idx_events_search_vector;");
        migrationBuilder.Sql("ALTER TABLE events DROP COLUMN IF EXISTS search_vector;");
    }
}
```

## EF Core Configuration

```csharp
// Infrastructure/Data/Configuration/EventConfiguration.cs
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // Existing configuration...

        // Configure computed column (EF Core aware but doesn't generate)
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("search_vector")
            .HasComputedColumnSql(
                "setweight(to_tsvector('english', COALESCE(title, '')), 'A') || " +
                "setweight(to_tsvector('english', COALESCE(description, '')), 'B')",
                stored: true)
            .IsRequired(false); // Computed columns are managed by database

        // Ignore in EF Core change tracking (database-managed)
        builder.Ignore("SearchVector");
    }
}
```

## Repository Interface Extension

```csharp
// Domain/Repositories/IEventRepository.cs (add methods)
public interface IEventRepository
{
    // Existing methods...

    /// <summary>
    /// Searches events using full-text search with pagination
    /// </summary>
    Task<IReadOnlyList<Event>> SearchAsync(
        EventSearchSpecification specification,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts total events matching search specification
    /// </summary>
    Task<int> CountSearchAsync(
        EventSearchSpecification specification,
        CancellationToken cancellationToken = default);
}
```

## Repository Implementation

```csharp
// Infrastructure/Data/Repositories/EventRepository.cs
using Npgsql;
using Microsoft.EntityFrameworkCore;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventRepository> _logger;

    public EventRepository(
        ApplicationDbContext context,
        ILogger<EventRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Event>> SearchAsync(
        EventSearchSpecification specification,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var offset = (page - 1) * pageSize;

        // Build parameterized SQL query
        var sql = @"
            SELECT
                e.*,
                ts_rank(e.search_vector, websearch_to_tsquery('english', @searchTerm)) AS rank
            FROM events e
            WHERE
                e.search_vector @@ websearch_to_tsquery('english', @searchTerm)
                AND (@category IS NULL OR e.category = @category)
                AND (@isFreeOnly IS NULL OR
                     (@isFreeOnly = true AND (e.ticket_price IS NULL OR e.ticket_price = 0)) OR
                     (@isFreeOnly = false))
                AND (@startDateFrom IS NULL OR e.start_date >= @startDateFrom)
            ORDER BY
                rank DESC,           -- Relevance first
                e.start_date ASC     -- Then by date
            LIMIT @pageSize
            OFFSET @offset;
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

        var events = await _context.Events
            .FromSqlRaw(sql, parameters)
            .AsNoTracking() // Read-only query
            .ToListAsync(cancellationToken);

        _logger.LogDebug(
            "Full-text search returned {Count} events for term '{SearchTerm}'",
            events.Count,
            specification.SearchTerm);

        return events;
    }

    public async Task<int> CountSearchAsync(
        EventSearchSpecification specification,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM events e
            WHERE
                e.search_vector @@ websearch_to_tsquery('english', @searchTerm)
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

        var count = await _context.Database
            .SqlQueryRaw<int>($"SELECT COUNT(*)::integer {sql.Substring(sql.IndexOf("FROM"))}", parameters)
            .FirstOrDefaultAsync(cancellationToken);

        return count;
    }

    // Alternative: LINQ-based approach (if you want to avoid raw SQL)
    // Requires: NpgsqlEntityFrameworkCore.PostgreSQL.FreeText extension
    /*
    public async Task<IReadOnlyList<Event>> SearchAsync_LinqApproach(
        EventSearchSpecification specification,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .Where(e => EF.Functions.ToTsVector("english", e.Title + " " + e.Description)
                .Matches(EF.Functions.WebSearchToTsQuery("english", specification.SearchTerm)));

        if (!string.IsNullOrEmpty(specification.Category))
            query = query.Where(e => e.Category == specification.Category);

        if (specification.IsFreeOnly.HasValue && specification.IsFreeOnly.Value)
            query = query.Where(e => e.TicketPrice == null || e.TicketPrice == 0);

        if (specification.StartDateFrom.HasValue)
            query = query.Where(e => e.StartDate >= specification.StartDateFrom.Value);

        var results = await query
            .OrderByDescending(e => EF.Functions.ToTsVector("english", e.Title + " " + e.Description)
                .Rank(EF.Functions.WebSearchToTsQuery("english", specification.SearchTerm)))
            .ThenBy(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return results;
    }
    */
}
```

## DbContext Configuration

```csharp
// Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Register PostgreSQL full-text search functions (optional, for LINQ approach)
        // modelBuilder.HasDbFunction(() => NpgsqlFullTextSearchDbFunctionsExtensions.ToTsVector(...));
    }
}
```

## Key Decisions

1. **Raw SQL Approach**: Chosen for explicit control and performance transparency
2. **Parameterized Queries**: Prevents SQL injection attacks
3. **websearch_to_tsquery**: Supports multi-term queries with AND/OR operators
4. **Computed Column**: Automatic synchronization, simpler than triggers
5. **GIN Index**: Optimal for full-text search (faster than GiST)
6. **Additional Indexes**: Improve performance when filters are combined with search
7. **AsNoTracking()**: Read-only queries don't need change tracking overhead

## Performance Expectations

- **GIN Index Build Time**: ~50-100ms per 10,000 events
- **Search Query Time**:
  - < 50ms for simple searches (< 100k events)
  - < 100ms for complex filters (< 1M events)
- **Index Size**: ~30-40% of table size
- **Update Overhead**: Negligible (computed column auto-updated)
