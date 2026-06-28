using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Models;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Application.Queries.SearchEvents;

/// <summary>
/// Query to search events using PostgreSQL full-text search
/// Searches across event titles and descriptions with ranking
/// Phase 6A.X Issue #36: Added ExcludeCancelled parameter to filter out cancelled events
/// </summary>
public record SearchEventsQuery(
    string SearchTerm,
    int Page = 1,
    int PageSize = 20,
    EventCategory? Category = null,
    bool? IsFreeOnly = null,
    DateTime? StartDateFrom = null,
    bool ExcludeCancelled = false
) : IQuery<PagedResult<EventSearchResultDto>>;
