using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetUserRsvps;

public class GetUserRsvpsQueryHandler : IQueryHandler<GetUserRsvpsQuery, IReadOnlyList<RsvpDto>>
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserRsvpsQueryHandler> _logger;

    public GetUserRsvpsQueryHandler(
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IMapper mapper,
        ILogger<GetUserRsvpsQueryHandler> logger)
    {
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<RsvpDto>>> Handle(GetUserRsvpsQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetUserRsvps"))
        using (LogContext.PushProperty("EntityType", "Rsvp"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetUserRsvps START: UserId={UserId}",
                request.UserId);

            try
            {
                // Validate request
                if (request.UserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserRsvps FAILED: Invalid UserId - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<RsvpDto>>.Failure("User ID is required");
                }

                // Get all registrations for the user
                var registrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);

                _logger.LogInformation(
                    "GetUserRsvps: Registrations loaded - UserId={UserId}, RegistrationCount={RegistrationCount}",
                    request.UserId, registrations.Count());

                // Map to DTOs
                var rsvpDtos = new List<RsvpDto>();

                foreach (var registration in registrations)
                {
                    // Get event details to populate event information in DTO
                    var @event = await _eventRepository.GetByIdAsync(registration.EventId, cancellationToken);

                    var rsvpDto = _mapper.Map<RsvpDto>(registration);

                    // Manually populate event information
                    if (@event != null)
                    {
                        rsvpDto = rsvpDto with
                        {
                            EventTitle = @event.Title.Value,
                            EventStartDate = @event.StartDate,
                            EventEndDate = @event.EndDate,
                            EventStatus = @event.Status
                        };
                    }

                    rsvpDtos.Add(rsvpDto);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetUserRsvps COMPLETE: UserId={UserId}, RsvpCount={RsvpCount}, Duration={ElapsedMs}ms",
                    request.UserId, rsvpDtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<RsvpDto>>.Success(rsvpDtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetUserRsvps FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
