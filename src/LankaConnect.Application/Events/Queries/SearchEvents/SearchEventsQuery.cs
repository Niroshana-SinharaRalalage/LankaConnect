using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Models;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.SearchEvents;

/// <summary>
/// Query to search events using PostgreSQL full-text search
/// Searches across event titles and descriptions with ranking
/// </summary>
public record SearchEventsQuery(
    string SearchTerm,
    int Page = 1,
    int PageSize = 20,
    EventCategory? Category = null,
    bool? IsFreeOnly = null,
    DateTime? StartDateFrom = null
) : IQuery<PagedResult<EventSearchResultDto>>;
