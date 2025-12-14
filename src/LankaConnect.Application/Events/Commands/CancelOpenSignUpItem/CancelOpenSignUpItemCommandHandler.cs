using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.CancelOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Handler for canceling (deleting) a user-submitted Open item
/// </summary>
public class CancelOpenSignUpItemCommandHandler : ICommandHandler<CancelOpenSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOpenSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CancelOpenSignUpItemCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Get the sign-up list from the event
        var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
        if (signUpList == null)
            return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Get the item
        var item = signUpList.GetItem(request.ItemId);
        if (item == null)
            return Result.Failure($"Sign-up item with ID {request.ItemId} not found");

        // Verify this is an Open item created by this user
        if (item.ItemCategory != SignUpItemCategory.Open)
            return Result.Failure("Only Open items can be canceled using this endpoint");

        if (!item.IsCreatedByUser(request.UserId))
            return Result.Failure("You can only cancel Open items that you created");

        // Cancel/remove the user's commitment first
        var cancelCommitResult = item.CancelCommitment(request.UserId);
        if (cancelCommitResult.IsFailure)
            return cancelCommitResult;

        // Remove the item from the list
        var removeResult = signUpList.RemoveItem(request.ItemId);
        if (removeResult.IsFailure)
            return removeResult;

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
