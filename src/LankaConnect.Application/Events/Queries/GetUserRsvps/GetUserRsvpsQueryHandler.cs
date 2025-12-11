using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.GetUserRsvps;

public class GetUserRsvpsQueryHandler : IQueryHandler<GetUserRsvpsQuery, IReadOnlyList<RsvpDto>>
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetUserRsvpsQueryHandler(
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<RsvpDto>>> Handle(GetUserRsvpsQuery request, CancellationToken cancellationToken)
    {
        // Get all registrations for the user
        var registrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);

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

        return Result<IReadOnlyList<RsvpDto>>.Success(rsvpDtos);
    }
}
