using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Queries.GetMyRegisteredEvents;

/// <summary>
/// Handler for GetMyRegisteredEventsQuery
/// Epic 1: Returns full EventDto for each registered event
/// Phase 6A.47: Now delegates to GetEventsQuery with filters for search/filter support
/// </summary>
public class GetMyRegisteredEventsQueryHandler : IQueryHandler<GetMyRegisteredEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IMediator _mediator;

    public GetMyRegisteredEventsQueryHandler(
        IRegistrationRepository registrationRepository,
        IMediator mediator)
    {
        _registrationRepository = registrationRepository;
        _mediator = mediator;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(
        GetMyRegisteredEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Phase 6A.47: If filters provided, use GetEventsQuery for search/filter support
        if (HasFilters(request))
        {
            // Get all event IDs user is registered for
            var registrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);
            var registeredEventIds = registrations.Select(r => r.EventId).Distinct().ToHashSet();

            if (registeredEventIds.Count == 0)
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

            // Filter to only registered events
            var filteredEvents = eventsResult.Value
                .Where(e => registeredEventIds.Contains(e.Id))
                .ToList();

            return Result<IReadOnlyList<EventDto>>.Success(filteredEvents);
        }

        // Original path: No filters, return all registered events
        var allRegistrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);

        if (allRegistrations.Count == 0)
        {
            return Result<IReadOnlyList<EventDto>>.Success(Array.Empty<EventDto>());
        }

        var eventIds = allRegistrations.Select(r => r.EventId).Distinct().ToList();

        // Delegate to GetEventsQuery without filters
        var getAllQuery = new GetEventsQuery();
        var allEventsResult = await _mediator.Send(getAllQuery, cancellationToken);

        if (allEventsResult.IsFailure)
        {
            return Result<IReadOnlyList<EventDto>>.Failure(allEventsResult.Error);
        }

        // Filter to registered events
        var registeredEvents = allEventsResult.Value
            .Where(e => eventIds.Contains(e.Id))
            .ToList();

        return Result<IReadOnlyList<EventDto>>.Success(registeredEvents);
    }

    private static bool HasFilters(GetMyRegisteredEventsQuery request)
    {
        return !string.IsNullOrWhiteSpace(request.SearchTerm)
            || request.Category.HasValue
            || request.StartDateFrom.HasValue
            || request.StartDateTo.HasValue
            || !string.IsNullOrWhiteSpace(request.State)
            || (request.MetroAreaIds != null && request.MetroAreaIds.Any());
    }
}
