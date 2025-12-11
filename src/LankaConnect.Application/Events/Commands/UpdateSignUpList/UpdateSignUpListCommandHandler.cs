using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpList;

/// <summary>
/// Handler for updating sign-up list details
/// Phase 6A.13: Edit Sign-Up List feature
/// </summary>
public class UpdateSignUpListCommandHandler : ICommandHandler<UpdateSignUpListCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSignUpListCommandHandler(
        IEventRepository _eventRepository,
        IUnitOfWork unitOfWork)
    {
        this._eventRepository = _eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSignUpListCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Get the sign-up list from the event
        var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
        if (signUpList == null)
            return Result.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Update the sign-up list using domain method
        var updateResult = signUpList.UpdateDetails(
            request.Category,
            request.Description,
            request.HasMandatoryItems,
            request.HasPreferredItems,
            request.HasSuggestedItems);

        if (updateResult.IsFailure)
            return updateResult;

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
