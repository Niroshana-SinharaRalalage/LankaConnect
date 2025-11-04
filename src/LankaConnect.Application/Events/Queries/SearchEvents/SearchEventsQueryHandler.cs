using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Models;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.SearchEvents;

/// <summary>
/// Handler for SearchEventsQuery
/// Executes full-text search using PostgreSQL tsvector and returns paginated results
/// </summary>
public class SearchEventsQueryHandler : IQueryHandler<SearchEventsQuery, PagedResult<EventSearchResultDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public SearchEventsQueryHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<EventSearchResultDto>>> Handle(
        SearchEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Calculate offset for pagination
        var offset = (request.Page - 1) * request.PageSize;

        // Execute search query
        var (events, totalCount) = await _eventRepository.SearchAsync(
            request.SearchTerm,
            request.PageSize,
            offset,
            request.Category,
            request.IsFreeOnly,
            request.StartDateFrom,
            cancellationToken);

        // Map to DTOs
        var eventDtos = _mapper.Map<IEnumerable<EventSearchResultDto>>(events);

        // Create paged result
        var pagedResult = totalCount == 0
            ? PagedResult<EventSearchResultDto>.Empty(request.Page, request.PageSize)
            : new PagedResult<EventSearchResultDto>(
                eventDtos.ToList(),
                totalCount,
                request.Page,
                request.PageSize);

        return Result<PagedResult<EventSearchResultDto>>.Success(pagedResult);
    }
}
