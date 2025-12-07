using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.CommitToSignUpItem;

public class CommitToSignUpItemCommandHandler : ICommandHandler<CommitToSignUpItemCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CommitToSignUpItemCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CommitToSignUpItemCommand request, CancellationToken cancellationToken)
    {
        // Get the event with sign-up lists
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Get the sign-up list
        var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
        if (signUpList == null)
            return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Get the sign-up item
        var signUpItem = signUpList.GetItem(request.SignUpItemId);
        if (signUpItem == null)
            return Result.Failure($"Sign-up item with ID {request.SignUpItemId} not found");

        // Phase 6A.17: Support both creating new commitments and updating existing ones
        // Check if user already has a commitment to this item
        var existingCommitment = signUpItem.Commitments.FirstOrDefault(c => c.UserId == request.UserId);

        Result commitResult;
        if (existingCommitment != null)
        {
            // User already committed - update the existing commitment
            commitResult = signUpItem.UpdateCommitment(
                request.UserId,
                request.Quantity,
                request.Notes,
                request.ContactName,
                request.ContactEmail,
                request.ContactPhone);
        }
        else
        {
            // New commitment - add it (Phase 2: with contact info)
            commitResult = signUpItem.AddCommitment(
                request.UserId,
                request.Quantity,
                request.Notes,
                request.ContactName,
                request.ContactEmail,
                request.ContactPhone);
        }

        if (commitResult.IsFailure)
            return Result.Failure(commitResult.Error);

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
