using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetFormResponses;

/// <summary>
/// Gets paginated responses for a form (organizer view).
/// </summary>
public record GetFormResponsesQuery(
    Guid EventId,
    Guid FormId,
    int Page = 1,
    int PageSize = 20
) : IQuery<FormResponsesPagedDto>;
