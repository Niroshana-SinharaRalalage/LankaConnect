using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Models;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.SearchEvents;

/// <summary>
/// Handler for SearchEventsQuery
/// Executes full-text search using PostgreSQL tsvector and returns paginated results
/// </summary>
public class SearchEventsQueryHandler : IQueryHandler<SearchEventsQuery, PagedResult<EventSearchResultDto>>
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

    public async Task<Result<PagedResult<EventSearchResultDto>>> Handle(
        SearchEventsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SearchEvents"))
        using (LogContext.PushProperty("EntityType", "Event"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SearchEvents START: SearchTerm={SearchTerm}, Page={Page}, PageSize={PageSize}, Category={Category}, IsFreeOnly={IsFreeOnly}, StartDateFrom={StartDateFrom}",
                request.SearchTerm, request.Page, request.PageSize, request.Category, request.IsFreeOnly, request.StartDateFrom);

            try
            {
                // Validate request
                if (request.Page <= 0)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SearchEvents FAILED: Invalid Page - Page={Page}, Duration={ElapsedMs}ms",
                        request.Page, stopwatch.ElapsedMilliseconds);

                    return Result<PagedResult<EventSearchResultDto>>.Failure("Page must be greater than 0");
                }

                if (request.PageSize <= 0 || request.PageSize > 100)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "SearchEvents FAILED: Invalid PageSize - PageSize={PageSize}, Duration={ElapsedMs}ms",
                        request.PageSize, stopwatch.ElapsedMilliseconds);

                    return Result<PagedResult<EventSearchResultDto>>.Failure("Page size must be between 1 and 100");
                }

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

                _logger.LogInformation(
                    "SearchEvents: Search executed - SearchTerm={SearchTerm}, TotalCount={TotalCount}, ReturnedCount={ReturnedCount}",
                    request.SearchTerm, totalCount, events.Count());

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

                stopwatch.Stop();

                _logger.LogInformation(
                    "SearchEvents COMPLETE: SearchTerm={SearchTerm}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}, TotalPages={TotalPages}, Duration={ElapsedMs}ms",
                    request.SearchTerm, request.Page, request.PageSize, pagedResult.TotalCount, pagedResult.TotalPages, stopwatch.ElapsedMilliseconds);

                return Result<PagedResult<EventSearchResultDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "SearchEvents FAILED: Exception occurred - SearchTerm={SearchTerm}, Page={Page}, PageSize={PageSize}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.SearchTerm, request.Page, request.PageSize, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
