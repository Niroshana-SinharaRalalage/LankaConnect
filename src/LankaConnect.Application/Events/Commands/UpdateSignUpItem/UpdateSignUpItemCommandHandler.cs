using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpItem;

/// <summary>
/// Handler for updating sign-up item details
/// Phase 6A.14: Edit Sign-Up Item feature
/// </summary>
public class UpdateSignUpItemCommandHandler : ICommandHandler<UpdateSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSignUpItemCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Get the sign-up list from the event
        var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
        if (signUpList == null)
            return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Get the sign-up item from the list
        var signUpItem = signUpList.GetItem(request.SignUpItemId);
        if (signUpItem == null)
            return Result.Failure($"Sign-up item with ID {request.SignUpItemId} not found");

        // Update the sign-up item using domain method
        var updateResult = signUpItem.UpdateDetails(
            request.ItemDescription,
            request.Quantity,
            request.Notes);

        if (updateResult.IsFailure)
            return updateResult;

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
