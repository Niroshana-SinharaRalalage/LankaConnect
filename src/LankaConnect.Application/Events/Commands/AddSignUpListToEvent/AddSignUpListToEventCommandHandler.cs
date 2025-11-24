using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.AddSignUpListToEvent;

public class AddSignUpListToEventCommandHandler : ICommandHandler<AddSignUpListToEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddSignUpListToEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddSignUpListToEventCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Create sign-up list based on type
        Result<SignUpList> signUpListResult;

        if (request.SignUpType == SignUpType.Predefined && request.PredefinedItems != null && request.PredefinedItems.Any())
        {
            signUpListResult = SignUpList.CreateWithPredefinedItems(
                request.Category,
                request.Description,
                request.PredefinedItems);
        }
        else
        {
            signUpListResult = SignUpList.Create(
                request.Category,
                request.Description,
                request.SignUpType);
        }

        if (signUpListResult.IsFailure)
            return Result.Failure(signUpListResult.Error);

        // Add to event
        var addResult = @event.AddSignUpList(signUpListResult.Value);
        if (addResult.IsFailure)
            return Result.Failure(addResult.Error);

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
