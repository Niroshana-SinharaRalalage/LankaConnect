using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

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

    public GetEventsByOrganizerQueryHandler(
        IEventRepository eventRepository,
        IMediator mediator)
    {
        _eventRepository = eventRepository;
        _mediator = mediator;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(
        GetEventsByOrganizerQuery request,
        CancellationToken cancellationToken)
    {
        // Phase 6A.47: If filters provided, use GetEventsQuery for search/filter support
        if (HasFilters(request))
        {
            // Get all event IDs created by this organizer
            var organizerEvents = await _eventRepository.GetByOrganizerAsync(request.OrganizerId, cancellationToken);
            var organizerEventIds = organizerEvents.Select(e => e.Id).Distinct().ToHashSet();

            if (organizerEventIds.Count == 0)
            {
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
                return Result<IReadOnlyList<EventDto>>.Failure(eventsResult.Error);
            }

            // Filter to only organizer's events
            var filteredEvents = eventsResult.Value
                .Where(e => organizerEventIds.Contains(e.Id))
                .ToList();

            return Result<IReadOnlyList<EventDto>>.Success(filteredEvents);
        }

        // Original path: No filters, return all organizer's events
        var allEvents = await _eventRepository.GetByOrganizerAsync(request.OrganizerId, cancellationToken);

        if (allEvents.Count == 0)
        {
            return Result<IReadOnlyList<EventDto>>.Success(Array.Empty<EventDto>());
        }

        var eventIds = allEvents.Select(e => e.Id).Distinct().ToList();

        // Delegate to GetEventsQuery without filters
        var getAllQuery = new GetEventsQuery();
        var allEventsResult = await _mediator.Send(getAllQuery, cancellationToken);

        if (allEventsResult.IsFailure)
        {
            return Result<IReadOnlyList<EventDto>>.Failure(allEventsResult.Error);
        }

        // Filter to organizer's events
        var organizerEventDtos = allEventsResult.Value
            .Where(e => eventIds.Contains(e.Id))
            .ToList();

        return Result<IReadOnlyList<EventDto>>.Success(organizerEventDtos);
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
