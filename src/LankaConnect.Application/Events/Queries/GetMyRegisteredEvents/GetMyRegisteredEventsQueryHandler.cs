using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
    private readonly ILogger<GetMyRegisteredEventsQueryHandler> _logger;

    public GetMyRegisteredEventsQueryHandler(
        IRegistrationRepository registrationRepository,
        IMediator mediator,
        ILogger<GetMyRegisteredEventsQueryHandler> logger)
    {
        _registrationRepository = registrationRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(
        GetMyRegisteredEventsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetMyRegisteredEvents"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetMyRegisteredEvents START: UserId={UserId}, HasFilters={HasFilters}",
                request.UserId, HasFilters(request));

            try
            {
                // Validate request
                if (request.UserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetMyRegisteredEvents FAILED: Invalid UserId - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Failure("User ID is required");
                }

                // Phase 6A.47: If filters provided, use GetEventsQuery for search/filter support
                if (HasFilters(request))
                {
                    _logger.LogInformation(
                        "GetMyRegisteredEvents: Using filtered path - SearchTerm={SearchTerm}, Category={Category}",
                        request.SearchTerm, request.Category);

                    // Get all event IDs user is registered for
                    var registrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);
                    var registeredEventIds = registrations.Select(r => r.EventId).Distinct().ToHashSet();

                    _logger.LogInformation(
                        "GetMyRegisteredEvents: User registrations loaded - UserId={UserId}, RegistrationCount={RegistrationCount}",
                        request.UserId, registeredEventIds.Count);

                    if (registeredEventIds.Count == 0)
                    {
                        stopwatch.Stop();

                        _logger.LogInformation(
                            "GetMyRegisteredEvents COMPLETE: No registrations found - UserId={UserId}, Duration={ElapsedMs}ms",
                            request.UserId, stopwatch.ElapsedMilliseconds);

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
                            "GetMyRegisteredEvents FAILED: GetEventsQuery failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.UserId, eventsResult.Error, stopwatch.ElapsedMilliseconds);

                        return Result<IReadOnlyList<EventDto>>.Failure(eventsResult.Error);
                    }

                    // Phase 6A.137: Fix duplicate key crash — GroupBy picks highest-priority status per event
                    var regStatusMap = BuildRegistrationStatusMap(registrations);
                    var filteredEvents = eventsResult.Value
                        .Where(e => registeredEventIds.Contains(e.Id))
                        .Select(e => e with { UserRegistrationStatus = regStatusMap.GetValueOrDefault(e.Id) })
                        .ToList();

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "GetMyRegisteredEvents COMPLETE: UserId={UserId}, TotalResults={TotalResults}, FilteredFromTotal={FilteredFromTotal}, Duration={ElapsedMs}ms",
                        request.UserId, filteredEvents.Count, eventsResult.Value.Count, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Success(filteredEvents);
                }

                // Original path: No filters, return all registered events
                _logger.LogInformation(
                    "GetMyRegisteredEvents: Using unfiltered path - UserId={UserId}",
                    request.UserId);

                var allRegistrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);

                _logger.LogInformation(
                    "GetMyRegisteredEvents: All registrations loaded - UserId={UserId}, RegistrationCount={RegistrationCount}",
                    request.UserId, allRegistrations.Count);

                if (allRegistrations.Count == 0)
                {
                    stopwatch.Stop();

                    _logger.LogInformation(
                        "GetMyRegisteredEvents COMPLETE: No registrations found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Success(Array.Empty<EventDto>());
                }

                var eventIds = allRegistrations.Select(r => r.EventId).Distinct().ToList();

                // Delegate to GetEventsQuery without filters
                var getAllQuery = new GetEventsQuery();
                var allEventsResult = await _mediator.Send(getAllQuery, cancellationToken);

                if (allEventsResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetMyRegisteredEvents FAILED: GetEventsQuery failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, allEventsResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Failure(allEventsResult.Error);
                }

                // Phase 6A.137: Fix duplicate key crash — GroupBy picks highest-priority status per event
                var registrationMap = BuildRegistrationStatusMap(allRegistrations);
                var registeredEvents = allEventsResult.Value
                    .Where(e => eventIds.Contains(e.Id))
                    .Select(e => e with { UserRegistrationStatus = registrationMap.GetValueOrDefault(e.Id) })
                    .ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetMyRegisteredEvents COMPLETE: UserId={UserId}, TotalResults={TotalResults}, Duration={ElapsedMs}ms",
                    request.UserId, registeredEvents.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EventDto>>.Success(registeredEvents);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetMyRegisteredEvents FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
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

    /// <summary>
    /// Phase 6A.137: Build a registration status map that handles duplicate registrations per event.
    /// When a user has multiple registrations for the same event (e.g., Preliminary + Confirmed),
    /// picks the highest-priority status. This prevents the ToDictionary ArgumentException crash.
    /// </summary>
    private static Dictionary<Guid, RegistrationStatus> BuildRegistrationStatusMap(
        IReadOnlyList<Registration> registrations)
    {
        return registrations
            .GroupBy(r => r.EventId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => GetStatusPriority(r.Status)).First().Status);
    }

    /// <summary>
    /// Phase 6A.137: Status priority for deduplication (lower = higher priority).
    /// Confirmed is highest priority since it represents a completed registration.
    /// </summary>
    private static int GetStatusPriority(RegistrationStatus status) => status switch
    {
        RegistrationStatus.Confirmed => 0,
        RegistrationStatus.CheckedIn => 1,
        RegistrationStatus.Attended => 2,
        RegistrationStatus.Waitlisted => 3,
        RegistrationStatus.RefundRequested => 4,
        RegistrationStatus.Preliminary => 5,
        _ => 6
    };
}
