using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetEventsByOrganizer;

/// <summary>
/// Handler for GetEventsByOrganizerQuery
/// Epic 1: Returns full EventDto for each event created by the organizer
/// Phase 6A.47: Now delegates to GetEventsQuery with filters for search/filter support
/// </summary>
public class GetEventsByOrganizerQueryHandler : IQueryHandler<GetEventsByOrganizerQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<GetEventsByOrganizerQueryHandler> _logger;

    public GetEventsByOrganizerQueryHandler(
        IEventRepository eventRepository,
        IMediator mediator,
        ILogger<GetEventsByOrganizerQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(
        GetEventsByOrganizerQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventsByOrganizer"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("OrganizerId", request.OrganizerId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventsByOrganizer START: OrganizerId={OrganizerId}, HasFilters={HasFilters}",
                request.OrganizerId, HasFilters(request));

            try
            {
                // Validate request
                if (request.OrganizerId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventsByOrganizer FAILED: Invalid OrganizerId - OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.OrganizerId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Failure("Organizer ID is required");
                }

                // Phase 6A.47: If filters provided, use GetEventsQuery for search/filter support
                if (HasFilters(request))
                {
                    _logger.LogInformation(
                        "GetEventsByOrganizer: Using filtered path - SearchTerm={SearchTerm}, Category={Category}",
                        request.SearchTerm, request.Category);

                    // Get all event IDs created by this organizer
                    var organizerEvents = await _eventRepository.GetByOrganizerAsync(request.OrganizerId, cancellationToken);
                    var organizerEventIds = organizerEvents.Select(e => e.Id).Distinct().ToHashSet();

                    _logger.LogInformation(
                        "GetEventsByOrganizer: Organizer events loaded - OrganizerId={OrganizerId}, EventCount={EventCount}",
                        request.OrganizerId, organizerEventIds.Count);

                    if (organizerEventIds.Count == 0)
                    {
                        stopwatch.Stop();

                        _logger.LogInformation(
                            "GetEventsByOrganizer COMPLETE: No events found - OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                            request.OrganizerId, stopwatch.ElapsedMilliseconds);

                        return Result<IReadOnlyList<EventDto>>.Success(Array.Empty<EventDto>());
                    }

                    // Use GetEventsQuery with filters
                    var getEventsQuery = new GetEventsQuery(
                        SearchTerm: request.SearchTerm,
                        Category: request.Category,
                        StartDateFrom: request.StartDateFrom,
                        StartDateTo: request.StartDateTo,
                        State: request.State,
                        MetroAreaIds: request.MetroAreaIds
                    );

                    var eventsResult = await _mediator.Send(getEventsQuery, cancellationToken);

                    if (eventsResult.IsFailure)
                    {
                        stopwatch.Stop();

                        _logger.LogWarning(
                            "GetEventsByOrganizer FAILED: GetEventsQuery failed - OrganizerId={OrganizerId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.OrganizerId, eventsResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result<IReadOnlyList<EventDto>>.Failure(eventsResult.Error);
                    }

                    // Filter to only organizer's events
                    var filteredEvents = eventsResult.Value
                        .Where(e => organizerEventIds.Contains(e.Id))
                        .ToList();

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "GetEventsByOrganizer COMPLETE: OrganizerId={OrganizerId}, TotalResults={TotalResults}, FilteredFromTotal={FilteredFromTotal}, Duration={ElapsedMs}ms",
                        request.OrganizerId, filteredEvents.Count, eventsResult.Value.Count, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Success(filteredEvents);
                }

                // Original path: No filters, return all organizer's events
                _logger.LogInformation(
                    "GetEventsByOrganizer: Using unfiltered path - OrganizerId={OrganizerId}",
                    request.OrganizerId);

                var allEvents = await _eventRepository.GetByOrganizerAsync(request.OrganizerId, cancellationToken);

                _logger.LogInformation(
                    "GetEventsByOrganizer: All organizer events loaded - OrganizerId={OrganizerId}, EventCount={EventCount}",
                    request.OrganizerId, allEvents.Count);

                if (allEvents.Count == 0)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "GetEventsByOrganizer COMPLETE: No events found - OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.OrganizerId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Success(Array.Empty<EventDto>());
                }

                var eventIds = allEvents.Select(e => e.Id).Distinct().ToList();

                // Delegate to GetEventsQuery without filters
                var getAllQuery = new GetEventsQuery();
                var allEventsResult = await _mediator.Send(getAllQuery, cancellationToken);

                if (allEventsResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventsByOrganizer FAILED: GetEventsQuery failed - OrganizerId={OrganizerId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.OrganizerId, allEventsResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Failure(allEventsResult.Error);
                }

                // Filter to organizer's events
                var organizerEventDtos = allEventsResult.Value
                    .Where(e => eventIds.Contains(e.Id))
                    .ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventsByOrganizer COMPLETE: OrganizerId={OrganizerId}, TotalResults={TotalResults}, Duration={ElapsedMs}ms",
                    request.OrganizerId, organizerEventDtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EventDto>>.Success(organizerEventDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventsByOrganizer FAILED: Exception occurred - OrganizerId={OrganizerId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.OrganizerId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    private static bool HasFilters(GetEventsByOrganizerQuery request)
    {
        return !string.IsNullOrWhiteSpace(request.SearchTerm)
            || request.Category.HasValue
            || request.StartDateFrom.HasValue
            || request.StartDateTo.HasValue
            || !string.IsNullOrWhiteSpace(request.State)
            || (request.MetroAreaIds != null && request.MetroAreaIds.Any());
    }
}
