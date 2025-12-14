using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.UpdateOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Handler for updating a user-submitted Open item
/// </summary>
public class UpdateOpenSignUpItemCommandHandler : ICommandHandler<UpdateOpenSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOpenSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateOpenSignUpItemCommand request, CancellationToken cancellationToken)
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
            return Result.Failure("Only Open items can be updated using this endpoint");

        if (!item.IsCreatedByUser(request.UserId))
            return Result.Failure("You can only update Open items that you created");

        // Update the item details
        var updateResult = item.UpdateDetails(request.ItemName, request.Quantity, request.Notes);
        if (updateResult.IsFailure)
            return updateResult;

        // Update the user's commitment quantity to match
        var commitment = item.Commitments.FirstOrDefault(c => c.UserId == request.UserId);
        if (commitment != null)
        {
            var commitResult = item.UpdateCommitment(
                request.UserId,
                request.Quantity,
                request.Notes,
                request.ContactName,
                request.ContactEmail,
                request.ContactPhone);

            if (commitResult.IsFailure)
                return commitResult;
        }

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
