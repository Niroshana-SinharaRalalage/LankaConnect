# API Layer: Full-Text Search Endpoint

## Controller Implementation

```csharp
// API/Controllers/EventsController.cs
using Microsoft.AspNetCore.Mvc;
using MediatR;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IMediator mediator, ILogger<EventsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Search events using full-text search
    /// </summary>
    /// <param name="q">Search term (required)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="category">Filter by event category (optional)</param>
    /// <param name="isFreeOnly">Show only free events (optional)</param>
    /// <param name="startDateFrom">Show events starting from this date (optional)</param>
    /// <returns>Paged list of events matching search criteria</returns>
    /// <response code="200">Returns the list of matching events</response>
    /// <response code="400">Invalid search parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<EventSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<EventSearchResultDto>>> SearchEvents(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null,
        [FromQuery] bool? isFreeOnly = null,
        [FromQuery] DateTime? startDateFrom = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Search request: q={Query}, page={Page}, pageSize={PageSize}",
            q, page, pageSize);

        var query = new SearchEventsQuery(
            SearchTerm: q,
            Page: page,
            PageSize: pageSize,
            Category: category,
            IsFreeOnly: isFreeOnly,
            StartDateFrom: startDateFrom
        );

        var result = await _mediator.Send(query, cancellationToken);

        // Add pagination metadata to response headers
        Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());
        Response.Headers.Add("X-Current-Page", result.Page.ToString());
        Response.Headers.Add("X-Page-Size", result.PageSize.ToString());

        return Ok(result);
    }

    /// <summary>
    /// Get search suggestions based on partial input (future enhancement)
    /// </summary>
    [HttpGet("search/suggestions")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetSearchSuggestions(
        [FromQuery] string q,
        [FromQuery] int limit = 5)
    {
        // TODO: Implement autocomplete suggestions
        // Could use PostgreSQL's ts_headline or a separate suggestions table
        return Ok(Array.Empty<string>());
    }
}
```

## API Response Examples

### Successful Search

```json
// GET /api/events/search?q=cricket%20tournament&page=1&pageSize=20&isFreeOnly=false

{
  "items": [
    {
      "id": 101,
      "title": "Sri Lanka Cricket Tournament 2025",
      "description": "Annual cricket championship featuring top teams from across the island...",
      "category": "Sports",
      "startDate": "2025-12-15T09:00:00Z",
      "endDate": "2025-12-15T18:00:00Z",
      "venue": "R. Premadasa Stadium, Colombo",
      "ticketPrice": 500.00,
      "isFree": false,
      "availableSeats": 1500,
      "thumbnailImageUrl": "https://cdn.lankaconnect.com/events/cricket-tournament.jpg",
      "searchRelevance": 0.8734,
      "formattedPrice": "LKR 500.00",
      "formattedDate": "Dec 15, 2025"
    },
    {
      "id": 205,
      "title": "Youth Cricket Skills Tournament",
      "description": "Exciting tournament for young cricket enthusiasts to showcase their skills...",
      "category": "Sports",
      "startDate": "2025-11-20T10:00:00Z",
      "endDate": "2025-11-20T17:00:00Z",
      "venue": "Thurstan College Grounds, Colombo",
      "ticketPrice": null,
      "isFree": true,
      "availableSeats": 300,
      "thumbnailImageUrl": "https://cdn.lankaconnect.com/events/youth-cricket.jpg",
      "searchRelevance": 0.7421,
      "formattedPrice": "Free",
      "formattedDate": "Nov 20, 2025"
    }
  ],
  "totalCount": 15,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

### Validation Error

```json
// GET /api/events/search?q=&page=1

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "SearchTerm": [
      "Search term is required"
    ]
  },
  "traceId": "00-abc123..."
}
```

### Empty Results

```json
// GET /api/events/search?q=nonexistent

{
  "items": [],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20,
  "totalPages": 0,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Swagger Configuration

```csharp
// API/Program.cs or Startup.cs
services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LankaConnect API",
        Version = "v1",
        Description = "API for event management and discovery"
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add search examples
    options.MapType<SearchEventsQuery>(() => new OpenApiSchema
    {
        Example = new OpenApiObject
        {
            ["q"] = new OpenApiString("cricket tournament"),
            ["page"] = new OpenApiInteger(1),
            ["pageSize"] = new OpenApiInteger(20),
            ["category"] = new OpenApiString("Sports"),
            ["isFreeOnly"] = new OpenApiBoolean(false),
            ["startDateFrom"] = new OpenApiString("2025-11-01T00:00:00Z")
        }
    });
});
```

## Rate Limiting (Optional but Recommended)

```csharp
// API/Program.cs
using AspNetCoreRateLimit;

// Add rate limiting to prevent abuse of search endpoint
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "GET:/api/events/search",
            Period = "1m",
            Limit = 30 // 30 requests per minute per IP
        }
    };
});

services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
services.AddInMemoryRateLimiting();

app.UseIpRateLimiting();
```

## CORS Configuration

```csharp
// API/Program.cs
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("https://lankaconnect.com", "https://www.lankaconnect.com")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("X-Total-Count", "X-Total-Pages", "X-Current-Page", "X-Page-Size");
    });
});

app.UseCors("AllowFrontend");
```

## Logging and Monitoring

```csharp
// API/Middleware/SearchLoggingMiddleware.cs
public class SearchLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SearchLoggingMiddleware> _logger;

    public SearchLoggingMiddleware(RequestDelegate next, ILogger<SearchLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/events/search"))
        {
            var searchTerm = context.Request.Query["q"].ToString();
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation(
                "Search completed: Term='{SearchTerm}', StatusCode={StatusCode}, Duration={Duration}ms",
                searchTerm,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        else
        {
            await _next(context);
        }
    }
}

// Register in Program.cs
app.UseMiddleware<SearchLoggingMiddleware>();
```

## Key Decisions

1. **Query Parameter 'q'**: Common convention for search endpoints
2. **Pagination Headers**: Expose metadata in response headers for client convenience
3. **Rate Limiting**: Protect against abuse (30 req/min per IP)
4. **CORS Headers**: Expose pagination headers to frontend
5. **Swagger Documentation**: Comprehensive API documentation with examples
6. **Logging Middleware**: Track search performance and popular terms
7. **Validation Errors**: Standard RFC 7807 Problem Details format
