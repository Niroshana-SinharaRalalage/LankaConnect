namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Generic paged result container for paginated queries
/// Provides pagination metadata along with the data
///
/// NOTE: This class has mutable properties to support Swagger/OpenAPI schema generation.
/// Always use the parameterized constructor to create instances. Do not use the parameterless constructor directly.
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// The items for the current page
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Parameterless constructor for serialization and Swagger schema generation
    /// Do not use this constructor directly in application code - use the parameterized constructor instead
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Creates a paged result with the specified items and pagination metadata
    /// </summary>
    /// <param name="items">The items for the current page</param>
    /// <param name="totalCount">Total number of items across all pages</param>
    /// <param name="page">Current page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
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

    /// <summary>
    /// Creates an empty paged result
    /// </summary>
    public static PagedResult<T> Empty(int page, int pageSize) =>
        new(Array.Empty<T>(), 0, page, pageSize);
}
