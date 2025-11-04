# Application Layer: Full-Text Search Implementation

## Query Definition

```csharp
// Application/Events/Queries/SearchEvents/SearchEventsQuery.cs
public record SearchEventsQuery(
    string SearchTerm,
    int Page = 1,
    int PageSize = 20,
    string? Category = null,
    bool? IsFreeOnly = null,
    DateTime? StartDateFrom = null
) : IRequest<PagedResult<EventSearchResultDto>>;
```

## Validation

```csharp
// Application/Events/Queries/SearchEvents/SearchEventsQueryValidator.cs
public class SearchEventsQueryValidator : AbstractValidator<SearchEventsQuery>
{
    public SearchEventsQueryValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty()
            .WithMessage("Search term is required")
            .MaximumLength(500)
            .WithMessage("Search term must not exceed 500 characters")
            .Must(BeValidSearchTerm)
            .WithMessage("Search term contains invalid characters");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.Category)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.StartDateFrom)
            .LessThanOrEqualTo(DateTime.UtcNow.AddYears(5))
            .When(x => x.StartDateFrom.HasValue)
            .WithMessage("Start date cannot be more than 5 years in the future");
    }

    private bool BeValidSearchTerm(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return false;

        // Allow alphanumeric, spaces, hyphens, apostrophes
        // Block SQL injection attempts and excessive special characters
        var invalidPattern = @"[;<>{}[\]\\|`]";
        return !Regex.IsMatch(searchTerm, invalidPattern);
    }
}
```

## Query Handler

```csharp
// Application/Events/Queries/SearchEvents/SearchEventsQueryHandler.cs
public class SearchEventsQueryHandler
    : IRequestHandler<SearchEventsQuery, PagedResult<EventSearchResultDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchEventsQueryHandler> _logger;

    public SearchEventsQueryHandler(
        IEventRepository eventRepository,
        IMapper mapper,
        ILogger<SearchEventsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<EventSearchResultDto>> Handle(
        SearchEventsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Searching events with term: {SearchTerm}, Page: {Page}, PageSize: {PageSize}",
            request.SearchTerm, request.Page, request.PageSize);

        // Sanitize search term (remove excessive whitespace)
        var sanitizedTerm = SanitizeSearchTerm(request.SearchTerm);

        // Create specification
        var specification = new EventSearchSpecification(
            sanitizedTerm,
            request.Category,
            request.IsFreeOnly,
            request.StartDateFrom
        );

        // Get total count
        var totalCount = await _eventRepository.CountSearchAsync(
            specification,
            cancellationToken);

        if (totalCount == 0)
        {
            _logger.LogInformation("No events found for search term: {SearchTerm}", sanitizedTerm);
            return PagedResult<EventSearchResultDto>.Empty(request.Page, request.PageSize);
        }

        // Get paged results
        var events = await _eventRepository.SearchAsync(
            specification,
            request.Page,
            request.PageSize,
            cancellationToken
        );

        var dtos = _mapper.Map<IReadOnlyList<EventSearchResultDto>>(events);

        return new PagedResult<EventSearchResultDto>(
            dtos,
            totalCount,
            request.Page,
            request.PageSize
        );
    }

    private static string SanitizeSearchTerm(string searchTerm)
    {
        // Remove excessive whitespace
        return Regex.Replace(searchTerm.Trim(), @"\s+", " ");
    }
}
```

## DTOs

```csharp
// Application/Events/Queries/SearchEvents/EventSearchResultDto.cs
public class EventSearchResultDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    public decimal? TicketPrice { get; set; }
    public bool IsFree => !TicketPrice.HasValue || TicketPrice.Value == 0;
    public int AvailableSeats { get; set; }
    public string? ThumbnailImageUrl { get; set; }

    // Search-specific properties
    public double SearchRelevance { get; set; } // Ranking score

    // Computed properties
    public string FormattedPrice => IsFree ? "Free" : $"LKR {TicketPrice:N2}";
    public string FormattedDate => StartDate.ToString("MMM dd, yyyy");
}

// Application/Common/Models/PagedResult.cs
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new(Array.Empty<T>(), 0, page, pageSize);
}
```

## Mapping Profile

```csharp
// Application/Events/MappingProfiles/EventMappingProfile.cs
public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        // Existing mappings...

        CreateMap<Event, EventSearchResultDto>()
            .ForMember(dest => dest.SearchRelevance, opt => opt.Ignore()); // Set by repository
    }
}
```

## Key Decisions

1. **Validation in FluentValidation**: Centralizes validation logic, reusable, testable
2. **Empty Search Terms**: Rejected at validation layer (400 Bad Request)
3. **Search Term Sanitization**: Remove excessive whitespace but preserve user intent
4. **Special Characters**: Block SQL injection patterns, allow normal punctuation
5. **Separate DTO**: EventSearchResultDto includes SearchRelevance for ranking display
