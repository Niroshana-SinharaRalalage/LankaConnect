using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Events.Commands.AddPassToEvent;

public class AddPassToEventCommandHandler : ICommandHandler<AddPassToEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddPassToEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddPassToEventCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Create PassName value object
        var passNameResult = PassName.Create(request.PassName);
        if (passNameResult.IsFailure)
            return Result.Failure(passNameResult.Error);

        // Create PassDescription value object
        var passDescriptionResult = PassDescription.Create(request.PassDescription);
        if (passDescriptionResult.IsFailure)
            return Result.Failure(passDescriptionResult.Error);

        // Create Money value object for price
        var priceResult = Money.Create(request.PriceAmount, request.PriceCurrency);
        if (priceResult.IsFailure)
            return Result.Failure(priceResult.Error);

        // Create EventPass entity
        var eventPassResult = EventPass.Create(
            passNameResult.Value,
            passDescriptionResult.Value,
            priceResult.Value,
            request.TotalQuantity);

        if (eventPassResult.IsFailure)
            return Result.Failure(eventPassResult.Error);

        // Add pass to event
        var addResult = @event.AddPass(eventPassResult.Value);
        if (addResult.IsFailure)
            return Result.Failure(addResult.Error);

        // Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
