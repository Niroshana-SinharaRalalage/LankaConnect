using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEvents;

/// <summary>
/// Query to get events with optional filtering and location-based sorting
/// Location-based sorting (when UserId or Lat/Lng provided):
/// - For authenticated users with preferred metro areas: Sort by those metros
/// - For authenticated users without preferences: Sort by user's location
/// - For anonymous users: Sort by provided coordinates
/// Phase 6A.47: Added SearchTerm for full-text search integration
/// Phase 6A.88: Added IncludeAllStatuses to control Draft/UnderReview visibility
/// Issue #36: Added StatusFilter for user-friendly status group filtering
/// </summary>
public record GetEventsQuery(
    EventStatus? Status = null,
    /// <summary>
    /// Issue #36: User-friendly status filter that maps to multiple EventStatus values.
    /// When provided, takes precedence over the Status parameter.
    /// See EventStatusFilter enum for mappings.
    /// </summary>
    EventStatusFilter? StatusFilter = null,
    EventCategory? Category = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    bool? IsFreeOnly = null,
    string? City = null,
    string? State = null,
    Guid? UserId = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    List<Guid>? MetroAreaIds = null,
    string? SearchTerm = null,
    /// <summary>
    /// Phase 6A.88: When true, includes Draft and UnderReview events.
    /// Default is false (public listings exclude Draft/UnderReview).
    /// Set to true for organizer's Event Management view.
    /// </summary>
    bool IncludeAllStatuses = false
) : IQuery<IReadOnlyList<EventDto>>;
