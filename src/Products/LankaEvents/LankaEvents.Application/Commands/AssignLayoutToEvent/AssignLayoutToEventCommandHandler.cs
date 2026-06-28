using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Products.LankaEvents.Application.Commands.AssignLayoutToEvent;

public class AssignLayoutToEventCommandHandler : ICommandHandler<AssignLayoutToEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IVenueLayoutRepository _venueLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignLayoutToEventCommandHandler> _logger;

    public AssignLayoutToEventCommandHandler(
        IEventRepository eventRepository,
        IVenueLayoutRepository venueLayoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignLayoutToEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _venueLayoutRepository = venueLayoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignLayoutToEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Assigning layout to event: EventId={EventId}, LayoutId={LayoutId}",
            request.EventId, request.LayoutId);

        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        var layout = await _venueLayoutRepository.GetWithZonesAndSeatsAsync(request.LayoutId, cancellationToken);
        if (layout == null)
            return Result.Failure("Venue layout not found");

        // Validate zone→tier mapping and capacity
        if (@event.HasTicketTiers)
        {
            var validationResult = layout.ValidateForEvent(@event.TicketTiers);
            if (validationResult.IsFailure)
                return validationResult;
        }

        // Assign layout to event
        var assignResult = layout.AssignToEvent(request.EventId);
        if (assignResult.IsFailure)
            return assignResult;

        // Set seating mode to AssignedSeating
        var seatingResult = @event.SetSeatingMode(SeatingMode.AssignedSeating);
        if (seatingResult.IsFailure)
            return seatingResult;

        // Set the venue layout reference on the event
        var layoutResult = @event.AssignVenueLayout(request.LayoutId);
        if (layoutResult.IsFailure)
            return layoutResult;

        _venueLayoutRepository.Update(layout);
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Layout assigned to event: EventId={EventId}, LayoutId={LayoutId}, TotalCapacity={Capacity}",
            request.EventId, request.LayoutId, layout.TotalCapacity);

        return Result.Success();
    }
}
