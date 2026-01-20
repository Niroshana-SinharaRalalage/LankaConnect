using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetUpcomingEventsForUser;

public class GetUpcomingEventsForUserQueryHandler : IQueryHandler<GetUpcomingEventsForUserQuery, IReadOnlyList<EventDto>>
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUpcomingEventsForUserQueryHandler> _logger;

    public GetUpcomingEventsForUserQueryHandler(
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IMapper mapper,
        ILogger<GetUpcomingEventsForUserQueryHandler> logger)
    {
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetUpcomingEventsForUserQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetUpcomingEventsForUser"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetUpcomingEventsForUser START: UserId={UserId}",
                request.UserId);

            try
            {
                // Validate request
                if (request.UserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUpcomingEventsForUser FAILED: Invalid UserId - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Failure("User ID is required");
                }

                // Get all confirmed registrations for the user
                var registrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);
                var confirmedRegistrations = registrations.Where(r => r.Status == RegistrationStatus.Confirmed).ToList();

                _logger.LogInformation(
                    "GetUpcomingEventsForUser: Registrations loaded - UserId={UserId}, TotalRegistrations={TotalRegistrations}, ConfirmedCount={ConfirmedCount}",
                    request.UserId, registrations.Count(), confirmedRegistrations.Count);

                // Get event IDs
                var eventIds = confirmedRegistrations.Select(r => r.EventId).ToList();

                // Fetch events
                var upcomingEvents = new List<EventDto>();

                foreach (var eventId in eventIds)
                {
                    var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);

                    // Filter: upcoming events (start date in the future) and published status
                    if (@event != null && @event.StartDate > DateTime.UtcNow && @event.Status == EventStatus.Published)
                    {
                        upcomingEvents.Add(_mapper.Map<EventDto>(@event));
                    }
                }

                // Sort by start date
                var sortedEvents = upcomingEvents.OrderBy(e => e.StartDate).ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetUpcomingEventsForUser COMPLETE: UserId={UserId}, UpcomingEventsCount={UpcomingEventsCount}, Duration={ElapsedMs}ms",
                    request.UserId, sortedEvents.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EventDto>>.Success(sortedEvents);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetUpcomingEventsForUser FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
