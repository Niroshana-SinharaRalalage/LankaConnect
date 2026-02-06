using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;

namespace LankaConnect.Application.Communications.Queries.GetPublishedNewsletters;

/// <summary>
/// Query to get published newsletters with optional filtering and location-based sorting
/// Phase 6A.74 Parts 10 & 11: Public newsletter list page with filtering
///
/// Location-based sorting (when UserId or Lat/Lng provided):
/// - For authenticated users with newsletter subscription location: Sort by subscribed locations
/// - For authenticated users without preferences: Sort by user's preferred metro areas
/// - For anonymous users: Sort by provided coordinates
///
/// Filters:
/// - PublishedFrom/PublishedTo: Date range for newsletter publication
/// - MetroAreaIds: List of metro area IDs (location targeting)
/// - State: Filter by state
/// - SearchTerm: Full-text search in title and description
/// </summary>
public record GetPublishedNewslettersQuery(
    DateTime? PublishedFrom = null,
    DateTime? PublishedTo = null,
    string? State = null,
    Guid? UserId = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    List<Guid>? MetroAreaIds = null,
    string? SearchTerm = null
) : IQuery<IReadOnlyList<NewsletterDto>>;
