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

        // Phase 6A.28 Issue 3 Fix: Cancel user's own commitment first (if exists)
        var userCommitment = item.Commitments.FirstOrDefault(c => c.UserId == request.UserId);
        if (userCommitment != null)
        {
            var cancelCommitResult = item.CancelCommitment(request.UserId);
            if (cancelCommitResult.IsFailure)
                return cancelCommitResult;
        }

        // Phase 6A.28 Issue 3 Fix: Check if there are OTHER users' commitments
        // User can only delete their own Open item if no one else has committed to it
        var otherCommitmentsCount = item.Commitments.Count(c => c.UserId != request.UserId);
        if (otherCommitmentsCount > 0)
        {
            return Result.Failure(
                $"Cannot delete this Open item because {otherCommitmentsCount} other user(s) have committed to it. " +
                "Your commitment has been canceled, but the item will remain available for others.");
        }

        // Remove the item from the list (now safe because only user's own commitment existed)
        var removeResult = signUpList.RemoveItem(request.ItemId);
        if (removeResult.IsFailure)
            return removeResult;

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
